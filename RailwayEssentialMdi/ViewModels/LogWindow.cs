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
using System.IO;
using System.Text;
using RailwayEssentialMdi.Commands;
using RailwayEssentialMdi.Entities;

namespace RailwayEssentialMdi.ViewModels
{
    public class LogWindow : BaseWindow
    {
        public enum Mode
        {
            General,
            Commands,
            Autoplay
        }

        public Mode LogMode { get; set; }

        public override string Name
        {
            get
            {
                if (LogMode == Mode.General)
                    return "General Log";
                else if (LogMode == Mode.Autoplay)
                    return "Autoplay Log";
                return "Command Log";
            }
        }

        public RelayCommand SaveCommand { get; }

        public LogEntity Log { get; }

        private bool _autoscroll;

        public bool Autoscroll
        {
            get => _autoscroll;
            set
            {
                _autoscroll = value;
                RaisePropertyChanged("Autoscroll");
            }
        }

        public LogWindow(LogEntity logMsgs)
        {
            LogMode = Mode.General;
            Log = logMsgs;
            
            SaveCommand = new RelayCommand(SaveCmd);
        }

        private void SaveCmd(object p)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{LogMode}_log",
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt"
            };

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;

                try
                {
                    File.WriteAllText(filename, Log.Message, Encoding.UTF8);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
