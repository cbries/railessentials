// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Data.cs

using System.Collections.Generic;

namespace railessentials.Locomotives
{
    public class Data
    {
        public bool IsStopped { get; set; }
        public bool IsLocked { get; set; }
        public List<Duration> Durations { get; set; }
        public Dictionary<string, bool> Settings { get; set; } = new();
        public string EnterBlockSide { get; set; }
        public SpeedCurve SpeedCurve { get; set; }
    }
}
