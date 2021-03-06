// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanItem.Path.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// ReSharper disable InconsistentNaming

namespace railessentials.Plan
{
    public partial class PlanItem
    {
        public List<string> GetAllPossibleWays()
        {
            var ways = new List<string>();
            var describedRoutes = GetDimensionRoutes();
            if (describedRoutes.Count == 0) return ways;

            foreach (var itRoute in describedRoutes)
            {
                if (string.IsNullOrEmpty(itRoute)) continue;
                if (itRoute.Trim().Length <= 1) continue;
                if (itRoute.EndsWith("!", StringComparison.OrdinalIgnoreCase))
                {
                    // one way
                    var v = itRoute.Trim().TrimEnd('!').ToLower();
                    if (!ways.Contains(v)) ways.Add(v);
                }
                else if (itRoute.EndsWith("+", StringComparison.OrdinalIgnoreCase))
                {
                    // connector, the other side must be found
                    // if the target does not exist following the way is not feasible

                    var connectors = Ctx.GetConnectors(editor.connectorId);
                    if (connectors.Length < 2)
                    {
                        Trace.WriteLine($"Connection between TrackConnectors with {editor.connectorId} can not be established.");
                        continue;
                    }

                    var v = itRoute.ToLower().Trim();
                    if (!ways.Contains(v)) ways.Add(v);

                    //var vv = PlanGlobals.Reverse(v).ToLower();
                    //if (!ways.Contains(vv)) ways.Add(vv);
                }
                else
                {
                    var v = itRoute.ToLower().Trim();
                    var containsInt = v.Any(char.IsDigit);

                    if (containsInt)
                    {
                        if (!ways.Contains(v)) ways.Add(v);

                        // NOTICE(1): do not add the reverse entry here
                        // it is added in GetAllowedPath() of required/necessarry
                    }
                    else
                    {
                        if (!ways.Contains(v)) ways.Add(v);

                        var vv = PlanGlobals.Reverse(v).ToLower();
                        if (!ways.Contains(vv)) ways.Add(vv);
                    }
                }
            }

            return ways;
        }

        private class Position
        {
            public int x { get; internal set; }
            public int y { get; internal set;  }

            public override string ToString()
            {
                return $"({x},{y})";
            }
        }

        /// <summary>
        /// Queries the coordinates of the PlanItem instance which is
        /// leaned against the side describes by `side` (e.g. "a" is left,
        /// "b" is top", "c" is right, and "d" is bottom.
        /// </summary>
        /// <param name="side">Can be `a`, `b`, `c`, or `d`.</param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private Position GetSideXy(char side, int offset)
        {
            int sideX = -1;
            int sideY = -1;

            switch (side)
            {
                case 'a':
                    {
                        sideX = StartCoord().x;
                        sideY = StartCoord().y + offset;
                    }
                    break;

                case 'b':
                    {
                        sideX = StartCoord().x + offset;
                        sideY = StartCoord().y;
                    }
                    break;

                case 'c':
                    {
                        sideX = StartCoord().x + Width() - 1;
                        sideY = StartCoord().y + offset;
                    }
                    break;

                case 'd':
                    {
                        sideX = StartCoord().x + offset;
                        sideY = StartCoord().y + Height() - 1;
                    }
                    break;
            }

            return new Position { x = sideX, y = sideY };
        }

        /// <summary>
        /// Returns the PlanItem which the logical correct items
        /// on the side described by `plan`. In most cases `p`
        /// is the instance returned by `GetSideXy()`.
        /// </summary>
        /// <see cref="GetSideXy"/>
        /// <param name="side"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private PlanItem GetBorderItem(char side, Position p)
        {
            switch (side)
            {
                case 'a':
                    {
                        var x_in = p.x - 1;
                        var y_in = p.y;
                        return Ctx.Get(x_in, y_in);
                    }

                case 'b':
                    {
                        var x_in = p.x;
                        var y_in = p.y - 1;
                        return Ctx.Get(x_in, y_in);
                    }

                case 'c':
                    {
                        var x_in = p.x + 1;
                        var y_in = p.y;
                        return Ctx.Get(x_in, y_in);
                    }

                case 'd':
                    {
                        var x_in = p.x;
                        var y_in = p.y + 1;
                        return Ctx.Get(x_in, y_in);
                    }
            }

            return null;
        }

        public List<Path> GetAllowedPath()
        {
            return GetAllowedPath(out _);
        }

        private void ApplySideInformation(ref Path pp, char from, char to)
        {
            switch (from)
            {
                case 'a': pp.FromSide = Path.Side.Left; break;
                case 'b': pp.FromSide = Path.Side.Top; break;
                case 'c': pp.FromSide = Path.Side.Right; break;
                case 'd': pp.FromSide = Path.Side.Bottom; break;
            }

            switch (to)
            {
                case 'a': pp.ToSide = Path.Side.Left; break;
                case 'b': pp.ToSide = Path.Side.Top; break;
                case 'c': pp.ToSide = Path.Side.Right; break;
                case 'd': pp.ToSide = Path.Side.Bottom; break;
            }
        }

