// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Entry.cs

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ecoslib.Statistics
{
    public class Entry
    {
        [JsonIgnore]
        public bool IsStarted { get; set; }
        [JsonIgnore]
        public DateTime Started { get; set; }

        [JsonProperty(PropertyName = "objectId")]
        public int ObjectId { get; set; }

        [JsonProperty(PropertyName = "durations")]
        public List<Duration> Durations { get; set; } = new();

        [JsonProperty(PropertyName = "touches")]
        public List<Touch> Touches { get; set; } = new();
    }
}
