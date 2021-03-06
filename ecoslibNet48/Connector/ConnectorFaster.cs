// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ConnectorFaster.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace ecoslib.Connector
{
    public delegate void ConnectorFasterMessageReceived(object sender, MessageEventArgs eventArgs);
    public delegate void ConnectorFasterFailed(object sender, MessageEventArgs eventArgs);

    public class ConnectorFaster : IConnector
    {
        public event EventHandler Started;
        public event ConnectorFasterFailed Failed;
        public event EventHandler Stopped;
        public event ConnectorFasterMessageReceived MessageReceived;

        public ILogger Logger { get; set; }
        public string LastError { get; private set; }

        public string IpAddress { get; set; }
        public UInt16 Port { get; set; }

        #region IConnector

        public bool Start()
        {
            try
            {
                if (_thread != null && _thread.IsAlive)
                    return true;

                _thread = new Thread(async () =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    await StartHandler();
                });

                _thread.Start();

                _run = _thread.ThreadState == ThreadState.Running 
                       || _thread.ThreadState == ThreadState.Background;

                if (_run)
                    return true;

                try
                {
                    //_thread?.Abort(null);
                    _thread = null;
                }
                catch
                {
                    // ignore
                }

                Stopped?.Invoke(this, null!);

                return false;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public bool Stop()
        {
            if (!_run) return true;

            _run = false;
            _client?.Disconnect();
            _client = null;
            Stopped?.Invoke(this, null!);
            return true;
        }

        public bool SendMessage(string commandMessage)
        {
            if (string.IsNullOrEmpty(commandMessage)) return false;
            if (_client == null || !_client.IsConnected) return false;
            if (_client.IsConnected) return _client.SendMessage(commandMessage);
            return false;
        }

        public bool SendCommand(object command)
        {
            if (command is ICommand c)
                return SendMessage(c.ToString());
            return SendMessage(null);
        }

        public bool SendCommands(IReadOnlyList<object> commands)
        {
            var cmds = new List<ICommand>();
            foreach (var c in commands)
            {
                if (c is ICommand cc)
                    cmds.Add(cc);
            }
            return SendMessage(string.Join("\r\n", cmds));
        }

        #endregion

        private bool _run;
        private Thread _thread;
        private TcpClient2 _client;

        private async Task StartHandler()
        {
            var ipaddr = IpAddress;
            int port = Port;

            try
            {
                _client = new TcpClient2
                {
                    Logger = Logger,
                    ThreadInstance = _thread
                };

                _client.LineReceived += (_, line) =>
                {
                    if (string.IsNullOrEmpty(line)) return;
                    Logger?.Log?.Debug($"<Connector> Recv ({line.Length}): {line}");
                    MessageReceived?.Invoke(this, new MessageEventArgs(line));
                };
                _client.SendFailed += (_, ex) =>
                {
                    Logger?.Log?.Error($"<Connector> Send failed: {ex.Message}");
                    Failed?.Invoke(this, new MessageEventArgs($"Send of message failed", ex));
                };

                _client.Connect(ipaddr, port);

                Logger?.Log?.Info($"<Connector> Connection established to {ipaddr}:{port}");
                Started?.Invoke(this, null!);

                await _client.HandleLines();
            }
            catch (Exception ex)
            {
                Logger?.Log?.Error($"<Connector> Connection failed to {ipaddr}:{port} with {ex.Message}");
                Failed?.Invoke(this, new MessageEventArgs($"Connection failed to {ipaddr}:{port} with {ex.Message}", ex));
            }
        }
    }
}
