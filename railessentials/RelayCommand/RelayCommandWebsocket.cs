// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RelayCommandWebsocket.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.ClientEngine;
using WebSocket4Net;
using Trace = System.Diagnostics.Trace;

namespace railessentials.RelayCommand
{
    public class RelayCommandWebsocket
    {
        private bool _websocketClosed;
        private readonly WebSocket _webSocket;
        private readonly AutoResetEvent _waitForConnect = new(false);

        public string LastError { get; private set; }

        public string Url { get; }

        public RelayCommandWebsocket(string targetUri)
        {
            Url = targetUri;
            _websocketClosed = false;
            _webSocket = new WebSocket(targetUri);
            _webSocket.Opened += websocket_Opened;
            _webSocket.Closed += websocket_Closed;
            _webSocket.Error += websocket_Error;
            _webSocket.MessageReceived += websocket_MessageReceived;
        }

        public bool Send(string data, out Exception exceptionInstance, int secondsToWait = 5)
        {
            exceptionInstance = null;

            if (string.IsNullOrEmpty(data)) 
                return true;

            if (_webSocket == null) return false;
            if (_webSocket.State == WebSocketState.Closing) return false;
            if (_webSocket.State == WebSocketState.Closed) return false;

            try
            {
                _webSocket.Open();

                var r = _waitForConnect.WaitOne(TimeSpan.FromSeconds(secondsToWait));
                if (!r)
                {
                    LastError = "Timeout during send to " + Url;
                    return false;
                }

                if (_webSocket.State == WebSocketState.Open)
                {
                    _webSocket.Send(data);

                    Close();

                    return true;
                }

                if(string.IsNullOrEmpty(LastError))
                    if (_websocketClosed)
                        LastError = "WebSocket connection closed unexpected";
                Close();
                return false;
            }
            catch (Exception ex)
            {
                exceptionInstance = ex;
            }

            return false;
        }

        public void Close()
        {
            _webSocket.Close("Finished");
            _webSocket.Dispose();
        }

        private void websocket_Opened(object sender, EventArgs e)
        {
            _waitForConnect.Set();
        }

        private void websocket_Error(object sender, ErrorEventArgs e)
        {
            LastError = e.Exception.Message;
        }

        private void websocket_Closed(object sender, EventArgs e)
        {
            _websocketClosed = true;
            _waitForConnect.Set();
        }

        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Trace.WriteLine($"{e.Message}");
        }

        public static RelayCommandWebsocketResult Send(string targetUri, string data)
        {
            var instance = new RelayCommandWebsocket(targetUri);
            var res = instance.Send(data, out var exceptionInstance);
            var result = new RelayCommandWebsocketResult
            {
                Result = res,
                Ex = exceptionInstance,
                LastError = instance.LastError ?? string.Empty
            };
            return result;
        }

        public static async Task SendDelayed(string targetUri, string data, int secondDelay)
        {
            await Task.Delay(secondDelay * 1000).ContinueWith(_ => Send(targetUri, data));
        }
    }

    public class RelayCommandWebsocketResult
    {
        public bool Result { get; set; }
        public Exception Ex { get; set; }
        public string LastError { get; set; }
    }
}
