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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecos2Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RailwayEssentialCore;
using RailwayEssentialMdi.Analyze;
using RailwayEssentialMdi.Bases;
using RailwayEssentialMdi.Commands;
using RailwayEssentialMdi.DataObjects;
using RailwayEssentialMdi.Entities;
using RailwayEssentialMdi.Interfaces;
using TrackInformation;
using Switch = TrackInformation.Switch;

namespace RailwayEssentialMdi.ViewModels
{
    public class RailwayEssentialModel : ViewModelBase, IRailwayEssentialModel, ILogging
    {
        public ObservableCollection<IContent> Windows { get; private set; }

        public IMainView MainView { get; set; }

        private Random _random = new Random(DateTime.Now.Millisecond);
        private ProjectFile _project;

        public ProjectFile Project
        {
            get => _project;

            set
            {
                _project = value;
                RaisePropertyChanged("Project");
            }
        }

        private readonly Theme.Theme _theme;
        private readonly Configuration _cfg;
        private Dispatcher.Dispatcher _dispatcher;
        private Autoplay.Autoplay _autoplayer = null;

        internal readonly SynchronizationContext _ctx = null;

        internal SynchronizationContext UiSyncCtx => _ctx;

        public Dispatcher.Dispatcher Dispatcher => _dispatcher;

        public Theme.Theme Theme => _theme;

        private readonly Category _itemStatus = new Category { Index = 0, Title = "Status", IconName = "cat_status.png" };
        private readonly Category _itemLocomotives = new Category { Index = 1, Title = "Locomotives", IconName = "cat_locomotive.png" };
        private readonly Category _itemS88 = new Category { Index = 2, Title = "S88 Ports", IconName = "cat_s88.png" };
        private readonly Category _itemSwitches = new Category { Index = 3, Title = "Switches", IconName = "cat_switch.png" };
        private readonly Category _itemRoutes = new Category { Index = 4, Title = "Routes", IconName = "cat_route.png" };
        private readonly Category _itemBlockRoutes = new Category { Index = 5, Title = "Block Routes", IconName = "cat_blockroutes.png" };

        private ObservableCollection<Item> _rootItems = new ObservableCollection<Item>();

        public ObservableCollection<Item> RootItems
        {
            get => _rootItems;
            set
            {
                _rootItems = value;
                RaisePropertyChanged("RootItems");
            }
        }

        internal void RemoveItemLocomotiveFromCategory(TrackInformation.Item obj)
        {
            if (obj == null)
                return;

            _itemLocomotives.Items.Remove(obj);
        }

        public void Log(string text, params object[] args)
        {
            _logMessagesGeneral?.Add(text, args);
        }

        public void LogError(string msg, params object[] args)
        {
            _ctx.Send(state =>
            {
                Xceed.Wpf.Toolkit.MessageBox.Show(string.Format(msg, args), "Error", MessageBoxButton.OK);
            }, new object());
        }

        public void LogNetwork(string text, params object[] args)
        {
            _logMessagesCommands?.Add(text, args);
        }

        public void LogAutoplay(string text, params object[] args)
        {
            _logMessagesAutoplay?.Add(text, args);
        }

