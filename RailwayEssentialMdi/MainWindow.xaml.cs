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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RailwayEssentialMdi.Analyze;
using RailwayEssentialMdi.Interfaces;
using RailwayEssentialMdi.ViewModels;
using TrackInformation;
using Xceed.Wpf.AvalonDock;

namespace RailwayEssentialMdi
{
    public partial class MainWindow : Window, IMainView
    {
        private RailwayEssentialModel _dataContext;
        private bool _initialized = false;

        public MainWindow()
        {
            InitializeComponent();

            Unloaded += MainWindow_Unloaded;

            EventManager.RegisterClassHandler(typeof(Window),
                Keyboard.KeyDownEvent, new KeyEventHandler(KeyDownInternal), true);
        }

        private bool _ctrlIsHold = false;

        private void KeyDownInternal(object sender, KeyEventArgs e)
        {
            if (!_initialized)
                return;

            if ((e.Key == Key.LeftCtrl && (e.KeyStates & KeyStates.Down) != 0)
                || e.Key == Key.RightCtrl && (e.KeyStates & KeyStates.Down) != 0)
            {
                _ctrlIsHold = true;
            }
            else
            {
                if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                    _ctrlIsHold = false;
            }

            if (e.Key == Key.S && _ctrlIsHold)
            {
                _dataContext?.Project?.Save();
                _dataContext?.SetDirty(false);
            }
        }

        private void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            _initialized = true;
        }

