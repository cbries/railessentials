// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoModeTaskBase.Decelerate.cs

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ecoslib.Entities;
using railessentials.Locomotives;

namespace railessentials.AutoMode
{
    public partial class AutoModeTaskBase
    {
        private async Task DecelerateLocomotiveCurve(
            Locomotive ecosLoc,
            SpeedCurve speedCurve,
            int maxSeconds = -1, 
            Func<bool> hasToBeCanceled = null
        )
        {
            Trace.WriteLine("DecelerateLocomotiveCurve()");

            if (maxSeconds <= -1)
                maxSeconds = speedCurve.MaxTime;

            var currentSpeed = (float)ecosLoc.Speedstep;
            var maxSpeed = speedCurve.MaxSpeed;
            var minSpeed = ecosLoc.GetNumberOfSpeedsteps() <= 28 ? 2 : 10;
            var timeSteps = (speedCurve.MaxTime / (float) maxSpeed) * 1000.0;

            await Task.Run(() =>
            {
                // 
                // IMPORTANT NOTE:
                // do not slow down the locomotive completly
                // we still have to reach the fbIn, when reached
                // the train will stop right at this moment
                //

                var sw = Stopwatch.StartNew();
                var idx = -1;
                for(var i = 0; i < speedCurve.Steps.Count - 1; ++i)
                {
                    var s0 = speedCurve.Steps[i];
                    var s1 = speedCurve.Steps[i + 1];
                    if(currentSpeed >= s0.Speed && currentSpeed < s1.Speed)
                    {
                        idx = i;
                        break;
                    }
                }
                
                if (idx == -1) 
                    idx = speedCurve.Steps.Count - 1;

              for (var i = idx; i > minSpeed; --i)
                {
                    var nextSpeed = speedCurve.Steps[i];

                    //
                    // walltime reached
                    //
                    if (sw.ElapsedMilliseconds / 1000 > maxSeconds)
                        return;

                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, (int)nextSpeed.Speed);

                    if (IsCanceled())
                    {
                        Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, 0);

                        return;
                    }

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            return;

                    //
                    // split delay for higher recognition
                    //
                    var sl = (int)timeSteps;
                    var deltaSteps = 10;
                    var slSteps = sl / deltaSteps;
                    for (var jj = 0; jj < deltaSteps; ++jj)
                    {
                        if (hasToBeCanceled != null)
                            if (hasToBeCanceled())
                                return;

                        //
                        // walltime reached
                        //
                        if (sw.ElapsedMilliseconds / 1000 > maxSeconds)
                            return;

                        System.Threading.Thread.Sleep(slSteps);
                    }

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            return;
                }

            }, CancelSource.Token);
        }

        private async Task DecelerateLocomotive(
            Locomotive ecosLoc,
            int maxSecsToStop = 10,
            Func<bool> hasToBeCanceled = null)
        {
            Trace.WriteLine("DecelerateLocomotive()");

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
    }
}
