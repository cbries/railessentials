// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: StopWatch.cs

using System;
using System.Diagnostics;

namespace ecoslib.Utilities
{
    public class StopWatch
    {
        public static StopWatch Create()
        {
            var instance = new StopWatch();
            instance.Start();
            return instance;
        }

        private readonly Stopwatch _sw;

        public StopWatch()
        {
            _sw = new Stopwatch();
        }

        public void Start()
        {
            if (_sw != null)
            {
                _sw.Reset();

                if (!_sw.IsRunning)
                    _sw.Start();
            }
        }

        public void Stop()
        {
            _sw?.Stop();
        }

        public void Show(string info)
        {
            Trace.WriteLine(string.Format("<RailwayEssential> {0} Time: {1}", info, Elapsed));
        }

        public TimeSpan Elapsed
        {
            get
            {
                if (_sw != null)
                    return _sw.Elapsed;
                return TimeSpan.Zero;
            }
        }

        public long ElapsedMilliseconds
        {
            get
            {
                if (_sw != null)
                    return _sw.ElapsedMilliseconds;
                return 0;
            }
        }

        public long ElapsedTicks
        {
            get
            {
                if (_sw != null)
                    return _sw.ElapsedTicks;
                return 0;
            }
        }
    }
}
