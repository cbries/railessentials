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
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RailwayEssentialCore;

namespace TrackPlanParser
{
    public class Track : List<TrackInfo>, ITrackEdit
    {
        private string GenerateItemName(string basename)
        {
            var usedNames = new List<string>();
            for (int i = 0; i < this.Count; ++i)
            {
                if(!string.IsNullOrEmpty(this[i].Name))
                    usedNames.Add(this[i].Name);
            }
            for (int i = 1; i < 9999; ++i)
            {
                string testname = $"{basename}#{i}";
                if (!usedNames.Contains(testname))
                    return testname;
            }
            return null;
        }

        public void Remove(int x, int y)
        {
            var item = Get(x, y);
            base.Remove(item);
        }

        public void ChangeSymbol(int x, int y, int themeId)
        {
            var item = Get(x, y);

            if (item != null)
            {
                item.ThemeId = themeId;
            }
            else
            {
                if (themeId <= 0)
                {
                    Remove(x, y);
                    return;
                }

                if (x < 0 || y < 0)
                {
                    Remove(x, y);
                    return;
                }

                string basename = Globals.GetThemeTypeName(themeId);

                Add(new TrackInfo
                {
                    Name = GenerateItemName(basename),
                    Description = "",
                    ThemeId = themeId,
                    Orientation = "rot0",
                    X = x,
                    Y = y
                });

            }
        }

        public void RotateSymbol(int x, int y, string orientation)
        {
            var item = Get(x, y);
            if (item != null)
                item.Orientation = orientation;
        }

        public JArray GetJson()
        {
            JArray ar = new JArray();
            foreach (var item in this)
            {
                TrackInfo info = item as TrackInfo;
                if (info == null)
                    continue;

                if (item.ThemeId <= 0)
                    continue;

                if (info.X < 0 || info.Y < 0)
                    continue;

                ar.Add(info.ToObject());

            }
            return ar;
        }

        public TrackInfo Get(int x, int y)
        {
            if (x < 0)
                return null;
            if (y < 0)
                return null;

            foreach (var e in this)
            {
                if (e == null)
                    continue;

                if (e.X == x && e.Y == y)
                    return e;
            }

            return null;
        }
    }
}