        private bool _isDirty;

        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                _isDirty = value;
                RaisePropertyChanged("IsDirty");
                RaisePropertyChanged("IsDirtyMessage");
            }
        }

        public string IsDirtyMessage
        {
            get
            {
                if (IsDirty)
                    return "*";
                return "";
            }
        }

        public ImageSource ConnectionStateIcon
        {
            get
            {
                if(IsDryRun)
                    return new BitmapImage(new Uri("/RailwayEssential;component/Resources/dryrun.png", UriKind.Relative));
                if (_dispatcher == null || _cfg == null)
                {
                    _itemStatus.Items.Clear();
                    return new BitmapImage(new Uri("/RailwayEssential;component/Resources/offline.png", UriKind.Relative));
                }
                if (!_dispatcher.GetRunMode())
                {
                    _itemStatus.Items.Clear();
                    return new BitmapImage(new Uri("/RailwayEssential;component/Resources/offline.png", UriKind.Relative));
                }
                return new BitmapImage(new Uri("/RailwayEssential;component/Resources/online.png", UriKind.Relative));
            }
        }

        public string ConnectionState
        {
            get
            {
                if (IsDryRun)
                    return "DRY RUN";
                if (_dispatcher == null || _cfg == null)
                    return "No connection";
                if (!_dispatcher.GetRunMode())
                    return "No connection";
                return $"{_cfg.IpAddress}:{_cfg.Port}";
            }
        }

        public ImageSource AutoplayStateIcon
        {
            get
            {
                if (_autoplayer == null)
                    return new BitmapImage(new Uri("/RailwayEssential;component/Resources/offline.png", UriKind.Relative));
                return new BitmapImage(new Uri("/RailwayEssential;component/Resources/autoplayon.gif", UriKind.Relative));
            }
        }

        public string AutoplayState
        {
            get
            {
                if (_autoplayer == null)
                    return "Autoplay OFF";
                return "Autoplay ON";
            }
        }

        public bool AutoplayState2 => _autoplayer != null;
        public bool AutoplayState3 => !AutoplayState2;

        internal Autoplay.Autoplay Autoplayer => _autoplayer;

        private void UpdateCanClose()
        {
            var wnds = GetWindowList<TrackWindow>();
            if (wnds.Count > 0)
                wnds[0].Entity.CanClose = false;
            for (int i = 1; i < wnds.Count; ++i)
                wnds[i].Entity.CanClose = true;
        }

        private List<string> _recentProjects = new List<string>();
        
        public IList<string> RecentProjects
        {
            get => _recentProjects;
            set
            {
                _recentProjects = (List<string>) value;
                RaisePropertyChanged("RecentProjects");
            }
        }

        public RelayCommand NewProjectCommand { get; }
        public RelayCommand OpenCommand { get; }
        public RelayCommand OpenRecentCommand { get; }
        public RelayCommand CloseCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand ExitCommand { get; }

        public System.Windows.Input.ICommand AssignToBlockCommand { get; }

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        public RelayCommand DryRunCommand { get;  }
        public RelayCommand TogglePowerCommand { get; }
        public RelayCommand AutoplayCommand { get; }
        public RelayCommand StopAllLocsCmd { get; }
        public RelayCommand RestartAllLocsCmd { get;  }
        public RelayCommand CmdStationsPropertiesCommand { get; }

        public RelayCommand ShowLogCommand { get; }
        public RelayCommand ShowCommandLogCommand { get; }
        public RelayCommand ShowAutoplayLogCommand { get; }
        public RelayCommand ShowIdentifiersCommand { get; }

        public RelayCommand AboutCommand { get; }

        public RelayCommand AnalyzeRoutesCommand { get; }
        public RelayCommand AnalyzeCleanCommand { get; }

        public RelayCommand LoadLayoutCommand { get; }
        public RelayCommand SaveLayoutCommand { get; }

        public RelayCommand AddTrackCommand { get; }
        public RelayCommand RemoveTrackCommand { get; }
        public RelayCommand OpenProjectDirectoryCommand { get; }

        private readonly LogEntity _logMessagesGeneral = new LogEntity();
        private readonly LogEntity _logMessagesCommands = new LogEntity();
        private readonly LogEntity _logMessagesAutoplay = new LogEntity();
        private TrackEntity _trackEntity = null;

        internal TrackEntity TrackEntity => _trackEntity;

        public RailwayEssentialModel()
        {
            Windows = new ObservableCollection<IContent>();

            Registry.Registry registry = new Registry.Registry();
            RecentProjects = registry.RecentProjects;

            IsVisualLabelActivated = false;
            _ctx = SynchronizationContext.Current;
            _cfg = new Configuration();

            NewProjectCommand = new RelayCommand(NewProject, CheckNewProject);
            OpenCommand = new RelayCommand(Open, CheckOpen);
            OpenRecentCommand = new RelayCommand(OpenRecent, CheckOpenRecent);
            CloseCommand = new RelayCommand(Close, CheckClose);
            SaveCommand = new RelayCommand(Save, CheckSave);
            ExitCommand = new RelayCommand(Exit, CheckExit);
            ConnectCommand = new RelayCommand(ConnectToCommandStation, CheckConnectToCommandStation);
            DisconnectCommand = new RelayCommand(DisconnectFromCommandStation, CheckDisconnectFromCommandStation);
            DryRunCommand = new RelayCommand(DryRun, CheckDryRun);
            TogglePowerCommand = new RelayCommand(TogglePower, CheckTogglePower);
            AutoplayCommand = new RelayCommand(DoAutoplay, CheckDoAutoplay);
            StopAllLocsCmd = new RelayCommand(DoStopAllLocsCmd, CheckStopAllLocsCmd);
            RestartAllLocsCmd = new RelayCommand(DoRestartAllLocsCmd, CheckRestartAllLocsCmd);
            CmdStationsPropertiesCommand = new RelayCommand(PropertiesCommandStation);
            ShowLogCommand = new RelayCommand(ShowLog);
            ShowCommandLogCommand = new RelayCommand(ShowCommandLog);
            ShowAutoplayLogCommand = new RelayCommand(ShowAutoplayLog);
            ShowIdentifiersCommand = new RelayCommand(ShowIdentifiers, CheckShowIdentifiers);
            AboutCommand = new RelayCommand(ShowAbout);
            AnalyzeRoutesCommand = new RelayCommand(AnalyzeRoutes, CheckAnalyzeRoutes);
            AnalyzeCleanCommand = new RelayCommand(AnalyzeClean, CheckAnalyzeClean);
            LoadLayoutCommand = new RelayCommand(LoadLayout);
            SaveLayoutCommand = new RelayCommand(SaveLayout);
            AddTrackCommand = new RelayCommand(AddTrack, CheckAddTrack);
            RemoveTrackCommand = new RelayCommand(RemoveTrack, CheckRemoveTrack);
            OpenProjectDirectoryCommand = new RelayCommand(OpenProjectDirectory, CheckOpenProjectDirectory);

            // TEST
            //int _counter = 0;
            //new Thread(() =>
            //{
            //    for (;;)
            //    {
            //        if (_logMessagesGeneral != null)
            //        {
            //            _logMessagesGeneral.Add("Message: {0}\r\n", _counter);

            //            if (_itemStatus != null)
            //            {
            //                if (_ctx != null)
            //                {
            //                    _ctx.Send(state =>
            //                    {
            //                        // ...
            //                    }, new object());
            //                }
            //            }

            //            ++_counter;

            //            Thread.Sleep(1000);
            //        }
            //    }
            //})
            //{ IsBackground = true }.Start();

            _theme = new Theme.Theme();
            var themePath = Utils.ThemeName.ExpandRailwayEssential();
            if (!themePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                themePath += ".json";
            if (!_theme.Load(themePath))
            {
                Trace.WriteLine("<Theme> Loading of theme failed: " + themePath);
                Log("<Theme> Loading of theme failed: " + themePath + "\r\n");
            }
        }

        public void NewProject(object p)
        {
            var tmpPrjName = "Project{0}\\Project{0}.railwayprj".GenerateUniqueName("Projects\\".ExpandRailwayEssential());

            var dname = Path.GetDirectoryName(tmpPrjName);
            if (!Directory.Exists(dname))
                dname = Path.GetDirectoryName(dname);

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Path.GetFileName(tmpPrjName),
                InitialDirectory = dname,
                DefaultExt = ".railwayprj",
                Filter = "RailwayEssential (.railwayprj)|*.railwayprj"
            };

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                var fname = dlg.FileName;
                var dirname = Path.GetDirectoryName(fname);
                var name = Path.GetFileNameWithoutExtension(fname);

                try
                {
                    Directory.CreateDirectory(Path.Combine(dirname, name));
                    fname = Path.Combine(dirname, name);
                    fname = Path.Combine(fname, name + ".railwayprj");
                    var o = new JObject
                    {
                        ["name"] = name,
                        ["version"] = 1.0,
#if DEBUG
                        ["targetHost"] = "192.168.178.61",
#else
                        ["targetHost"] = "127.0.0.1",
#endif
                        ["targetPort"] = 15471,
                        ["designerColumns"] = 40,
                        ["designerRows"] = 40,
                        ["objects"] = new JArray() {"TrackObjects.json"},
                        ["track"] = new JObject(),
                        ["trackViews"] = new JArray()
                    };

                    var o0 = o["track"] as JObject;
                    if (o0 != null)
                    {
                        o0["name"] = name;
                        o0["path"] = "TrackPlan.json";
                        o0["weave"] = "TrackWeaving.json";
                    }

                    if (o["trackViews"] is JArray o1)
                    {
                        var oo = new JObject
                        {
                            ["name"] = "S#1",
                            ["startX"] = 0,
                            ["startY"] = 0,
                            ["show"] = true
                        };
                        o1.Add(oo);
                    }

                    var targetDirectory = Path.GetDirectoryName(fname);

                    if(string.IsNullOrEmpty(targetDirectory) || o0 == null)
                        throw new Exception("Project creation failed.");

                    // generate default files
                    var fname0 = Path.Combine(targetDirectory, o0["path"].ToString());
                    File.WriteAllText(fname0, new JArray().ToString(Formatting.Indented));
                    var fname1 = Path.Combine(targetDirectory, o0["weave"].ToString());
                    File.WriteAllText(fname1, new JArray().ToString(Formatting.Indented));

                    File.WriteAllText(
                        Path.Combine(targetDirectory, "TrackObjects.json"),
                        new JObject().ToString(Formatting.Indented), Encoding.UTF8);

                    File.WriteAllText(fname, o.ToString(Formatting.Indented), Encoding.UTF8);

                    Project = new ProjectFile();
                    Project.Load(fname);
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    AfterOpen();
                }
            }
        }

        public void Open(object p)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = false,
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"Testprojekte\"),
                DefaultExt = ".railwayprj",
                Filter = "RailwayEssential Project (.railwayprj)|*.railwayprj"
            };

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;

                try
                {
                    if (Project != null)
                    {
                        if (CloseCommand.CanExecute(null))
                            CloseCommand.Execute(null);
                    }

                    var prj = new ProjectFile();
                    if (!prj.Load(filename))
                        Log("Project load failed: " + prj.Filepath + "\r\n");
                    else
                    {
                        Project = prj;
                        Log("Project opened: " + prj.Name + "\r\n");
                    }
                }
                catch
                {
                    // ignore
                }
            }

            AfterOpen();
        }

        public void OpenRecent(object p)
        {
            string pp = p as string;
            if (string.IsNullOrEmpty(pp) || !Directory.Exists(pp))
            {
                LogError($"Project vanished: {0}");
                return;
            }

            string name = Path.GetFileName(pp);
            string path = Path.Combine(pp, name + ".railwayprj");
            if (!File.Exists(path))
            {
                LogError($"Project file is missing: {path}");
                return;
            }

            try
            {
                if (Project != null)
                {
                    if (CloseCommand.CanExecute(null))
                        CloseCommand.Execute(null);
                }

                var prj = new ProjectFile();
                if (!prj.Load(path))
                    Log("Project load failed: " + prj.Filepath + "\r\n");
                else
                {
                    Project = prj;
                    Log("Project opened: " + prj.Name + "\r\n");
                }
            }
            catch(Exception ex)
            {
                LogError($"Open failed: {ex.Message}");
                return;
            }

            AfterOpen();
        }

        private async void AfterOpen()
        {
            if (_project == null)
                return;

            string p4recent = _project.Filepath;
            if (!string.IsNullOrEmpty(p4recent))
            {
                var p = Path.GetDirectoryName(p4recent);
                if (Directory.Exists(p))
                {
                    Registry.Registry registry = new Registry.Registry();
                    var recentProjects = registry.RecentProjects;
                    if (recentProjects.Count > 0)
                    {
                        if (!recentProjects[0].Equals(p, StringComparison.OrdinalIgnoreCase))
                        {
                            recentProjects.Insert(0, p);
                        }
                    }
                    else
                    {
                        recentProjects.Insert(0, p);
                    }

                    registry.SetRecent(recentProjects);

                    RecentProjects = recentProjects;
                }
            }

            if(Windows == null)
                Windows = new ObservableCollection<IContent>();

            RootItems.Add(_itemStatus);
            RootItems.Add(_itemLocomotives);
            RootItems.Add(_itemS88);
            RootItems.Add(_itemSwitches);
            RootItems.Add(_itemRoutes);
            RootItems.Add(_itemBlockRoutes);

            _cfg.IpAddress = Project.TargetHost;
            _cfg.Port = Project.TargetPort;
            _cfg.DesignerColumns = Project.DesignerColumns;
            _cfg.DesignerRows = Project.DesignerRows;

            _dispatcher = new Dispatcher.Dispatcher(_cfg)
            {
                Configuration = _cfg,
                Model = this,
                Logger = this
            };

            var prjTrack = Project.Track;

            _trackEntity = new TrackEntity(_dispatcher)
            {
                TrackObjectFilepath = Path.Combine(_project.Dirpath, prjTrack.Path),
                Theme = _theme,
                Ctx = _ctx,
                ProjectTrack = prjTrack,
                Cfg = _cfg,
                Model = this
            };

            _trackEntity.Initialize();

            int numberOfViewsCreated = 0;

            foreach (var view in Project.TrackViews)
            {
                if (view == null)
                    continue;

                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

                TrackWindow item = null;

                if (numberOfViewsCreated == 0)
                {
                    if (view.Show)
                    {
                        item = new TrackWindow(_trackEntity, view, tcs);
                        item.Loaded += (s, ev) =>
                        {
                            try
                            {
                                tcs.SetResult(true);
                            }
                            catch
                            {
                                // ignore
                            }
                        };
                        item.Closing += (s, ev) => Windows.Remove(item);
                        Windows.Add(item);
                        ++numberOfViewsCreated;
                    }
                }
                else
                {
                    var trackEntityClone = _trackEntity.Clone();
                    item = new TrackWindow(trackEntityClone, view, tcs);
                    item.Loaded += (s, ev) =>
                    {
                        try
                        {
                            tcs.SetResult(true);
                        }
                        catch
                        {
                            // ignore
                        }
                    };
                    item.Closing += (s, ev) =>
                    {
                        var v = item.ProjectTrackView;
                        if (v != null)
                            Project.TrackViews.Remove(v);
                        Windows.Remove(item);
                    };
                    Windows.Add(item);
                    ++numberOfViewsCreated;
                }

                if (item != null)
                {
                    item.Entity.IsActive = true;

                    await tcs.Task;
                }
            }
            UpdateCanClose();

            _dispatcher.UpdateUi += DispatcherOnUpdateUi;
            _dispatcher.ReadyToPlay += DispatcherOnReadyToPlay;

            var dataProvider = _dispatcher.GetDataProvider();
            dataProvider.DataChanged += OnDataChanged;
            dataProvider.CommandsReady += DataProviderOnCommandsReady;

            foreach (var objFilename in Project.Objects)
            {
                string absolutePath = Path.Combine(Project.Dirpath, objFilename);
                if (!File.Exists(absolutePath))
                    continue;

                dataProvider.LoadObjects(absolutePath);
            }

            //if (MainView != null)
            //    MainView.LoadLayout();

            UpdateTrackUi();

            UpdateBlockRouteItems();
            UpdateCanClose();            
        }

        public async void AddTrack(object p)
        {
            var trackEntityClone = _trackEntity.Clone();

            var currentViews = Project.TrackViews;
            List<string> currentViewNames = new List<string>();
            foreach (var vv in currentViews)
                currentViewNames.Add(vv.Name);
            string newViewName = null;
            for (int i = 1; i < 1000; ++i)
            {
                var testname = $"TrackView{i}";
                if (!currentViewNames.Contains(testname))
                {
                    newViewName = testname;
                    break;
                }
            }

            var view = new ProjectTrackView
            {
                Name = !string.IsNullOrEmpty(newViewName) ? newViewName : "Unnamed",
                Show = true,
                StartX = 0,
                StartY = 0
            };

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            var item = new TrackWindow(trackEntityClone, view, tcs);
            item.Loaded += (s, ev) =>
            {
                try
                {
                    tcs.SetResult(true);

                    UpdateTrackUi();
                }
                catch
                {
                    // ignore
                }
            };
            item.Closing += (s, ev) =>
            {
                var v = item.ProjectTrackView;
                if (v != null)
                    Project.TrackViews.Remove(v);

                Windows.Remove(item);
            };
            lock (Windows) { Windows.Add(item); }
            item.Entity.IsSelected = true;
            item.Entity.IsActive = true;
            await tcs.Task;
            Project.TrackViews.Add(view);
            UpdateCanClose();
        }

        public void RemoveTrack(object p)
        {
            // TODO
        }

        public void UpdateBlockRouteItems()
        {
            if (_itemBlockRoutes == null)
                return;

            SetDirty(true);

            _itemBlockRoutes.Items.Clear();

            if (Project == null || Project.BlockRoutes == null)
                return;

            int n = Project.BlockRoutes.Count;

            for(int i=0; i < n; ++i)
            {
                var route = Project.BlockRoutes[i];
                if (route == null)
                    continue;

                if (route.Count < 2)
                    continue;

                var firstItem = route[0];
                var lastItem = route[route.Count - 1];

                var firstCoord = $"{firstItem.X},{firstItem.Y}";
                var lastCoord = $"{lastItem.X},{lastItem.Y}";

                var t0 = TrackEntity.Track.Get(firstItem.X, firstItem.Y);
                var t1 = TrackEntity.Track.Get(lastItem.X, lastItem.Y);

                string firstName = firstCoord;
                if (t0 != null && !string.IsNullOrEmpty(t0.Name))
                    firstName = t0.Name;
                string lastName = lastCoord;
                if (t1 != null && !string.IsNullOrEmpty(t1.Name))
                    lastName = t1.Name;

                var item = new Items.BlockRouteItem
                {
                    Title = $"#{i + 1}: {firstName} -> {lastName}",
                    RoutePoints = route
                };

                _itemBlockRoutes.Items.Add(item);
            }
        }

        private T GetWindow<T>() where T : class
        {
            foreach (var item in Windows)
            {
                if (item is T)
                    return item as T;
            }

            return default(T);
        }

        private List<T> GetWindowList<T>() where T : class
        {
            var list = new List<T>();
            foreach (var item in Windows)
            {
                if (item is T)
                    list.Add(item as T);
            }
            return list;
        }

        private void DispatcherOnReadyToPlay(object sender, EventArgs eventArgs)
        {
            var weaveFilepath = Path.Combine(Project.Dirpath, Project.Track.Weave);
            _dispatcher.InitializeWeaving(_trackEntity.Track, weaveFilepath);

            var allSwitches = _dispatcher.GetDataProvider().Objects.OfType<TrackInformation.Switch>();
            foreach (var sw in allSwitches)
                sw?.ChangeDirection(0);
        }

        private void DispatcherOnUpdateUi(object sender, TrackWeaver.TrackWeaver trackWeaver)
        {
            int n = Windows.Count;

            for (int i = 0; i < n; ++i)
            {
                try
                {
                    if (i >= Windows.Count)
                        return;

                    var ww = Windows[i] as TrackWindow;
                    if (ww == null || ww.Entity == null)
                        continue;

                    ww.Entity.UpdateTrackViewerUi(trackWeaver);
                }
                catch
                {
                    // ignore
                }
            }
        }

        internal void TriggerUpdateUi()
        {
            DispatcherOnUpdateUi(null, _dispatcher.Weaver);
        }

        private async void DataProviderOnCommandsReady(object sender, IReadOnlyList<ICommand> commands)
        {
            if (_dispatcher != null)
                await _dispatcher.ForwardCommands(commands);
        }

        private void OnDataChanged(object sender)
        {
            _ctx.Send(state =>
            {
                var dataProvider = _dispatcher.GetDataProvider();

                IsDirty = true;

                foreach (var e in dataProvider.Objects)
                {
                    if (e == null)
                        continue;

                    if (e is Ecos2)
                    {
                        var ee = e as Ecos2;

                        if (_itemStatus != null && _itemStatus.Items.Count < 4)
                        {
                            _itemStatus.Items.Clear();
                            _itemStatus.Items.Add(new Item { Title = $"{ee.Name}", IconName = "esu0.png" });
                            _itemStatus.Items.Add(new Item { Title = $"Application Version: {ee.ApplicationVersion}", IconName = "esu0.png" });
                            _itemStatus.Items.Add(new Item { Title = $"Protocol Version: {ee.ProtocolVersion}", IconName = "esu0.png" });
                            _itemStatus.Items.Add(new Item { Title = $"Hardware Version: {ee.HardwareVersion}", IconName = "esu0.png" });
                        }
                        else
                        {
                            if (_itemStatus != null)
                            {
                                _itemStatus.Items[0].Title = $"{ee.Name}";
                                _itemStatus.Items[1].Title = $"Application Version: {ee.ApplicationVersion}";
                                _itemStatus.Items[2].Title = $"Protocol Version: {ee.ProtocolVersion}";
                                _itemStatus.Items[3].Title = $"Hardware Version: {ee.HardwareVersion}";
                            }
                        }

                        RaisePropertyChanged("TogglePowerCaption");
                    }
                    else if (e is Locomotive)
                    {
                        var ee = e as Locomotive;

                        if (_itemLocomotives.Items.Any(x => x.ObjectId == ee.ObjectId))
                        {
                            ee.UpdateTitle();
                            ee.UpdateSubTitle();
                        }
                        else
                        {
                            ee.UpdateTitle();
                            ee.UpdateSubTitle();
                            _itemLocomotives.Items.Add(ee);
                        }

                        Log($"Locomotive {ee.Addr}, {ee.Name}\r\n");
                    }
                    else if (e is S88)
                    {
                        var ee = e as S88;

                        if (_itemS88.Items.Any(x => x.ObjectId == ee.ObjectId))
                        {
                            ee.UpdateTitle();
                            ee.UpdateSubTitle();
                        }
                        else
                        {
                            ee.UpdateTitle();
                            ee.UpdateSubTitle();
                            _itemS88.Items.Add(ee);
                        }
                    }
                    else if (e is Switch)
                    {
                        var ee = e as Switch;

                        if (_itemSwitches.Items.Any(x => x.ObjectId == ee.ObjectId))
                        {
                            ee.UpdateTitle();
                            ee.UpdateSubTitle();
                        }
                        else
                        {
                            ee.UpdateTitle();
                            ee.UpdateSubTitle();
                            _itemSwitches.Items.Add(ee);
                        }
                    }
                    else if (e is TrackInformation.Route)
                    {
                        var ee = e as TrackInformation.Route;

                        if (_itemRoutes.Items.Any(x => x.ObjectId == ee.ObjectId))
                        {
                            ee.UpdateTitle();
                            ee.UpdateSubTitle();
                        }
                        else
                        {
                            ee.UpdateTitle();
                            ee.UpdateSubTitle();
                            _itemRoutes.Items.Add(ee);
                        }
                    }
                }
            }, new object());
        }

        public void Close(object o)
        {
            if (_project != null)
            {
                TrackEntity.DisableEdit();

                _autoplayer?.Stop();
                _autoplayer?.Cleanup();
                try
                {
                    if (_autoplayer != null)
                    {
                        _autoplayer.Started -= Started;
                        _autoplayer.Stopped -= Stopped;
                        _autoplayer.Failed -= Failed;
                    }
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _autoplayer = null;
                }

                if (IsDirty)
                {
                    var r = Helper.Ask("Project has been modified, save before close?", 
                        "Project modified", "Save Project", "Discard Changes");
                    if(r)
                        Save(null);
                }

                Windows.Clear();
                RootItems.Clear();

                if (_itemStatus != null)
                    _itemStatus.Items.Clear();
                if(_itemLocomotives!= null)
                    _itemLocomotives.Items.Clear();
                if(_itemSwitches != null)
                    _itemSwitches.Items.Clear();
                if(_itemS88 != null)
                     _itemS88.Items.Clear();
                if (_itemRoutes != null)
                    _itemRoutes.Items.Clear();
                if(_itemBlockRoutes != null)
                    _itemBlockRoutes.Items.Clear();

                IsDryRun = false;
                IsDirty = false;

                Project = null;
            }
        }

        internal void Save()
        {
            Save(null);
        }

        public void Save(object p)
        {
            if (_trackEntity is IPersist ee)
            {
                bool r = ee.Save();
                if (!r)
                    Log("<Save> Failure storing file: " + _trackEntity.TrackObjectFilepath + "\r\n");
            }

            Project.TargetHost = _cfg.IpAddress;
            Project.TargetPort = _cfg.Port;
            Project.DesignerColumns = _cfg.DesignerColumns;
            Project.DesignerRows = _cfg.DesignerRows;

            // transfer window dimensions
            // ...

            Project.Save();

            var globalFilepath = Path.Combine(_project.Dirpath, "TrackObjects.json");
            var r3 = _dispatcher?.GetDataProvider().SaveObjects(globalFilepath);
            if (r3.HasValue)
                Log("Storing failed: " + globalFilepath + "\r\n");

            SetDirty(false);
        }

        public void Exit(object p)
        {
            System.Windows.Application.Current.Shutdown();
        }

        public string TogglePowerCaption
        {
            get
            {
                string s0 = "Switch on (GO)";
                string s1 = "Switch off (STOP)";
                string s2 = "Power Down (SHUTDOWN)";

                if (_dispatcher == null)
                {
                    _itemStatus.Title = "Status";
                    _itemStatus.Items.Clear();
                    return s2;
                }

                var data = _dispatcher.GetDataProvider();
                if (data == null)
                {
                    _itemStatus.Title = "Status";
                    _itemStatus.Items.Clear();
                    return s2;
                }

                var ecos = data.GetObjectBy(1) as TrackInformation.Ecos2;
                if (ecos == null)
                {
                    _itemStatus.Title = "Status";
                    _itemStatus.Items.Clear();
                    return s2;
                }

                if (ecos.CurrentState == Ecos2.State.Go)
                {
                    _itemStatus.Title = "Status (GO)";
                    return s1;
                }

                if (ecos.CurrentState == Ecos2.State.Stop)
                {
                    _itemStatus.Title = "Status (STOP)";
                    return s0;
                }

                _itemStatus.Title = "Status";
                _itemStatus.Items.Clear();

                return s2;
            }
        }

        public async void TogglePower(object p)
        {
            var data = _dispatcher.GetDataProvider();
            if (data == null)
                return;

            var ecos = data.GetObjectBy(1) as TrackInformation.Ecos2;
            if (ecos == null)
                return;

            List<ICommand> cmds = new List<ICommand>();

            if (ecos.CurrentState == Ecos2.State.Go)
            {
                Log("switch Ecos off\r\n");
                cmds.Add(CommandFactory.Create("set(1, stop)"));
                cmds.Add(CommandFactory.Create("get(1, status)"));
            }
            else if (ecos.CurrentState == Ecos2.State.Stop)
            {
                Log("switch Ecos on\r\n");
                cmds.Add(CommandFactory.Create("set(1, go)"));
                cmds.Add(CommandFactory.Create("get(1, status)"));
            }

            await _dispatcher.ForwardCommands(cmds);
        }

        public void DoAutoplay(object p)
        {
            try
            {
                if (_autoplayer != null)
                {
                    _autoplayer.Stop();
                    _autoplayer.Started -= Started;
                    _autoplayer.Stopped -= Stopped;
                    _autoplayer.Failed -= Failed;
                    _autoplayer = null;
                    return;
                }

                if (_autoplayer == null)
                    _autoplayer = new Autoplay.Autoplay()
                    {
                        Ctx = this
                    };

                _autoplayer.Started += Started;
                _autoplayer.Stopped += Stopped;
                _autoplayer.Failed += Failed;

                _autoplayer.Start();
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                RaisePropertyChanged("AutoplayStateIcon");
                RaisePropertyChanged("AutoplayState");
                RaisePropertyChanged("AutoplayState2");
                RaisePropertyChanged("AutoplayState3");
            }
        }

        private bool _locsHasBeenStopped;

        public void DoStopAllLocsCmd(object p)
        {
            try
            {
                var dp = Dispatcher.GetDataProvider();
                if (dp == null)
                    return;
                foreach (var o in dp.Objects.OfType<Locomotive>())
                    o?.Stop();
            }
            catch
            {
                // ignore
            }
            finally
            {
                _locsHasBeenStopped = true;
            }
        }

        public void DoRestartAllLocsCmd(object p)
        {
            try
            {
                var dp = Dispatcher.GetDataProvider();
                if (dp == null)
                    return;
                foreach (var o in dp.Objects.OfType<Locomotive>())
                    o?.Restart();
            }
            catch
            {
                // ignore
            }
            finally
            {
                _locsHasBeenStopped = false;
            }
        }

        private void Started(object sender, EventArgs eventArgs)
        {
            _ctx?.Send(state =>
            {
                LogAutoplay("Autoplay started");
            }, new object());
        }

        private void Stopped(object sender, EventArgs eventArgs)
        {
            _ctx?.Send(state =>
            {
                LogAutoplay("Autoplay stopped");
            }, new object());
        }

        private void Failed(object sender, EventArgs eventArgs)
        {
            _ctx?.Send(state =>
            {
                LogAutoplay("Autoplay failed");
            }, new object());
        }

        public void ShowLocomotive(object p)
        {
            if (!(p is Locomotive locItem))
            {
                LogError($"Unknown Locomotive");
                return;
            }
            LocomotivesWindow w = null;
            var locWindows = Windows.OfType<LocomotivesWindow>();
            foreach (var lw in locWindows)
            {
                if (lw.Entity.ObjectItem.ObjectId == locItem.ObjectId)
                {
                    w = lw;
                    break;
                }
            }

            if (w != null)
            {
                w.Entity.ObjectItem = locItem;
                w.UpdateFuncset();

                return;
            }

            var item2 = new LocomotivesWindow
            {
                Entity = new LocomotiveEntity
                {
                    ObjectItem = locItem,
                    Model = this
                }
            };

            item2.Entity.UpdateUi();
            item2.Closing += (s, e) => Windows.Remove(item2);

            lock (Windows)
            {
                Windows.Add(item2);
            }
            item2.UpdateFuncset();
        }

        public void SetLockToLocomotive(Locomotive locItem, bool state)
        {
            locItem.Locked = state;
            Save();
            Thread.Sleep(125);
            TrackEntity.UpdateAllVisualBlocks();
        }

        public void DryRun(object p)
        {
            IsDryRun = true;
            var weaveFilepath = Path.Combine(Project.Dirpath, Project.Track.Weave);
            _dispatcher.InitializeWeaving(_trackEntity.Track, weaveFilepath);
            Thread.Sleep(125);
            TriggerUpdateUi();
        }

        public void ConnectToCommandStation(object p)
        {
            try
            {
                if (IsDryRun)
                    return;

                if (_dispatcher != null)
                    _dispatcher.SetRunMode(true);
            }
            catch (Exception ex)
            {
                LogNetwork("Could not connect to command station ({1}:{2}): {0}\r\n", ex.Message, _cfg.IpAddress, _cfg.Port);
            }
        }

        public void DisconnectFromCommandStation(object p)
        {
            TrackEntity.DisableEdit();

            if (IsDryRun)
            {
                IsDryRun = false;
            }
            else
            {
                if (_dispatcher != null)
                    _dispatcher.SetRunMode(false);
            }
        }

        public void PropertiesCommandStation(object p)
        {
            var w = GetWindow<PropertiesWindow>();
            if (w != null)
                return;

            var item2 = new PropertiesWindow(_cfg);
            item2.Closing += (s, e) => Windows.Remove(item2);
            lock (Windows)
            {
                Windows.Add(item2);
            }
        }

        public void ShowLog(object p)
        {
            var w = GetWindow<LogWindow>();
            if (w?.LogMode == LogWindow.Mode.General)
                return;

            var item2 = new LogWindow(_logMessagesGeneral)
            {
                LogMode = LogWindow.Mode.General,
                Model = this
            };
            item2.Closing += (s, e) => Windows.Remove(item2);
            lock (Windows)
            {
                Windows.Add(item2);
            }
        }

        public void ShowCommandLog(object p)
        {
            var w = GetWindow<LogWindow>();
            if (w?.LogMode == LogWindow.Mode.Commands)
                return;

            var item2 = new LogWindow(_logMessagesCommands)
            {
                LogMode = LogWindow.Mode.Commands,
                Model = this
            };
            item2.Closing += (s, e) => Windows.Remove(item2);
            lock (Windows)
            {
                Windows.Add(item2);
            }
        }

        public void ShowAutoplayLog(object p)
        {
            var w = GetWindow<LogWindow>();
            if (w?.LogMode == LogWindow.Mode.Autoplay)
                return;

            var item2 = new LogWindow(_logMessagesAutoplay)
            {
                LogMode = LogWindow.Mode.Autoplay,
                Model = this
            };
            item2.Closing += (s, e) => Windows.Remove(item2);
            lock (Windows)
            {
                Windows.Add(item2);
            }
        }

        private bool _isVisualLabelActivated;

        public bool IsVisualLabelActivated
        {
            get => _isVisualLabelActivated;
            set
            {
                _isVisualLabelActivated = value;
                RaisePropertyChanged("IsVisualLabelActivated");
            }
        }

        public void ShowIdentifiers(object p)
        {
            if (p == null)
                return;

            try
            {
                bool pp = (bool) p;

                if (pp)
                {
                    TrackEntity?.UpdateAllVisualIds(true);
                }
                else
                {
                    TrackEntity?.UpdateAllVisualIds(false);
                }
            }
            catch
            {
                // ignore
            }
        }

        public void ShowAbout(object p)
        {
            var dlg = new About.About();
            dlg.ShowDialog();
        }

        private bool AskForAnalyzeClean()
        {
            if (Project.BlockRoutes.Count > 0)
            {
                System.Windows.Style style = new System.Windows.Style();
                style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.YesButtonContentProperty, "Reset BlockRoutes"));
                style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.NoButtonContentProperty, "Cancel"));
                MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show("BlockRoutes exist, the following analysis will reset them.", "BlockRoutes exist", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes, style);
                if (result == MessageBoxResult.No || result == MessageBoxResult.Cancel)
                    return false;
            }
            
            return true;
        }

        public void AnalyzeRoutes(object p)
        {
            if (Project != null && Project.BlockRoutes != null)
            {
                if (!AskForAnalyzeClean())
                    return;

                Project.BlockRoutes.Clear();
            }

            Analyze.Analyze a = new Analyze.Analyze(this);
            AnalyzeResult res = a.Execute();
            if (res == null)
                throw new Exception("Analyzation failed");

            Trace.WriteLine("Analyze:\r\n" + res);

            if (res.Routes != null)
            {
                foreach (var r in res.Routes)
                {
                    var route = r?.ToRoute();
                    if (route != null && route.Count > 0 && Project != null)
                        Project.BlockRoutes?.Add(route);
                }
            }

            UpdateBlockRouteItems();

            Log(res.ToString());
        }

        public void AnalyzeClean(object p)
        {
            if (Project != null && Project.BlockRoutes != null)
            {
                if(!AskForAnalyzeClean())
                    return;

                Project.BlockRoutes.Clear();
                UpdateBlockRouteItems();
                Save();
            }
        }

        public void LoadLayout(object p)
        {
            MainView.LoadLayout();
        }

        public void SaveLayout(object p)
        {
            MainView.SaveLayout();
        }

        public void OpenProjectDirectory(object p)
        {
            var fname = Project.Filepath;
            if (!File.Exists(fname))
            {
                LogError($"Project file is missing: {fname}");
            }
            else
            {
                var pp = Path.GetDirectoryName(fname);
                try
                {
                    Process.Start("explorer.exe", pp);
                }
                catch (Exception)
                {
                    LogError($"explorer.exe failed to open dir project directory: {pp}");
                }
            }
        }

