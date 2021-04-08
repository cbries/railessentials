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
            int maxSeconds = -1,
            Func<bool> hasToBeCanceled = null)
        {
            if (ecosLoc == null) return;
            if (speedCurve == null) return;

            currentSpeed = KickStart(currentSpeed, ecosLoc);

            if (maxSeconds <= -1)
                maxSeconds = speedCurve.MaxTime;

            var targetSpeed = speedCurve.MaxSpeed;
            var timeSteps = speedCurve.MaxTime / (float)speedCurve.MaxSpeed * 1000.0;

            await Task.Run(() =>
            {
                var hasCanceled = false;

                var sw = Stopwatch.StartNew();
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

                    //
                    // walltime reached
                    //
                    if (sw.ElapsedMilliseconds / 1000 > maxSeconds)
                        return;

                    if (IsCanceled())
                    {
                        hasCanceled = true;
                        break;
                    }

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;

                    if (__delayAccelerate((int)timeSteps, sw, maxSeconds, hasToBeCanceled))
                        return;

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
            Func<bool> hasToBeCanceled = null)
        {
            const int maxSeconds = 10;
            var maxSpeedSteps = ecosLoc.GetNumberOfSpeedsteps();
            var msecsDelay = maxSpeedSteps < 30 ? 1000 : 250;

            await Task.Run(() =>
            {
                currentSpeed = KickStart(currentSpeed, ecosLoc);

                var hasCanceled = false;
                var newCurrentSpeed = currentSpeed;

                var sw = Stopwatch.StartNew();
                
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

                    if (__delayAccelerate(msecsDelay, sw, maxSeconds, hasToBeCanceled))
                        return;

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;
                }

                if (!hasCanceled)
                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, newCurrentSpeed);

            }, CancelSource.Token);
        }

        private bool __delayAccelerate(
            int timeSteps,
            Stopwatch sw,
            int maxSeconds = 10,
            Func<bool> hasToBeCanceled = null)
        {
            var sl = timeSteps;
            var deltaSteps = 10;
            var slSteps = sl / deltaSteps;
            for (var jj = 0; jj < deltaSteps; ++jj)
            {
                if (hasToBeCanceled != null)
                    if (hasToBeCanceled())
                        return true;

                //
                // walltime reached
                //
                if (sw.ElapsedMilliseconds / 1000 > maxSeconds)
                    return true;

                System.Threading.Thread.Sleep(slSteps);
            }

            return false;
        }

        private int KickStart(int currentSpeed, Locomotive ecosLoc)
        {
            var maxSpeedSteps = ecosLoc.GetNumberOfSpeedsteps();
            var previousSpeed = currentSpeed;

            var kickStartSpeed = 0;
            if (maxSpeedSteps <= 14)
                kickStartSpeed = Globals.DccKickStartM14;
            else if (maxSpeedSteps <= 28)
                kickStartSpeed = Globals.DccKickStartM28;
            else if (maxSpeedSteps <= 128)
                kickStartSpeed = Globals.DccKickStartM128;

            if (currentSpeed > kickStartSpeed)
                return currentSpeed;
          
            Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, kickStartSpeed);
            System.Threading.Thread.Sleep(Globals.DccKickStartDelayMsecs);
            if (previousSpeed < 1) return kickStartSpeed;
            Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, previousSpeed);
            return previousSpeed;
        }
    }
}
