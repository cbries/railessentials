// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IWsServer.cs

using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ecoslib.Sniffer
{
    public interface IWsServer
    {
        ILogger Logger { get; set; }

        ConcurrentBag<IWsClient> ConnectedClients { get; }

        ServerCfg GetCfg();
        void Listen();
        void Stop();
        void SendMessage(JToken token);
    }
}
