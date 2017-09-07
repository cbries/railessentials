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
using System.Text.RegularExpressions;

namespace RailwayEssentialMdi.ViewModels
{
    using System.Windows.Controls.Primitives;
    using Commands;
    using Entities;
    using Interfaces;

    public class LocomotivesWindow : BaseWindow
    {
        public ILocomotiveView LocomotiveView { get; set; }

        private LocomotiveEntity _entity;

        public LocomotiveEntity Entity
        {
            get
            {
                return _entity;
            }

            set
            {
                _entity = value;

                UpdateFuncset();

                RaisePropertyChanged("Entity");
            }
        }

        private bool ValidateName(string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z0-9_ ]+$");
        }

        public override string Name
        {
            get => Entity != null ? Entity.Name : "-";
            set
            {
                if (ValidateName(value))
                    Entity.Name = value;

                RaisePropertyChanged("Name");
            }
        }

        public int Speed
        {
            get => Entity == null ? 0 : Entity.ObjectItem.Speed;
            set
            {
                if (Entity != null)
                {
                    Entity.ObjectItem.Speed = value;
                    RaisePropertyChanged("Speed");
                }
            }
        }

        public RelayCommand SwitchFncCommand { get; }
        public RelayCommand SpeedIncCommand { get; }
        public RelayCommand SpeedDecCommand { get; }
        public RelayCommand StopCommand { get; }

        public LocomotivesWindow()
        {
            SwitchFncCommand = new RelayCommand(SwitchFnc);
            SpeedIncCommand = new RelayCommand(SpeedInc);
            SpeedDecCommand = new RelayCommand(SpeedDec);
            StopCommand = new RelayCommand(Stop);

            _entity?.UpdateUi();

            UpdateFuncset();
        }

        private void SwitchFnc(object p)
        {
            int index;
            if (!int.TryParse(p.ToString(), out index))
                return;

            var name = $"F{index}";

            if (string.IsNullOrEmpty(name))
                return;

            ToggleButton btn = LocomotiveView.GetToggleButton(name);
            
            if(Entity != null && btn != null && btn.IsChecked.HasValue)
                Entity.ObjectItem.ToggleFunction((uint)index, btn.IsChecked.Value);
        }

        private void SpeedInc(object p)
        {
            if (Entity == null)
                return;
            var v = Entity.ObjectItem.Speed;
            v += 5;
            if (v >= 100)
                v = 100;

            Speed = v;

            PromoteSpeed();
        }

        private void SpeedDec(object p)
        {
            if (Entity == null)
                return;
            var v = Entity.ObjectItem.Speed;
            v -= 5;
            if (v <= 0)
                v = 0;

            Speed = v;

            PromoteSpeed();
        }

        private void Stop(object p)
        {
            Speed = 0;
            PromoteSpeed();

            if(Entity != null)
                Entity.ObjectItem.Stop();
        }

        public void PromoteSpeed()
        {
            Entity.ObjectItem.ChangeSpeed(Entity.ObjectItem.Speed);
        }

        public void UpdateFuncset()
        {
            RaisePropertyChanged("Name");

            if (LocomotiveView == null)
                return;

            int nrOfFunctions = 0;
            if (_entity != null && _entity.ObjectItem != null)
                nrOfFunctions = _entity.ObjectItem.NrOfFunctions;
            
            List<string> fncNames = new List<string>();

            for (int i = 0; i < 32; ++i)
            {
                string name = $"F{i}";

                if (string.IsNullOrEmpty(name))
                    continue;

                if (i < nrOfFunctions)
                {
                    if (_entity != null && _entity.ObjectItem != null)
                    {
                        var state = _entity.ObjectItem.Funcset[i];
                        LocomotiveView.SetToggleButton(name, state);
                        LocomotiveView.SetToggleButtonVisibility(name, true);
                        fncNames.Add(name);
                    }
                }
                else
                {
                    LocomotiveView.SetToggleButtonVisibility(name, false);
                }
            }

            fncNames.Insert(0, "--");

            Entity.ObjectItem.FncNames = fncNames;
        }        
    }
}
