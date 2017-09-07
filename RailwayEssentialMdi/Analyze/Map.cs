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
using System.Linq;
using System.Runtime.InteropServices;
using RailwayEssentialMdi.ViewModels;

namespace RailwayEssentialMdi.Analyze
{
    public class DirectionInfo
    {
        public bool IsFromLeft { get; private set; }
        public bool IsFromTop { get; private set; }
        public bool IsFromRight { get; private set; }
        public bool IsFromBottom { get; private set; }

        public DirectionInfo(MapItem from, MapItem to)
        {
            IsFromLeft = to.Info.IsLeft(from.Info);
            IsFromTop = to.Info.IsUp(from.Info);
            IsFromRight = to.Info.IsRight(from.Info);
            IsFromBottom = to.Info.IsDown(from.Info);
        }
    }

    public class Map
    {
        private readonly RailwayEssentialModel _model;

        public List<MapItem> Items { get; private set; }

        public Map(RailwayEssentialModel model)
        {
            _model = model;

            Items = new List<MapItem>();
        }

        public MapItem Get(int x, int y)
        {
            foreach (var e in Items)
            {
                if (e == null)
                    continue;

                if (e.X0 == x && e.Y0 == y)
                    return e;

                if (e.X1 == x && e.Y1 == y)
                    return e;
            }

            return null;
        }

        public MapItem[] GetConnectors(int connectorId)
        {
            if (connectorId == -1)
                return null;

            var items = new MapItem[2];

            var o = _model?.TrackEntity;

            int maxX = 40;
            int maxY = 40;

            if (o != null)
            {
                maxX = o.Cfg.DesignerColumns;
                maxY = o.Cfg.DesignerRows;
            }

            var index = 0;

            for (int x = 0; x < maxX; ++x)
            {
                for (int y = 0; y < maxY; ++y)
                {
                    var item = Get(x, y);
                    if (item == null)
                        continue;
                    if (!item.IsConnector)
                        continue;
                    if (item.ConnectorId == connectorId)
                    {
                        items[index] = item;
                        index++;

                        if (index == 2)
                            return items;
                    }
                }
            }

            return items;
        }

        public void Build()
        {
            if (_model?.TrackEntity == null)
                return;

            var o = _model?.TrackEntity;

            var maxX = o.Cfg.DesignerColumns;
            var maxY = o.Cfg.DesignerRows;

            for (int x = 0; x <= maxX; ++x)
            {
                for (int y = 0; y <= maxY; ++y)
                {
                    var it = o.Track.Get(x, y);

                    if (it == null)
                        continue;

                    if (it.ThemeId == -1)
                        continue;

                    var mapItem = new MapItem(_model, this)
                    {
                        Info = it
                    };

                    Items.Add(mapItem);
                }
            }

            foreach (var item in Items)
                item?.UpdateMovement();
        }

        private List<MapItem> GetBlocks()
        {
            List<MapItem> blocks = new List<MapItem>();

            foreach (var e in Items)
            {
                if (e == null)
                    continue;

                if (e.IsBlock)
                    blocks.Add(e);
            }

            return blocks;
        }

        public int GetRoutes()
        {
            var blks = GetBlocks();

            foreach (var b0 in blks)
            {
                if (b0 == null)
                    continue;

                _branches.Clear();
                _currentWay = "";

                GetAllPath(b0);                
            }

            foreach(var p in _paths)
                Trace.WriteLine("P: " + p);

            foreach (var p in _paths)
            {
                if (string.IsNullOrEmpty(p))
                    continue;

                WayPoints wps = new WayPoints(this, p);
                
                if(Routes == null)
                    Routes = new List<WayPoints>();

                Routes.Add(wps);
            }

            if (Routes == null)
                return 0;

            return Routes.Count;
        }

        public static List<RouteGroup> GetRouteGroups(List<Route> routes)
        {
            var list = new List<RouteGroup>();

            if (routes == null || routes.Count <= 0)
                return list;

            int nrOfRoutes = routes.Count;

            for (int i = 0; i < nrOfRoutes; ++i)
            {
                var route = routes[i];
                if (route == null)
                    continue;

                RouteGroup grp = new RouteGroup();
                grp.Routes.Add(route);

                for (int j = 0; j < nrOfRoutes; ++j)
                {
                    if (i == j)
                        continue;

                    var r = routes[j];
                    if (r == null)
                        continue;

                    bool res = Route.Cross(route, r);
                    if (res)
                        grp.Routes.Add(r);
                }

                list.Add(grp);
            }

            return list;
        }

