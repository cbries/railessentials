// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: WsClient.cs

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.SocketBase;
using SuperWebSocket;

namespace ecoslib.Sniffer
{
    namespace WebSocket
    {
        public delegate void DataReceivedDelegate(object sender, string line);
        public delegate void SendFailedDelegate(object sender, Exception ex);
    }

    public class WsClient : IWsClient
    {
        public event WebSocket.SendFailedDelegate SendFailed;

        public ILogger Logger { get; set; }
        public WebSocketSession NativeSession { get; set; }

        public bool IsConnected
        {
            get
            {
                if (NativeSession == null) return false;
                return NativeSession.Connected;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (NativeSession == null) return;

                NativeSession.Close(CloseReason.ClientClosing);
                NativeSession = null;
            }
            catch
            {
                // ignore
            }
        }

        public bool SendMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return false;
            if (!IsConnected) return false;

            try
            {
                NativeSession.Send(msg);
                return true;
            }
            catch(Exception ex)
            {
                SendFailed?.Invoke(this, ex);
                Logger?.Log?.Error("Send message failed", ex);
                return false;
            }
        }

        public bool SendMessage(JToken token)
        {
            return SendMessage(token.ToString(Formatting.None));
        }
    }
}
