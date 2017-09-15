/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RailwayEssentialCore;
using RailwayEssentialMdi.ViewModels;
using TrackInformation;
using TrackInformationCore;
using TrackPlanParser;
using TrackWeaver;

namespace RailwayEssentialMdi.Autoplay
{
    public class AutoplayRouteThread
    {
        public RailwayEssentialModel Model { get; set; }
        public TrackWeaver.TrackWeaver Weaver => Model.Dispatcher.Weaver;
        public Analyze.Route Route { get; set; }
        public TrackInfo SrcBlock { get; private set; }
        public TrackInfo DestBlock { get; private set; }
        public Task Task { get; private set; }
        public Autoplay Autoplayer { get; set; }

        private CancellationTokenSource _cts;
        private CancellationToken _tkn;

        private static readonly string Prefix = "§ ";

        public static AutoplayRouteThread Start(RailwayEssentialModel model, Autoplay autoplayer, Analyze.Route route)
        {
            var c = new AutoplayRouteThread
            {
                Model = model,
                Autoplayer = autoplayer,
                Route = route
            };

            c.Initialize();

            return c;
        }

        private void Initialize()
        {
            var firstItem = Route.First();
            var lastItem = Route.Last();

            if (firstItem == lastItem)
                return;

            if (firstItem == null || lastItem == null)
                return;

            SrcBlock = Model.TrackEntity.Track.Get(firstItem.X, firstItem.Y);
            DestBlock = Model.TrackEntity.Track.Get(lastItem.X, lastItem.Y);
        }

        public bool IsRunning
        {
            get
            {
                if (_cts == null)
                    return false;
                if (Task.Status == TaskStatus.Running || Task.Status == TaskStatus.WaitingToRun)
                    return true;
                if (Task.Status == TaskStatus.Canceled)
                    return false;
                return false;
            }
        }

        private Dictionary<string, string> GetBlockData(TrackInfo destBlock)
        {
            if (destBlock == null)
                return null;

            Trace.WriteLine($"{Prefix} Destination {destBlock}");

            var eventSpec = DestBlock.GetOption("events");
            JObject events = null;
            if (!string.IsNullOrEmpty(eventSpec))
                events = JObject.Parse(eventSpec);
            if (events == null)
                return null;

            Dictionary<string, string> results = new Dictionary<string, string>();

              string[] sensors = new string[3];
            string[] eventNames = new string[3];
            for (int i = 0; i < 3; ++i)
            {
                if (events[$"sensor{i}"] != null)
                    sensors[i] = events[$"sensor{i}"].ToString();

                if (events[$"event{i}"] != null)
                    eventNames[i] = events[$"event{i}"].ToString();

                if (!string.IsNullOrEmpty(sensors[i]))
                {
                    if (results.ContainsKey(sensors[i]))
                    {
                        // ignore
                    }
                    else
                    {
                        results.Add(sensors[i], eventNames[i]);
                    }
                }
            }

            return results;
        }

        private class ItemData
        {
            public Analyze.Route Route { get; set; }
            public TrackInfo Info { get; set; }
            public IItem Item { get; set; }
            public TrackInformation.Switch ItemSwitch => Item as TrackInformation.Switch;
            public S88 ItemS88 => Item as S88;
            public Func<TrackCheckResult> S88Checker { get; set; }
            public bool S88HasBeenHandled { get; set; }
            
            public bool IsS88 => Item is S88;
            public bool IsSwitch => Item is TrackInformation.Switch;
            public bool HasSwitchTurn
            {
                get
                {
                    if (!IsSwitch)
                        return false;

                    foreach (var r in Route)
                    {
                        if (r == null)
                            continue;

                        if (r.X != Info.X)
                            continue;

                        if (r.Y != Info.Y)
                            continue;

                        if (!Globals.SwitchIds.Contains(r.ThemeId))
                            return false;

                        return r.HasTurn;
                    }

                    return false;
                }
            }

            public string DestBlockEvent { get; set; }
        }

