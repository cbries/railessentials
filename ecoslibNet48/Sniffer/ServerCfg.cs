// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ServerCfg.cs

using System.Net;

namespace ecoslib.Sniffer
{
    public class ServerCfg
    {
        public IPAddress ServerIp { get; set; } = IPAddress.Any;
        public ushort ServerPort { get; set; } = 45098;
    }
}
