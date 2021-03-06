// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: TextElement.cs

using System.Xml;
using railessentials.Plan;

namespace railessentials.Importer.Rocrail.Xml
{
    public class TextElement : TrackElement
    {
        public TextElement()
        {
            ElementType = PlanItemT.Tx;
        }

        public string Text { get; set; }
        public bool IsBold { get; set; } = true;
        public bool IsItalic { get; set; }
        public bool IsUnderline { get; set; }
        public int Width { get; set; } = 4;
        public int Height { get; set; } = 1;

        public override bool ParseXmlNode(XmlNode node)
        {
            var res = base.ParseXmlNode(node);
            if (!res) return false;

            var attr = node.Attributes;
            if (attr == null) return false;

            if (attr["text"] != null)
                Text = attr["text"].Value ?? string.Empty;

            if (attr["bold"] != null)
                IsBold = bool.Parse(attr["bold"]?.Value ?? "false");
            if (attr["italic"] != null)
                IsItalic = bool.Parse(attr["italic"]?.Value ?? "false");
            if (attr["underlined"] != null)
                IsUnderline = bool.Parse(attr["underlined"]?.Value ?? "false");
            if (attr["cx"] != null)
                Width = int.Parse(attr["cx"]?.Value ?? "4");
            if (attr["cy"] != null)
                Height = int.Parse(attr["cy"]?.Value ?? "4");

            return true;
        }
    }
}
