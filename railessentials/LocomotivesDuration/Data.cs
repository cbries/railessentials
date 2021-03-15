// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Data.cs

using System.Collections.Generic;

namespace railessentials.LocomotivesDuration
{
    public class Data
    {
        public const int MaxDecelerateEntries = 25;

        public List<Duration> Durations { get; set; } = new();

        /// <summary>
        /// key := BlockId, e.g. "B01[+]"
        /// </summary>
        public Dictionary<string, List<Duration>> DecelerateDurations { get; set; } = new();
    }
}
