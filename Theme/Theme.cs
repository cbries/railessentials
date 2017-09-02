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
using Newtonsoft.Json.Linq;

namespace Theme
{
    public class Theme
    {
        public string ThemeName { get; private set; }
        public string ThemeDirectory { get; private set; }

        private List<string> _categoryNames;

        public List<string> CategoryNames
        {
            get
            {
                if (Categories == null || Categories.Count <= 0)
                    return null;

                if (_categoryNames == null)
                {
                    _categoryNames = new List<string>();

                    foreach (var e in Categories)
                    {
                        if (e == null)
                            continue;
                        if (string.IsNullOrEmpty(e.Name))
                            continue;

                        _categoryNames.Add(e.Name);
                    }
                }

                return _categoryNames;
            }
        }

        // [key:=name, value:=physical name]
        public Dictionary<string, string> GetDefaultForCategory(string catname)
        {
            Dictionary<string, string> items = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(catname))
                return items;

            foreach (var e in Categories)
            {
                if (e == null)
                    continue;

                if (e.Name.Equals(catname, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var ee in e.Objects)
                    {
                        if (ee == null)
                            continue;

                        var name = ee.Name;
                        var physicalName = ee.Off.Default;

                        if (items.ContainsKey(name))
                            items[name] = physicalName;
                        else
                            items.Add(name, physicalName);
                    }
                }
            }

            return items;
        }

        public List<ThemeCategory> Categories { get; set; }

        public ThemeItem Get(int themeItemId)
        {
            if (themeItemId <= 0)
                return null;

            foreach (var e in Categories)
            {
                if (e == null)
                    continue;
                foreach (var ee in e.Objects)
                {
                    if (ee == null)
                        continue;

                    if (ee.UniqueIdentifier == themeItemId)
                        return ee;
                }
            }

            return null;
        }

        public ThemeItem Get(string catname, string symbolname)
        {
            if (string.IsNullOrEmpty(catname) || string.IsNullOrEmpty(symbolname))
                return null;

            foreach (var e in Categories)
            {
                if (e == null)
                    continue;

                if (!e.Name.Equals(catname, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var ee in e.Objects)
                {
                    if (ee == null)
                        continue;

                    if (ee.Off.Default.Equals(symbolname, StringComparison.OrdinalIgnoreCase))
                        return ee;
                }
            }

            return null;
        }

        public Theme()
        {
            Categories = new List<ThemeCategory>();
        }

        public bool Load(string themeJsonFilePath)
        {
            ThemeName = null;
            ThemeDirectory = null;

            if (string.IsNullOrEmpty(themeJsonFilePath))
                return false;
            if (!File.Exists(themeJsonFilePath))
                return false;

            try
            {
                string cnt = File.ReadAllText(themeJsonFilePath, Encoding.UTF8);
                if (string.IsNullOrEmpty(cnt))
                    return false;

                JArray ar = JArray.Parse(cnt);
                foreach (var e in ar)
                {
                    if (e == null)
                        continue;

                    var cat = new ThemeCategory();
                    if (cat.Parse(e))
                        Categories.Add(cat);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("<Theme> " + ex.Message);
                return false;
            }

            ThemeName = Path.GetFileNameWithoutExtension(themeJsonFilePath);
            ThemeDirectory = Path.GetDirectoryName(themeJsonFilePath);

            return true;
        }
    }
}
