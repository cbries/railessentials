// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoMode.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecoslib.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Analyzer;
using railessentials.Occ;
using railessentials.Plan;
using railessentials.Route;
using RouteList = railessentials.Route.RouteList;
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Local
// ReSharper disable RemoveRedundantBraces

namespace railessentials.AutoMode
{
    public delegate void AutoModeStarted(AutoMode sender);
    public delegate void AutoModeStopping(AutoMode sender);
    public delegate void AutoModeStopped(AutoMode sender);
    public delegate void AutoModeFailed(AutoMode sender, string reason);
    public delegate void AutoModeFauled(AutoMode sender, Exception ex);
    public delegate void AutoModeUpdate(AutoMode sender, string message);

    public class AutoMode
    {
        public const int RunPauseForBlockSeconds = 10;
        private const int RunDelayBetweenChecksMsecs = 2500;

        public event AutoModeStarted Started;
        public event AutoModeStopping Stopping;
        public event AutoModeStopped Stopped;
        public event AutoModeUpdate Update;

        private readonly ClientHandler.ClientHandler _ctx;
        private bool _isStopped = true;
        private bool _isStopping;
        private bool _isStarted;

        internal Metadata _metadata;
        internal object _metadataLock;
        private DataProvider _dataProvider;
        private DataProvider _dataProviderS88;
        private RouteList _routeList;
        private PlanField _planfield;

        private readonly object _autoModeTasksLock = new();
        private readonly List<AutoModeTaskCore> _autoModeTasks = new();

        internal ClientHandler.ClientHandler GetClientHandler()
        {
            return _ctx;
        }

        public AutoMode(ClientHandler.ClientHandler ctx)
        {
            _ctx = ctx;
        }

        public bool IsStarted()
        {
            return _isStarted && !_isStopped;
        }

        public bool IsStopping()
        {
            return _isStopping && !_isStopped;
        }

        public bool IsStopped()
        {
            return _isStopped;
        }

        public void Stop()
        {
            _isStarted = false;
            _isStopping = true;

            Update?.Invoke(this, "AutoMode STOPPING");
        }

        public void StartLocomotive(int oid)
        {
            // This call is a dummy and not really used in the moment.
            // When the locomotive is not stopped 
            // (i.e. railessentials.Locomotives.Data.IsStopped := false)
            // then the locomotive will start in one of the next
            // iteration of finding a free route

            // REMARK Maybe we will change this behaviour in future, keep this method!
        }

        public void FinalizeLocomotive(int oid)
        {
            // This call do not stop the locomotive immediatelly.
            // The flag for disabling the loc is already set to
            // true (i.e. IsStopped:=true) and this will not start
            // a next round for the loc. The current trip will finish
            // until the loc reaches it's current target final block.

            // REMARK Maybe we will change this behaviour in future, keep this method!
        }

        public void StopLocomotive(int oid)
        {
            if (oid <= 0) return;

            lock (_autoModeTasksLock)
            {
                foreach (var it in _autoModeTasks)
                    it?.Cancel();
            }
        }

        public async Task HandleFeedbacks()
        {
            await Task.Run(() =>
            {
                //_ctx?.Logger?.Log.Info("+++ handle feedbacks +++");
            });
        }

        public async Task Run()
        {
            Initialize();

            await Task.Run(async () =>
            {
                _isStarted = true;
                _isStopped = false;
                _isStopping = false;

                Started?.Invoke(this);
                Update?.Invoke(this, "AutoMode START");

                while (_isStopped == false)
                {
                    if (_isStopping) break;

                    var nextRouteInformation = CheckForRoutesAndAssign();
                    if (nextRouteInformation != null)
                    {
                        LogInfo($"{nextRouteInformation}");

                        var instance = AutoModeTaskBase.Create(nextRouteInformation, this);
                        instance.Finished += Instance_Finished;
                        lock (_autoModeTasksLock)
                        {
                            _autoModeTasks.Add(instance);
                        }

                        try
                        {
                            _ = Task.Run(async () => await instance.Run());
                        }
                        catch
                        {
                            // catch any exception
                            // do not bubble them up
                        }
                    }

                    System.Threading.Thread.Sleep(RunDelayBetweenChecksMsecs);
                }

                // stop all tasks, cleanup tasks and event handler
                if (_isStopping)
                {
                    //
                    // Because of issue #75 we do not cancel running tasks.
                    // If tasks are canceled during run, the locomotive will 
                    // not stop automatically in their destination.
                    // For imporving this, we will wait at this point
                    // of execution until all tasks are finished.
                    // REMARK: https://github.com/cbries/railessentials/issues/75
                    //

                    Stopping?.Invoke(this);

                    await WaitForTasks();
                }

                _isStopped = true;

                Stopped?.Invoke(this);
            });
        }

