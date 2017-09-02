/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Diagnostics;

namespace RailwayEssentialCore
{
    public class StopWatch
    {
        public static StopWatch Create()
        {
            StopWatch instance = new StopWatch();
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
            if (_sw != null)
                _sw.Stop();
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
