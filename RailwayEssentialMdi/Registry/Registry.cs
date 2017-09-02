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
using System.Linq;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace RailwayEssentialMdi.Registry
{
    public class Registry
    {
        private static string SubKeyAppName = "SOFTWARE\\RailwayEssential";
        private static string SubKeyRecentProjects = "Recent";
        private static int MaxRecent = 10;

        private readonly RegistryKey _appKey;

        public List<string> RecentProjects
        {
            get
            {
                if(_appKey == null)
                    return new List<string>();
                var v = _appKey.GetValue(SubKeyRecentProjects) as string;
                if(string.IsNullOrEmpty(v))
                    return new List<string>();
                var parts = v.Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts == null || parts.Length == 0)
                    return new List<string>();
                return parts.ToList();
            }
        }

        public void SetRecent(List<string> recentPaths)
        {
            if(recentPaths == null)
                recentPaths = new List<string>();

            var entries = new List<string>();
            if (recentPaths.Count > MaxRecent)
            {
                for (int i = 0; i < MaxRecent; ++i)
                    entries.Add(recentPaths[i]);
            }
            else
            {
                for(int i=0; i < recentPaths.Count; ++i)
                    entries.Add(recentPaths[i]);
            }

            if (_appKey != null)
                _appKey.SetValue(SubKeyRecentProjects, string.Join(";", entries));
        }

        private RegistryKey GetKey(string name)
        {
            RegistryKey myKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            RegistryKey key = myKey.OpenSubKey(name, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            if (key == null)
                key = myKey.CreateSubKey(name);
            return key;
        }

        public Registry()
        {
            _appKey = GetKey(SubKeyAppName);
        }       
    }
}
