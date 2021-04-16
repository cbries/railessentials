// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Data.cs

using System;
using System.Collections.Generic;

namespace railessentials.Locomotives
{
    public class Data
    {
        public bool IsCleaner { get; set; } = false;
        public bool IsStopped { get; set; }
        public bool IsLocked { get; set; }
        public Dictionary<string, bool> Settings { get; set; } = new();
        public string EnterBlockSide { get; set; }
        public SpeedCurve SpeedCurve { get; set; }
        public Dictionary<string, SpeedLevel> SpeedLevels { get; set; } = new();

        public SpeedLevel GetLevel(string level)
        {
            var def = new SpeedLevel { Value = 0 };

            if (string.IsNullOrEmpty(level))
                return def;

            if (SpeedLevels == null) return def;

            foreach(var it in SpeedLevels)
            {
                if (it.Key.IndexOf(level, StringComparison.OrdinalIgnoreCase) != -1)
                    return it.Value;
            }

            return def;
        }
    }
}
