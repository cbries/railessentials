// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Touches.cs

using System;
using Newtonsoft.Json;

namespace ecoslib.Statistics
{
    public class Touch
    {
        [JsonProperty(PropertyName = "dateTime")]
        public DateTime DateTime { get; set; }
    }
}
