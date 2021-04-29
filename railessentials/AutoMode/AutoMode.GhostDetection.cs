// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoMode.GhostDetection.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Occ;
using railessentials.Plan;

namespace railessentials.AutoMode
{
    public partial class AutoMode
    {
        private static int _walltimeSecondsStopGhost = 30;
        private BackgroundWorker _bwGhost = null;

        public bool StartGhostDetection()
        {
            if (_bwGhost is {IsBusy: true})
            {
                LogInfo($"Ghost detection already running.");
                return true;
            }
            
            if(_bwGhost != null)
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
            while(_bwGhost is {CancellationPending: false})
            {
                //
                // STEPS FOR SUCCESS
                // (1) query current used routes
                // (2) query all s88 feedback items which are allowed in the routes
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
                var allowedFbs = _getAllowedFbsOfRoutes(usedRoutes);

                System.Threading.Thread.Sleep(250);
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

        private List<PlanItem> _getAllowedFbsOfRoutes(List<string> routes)
        {
            var res = new List<PlanItem>();

            JObject planField = null;
            lock (_metadataLock)
                planField = _metadata?.Metamodel?["planField"] as JObject;
            if (planField == null)
                return res;

            var field = JsonConvert.DeserializeObject<PlanField>(planField.ToString());
            
            foreach (var itRoute in routes)
            {
                if (string.IsNullOrEmpty(itRoute)) continue;

                var routeData = _routeList.GetByName(itRoute);
                if (routeData == null) continue;
                
                foreach (var itSensor in routeData.Sensors)
                {
                    if(itSensor == null) continue;
                    var itemSensor = field?.Get(itSensor.x, itSensor.y);
                    if(itemSensor != null)
                        res.Add(itemSensor);
                }

                var startBlock = routeData.Blocks[0];
                if (startBlock != null)
                {
                    if(GetFeedbacksForBlock(startBlock, out var fbEnter, out var fbIn))
                    {
                        var fbEnterItem = field?.Get(fbEnter);
                        if(fbEnterItem != null)
                            res.Add(fbEnterItem); // duplicates allowed

                        var fbInItem = field?.Get(fbIn);
                        if(fbInItem != null)
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

        public async Task<bool> StopGhostDetection()
        {
            LogInfo($"Stop Ghost detection...");
            if (_bwGhost is not {IsBusy: true}) return true;
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
