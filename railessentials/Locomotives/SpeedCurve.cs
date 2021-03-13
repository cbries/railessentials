// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: SpeedCurve.cs

using System.Collections.Generic;
using Newtonsoft.Json;

namespace railessentials.Locomotives
{
    public class SpeedCurve
    {
        [JsonProperty(PropertyName = "maxSpeedstep")]
        public int MaxSpeedstep { get; set; }
        [JsonProperty(PropertyName = "maxTimeSeconds")]
        public int MaxTimeSeconds { get; set; }
        [JsonProperty(PropertyName = "steps")]
        public List<SpeedStep> Steps { get; set; }
    }

    public class SpeedStep  
    {
        [JsonProperty(PropertyName = "speed")]
        public int Speed { get; set; }

        [JsonProperty(PropertyName = "timeStep")]
        public float TimeStep { get; set; }
    }
}
