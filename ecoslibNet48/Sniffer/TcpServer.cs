// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: TcpServer.cs

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ecoslib.Connector;
using ecoslib.Utilities;

namespace ecoslib.Sniffer
{
    public delegate void ClientConnected(object sender, ITcpClient client);
    public delegate void ClientFailed(object sender, MessageEventArgs eventArgs);
    public delegate void MessageReceived(object sender, MessageEventArgs eventArgs);
    public delegate void SendFailed(object sender, MessageEventArgs eventArgs);

    public class TcpServer : ITcpServer
    {
        public event EventHandler Stopped;
        public event ClientConnected ClientConnected;
        public event ClientFailed ClientFailed;
        public event MessageReceived MessageReceived;
        public event SendFailed SendFailed;

        private TcpListener _tcpServer;
        private readonly ServerCfg _serverCfg = new();
        private Thread _serverThread;
        
        public ILogger Logger { get; set; }

        public ConcurrentBag<ITcpClient> ConnectedClients { get; private set; } = new();

        public ServerCfg GetCfg()
        {
            return _serverCfg;
        }

        public void Listen()
        {
            _stopped = false;

            if (_serverThread != null && _serverThread.IsAlive)
                return;

            var ip = _serverCfg.ServerIp;
            var port = _serverCfg.ServerPort;

            _tcpServer = new TcpListener(ip, port);
            _tcpServer.Start();

            _serverThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                ListenInternal();
            });

            _serverThread.Start();
        }

        private void ListenInternal()
        {
            try
            {
                Logger?.Log?.Info("<TcpServer> Listen started...");

                while (true)
                {
                    var client = _tcpServer.AcceptTcpClient();
                    var t = new Thread(HandleDevice);
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Logger?.Log?.Error($"Listen failed: {e}");

                _tcpServer.Stop();
            }
        }

        private bool _stopped;

        public void Stop()
        {
            if (_stopped) return;

            _stopped = true;

            foreach(var itClient in ConnectedClients)
            {
                if (itClient == null) continue;
                if (!itClient.IsConnected) continue;

                try
                {
                    itClient.SendMessage("Quit\r\n\r\n");
                    itClient.Disconnect();
                }
                catch
                {
                    // ignore
                }
            }

            while (!ConnectedClients.IsEmpty)
                ConnectedClients.TryTake(out _);

            Stopped?.Invoke(this, null!);
        }

        public void HandleDevice(object obj)
        {
            var nativeClient = obj as System.Net.Sockets.TcpClient;
            if(nativeClient == null)
            {
                Logger?.Log?.Warn($"Invalid client connected");
                return;
            }

            var client = new Connector.TcpClient
            {
                Logger = Logger,
                NativeClient = nativeClient
            };

            if (_stopped)
            {
                // TODO send message that the service is stopped

                return;
            }

            ConnectedClients.Add(client);

            var threadClient = new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                await StartHandler(client);
            });

            client.ThreadInstance = threadClient;

            threadClient.Start();

            var ipaddr = client.Ip;
            var port = client.Port;

            Logger?.Log?.Info($"<Connector> Connection established from {ipaddr}:{port}");

            ClientConnected?.Invoke(this, client);
        }

        private async Task StartHandler(Connector.TcpClient client)
        {
            if (client == null) return;

            try
            {
                client.LineReceived += (_, line) =>
                {
                    if (string.IsNullOrEmpty(line)) return;
                    Logger?.Log?.Debug($"<TcpServer> Recv ({line.Length}): {line}");
                    MessageReceived?.Invoke(client, new MessageEventArgs(line));
                };

                client.SendFailed += (_, ex) =>
                {
                    Logger?.Log?.Error($"<TcpServer> Send failed: {ex.Message}");
                    SendFailed?.Invoke(client, new MessageEventArgs($"Send of message to {ex.Message}"));
                };

                client.Disconnected += (sender, _) =>
                {
                    var tcpClient = sender as ITcpClient;
                    if (tcpClient == null) return;

                    ConnectedClients = new ConcurrentBag<ITcpClient>(ConnectedClients.Except(new[] { tcpClient }));
                };

                await client.HandleLines();
            }
            catch (Exception ex)
            {
                ClientFailed?.Invoke(client, new MessageEventArgs($"{ex.Message}"));
            }
        }

        /// <summary>
        /// All messages will be converted to base64 encoded strings.
        /// If the message is plain text, it will be encoded on-demand.
        /// </summary>
        /// <param name="msg">base64 encoded text, plain text will be encoded on-demand when forceBase64Encode is true</param>
        /// <param name="forceBase64Encode">if true msg will be encoded to base64</param>
        public void SendMessage(string msg, bool forceBase64Encode = true)
        {
            if (string.IsNullOrEmpty(msg)) return;
            var encodedMsg = msg;
            if(!encodedMsg.IsBase64String() && forceBase64Encode)
                encodedMsg = System.Text.Encoding.UTF8.ToBase64(msg);

            foreach(var itClient in ConnectedClients)
            {
                if (itClient == null) continue;
                if (!itClient.IsConnected) continue;

                itClient.SendMessage(encodedMsg);
            }
        }
    }
}
