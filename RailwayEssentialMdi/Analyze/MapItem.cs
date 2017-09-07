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
using System.Linq;
using RailwayEssentialMdi.ViewModels;
using Theme;
using TrackPlanParser;

namespace RailwayEssentialMdi.Analyze
{
    public class MapItem
    {
        private static int _instanceId;
        private int _localId;
        private TrackInfo _info;
        private readonly Map _ctx;
        private readonly RailwayEssentialModel _model;
        
        public TrackInfo Info
        {
            get => _info;
            set
            {
                _info = value;

                UpdateOrientation();
            }
        }

        public MapItem ShallowCopy()
        {
            var item = new MapItem(_model, _ctx)
            {
                _localId = _localId,
                _info = _info,
                HasTurn = HasTurn
            };
            return item;
        }

        private Entities.TrackEntity TrackEntity => _model?.TrackEntity;
        public int ThemeId => Info?.ThemeId ?? -1;
        private Theme.Theme Theme => _model?.TrackEntity.Theme;
        private int MaxX => TrackEntity?.Cfg?.DesignerColumns ?? -1;
        private int MaxY => TrackEntity?.Cfg?.DesignerRows ?? -1;
        public string Identifier => $"({Info.X},{Info.Y})";
        public bool HasTurn { get; set; }

        public List<string> BlockIdentifiers
        {
            get
            {
                List <string> ids = new List<string>();
                for (int x = X0; x <= X1; ++x)
                {
                    for (int y = Y0; y <= Y1; ++y)
                    {
                        var item = _ctx.Get(x, y);
                        if (item != null)
                            ids.Add($"({x},{y})");
                    }
                }
                return ids;
            }
        }
        public int Idx => _localId;

        private bool _dimensionInitialized;
        private int _orientationIndex;
        private ThemeItem _themeInfo;
        private ThemeItemDimension _dim;

        private void InitializeDimension()
        {
            if (_dimensionInitialized)
                return;
            _dimensionInitialized = true;
            _orientationIndex = Helper.GetOrientation(Info);
            if(Theme != null)
                _themeInfo = Theme.Get(ThemeId);
            if (_themeInfo != null)
            {
                if (_orientationIndex > 0 && _themeInfo.Dimensions.Count > _orientationIndex)
                    _dim = _themeInfo.Dimensions[_orientationIndex];
            }
        }

        public int X0
        {
            get
            {
                InitializeDimension();
                return Info.X;
            }
        }

        public int Y0
        {
            get
            {
                InitializeDimension();
                return Info.Y;
            }
        }

        public int X1 => X0 + Width - 1;
        public int Y1 => Y0 + Height - 1;

        public int Width
        {
            get
            {
                InitializeDimension();
                return _dim.X;
            }
        }

        public int Height
        {
            get
            {
                InitializeDimension();
                return _dim.Y;
            }
        }

        public bool IsTrack => RailwayEssentialCore.Globals.TrackIds.Contains(ThemeId);
        public bool IsSwitch => RailwayEssentialCore.Globals.SwitchIds.Contains(ThemeId);
        public bool IsSignal => RailwayEssentialCore.Globals.SignalIds.Contains(ThemeId);
        public bool IsBlock => RailwayEssentialCore.Globals.BlockIds.Contains(ThemeId);
        public bool IsSensor => RailwayEssentialCore.Globals.SensorIds.Contains(ThemeId);
        public bool IsDirection => RailwayEssentialCore.Globals.DirectionIds.Contains(ThemeId);
        public bool IsConnector => RailwayEssentialCore.Globals.ConnectorIds.Contains(ThemeId);

        public int ConnectorId
        {
            get
            {
                var connectorId = -1;
                if (IsConnector && Info.Options["connectorIdentifier"] != null)
                    connectorId = (int)Info.Options["connectorIdentifier"];
                return connectorId;
            }
        }

        public MapItem GetConnectorTarget()
        {
            if (!IsConnector)
                return null;
            if (ConnectorId == -1)
                return null;

            var cons = _ctx.GetConnectors(ConnectorId);
            if (cons != null && cons.Length == 2)
            {
                if (cons[0] != null && cons[0].Idx != Idx)
                    return cons[0];
                if (cons[1] != null && cons[1].Idx != Idx)
                    return cons[1];
            }

            return null;
        }

