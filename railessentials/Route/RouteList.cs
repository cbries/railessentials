// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RouteList.cs

using System;
using System.Collections.Generic;
using railessentials.Analyzer;

namespace railessentials.Route
{
    public class RouteList : List<Route>
    {
        private RouteList GetRoutesWith(int x, int y)
        {
            var res = new RouteList();

            foreach (var it in this)
            {
                foreach (var itt in it.Tracks)
                {
                    if (itt.x != x) continue;
                    if (itt.y != y) continue;

                    res.Add(it);

                    break;
                }
            }

            return res;
        }

        public RouteList GetCrossingRoutesOf(Route route)
        {
            var res = new RouteList();

            foreach(var itTrack in route.Tracks)
            {
                if (itTrack == null) continue;
                var x = itTrack.x;
                var y = itTrack.y;

                var r = GetRoutesWith(x, y);
                foreach (var itR in r)
                {
                    var isAlreadyAdded = false;
                    foreach(var itt in res)
                    {
                        isAlreadyAdded = itt.Name.Equals(itR.Name, StringComparison.OrdinalIgnoreCase);
                        if (isAlreadyAdded) break;
                    }

                    if (!isAlreadyAdded)
                        res.Add(itR);
                }
            }

            return res;
        }

        public RouteList GetRoutesWithFromBlock(string fromBlock, SideMarker sideToLeave, bool onlyEnabled)
        {
            var res = new RouteList();

            foreach (var it in this)
            {
                var from = it?.Blocks?[0];
                if (from == null) continue;
                var id = from.identifier;
                if (string.IsNullOrEmpty(id)) continue;
                if (!id.Equals(fromBlock, StringComparison.Ordinal)) continue;
                if (from.side != sideToLeave) continue;

                res.Add(it);
            }

            if(onlyEnabled)
                return GetRoutesEnabled(res);

            return res;
        }

        public static RouteList GetRoutesEnabled(RouteList listOfRoutes)
        {
            var res = new RouteList();
            foreach(var it in listOfRoutes)
            {
                if (it == null) continue;
                if (it.IsDisabled) continue;
                res.Add(it);
            }
            return res;
        }

        public Route GetByName(string routeName)
        {
            if (string.IsNullOrEmpty(routeName)) return null;

            foreach (var it in this)
            {
                if (string.IsNullOrEmpty(it?.Name)) continue;
                if (it.Name.Equals(routeName, StringComparison.OrdinalIgnoreCase))
                    return it;
            }
            
            return null;
        }
    }
}