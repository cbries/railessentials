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
using TrackInformationCore;
using TrackPlanParser;

namespace TrackWeaver
{
    public class TrackCheckResult
    {
        public enum SwitchDirection { Straight, Turn }
        public SwitchDirection? Direction { get; set; }

        public bool? State { get; set; }
    }

    public class TrackWeaverItem
    {
        public IItem ObjectItem { get; set; }

        public Dictionary<TrackInfo, Func<TrackCheckResult>> TrackObjects { get; set; }
    }

    public class TrackWeaver
    {
        private readonly List<TrackWeaverItem> _associations = new List<TrackWeaverItem>();

        public List<TrackWeaverItem> WovenSeam => _associations;

        private TrackWeaverItem GetItem(IItem item)
        {
            foreach (var e in _associations)
            {
                if (e == null)
                    continue;

                if (e.ObjectItem == item)
                    return e;
            }

            return null;
        }

        public List<IItem> GetObject(TrackInfo trackInfo)
        {
            if (trackInfo == null)
                return null;

            List<IItem> items = new List<IItem>();

            foreach (var assoc in _associations)
            {
                if (assoc == null)
                    continue;

                if (assoc.TrackObjects.ContainsKey(trackInfo))
                    items.Add(assoc.ObjectItem);
            }

            return items;
        }

        public Func<TrackCheckResult> GetCheckFnc(IItem item, TrackInfo trackInfo)
        {
            if (item == null || trackInfo == null)
                return null;

            var e = GetItem(item);
            if (e == null)
                return null;

            if (e.TrackObjects == null || e.TrackObjects.Count == 0)
                return null;

            if (!e.TrackObjects.ContainsKey(trackInfo))
                return null;

            return e.TrackObjects[trackInfo];
        }

        public void Link(IItem item, TrackInfo trackObject, Func<TrackCheckResult> fncCheckState)
        {
            if (item == null || trackObject == null)
                return;

            var e = GetItem(item);
            if (e != null)
            {
                if (e.TrackObjects == null)
                    e.TrackObjects = new Dictionary<TrackInfo, Func<TrackCheckResult>>();

                if (e.TrackObjects.ContainsKey(trackObject))
                    e.TrackObjects[trackObject] = fncCheckState;
                else
                    e.TrackObjects.Add(trackObject, fncCheckState);
            }
            else
            {
                e = new TrackWeaverItem
                {
                    ObjectItem = item,
                    TrackObjects = new Dictionary<TrackInfo, Func<TrackCheckResult>>
                    {
                        {trackObject, fncCheckState}
                    }
                };

                _associations.Add(e);
            }

        }

        public void UnLink(TrackInfo trackObject)
        {
            if (trackObject == null)
                return;

            foreach (var e in _associations)
            {
                if (e == null)
                    continue;

                if (e.TrackObjects.ContainsKey(trackObject))
                    e.TrackObjects.Remove(trackObject);
            }
        }

        public void UnLink(IItem item)
        {
            if (item == null)
                return;

            var e = GetItem(item);
            if (e != null)
                _associations.Remove(e);
        }
    }
}
