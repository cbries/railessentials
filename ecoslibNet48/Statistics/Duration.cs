// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Class1.cs

using System;
using Newtonsoft.Json;

namespace ecoslib.Statistics
{
    public class Duration
    {
        [JsonProperty(PropertyName = "start")]
        public DateTime Start { get; set; }
        
        [JsonProperty(PropertyName = "stop")]
        public DateTime Stop { get; set; }
    }
}
