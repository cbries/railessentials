// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoModeTaskBase.Accelerate.cs

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ecoslib.Entities;
using railessentials.Locomotives;

namespace railessentials.AutoMode
{
    public partial class AutoModeTaskBase
    {
        private async Task AccelerateLocomotiveCurve(
            int currentSpeed,
            Locomotive ecosLoc,
            SpeedCurve speedCurve,
            int maxTime = -1, // TODO measure real time between FB enter and FB in
            Func<bool> hasToBeCanceled = null)
        {
            Trace.WriteLine("AccelerateLocomotiveCurve()");

            if (ecosLoc == null) return;
            if (speedCurve == null) return;

            var targetSpeed = speedCurve.MaxSpeed;
            var delayBetween = speedCurve.MaxTime / (float)speedCurve.MaxSpeed * 1000.0;

            await Task.Run(() =>
            {
                var hasCanceled = false;
                
                var idxOfCurrentSpeed = 0;
                for (var i = 0; i < speedCurve.Steps.Count - 1; ++i)
                {
                    var itSpeed = speedCurve.Steps[i];
                    var itSpeed2 = speedCurve.Steps[i + 1];
                    if (currentSpeed >= itSpeed.Speed && currentSpeed < itSpeed2.Speed)
                    {
                        idxOfCurrentSpeed = i;
                        break;
                    }
                }

                for (var i = idxOfCurrentSpeed; i <= speedCurve.Steps.Count; ++i)
                {
                    var newSpeed = speedCurve.Steps[i].Speed;
                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, (int)newSpeed);
                    if (newSpeed >= targetSpeed)
                        break;

                    if (IsCanceled())
                    {
                        hasCanceled = true;
                        break;
                    }

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;

                    System.Threading.Thread.Sleep((int)delayBetween);

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;
                }

                if (hasCanceled)
                {
                    // TBD

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
            Trace.WriteLine("AccelerateLocomotive()");

            var maxSpeedSteps = ecosLoc.GetNumberOfSpeedsteps();
            var msecsDelay = maxSpeedSteps < 30 ? 1000 : 250;

            delayBetween ??= new TimeSpan(0, 0, 0, 0, msecsDelay);

            await Task.Run(() =>
            {
                var hasCanceled = false;
                var newCurrentSpeed = currentSpeed;

                for (var i = currentSpeed; i <= targetSpeed; ++i)
                {
                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, i);
                    newCurrentSpeed = i;

                    if (IsCanceled())
                    {
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

                if (!hasCanceled)
                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, newCurrentSpeed);

            }, CancelSource.Token);
        }

    }
}
