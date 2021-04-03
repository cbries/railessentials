// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanItem.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace railessentials.Plan
{
    public enum PlanItemT
    {
        Unknown,
        Block = 1, Bk = 1,
        Switch = 2, Sw = 2, 
        Feedback = 3, Fb = 3, 
        Track = 4, Tk = 4, 
        Signal = 5, Sg = 5, 
        Tx, 
        St, 
        Lc, 
        Vr, 
        Co
    }

    public partial class PlanItem
    {
        [JsonIgnore]
        public PlanField Ctx { get; set; }

        #region metamodel

        public string name { get; set; }
        public string identifier { get; set; }
        public string basename { get; set; }
        public bool clickable { get; set; }
        public List<string> routes { get; set; } = new();
        public Dictionary<string, List<PlanSwitchState>> states { get; set; } = new();
        public PlanItemCoord coord { get; set; } = new();
        public PlanItemEditor editor { get; set; } = new();
        public List<PlanItemDimension> dimensions { get; set; } = new();

        [JsonProperty(PropertyName = "addresses")]
        public PlanItemAddresses Addresses { get; set; } = new();

        [JsonProperty(PropertyName = "groupName")]
        public string GroupName { get; set; } // see #70

        #endregion metamodel

        /// <summary>
        /// Returns the current `themeDimIdx` values.
        /// In case the value does not fit into the range
        /// of routes, a modul division is executed to
        /// get the minimum allowed value for the
        /// rotation. Keep in mind a maximum range
        /// of `0..3` is allowed, because a track element
        /// can only have 0°, 90°, 180°, and 270° degree
        /// of rotation
        /// </summary>
        /// <returns></returns>
        public int GetThemeDimensionIndex()
        {
            var themeDimY = 0;
            if (editor != null)
                themeDimY = editor.themeDimIdx;

            if (routes != null
                && routes.Count > 0)
            {
                if (themeDimY >= routes.Count)
                    themeDimY = routes.Count % themeDimY;

                if (themeDimY >= routes.Count)
                {
                    if (themeDimY % 2 == 0) return 1;
                    if (themeDimY % 3 == 0) return 2;
                    if (themeDimY % 4 == 0) return 3;

                    return 0;
                }
            }

            return themeDimY;
        }

        /// <summary>
        /// Queries the currently selected possible routes.
        /// It checks `themeDimIdx` if its fits into the range
        /// of `routes` and will return the list of allowed
        /// ways on the current rotation representation.
        /// </summary>
        /// <returns></returns>
        public List<string> GetDimensionRoutes()
        {
            if (!PlanGlobals.IsTrackItem(editor.themeId))
                return new List<string>();

            string str;
            if (editor == null)
            {
                str = routes[0];
            }
            else
            {
                var dimIdx = GetThemeDimensionIndex();
                str = routes[dimIdx];
            }
            if (string.IsNullOrEmpty(str))
                return new List<string>();
            var parts = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.ToList();
        }

        [JsonIgnore]
        public int Circumference => 2 * Width() + 2 * Height();

        public int Width()
        {
            var s = StartCoord();
            var e = EndCoord();
            var w = e.x - s.x;
            return w + 1;
        }

        public int Height()
        {
            var s = StartCoord();
            var e = EndCoord();
            var h = e.y - s.y;
            return h + 1;
        }

        public PlanItemCoord StartCoord()
        {
            return coord;
        }

        public PlanItemCoord EndCoord()
        {
            if (dimensions != null 
                && dimensions.Count > 0)
            {
                var dim = dimensions[GetThemeDimensionIndex()];
                var endCoord = new PlanItemCoord
                {
                    x = coord.x + dim.W - 1,
                    y = coord.y + dim.H - 1
                };

                return endCoord;
            }

            return new PlanItemCoord
            {
                x = coord.x,
                y = coord.y
            };
        }

        [JsonIgnore]
        public bool IsTrack => PlanGlobals.TrackIds.Contains(editor.themeId);
        [JsonIgnore]
        public bool IsSwitch => PlanGlobals.SwitchIds.Contains(editor.themeId);
        [JsonIgnore]
        public bool IsSignal => PlanGlobals.SignalIds.Contains(editor.themeId);
        [JsonIgnore]
        public bool IsBlock => PlanGlobals.BlockIds.Contains(editor.themeId);
        [JsonIgnore]
        public bool IsSensor => PlanGlobals.SensorIds.Contains(editor.themeId);
        [JsonIgnore]
        public bool IsDirection => PlanGlobals.DirectionIds.Contains(editor.themeId);
        [JsonIgnore]
        public bool IsConnector => PlanGlobals.ConnectorIds.Contains(editor.themeId);
        [JsonIgnore]
        public bool IsButton => PlanGlobals.ButtonIds.Contains(editor.themeId);

        public override string ToString()
        {
            try
            {
                var s = StartCoord();
                var e = EndCoord();
                return $"{identifier}  Start({s.x}, {s.y}) -> Stop({e.x}, {e.y})";
            }
            catch
            {
                return base.ToString();
            }
        }
    }
}
