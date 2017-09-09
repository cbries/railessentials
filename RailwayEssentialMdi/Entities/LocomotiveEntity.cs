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

using System.Drawing.Imaging;
using System.IO.Packaging;
using System.Runtime.Remoting.Messaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RailwayEssentialMdi.Entities
{
    using System;
    using System.Diagnostics;

    public class LocomotiveEntity : Bases.ViewModelBase
    {
        public event EventHandler Changed;

        private TrackInformation.Locomotive _objectItem;

        public TrackInformation.Locomotive ObjectItem
        {
            get => _objectItem;
            set
            {
                _objectItem = value;

                if (_objectItem != null)
                    _objectItem.Ctx = Model;

                UpdateUi();

                RaisePropertyChanged("ObjectItem");
                RaisePropertyChanged("Name");
            }
        }
        
        #region Name

        public string Name
        {
            get => _objectItem != null ? _objectItem.Name : "-";

            set
            {
                if (_objectItem != null)
                {
                    _objectItem.Name = value;
                    _objectItem.ChangeName(value);
                }

                RaisePropertyChanged("Name");
            }
        }

        #endregion

        #region MaxSpeed 

        public int MaxSpeedPercentage
        {
            get => _objectItem != null ? _objectItem.MaxSpeedPercentage : 50;
            set
            {
                if (_objectItem != null)
                    _objectItem.MaxSpeedPercentage = value;

                RaisePropertyChanged("MaxSpeedPercentage");
            }
        }

        #endregion

        #region BlockSpeed 

        public int BlockSpeedPercentage
        {
            get => _objectItem != null ? _objectItem.BlockSpeedPercentage : 25;
            set
            {
                if (_objectItem != null)
                    _objectItem.BlockSpeedPercentage = value;

                RaisePropertyChanged("BlockSpeedPercentage");
            }
        }

        #endregion

        #region ContentId

        private string _contentId = null;
        public string ContentId
        {
            get { return _contentId; }
            set
            {
                if (_contentId != value)
                {
                    _contentId = value;
                    RaisePropertyChanged("ContentId");
                }
            }
        }

        #endregion

        #region IsSelected

        private bool _isSelected = false;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    RaisePropertyChanged("IsSelected");
                }
            }
        }

        #endregion

        #region IsActive

        private bool _isActive = false;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    RaisePropertyChanged("IsActive");
                }
            }
        }

        #endregion

        #region IsEnabled

        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    RaisePropertyChanged("IsEnabled");
                }
            }
        }

        #endregion

        #region Drive Direction

        private bool _driveForward;
        private bool _driveBackward;

        public bool DriveForward
        {
            get => _driveForward;
            set
            {
                _driveForward = value;
                _driveBackward = !value;

                ObjectItem.ChangeDirection(!value);

                if (_objectItem != null)
                    _objectItem.Speed = 0;

                UpdateUi();
            }
        }

        public bool DriveBackward
        {
            get => _driveBackward;
            set
            {
                _driveBackward = value;
                _driveForward = !value;

                ObjectItem.ChangeDirection(value);

                if (_objectItem != null)
                    _objectItem.Speed = 0;

                UpdateUi();
            }
        }

        #endregion

        #region Locomotive Image

        public ImageSource LocomotiveImage
        {
            get
            {
                try
                {
                    return ImageHelper.Base64ToImageSource(ObjectItem.LocomotiveImageBase64);
                }
                catch
                {
                    return null;
                }
            }
        }

        public ImageSource LocomotiveIconImage
        {
            get
            {
                try
                {
                    var img = ImageHelper.Base64ToImageSource(ObjectItem.LocomotiveImageBase64);
                    if (img != null)
                        return img;

                    var imgIcon = new BitmapImage(new Uri(@"pack://application:,,,/RailwayEssential;component/Resources/Main.ico"));
                    return imgIcon;
                }
                catch
                {
                    return null;
                }
            }
        }

        #endregion

        public void UpdateUi()
        {
            //Trace.WriteLine(" *** UpdateUi() of Locomotive *** ");

            if (_objectItem == null)
            {
                IsEnabled = false;

                return;
            }

            IsEnabled = true;

            if (ObjectItem != null && ObjectItem.Direction == 1)
            {
                _driveBackward = true;
                _driveForward = false;
            }
            else
            {
                _driveBackward = false;
                _driveForward = true;
            }

            if (ObjectItem != null)
            {
                // ...
            }

            RaisePropertyChanged("DriveForward");
            RaisePropertyChanged("DriveBackward");
            RaisePropertyChanged("LocomotiveImage");
            RaisePropertyChanged("LocomotiveIconImage");

            if (ObjectItem != null)
            {
                ObjectItem.RaisePropertyChange("Speed");
                ObjectItem.RaisePropertyChange("ObjectItem.Speed");
                ObjectItem.RaisePropertyChange("ObjectItem.MaxSpeedPercentage");
                ObjectItem.RaisePropertyChange("ObjectItem.BlockSpeedPercentage");
                ObjectItem.UpdateTitle();
                ObjectItem.UpdateSubTitle();
            }

            Model?.UpdateTrackUi();
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            var hander = Changed;
            hander?.Invoke(this, EventArgs.Empty);
        }
    }
}
