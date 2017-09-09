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
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using RailwayEssentialMdi.Interfaces;
using Image = System.Drawing.Image;

namespace RailwayEssentialMdi.Views
{
    public partial class LocomotivesView : UserControl, ILocomotiveView
    {
        private ViewModels.LocomotivesWindow _dataContext;

        public LocomotivesView()
        {
            InitializeComponent();
        }

        private void LocomotivesView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var m = DataContext as ViewModels.LocomotivesWindow;
            if (m != null)
            {
                _dataContext = m;

                m.LocomotiveView = this;

                _dataContext.UpdateFuncset();
            }
        }

        #region ILocomotiveView

        public ToggleButton GetToggleButton(string name)
        {
            foreach (var c in FncButtons.Children)
            {
                if (c == null)
                    continue;
                var tbn = c as ToggleButton;
                if (tbn == null)
                    continue;

                if (tbn.Content.Equals(name))
                    return tbn;
            }

            return null;
        }

        public void SetToggleButton(string name, bool state)
        {
            var btn = GetToggleButton(name);
            if (btn != null)
                btn.IsChecked = state;
        }

        public void SetToggleButtonVisibility(string name, bool visible)
        {
            var btn = GetToggleButton(name);
            if (btn != null)
            {
                if(visible)
                    btn.Visibility = Visibility.Visible;
                else
                    btn.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        private void CmdFncTypeSet_OnClick(object sender, RoutedEventArgs e)
        {
            if (_dataContext == null)
                return;

            _dataContext.Entity.ObjectItem.ApplyFncType();
        }

        private void SpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_dataContext == null)
                return;
            var v = (int) SpeedSlider.Value;
            if (_dataContext.Speed != v)
                _dataContext.Speed = v;
        }

        private void SpeedSlider_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (_dataContext == null)
                return;

            _dataContext.PromoteSpeed();
        }

        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Please select a locomotive image"
            };
            var res = dlg.ShowDialog();
            if (res.HasValue && !res.Value)
                return;

            Image img = Image.FromFile(dlg.FileName);
            ImageFormat fmt = ImageFormat.Bmp;
            //var imgExt = Path.GetExtension(dlg.FileName);
            //if (!string.IsNullOrEmpty(imgExt))
            //{
            //    if (imgExt.EndsWith("png", StringComparison.OrdinalIgnoreCase))
            //        fmt = ImageFormat.Png;
            //    else if(imgExt.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            //        || imgExt.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase))
            //        fmt = ImageFormat.Jpeg;
            //    else if (imgExt.EndsWith("bmp", StringComparison.OrdinalIgnoreCase))
            //        fmt = ImageFormat.Bmp;
            //    else
            //    {
            //        return;
            //    }
            //}
            var imgBase64 = ImageHelper.ImageToBase64(img, fmt);
            if (_dataContext != null && _dataContext.Entity != null)
            {
                var locobj = _dataContext.Entity.ObjectItem;
                if (locobj != null)
                {
                    locobj.LocomotiveImageBase64Format = fmt;
                    locobj.LocomotiveImageBase64 = imgBase64;                    
                }
                _dataContext.Entity.UpdateUi();
            }
        }
    }
}
