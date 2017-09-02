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

namespace RailwayEssentialMdi.Analyze
{
    public class AnalyzeResult
    {
        public int NumberOfRoutes { get; set; }

        public List<WayPoints> Routes { get; set; }

        public List<RouteGroup> RouteGroups { get; set; }

        public AnalyzeResult()
        {
            NumberOfRoutes = -1;
            Routes = new List<WayPoints>();
            RouteGroups= new List<RouteGroup>();
        }

        public override string ToString()
        {
            string m = "";
            m += $"Number of Routes: {NumberOfRoutes}\r\n";
            if (Routes != null)
            {
                for (int i = 0; i < NumberOfRoutes; ++i)
                {
                    var r = Routes[i];
                    m += $"#{i + 1} (Steps {r.Count}): ";
                    foreach (var rr in r)
                    {
                        if (rr.HasTurn)
                            m += $"{rr.Identifier}>->";
                        else
                            m += $"{rr.Identifier}->";
                    }
                    m += "END\r\n";
                }
            }
            m += $"Number of Groups: {RouteGroups.Count}\r\n";
            foreach (var grp in RouteGroups)
            {
                if (grp == null)
                    continue;
                m += grp + "\r\n";
            }
            return m;
        }
    }
}