        private async Task WaitForTasks()
        {
            await Task.Run(() =>
            {
                lock (_autoModeTasksLock)
                {
                    var iMax = 0;
                    foreach (var it in _autoModeTasks)
                    {
                        if (it == null) continue;
                        ++iMax;
                    }

                    var listOfFinishedTasks = new List<int>();
                    var previousMessage = string.Empty;

                    //
                    // TODO add walltime to avoid endless waiting
                    //
                    while (true)
                    {
                        var noOfWaitingTasks = iMax - listOfFinishedTasks.Count;
                        var allTasksStopped = iMax == 0;
                        
                        for (var j = 0; j < iMax; ++j)
                        {
                            var task = _autoModeTasks[j];
                            if (task == null) continue;
                            if (task.IsFinished)
                            {
                                task.Finished -= Instance_Finished;
                                if (!listOfFinishedTasks.Contains(j))
                                    listOfFinishedTasks.Add(j);
                            }
                            else
                            {
                                // ignore
                            }

                            var m = $"Waiting for {noOfWaitingTasks} locomotives...";
                            if(!m.Equals(previousMessage, StringComparison.OrdinalIgnoreCase))
                            {
                                _ctx?.Logger?.Log.Info(m);
                                SendAutoModeStateToClients(m);
                                previousMessage = m;
                            }

                            allTasksStopped = noOfWaitingTasks == 0;
                            if (allTasksStopped) break;

                            System.Threading.Thread.Sleep(10);
                        }

                        if (allTasksStopped) break;

                        System.Threading.Thread.Sleep(100);
                    }

                    _autoModeTasks.Clear();
                }

                CleanOccAfterStop();

                _isStopping = false;
            });
        }

        private void CleanOccAfterStop()
        {
            //
            // check periodically if the tasks are really stopped
            //
            const int maxCheckSteps = 10;
            const int checkStep = (2 * RunDelayBetweenChecksMsecs) / maxCheckSteps;
            for (var i = 0; i < maxCheckSteps; i += checkStep)
            {
                if (_isStopped) break;
                System.Threading.Thread.Sleep(checkStep);
            }

            var locOids = new List<int>();
            foreach (var itOcc in _metadata?.Occ.Blocks ?? new List<OccBlock>())
            {
                if (itOcc == null) continue;
                locOids.Add(itOcc.Oid);
            }
            locOids.ForEach(ResetRouteFor);
            CleanOcc();
        }

        private void Instance_Finished(AutoModeTaskCore sender)
        {
            lock (_autoModeTasksLock)
            {
                _autoModeTasks.Remove(sender);
            }
        }

        public void ResetRouteFor(int locOid)
        {
            if (locOid <= 0) return;
            var routeFinalName = string.Empty;
            var routeNextName = string.Empty;
            var blocks = _metadata.Occ.Blocks;
            foreach (var itOccBlock in blocks)
            {
                if (itOccBlock.Oid != locOid) continue;
                routeFinalName = itOccBlock.RouteToFinal;
                routeNextName = itOccBlock.RouteToNext;
                CleanOccBlock(itOccBlock);
                break;
            }

            var routeFinal = _routeList.GetByName(routeFinalName);
            if (routeFinal != null)
                routeFinal.Occupied = false;

            var routeNext = _routeList.GetByName(routeNextName);
            if (routeNext != null)
                routeNext.Occupied = false;

            SaveOccAndPromote();
            SaveRoutesAndPromote();

            var ar = new JArray();
            if (!string.IsNullOrEmpty(routeFinalName)) ar.Add(routeFinalName);
            if (!string.IsNullOrEmpty(routeNextName)) ar.Add(routeNextName);
            if (ar.Count > 0)
            {
                _ctx?.SendCommandToClients(new JObject
                {
                    ["command"] = "autoMode",
                    ["data"] = new JObject
                    {
                        ["command"] = "routeReset",
                        ["routeNames"] = ar
                    }
                });
            }
        }

