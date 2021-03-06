// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: FbEvent.cs

using System.Xml;
using Utilities;

namespace railessentials.Importer.Rocrail.Xml
{
    public class FbEvent
    {
        public BlockElement Owner { get; set; }

        public string FbId { get; set; }
        public string Action { get; set; }
        public string From { get; set; }
        //public string ByRoute { get; set; }
        //public bool EndPuls { get; set; }
        //public bool UseTimer2 { get; set; }

        public bool Parse(XmlNode node)
        {
            if (node == null) return false;
            FbId = node.Attributes.GetString("id");
            Action = node.Attributes.GetString("action");

            // [+]-side := all
            // [-]-side := all-reverse
            From = node.Attributes.GetString("from");
            
            //ByRoute = node.Attributes.GetString("byroute");
            //EndPuls = node.Attributes.GetBool("endpuls", false);
            //UseTimer2 = node.Attributes.GetBool("use_timer2", false);
            return true;
        }
    }
}