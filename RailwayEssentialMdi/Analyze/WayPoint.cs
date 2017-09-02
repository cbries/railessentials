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

namespace RailwayEssentialMdi.Analyze
{
    public class WayPoint
    {
        public int ThemeId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Orientation { get; set; }
        public bool HasTurn { get; set; }

        public JObject ToJson()
        {
            JObject o = new JObject
            {
                ["themeId"] = ThemeId,
                ["x"] = X,
                ["y"] = Y,
                ["orientation"] = Orientation,
                ["hasTurn"] = HasTurn
            };
            return o;
        }

        public bool Parse(JToken tkn)
        {
            if (tkn == null)
                return false;

            try
            {
                JObject o = tkn as JObject;
                if (o == null)
                    return false;

                if (o["themeId"] != null)
                    ThemeId = (int) o["themeId"];

                if (o["x"] != null)
                    X = (int) o["x"];

                if (o["y"] != null)
                    Y = (int) o["y"];

                if (o["orientation"] != null)
                    Orientation = (int) o["orientation"];

                if (o["hasTurn"] != null)
                    HasTurn = (bool) o["hasTurn"];

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