        public MapItem(RailwayEssentialModel model, Map ctx)
        {
            ++_instanceId;

            _localId = _instanceId;

            _ctx = ctx;
            _model = model;
        }

        public bool Left2Right { get; private set; }
        public bool Left2Top { get; private set; }
        public bool Left2Bottom { get; private set; }

        public bool Top2Left { get; private set; }
        public bool Top2Right { get; private set; }
        public bool Top2Bottom { get; private set; }

        public bool Right2Left { get; private set; }
        public bool Right2Top { get; private set; }
        public bool Right2Bottom { get; private set; }

        public bool Bottom2Left { get; private set; }
        public bool Bottom2Right { get; private set; }
        public bool Bottom2Top { get; private set; }

        /// <summary> human-readable information of possible ways </summary>
        public string DirectionInfo
        {
            get
            {
                string m = "";

                if (Left2Right) m += "Left2Right, ";
                if (Left2Top) m += "Left2Top, ";
                if (Left2Bottom) m += "Left2Bottom, ";

                if (Top2Left) m += "Top2Left, ";
                if (Top2Right) m += "Top2Right, ";
                if (Top2Bottom) m += "Top2Bottom, ";

                if (Right2Left) m += "Right2Left, ";
                if (Right2Top) m += "Right2Top, ";
                if (Right2Bottom) m += "Right2Bottom, ";

                if (Bottom2Left) m += "Bottom2Left, ";
                if (Bottom2Right) m += "Bottom2Right, ";
                if (Bottom2Top) m += "Bottom2Top";

                return m.Trim().TrimEnd(',');
            }
        }

