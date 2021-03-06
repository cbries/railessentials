// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ClientHandler.Debug.cs

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace railessentials.ClientHandler
{
    public partial class ClientHandler : IDebugMessages
    {
        public void SendDebugMessages(List<string> msgs, DebugMessageLevel level = DebugMessageLevel.Info)
        {
            msgs.ForEach(x => SendDebugMessage(x, level));
        }

        public void SendDebugMessage(string msg, DebugMessageLevel level = DebugMessageLevel.Info)
        {
            if (string.IsNullOrEmpty(msg)) return;

            SendDebug(new List<DebugMessage> {
                DebugMessage.Instance(msg, level)
            });
        }

        public void SendDebug(DebugMessage message)
        {
            SendDebug(new List<DebugMessage> { message });
        }

        public void SendDebug(List<DebugMessage> messages)
        {
            if (messages == null) return;

            var wsServer = _sniffer?.GetWsServer();
            if (wsServer == null) return;

            var ar = new JArray();
            foreach (var it in messages)
            {
                if (it == null) continue;
                ar.Add(it.ToString());
            }

            var o = new JObject
            {
                ["command"] = "debugMessages",
                ["messages"] = ar
            };

            foreach (var itClient in wsServer.ConnectedClients)
                itClient?.NativeSession?.Send(o.ToString(Formatting.None));
        }

    }
}
