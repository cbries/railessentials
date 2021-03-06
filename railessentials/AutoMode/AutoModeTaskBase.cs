// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoModeTaskBase.cs

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ecoslib.Entities;
using railessentials.Analyzer;
using railessentials.Plan;

namespace railessentials.AutoMode
{
    public class AutoModeTaskBase : AutoModeTaskCore
    {
        private const int TimeBetweenFbTestMsecs = 500;
                
        private bool IsCanceled()
        {
            if (!CancelSource.IsCancellationRequested) return false;
            
            // frees the current route and resets the relevant block states
            Ctx?.ResetRouteFor(Route.LocomotiveObjectId);
            
            SendDebugMessage($"{Route.Route.Name} has been canceled");
            return true;
        }

        public async override Task Run()
        {
            #region prepare data for autoMode task

            var dpS88 = Route.DataProviderS88;

            var currentDirection = Route.Locomotive.Direction;
            var currentSpeed = Route.Locomotive.Speedstep;
            var maxSpeed = Route.Locomotive.GetNumberOfSpeedsteps();

            // TODO change targetSpeed source -- currently only for tests
            var targetSpeed = (int)(maxSpeed / 2.0);

            #endregion

            await Task.Run(async () =>
            {
                SendDebugMessage($"Start: {Route.Route.Name}");

                if (IsCanceled()) return;

                //
                // NOTE start train, if the fbEnter is reached before the train got its full speed, cancel the speed-up
                //
                var fbEnterAlreadyReached = false;
                await AccelerateLocomotive(currentSpeed, targetSpeed, Route.Locomotive, hasToBeCanceled: () =>
                {
                    fbEnterAlreadyReached = IsFbReached("FbEnter", Route.FbEnter, dpS88, out var hasError);
                    // TODO handle hasError (e.g. cancel route)
                    return fbEnterAlreadyReached;
                });

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
                var sw = new Stopwatch();
                sw.Start();

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
                await DecelerateLocomotive(Route.Locomotive, hasToBeCanceled: () =>
                {
                    fbInAlreadyReached = IsFbReached("FbIn", Route.FbIn, dpS88, out var hasError);
                    // TODO handle hasError (e.g. cancel route)
                    return fbInAlreadyReached;
                });

                if (IsCanceled()) return;

                //
                // NOTE wait for entering the fbIn feedback
                //
                if (!fbInAlreadyReached)
                    await WaitForFb("FbIn", Route.FbIn, dpS88);
                sw.Stop();
                SendDebugMessage($"{sw.Elapsed.TotalSeconds} seconds between 'enter' and 'in'.");

                if (IsCanceled()) return;

                //
                // TODO save the time for future use
                //
                //...

                //
                // NOTE stop the locomotive in any case when fbIn is reached
                //
                Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(Route.Locomotive, 0);

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

                TriggerFinished();

            }, CancelSource.Token);
        }

        private async Task DecelerateLocomotive(
            Locomotive ecosLoc,
            int maxSecsToStop = 10,
            Func<bool> hasToBeCanceled = null)
        {
            var currentSpeed = (float)ecosLoc.Speedstep;
            var deltaSpeedSteps = currentSpeed / maxSecsToStop;

            var minSpeed = ecosLoc.GetNumberOfSpeedsteps() <= 28 ? 2 : 10;

            await Task.Run(() =>
            {
                // 
                // IMPORTANT NOTE:
                // do not slow down the locomotive completly
                // we still have to reach the fbIn, when reached
                // the train will stop right at this moment
                //

                for (var i = currentSpeed; i > minSpeed; i -= deltaSpeedSteps)
                {
                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, (int)i);

                    if (IsCanceled())
                    {
                        Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, 0);

                        break;
                    }

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;

                    var sleepMs = (maxSecsToStop * 1000) / deltaSpeedSteps;
                    System.Threading.Thread.Sleep((int)sleepMs);

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;
                }
                
            }, CancelSource.Token);
        }

        private async Task AccelerateLocomotive(
            int currentSpeed,
            int targetSpeed,
            Locomotive ecosLoc,
            TimeSpan? delayBetween = null,
            Func<bool> hasToBeCanceled = null)
        {
            var maxSpeedSteps = ecosLoc.GetNumberOfSpeedsteps();
            var msecsDelay = maxSpeedSteps < 30 ? 1000 : 250;

            delayBetween ??= new TimeSpan(0, 0, 0, 0, msecsDelay);

            // TODO add nice accelerate curve

            await Task.Run(() =>
            {
                var hasCanceled = false;

                for (var i = currentSpeed; i <= targetSpeed; ++i)
                {
                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, i);

                    if (IsCanceled()) {
                        hasCanceled = true;
                        break;
                    }

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;

                    System.Threading.Thread.Sleep(delayBetween.Value);

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;
                }

                if(!hasCanceled)
                {
                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, targetSpeed);
                }

            }, CancelSource.Token);
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
            var r = Utilities.GetEcosAddress(fb, out var ecosAddr1, out var ecosAddr2, out _, out _);
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
            var pinState = item.Pin((uint)itemPin);
#if DEBUG
            Trace.WriteLine($"{r0} > {ecosAddr} is {itemOffset}:{itemPin} = {pinState}");
#endif
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
