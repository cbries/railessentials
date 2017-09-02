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
using Newtonsoft.Json.Linq;

namespace RailwayEssentialMdi.DataObjects
{
    public class ProjectTrackView
    {
        public string Name { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public bool Show { get; set; }

        public bool Parse(JToken tkn)
        {
            var o = tkn as JObject;
            if (o == null)
                return false;

            try
            {
                if (o["name"] != null)
                    Name = o["name"].ToString();

                if (o["startX"] != null)
                    StartX = (int) o["startX"];

                if (o["startY"] != null)
                    StartY = (int) o["startY"];

                if (o["show"] != null)
                    Show = (bool) o["show"];

                return true;
            }
            catch
            {
                return false;
            }
        }

        public JObject ToJson()
        {
            JObject o = new JObject
            {
                ["name"] = Name,
                ["startX"] = StartX,
                ["startY"] = StartY,
                ["show"] = Show
            };
            return o;
        }

    }
}
