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
using TrackPlanParser;

namespace RailwayEssentialMdi.Analyze
{
    public static class AnalyzeHelper
    {
        public static string Reverse(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static bool IsLeft(this TrackInfo item, TrackInfo neighbourItem)
        {
            if (neighbourItem == null)
                return false;
            if (neighbourItem.X - 1 < item.X && neighbourItem.Y == item.Y)
                return true;
            return false;
        }

        public static bool IsRight(this TrackInfo item, TrackInfo neighbourItem)
        {
            if (neighbourItem == null)
                return false;
            if (neighbourItem.X + 1 > item.X && neighbourItem.Y == item.Y)
                return true;
            return false;
        }

        public static bool IsUp(this TrackInfo item, TrackInfo neighbourItem)
        {
            if (neighbourItem == null)
                return false;
            if (neighbourItem.Y - 1 < item.Y && neighbourItem.X == item.X)
                return true;
            return false;
        }

        public static bool IsDown(this TrackInfo item, TrackInfo neighbourItem)
        {
            if (neighbourItem == null)
                return false;
            if (neighbourItem.Y + 1 > item.Y && neighbourItem.X == item.X)
                return true;
            return false;
        }
    }
}