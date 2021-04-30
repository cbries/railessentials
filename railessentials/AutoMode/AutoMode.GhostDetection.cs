// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoMode.GhostDetection.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Analyzer;
using railessentials.Plan;
using railessentials.Route;

namespace railessentials.AutoMode
{
    public partial class AutoMode
    {
        private static int _walltimeSecondsStopGhost = 30;
        private BackgroundWorker _bwGhost = null;

        public bool StartGhostDetection()
        {
            if (!_ctx._cfg.Cfg.GhostDetection)
            {
                LogInfo("Ghost detection is disabled.");
                return true;
            }

            if (_bwGhost is { IsBusy: true })
            {
                LogInfo($"Ghost detection already running.");
                return true;
            }

            if (_bwGhost != null)
            {
                try
                {
                    _bwGhost.Dispose();
                    _bwGhost = null;
                }
                catch
                {
                    // ignore
                }
            }

            _bwGhost = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = false
            };

            _bwGhost.DoWork += BwGhostOnDoWork;
            _bwGhost.RunWorkerCompleted += BwGhostOnRunWorkerCompleted;

            _bwGhost.RunWorkerAsync();

            LogInfo($"Ghost detection started.");

            return _bwGhost.IsBusy;

        }

        private void BwGhostOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LogInfo("Ghost detection finished.");
        }

        private void BwGhostOnDoWork(object sender, DoWorkEventArgs e)
        {
            const int HighRateCheckMs = 250;
            const int LowRateCheckMs = 1000;
            var checkDelayMs = HighRateCheckMs;
            var faultyFbsSent = false;

            while (_bwGhost is { CancellationPending: false })
            {
                //
                // STEPS FOR SUCCESS
                // (1) query current used routes
                // (2) query all s88 feedback items which are allowed in the routes and blocks
                // (2.1) query all s88 feedback items which are allowed for occupied blocks
                // (3) get all activated s88 feedbacks
                // (4) query all activated s88 feedbacks of (3) which are not in the list of (2)
                // (5) if any activated feedback exist in a result of (4), send the list 
                //     of the activated feedbacks to all clients as {error}
                // (6) stop all trains
                // (7) highlight the relevant feedback item
                // NOTE do not stop AutoMode, just set the speed of the locomotive to zero
                //

                // (1)
                var usedRoutes = _getCurrentUsedRoutes();

                // (2)
                var allowedFbs1 = _getAllowedFbsOfRoutes(usedRoutes);
                // (2.1)
                var allowedFbs2 = _getAllowedFbsOfOccupiedBlocks();
                var allowedFbs = new List<PlanItem>();
                allowedFbs.AddRange(allowedFbs1);
                allowedFbs.AddRange(allowedFbs2);

                // (3)
                var activeFbs = _getAllActivatedFbs();

                // (4)
                var faultyFbs = _getFaultyFbs(allowedFbs, activeFbs);
#if DEBUG
                Trace.WriteLine("Faulty FBs: " + faultyFbs.Count);
#endif
                // (5)
                if (faultyFbs.Count == 0)
                {
                    checkDelayMs = HighRateCheckMs;

                    if (faultyFbsSent)
                    {
                        // if we had a faulty state in the past
                        // reset the state and inform the clients

                        SendAutoModeGhostResetToClients();

                        faultyFbsSent = false;
                    }
                }
                else
                {
                    //
                    // there are faulty FBs (i.e. a ghost train exists)
                    //

                    SendAutoModeGhostFoundToClients(faultyFbs);

                    faultyFbsSent = true;
                    checkDelayMs = LowRateCheckMs;
                }

                // (6)
                if (faultyFbs.Count > 0)
                {
                    if (_ctx != null)
                    {
                        if (_ctx.HasAnyTrainSpeed())
                        {
                            LogInfo($"Ghost train detected, stop all locomotives.");
                            _ctx.StopAllLocomotives();
                        }
                    }
                }

                // (7)
                // applied on client-side

                System.Threading.Thread.Sleep(checkDelayMs);
            }
        }

        private List<string> _getCurrentUsedRoutes()
        {
            var res = new List<string>();
            lock (_metadataLock)
            {
                var blocks = _metadata.Occ.Blocks;
                foreach (var itOccBlock in blocks)
                {
                    if (itOccBlock == null) continue;
                    if (!itOccBlock.IsTraveling) continue;
                    if (string.IsNullOrEmpty(itOccBlock.FinalBlock)) continue;

                    res.Add(itOccBlock.RouteToFinal);
                }
            }
            return res;
        }

        private List<PlanItem> _getAllowedFbsOfRoutes(IEnumerable<string> routes)
        {
            var res = new List<PlanItem>();

            var field = _getPlanField();

            foreach (var itRoute in routes)
            {
                if (string.IsNullOrEmpty(itRoute)) continue;

                var routeData = _routeList.GetByName(itRoute);
                if (routeData == null) continue;

                foreach (var itSensor in routeData.Sensors)
                {
                    if (itSensor == null) continue;
                    var itemSensor = field?.Get(itSensor.x, itSensor.y);
                    if (itemSensor != null)
                        res.Add(itemSensor);
                }

                var startBlock = routeData.Blocks[0];
                if (startBlock != null)
                {
                    if (GetFeedbacksForBlock(startBlock, out var fbEnter, out var fbIn))
                    {
                        var fbEnterItem = field?.Get(fbEnter);
                        if (fbEnterItem != null)
                            res.Add(fbEnterItem); // duplicates allowed

                        var fbInItem = field?.Get(fbIn);
                        if (fbInItem != null)
                            res.Add(fbInItem); // duplicates allowed
                    }
                }

                var finalBlock = routeData.Blocks[1];
                if (finalBlock != null)
                {
                    if (GetFeedbacksForBlock(finalBlock, out var fbEnter, out var fbIn))
                    {
                        var fbEnterItem = field?.Get(fbEnter);
                        if (fbEnterItem != null)
                            res.Add(fbEnterItem); // duplicates allowed

                        var fbInItem = field?.Get(fbIn);
                        if (fbInItem != null)
                            res.Add(fbInItem); // duplicates allowed
                    }
                }
            }

            return res;
        }

        private List<PlanItem> _getAllowedFbsOfOccupiedBlocks()
        {
            var res = new List<PlanItem>();
            var startBlocks = new List<string>();
            lock (_metadataLock)
            {
                var blocks = _metadata.Occ.Blocks;
                foreach (var itOccBlock in blocks)
                {
                    if (itOccBlock == null) continue;
                    if (itOccBlock.Oid <= 0) continue;
                    if (string.IsNullOrEmpty(itOccBlock.FromBlock)) continue;
                    startBlocks.Add(itOccBlock.FromBlock);
                }
            }

            var field = _getPlanField();

            foreach (var itBlock in startBlocks)
            {
                if (string.IsNullOrEmpty(itBlock)) continue;

                //
                // query fbEnter and fbIn
                //
                var r = GetFeedbacksForBlock(new RouteBlock
                {
                    identifier = itBlock,
                    side = SideMarker.Plus
                },
                    out var fbEnter,
                    out var fbIn);
                if (r)
                {
                    var fbEnterItem = field.Get(fbEnter);
                    res.Add(fbEnterItem);

                    var fbInItem = field.Get(fbIn);
                    res.Add(fbInItem);
                }

                //
                // query block's direct neighbour, if they are sensors/feedback then
                // our heuristic guess they are part of the allowed fbs when the block is occupied
                //
                var blockItem = field.Get(itBlock);
                if (blockItem != null)
                {
                    blockItem.Ctx = field;

                    var ways = blockItem.GetAllPossibleWays();
                    // a block can only have two feasible ways
                    
                    var from = ways[0][0];
                    var fromP = blockItem.GetSideXy(from, 0);
                    if (from == 'a') fromP.x--;
                    else if (from == 'b') fromP.y--;
                    var fromItem = blockItem.GetBorderItem(from, fromP);
                    if(fromItem is {IsSensor: true})
                        res.Add(fromItem);

                    var to = ways[1][0];
                    var toP = blockItem.GetSideXy(to, 0);
                    //if (to == 'c') toP.x++;
                    //else if (to == 'd') toP.y++;
                    var toItem = blockItem.GetBorderItem(to, toP);
                    if(toItem is {IsSensor: true})
                        res.Add(toItem);
                }
            }

            return res;
        }

        internal class FbActive
        {
            public int Addr { get; set; }
            public ecoslib.Entities.S88 Fb { get; set; }
        }

        private List<PlanItem> _getAllActivatedFbs()
        {
            var res = new List<PlanItem>();
            var feedbacks = _dataProviderS88.GetPorts();
            if (feedbacks == null) return res;
            var enabledFbs = new List<FbActive>();
            var offsetStart = 0;
            foreach (var fb in feedbacks)
            {
                if (fb == null) continue;

                for (var i = 0; i < fb.Ports; ++i)
                {
                    var state = fb.Pin((uint)i);
                    if (state)
                    {
                        var fbAddr = offsetStart + i + 1;
                        enabledFbs.Add(new FbActive
                        {
                            Addr = fbAddr,
                            Fb = fb
                        });
                    }
                }

                offsetStart += fb.Ports;
            }

            if (enabledFbs.Count == 0) return res;

            var field = _getPlanField();

            foreach (var fb in enabledFbs)
            {
                var sensor = field.GetSensorByAddr(fb.Addr);
                if (sensor == null) continue;
                res.Add(sensor);
            }

            return res;
        }

        private List<PlanItem> _getFaultyFbs(
            List<PlanItem> allowedFbs,
            List<PlanItem> activeFbs)
        {
            var faultyFbs = new List<PlanItem>();
            foreach (var itFb in activeFbs)
            {
                var id = itFb?.identifier;
                if (string.IsNullOrEmpty(id)) continue;
                var found = false;
                foreach (var itAllowed in allowedFbs)
                {
                    var itName = itAllowed?.identifier;
                    if (string.IsNullOrEmpty(itName)) continue;
                    if (itName.Equals(id))
                        found = true;
                }
                if (!found)
                    faultyFbs.Add(itFb);
            }
            return faultyFbs;
        }

        private PlanField _getPlanField()
        {
            JObject planField;
            lock (_metadataLock)
                planField = _metadata?.Metamodel?["planField"] as JObject;
            if (planField == null)
                return null;

            return JsonConvert.DeserializeObject<PlanField>(planField.ToString());
        }

        public async Task<bool> StopGhostDetection()
        {
            if (!_ctx._cfg.Cfg.GhostDetection)
            {
                LogInfo("Ghost detection was disabled.");
                return true;
            }

            LogInfo($"Stop Ghost detection...");
            if (_bwGhost is not { IsBusy: true }) return true;
            _bwGhost.CancelAsync();
            return await Task.Run(() =>
            {
                var walltime = DateTime.Now + TimeSpan.FromSeconds(_walltimeSecondsStopGhost);
                while (_bwGhost.IsBusy)
                {
                    if (DateTime.Now > walltime)
                    {
                        LogInfo($"Stop of Ghost detection failed.");
                        return false;
                    }

                    System.Threading.Thread.Sleep(125);
                }
                _bwGhost.Dispose();
                _bwGhost = null;
                LogInfo($"Ghost detection stopped.");
                return true;
            });
        }
    }
}
