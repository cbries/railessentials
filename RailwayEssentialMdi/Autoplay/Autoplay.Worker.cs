﻿/*
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
using System.Threading;

namespace RailwayEssentialMdi.Autoplay
{
    public partial class Autoplay
    {
        private static int WorkerDelay = 1 * 1000;

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var w = sender as BackgroundWorker;

            for(;;)
            {
                if (w != null && w.CancellationPending)
                {
                    e.Cancel = true;

                    return;
                }

                try
                {
                    Check();
                }
                catch
                {
                    // ignore
                }

                Thread.Sleep(WorkerDelay);
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var msg = "";

            if(e.Cancelled)
                msg = "Canceled!\n";
            else if (e.Error != null)
                msg = "Error: " + e.Error.Message + "\n";
            else
                msg = "Done!\n";

            if (Ctx != null && Ctx._ctx != null)
            {
                Ctx._ctx.Send(state =>
                {
                    Ctx.LogAutoplay(msg);
                }, new object());
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //this.tbProgress.Text = (e.ProgressPercentage.ToString() + "%");
        }
    }
}
