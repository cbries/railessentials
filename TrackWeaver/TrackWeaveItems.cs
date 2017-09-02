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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackWeaver
{
    public class TrackWeaveItems
    {
        private readonly List<TrackWeaveItem> _items = new List<TrackWeaveItem>();

        private string _filePath;

        public List<TrackWeaveItem> Items => _items;

        public TrackWeaveItems()
        {
            
        }

        public bool Load(string filePath)
        {
            _filePath = filePath;

            if (!File.Exists(filePath))
                return false;

            string cnt = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrEmpty(cnt))
                return true;

            try
            {
                JArray ar = JArray.Parse(cnt);

                if (ar == null)
                    return true;

                foreach (var o in ar)
                {
                    JObject oo = o as JObject;
                    if (oo == null)
                        continue;

                    TrackWeaveItem item = new TrackWeaveItem();
                    if(item.Parse(oo))
                        _items.Add(item);
                }

                return true;
            }
            catch(Exception ex)
            {
                Trace.WriteLine("<Exception> " + ex.Message);
                return false;
            }
        }

        public bool Save()
        {
            if (string.IsNullOrEmpty(_filePath))
                return false;

            try
            {
                JArray ar = new JArray();
                foreach (var e in _items)
                {
                    if (e == null)
                        continue;

                    ar.Add(e.ToJson());
                }
                File.WriteAllText(_filePath, ar.ToString(Formatting.Indented), Encoding.UTF8);
                return true;
            }
            catch(Exception ex)
            {
                Trace.WriteLine("<Error> " + ex.Message);
                return false;
            }
        }
    }
}
