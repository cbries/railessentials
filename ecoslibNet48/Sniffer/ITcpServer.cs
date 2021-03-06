// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ITcpServer.cs

using System.Collections.Concurrent;
using System.Collections.Generic;
using ecoslib.Connector;

namespace ecoslib.Sniffer
{
    public interface ITcpServer
    {
        ILogger Logger { get; set; }

        ConcurrentBag<ITcpClient> ConnectedClients { get; }

        ServerCfg GetCfg();
        void Listen();
        void Stop();
        void SendMessage(string msg, bool forceBase64Encode = true);
    }
}
