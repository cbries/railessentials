// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Route.cs

using System.Collections.Generic;
using Newtonsoft.Json;
using railessentials.Analyzer;

// ReSharper disable InconsistentNaming

namespace railessentials.Route
{
    public class Route
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "tracks")]
        public List<RouteTrack> Tracks { get; set; } = new();

        [JsonProperty(PropertyName = "switches")]
        public List<RouteSwitch> Switches { get; set; } = new();

        [JsonProperty(PropertyName = "sensors")]
        public List<RouteSensors> Sensors { get; set; } = new();
        
        [JsonProperty(PropertyName = "signals")]
        public List<RouteSignal> Signals { get; set; } = new();

        [JsonProperty(PropertyName = "blocks")]
        public List<RouteBlock> Blocks { get; set; }

        [JsonProperty(PropertyName = "isDisabled")]
        public bool IsDisabled { get; set; }

        // runtime attribute
        [JsonProperty(PropertyName = "locked")]
        public bool Locked { get; set; }

        // runtime attribute
        [JsonProperty(PropertyName = "occupied")]
        public bool Occupied { get; set; }

        [JsonProperty(PropertyName = "additionalBlockLocks")]
        public List<string> AdditionalBlockLocks { get; set; } = new();
    }

    public class RouteTrack
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    public class RouteSwitch
    {
        public int x { get; set; }
        public int y { get; set; }
        public RouteSwitchSwitch Switch { get; set; }
        public string State => Switch?.State;
    }

    public class RouteSwitchSwitch
    {
        public string State { get; set; }
    }

    public class RouteSignal
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    public class RouteBlock
    {
        public int x { get; set; }
        public int y { get; set; }
        public string identifier { get; set; }
        public bool start { get; set; }
        public SideMarker side { get; set; }
    }

    public class RouteSensors
    {
        public int x { get; set; }
        public int y { get; set; }
    }
}