        public List<RouteGroup> GetRouteGroups(List<WayPoints> wps = null)
        {
            var list = new List<RouteGroup>();

            if (wps == null)
                wps = Routes;

            if (wps == null || wps.Count <= 0)
                return list;

            int nrOfRoutes = wps.Count;

            for (int i = 0; i < nrOfRoutes; ++i)
            {
                var route = wps[i];
                if (route == null)
                    continue;

                var rr0 = route.ToRoute();
                if (rr0 == null)
                    continue;

                RouteGroup grp = new RouteGroup();
                grp.Routes.Add(rr0);

                for (int j = 0; j < nrOfRoutes; ++j)
                {
                    if (i == j)
                        continue;

                    var r = wps[j];
                    if (r == null)
                        continue;

                    var rr1 = r.ToRoute();
                    if (rr1 == null)
                        continue;

                    bool res = Route.Cross(rr0, rr1);
                    if (res)
                        grp.Routes.Add(rr1);
                }

                list.Add(grp);
            }

            return list;
        }

        private void GetAllPath(MapItem from)
        {
            if (from == null)
                return;

            bool wasConnected;
            var neighbours = from.GetReachableNeighbours(out wasConnected);

            foreach (var n in neighbours)
            {
                var nItem = Get(n.X, n.Y);

                _currentWay += $"{from.Identifier}->";

                StartWalk(nItem, from);
            }

            // check for branches
            if (_branches.Count > 0)
            {
                BranchInfo branch = null;

                while((branch=_branches.Pop()) != null)
                { 
                    for (int j = 0; j < branch.Neighbours.Count; ++j)
                    {
                        _currentWay = branch.RecentWay;

                        var nb = branch.Neighbours[j];

                        bool r = CheckSwitchAndTurn(branch.Item, nb);
#if DEBUG
                        if(r)
                            Trace.WriteLine(" TURN #1 ");
#endif
                        StartWalk(nb, branch.Item);
                    }

                    branch.Neighbours.Clear();

                    if (_branches.Count == 0)
                        break;
                }
            }
        }

        private void StartWalk(MapItem item, MapItem comingFrom)
        {
            _currentWay += $"{item.Identifier}->";

            Walk(item, comingFrom);
        }

