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
using System.Threading.Tasks;

namespace RailwayEssentialMdi.ViewModels
{
    using System.Diagnostics;
    using RailwayEssentialCore;
    using DataObjects;
    using Commands;
    using Entities;

    public class TrackWindow : BaseWindow, ITrackWindow
    {
        public event EventHandler Loaded;

        public object TrackView { get; set; }
        public ProjectTrackView ProjectTrackView { get; set; }
        public ITrackViewerZoom TrackViewZoomer { get; set; }
        private readonly TrackEntity _entity;
        public TrackEntity Entity => _entity;
        public override string Name => ProjectTrackView.Name;
        
        public RelayCommand SaveCommand { get; }

        public RelayCommand ZoomResetCommand { get; }
        public RelayCommand ZoomPlusCommand { get; }
        public RelayCommand ZoomMinusCommand { get; }
        public RelayCommand EditCommand { get; }

        public RelayCommand PlusColumRightCommand { get; }
        public RelayCommand MinusColumnRightCommand { get; }
        public RelayCommand MinusRowBottomCommand { get; }
        public RelayCommand PlusRowBottomCommand { get; }
        
        public TrackWindow(TrackEntity entity, ProjectTrackView trackView, TaskCompletionSource<bool> tcs)
        {
            _entity = entity;

            ProjectTrackView = trackView;

            PlusColumRightCommand = new RelayCommand(PlusColumRight);
            MinusColumnRightCommand = new RelayCommand(MinusColumnRight);
            PlusRowBottomCommand = new RelayCommand(PlusRowBottom);
            MinusRowBottomCommand = new RelayCommand(MinusRowBottom);

            ZoomResetCommand = new RelayCommand(ZoomReset);
            ZoomPlusCommand = new RelayCommand(ZoomPlus);
            ZoomMinusCommand = new RelayCommand(ZoomMinus);
            EditCommand = new RelayCommand(EditState, CheckEditState);

            SaveCommand = new RelayCommand(Save);

            RaisePropertyChanged("BlockEventNames");
        }

        private void Save(object p)
        {
            if (_entity == null)
                return;

            _entity.ApplyAssignment();
        }

        private void PlusColumRight(object p)
        {
            Trace.WriteLine("+ Column");
        }

        private void MinusColumnRight(object p)
        {
            Trace.WriteLine("- Column");
        }

        private void PlusRowBottom(object p)
        {
            Trace.WriteLine("+ Row");
        }

        private void MinusRowBottom(object p)
        {
            Trace.WriteLine("- Row");
        }

        private void ZoomReset(object p)
        {
            if (TrackViewZoomer == null)
                return;

            TrackViewZoomer.ZoomLevel = 0.0;
        }

        private void ZoomPlus(object p)
        {
            if (TrackViewZoomer == null)
                return;

            var v = TrackViewZoomer.ZoomLevel;
            v += TrackViewZoomer.ZoomLevelIncrement;
            TrackViewZoomer.ZoomLevel = v;
        }

        private void ZoomMinus(object p)
        {
            if (TrackViewZoomer == null)
                return;

            var v = TrackViewZoomer.ZoomLevel;
            v -= TrackViewZoomer.ZoomLevelIncrement;
            TrackViewZoomer.ZoomLevel = v;
        }
        
        private void EditState(object p)
        {
            if(Entity.IsEditEnabled)
                Entity.DisableEdit();
            else
                Entity.EnableEdit();
        }

        private bool CheckEditState(object p)
        {
            if (_entity == null)
                return false;

            if (_entity.Dispatcher == null)
                return false;

            var m = _entity.Dispatcher.Model as RailwayEssentialModel;
            if (m == null)
                return false;

            if (m.IsDryRun)
                return true;

            if (!_entity.Dispatcher.GetRunMode())
                return false;

            return true;
        }

        #region ITrackWindow

        public void ViewerReady()
        {
            _entity?.ViewerReady();

            Loaded?.Invoke(this, null);
        }

        public void PromoteViewer(ITrackViewer trackViewer)
        {
            _entity?.PromoteViewer(trackViewer);
        }

        #endregion
    }
}
