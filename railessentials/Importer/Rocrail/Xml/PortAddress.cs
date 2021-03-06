// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PortAddress.cs

using System.Xml;
using Utilities;

namespace railessentials.Importer.Rocrail.Xml
{
    public class PortAddress
    {
        public int Addr { get; set; }

        public int Addr1 { get; set; }
        public int Port1 { get; set; }
        public bool Inverse1 { get; set; }

        public int Addr2 { get; set; }
        public int Port2 { get; set; }
        public bool Inverse2 { get; set; }
        
        public bool Parse(XmlAttributeCollection attr)
        {
            if (attr == null) return false;
            if (attr.Count == 0) return false;
            try
            {
                Addr = attr.GetInt("addr", 0);

                Addr1 = attr.GetInt("addr1", 0);
                Port1 = attr.GetInt("port1", 0);
                Inverse1 = attr.GetBool("inv", false);

                Addr2 = attr.GetInt("addr2", 0);
                Port2 = attr.GetInt("port2", 0);
                Inverse2 = attr.GetBool("inv2", false);

                if (Addr1 == Addr2 && Port1 == Port2)
                {
                    Addr2 = 0;
                    Port2 = 0;
                }

                    return true;
            }
            catch
            {
                // ignore
            }
            return false;
        }
    }
}