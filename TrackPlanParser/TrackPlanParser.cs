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
using Newtonsoft.Json.Linq;

namespace TrackPlanParser
{
    public class TrackPlanParser
    {
        private readonly string _trackFilepath;

        public List<string> Heads { get; set; }

        public Track Track { get; private set; }

        public TrackPlanParser(string filepath)
        {
            _trackFilepath = filepath;
            Heads = new List<string>();
            Track = new Track();
        }

        public bool Parse()
        {
            if (!File.Exists(_trackFilepath))
                return false;

            try
            {
                string cnt = File.ReadAllText(_trackFilepath);
                if (string.IsNullOrEmpty(cnt))
                    return false;

                JArray ar = JArray.Parse(cnt);
                if (ar == null || ar.Count <= 0)
                    return true;

                foreach (var o in ar)
                {
                    JObject oo = o as JObject;
                    if (oo == null)
                        continue;

                    var info = new TrackInfo();
                    info.Parse(oo);
                    Track.Add(info);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("<Parser> " + ex.Message);
                return false;
            }

            return true;
        }
    }
}
