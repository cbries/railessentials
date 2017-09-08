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
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RailwayEssentialCore;
using RailwayEssentialMdi.DataObjects;
using RailwayEssentialMdi.ViewModels;
using RailwayEssentialWeb;
using TrackWeaver;
using Switch = TrackInformation.Switch;

namespace RailwayEssentialMdi.Entities
{
    public partial class TrackEntity : Bases.ViewModelBase, IPersist
    {
        public event EventHandler Changed;

        public const string ToolContentId = "TrackEntityTool";

        private bool _initialized;
        private string _tmpTrackName;
        private Theme.Theme _theme;
        private Dispatcher.Dispatcher _dispatcher;
        private ITrackViewer _trackViewer;
        private TrackPlanParser.Track _track;
        private WebGenerator _webGenerator;
        private string _trackname = "";

        public bool IsClone { get; private set; }

        public object Window { get; set; }

        public string TrackObjectFilepath { get; set; }

        internal SynchronizationContext Ctx { get; set; }

        public ProjectTrack ProjectTrack { get; set; }

        internal ITrackViewer Viewer => _trackViewer;

        internal Dispatcher.Dispatcher Dispatcher => _dispatcher;

        #region Name

        public string Name
        {
            get
            {
                if (ProjectTrack == null)
                    return "-";

                return ProjectTrack.Name;
            }

            set
            {
                if (ProjectTrack != null)
                    ProjectTrack.Name = value;
                RaisePropertyChanged("Name");
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

        #region CanClose

        private bool _canClose = false;
        public bool CanClose
        {
            get
            {
                return _canClose;
            }
            set
            {
                if (_canClose != value)
                {
                    _canClose = value;
                    RaisePropertyChanged("CanClose");
                    RaisePropertyChanged("CanEdit");
                }
            }
        }

        #endregion

        #region CanEdit

        public bool CanEdit
        {
            get
            {
                return !_canClose;
            }
        }

        #endregion

        #region edit mode buttons



        private readonly bool[] _btnModes = new[] { true, false, false, false, false };

        internal void RaiseModeBlah()
        {
            RaisePropertyChanged("BtnModeAddIsChecked");
            RaisePropertyChanged("BtnModeMoveIsChecked");
            RaisePropertyChanged("BtnModeRotateIsChecked");
            RaisePropertyChanged("BtnModeRemoveIsChecked");
            RaisePropertyChanged("BtnModeObjectIsChecked");
        }

        private void ResetModeBlah()
        {
            for (int i = 0; i < _btnModes.Length; ++i)
                _btnModes[i] = false;
        }

        public bool BtnModeAddIsChecked
        {
            get => _btnModes[0];
            set
            {
                ResetModeBlah();
                _btnModes[0] = value;
                RaiseModeBlah();
            }
        }

        public bool BtnModeMoveIsChecked
        {
            get => _btnModes[1];
            set
            {
                ResetModeBlah();
                _btnModes[1] = value;
                RaiseModeBlah();
            }
        }

        public bool BtnModeRotateIsChecked
        {
            get => _btnModes[2];
            set
            {
                ResetModeBlah();
                _btnModes[2] = value;
                RaiseModeBlah();
            }
        }

        public bool BtnModeRemoveIsChecked
        {
            get => _btnModes[3];
            set
            {
                ResetModeBlah();
                _btnModes[3] = value;
                RaiseModeBlah();
            }
        }

        public bool BtnModeObjectIsChecked
        {
            get => _btnModes[4];
            set
            {
                ResetModeBlah();
                _btnModes[4] = value;
                RaiseModeBlah();
            }
        }


        #endregion

        public bool IsEditEnabled { get; private set; }

        public TrackEntity Clone()
        {
            var e = new TrackEntity(_dispatcher)
            {
                TrackObjectFilepath = TrackObjectFilepath,
                Theme = _theme,
                Ctx = Ctx,
                ProjectTrack = ProjectTrack,
                Cfg = Cfg,
                Model = Model,
                IsClone = true,
                _trackname = _trackname
            };

            e.Initialize();

            return e;
        }

        public void DisableEdit()
        {
            Model?.ExecuteJs("changeEditMode(false);");
            IsEditEnabled = false;
            RaisePropertyChanged("CanClose");
            RaisePropertyChanged("CanEdit");
            RaisePropertyChanged("IsEditEnabled");
        }

        public void EnableEdit()
        {
            Model?.ExecuteJs("changeEditMode(true);", this);
            IsEditEnabled = true;
            RaisePropertyChanged("CanClose");
            RaisePropertyChanged("CanEdit");
            RaisePropertyChanged("IsEditEnabled");
        }

        public Theme.Theme Theme
        {
            get => _theme;
            set
            {
                _theme = value;
                RaisePropertyChanged("Theme");
            }
        }

        public string TrackEditor { get; private set; }

        public TrackPlanParser.Track Track => _track;

        public IWebGenerator WebGenerator { get; set; }

        public Configuration Cfg { get; set; }

        public TrackEntity(Dispatcher.Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            ContentId = ToolContentId;
            TrackEditor = Utils.TrackplansEditor.ExpandRailwayEssential();
        }

        public bool Initialize()
        {
            if (_initialized)
                return true;

            _initialized = true;

            TrackPlanParser.TrackPlanParser parser = new TrackPlanParser.TrackPlanParser(TrackObjectFilepath);

            parser.Parse();

            _track = parser.Track;

            if (string.IsNullOrEmpty(_trackname))
                _trackname = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + "_track.html";

            _tmpTrackName = _trackname;
            _tmpTrackName = Path.Combine(Utils.TrackplansEditor, _tmpTrackName);
            _tmpTrackName = _tmpTrackName.ExpandRailwayEssential();

            _webGenerator = new WebGenerator(_theme)
            {
                Columns = Cfg.DesignerColumns,
                Rows = Cfg.DesignerRows
            };

            GeneratePhysicalTrackViewerUi();

            return true;
        }

        public bool ViewerReady()
        {
            if (_trackViewer != null && _trackViewer.JsCallback != null)
            {
                _trackViewer.JsCallback.CellEdited += JsCallbackOnCellEdited;
                _trackViewer.JsCallback.CellClicked += JsCallbackOnCellClicked;
                _trackViewer.JsCallback.CellSelected += JsCallbackOnCellSelected;
                _trackViewer.JsCallback.EditModeChanged += JsCallbackOnEditModeChanged;
                _trackViewer.JsCallback.TrackEdit = _track;
            }

            JArray arClicks = new JArray();

            // load current track
            foreach (var item in _track)
            {
                if (item == null)
                    continue;

                var themeItem = _theme.Get(item.ThemeId);
                if (themeItem != null)
                {
                    var symbol = Path.GetFileNameWithoutExtension(themeItem.Off.Default);
                    var orientation = item.Orientation;

                    JObject o = new JObject
                    {
                        ["col"] = item.X,
                        ["row"] = item.Y,
                        ["themeId"] = item.ThemeId,
                        ["symbol"] = symbol,
                        ["orientation"] = orientation
                    };

                    arClicks.Add(o);
                }
            }

            Model?.ExecuteJs($"simulateClick2({arClicks.ToString(Formatting.None)});");

            return true;
        }

        public void PromoteViewer(ITrackViewer trackViewer)
        {
            _trackViewer = trackViewer;
            _trackViewer.SetUrl(_tmpTrackName.Replace(" ", "%20"));
        }

        private void JsCallbackOnEditModeChanged(object o, bool editState)
        {
            if (!editState)
            {
                ShowObjectEdit = false;
            }
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            var hander = Changed;
            hander?.Invoke(this, EventArgs.Empty);
        }

        private void GeneratePhysicalTrackViewerUi()
        {
            if (_webGenerator == null)
                return;

            _webGenerator.Generate(_tmpTrackName);
        }
        
        public bool UpdateTrackViewerUi(TrackWeaver.TrackWeaver weaver)
        {
            var sw = StopWatch.Create();

            try
            {
                if (weaver == null)
                    return false;

                var ws = weaver.WovenSeam;
                if (ws == null)
                    return false;

                foreach (var seam in ws)
                {
                    if (seam == null)
                        continue;

                    if (seam.TrackObjects.Count == 0)
                        continue;

                    foreach (var trackItem in seam.TrackObjects.Keys)
                    {
                        if (trackItem == null)
                            continue;

                        var checkState = seam.TrackObjects[trackItem];

                        TrackCheckResult checkResult = null;
                        if (checkState != null)
                            checkResult = checkState();

                        var x = trackItem.X;
                        var y = trackItem.Y;
                        var orientation = trackItem.Orientation;

                        int themeId = trackItem.ThemeId;
                        var themeObject = _theme.Get(themeId);
                        if (themeObject == null)
                            return false;
                        string symbol = "";

                        switch (seam.ObjectItem.TypeId())
                        {
                            case TrackInformation.Locomotive.Typeid:
                                continue;

                            case TrackInformation.Ecos2.Typeid:
                                continue;

                            case TrackInformation.Route.Typeid:
                                continue;

                            case TrackInformation.S88.Typeid:
                                {
                                    bool rS88 = checkResult?.State != null && checkResult.State.Value;

                                    bool partOfBusyRoute = false;

                                    var m = Model as RailwayEssentialModel;
                                    var blockRoutes = m.Project.BlockRoutes;
                                    foreach (var r in blockRoutes)
                                    {
                                        if (r.IsBusy)
                                        {
                                            foreach (var wp in r)
                                            {
                                                if (wp.X == x && wp.Y == y)
                                                {
                                                    partOfBusyRoute = true;
                                                    goto afterForeach;
                                                }
                                            }
                                        }
                                    }
                                    afterForeach:

                                    if (rS88)
                                    {
                                        if (partOfBusyRoute)
                                            symbol = themeObject.Active.Route;
                                        else
                                            symbol = themeObject.Active.Occ;
                                    }
                                    else
                                    {
                                        if (partOfBusyRoute)
                                            symbol = themeObject.Off.Route;
                                        else
                                            symbol = themeObject.Off.Default;
                                    }
                                }
                                break;

                            case TrackInformation.Switch.Typeid:
                                {
                                    if (checkResult != null && checkResult.Direction.HasValue)
                                    {
                                        var direction = checkResult.Direction.Value;

                                        if (seam.ObjectItem is Switch objS && objS.InvertCommand)
                                        {
                                            if (direction == TrackCheckResult.SwitchDirection.Straight)
                                                direction = TrackCheckResult.SwitchDirection.Turn;
                                            else
                                                direction = TrackCheckResult.SwitchDirection.Straight;
                                        }

                                        if (direction == TrackCheckResult.SwitchDirection.Straight)
                                        {
                                            //if (seam.ObjectItem.IsRouted)
                                            //    symbol = themeObject.Active.Route;
                                            //else
                                                symbol = themeObject.Active.Default;
                                        }
                                        else if (direction == TrackCheckResult.SwitchDirection.Turn)
                                        {
                                            //if (seam.ObjectItem.IsRouted)
                                            //    symbol = themeObject.Off.Route;
                                            //else
                                                symbol = themeObject.Off.Default;
                                        }
                                        else
                                        {
                                            Trace.WriteLine("<Switch> Unknown direction: " + direction);
                                        }
                                    }
                                }
                                break;

                            default:
                                break;
                        }

                        if (_trackViewer != null && _trackViewer.JsCallback != null)
                        {
                            _trackViewer.JsCallback.TrackEdit.ChangeSymbol(x, y, themeId);
                            Model?.ExecuteJs($"changeSymbol({x}, {y}, {themeId}, \"{orientation}\", \"{symbol}\");");
                        }

                        //Trace.WriteLine($"CHANGE: {x},{y} -> {themeId} | {symbol} | {orientation}");
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                sw.Stop();
                //sw.Show("UpdateUi()");
            }

            return true;
        }

        #region IPersist

        public bool Save()
        {
            return Save(TrackObjectFilepath);
        }

        public bool Save(string targetFilepath)
        {
            if (_track == null)
                return false;

            try
            {
                var trackObject = Track.GetJson();
                if (trackObject != null)
                    File.WriteAllText(targetFilepath, trackObject.ToString(Formatting.Indented));

                return true;
            }
            catch (Exception ex)
            {
                var logger = _dispatcher.Logger;
                if (logger != null)
                    logger.Log("<TrackEntity> " + ex.Message + "\r\n");

                return false;
            }
        }

        #endregion
    }
}
