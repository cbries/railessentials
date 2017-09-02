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
using RailwayEssentialCore;

namespace RailwayEssentialMdi.Analyze
{
    public class WayPoints : List<MapItem>
    {
        public Map Ctx { get; private set; }

        public WayPoints(Map ctx, string path)
        {
            Ctx = ctx;
            var parts = path.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pp in parts)
            {
                if (string.IsNullOrEmpty(pp))
                    continue;

                bool hasTurn = false;

                var m = pp.Trim();
                if (m.EndsWith(">", StringComparison.OrdinalIgnoreCase))
                {
                    hasTurn = true;
                    m = m.TrimEnd('>');
                }

                m = m.Trim().TrimStart('(').TrimEnd(')').Trim();
                if (string.IsNullOrEmpty(m))
                    continue;
                var mparts = m.Split(',');
                int x, y;
                if (!int.TryParse(mparts[0], out x))
                    x = -1;
                if (!int.TryParse(mparts[1], out y))
                    y = -1;

                var item = Ctx.Get(x, y);

                if (item != null)
                {
                    if (Globals.SwitchIds.Contains(item.ThemeId) && hasTurn)
                    {
                        var cpy = item.ShallowCopy();
                        cpy.HasTurn = hasTurn;
                        Add(cpy);
                    }
                    else
                    {
                        Add(item);
                    }
                }
            }
        }

        public Route ToRoute()
        {
            lock (this)
            {
                var wps = new Route();
                foreach (var item in this)
                {
                    if (item == null)
                        continue;

                    if (item.ThemeId == -1)
                        continue;

                    WayPoint wp = new WayPoint
                    {
                        X = item.Info.X, 
                        Y = item.Info.Y,
                        ThemeId = item.ThemeId,
                        Orientation = Helper.GetOrientation(item.Info),
                        HasTurn = item.HasTurn
                    };

                    wps.Add(wp);
                }
                return wps;
            }
        }
    }
}