        private void UpdateOrientation()
        {
            ThemeItemRoute e = GetWays();

            if (e == null)
                return;

            List<string> parts = new List<string>();

            if (IsDirection)
            {
                var rule = e.Value.TrimEnd('!');
                parts = rule.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else if (IsConnector)
            {
                var rule = e.Value.TrimEnd('+');
                parts = rule.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (parts.Count == 1)
                    parts.Add(parts[0].Reverse());
            }
            else if (IsTrack || IsSignal || IsBlock || IsSensor)
            {
                parts = e.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (parts.Count <= 1
                    && !parts[0].EndsWith("!", StringComparison.OrdinalIgnoreCase)
                    && !parts[0].EndsWith("+", StringComparison.OrdinalIgnoreCase)
                )
                {
                    parts.Add(parts[0].Reverse());
                }
            }
            else if (IsSwitch)
            {
                parts = e.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            foreach (var p in parts)
            {
                if (p.Length == 2)
                {
                    if (p[0] == 'A' && p[1] == 'C')
                        Left2Right = true;
                    if (p[0] == 'A' && p[1] == 'B')
                        Left2Top = true;
                    if (p[0] == 'A' && p[1] == 'D')
                        Left2Bottom = true;

                    if (p[0] == 'B' && p[1] == 'A')
                        Top2Left = true;
                    if (p[0] == 'B' && p[1] == 'C')
                        Top2Right = true;
                    if (p[0] == 'B' && p[1] == 'D')
                        Top2Bottom = true;

                    if (p[0] == 'C' && p[1] == 'A')
                        Right2Left = true;
                    if (p[0] == 'C' && p[1] == 'B')
                        Right2Top = true;
                    if (p[0] == 'C' && p[1] == 'D')
                        Right2Bottom = true;

                    if (p[0] == 'D' && p[1] == 'A')
                        Bottom2Left = true;
                    if (p[0] == 'D' && p[1] == 'B')
                        Bottom2Top = true;
                    if (p[0] == 'D' && p[1] == 'C')
                        Bottom2Right = true;
                }
            }
        }

        public bool CanMoveUp { get; private set; }
        public bool CanMoveDown { get; private set; }
        public bool CanMoveLeft { get; private set; }
        public bool CanMoveRight { get; private set; }

        #region check exit states

        public bool IsLeftExit
        {
            get
            {
                if (Top2Left) return true;
                if (Right2Left) return true;
                if (Bottom2Left) return true;
                return false;
            }
        }

        public bool IsTopExit
        {
            get
            {
                if (Left2Top) return true;
                if (Right2Top) return true;
                if (Bottom2Top) return true;
                return false;
            }
        }

        public bool IsRightExit
        {
            get
            {
                if (Left2Right) return true;
                if (Top2Right) return true;
                if (Bottom2Right) return true;
                return false;
            }
        }

        public bool IsBottomExit
        {
            get
            {
                if (Left2Bottom) return true;
                if (Top2Bottom) return true;
                if (Right2Bottom) return true;
                return false;
            }
        }

        #endregion

        #region check entrance states

        public bool IsLeftEntrance
        {
            get
            {
                if (Left2Bottom) return true;
                if (Left2Right) return true;
                if (Left2Top) return true;
                return false;
            }
        }

        public bool IsTopEntrance
        {
            get
            {
                if (Top2Bottom) return true;
                if (Top2Left) return true;
                if (Top2Right) return true;
                return false;
            }
        }

        public bool IsRightEntrance
        {
            get
            {
                if (Right2Left) return true;
                if (Right2Top) return true;
                if (Right2Bottom) return true;
                return false;
            }
        }

        public bool IsBottomEntrance
        {
            get
            {
                if (Bottom2Top) return true;
                if (Bottom2Left) return true;
                if (Bottom2Right) return true;
                return false;
            }
        }

        #endregion

        public void UpdateMovement()
        {
            var neighbours = GetNeighbours();

            if (neighbours == null || neighbours.Count <= 0)
            {
                CanMoveDown = false;
                CanMoveLeft = false;
                CanMoveRight = false;
                CanMoveUp = false;

                return;
            }

            foreach (var n in neighbours)
            {
                if (n == null || n.ThemeId == -1)
                    continue;

                MapItem nItem = _ctx.Get(n.Info.X, n.Info.Y);

                if (Info.IsLeft(n.Info))
                    CanMoveLeft = IsLeftExit && nItem.IsRightEntrance;
                else if (Info.IsRight(n.Info))
                    CanMoveRight = IsRightExit && nItem.IsLeftEntrance;
                else if (Info.IsUp(n.Info))
                    CanMoveUp = IsTopExit && nItem.IsBottomEntrance;
                else if (Info.IsDown(n.Info))
                    CanMoveDown = IsBottomExit && nItem.IsTopEntrance;
            }
        }

        private List<MapItem> GetNeighbours(TrackInfo ignore = null)
        {
            if (Info == null)
                return null;

            List<MapItem> neighbours = new List<MapItem>();

            var x = Info.X;
            var y = Info.Y;

            var width = Width;
            var height = Height;

            List<Coord> indeces = new List<Coord>();

            #region edges

            // top edge
            var startX = x;
            var startY = y - 1;
            if (startY > 0)
            {
                for (int col = 0; col < width; ++col)
                    indeces.Add(new Coord(startX + col, startY));
            }

            // bottom edge
            startX = x;
            startY = y + height;
            if (startY <= MaxY)
            {
                for (int col = 0; col < width; ++col)
                    indeces.Add(new Coord(startX + col, startY));
            }

            // left edge
            startY = y;
            startX = x - 1;
            if (startX > 0)
            {
                for (int row = 0; row < height; ++row)
                    indeces.Add(new Coord(startX, startY + row));
            }

            // right edge
            startY = y;
            startX = x + width;
            if (startX <= MaxX)
            {
                for (int row = 0; row < height; ++row)
                    indeces.Add(new Coord(startX, startY + row));
            }

            #endregion

            foreach (var e in indeces)
            {
                if (ignore?.X == e.X && ignore.Y == e.Y)
                    continue;

                var item = _ctx.Get(e.X, e.Y);
                if (item != null)
                    neighbours.Add(item);
            }

            return neighbours;
        }

        public List<TrackInfo> GetReachableNeighbours(out bool wasConnected, TrackInfo ignore = null)
        {
            wasConnected = false;
            List<TrackInfo> neighbours = new List<TrackInfo>();

            const int max = 10;
            int maxCounter;

            var x = Info.X;
            var y = Info.Y;

            var width = Width;
            var height = Height;

            MapItem ignoreItem = null;
            if (ignore != null)
                ignoreItem = _ctx.Get(ignore.X, ignore.Y);

            if (IsConnector && (ignoreItem != null && !ignoreItem.IsConnector))
            {
                var cons = _ctx.GetConnectors(ConnectorId);
                if (cons != null && cons.Length == 2)
                {
                    TrackInfo target = null;
                    TrackInfo source = null;

                    if (cons[0] != null && cons[0].Idx != Idx)
                        target = cons[0].Info;
                    else if (cons[1] != null && cons[1].Idx != Idx)
                        target = cons[1].Info;

                    if (cons[0] != null && cons[1] != null && target != null)
                    {
                        if (cons[0].Info != null && target.Equals(cons[0].Info))
                            source = cons[1].Info;
                        if (cons[1].Info != null && target.Equals(cons[1].Info))
                            source = cons[0].Info;
                    }

                    if (target != null)
                    {
                        var targetItem = _ctx.Get(target.X, target.Y);
                        if (targetItem != null)
                        {
                            wasConnected = true;
                            return targetItem.GetReachableNeighbours(out _, source);
                        }
                    }
                }
            }

            if (CanMoveDown)
            {
                var yy = y + height;

                var item = _ctx.Get(x, yy);
                var itemInfo = item.Info;

                if (ignore != null && ignore.Equals(itemInfo))
                {
                    // ignore
                }
                else if (itemInfo != null)
                    neighbours.Add(itemInfo);
            }
            if (CanMoveUp)
            {
                var yy = y - 1;

                var item = _ctx.Get(x, yy);
                TrackInfo itemInfo = item.Info;
                maxCounter = 0;
                while (itemInfo == null && maxCounter < max)
                {
                    --yy;

                    item = _ctx.Get(x, yy);
                    itemInfo = item.Info;
                }

                if (ignore != null && ignore.Equals(itemInfo))
                {
                    // ignore    
                }
                else if (itemInfo != null)
                    neighbours.Add(itemInfo);
            }
            if (CanMoveLeft)
            {
                var xx = x - 1;

                var item = _ctx.Get(xx, y);
                TrackInfo itemInfo = item.Info;
                maxCounter = 0;
                while (itemInfo == null && maxCounter < max)
                {
                    --xx;

                    item = _ctx.Get(xx, y);
                    itemInfo = item.Info;
                }

                if (ignore != null && ignore.Equals(itemInfo))
                {
                    // ignore    
                }
                else if (itemInfo != null)
                    neighbours.Add(itemInfo);
            }
            if (CanMoveRight)
            {
                var xx = x + width;

                var item = _ctx.Get(xx, y);
                TrackInfo itemInfo = item.Info;
                if (ignore != null && ignore.Equals(itemInfo))
                {
                    // ignore    
                }
                else if (itemInfo != null)
                    neighbours.Add(itemInfo);
            }
            return neighbours;
        }

        public List<int> GetReachableNeighbourIds()
        {
            List<int> indeces = new List<int>();
            var neighbours = GetReachableNeighbours(out _);
            foreach (var n in neighbours)
            {
                MapItem nItem = _ctx.Get(n.X, n.Y);
                indeces.Add(nItem.Idx);
            }
            return indeces;
        }

        private MapItem LeftItem => _ctx.Get(X0 - 1, Y0);
        private MapItem TopItem => _ctx.Get(X0, Y0 - 1);
        private MapItem RightItem => _ctx.Get(X1 + 1, Y0);
        private MapItem BottomItem => _ctx.Get(X0, Y1 + 1);

        public bool CanGoFromLeftToBottom()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (LeftItem == null || BottomItem == null)
                return false;

            if (!LeftItem.IsRightExit && !BottomItem.IsTopEntrance)
                return false;

            if (Left2Bottom)
                return true;

            return false;
        }

        public bool CanGoFromLeftToTop()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (LeftItem == null || TopItem == null)
                return false;

            if (!LeftItem.IsRightExit && !TopItem.IsBottomEntrance)
                return false;

            if (Left2Top)
                return true;

            return false;
        }

