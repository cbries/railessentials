// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: SignalElement.cs

using System.Xml;
using railessentials.Plan;

namespace railessentials.Importer.Rocrail.Xml
{
    public class SignalElement : TrackElement
    {
        public SignalElement()
        {
            ElementType = PlanItemT.Sg;
        }

        public string Signal { get; set; }
        public PortAddress Address { get; set; } = new();
        public string State { get; set; } // "red", "green"

        public override bool ParseXmlNode(XmlNode node)
        {
            var res = base.ParseXmlNode(node);
            if (!res) return false;

            var attr = node.Attributes;
            if (attr == null) return false;

            Signal = attr["signal"]?.Value;
            Address.Parse(attr);
            State = attr["state"]?.Value;
            
            return true;
        }
    }
}
