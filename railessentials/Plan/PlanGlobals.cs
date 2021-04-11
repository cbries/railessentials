// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanGlobals.cs

using System;
using System.Collections.Generic;

namespace railessentials.Plan
{
    public static class PlanGlobals
    {
        public enum ThemeIdType
        {
            Track, Switch, Signal, Block, Sensor, Direction, Connector, Button
        }

        public static readonly List<int> TrackIds = new() { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 253, 254, 255 };
        public static readonly List<int> SwitchIds = new() { 50, 51, 52, 53, 54, 55, 56, 57, 58, 59 };
        public static readonly List<int> SignalIds = new() { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109 };
        public static readonly List<int> BlockIds = new() { 150, 151, 152 };
        public static readonly List<int> SensorIds = new() { 200, 201, 202 };
        public static readonly List<int> DirectionIds = new() { 14 };
        public static readonly List<int> ConnectorIds = new() { 17, 18, 19 };
        public static readonly List<int> ButtonIds = new() { 71, 72, 73 };
        public static readonly List<int> DecouplerIds = new() { 70 };
        public static readonly List<int> LabelIds = new() { 1010 };
        
        public static bool IsTrackItem(int themeId)
        {
            if (themeId < 0) return false;
            if (TrackIds.Contains(themeId)) return true;
            if (SwitchIds.Contains(themeId)) return true;
            if (SignalIds.Contains(themeId)) return true;
            if (BlockIds.Contains(themeId)) return true;
            if (SensorIds.Contains(themeId)) return true;
            if (DirectionIds.Contains(themeId)) return true;
            if (ConnectorIds.Contains(themeId)) return true;
            if (DecouplerIds.Contains(themeId)) return true;
            return false;
        }

        public static ThemeIdType GetThemeType(int themeId)
        {
            if (DirectionIds.Contains(themeId)) return ThemeIdType.Direction;
            if (ConnectorIds.Contains(themeId)) return ThemeIdType.Connector;
            if (SensorIds.Contains(themeId)) return ThemeIdType.Sensor;
            if (BlockIds.Contains(themeId)) return ThemeIdType.Block;
            if (SignalIds.Contains(themeId)) return ThemeIdType.Signal;
            if (SwitchIds.Contains(themeId)) return ThemeIdType.Switch;
            if (TrackIds.Contains(themeId)) return ThemeIdType.Track;
            if (ButtonIds.Contains(themeId)) return ThemeIdType.Button;
            return ThemeIdType.Track;
        }

        public static string GetThemeTypeName(int themeId)
        {
            var type = GetThemeType(themeId);
            switch (type)
            {
                case ThemeIdType.Block: return "Block";
                case ThemeIdType.Connector: return "Connector";
                case ThemeIdType.Direction: return "Direction";
                case ThemeIdType.Sensor: return "Sensor";
                case ThemeIdType.Signal: return "Signal";
                case ThemeIdType.Switch: return "Switch";
                case ThemeIdType.Track: return "Track";
                case ThemeIdType.Button: return "Button";
            }
            return "Track";
        }

        public static string Reverse(string s)
        {
            var charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
