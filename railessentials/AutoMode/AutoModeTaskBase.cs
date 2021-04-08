// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoModeTaskBase.cs

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ecoslib.Entities;
using Newtonsoft.Json;
using railessentials.Analyzer;
using railessentials.Feedbacks;
using railessentials.Plan;
using railessentials.Route;

namespace railessentials.AutoMode
{
    public partial class AutoModeTaskBase : AutoModeTaskCore
    {
        private const int TimeBetweenFbTestMsecs = 500;
        private const string MaxSpeedLevelAutoMode = "speedLevel3";

        private bool IsCanceled()
        {
            if (!CancelSource.IsCancellationRequested) return false;

            // frees the current route and resets the relevant block states
            Ctx?.ResetRouteFor(Route.LocomotiveObjectId);

            SendDebugMessage($"{Route.Route.Name} has been canceled");

            IsFinished = true;

            return true;
        }

        private void SwitchAccessory(string targetState, Accessory ecosAcc, bool inverse)
        {
            if (string.IsNullOrEmpty(targetState)) return;
            if (ecosAcc == null) return;

            var clientHandler = Ctx?.GetClientHandler();
            if (clientHandler == null) return;

            Ctx?.LogInfo($"Switch accessory {ecosAcc.Caption} to {targetState}.");

            var realTargetState = targetState;
            if (inverse)
            {
                if (realTargetState.Equals("red", StringComparison.OrdinalIgnoreCase))
                    realTargetState = "green";
                else
                    realTargetState = "red";
            }

            if (clientHandler.IsSimulationMode())
            {
                if (realTargetState.Equals("red", StringComparison.OrdinalIgnoreCase))
                    ecosAcc.SwitchSimulation(1);
                else if (realTargetState.Equals("green", StringComparison.OrdinalIgnoreCase))
                    ecosAcc.SwitchSimulation(0);
            }
            else
            {
                if (realTargetState.Equals("red", StringComparison.OrdinalIgnoreCase))
                    ecosAcc.Switch(1);
                else if (realTargetState.Equals("green", StringComparison.OrdinalIgnoreCase))
                    ecosAcc.Switch(0);
            }
        }

        private void StartDelayTask(OnStart it, Accessory ecosAcc, bool inverse)
        {
            if (ecosAcc == null) return;
            var afterDelay = it.StateAfterDelay;
            if (afterDelay == null) return;
            var sec = afterDelay.Seconds;
            var state = afterDelay.State;
            if (sec <= 0) return;
            if (string.IsNullOrEmpty(state)) return;
            Task.Run(async () =>
            {
                Ctx?.LogInfo($"Wait {sec} seconds to switch accessory {ecosAcc.Caption} to {state}.");
                await Task.Delay(TimeSpan.FromSeconds(sec));
                SwitchAccessory(state, ecosAcc, inverse);
                UpdateEcosUnit();
            });
        }

        private void UpdateEcosUnit()
        {
            var clientHandler = Ctx?.GetClientHandler();
            if (clientHandler == null) return;

            if (clientHandler.IsSimulationMode())
            {
                clientHandler.SaveAll();
                clientHandler._sniffer?.TriggerDataProviderModifiedForSimulation();
            }
            else
            {
                clientHandler._sniffer?.SendCommandsToEcosStation();
            }
        }

