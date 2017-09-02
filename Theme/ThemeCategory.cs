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

namespace Theme
{
    public class ThemeCategory
    {
        public string Name { get; set; }

        public List<ThemeItem> Objects { get; set; }

        public ThemeCategory()
        {
            Objects = new List<ThemeItem>();
        }

        public bool Parse(JToken tkn)
        {
            var o = tkn as JObject;
            if (o == null)
                return false;

            if (o["category"] != null)
                Name = o["category"].ToString();

            if (o["objects"] != null)
            {
                JArray ar = o["objects"] as JArray;
                if (ar == null)
                    return true;

                foreach (var e in ar)
                {
                    if (e == null)
                        continue;

                    var item = new ThemeItem();
                    if (item.Parse(e))
                        Objects.Add(item);
                }
            }

            return true;
        }
    }
}
