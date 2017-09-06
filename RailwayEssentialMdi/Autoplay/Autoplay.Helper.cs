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
using RailwayEssentialMdi.Analyze;
using TrackInformation;
using TrackWeaver;
using Route = RailwayEssentialMdi.Analyze.Route;

namespace RailwayEssentialMdi.Autoplay
{
    public partial class Autoplay
    {
        public void SetRoute(Analyze.Route route, bool state)
        {
            if (route == null)
                return;

            var n = route.Count;

            var trackEntity = Ctx.TrackEntity;

            for (int idx = 1; idx < n - 1; ++idx)
            {
                var r = route[idx];
                if (r == null)
                    continue;

                if (trackEntity?.Viewer != null)
                {
                    string themeIcon = null;

                    var trackInfo = trackEntity.Track.Get(r.X, r.Y);
                    if (trackInfo != null)
                    {
                        var themeInfo = Theme?.Get(trackInfo.ThemeId);
                        if (themeInfo != null)
                        {
                            TrackCheckResult checkResult = null;
                            TrackInformation.S88 s88item = null;

                            var w = Ctx.Dispatcher.Weaver;
                            var objItems = w.GetObject(trackInfo);
                            if (objItems != null && objItems.Count > 0)
                            {
                                s88item = objItems[0] as S88;
                                var checkFnc = w.GetCheckFnc(s88item, trackInfo);
                                if (checkFnc != null)
                                    checkResult = checkFnc();
                            }

                            if (checkResult == null)
                            {
                                if (state)
                                {
                                    if (Route.HasPoint(route, trackInfo.X, trackInfo.Y))
                                        themeIcon = themeInfo.Off.Route;
                                    else
                                        themeIcon = themeInfo.Off.Default;
                                }
                                else
                                {
                                    themeIcon = themeInfo.Off.Default;
                                }
                            }
                            else
                            {
                                bool rS88 = checkResult?.State != null && checkResult.State.Value;

                                if (rS88 && s88item != null)
                                {
                                    if (s88item.IsRouted)
                                        themeIcon = themeInfo.Active.Route;
                                    else
                                        themeIcon = themeInfo.Active.Occ;
                                }
                                else
                                {
                                    if (s88item != null && s88item.IsRouted)
                                    {
                                        themeIcon = themeInfo.Off.Route;
                                    }
                                    else
                                    {
                                        if (state && Route.HasPoint(route, trackInfo.X, trackInfo.Y))
                                            themeIcon = themeInfo.Off.Route;
                                        else
                                            themeIcon = themeInfo.Off.Default;
                                    }
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(themeIcon))
                    {
                        var x = trackInfo.X;
                        var y = trackInfo.Y;
                        var themeId = trackInfo.ThemeId;
                        var orientation = trackInfo.Orientation;
                        var symbol = themeIcon;

                        var isSwitch = RailwayEssentialCore.Globals.SwitchIds.Contains(themeId);

                        if (r.HasTurn && isSwitch)
                        {
                            var parts = symbol.Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
                            if(parts.Length == 2)
                                symbol = parts[0] + "-t-" + parts[1];
                            else if (parts.Length == 1)
                                symbol = parts[0] + "-t";
                        }

                        Ctx.ExecuteJs($"changeSymbol({x}, {y}, {themeId}, \"{orientation}\", \"{symbol}\");");
                    }
                }
            }
        }

        private AutoplayRouteThread GetByRoute(Analyze.Route route)
        {
            foreach (var thread in _blockRouteThreads)
            {
                if (thread == null)
                    continue;

                if (thread.Route == route)
                    return thread;
            }

            return null;
        }

        internal int GetLocObjectIdOfRoute(Analyze.Route route, bool destination = false)
        {
            if (!destination)
            {
                var startPoint = route.First();
                if (startPoint != null)
                {
                    var startItem = Ctx.TrackEntity.Track.Get(startPoint.X, startPoint.Y);
                    if (startItem != null)
                    {
                        var locObjectId = startItem.GetLocomotiveObjectId();
                        if (locObjectId != -1)
                            return locObjectId;
                    }
                }
            }
            else
            {
                var endPoint = route.Last();
                if (endPoint != null)
                {
                    var endItem = Ctx.TrackEntity.Track.Get(endPoint.X, endPoint.Y);
                    if (endItem != null)
                    {
                        var locObjectId = endItem.GetLocomotiveObjectId();
                        if (locObjectId != -1)
                            return locObjectId;
                    }
                }
            }

            return -1;
        }

        private List<RouteGroup> GetBlockGroupsOfRoute(Route route)
        {
            if(route == null)
                return new List<RouteGroup>();

            List<RouteGroup> grps = new List<RouteGroup>();
            foreach (var grp in GetFreeBlockGroups())
            {
                if (grp == null)
                    continue;

                foreach (var r0 in grp.Routes)
                {
                    if (r0 == route)
                    {
                        grps.Add(grp);
                        break;
                    }
                }
            }

            return grps;
        }

        private List<RouteGroup> GetFreeBlockGroups()
        {
            List<Route> busyRoutes = new List<Route>();
            foreach (var r in Ctx.Project.BlockRoutes)
            {
                if (r == null)
                    continue;
                if (r.IsBusy)
                    busyRoutes.Add(r);
            }

            List<RouteGroup> grps = new List<RouteGroup>();
            foreach (var grp in Ctx.Project.BlockRouteGroups)
            {
                if (grp == null)
                    continue;

                foreach (var r0 in grp.Routes)
                {
                    foreach (var r1 in busyRoutes)
                    {
                        if(Route.Cross(r0, r1, true))
                            goto Outer;
                    }
                }

                grps.Add(grp);

                Outer:
                    continue;
            }
            
            return grps;
        }
    }
}
