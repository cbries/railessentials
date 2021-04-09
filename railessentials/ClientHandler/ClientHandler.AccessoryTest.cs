// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ClientHandler.AccessoryTest.cs

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace railessentials.ClientHandler
{
    internal class AccessoryTestData
    {
        public int Id { get; set; } = 0;
        public int Periods { get; set; } = 0;
        public int PauseMsecs { get; set; } = 0;

        public bool _isFinished = false;
        public int _runs = 0;
        public int _lastRun = 0;
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

            if (accessory.Id <= 0)
            {
                errorMessage = "Invalid accessory id";
                return false;
            }

            var isAvailable = false;

            // check if accessory is already added and running
            foreach (var it in _cqAccessoryTester)
            {
                if (it.Key != accessory.Id) continue;
                if (it.Value == null) continue;

                isAvailable = true;

                if (!it.Value._isFinished)
                {
                    errorMessage = "Accessory already added and running";
                    return false;
                }
            }

            var res = false;

            if (isAvailable)
            { 
                res = _cqAccessoryTester.TryRemove(accessory.Id, out _);
                if (!res)
                {
                    errorMessage = "Remove of the previous accessory failed.";
                    return false;
                }
            }

            res = _cqAccessoryTester.TryAdd(accessory.Id, accessory);
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
                // TODO

                Thread.Sleep(AccessoryTestMinimumDelayBetweenCalls);
            }
        }
    }
}