        private void ApplyAccessoryStartCommands()
        {
            var fromBlockId = Route.FromBlock.identifier;
            var fromSide = Route.FromBlock.side;
            if (string.IsNullOrEmpty(fromBlockId)) return;
            var fbData = Ctx._metadata.FeedbacksData.GetByBlockId(fromBlockId, fromSide);
            if (fbData == null) return;

            var clientHandler = Ctx?.GetClientHandler();
            if (clientHandler == null) return;

            try
            {
                if (fbData.OnStart == null) return;
                if (fbData.OnStart.Count == 0) return;

                var dp = Route.DataProvider;

                foreach (var it in fbData.OnStart)
                {
                    if (it == null) continue;

                    var accItemObj = Ctx._metadata.GetMetamodelItem(it.Accessory);
                    var accItem = JsonConvert.DeserializeObject<PlanItem>(accItemObj.ToString(Formatting.None));
                    if (accItem == null) continue;
                    Utilities.GetAccessoryEcosAddresses(accItem,
                        out var addr1, out var inverse1,
                        out var addr2, out var inverse2);

                    //
                    // e.g. 2-wings-semaphore can have two addresses
                    //
                    if (addr1 > 0 && addr2 > 0)
                    {
                        var ecosAcc1 = dp.GetAccessoryByAddress(addr1) as Accessory;
                        if (ecosAcc1 != null)
                        {
                            SwitchAccessory(it.State, ecosAcc1, inverse1);
                            StartDelayTask(it, ecosAcc1, inverse1);
                        }

                        var ecosAcc2 = dp.GetAccessoryByAddress(addr2) as Accessory;
                        if (ecosAcc2 != null)
                        {
                            SwitchAccessory(it.State, ecosAcc2, inverse2);
                            StartDelayTask(it, ecosAcc2, inverse2);
                        }
                    }
                    else
                    {   //
                        // in most cases we just have a single address
                        //
                        var addr = 0;
                        var inverse = false;
                        if (addr1 > 0)
                        {
                            addr = addr1;
                            inverse = inverse1;
                        }
                        else if (addr2 > 0)
                        {
                            addr = addr2;
                            inverse = inverse2;
                        }

                        if (addr > 0)
                        {
                            var ecosAcc = dp.GetAccessoryByAddress(addr) as Accessory;
                            if (ecosAcc == null) continue;
                            SwitchAccessory(it.State, ecosAcc, inverse);
                            StartDelayTask(it, ecosAcc, inverse);
                        }
                    }
                }

                UpdateEcosUnit();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"<Exception> {ex.Message}");
            }
        }

        private void SetAccessories()
        {
            ApplyAccessoryStartCommands();
            // TODO add OnStop actions
        }