        public bool CanGoFromLeftToRight()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (LeftItem == null || RightItem == null)
                return false;

            if (!LeftItem.IsRightExit && !RightItem.IsLeftEntrance)
                return false;

            if (Left2Right)
                return true;

            return false;
        }

        public bool CanGoFromRightToBottom()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (RightItem == null || BottomItem == null)
                return false;

            if (!RightItem.IsLeftExit && !BottomItem.IsTopEntrance)
                return false;

            if (Right2Bottom)
                return true;

            return false;
        }

        public bool CanGoFromRightToTop()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (RightItem == null || TopItem == null)
                return false;

            if (!RightItem.IsLeftExit && !TopItem.IsBottomEntrance)
                return false;

            if (Right2Top)
                return true;

            return false;
        }

        public bool CanGoFromRightToLeft()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (RightItem == null || LeftItem == null)
                return false;

            if (!RightItem.IsLeftExit && !LeftItem.IsRightEntrance)
                return false;

            if (Right2Left)
                return true;

            return false;
        }

        public bool CanGoFromTopToBottom()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (TopItem == null || BottomItem == null)
                return false;

            if (!TopItem.IsBottomExit && !BottomItem.IsTopEntrance)
                return false;

            if (Top2Bottom)
                return true;

            return false;
        }

        public bool CanGoFromTopToRight()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (TopItem == null || RightItem == null)
                return false;

            if (!TopItem.IsBottomExit && !RightItem.IsLeftEntrance)
                return false;

            if (Top2Right)
                return true;

            return false;
        }

        public bool CanGoFromTopToLeft()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (TopItem == null || LeftItem == null)
                return false;

            if (!TopItem.IsBottomExit && !LeftItem.IsRightEntrance)
                return false;

            if (Top2Left)
                return true;

            return false;
        }

        public bool CanGoFromBottomToTop()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (BottomItem == null || TopItem == null)
                return false;

            if (!BottomItem.IsTopExit && !TopItem.IsBottomEntrance)
                return false;

            if (Bottom2Top)
                return true;

            return false;
        }

        public bool CanGoFromBottomToRight()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (BottomItem == null || RightItem == null)
                return false;

            if (!BottomItem.IsTopExit && !RightItem.IsLeftEntrance)
                return false;

            if (Bottom2Right)
                return true;

            return false;
        }

        public bool CanGoFromBottomToLeft()
        {
            var nbs = GetReachableNeighbours(out _);
            if (nbs == null || nbs.Count <= 0)
                return false;

            if (BottomItem == null || LeftItem == null)
                return false;

            if (!BottomItem.IsTopExit && !LeftItem.IsRightEntrance)
                return false;

            if (Bottom2Left)
                return true;

            return false;
        }

        public class WayInfo
        {
            public WayInfo(MapItem item)
            {
                LeftToBottom = item.CanGoFromLeftToBottom();
                LeftToTop = item.CanGoFromLeftToTop();
                LeftToRight = item.CanGoFromLeftToRight();

                TopToBottom = item.CanGoFromTopToBottom();
                TopToLeft = item.CanGoFromTopToLeft();
                TopToRight = item.CanGoFromTopToRight();

                RightToLeft = item.CanGoFromRightToLeft();
                RightToTop = item.CanGoFromRightToTop();
                RightToBottom = item.CanGoFromRightToBottom();

                BottomToTop = item.CanGoFromBottomToTop();
                BottomToLeft = item.CanGoFromBottomToLeft();
                BottomToRight = item.CanGoFromBottomToRight();
            }

            public bool LeftToBottom { get; }
            public bool LeftToTop { get; }
            public bool LeftToRight { get; }

            public bool TopToBottom { get; }
            public bool TopToLeft { get; }
            public bool TopToRight { get; }

            public bool RightToLeft { get; }
            public bool RightToTop { get; }
            public bool RightToBottom { get; }

            public bool BottomToTop { get; }
            public bool BottomToLeft { get; }
            public bool BottomToRight { get; }

            public override string ToString()
            {
                string m = "";

                if (LeftToBottom) m += "LeftToBottom, ";
                if (LeftToTop) m += "LeftToTop, ";
                if (LeftToRight) m += "LeftToRight, ";

                if (TopToBottom) m += "TopToBottom, ";
                if (TopToLeft) m += "TopToLeft, ";
                if (TopToRight) m += "TopToRight, ";

                if (RightToLeft) m += "RightToLeft, ";
                if (RightToTop) m += "RightToTop, ";
                if (RightToBottom) m += "RightToBottom, ";

                if (BottomToTop) m += "BottomToTop, ";
                if (BottomToLeft) m += "BottomToLeft, ";
                if (BottomToRight) m += "BottomToRight";

                return m;
            }
        }

        public WayInfo GetWayInfo()
        {
            return new WayInfo(this);
        }

        /// <summary>
        ///     "AB"        -> Straight
        ///     "CA,CD"     -> Switch
        ///     "CA,CB,CD"  -> Threeway
        /// </summary>
        /// <returns></returns>
        public ThemeItemRoute GetWays()
        {
            var themeInfo = Theme?.Get(ThemeId);
            if (themeInfo == null)
                return null;
            int index = Helper.GetOrientation(Info);
            return themeInfo.GetRoute(index);
        }
    }
}
