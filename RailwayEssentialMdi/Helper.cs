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
using System.IO;
using System.Windows;
using TrackPlanParser;
using TrackWeaver;

namespace RailwayEssentialMdi
{
    internal class Helper
    {
        public static int GetOrientation(TrackInfo info)
        {
            if (info == null)
                return 0;
            if (string.IsNullOrEmpty(info.Orientation))
                return 0;
            if (info.Orientation.Equals("rot0", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (info.Orientation.Equals("rot90", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (info.Orientation.Equals("rot180", StringComparison.OrdinalIgnoreCase))
                return 2;
            if (info.Orientation.Equals("rot-90", StringComparison.OrdinalIgnoreCase))
                return 3;
            return 0;
        }

        public static List<TrackInformationCore.IItem> GetObjects(Dispatcher.Dispatcher dispatcher, Track track, int x, int y
        )
        {
            var trackInfo = track.Get(x, y);

            if (trackInfo == null)
                return null;

            var items = new List<TrackInformationCore.IItem>();

            var weaver = dispatcher.Weaver;
            if (weaver != null)
            {
                var ws = weaver.WovenSeam;
                if (ws != null)
                {
                    foreach (var seam in ws)
                    {
                        if (seam == null)
                            continue;

                        foreach (TrackInfo key in seam.TrackObjects.Keys)
                        {
                            if (key == null)
                                continue;

                            if (key.X == trackInfo.X && key.Y == trackInfo.Y && key.ThemeId == trackInfo.ThemeId)
                                items.Add(seam.ObjectItem);
                        }
                    }
                }
            }

            return items;
        }

        /// <summary> Call of GetWeaveItem(..) can be speed up by NOT reading the weave file on any call. </summary>
        /// <param name="dispatcher"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static TrackWeaveItem GetWeaveItem(Dispatcher.Dispatcher dispatcher, int x, int y)
        {
            var m = dispatcher.Model as ViewModels.RailwayEssentialModel;
            if (m == null)
                return null;

            var prj = m.Project;

            var weaveFilepath = Path.Combine(prj.Dirpath, prj.Track.Weave);
            TrackWeaveItems weaverItems = new TrackWeaveItems();
            if (!weaverItems.Load(weaveFilepath))
                return null;

            foreach (var e in weaverItems.Items)
            {
                if (e?.VisuX == x && e.VisuY == y)
                    return e;
            }

            return null;
        }

        public static bool Ask(string promptMsg, string title, string yesText = "Yes", string noText = "No")
        {
            System.Windows.Style style = new System.Windows.Style();
            style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.YesButtonContentProperty, yesText));
            style.Setters.Add(new Setter(Xceed.Wpf.Toolkit.MessageBox.NoButtonContentProperty, noText));
            MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(promptMsg, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes, style);
            if (result == MessageBoxResult.Yes || result == MessageBoxResult.OK)
                return true;
            return false;

        }
    }

    public static class ByteArrayExt
    {
        public static byte[] SetBit(this byte[] self, int index, bool value)
        {
            int byteIndex = index / 8;
            int bitIndex = index % 8;
            byte mask = (byte)(1 << bitIndex);

            self[byteIndex] = (byte)(value ? (self[byteIndex] | mask) : (self[byteIndex] & ~mask));
            return self;
        }

        public static byte[] ToggleBit(this byte[] self, int index)
        {
            int byteIndex = index / 8;
            int bitIndex = index % 8;
            byte mask = (byte)(1 << bitIndex);

            self[byteIndex] ^= mask;
            return self;
        }

        public static bool GetBit(this byte[] self, int index, bool value)
        {
            int byteIndex = index / 8;
            int bitIndex = index % 8;
            byte mask = (byte)(1 << bitIndex);

            return (self[byteIndex] & mask) != 0;
        }
    }
}
