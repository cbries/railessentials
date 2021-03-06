// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: TrackElement.cs

using System;
using System.Collections.Generic;
using System.Xml;
using railessentials.Plan;

namespace railessentials.Importer.Rocrail.Xml
{
    public class TrackElement
    {
        public PlanItemT ElementType { get; set; }

        public TrackElement()
        {
            ElementType = PlanItemT.Tk;
        }

        public string Type { get; set; }
        public string Identifier { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string BlockId { get; set; }
        public List<string> RouteIds { get; } = new();

        // west, east, north, south
        public string Orientation { get; set; }

        // if not used "1" is the default value (reserved)
        public int ConnectorId { get; set; }
        
        public void AddRoutes(string nativeXmlValue)
        {
            if (string.IsNullOrEmpty(nativeXmlValue)) return;
            var parts = nativeXmlValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;
            for (var i = 0; i < parts.Length; ++i)
                parts[i] = parts[i].Trim();
            RouteIds.AddRange(parts);
        }

        public virtual bool ParseXmlNode(XmlNode node)
        {
            if (node?.Attributes == null) return false;

            var attr = node.Attributes;

            Type = attr["type"]?.Value;
            Identifier = attr["id"]?.Value;
            BlockId = attr["blockid"]?.Value;

            var x = attr["x"]?.Value;
            var y = attr["y"]?.Value;

            X = int.Parse(x == null ? "-1" : $"{x}");
            Y = int.Parse(y == null ? "-1" : $"{y}");

            AddRoutes(attr["routeids"]?.Value);

            Orientation = attr["ori"]?.Value ?? "west";

            if (attr["tknr"] != null)
                ConnectorId = int.Parse(attr["tknr"]?.Value ?? "1");

            return true;
        }
    }
}