        /// <summary>
        /// connectors[0] := start connector
        /// connectors[1] := target connector
        /// </summary>
        /// <param name="connectors"></param>
        /// <returns></returns>
        public List<Path> GetAllowedPath(out PlanItem[] connectors)
        {
            connectors = null;

            var ways = GetAllPossibleWays();

            var res = new List<Path>();

            foreach (var w in ways)
            {
                if (string.IsNullOrEmpty(w)) continue;
                var containsInt = w.Any(char.IsDigit);
                if (containsInt)
                {
                    var parts = w.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        continue; // incorrect format
                    var from = parts[0].Trim();
                    var to = parts[1].Trim();
                    if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
                        continue; // incorrect format

                    try
                    {
                        var fromSide = from[0];
                        var fromOffset = int.Parse(from.Substring(1));
                        if (!(fromSide >= 'a' && fromSide <= 'd'))
                            continue; // incorrect format

                        var toSide = to[0];
                        var toOffset = int.Parse(to.Substring(1));
                        if (!(toSide >= 'a' && toSide <= 'd'))
                            continue; // incorrect format

                        var fromSideCoord = GetSideXy(fromSide, fromOffset);
                        var toSideCoord = GetSideXy(toSide, toOffset);

                        var fromPreviousItem = GetBorderItem(fromSide, fromSideCoord);
                        var toNextItem = GetBorderItem(toSide, toSideCoord);

                        if (this.IsBlock)
                        {
                            if (toNextItem == null) continue;
                        }
                        else
                        {
                            if (fromPreviousItem == null) continue;
                            if (toNextItem == null) continue;
                        }

                        var pp = new Path
                        {
                            From = fromPreviousItem,
                            To = toNextItem
                        };

                        if(IsSwitch)
                            ApplySideInformation(ref pp, fromSide, toSide);

                        res.Add(pp);

                        // NOTICE(1) -- check previous note
                        if (this.IsSwitch)
                        {
                            var ppReverse = new Path
                            {
                                From = toNextItem,
                                To = fromPreviousItem
                            };

                            ApplySideInformation(ref ppReverse, toSide, fromSide);

                            res.Add(ppReverse);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("GetAllowedPath(): " + ex.Message);
                    }
                }
                else
                {
                    if (w.Length < 2)
                        continue; // single target/source not supported

                    if (w.EndsWith("+", StringComparison.OrdinalIgnoreCase))
                    {
                        if(editor.connectorId == 1)
                        {
                            Trace.WriteLine("Connector id with 1 are ignored.");
                            continue;
                        }

                        var connectorItems = Ctx.GetConnectors(editor.connectorId);
                        if (connectorItems.Length != 2)
                            continue; // we need two elements with the same connectorId

                        PlanItem startConnector = null;
                        PlanItem targetConnector = null;
                        if (identifier.Equals(connectorItems[0].identifier))
                        {
                            startConnector = connectorItems[0];
                            targetConnector = connectorItems[1];
                        }
                        else if (identifier.Equals(connectorItems[1].identifier))
                        {
                            startConnector = connectorItems[1];
                            targetConnector = connectorItems[0];
                        }
                        if (startConnector == null)
                            continue; // wrong connectors found
                        if (targetConnector == null)
                            continue; // invalid configuration

                        // from looks like "A+"
                        // to looks like "B+"

                        var from = w[0];

                        // get target themeDimIdx 
                        var targetRoutes = targetConnector.GetDimensionRoutes();
                        if (targetRoutes.Count != 1)
                            continue; // invalid configuration; only one route for connector allowed
                        var targetMainRoute = targetRoutes.First().ToLower();
                        var to = targetMainRoute[0] == '+' ? targetMainRoute[1] : targetMainRoute[0];

                        var fromPreviousItem = GetBorderItem(from, new Position
                        {
                            x = coord.x,
                            y = coord.y
                        });

                        var toNextItem = GetBorderItem(to, new Position
                        {
                            x = targetConnector.coord.x,
                            y = targetConnector.coord.y
                        });

                        if (fromPreviousItem == null) continue;
                        if (toNextItem == null) continue;

                        connectors = new[] { startConnector, targetConnector };

                        var pp0 = new Path { From = fromPreviousItem, To = toNextItem };
                        var pp1 = new Path { From = toNextItem, To = fromPreviousItem };

                        res.Add(pp0);
                        res.Add(pp1);
                    }
                    else
                    {
                        var from = w[0];
                        var to = w[1];

                        if (from == to)
                            continue; // incorrect format

                        var fromPreviousItem = GetBorderItem(from, new Position
                        {
                            x = coord.x,
                            y = coord.y
                        });

                        var toNextItem = GetBorderItem(to, new Position
                        {
                            x = coord.x,
                            y = coord.y
                        });

                        if (fromPreviousItem == null) continue;
                        if (toNextItem == null) continue;

                        var pp = new Path
                        {
                            From = fromPreviousItem,
                            To = toNextItem
                        };

                        if (IsSwitch)
                            ApplySideInformation(ref pp, from, to);

                        res.Add(pp);
                    }
                }
            }

            return res;
        }

    }
}
