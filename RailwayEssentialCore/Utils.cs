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
using System.IO;

namespace RailwayEssentialCore
{
    public static class Utils
    {
        public const string ThemeName = @"\Themes\RailwayEssential";
        public const string TrackplansEditor = @"\Trackplans\Webeditor";
        
        public static string ExpandRailwayEssential(this string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var p = Directory.GetCurrentDirectory();
            var pp = path.TrimStart('/', '\\');

            return Path.Combine(p, pp);
        }

        public static string GenerateUniqueName(this string fmt, string dirname=null)
        {
            for (int i = 0; i < 1000; ++i)
            {
                if (!string.IsNullOrEmpty(dirname))
                {
                    var name = Path.Combine(dirname, string.Format(fmt, i));
                    if (!File.Exists(name))
                        return name;
                }
                else
                {
                    var name = string.Format(fmt, i);
                    if (!File.Exists(name))
                        return name;
                }
            }

            return null;
        }

    }
}
