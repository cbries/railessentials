// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: BlockElement.cs

using System;
using System.Collections.Generic;
using System.Xml;
using railessentials.Plan;
using Utilities;

namespace railessentials.Importer.Rocrail.Xml
{
    public class BlockElement : TrackElement
    {
        public BlockElement()
        {
            ElementType = PlanItemT.Bk;
        }

        public string Id { get; private set; } = string.Empty;
        public bool SmallSymbol { get; private set; }
        public List<FbEvent> FbEvents { get; } = new();

        public override bool ParseXmlNode(XmlNode node)
        {
            var res = base.ParseXmlNode(node);
            if (!res) return false;

            var attr = node.Attributes;
            if (attr == null) return false;

            Id = attr.GetString("id");

            if (attr["smallsymbol"] != null)
                SmallSymbol = bool.Parse(attr["smallsymbol"].Value);

            if(node.HasChildNodes)
            {
                foreach(var itChild in node.ChildNodes)
                {
                    var nodeChild = itChild as XmlNode;
                    if (nodeChild == null) continue;
                    if (string.IsNullOrEmpty(nodeChild.Name)) continue;
                    if (!nodeChild.Name.Equals("fbevent", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var instance = new FbEvent();
                    if (instance.Parse(nodeChild))
                    {
                        instance.Owner = this;
                        FbEvents.Add(instance);
                    }
                }
            }

            return true;
        }
    }
}
