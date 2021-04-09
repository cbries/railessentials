// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ClientHandler.AccessoryTest.cs

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using ecoslib.Entities;
// ReSharper disable InconsistentNaming

namespace railessentials.ClientHandler
{
    internal class AccessoryTestData
    {
        public Accessory EcosAcc { get; set; }
        public int Periods { get; set; } = 0;
        public int PauseMsecs { get; set; } = 0;

        public bool _isFinished;
        public int _recentState;
        public int _runs;
        public DateTime _lastRun = DateTime.MinValue;
    }

    public partial class ClientHandler
    {
        private const int AccessoryTestMinimumDelayBetweenCalls = 250;
        private BackgroundWorker _bgAccessoryTester;
        private readonly ConcurrentDictionary<int, AccessoryTestData> _cqAccessoryTester = new();

        internal bool AddAccessoryTest(AccessoryTestData accessory, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (accessory == null)
            {
                errorMessage = "Invalid accessory data";
                return false;
            }

            if (accessory.EcosAcc.Addr <= 0)
            {
                errorMessage = "Invalid accessory id";
                return false;
            }

            var isAvailable = false;

            // check if accessory is already added and running
            foreach (var it in _cqAccessoryTester)
            {
                if (it.Key != accessory.EcosAcc.Addr) continue;
                if (it.Value == null) continue;

                isAvailable = true;

                if (!it.Value._isFinished)
                {
                    errorMessage = "Accessory already added and running";
                    return false;
                }
            }

            bool res;

            if (isAvailable)
            {
                res = _cqAccessoryTester.TryRemove(accessory.EcosAcc.Addr, out _);
                if (!res)
                {
                    errorMessage = "Remove of the previous accessory failed.";
                    return false;
                }
            }

            res = _cqAccessoryTester.TryAdd(accessory.EcosAcc.Addr, accessory);
            if (!res)
                errorMessage = "Adding of accessory failed.";

            return res;
        }

        private void InitAccessoryBw()
        {
            if (_bgAccessoryTester == null)
            {
                _bgAccessoryTester = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = false
                };
                _bgAccessoryTester.DoWork += BgAccessoryTesterOnDoWork;
                _bgAccessoryTester.ProgressChanged += BgAccessoryTesterOnProgressChanged;
                _bgAccessoryTester.RunWorkerAsync();
            }
        }

        private void BgAccessoryTesterOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Trace.WriteLine(">" + e.ProgressPercentage);
        }

        private void BgAccessoryTesterOnDoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                foreach (var it in _cqAccessoryTester)
                {
                    if (it.Value._isFinished) continue;
                    var dtLastRun = it.Value._lastRun;
                    var dtNow = DateTime.Now;
                    var dtDelta = dtNow - dtLastRun;
                    if (dtDelta.Milliseconds < it.Value.PauseMsecs)
                        continue; // do not run when delta is less the walltime

                    // switch the accessory
                    if (it.Value._recentState == 0) it.Value._recentState = 1;
                    else it.Value._recentState = 0;

                    var acc = it.Value.EcosAcc;

                    if (IsSimulationMode())
                    {
                        acc.SwitchSimulation(it.Value._recentState);
                    }
                    else
                    {
                        acc.Switch(it.Value._recentState);
                    }

                    it.Value._runs++;

                    if(it.Value._runs >= 2 * it.Value.Periods)
                    {
                        it.Value._isFinished = true;
                        it.Value._lastRun = DateTime.MaxValue;
                    }
                    else
                    {
                        it.Value._lastRun = DateTime.Now;
                    }
                }

                if (IsSimulationMode())
                {
                    _sniffer?.TriggerDataProviderModifiedForSimulation();
                }
                else
                {
                    _sniffer?.SendCommandsToEcosStation();
                }

                Thread.Sleep(AccessoryTestMinimumDelayBetweenCalls);
            }

            // ignore, function will never end
        }
    }
}