        public async override Task Run()
        {
            #region prepare data for autoMode task

            var dpS88 = Route.DataProviderS88;

            //var currentDirection = Route.Locomotive.Direction;
            var currentSpeed = Route.Locomotive.Speedstep;
            var maxSpeed = Route.Locomotive.GetNumberOfSpeedsteps();
            var speedCurve = Route.LocomotivesData?.SpeedCurve;

            var targetSpeed = (int)(maxSpeed / 2.0);
            if (speedCurve != null)
                targetSpeed = speedCurve.MaxSpeed;
            else
            {
                if(Route.LocomotivesData != null)
                    targetSpeed = Route.LocomotivesData.GetLevel(MaxSpeedLevelAutoMode).Value;
            }

            #endregion

            await Task.Run(async () =>
            {
                SendDebugMessage($"Start: {Route.Route.Name}");

                //
                // NOTE reset additional locked blocks
                //
                UnlockAdditionalBlocksFor(Route.FromBlock);
                Ctx?.SaveFeedbacksAndPromote();

                //
                // NOTE apply all accessory states before start of locomotive traveling
                //
                SetAccessories();

                if (IsCanceled()) return;

                //
                // NOTE start train, if the fbEnter is reached before the train got its full speed, cancel the speed-up
                //
                var fbEnterAlreadyReached = false;
                if (speedCurve != null)
                {
                    SendDebugMessage($"Using speed curve to accelerate {Route.Locomotive.Caption}.");

                    await AccelerateLocomotiveCurve(currentSpeed, Route.Locomotive, speedCurve, hasToBeCanceled: () =>
                    {
                        fbEnterAlreadyReached = IsFbReached("FbEnter", Route.FbEnter, dpS88, out var hasError);
                        // TODO handle hasError (e.g. cancel route)
                        return fbEnterAlreadyReached;
                    });
                }
                else
                {
                    await AccelerateLocomotive(currentSpeed, targetSpeed, Route.Locomotive, () =>
                    {
                        fbEnterAlreadyReached = IsFbReached("FbEnter", Route.FbEnter, dpS88, out var hasError);
                        // TODO handle hasError (e.g. cancel route)
                        return fbEnterAlreadyReached;
                    });
                }

                if (IsCanceled()) return;

                //
                // NOTE wait for entering the fbEnter feedback
                //
                if (!fbEnterAlreadyReached)
                    await WaitForFb("FbEnter", Route.FbEnter, dpS88);

                if (IsCanceled()) return;

                //
                // NOTE start measurement how long it takes to reach fbIn feedback
                //
                var startDt = DateTime.Now;

                //
                // NOTE trigger Ui update to visualize entering destination
                //
                Route.OccBlock.FinalEntered = true;
                Ctx?.SaveOccAndPromote();

                if (IsCanceled()) return;

                //
                // NOTE decelerate the train
                //
                var fbInAlreadyReached = false;
                if (speedCurve != null)
                {
                    SendDebugMessage($"Using speed curve to decelerate {Route.Locomotive.Caption}.");

                    var durationSeconds = 10.0;

                    if (Ctx?._metadataLock != null)
                    {
                        lock (Ctx._metadataLock)
                        {
                            durationSeconds = Ctx._metadata.LocomotivesDurationData.GetAverageDecelerationSeconds(
                                Route.LocomotiveObjectId,
                                Route.TargetBlock.identifier);
                        }
                    }

                    await DecelerateLocomotiveCurve(Route.Locomotive, speedCurve, maxSeconds: (int)durationSeconds, hasToBeCanceled: () =>
                    {
                        fbInAlreadyReached = IsFbReached("FbIn", Route.FbIn, dpS88, out var hasError);
                        // TODO handle hasError (e.g. cancel route)
                        return fbInAlreadyReached;
                    });
                }
                else
                {
                    await DecelerateLocomotive(Route.Locomotive, hasToBeCanceled: () =>
                    {
                        fbInAlreadyReached = IsFbReached("FbIn", Route.FbIn, dpS88, out var hasError);
                        // TODO handle hasError (e.g. cancel route)
                        return fbInAlreadyReached;
                    });
                }

                if (IsCanceled()) return;

                //
                // NOTE wait for entering the fbIn feedback
                //
                if (!fbInAlreadyReached)
                    await WaitForFb("FbIn", Route.FbIn, dpS88);

                var stopDt = DateTime.Now;
                var delta = stopDt - startDt;
                SendDebugMessage($"{delta.TotalSeconds} seconds between 'enter' and 'in'.");

                if (IsCanceled()) return;

                //
                // save duration between FB-enter and FB-in
                //
                if (Ctx?._metadataLock != null)
                {
                    lock (Ctx._metadataLock)
                    {
                        if (Ctx?._metadata != null)
                        {
                            Ctx?._metadata.LocomotivesDurationData.AddDecelerateDuration(
                                Route.LocomotiveObjectId,
                                Route.TargetBlock.identifier,
                                startDt,
                                stopDt);

                            Ctx?._metadata.Save(Metadata.SaveModelType.LocomotivesDurationsData);
                        }
                    }
                }

                //
                // NOTE stop the locomotive in any case when fbIn is reached
                //
                Ctx?.GetClientHandler()?.LocomotiveChangeSpeedstep(Route.Locomotive, 0);

                //
                // NOTE check if the reached block has any additional blocks to lock
                //
                LockAdditionalBlocksFor(Route.TargetBlock);
                Ctx?.SaveFeedbacksAndPromote();

                //
                // NOTE reset the current OCC information and set the final block as new from block
                // NOTE reset the recent route, it is not occupied anymore
                // !! the order is crucial !!
                //
                var finalBlock = Route.OccBlock.FinalBlock;
                Ctx?.ResetRouteFor(Route.LocomotiveObjectId);
                Route.OccBlock.FromBlock = finalBlock;
                Route.OccBlock.ReachedTime = DateTime.Now;
                Route.OccBlock.SecondsToWait = AutoMode.RunPauseForBlockSeconds;
                Ctx?.SaveOccAndPromote();

                if (IsCanceled()) return;

                //
                // NOTE set entering side for the locomotive
                //
                if (Route.TargetBlock.side == SideMarker.Plus)
                    Route.LocomotivesData.EnterBlockSide = "'+' Side";
                else if (Route.TargetBlock.side == SideMarker.Minus)
                    Route.LocomotivesData.EnterBlockSide = "'-' Side";
                else
                    Route.LocomotivesData.EnterBlockSide = string.Empty;
                Ctx?.SaveLocomotivesAndPromote();

                if (IsCanceled()) return;

                //
                // NOTE leave the task
                //
                SendDebugMessage($"Finished: {Route.Route.Name}");

                IsFinished = true;

                TriggerFinished();
                
            }, CancelSource.Token);
        }

