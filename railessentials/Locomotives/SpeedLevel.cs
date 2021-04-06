// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: SpeedLevel.cs

using Newtonsoft.Json;

namespace railessentials.Locomotives
{
    public class SpeedLevel
    {
        [JsonProperty(PropertyName = "value")]
        public int Value { get; set; }
    }
}