        public void SendAutoModeStateToClients(string additionalMessage = null)
        {
            _ctx?.SendCommandToClients(new JObject
            {
                ["command"] = "autoMode",
                ["data"] = new JObject
                {
                    ["command"] = "state",
                    ["state"] = new JObject {
                        ["started"] = IsStarted(),
                        ["stopping"] = IsStopping(),
                        ["stopped"] = IsStopped(),
                        ["message"] = additionalMessage ?? string.Empty
                    }
                }
            });
        }

        public void SendRouteToClients()
        {
            var routeNames = new JArray();
            lock (_autoModeTasksLock)
            {
                foreach (var it in _autoModeTasks)
                {
                    try
                    {
                        if (it == null) continue;
                        routeNames.Add(it.RouteName);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            SendAutoModeStateToClients();

            if (routeNames.Count > 0)
            {
                _ctx?.SendCommandToClients(new JObject
                {
                    ["command"] = "autoMode",
                    ["data"] = new JObject
                    {
                        ["command"] = "routeShow",
                        ["routeNames"] = routeNames
                    }
                });
            }
        }

        private NextRouteInformation CheckForRoutesAndAssign()
        {
            lock (_metadataLock)
            {
                foreach (var itOccBlock in _metadata.Occ.Blocks)
                {
                    // has already a destination
                    if (!string.IsNullOrEmpty(itOccBlock.FinalBlock)) continue;

                    //
                    // the most interesting call is `GetNextRoute(..)` which
                    // is responsible for selecting the best next journey path
                    //
                    var nextRoute = GetNextRoute(itOccBlock, out var locomotiveObjectId);
                    if (nextRoute == null) continue;

                    //
                    // query feedback sensors for the route
                    // in case no feedback sensors are set
                    // the route is invalid and will not be used
                    //
                    var resFb = GetFeedbacksForBlock(nextRoute.Blocks[1], out var fbEnter, out var fbIin);
                    if (!resFb)
                    {
                        LogInfo($"Route {nextRoute.Name} has no feedback sensors and will be ignored");
                        continue;
                    }

                    nextRoute.Occupied = true;

                    var fromBlock = nextRoute.Blocks[0];
                    var targetBlock = nextRoute.Blocks[1];

                    LogInfo($"Route: {nextRoute.Name} ({fromBlock.identifier} going to {targetBlock.identifier})");

                    itOccBlock.FinalBlock = targetBlock.identifier;
                    itOccBlock.RouteToFinal = nextRoute.Name;

                    SaveOccAndPromote();
                    SaveRoutesAndPromote();

                    // 
                    // change switch states for the route
                    //
                    _ctx?.ApplyRouteCommandForSwitches(nextRoute.Switches);
                    if (_ctx != null && _ctx.IsSimulationMode())
                    {
                        _ctx.SaveAll();
                        _ctx?._sniffer?.TriggerDataProviderModifiedForSimulation();
                    }
                    else
                    {
                        _ctx?._sniffer?.SendCommandsToEcosStation();
                    }

                    //
                    // inform all client about a new taken route
                    //
                    _ctx?.SendCommandToClients(new JObject
                    {
                        ["command"] = "autoMode",
                        ["data"] = new JObject
                        {
                            ["command"] = "routeShow",
                            ["routeNames"] = new JArray
                            {
                                nextRoute.Name
                            }
                        }
                    });

                    // prepare route information
                    var locDataEcos = _dataProvider.GetObjectBy(locomotiveObjectId) as Locomotive;
                    var locData = _metadata.LocomotivesData.GetData(locomotiveObjectId);
                    var planField = GetPlanField(_metadata);

                    return new NextRouteInformation
                    {
                        Route = nextRoute,
                        FbEnter = planField?.Get(fbEnter),
                        FbIn = planField?.Get(fbIin),
                        LocomotiveObjectId = locomotiveObjectId,
                        Locomotive = locDataEcos,
                        LocomotivesData = locData,
                        DataProvider = _dataProvider,
                        DataProviderS88 = _dataProviderS88,
                        OccBlock = itOccBlock,
                        FromBlock = fromBlock,
                        TargetBlock = targetBlock
                    };
                }
            }

            return null;
        }

        private Feedbacks.Data GetFeedbackDataOf(string blockName, SideMarker side)
        {
            if (string.IsNullOrEmpty(blockName)) return null;
            if (side == SideMarker.None) return null;

            Feedbacks.FeedbacksData fbs;
            lock (_metadataLock)
                fbs = _metadata.FeedbacksData;
            if (fbs == null) return null;

            foreach (var itFb in fbs.Entries)
            {
                if (string.IsNullOrEmpty(itFb?.BlockId)) continue;
                if (!itFb.BlockId.StartsWith(blockName, StringComparison.OrdinalIgnoreCase)) continue;

                if (side == SideMarker.Plus)
                {
                    if (!itFb.BlockId.EndsWith("[+]", StringComparison.Ordinal))
                        continue;
                }
                else if (side == SideMarker.Minus)
                {
                    if (!itFb.BlockId.EndsWith("[-]", StringComparison.Ordinal))
                        continue;
                }

                return itFb;
            }

            return null;
        }

        internal IReadOnlyList<Feedbacks.Data> GetFeedbacksDataForBlock(string blockName)
        {
            var blocks = new List<Feedbacks.Data>();

            if (string.IsNullOrEmpty(blockName)) return blocks;
            Feedbacks.FeedbacksData fbs;
            lock (_metadataLock)
                fbs = _metadata.FeedbacksData;
            if (fbs == null) return blocks;

            foreach (var itFb in fbs.Entries)
            {
                if (string.IsNullOrEmpty(itFb?.BlockId)) continue;
                if (!itFb.BlockId.StartsWith(blockName, StringComparison.OrdinalIgnoreCase)) continue;
                blocks.Add(itFb);
            }

            return blocks;
        }

        private bool GetFeedbacksForBlock(RouteBlock block, out string fbEnter, out string fbIn)
        {
            fbEnter = string.Empty;
            fbIn = string.Empty;

            if (block == null) return false;
            if (_metadataLock == null) return false;

            Feedbacks.FeedbacksData fbs;
            lock (_metadataLock)
                fbs = _metadata.FeedbacksData;
            if (fbs == null) return false;

            foreach (var itFb in fbs.Entries)
            {
                if (string.IsNullOrEmpty(itFb?.BlockId)) continue;
                if (!itFb.BlockId.StartsWith(block.identifier, StringComparison.OrdinalIgnoreCase)) continue;

                if (block.side == SideMarker.Plus)
                {
                    if (!itFb.BlockId.EndsWith("[+]", StringComparison.Ordinal))
                        continue;
                }
                else if (block.side == SideMarker.Minus)
                {
                    if (!itFb.BlockId.EndsWith("[-]", StringComparison.Ordinal))
                        continue;
                }

                fbEnter = itFb.FbEnter;
                fbIn = itFb.FbIn;

                if (!string.IsNullOrEmpty(fbEnter)
                    && !string.IsNullOrEmpty(fbIn))
                    return true;

                fbEnter = string.Empty;
                fbIn = string.Empty;
            }

            return false;
        }

        internal void SaveFeedbacksAndPromote(bool promote = true)
        {
            if (_metadata == null || _metadataLock == null) return;
            lock (_metadataLock)
            {
                _metadata.Save(Metadata.SaveModelType.FeedbacksData);
            }
            if (promote)
                _ctx?.SendModelToClients(ClientHandler.ClientHandler.ModelType.UpdateFeedbacks);
        }

        internal void SaveOccAndPromote(bool promote = true)
        {
            if (_metadata == null || _metadataLock == null) return;
            lock (_metadataLock)
            {
                _metadata.Save(Metadata.SaveModelType.OccData);
            }
            if (promote)
                _ctx?.SendModelToClients(ClientHandler.ClientHandler.ModelType.UpdateOcc);
        }

        internal void SaveRoutesAndPromote(bool promote = true)
        {
            if (_metadata == null || _metadataLock == null) return;
            lock (_metadataLock)
            {
                _metadata.SetRoutes(_routeList);
                _metadata?.Save(Metadata.SaveModelType.RouteData);
            }
            if (promote)
                _ctx?.SendModelToClients(ClientHandler.ClientHandler.ModelType.UpdateRoutes);
        }

        internal void SaveLocomotivesAndPromote(bool promote = true)
        {
            if (_metadata == null || _metadataLock == null) return;
            lock (_metadataLock)
            {
                _metadata?.Save(Metadata.SaveModelType.LocomotivesData);
            }
            if (promote)
                _ctx?.SendModelToClients(ClientHandler.ClientHandler.ModelType.UpdateLocomotivesData);
        }

        private void Initialize()
        {
            if (_metadata != null) return;

            _metadata = _ctx._metadata;
            _metadataLock = _ctx._metadata;
            _dataProvider = _ctx._sniffer.GetDataProvider() as DataProvider;
            _dataProviderS88 = _ctx._sniffer.GetDataProviderS88() as DataProvider;

            var nativeRouteData = _metadata.Routes.ToString();
            _routeList = JsonConvert.DeserializeObject<RouteList>(nativeRouteData);
            _planfield = GetPlanField(_metadata);
        }

        public void ApplyRouteDisableState(string routeName, bool disableState)
        {
            if (string.IsNullOrEmpty(routeName)) return;

            foreach (var itRoute in _routeList)
            {
                if (itRoute == null) continue;
                if (routeName.Equals(itRoute.Name, StringComparison.OrdinalIgnoreCase))
                {
                    itRoute.IsDisabled = disableState;
                    return;
                }
            }
        }

        /// <summary>
        /// Resets all OCC states.
        /// Frees the occupied state for all routes.
        /// </summary>
        public void CleanOcc()
        {
            var occ = _metadata?.Occ;
            if (occ != null)
            {
                for (var i = 0; i < occ.Blocks.Count; ++i)
                    occ.Blocks[i] = CleanOccBlock(occ.Blocks[i]);

                _metadata.Occ = occ;

                SaveOccAndPromote();
            }

            if (_routeList != null)
            {
                foreach (var itRoute in _routeList)
                {
                    if (itRoute == null) continue;
                    itRoute.Occupied = false;
                }

                SaveRoutesAndPromote();
            }
        }

        /// <summary>
        /// Resets the block of a occ block to empty.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        internal static OccBlock CleanOccBlock(OccBlock block)
        {
            block.FinalBlock = string.Empty;
            block.NextBlock = string.Empty;
            block.RouteToFinal = string.Empty;
            block.RouteToNext = string.Empty;
            block.NextEntered = false;
            block.FinalEntered = false;
            return block;
        }

        private bool IsLocAllowedForTargetBlock(
            Locomotives.Data locData,
            Feedbacks.Data fbData
            )
        {
            if (locData == null) return false;
            if (fbData == null) return false;

            var locOption = locData.Settings.Where(x => x.Value && x.Key.StartsWith("Type", StringComparison.OrdinalIgnoreCase)).ToList();
            var fbOption = fbData.Settings.Where(x => x.Value && x.Key.StartsWith("Type", StringComparison.OrdinalIgnoreCase)).ToList();

            if (locOption.Count == 0) return false;
            if (fbOption.Count == 0) return false;

            foreach (var it in locOption)
            {
                foreach (var itt in fbOption)
                {
                    if (it.Key.Equals(itt.Key, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        public Route.Route GetNextRoute(
            OccBlock occBlock,
            out int locomotiveObjectId)
        {
            locomotiveObjectId = 0;
            var occFromBlock = occBlock.FromBlock;
            if (string.IsNullOrEmpty(occFromBlock)) return null;

            var occLocOid = occBlock.Oid;
            var locDataEcos = _dataProvider.GetObjectBy(occLocOid) as Locomotive;
            var locData = _metadata.LocomotivesData.GetData(occLocOid);

            if (locDataEcos == null) return null;
            if (locData == null) return null;

            //
            // NOTE check if the OCC has waited long enough for a new start
            // 
            var lastReachedTime = occBlock.ReachedTime;
            var allowedMinimumTime = lastReachedTime.AddSeconds(occBlock.SecondsToWait);
            if (allowedMinimumTime > DateTime.Now)
                return null;

            //
            // do not start any loc on any route when the loc is locked (i.e. not allowed to start)
            //
            if (locData.IsLocked) return null;

            //
            // do not start any loc on any route when the loc is "IsStopped:=true"
            //
            // REMARK we habe to distinguish IsLocked and IsStopped somehow
            //
            if (locData.IsStopped) return null;

            var sideToLeave = locData.EnterBlockSide.IndexOf("+", StringComparison.Ordinal) != -1
                ? SideMarker.Minus
                : SideMarker.Plus;

            var originalSideEntered = sideToLeave == SideMarker.Minus
                ? SideMarker.Plus
                : SideMarker.Minus;

            var routesFrom2 = _routeList.GetRoutesWithFromBlock(occFromBlock, sideToLeave, true);

            //
            // filter routes by allowed options, e.g. "mainline", "intercity", ...
            // 
            var routesFrom3 = new RouteList();
            foreach (var it in routesFrom2)
            {
                var targetBlock = it.Blocks[1];
                var targetBlockIdentifier = targetBlock.identifier;
                if (string.IsNullOrEmpty(targetBlockIdentifier)) continue;

                var targetFbData = GetFeedbackDataOf(targetBlockIdentifier, sideToLeave);
                if (targetFbData == null) continue;

                //
                // when the target block is disabled, do not use for routing
                //
                if (targetFbData.Settings != null && targetFbData.Settings.ContainsKey("BlockEnabled"))
                {
                    var blockEnabled = targetFbData.Settings["BlockEnabled"];
                    if (!blockEnabled) continue;
                }

                if (IsLocAllowedForTargetBlock(locData, targetFbData))
                    routesFrom3.Add(it);
            }

            //
            // check if routes have target blocks which are locked by other blocks
            // if fromBlock is referenced, the target is allowed
            //
            var routesFrom = new RouteList();
            foreach (var it in routesFrom3)
            {
                var targetBlock = it.Blocks[1];
                var targetBlockIdentifier = targetBlock.identifier;
                if (string.IsNullOrEmpty(targetBlockIdentifier)) continue;

                var targetFbData = GetFeedbackDataOf(targetBlockIdentifier, sideToLeave);
                if (targetFbData == null) continue;

                var lockedBy = targetFbData.LockedByBlock;
                if (string.IsNullOrEmpty(lockedBy))
                {
                    routesFrom.Add(it);
                }
                else
                {
                    var fromBlock = it.Blocks[0];
                    var fromBlockIdentifier = fromBlock.identifier;

                    if (lockedBy.StartsWith(fromBlockIdentifier, StringComparison.OrdinalIgnoreCase))
                        routesFrom.Add(it);
                }
            }

            //
            // in case there is no route to leave on the sideToLeave
            // probably the trains' direction must change, if change
            // is allowed:
            // (1) check for a new route on the opposide sideToLeave
            // (2) if one or more route available, check of the train is allowed to change the direction (as well the block)
            // (3) change the direction
            // (4) change the sideToLeave
            // (5) ...start the additional route selection routines
            //
            if (routesFrom.Count == 0)
            {
                string step4enterBlockSide;

                LogInfo($"The side to leave {sideToLeave} does not have any route to take.");
                if (sideToLeave == SideMarker.Minus)
                {
                    step4enterBlockSide = "'-' Side";
                    sideToLeave = SideMarker.Plus;
                }
                else
                {
                    step4enterBlockSide = "'+' Side";
                    sideToLeave = SideMarker.Minus;
                }

                #region (1)
                //
                // (1)
                //
                routesFrom = _routeList.GetRoutesWithFromBlock(occFromBlock, sideToLeave, true);
                if (routesFrom.Count == 0)
                {
                    LogInfo($"The other side to leave {sideToLeave} does not have any route to take.");
                    LogInfo($"No route to take from {occFromBlock} for Locomotive({locDataEcos.Name ?? "-"}).");
                    return null;
                }

                #endregion (1)

                #region (2)
                //
                // (2)
                //
                if (locData.Settings.ContainsKey("OptionDirection"))
                {
                    var locState = locData.Settings["OptionDirection"];
                    if (!locState)
                    {
                        LogInfo($"Locomotive({locDataEcos.Name}) is not allowed to change the direction.");
                        return null;
                    }
                }

                var fbData = GetFeedbackDataOf(occBlock.FromBlock, originalSideEntered);
                if (fbData == null)
                {
                    LogInfo($"No feedback data available for block {occBlock.FromBlock}.");
                    return null;
                }

                if (fbData.Settings.ContainsKey("OptionDirection"))
                {
                    var blockState = fbData.Settings["OptionDirection"];
                    if (!blockState)
                    {
                        LogInfo($"Block({fbData.BlockId}) does not allow to change the direction.");
                        return null;
                    }
                }

                #endregion (2)

                #region (3)
                //
                // (3)
                //
                var currentDirection = locDataEcos.Direction;
                var newDirection = currentDirection == 1 ? 0 : 1;
                if (_ctx.IsSimulationMode())
                {
                    locDataEcos.ChangeDirectionSimulation(newDirection == 1);
                    _ctx.SaveAll();
                    _ctx?._sniffer?.TriggerDataProviderModifiedForSimulation();
                }
                else
                {
                    locDataEcos.ChangeDirection(newDirection == 1);
                    _ctx?._sniffer?.SendCommandsToEcosStation();
                }

                #endregion (3)

                #region (4)
                //
                // (4)
                //
                // EnterBlockSide = "'+' Side"
                // EnterBlockSide = "'-' Side"
                if (string.IsNullOrEmpty(step4enterBlockSide))
                {
                    LogInfo($"Invalid enterBlockSide value for Locomotive({locDataEcos.Name}).");
                    return null;
                }

                locData.EnterBlockSide = step4enterBlockSide;
                SaveLocomotivesAndPromote();
                SaveOccAndPromote();

                #endregion (4)
            }
            var routesFromFiltered = routesFrom.FilterBy(locDataEcos, locData, _metadata.FeedbacksData);
            var routesFromNotOccupied = routesFromFiltered.FilterNotOccupiedOrLocked(_metadata.Occ);
            if (routesFromNotOccupied.Count == 0) return null;

            //
            // filter routes if any accessory is in "maintenance" mode
            //
            var routesNoMaintenance = routesFromNotOccupied.FilterSwitchesMaintenance(_metadata.Metamodel);

            //
            // filter all routes which cross occupied routes
            //
            var routesNoCross = routesNoMaintenance.FilterNoCrossingOccupied(_routeList);
            if (routesNoCross.Count == 0)
            {
                //
                // no route free to take
                //
                return null;
            }

            locomotiveObjectId = occLocOid;

            var idx = GetRndBetween(routesNoCross.Count);
            return routesNoCross[idx];
        }

        #region Helper

        internal void LogInfo(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _ctx?.Logger?.Log?.Info($"{msg}");
            _ctx?.SendDebugMessage($"{msg}");
        }

        private static int GetRndBetween(int max = int.MaxValue, int from = 0)
        {
            var r = new Random(Guid.NewGuid().GetHashCode());
            return r.Next(from, max);
        }

        private static PlanField GetPlanField(Metadata metadata)
        {
            var metamodel = metadata?.Metamodel;
            if (metamodel == null) return null;
            var planfield = JsonConvert.DeserializeObject<Dictionary<string, PlanField>>(metamodel.ToString(Formatting.None));
            return planfield["planField"];
        }

        #endregion
    }
}