        private void Walk(MapItem item, MapItem comingFrom)
        {
            if (item == null)
            {
                _currentWay = "";
                return;
            }

            DirectionInfo dirinfo = new DirectionInfo(comingFrom, item);

            bool isFromLeft = dirinfo.IsFromLeft;
            bool isFromTop = dirinfo.IsFromTop;
            bool isFromRight = dirinfo.IsFromRight;
            bool isFromBottom = dirinfo.IsFromBottom;

            if (item != null && item.IsBlock)
            {
                _paths.Add(_currentWay);
                _currentWay = "";
                return;
            }

            bool wasConnected;
            var nbs = item.GetReachableNeighbours(out wasConnected, comingFrom.Info);

            if (nbs.Count == 0)
            {
                _currentWay = "";
            }
            else
            {
                if (nbs.Count == 1)
                {
                    var it = nbs[0];
                    var nbsItem = Get(it.X, it.Y);

                    if (item.IsDirection || item.IsSwitch)
                    {
                        bool stopWalk = false;

                        bool nIsLeft = item.Info.IsLeft(it);
                        bool nIsUp = item.Info.IsUp(it);
                        bool nIsRight = item.Info.IsRight(it);
                        bool nIsDown = item.Info.IsDown(it);

                        if (isFromLeft)
                        {
                            if (nIsDown && !item.CanGoFromLeftToBottom()
                                || nIsUp && !item.CanGoFromLeftToTop()
                                || nIsRight && !item.CanGoFromLeftToRight())
                            {
                                stopWalk = true;
                            }
                        }
                        else if (isFromTop)
                        {
                            if (nIsLeft && !item.CanGoFromTopToLeft()
                                || nIsDown && !item.CanGoFromTopToBottom()
                                || nIsRight && !item.CanGoFromTopToRight())
                            {
                                stopWalk = true;
                            }
                        }
                        else if (isFromRight)
                        {
                            if (nIsUp && !item.CanGoFromRightToTop()
                                || nIsDown && !item.CanGoFromRightToBottom()
                                || nIsLeft && !item.CanGoFromRightToLeft())
                            {
                                stopWalk = true;
                            }
                        }
                        else if (isFromBottom)
                        {
                            if (nIsLeft && !item.CanGoFromBottomToLeft()
                                || nIsUp && !item.CanGoFromBottomToTop()
                                || nIsRight && !item.CanGoFromBottomToRight())
                            {
                                stopWalk = true;
                            }
                        }

                        if(stopWalk)
                        {
                            _currentWay = "";
                            return;
                        }
                    }
                   
                    _currentWay += $"{nbsItem.Identifier} -> ";

                    if (wasConnected)
                    {
                        if (item != null)
                        {
                            var target = item.GetConnectorTarget();
                            if(target != null)
                                _currentWay += $"{target.Identifier} -> ";
                            Walk(nbsItem, target ?? item);
                        }
                    }
                    else
                    {
                        Walk(nbsItem, item);
                    }
                }
                else
                {
                    List<int> indecesForRemove = new List<int>();

                    for(int i=0; i < nbs.Count; ++i)
                    {
                        var n = nbs[i];

                        bool nIsLeft = item.Info.IsLeft(n);
                        bool nIsUp = item.Info.IsUp(n);
                        bool nIsRight = item.Info.IsRight(n);
                        bool nIsDown = item.Info.IsDown(n);

                        //string sourceInfo = isFromLeft
                        //    ? "From Left"
                        //    : isFromTop
                        //        ? "From Top"
                        //        : isFromRight
                        //            ? "From Right"
                        //            : isFromBottom
                        //                ? "From Bottom"
                        //                : "Unknown";

                        //string targetInfo = nIsLeft
                        //    ? "Going Left"
                        //    : nIsUp
                        //        ? "Going Up"
                        //        : nIsRight
                        //            ? "Going Right"
                        //            : nIsDown
                        //                ? "Going Down"
                        //                : "Unknown";

                        //Trace.Write($" [{sourceInfo} -> {targetInfo}] ");

                        if (isFromLeft)
                        {
                            if (nIsDown && !item.CanGoFromLeftToBottom()
                                || nIsUp && !item.CanGoFromLeftToTop()
                                || nIsRight && !item.CanGoFromLeftToRight())
                            {
                                indecesForRemove.Add(i);
                            }
                        }
                        else if (isFromTop)
                        {
                            if (nIsLeft && !item.CanGoFromTopToLeft()
                                || nIsDown && !item.CanGoFromTopToBottom()
                                || nIsRight && !item.CanGoFromTopToRight())
                            {
                                indecesForRemove.Add(i);
                            }
                        }
                        else if (isFromRight)
                        {
                            if (nIsUp && !item.CanGoFromRightToTop()
                                || nIsDown && !item.CanGoFromRightToBottom()
                                || nIsLeft && !item.CanGoFromRightToLeft())
                            {
                                indecesForRemove.Add(i);
                            }
                        }
                        else if (isFromBottom)
                        {
                            if (nIsLeft && !item.CanGoFromBottomToLeft()
                                || nIsUp && !item.CanGoFromBottomToTop()
                                || nIsRight && !item.CanGoFromBottomToRight())
                            {
                                indecesForRemove.Add(i);
                            }
                        }
                    }

                    indecesForRemove.Reverse();
                    foreach(var idx in indecesForRemove)
                        nbs.RemoveAt(idx);

                    if (nbs.Count > 1)
                    {
                        var branchInfo = new BranchInfo
                        {
                            Item = item,
                            RecentWay = _currentWay
                        };

                        foreach (var n in nbs)
                        {
                            var nItem = Get(n.X, n.Y);
                            if (nItem != null)
                            {
                                branchInfo.Neighbours.Add(nItem);
                            }
                        }

                        _currentWay = "";

                        _branches.Push(branchInfo);
                    }
                    else if(nbs.Count == 1)
                    {
                        var it = nbs[0];
                        var nbsItem = Get(it.X, it.Y);

                        bool r = CheckSwitchAndTurn(item, nbsItem);
#if DEBUG
                        if (r)
                            Trace.WriteLine(" TURN #2 ");
#endif
                        _currentWay += $"{nbsItem.Identifier} -> ";
                        Walk(nbsItem, item);
                    }
                }
            }
        }

        private bool CheckSwitchAndTurn(MapItem currentItem, MapItem neighbour)
        {
            if (currentItem == null)
                return false;
            if (!currentItem.IsSwitch)
                return false;
            if (neighbour == null)
                return false;

            var previousWay = new WayPoints(this, _currentWay);
            int lastIndex = previousWay.Count - 1;
            var fromWp = previousWay[lastIndex - 1];
            var fromItem = Get(fromWp.X0, fromWp.Y1);
            var nb = neighbour;

            var xdelta = nb.X0 - fromItem.X0;
            var ydelta = nb.Y0 - fromItem.Y0;

            bool hasTurn = Math.Abs(xdelta) == 1 || Math.Abs(ydelta) == 1;

            if (hasTurn)
            {
                _currentWay = _currentWay.Trim();
                int n = _currentWay.LastIndexOf("->", StringComparison.OrdinalIgnoreCase);
                _currentWay = _currentWay.Substring(0, n).Trim();
                _currentWay += ">->";
            }

            return hasTurn;
        }

        private class BranchInfo
        {
            public MapItem Item { get; set; }
            public string RecentWay { get; set; }
            public List<MapItem> Neighbours { get; private set; }

            public BranchInfo()
            {
                Neighbours = new List<MapItem>();
            }

            public void NeighbourHasBeenVisited(MapItem neighbour)
            {
                if (neighbour == null)
                    return;
                Neighbours.Remove(neighbour);
            }
        }
        
        private string _currentWay = "";

        private readonly Stack<BranchInfo> _branches = new Stack<BranchInfo>();

        private readonly List<string> _paths = new List<string>();

        public List<WayPoints> Routes { get; private set; }
    }
}
