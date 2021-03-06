// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: XmlUtilities.cs

using System.Xml;

namespace Utilities
{
    public static class XmlUtilities
    {
        public static int GetInt(this XmlAttributeCollection attr, string name, int def)
        {
            if (attr[name] == null) return def;
            var v = attr[name].Value;
            return int.TryParse(v, out var vv) ? vv : def;
        }

        public static bool GetBool(this XmlAttributeCollection attr, string name, bool def)
        {
            if (attr[name] == null) return def;
            var v = attr[name].Value;
            return bool.TryParse(v, out var vv) ? vv : def;
        }

        public static string GetString(this XmlAttributeCollection attr, string name, string def = "")
        {
            if (attr[name] == null) return def;
            var v = attr[name].Value;
            return v ?? def;
        }
    }
}