        private void LockAdditionalBlocksFor(RouteBlock block)
        {
            var reachedBlock = Ctx?.GetClientHandler()?._metadata?.FeedbacksData.GetByBlockId(block.Caption);
            if (reachedBlock == null) return;
            var additionalBlockLocks = reachedBlock.AdditionalBlockLocks;
            if (additionalBlockLocks.Count == 0) return;

            foreach(var blockToLock in additionalBlockLocks)
            {
                if (string.IsNullOrEmpty(blockToLock)) continue;
                var fbs = Ctx?.GetFeedbacksDataForBlock(blockToLock);
                if (fbs == null || fbs.Count == 0) continue;
                foreach (var itt in fbs)
                {
                    if (itt == null) continue;
                    itt.LockedByBlock = block.identifier;
                }
            }
        }

        private void UnlockAdditionalBlocksFor(RouteBlock block)
        {
            var fbPlus = Ctx?.GetClientHandler()?._metadata?.FeedbacksData.GetByBlockId(block.identifier, SideMarker.Plus);
            if (fbPlus != null) fbPlus.LockedByBlock = string.Empty;

            var fbMinus = Ctx?.GetClientHandler()?._metadata?.FeedbacksData.GetByBlockId(block.identifier, SideMarker.Minus);
            if (fbMinus != null) fbMinus.LockedByBlock = string.Empty;
        }

        private async Task WaitForFb(string fbName, PlanItem fb, DataProvider dpS88)
        {
            await Task.Run(() =>
            {
                var reached = false;
                while (!reached && !IsCanceled())
                {
                    reached = IsFbReached(fbName, fb, dpS88, out var hasError);
                    // TODO handle hasError (e.g. cancel route)
                    if (!reached)
                        System.Threading.Thread.Sleep(TimeBetweenFbTestMsecs);
                }

                SendDebugMessage($"{fbName} reached!");

            }, CancelSource.Token);
        }

        private bool IsFbReached(string fbName, PlanItem fb, DataProvider dpS88, out bool hasError)
        {
            hasError = false;
            int ecosAddr;
            var r = Utilities.GetFeedbackAddress(fb, out var ecosAddr1, out var ecosAddr2, out _, out _);
            if (r)
            {
                var ec1 = 0;
                var ec2 = 0;
                if (ecosAddr1 != null) ec1 = ecosAddr1.Value;
                if (ecosAddr2 != null) ec2 = ecosAddr2.Value;
                Utilities.GetValidAddress(ec1, ec2, false, false, out ecosAddr, out _);
            }
            else
            {
                ecosAddr = fb.Addresses.Addr;
            }

            if (ecosAddr == 0)
            {
                Ctx?.LogInfo($"Feedback({fbName}) does not have a valid address.");
                hasError = true;
                return false;
            }

            var r0 = dpS88.GetFeedbackByAddress(ecosAddr, out var item, out var itemOffset, out var itemPin);
            if (item == null) return false;

            var pinState = item.Pin((uint)itemPin);
//#if DEBUG
//            Trace.WriteLine($"{r0} > {ecosAddr} is {itemOffset}:{itemPin} = {pinState}");
//#endif
            return pinState;
        }

        public static AutoModeTaskBase Create(NextRouteInformation route, AutoMode ctx)
        {
            var instance = new AutoModeTaskBase
            {
                Ctx = ctx,
                Route = route
            };

            return instance;
        }
    }
}