#region can execute checks

        public bool CheckAnalyzeRoutes(object p)
        {
            if (_dispatcher == null)
                return false;

            if (Project == null)
                return false;

            return true;
        }

        public bool CheckAnalyzeClean(object o)
        {
            if (_dispatcher == null)
                return false;

            if (Project == null)
                return false;

            return true;
        }

        public bool CheckTogglePower(object p)
        {
            if (_project == null)
                return false;
            if (_dispatcher == null)
                return false;
            if (!_dispatcher.GetRunMode())
                return false;
            return true;
        }

        public bool CheckDoAutoplay(object p)
        {
            if (_project == null)
                return false;

            if (_trackEntity == null)
                return false;

            if (IsDryRun)
                return true;

            return _dispatcher.GetRunMode();
        }

        public bool CheckStopAllLocsCmd(object p)
        {
            if (_project == null)
                return false;
            if (_locsHasBeenStopped)
                return false;
            if (IsDryRun || Dispatcher.GetRunMode())
                return true;
            return false;
        }

        public bool CheckRestartAllLocsCmd(object p)
        {
            if (_project == null)
                return false;
            if (IsDryRun || Dispatcher.GetRunMode())
            {
                if(_locsHasBeenStopped)
                    return true;
            }
            return false;
        }

        public bool CheckAddTrack(object p)
        {
            if (_project == null)
                return false;
            //if (_trackEntity != null)
            //    return false;
            return true;
        }

        public bool CheckRemoveTrack(object p)
        {
            if (_project == null)
                return false;
            if (_trackEntity != null)
                return true;
            return false;
        }

        private bool CheckOpenProjectDirectory(object p)
        {
            if (Project != null)
                return true;
            return false;
        }

        private bool CheckDisconnectFromCommandStation(object p)
        {
            if (_dispatcher == null)
                return false;

            if (IsDryRun)
                return true;

            return _dispatcher.GetRunMode();
        }

        private bool CheckConnectToCommandStation(object o1)
        {
            if (IsDryRun)
                return false;

            if (_project == null || _cfg == null)
                return false;

            if (_dispatcher == null)
                return false;

            if (_dispatcher.GetRunMode())
                return false;

            return true;
        }

        private bool _isDryRun;

        internal bool IsDryRun
        {
            get => _isDryRun;
            set
            {
                _isDryRun = value;
                
                RaisePropertyChanged("IsDryRun");
                RaisePropertyChanged("ConnectionState");
                RaisePropertyChanged("ConnectionStateIcon");
            }
        }

        public bool CheckDryRun(object p)
        {
            if (_project == null || _cfg == null)
                return false;

            if (IsDryRun)
                return false;

            if (_dispatcher == null)
                return false;

            if (_dispatcher.GetRunMode())
                return false;

            return true;
        }

        public bool CheckNewProject(object p)
        {
            return CheckOpen(p);
        }

        public bool CheckOpen(object p)
        {
            if (_project == null)
                return true;

            return false;
        }

        public bool CheckOpenRecent(object p)
        {
            if (_project == null)
                return true;
            return false;
        }

        public bool CheckClose(object p)
        {
            if (_project == null)
                return false;

            return true;
        }

        public bool CheckSave(object p)
        {
            if (_project == null)
                return false;

            return true;
        }

        public bool CheckExit(object p)
        {
            return true;
        }

        public bool CheckShowIdentifiers(object p)
        {
            if (_project == null)
                return false;
            if (_trackEntity == null)
                return false;

            return true;
        }