        private void MainWindow_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is RailwayEssentialModel m)
            {
                _dataContext = m;

                m.MainView = this;
            }
        }

        private void DockManager_OnDocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            if (!_initialized)
                return;

            //Trace.WriteLine("Document: " + e.Document);
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            //SaveLayout();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (_dataContext == null)
                return;

            _dataContext.Close(null);
        }

        private void TreeView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PropagateTreeViewSelection();
        }

        private void TreeView_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        private void TreeView_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_dataContext == null)
                return;

            var s = Explorer;
            if (s == null)
                return;

            var item = s.SelectedItem;

            if (item is Items.BlockRouteItem routeGrpItem)
            {

                ContextMenu mnu = new ContextMenu();

                MenuItem m0 = new MenuItem {Header = "Force Switches"};
                m0.Click += (o, args) => PropagateTestRoute(routeGrpItem);

                MenuItem m1 = new MenuItem {Header = "Show Route"};
                m1.Click += (o, args) => _dataContext.ShowBlockRoutePreview(routeGrpItem);

                MenuItem m2 = new MenuItem {Header = "Reset Route"};
                m2.Click += (o, args) => _dataContext.ResetBlockRoutePreview();

                mnu.Items.Add(m0);
                mnu.Items.Add(m1);
                mnu.Items.Add(m2);

                s.ContextMenu = mnu;
                s.ContextMenu.IsOpen = true;
            }
            else if (item is TrackInformation.Locomotive locItem)
            {
                ContextMenu mnu = new ContextMenu();

                MenuItem m0 = new MenuItem {Header = "Stop"};
                m0.Click += (o, args) => locItem.Stop();

                MenuItem m1 = new MenuItem {Header = "Send to Block...", IsEnabled = true};
                if (_dataContext.Project == null /*|| !_dataContext.AutoplayState2*/)
                {
                    m1.IsEnabled = false;
                }
                else
                {
                    var m = _dataContext;

                    foreach (var route in m.Project.BlockRoutes)
                    {
                        if (route == null)
                            continue;

                        var routeStartBlock = route.First() as WayPoint;
                        if (routeStartBlock == null)
                            continue;

                        var x = routeStartBlock.X;
                        var y = routeStartBlock.Y;

                        var trackInfo = m.TrackEntity.Track.Get(x, y);
                        if (trackInfo == null)
                            continue;

                        var locObjectId = trackInfo.GetLocomotiveObjectId();
                        if (locObjectId == -1)
                            continue;

                        if (locObjectId != locItem.ObjectId)
                            continue;

                        var targetBlock = route.Last() as WayPoint;
                        if (targetBlock == null)
                            continue;

                        x = targetBlock.X;
                        y = targetBlock.Y;

                        trackInfo = m.TrackEntity.Track.Get(x, y);
                        if (trackInfo == null)
                            continue;

                        var header = trackInfo.Name;
                        if (string.IsNullOrEmpty(header))
                            header = trackInfo.ToString();

                        MenuItem itm = new MenuItem {Header = header};
                        itm.Click += (o, args) =>
                        {
                            var autoplayer = _dataContext.Autoplayer;
                            autoplayer?.SetNext(locItem, route);
                        };

                        m1.Items.Add(itm);
                    }
                }

                MenuItem m2 = null;
                if (locItem.Locked)
                {
                    m2 = new MenuItem { Header = "Unlock" };
                    m2.Click += (o, args) => _dataContext.SetLockToLocomotive(locItem, false);
                }
                else
                {
                    m2 = new MenuItem { Header = "Lock" };
                    m2.Click += (o, args) => _dataContext.SetLockToLocomotive(locItem, true);
                }

                mnu.Items.Add(m0);
                mnu.Items.Add(m1);
                mnu.Items.Add(m2);

                s.ContextMenu = mnu;
                s.ContextMenu.IsOpen = true;
            }
            else if (item is Category catItem)
            {
                ContextMenu mnu = new ContextMenu();

                switch (catItem.Index)
                {
                    case 1: // Locomotives
                    {
                        MenuItem m0 = new MenuItem {Header = "Sync"};
                        m0.Click += (o, args) =>
                        {
                            var m = _dataContext;

                            var r = Helper.Ask(
                                "Sync will remove Locomotives which are currently unknown by the connected command station. Do you like to sync anyway?",
                                "Sync Locomotives", "Yes", "Abort");
                            if (r)
                            {
                                var dataProvider = m?.Dispatcher?.GetDataProvider();
                                if (dataProvider != null)
                                {
                                    lock (dataProvider.Objects)
                                    {
                                        var objs = dataProvider.Objects.OfType<Locomotive>()
                                            .Where(x => !x.IsKnownByCommandStation);

                                        List<int> indeces = new List<int>();

                                        foreach (var oo in objs)
                                            indeces.Add(dataProvider.Objects.IndexOf(oo));

                                        indeces.Sort();
                                        indeces.Reverse();

                                        foreach (int idx in indeces)
                                        {
                                            m.RemoveItemLocomotiveFromCategory(
                                                dataProvider.Objects[idx] as TrackInformation.Item);
                                            dataProvider.Objects.RemoveAt(idx);
                                        }
                                    }

                                    m.Project.Save();
                                    m.SetDirty(false);
                                }
                            }
                        };

                        mnu.Items.Add(m0);
                    }
                        break;

                    case 2: // S88
                        break;

                    case 3: //Switches
                        break;
                }

                if (mnu.Items.Count > 0)
                {
                    s.ContextMenu = mnu;
                    s.ContextMenu.IsOpen = true;
                }
                else
                {
                    mnu.Items.Clear();

                    if (s.ContextMenu != null)
                    {
                        s.ContextMenu.Items.Clear();
                        s.ContextMenu.IsOpen = false;
                        e.Handled = true;
                    }
                }
            }
            else
            {
                if (s.ContextMenu != null)
                {
                    s.ContextMenu.Items.Clear();
                    s.ContextMenu.IsOpen = false;
                    e.Handled = true;
                }
            }
        }

        private void PropagateTestRoute(Items.BlockRouteItem item=null)
        {
            if (_dataContext == null)
                return;

            var s = Explorer;
            if (s == null)
                return;

            if (item == null)
                item = s.SelectedItem as Items.BlockRouteItem;

            if (item != null)
                _dataContext.TestBlockRoute(item);
        }

        private void TreeView_OnKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                PropagateTreeViewSelection();
            else if (e.Key == Key.Escape)
                _dataContext.ResetBlockRoutePreview();
        }

        private void PropagateTreeViewSelection()
        {
            if (_dataContext == null)
                return;

            var s = Explorer;
            if (s == null)
                return;

            var item = s.SelectedItem;

            if (item is TrackInformation.Locomotive)
                _dataContext.SetCurrentLocomotive(item);
            else if (item is TrackInformation.Switch)
                _dataContext.SetCurrentSwitch(item);
            else if (item is Items.BlockRouteItem)
                _dataContext.ShowBlockRoutePreview(item);
            else
            {
                // ...
            }
        }

        #region IMainView

        public void SaveLayout()
        {
            try
            {
                var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                serializer.Serialize(@".\AvalonDock.config");
            }
            catch
            {
                // ignore
            }
        }

        public void LoadLayout()
        {
            try
            {
                var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dockManager);
                serializer.LayoutSerializationCallback += (s, args) =>
                {
                    args.Content = args.Content;
                };

                if (File.Exists(@".\AvalonDock.config"))
                    serializer.Deserialize(@".\AvalonDock.config");
            }
            catch
            {
                // ignore
            }
        }

        public DockingManager GetDock()
        {
            return dockManager;
        }

        #endregion
    }
}