        private ItemData Get(List<ItemData> items, int x, int y)
        {
            if (items == null || items.Count == 0)
                return null;
            if (x == -1 || y == -1)
                return null;

            foreach (var it in items)
            {
                if (it == null)
                    continue;
                if (it.Info == null)
                    continue;
                if (it.Info.X == x && it.Info.Y == y)
                    return it;
            }

            return null;
        }

        public void Start()
        {
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();
            _tkn = _cts.Token;

            Task = new Task(() =>
            {
                // **********************************************************************
                // ** Route Thread 
                // **********************************************************************

                bool isRouteSet = false; // flag initialization of route's handling thread
                Locomotive locObject = null; // the current locomotive on the route
                List<ItemData> routeData = new List<ItemData>(); // s88 and switches on the route 

                bool fncHasBeenStarted = false;
                bool fncHasBeenStopped = false;

                for (;;)
                {
                    var s = SrcBlock.ToString().Replace(" ", "");
                    var d = DestBlock.ToString().Replace(" ", "");

                    if (!isRouteSet)
                    {
                        Route.IsBusy = true;
                        isRouteSet = true;
                        Autoplayer?.SetRoute(Route, true);
                        int locObjectId = -1;
                        if (Autoplayer != null)
                            locObjectId = Autoplayer.GetLocObjectIdOfRoute(Route);
                        locObject = Model.Dispatcher.GetDataProvider().GetObjectBy(locObjectId) as Locomotive;
                        DestBlock.SetLocomotivePreviewObjectId(locObjectId);

                        if (locObject != null)
                        {
                            Model?.LogAutoplay($"{Prefix} Locomotive: {locObject.Name}\n");
                            Trace.WriteLine($"{Prefix} Locomotive: {locObject.Name}");
                        }

                        TrackWeaveItems weaverItems = new TrackWeaveItems();
                        var weaveFilepath = Path.Combine(Model.Project.Dirpath, Model.Project.Track.Weave);
                        if (!weaverItems.Load(weaveFilepath))
                            throw new Exception("Reading weave file failed.");

                        Dictionary<TrackInfo, List<IItem>> trackObjects = new Dictionary<TrackInfo, List<IItem>>();

                        foreach (var pt in Route)
                        {
                            if (pt == null)
                                continue;

                            var item = Model.TrackEntity.Track.Get(pt.X, pt.Y);
                            if (item == null)
                                continue;

                            var itemObjects = Model.Dispatcher.Weaver.GetObject(item);
                            if (itemObjects.Count == 0)
                                continue;

                            if (trackObjects.ContainsKey(item))
                                trackObjects[item].AddRange(itemObjects);
                            else
                                trackObjects.Add(item, itemObjects);
                        }

                        #region DEBUG route's track info
                        Trace.WriteLine($"{Prefix} Route's track infos:");
                        foreach (var info in trackObjects.Keys)
                        {
                            var objs = trackObjects[info];

                            Trace.Write($"{Prefix} {info}: ");
                            foreach (var o in objs)
                                Trace.Write($"{o.ObjectId}, ");
                            Trace.WriteLine("||");
                        }
                        #endregion

                        Dictionary<TrackInfo, S88> s88TrackObjects = new Dictionary<TrackInfo, S88>();
                        Dictionary<TrackInfo, TrackInformation.Switch> switchTrackObjects = new Dictionary<TrackInfo, TrackInformation.Switch>();

                        #region prepare route data

                        foreach (var trackInfo in trackObjects.Keys)
                        {
                            var itemObjects = trackObjects[trackInfo];
                            if (itemObjects.Count == 0)
                                continue;

                            foreach (var obj in itemObjects)
                            {
                                if (obj == null)
                                    continue;

                                if (obj is S88)
                                    s88TrackObjects.Add(trackInfo, obj as S88);

                                if(obj is TrackInformation.Switch)
                                    switchTrackObjects.Add(trackInfo, obj as TrackInformation.Switch);
                            }
                        }

                        foreach (var trackInfo in s88TrackObjects.Keys)
                        {
                            var s88Obj = s88TrackObjects[trackInfo];

                            var fnc = Weaver.GetCheckFnc(s88Obj, trackInfo);
                            if (fnc == null)
                                Model.LogError($"S88-Checker is missing: {s88Obj}");

                            var data = new ItemData
                            {
                                Route = Route,
                                Info = trackInfo,
                                Item = s88Obj,
                                S88Checker = fnc
                            };

                            routeData.Add(data);
                        }

                        foreach (var trackInfo in switchTrackObjects.Keys)
                        {
                            var switchObj = switchTrackObjects[trackInfo];

                            var data = new ItemData
                            {
                                Route = Route,
                                Info = trackInfo,
                                Item = switchObj
                            };

                            routeData.Add(data);
                        }

                        var sensorsAndEvents = GetBlockData(DestBlock);
                        if (sensorsAndEvents != null)
                        {
                            foreach (var sensorName in sensorsAndEvents.Keys)
                            {
                                var eventName = sensorsAndEvents[sensorName];

                                TrackInfo sensorTrackInfo = null;

                                foreach (var item in Model.TrackEntity.Track)
                                {
                                    if (item == null || string.IsNullOrEmpty(item.Name))
                                        continue;

                                    if (item.Name.Equals(sensorName))
                                    {
                                        sensorTrackInfo = item;
                                        break;
                                    }
                                }

                                if (sensorTrackInfo != null)
                                {
                                    var it = Get(routeData, sensorTrackInfo.X, sensorTrackInfo.Y);

                                    if (it == null)
                                    {
                                        var s88Obj = Helper.GetObject(Model.Dispatcher, Model.TrackEntity.Track, sensorTrackInfo.X, sensorTrackInfo.Y);

                                        var fnc = Weaver.GetCheckFnc(s88Obj, sensorTrackInfo);
                                        if (fnc == null)
                                            Model.LogError($"S88-Checker is missing: {s88Obj}");

                                        var data = new ItemData
                                        {
                                            Route = Route,
                                            Info = sensorTrackInfo,
                                            Item = s88Obj,
                                            DestBlockEvent = eventName,
                                            S88Checker = fnc
                                        };

                                        routeData.Add(data);
                                    }
                                    else
                                    {
                                        it.DestBlockEvent = eventName;
                                    }

                                    Trace.WriteLine($"{Prefix} Sensor({sensorName}) with Event({eventName})");
                                }
                            }
                        }

                        #endregion

                        #region set switches to let the locomotive pass the route

                        foreach (var data in routeData)
                        {
                            if (data == null || !data.IsSwitch || data.ItemSwitch == null)
                                continue;

                            var sw = data.ItemSwitch;
                            var v = data.HasSwitchTurn ? 0 : 1;
                            if (sw.InvertCommand)
                            {
                                if (v == 1) v = 0;
                                else v = 1;
                            }
                            var vs = v == 1 ? "TURN" : "STRAIGHT";
                            Trace.WriteLine($"{Prefix} Switch '{sw.Name1}' change to '{vs}'");
                            sw.ChangeDirection(v);
                        }

                        #endregion

                        if (locObject != null)
                        {
                            locObject.ChangeDirection(false);
                            locObject.ChangeSpeed(locObject.MaxSpeedPercentage);
                        }

                        Model?.LogAutoplay($"{Prefix} {s}  TO  {d}\n");
                        Trace.WriteLine($"{Prefix} {s}  TO  {d}");

                        Model.UiSyncCtx?.Send((x) =>
                        {
                            Model.TrackEntity.UpdateAllVisualBlocks();
                        }, new object());


                    }

                    foreach (var s88Data in routeData)
                    {
                        if (s88Data == null || !s88Data.IsS88 || s88Data.ItemS88 == null)
                            continue;

                        if (s88Data.S88HasBeenHandled)
                            continue;

                        bool state = false;
                        try
                        {
                            var bb = s88Data.S88Checker();
                            if (bb != null && bb.State != null)
                                state = bb.State.Value;
                        }
                        catch(Exception ex)
                        {
                            Model?.LogAutoplay($"{Prefix} {ex.Message}\n");
                            Trace.WriteLine($"{Prefix} {ex.Message}");
                        }

                        if (state && !s88Data.S88HasBeenHandled)
                        {
                            s88Data.S88HasBeenHandled = true;

                            var weaveItem = Helper.GetWeaveItem(this.Model.Dispatcher, s88Data.Info.X, s88Data.Info.Y);

                            Func<Locomotive, bool> startFncs = delegate(Locomotive locObj)
                            {
                                var startFncGroup = weaveItem.StartFncGroupTypes;
                                
                                foreach (var i in startFncGroup)
                                {
                                    var fncName = Enum.GetNames(typeof(FncGroupTypes))[(int)i];
                                    Model?.LogAutoplay($"{Prefix} {locObj.Name} switch on {fncName}\n");
                                    Trace.WriteLine($"{Prefix} {locObj.Name} switch on {fncName}");
                                    locObj.ToggleFunctionType((int)i, true);
                                }

                                return startFncGroup.Count > 0;

                            };

                            Func<Locomotive, bool> stopFncs = delegate(Locomotive locObj)
                            {
                                var stopFncGroup = weaveItem.StopFncGroupTypes;
                                foreach (var i in stopFncGroup)
                                {
                                    var fncName = Enum.GetNames(typeof(FncGroupTypes))[(int)i];
                                    Model?.LogAutoplay($"{Prefix} {locObj.Name} switch off {fncName}\n");
                                    Trace.WriteLine($"{Prefix} {locObj.Name} switch off {fncName}");
                                    locObj.ToggleFunctionType((int)i, false);
                                }

                                return stopFncGroup.Count > 0;
                            };

                            string evName = s88Data.DestBlockEvent;
                            bool isInEvent = false;
                            bool isEnterEvent = false;

                            if (!string.IsNullOrEmpty(evName))
                            {
                                isInEvent = evName.Equals("in", StringComparison.OrdinalIgnoreCase);
                                isEnterEvent = evName.Equals("enter", StringComparison.OrdinalIgnoreCase);
                            }

                            if (weaveItem != null)
                            {
                                var toggleFncs = weaveItem.FncToggle;

                                if (toggleFncs)
                                {
                                    if (!fncHasBeenStarted && fncHasBeenStopped && !isInEvent)
                                    {
                                        startFncs(locObject);
                                        fncHasBeenStarted = true;
                                        fncHasBeenStopped = false;
                                    }
                                    else if (fncHasBeenStarted && fncHasBeenStopped)
                                    {
                                        // ignore
                                    }
                                    else if (fncHasBeenStarted && !fncHasBeenStopped)
                                    {
                                        if (isEnterEvent)
                                        {
                                            fncHasBeenStopped = stopFncs(locObject);
                                            fncHasBeenStarted = false;
                                        }
                                    }
                                    else
                                    {
                                        if (!fncHasBeenStarted && !fncHasBeenStopped)
                                        {
                                            fncHasBeenStarted = startFncs(locObject);
                                            fncHasBeenStopped = false;
                                        }
                                    }
                                }
                                else
                                {
                                    fncHasBeenStarted = startFncs(locObject);
                                    fncHasBeenStopped = stopFncs(locObject);
                                }

                                Model?.UpdateWindowUi(1);
                            }

                            if (!string.IsNullOrEmpty(evName))
                            {
                                var stopped = false;

                                if (evName.Equals("enter", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (locObject != null)
                                    {
                                        var blockSpeed = locObject.BlockSpeedPercentage;
                                        if (blockSpeed >= locObject.MaxSpeedPercentage)
                                            blockSpeed = -1;

                                        if (blockSpeed <= 0)
                                        {
                                            var currentSpeed = locObject.Speed;
                                            currentSpeed -= (int) (currentSpeed / 2.0f);
                                            locObject.ChangeSpeed(currentSpeed);
                                            Model?.LogAutoplay($"{Prefix} [ENTER] {locObject.Name} change speed to {currentSpeed}\n");
                                        }
                                        else
                                        {
                                            locObject.ChangeSpeed(blockSpeed);
                                            Model?.LogAutoplay($"{Prefix} [ENTER] {locObject.Name} change speed to {blockSpeed}\n");
                                        }
                                    }

                                    Trace.WriteLine($"{Prefix} New speed {Locomotive.SpeedBlockEntered} for {locObject.Name}");
                                }
                                else if (evName.Equals("enter in", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (locObject != null)
                                    {
                                        var currentSpeed = locObject.Speed;
                                        var secondsToStop = 3;
                                        int speedSteps = currentSpeed / secondsToStop;

                                        var o = locObject;

                                        Model?.LogAutoplay($"{Prefix} [ENTER IN] {locObject.Name} change speed to {Locomotive.SpeedStop} in {secondsToStop} seconds.\n");

                                        var task = Task.Run(() =>
                                        {
                                            currentSpeed -= speedSteps;

                                            if (currentSpeed <= 0)
                                            {
                                                o?.ChangeSpeed(Locomotive.SpeedStop);
                                            }
                                            else
                                            {
                                                o?.ChangeSpeed(currentSpeed);
                                            }

                                            Trace.WriteLine($"{Prefix} Loc speed {locObject.Name} is {currentSpeed}");

                                            Thread.Sleep(1 * 1000);
                                        });

                                        task.Wait();

                                        stopped = true;
                                    }
                                }
                                else if (evName.Equals("in", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (locObject != null)
                                        locObject.ChangeSpeed(Locomotive.SpeedStop);

                                    Model?.LogAutoplay($"{Prefix} [IN] {locObject.Name} change speed to {Locomotive.SpeedStop}\n");
                                    Trace.WriteLine($"{Prefix} STOP {Locomotive.SpeedStop}");

                                    stopped = true;
                                }

                                if (stopped)
                                {
                                    // move loc from source block to destination block
                                    // reset current route
                                    // be ready for next routing decision

                                    SrcBlock.SetLocomotiveObjectId(-1);
                                    DestBlock.SetLocomotiveObjectId(locObject.ObjectId);
                                    DestBlock.SetLocomotivePreviewObjectId(-1);
                                    Model.UiSyncCtx?.Send(x =>
                                    {
                                        Autoplayer.SetRoute(Route, false);
                                        Model.TrackEntity.UpdateAllVisualBlocks();
                                        Model.Save();
                                    }, new object());
                                    Route.IsBusy = false;
                                    Route.StartBusiness = DateTime.MaxValue;
                                    Route.StopBusiness = DateTime.Now;

                                    if (_cts != null)
                                    {
                                        try
                                        {
                                            _cts.Dispose();
                                            _cts = null;
                                        }
                                        catch
                                        {
                                            // ignore
                                        }                
                                    }

                                    return;
                                }
                            }
                        }
                    }

                    #region Thread stuff

                    Thread.Sleep(1 * 125);

                    if (_tkn.IsCancellationRequested)
                    {
                        Trace.WriteLine($"{Prefix} Stop requested...");
                        Route.IsBusy = false;
                        return;
                    }

                    #endregion
                }

                // **********************************************************************
                // ** END Route Thread 
                // **********************************************************************

            }, _tkn);

            Task?.Start();
        }

        public void Stop(bool waitFor=false)
        {
            try
            {
                if (!_tkn.CanBeCanceled)
                    return;

                if (_cts == null)
                    return;

                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();

                    if (waitFor)
                    {

                        bool r = Task.Wait(30 * 1000);
                        if (!r)
                            throw new Exception("Can not stop thread for Route: " + Route);

                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Execution of Task failed: " + ex.Message);
            }
            finally
            {
                
            }
        }

        public void Cleanup()
        {
            try
            {
                _cts?.Dispose();
                _cts = null;
            }
            catch
            {
                // ignore
            }

            try
            {
                Task?.Dispose();
                Task = null;
            }
            catch
            {
                // ignore
            }
        }
    }
}