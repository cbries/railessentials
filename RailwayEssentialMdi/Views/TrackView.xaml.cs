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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using RailwayEssentialMdi.ViewModels;
using TrackInformation;
using TrackInformationCore;
using CheckBox = System.Windows.Controls.CheckBox;
using UserControl = System.Windows.Controls.UserControl;

namespace RailwayEssentialMdi.Views
{
    public partial class TrackView : UserControl
    {
        public TrackView()
        {
            InitializeComponent();
        }

        private readonly List<CheckBox> _generatedCheckboxes = new List<CheckBox>();

        private void TrackView_OnInitialized(object sender, EventArgs e)
        {
            var fncNames = Locomotive.GetFncTypenames();
            int n = fncNames.Count;

            DockPanel[] panel = { new DockPanel(), new DockPanel() };
            
            for (int i = 0; i < n; ++i)
            {
                var name = fncNames[i];
                if (string.IsNullOrEmpty(name))
                    continue;

                CheckBox chkStart = new CheckBox { Name = "ChkFncStart" + i, Content = name, Margin = new Thickness(2,2,0,2) };
                CheckBox chkStop = new CheckBox { Name = "ChkFncStop" + i, Content = name, Margin = new Thickness(2, 2, 0, 2) };

                _generatedCheckboxes.Add(chkStart);
                _generatedCheckboxes.Add(chkStop);

                panel[0].Children.Add(chkStart);
                panel[1].Children.Add(chkStop);
            }

            GrpS88FncsStart.Content = panel[0];
            GrpS88FncsStop.Content = panel[1];
        }

        private void TrackView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var ctx = DataContext as TrackWindow;
            if (ctx == null)
                return;

            ctx.TrackView = this;
            ctx.TrackViewZoomer = trackViewer;
        }

        public CheckBox GetChk(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            foreach (var chk in _generatedCheckboxes)
            {
                if (chk == null)
                    continue;
                if (string.IsNullOrEmpty(chk.Name))
                    continue;
                if (chk.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return chk;
            }
            return null;
        }

        public CheckBox GetCtrl(string name)
        {
            if (name.Equals("ChkFncToggle", StringComparison.OrdinalIgnoreCase))
                return ChkFncToggle;
            return null;
        }

        public void SetCheckboxState(bool state)
        {
            foreach (var chk in _generatedCheckboxes)
            {
                if (chk == null)
                    continue;
                chk.IsEnabled = state;
                chk.IsChecked = false;
            }
        }

        public List<FncTypes> StartFncs
        {
            get
            {
                var fncNames = Locomotive.GetFncTypenames();
                int n = fncNames.Count;

                List<FncTypes> fncs = new List<FncTypes>();
                for(int i=0; i < n; ++i)
                {
                    var name = $"ChkFncStart{i}";
                    var chk = GetChk(name);
                    if (chk == null)
                        continue;

                    bool state = chk.IsChecked ?? false;
                    if(state)
                        fncs.Add((FncTypes) i);
                }
                return fncs;
            }
        }

        public List<FncTypes> StopFncs
        {
            get
            {
                var fncNames = Locomotive.GetFncTypenames();
                int n = fncNames.Count;

                List<FncTypes> fncs = new List<FncTypes>();
                for (int i = 0; i < n; ++i)
                {
                    var name = $"ChkFncStop{i}";
                    var chk = GetChk(name);
                    if (chk == null)
                        continue;

                    bool state = chk.IsChecked ?? false;
                    if(state)
                        fncs.Add((FncTypes)i);
                }
                return fncs;
            }
        }

        public bool ToggleFncs
        {
            get
            {
                var name = $"ChkFncToggle";
                var chk = GetCtrl(name);
                if (chk == null)
                    return false;

                return chk.IsChecked ?? false;
            }
        }

    }
}
