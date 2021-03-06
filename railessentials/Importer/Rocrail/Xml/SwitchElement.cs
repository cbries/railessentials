// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: SwitchElement.cs

using System.Xml;
using railessentials.Plan;

namespace railessentials.Importer.Rocrail.Xml
{
    public class SwitchElement : TrackElement
    {
        public SwitchElement()
        {
            ElementType = PlanItemT.Sw;
        }

        public string Dir { get; set; }

        public PortAddress Address { get; set; } = new();

        public string AccNr { get; set; }

        public override bool ParseXmlNode(XmlNode node)
        {
            var res = base.ParseXmlNode(node);
            if (!res) return false;

            var attr = node.Attributes;
            if (attr == null) return false;

            Dir = attr["dir"]?.Value;

            Address.Parse(attr);
            AccNr = attr["accnr"]?.Value ?? string.Empty;

            return true;
        }
    }
}
