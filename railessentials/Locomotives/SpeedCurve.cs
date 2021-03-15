// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: SpeedCurve.cs

using System;
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

        public int GetIndexOfNearestSpeed(int speed)
        {
            if (speed <= 0) return 0;
            var iMax = Steps.Count;
            for(var i=0; i < iMax - 1; ++i)
            {
                var s0 = Steps[i].Speed;
                var s1 = Steps[i + 1].Speed;

                if (speed >= s0 && speed < s1)
                    return i;
            }
            return 0;
        }
    }

    public class SpeedStep  
    {
        [JsonProperty(PropertyName = "speed")]
        public float Speed { get; set; }

        [JsonProperty(PropertyName = "timeStep")]
        public float TimeStep { get; set; }
    }
}
