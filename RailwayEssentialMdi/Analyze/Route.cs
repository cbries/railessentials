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
    public class Route : List<WayPoint>
    {
        public bool IsBusy { get; set; }
        public DateTime StartBusiness { get; set; }
        public DateTime StopBusiness { get; set; }

        public static bool Cross(Route r0, Route r1, bool ignoreBlocks = false)
        {
            foreach(var runner0 in r0)
            {
                if (Globals.BlockIds.Contains(runner0.ThemeId))
                    continue;

                foreach(var runner1 in r1)
                {
                    if (Globals.BlockIds.Contains(runner1.ThemeId))
                        continue;

                    if (runner0.X == runner1.X)
                    {
                        if (runner0.Y == runner1.Y)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool HasPoint(Route route, int x, int y)
        {
            if (route == null)
                return false;
            if (x == -1 || y == -1)
                return false;

            foreach (var wp in route)
            {
                if (wp?.X == x && wp.Y == y)
                    return true;
            }

            return true;
        }
    }
}
