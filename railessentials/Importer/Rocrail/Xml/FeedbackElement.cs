// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: FeedbackElement.cs

using System.Xml;
using railessentials.Plan;

namespace railessentials.Importer.Rocrail.Xml
{
    public class FeedbackElement : TrackElement
    {
        public int Val { get; set; }
        public int Load { get; set; }
        public int Regval { get; set; }
        public int Maxload { get; set; }
        public int Baseaddr { get; set; }
        public int Offset { get; set; }
        public PortAddress Address { get; set; } = new();
        //public new string BlockId { get; set; }
        public int Bididir { get; set; }
        public string State { get; set; } // true, false

        public FeedbackElement()
        {
            ElementType = PlanItemT.Fb;
        }

        public override bool ParseXmlNode(XmlNode node)
        {
            var res = base.ParseXmlNode(node);
            if (!res) return false;

            var attr = node.Attributes;
            if (attr == null) return false;

            if (attr["val"] != null)
            {
                var v = attr["val"].Value;
                if(!string.IsNullOrEmpty(v))
                    Val = int.Parse(v);
            }

            if (attr["load"] != null)
            {
                var v = attr["load"].Value;
                if (!string.IsNullOrEmpty(v))
                    Load = int.Parse(v);
            }
            
            if (attr["regval"] != null)
            {
                var v = attr["regval"].Value;
                if (!string.IsNullOrEmpty(v)) 
                    Regval = int.Parse(v);
            }
            
            if (attr["maxload"] != null)
            {
                var v = attr["maxload"].Value;
                if (!string.IsNullOrEmpty(v)) 
                    Maxload = int.Parse(v);
            }
            
            if (attr["baseaddr"] != null)
            {
                var v = attr["baseaddr"].Value;
                if (!string.IsNullOrEmpty(v)) 
                    Baseaddr = int.Parse(v);
            }

            if (attr["offset"] != null)
            {
                var v = attr["offset"].Value;
                if(!string.IsNullOrEmpty(v))
                    Offset = int.Parse(v);
            }

            Address.Parse(attr);

            BlockId = attr["blockid"]?.Value ?? string.Empty;
            
            if (attr["bididir"] != null)
                // ReSharper disable once ConstantNullCoalescingCondition
                Bididir = int.Parse(attr["bididir"]?.Value ?? "0");

            State = attr["state"]?.Value;

            return true;
        }
    }
}
