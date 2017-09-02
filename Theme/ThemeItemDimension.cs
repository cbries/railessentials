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

namespace Theme
{
    public class ThemeItemDimension
    {
        public string Value { get; set; }

        public int X { get; set; }
        public int Y { get; set; }

        public bool Parse(JToken tkn)
        {
            var v = tkn.ToString();
            if (string.IsNullOrEmpty(v))
                return false;

            Value = v;

            var parts = Value.Split(new[] {'x'});

            X = 1;
            Y = 1;

            if (parts.Length == 2)
            {
                int vv;
                if (int.TryParse(parts[0], out vv))
                    X = vv;
                if (int.TryParse(parts[1], out vv))
                    Y = vv;
            }

            return true;
        }

    }
}
