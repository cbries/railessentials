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
using System.ComponentModel;
using RailwayEssentialMdi.Autoplay.Ui;
using RailwayEssentialMdi.ViewModels;

namespace RailwayEssentialMdi.Autoplay
{
    public partial class Autoplay
    {
        public EventHandler Started;
        public EventHandler Stopped;
        public EventHandler Failed;

        public RailwayEssentialModel Ctx { get; set; }

        private BackgroundWorker _worker = null;

        private bool _stopped = true;
        private bool _started = false;

        public bool IsRunning
        {
            get
            {
                if (_worker == null)
                    return false;

                if (_stopped && !_started)
                    return false;

                if (_worker.CancellationPending)
                    return false;

                return _started && _worker.IsBusy;
            }
        }

        public Autoplay()
        {
            
        }

        public bool Start()
        {
            if (_started)
                return _started;

            _stopped = false;
            _started = true;

            if (IsRunning)
                return true;

            _worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _worker.DoWork += Worker_DoWork;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            try
            {
                InitializeRouteThreads();
            }
            catch (Exception ex)
            {
                Ctx.LogError(ex.Message);
            }

            _worker.RunWorkerAsync();

            Started?.Invoke(this, null);

            return true;
        }

        public bool Stop()
        {
            if (_stopped)
                return _stopped;

            _started = false;
            _stopped = true;

            try
            {
                if (_worker != null && _worker.WorkerSupportsCancellation)
                    _worker.CancelAsync();
            }
            catch
            {
                // ignore
            }

            // in case any train is running
            // let them reach their destination
            var tasks = GetRunningRouteThreads();
            if (tasks.Count > 0)
            {
                DoWorkWithModal(progress =>
                {
                    for (;;)
                    {
                        var ts = GetRunningRouteThreads();
                        if (ts.Count == 0)
                            break;

                        int n = ts.Count;
                        if (n > 1)
                        {
                            progress.Report($"Wait for {ts.Count} trains which run for reaching their destination.");
                        }
                        else
                        {
                            progress.Report($"Wait for one train until it reaches its final destination.");
                        }

                        System.Threading.Thread.Sleep(250);
                    }
                });
            }

            try
            {
                StopRouteThreads();
            }
            catch
            {
                // ignore
            }

            Stopped?.Invoke(this, null);

            return true;
        }

        public void Cleanup()
        {
            if (_worker == null)
                return;

            try
            {
                _worker.Dispose();
                _worker = null;
            }
            catch
            {
                // ignore
            }
        }

        public static void DoWorkWithModal(Action<IProgress<string>> work)
        {
            ProgressWindow dlg = new ProgressWindow {Title = "Wait for Trains..."};
            BackgroundWorker worker = new BackgroundWorker();
            dlg.Loaded += (_, args) =>
            {
                Progress<string> progress = new Progress<string>(data => dlg.Msg.Text = data);
                worker.DoWork += (s, workerArgs) => work(progress);
                worker.RunWorkerCompleted += (s, workerArgs) => dlg.Close();
                worker.RunWorkerAsync();
                worker.Dispose();
            };

            dlg.CmdCancel.Click += (s, ev) =>
            {
                dlg.Close();

                if (worker != null)
                {
                    worker.Dispose();
                    worker = null;
                }
            };
            dlg.ShowDialog();
        }
    }
}
