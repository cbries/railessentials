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
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RailwayEssentialCore;
using RailwayEssentialMdi.Analyze;

namespace RailwayEssentialMdi.DataObjects
{
    public class ProjectFile : IPersist
    {
        public string Filepath { get; private set; }
        public string Dirpath { get; private set; }

        public string Name { get; set; }
        public float Version { get; set; }
        public string TargetHost { get; set; }
        public UInt16 TargetPort { get; set; }
        public int DesignerColumns { get; set; }
        public int DesignerRows { get; set; }
        public double ZoomLevel { get; set; }
        public ProjectGamepad Gamepad { get; set; }
        public List<string> Objects { get; set; }
        public ProjectTrack Track { get; set; }
        public List<ProjectTrackView> TrackViews { get; set; }
        public List<Route> BlockRoutes { get; set; }
        public List<RouteGroup> BlockRouteGroups { get; private set; }

        public ProjectFile()
        {
            ZoomLevel = 1.0f;
            Objects = new List<string>();
            Track = new ProjectTrack();
            TrackViews = new List<ProjectTrackView>();
            BlockRoutes = new List<Route>();
            BlockRouteGroups= new List<RouteGroup>();
            Gamepad= new ProjectGamepad();
        }

        public bool Load(string path)
        {
            try
            {
                Filepath = path;
                Dirpath = Path.GetDirectoryName(path);

                if (!File.Exists(path))
                    return false;

                string cnt = File.ReadAllText(path, Encoding.UTF8);

                if (string.IsNullOrEmpty(cnt))
                    return false;

                JObject o = JObject.Parse(cnt);

                if (o["name"] != null)
                    Name = o["name"].ToString();

                if (o["version"] != null)
                {
                    float v;
                    Version = float.TryParse(o["version"].ToString(), out v) ? v : 1.0f;
                }

                if (o["targetHost"] != null)
                    TargetHost = o["targetHost"].ToString();

                if (o["targetPort"] != null)
                {
                    UInt16 v;
                    if (UInt16.TryParse(o["targetPort"].ToString(), out v))
                        TargetPort = v;
                    else
                        TargetPort = 15471;
                }

                if (o["gamepad"] != null)
                {
                    var go = new ProjectGamepad();
                    if (go.Parse(o["gamepad"]))
                        Gamepad = go;
                    else
                        Gamepad = new ProjectGamepad();
                }

                if (o["designerColumns"] != null)
                    DesignerColumns = (int) o["designerColumns"];

                if (o["designerRows"] != null)
                    DesignerRows = (int) o["designerRows"];

                if (o["zoomLevel"] != null)
                    ZoomLevel = (double) o["zoomLevel"];

                if (o["objects"] != null)
                {
                    JArray ar = o["objects"] as JArray;
                    if (ar != null)
                    {
                        foreach (var e in ar)
                        {
                            if (e == null)
                                continue;
                            if (string.IsNullOrEmpty(e.ToString()))
                                continue;
                            Objects.Add(e.ToString());
                        }
                    }
                }

                if (o["track"] != null)
                {
                    JObject ao = o["track"] as JObject;
                    if (ao != null)
                    {
                        var item = new ProjectTrack();
                        if (item.Parse(ao))
                            Track = item;
                    }
                }

                if (o["trackViews"] != null)
                {
                    JArray ar = o["trackViews"] as JArray;
                    if (ar != null)
                    {
                        for (int i = 0; i < ar.Count; ++i)
                        {
                            var obj = new ProjectTrackView();
                            if (obj.Parse(ar[i]))
                                TrackViews.Add(obj);
                        }
                    }
                }

                if (o["routes"] != null || o["blockRoutes"] != null)
                {
                    JArray ar = o["routes"] as JArray;
                    if(ar == null)
                        ar = o["blockRoutes"] as JArray;
                    if (ar != null)
                    {
                        for (int i = 0; i < ar.Count; ++i)
                        {
                            var arr = ar[i] as JArray;
                            if (arr == null)
                                continue;

                            Route route = new Route();

                            for (int j = 0; j < arr.Count; ++j)
                            {
                                WayPoint w = new WayPoint();
                                if (w.Parse(arr[j]))
                                    route.Add(w);
                            }

                            if (route.Count > 0)
                                BlockRoutes.Add(route);
                        }
                    }

                    if (BlockRoutes != null && BlockRoutes.Count > 0)
                        BlockRouteGroups = Map.GetRouteGroups(BlockRoutes);
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("<Project> " + ex.Message);
                return false;
            }
        }

        private JObject ToJson()
        {
            JArray objects = new JArray();
            foreach (var e in Objects)
                objects.Add(e);

            JArray trackViews = new JArray();
            foreach (var e in TrackViews)
                trackViews.Add(e.ToJson());

            JArray routes = new JArray();
            foreach (var r in BlockRoutes)
            {
                if (r == null)
                    continue;

                JArray routePoints = new JArray();
                foreach (var w in r)
                    routePoints.Add(w.ToJson());
                if (routePoints.Count > 0)
                    routes.Add(routePoints);
            }

            JObject o = new JObject
            {
                ["name"] = Name,
                ["version"] = Version,
                ["targetHost"] = TargetHost,
                ["targetPort"] = TargetPort,
                ["gamepad"] = Gamepad.ToJson(),
                ["designerColumns"] = DesignerColumns,
                ["designerRows"] = DesignerRows,
                ["zoomLevel"] = ZoomLevel,
                ["track"] = Track.ToJson(),
                ["trackViews"] = trackViews,
                ["objects"] = objects,
                ["blockRoutes"] = routes
            };

            return o;
        }

        #region IPersist

        public bool Save()
        {
            return Save(Filepath);
        }

        public bool Save(string targetFilepath)
        {
            var cnt = ToJson().ToString(Formatting.Indented);
            try
            {
                File.WriteAllText(targetFilepath, cnt, Encoding.UTF8);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
