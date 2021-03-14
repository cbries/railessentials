// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: SpeedCurve.cs

using System.Collections.Generic;
using Newtonsoft.Json;

namespace railessentials.Locomotives
{
    public class SpeedCurve
    {
        [JsonProperty(PropertyName = "maxSpeed")]
        public int MaxSpeed { get; set; }

        [JsonProperty(PropertyName = "maxTime")]
        public int MaxTime { get; set; }
        
        [JsonProperty(PropertyName = "steps")]
        public List<SpeedStep> Steps { get; set; }
    }

    public class SpeedStep  
    {
        [JsonProperty(PropertyName = "speed")]
        public float Speed { get; set; }

        [JsonProperty(PropertyName = "timeStep")]
        public float TimeStep { get; set; }
    }
}
