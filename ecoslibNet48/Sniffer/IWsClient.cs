// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IWsClient.cs

using System.Net;
using Newtonsoft.Json.Linq;
using SuperWebSocket;

namespace ecoslib.Sniffer
{
    public interface IWsClient
    {
        ILogger Logger { get; set; }
        WebSocketSession NativeSession { get; set; }

        bool IsConnected { get; }
        void Disconnect();
        bool SendMessage(string msg);
        bool SendMessage(JToken token);
    }
}
