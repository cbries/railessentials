// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AverageDurations.cs

using System;
using System.Collections.Generic;

namespace railessentials.LocomotivesDuration
{
    public class AverageDurations : Dictionary<string, List<AverageDurationEntry>>
    {
        public double GetDuration(int locomotiveId, string blockId, double defaultValue = 10.0)
        {
            if (locomotiveId <= 0) return defaultValue;
            if (string.IsNullOrEmpty(blockId)) return defaultValue;
            if (!ContainsKey($"{locomotiveId}")) return defaultValue;
            var entries = this[$"{locomotiveId}"];
            if (entries == null || entries.Count == 0) return defaultValue;
            foreach(var it in entries)
            {
                if (string.IsNullOrEmpty(it.BlockId)) continue;
                if (it.BlockId.Equals(blockId, StringComparison.OrdinalIgnoreCase))
                    return it.Duration;
            }
            return defaultValue;
        }
    }

    public class AverageDurationEntry
    {
        public string BlockId { get; set; }
        public double Duration { get; set; }
    }
}