#endregion

        #region IRailwayEssentialModel

        public void TriggerPropertyChanged(string name)
        {
            if (_ctx == null || string.IsNullOrEmpty(name))
                return;

            _ctx.Send(state =>
            {
                RaisePropertyChanged(name);
            }, new object());
        }

        //private TrackInformation.Locomotive _currentLocomotive;
        private TrackInformation.Switch _currentSwitch;

        public void SetCurrentLocomotive(object locomotiveItem)
        {
            //_currentLocomotive = locomotiveItem as TrackInformation.Locomotive;

            ShowLocomotive(locomotiveItem as TrackInformation.Locomotive);
        }

        public void SetCurrentSwitch(object switchItem)
        {
            _currentSwitch = switchItem as TrackInformation.Switch;
            // TODO add ShowSwitch();
        }

        public void SetDirty(bool state)
        {
            IsDirty = state;
        }

        public void ShowBlockRoutePreview(object blockRouteItem)
        {
            ResetBlockRoutePreview();

            var item = blockRouteItem as Items.BlockRouteItem;
            if (item == null)
                return;

            JArray arStart = new JArray();
            JArray arEnd = new JArray();
            JArray arGeneral = new JArray();

            int n = item.RoutePoints.Count;
            for(int idx = 0; idx < n; ++idx)
            {
                var r = item.RoutePoints[idx];

                bool isStart = false;
                bool isEnd = false;

                if (idx == 0)
                    isStart = true;
                else if (idx == n - 1)
                    isEnd = true;

                int w = 1;
                int h = 1;
                var trackInfo = TrackEntity.Track.Get(r.X, r.Y);
                if (trackInfo != null && _theme != null)
                {
                    var themeInfo = _theme.Get(trackInfo.ThemeId);
                    if (themeInfo != null)
                    {
                        var orientationIndex = Helper.GetOrientation(trackInfo);
                        w = themeInfo.Dimensions[orientationIndex].X;
                        h = themeInfo.Dimensions[orientationIndex].Y;
                    }
                }

                bool isBlock = false;

                if(trackInfo != null)
                    isBlock = new List<int> {150, 151, 152}.Contains(trackInfo.ThemeId);

                if (w > 1 || h > 1)
                {
                    for (int xx = 0; xx < w; ++xx)
                    {
                        for (int yy = 0; yy < h; ++yy)
                        {
                            JObject o = new JObject
                            {
                                ["col"] = r.X + xx,
                                ["row"] = r.Y + yy
                            };

                            if (isBlock)
                            {
                                if (isStart)
                                    arStart.Add(o);
                                else if (isEnd)
                                    arEnd.Add(o);
                                else
                                    arGeneral.Add(o);
                            }
                            else
                            {
                                arGeneral.Add(o);
                            }
                        }
                    }
                }
                else
                {
                    JObject o = new JObject
                    {
                        ["col"] = r.X,
                        ["row"] = r.Y
                    };

                    arGeneral.Add(o);
                }
            }

            if (TrackEntity != null && TrackEntity.Viewer != null)
            {
                ExecuteJs($"highlightRoute({arStart.ToString(Formatting.None)}, 'routeHighlightStart');");
                ExecuteJs($"highlightRoute({arEnd.ToString(Formatting.None)}, 'routeHighlightEnd');");
                ExecuteJs($"highlightRoute({arGeneral.ToString(Formatting.None)}, 'routeHighlight');");
            }
        }

        public void TestBlockRoute(object blockRouteItem)
        {
            var item = blockRouteItem as Items.BlockRouteItem;
            if (item == null)
                return;

            int n = item.RoutePoints.Count;
            for (int idx = 0; idx < n; ++idx)
            {
                var r = item.RoutePoints[idx];
                if (r == null)
                    continue;

                var trackInfo = TrackEntity.Track.Get(r.X, r.Y);
                if (trackInfo != null)
                {
                    var itemObjects = _dispatcher.Weaver.GetObject(trackInfo);
                    if (itemObjects.Count == 0)
                        continue;

                    var switchItem = itemObjects[0] as TrackInformation.Switch;
                    if (switchItem == null)
                        continue;

                    var hasTurn = r.HasTurn;                   
                    var v = hasTurn ? 0 : 1;
                    if (switchItem.InvertCommand)
                    {
                        if (v == 1) v = 0;
                        else v = 1;
                    }
                    var vs = v == 1 ? "TURN" : "STRAIGHT";
                    Trace.WriteLine($"<Test> Switch '{switchItem.Name1}' change to '{vs}'");
                    switchItem.ChangeDirection(v);
                }
            }
        }

        public void ResetBlockRoutePreview()
        {
            ExecuteJs("resetHighlightRoute()");
        }

        public void UpdateTrackUi()
        {
            var state = false;
            if (_dispatcher?.Model != null)
                state = _dispatcher.Model.IsVisualLabelActivated;

            var e = Windows.OfType<TrackWindow>().Where(x => !x.Entity.IsClone);
            var trackWindows = e as TrackWindow[] ?? e.ToArray();
            if(trackWindows.Any())
                trackWindows.First().Entity.RaiseUiUpdates();

            TrackEntity?.UpdateAllVisualIds(state);
            TrackEntity?.UpdateAllVisualBlocks();
        }

        public void ExecuteJs(string code, object sender=null)
        {
            if(string.IsNullOrEmpty(code))
                return;

            if (sender != null)
            {
                if (sender is TrackEntity s)
                {
                    s.Viewer?.ExecuteJs(code);
                }
            }
            else
            {

                foreach (var w in Windows)
                {
                    var ww = w as TrackWindow;
                    if (ww == null)
                        continue;
                    if (ww.Entity == null)
                        continue;

                    ww.Entity.Viewer?.ExecuteJs(code);
                }
            }
        }

        #endregion
    }
}
