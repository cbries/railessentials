// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: WsServer.cs

using System;
using System.Collections.Concurrent;
using ecoslib.Connector;
using Newtonsoft.Json.Linq;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketEngine;
using SuperWebSocket;

namespace ecoslib.Sniffer
{
    namespace WebSocket
    {
        public delegate void StartFailed(object sender);
        public delegate void Stopped(object sender);

        public delegate void ClientConnected(object sender, WebSocketSession session);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="session"></param>
        /// <param name="reason">SuperSocket.SocketBase.CloseReason</param>
        public delegate void ClientDisconnected(object sender, WebSocketSession session, int reason);
        public delegate void ClientFailed(object sender, MessageEventArgs eventArgs);
        public delegate void MessageReceived(object sender, MessageEventArgs eventArgs);
        public delegate void SendFailed(object sender, MessageEventArgs eventArgs);
    }

    public class WsServer : IWsServer
    {
        public event WebSocket.StartFailed StartFailed;
        public event WebSocket.Stopped Stopped;
        public event WebSocket.ClientConnected ClientConnected;
        public event WebSocket.ClientDisconnected ClientDisconnected;
        public event WebSocket.ClientFailed ClientFailed;
        public event WebSocket.MessageReceived MessageReceived;
        public event WebSocket.SendFailed SendFailed;

        public ILogger Logger { get; set; }
        public ConcurrentBag<IWsClient> ConnectedClients { get; } = new();

        private WebSocketServer _wsServer;
        private readonly ServerCfg _serverCfg = new();

        private bool _stopped = true;

        public ServerCfg GetCfg()
        {
            return _serverCfg;
        }
        
        private bool Setup()
        {
            var r = new RootConfig();

            const int timeOutSeconds = 60;
            const int timeOutMinutes = 60;
            const int timeOutMultiplicator = 10;

            var s = new ServerConfig
            {
                Name = "RailwayEssentialWebSocket",
                Ip = _serverCfg.ServerIp.ToString(),
                Port = _serverCfg.ServerPort,
                KeepAliveTime = 120,
                KeepAliveInterval = 1,
                ClearIdleSession = false,
                ClearIdleSessionInterval = 180,
                IdleSessionTimeOut = timeOutSeconds * timeOutMinutes * timeOutMultiplicator,
                Mode = SocketMode.Tcp,
                MaxRequestLength = 4096 * 1024
            };

            var f = new SocketServerFactory();

            _wsServer = new WebSocketServer();

            return _wsServer.Setup(r, s, f);
        }

        public void Listen()
        {
            if (!_stopped) return;

            if (_wsServer != null)
            {
                if (_wsServer.State == ServerState.Running
                || _wsServer.State == ServerState.Starting
                || _wsServer.State == ServerState.Initializing)
                    return;
            }

            _stopped = false;

            try
            {
                if (_wsServer != null)
                {
                    _wsServer.Stop();
                    _wsServer = null;
                }

                var res = Setup();
                if (!res)
                {
                    StartFailed?.Invoke(this);
                    Logger?.Log?.Warn("Start failed.");
                }
                else
                {
                    _wsServer.NewMessageReceived += WsServerOnNewMessageReceived;
                    _wsServer.NewDataReceived += WsServerOnNewDataReceived;
                    _wsServer.NewSessionConnected += WsServerOnNewSessionConnected;
                    _wsServer.SessionClosed += WsServerOnSessionClosed;

                    res = _wsServer.Start();
                    if (!res)
                    {
                        StartFailed?.Invoke(this);
                        Logger?.Log?.Warn("Start failed.");
                    }
                    else
                    {
                        Logger?.Log?.Info("WebSocket server started.");
                    }
                }
            }
            catch(Exception ex)
            {
                Logger?.Log?.Error($"Start failed: {ex}");
            }
        }

        private void WsServerOnSessionClosed(WebSocketSession session, CloseReason value)
        {
            // currently we do not support multiple clients
            // TODO adding master/slave concept, only one master
            // TODO is allowed to connect and to administrate the
            // TODO configuration, track, etc.

            ClientDisconnected?.Invoke(this, session, (int)value);
        }

        private void WsServerOnNewSessionConnected(WebSocketSession session)
        {
            if (_stopped)
            {
                // TODO send message that the service is stopped

                return;
            }

            var client = new WsClient
            {
                NativeSession = session
            };

            ConnectedClients.Add(client);

            var addr = session.RemoteEndPoint.Address;
            var port = session.RemoteEndPoint.Port;

            Logger?.Log?.Info($"<Connector> Connection established from {addr}:{port}");

            ClientConnected?.Invoke(this, session);
        }

        private void WsServerOnNewDataReceived(WebSocketSession session, byte[] value)
        {
            // NOT IMPLEMENTED
        }
        
        private void WsServerOnNewMessageReceived(WebSocketSession session, string message)
        {
            IWsClient client = null;
            foreach (var it in ConnectedClients)
            {
                if (it == null) continue;
                if (it.NativeSession.Equals(session))
                {
                    client = it;
                    break;
                }
            }

            if(client != null)
                MessageReceived?.Invoke(client, new MessageEventArgs(message));
            else
                ClientFailed?.Invoke(session, new MessageEventArgs("Invalid client session"));
        }

        public void Stop()
        {
            try
            {
                _stopped = true;

                _wsServer?.Stop();

                if (_wsServer != null)
                {
                    _wsServer.NewMessageReceived -= WsServerOnNewMessageReceived;
                    _wsServer.NewDataReceived -= WsServerOnNewDataReceived;
                    _wsServer.NewSessionConnected -= WsServerOnNewSessionConnected;
                }

                try
                {
                    _wsServer?.Dispose();
                    _wsServer = null;
                }
                catch
                {
                    // ignore
                }

                Stopped?.Invoke(this);
            }
            catch(Exception ex)
            {
                Logger?.Log?.Error($"Stop failed: {ex}");
            }
        }

        public void SendMessage(JToken token)
        {
            try
            {
                foreach(var it in ConnectedClients)
                {
                    if (it == null) continue;
                    if (!it.IsConnected) continue;
                    var res = it.SendMessage(token);
                    if (!res) throw new Exception("Send for the client returned false.");
                }
            }
            catch(Exception ex)
            {
                Logger?.Log?.Error("SendMessage failed", ex);
                SendFailed?.Invoke(this, new MessageEventArgs("SendMessage failed", ex));
            }
        }
    }
}
