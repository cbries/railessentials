// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: CoElement.cs

using System.Xml;
using railessentials.Plan;

namespace railessentials.Importer.Rocrail.Xml
{
    public class CoElement : TrackElement
    {
        public CoElement()
        {
            ElementType = PlanItemT.Co;
        }

        public int Value { get; set; }

        public override bool ParseXmlNode(XmlNode node)
        {
            var res = base.ParseXmlNode(node);
            if (!res) return false;

            var attr = node.Attributes;
            if (attr == null) return false;

            /*
             <co id="Seg3" x="19" y="6" z="0" porttype="0" prev_id="Seg3" nr="0" desc="" decid="" show="true" showid="true" svgtype="0" blockid="" routeids="" grpid="SchalterSegmentdrehscheibe" tristate="false" toggleswitch="true" operable="true" iid="esu" bus="0" uidname="" addr="257" port="2" gate="0" param="0" paramoff="0" value="1" delay="0" prot="N" inv="false" blink="false" colortype="false" asswitch="false" accessory="true" redChannel="0" greenChannel="0" blueChannel="0" whiteChannel="0" white2Channel="0" brightnessChannel="0" state="off" svgacctype="false" plancolor="false">
               <color red="0" green="0" blue="0" white="0" white2="0" saturation="254" brightness="0" id=""/>
             </co>
             */

            if (attr["value"] != null)
                Value = int.Parse(attr["value"]?.Value ?? "0");

            return true;
        }
    }
}
