// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: TcpClient.cs

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ecoslib.Connector
{
    public delegate void LineReceivedDelegate(object sender, string line);
    public delegate void SendFailedDelegate(object sender, Exception ex);

    public class TcpClient : ITcpClient
    {
        public event LineReceivedDelegate LineReceived;
        public event SendFailedDelegate SendFailed;
        public event EventHandler Disconnected;
        public event EventHandler Failed;

        public ILogger Logger { get; set; }
        public System.Net.Sockets.TcpClient NativeClient { get; set; }
        public System.Threading.Thread ThreadInstance { get; set; }

        public IPAddress Ip => RemoteEndPoint.Address;
        public int Port => RemoteEndPoint.Port;

        private IPEndPoint RemoteEndPoint
        {
            get
            {
                var p = NativeClient?.Client?.RemoteEndPoint as IPEndPoint;
                if (p == null)
                    return new IPEndPoint(IPAddress.None, 0);
                return p;
            }
        }

        public bool IsConnected
        {
            get
            {
                if (NativeClient == null) return false;
                return NativeClient.Connected;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (NativeClient == null) return;

                NativeClient.Close();
                NativeClient.Dispose();
                NativeClient = null;
            }
            catch
            {
                // ignore
            }
        }

        public bool SendMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return false;
            if (!NativeClient.Connected) return false;

            var strm = NativeClient.GetStream();
            var writer = new StreamWriter(strm) { AutoFlush = true };

            try
            {
                Logger?.Log.Debug($"<ecos> {msg}");

                if (NativeClient.Connected)
                    writer.WriteLine(msg);

                return true;
            }
            catch (Exception ex)
            {
                SendFailed?.Invoke(this, ex);
                Logger?.Log?.Error("SendMessage failed", ex);
                return false;
            }
        }

        public async Task HandleLines()
        {
            try
            {
                var strm = NativeClient.GetStream();
                using var reader = new StreamReader(strm, Encoding.UTF8);
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                    LineReceived?.Invoke(this, line);
            }
            catch (IOException ex)
            {
                //Logger?.Log?.Error("HandleLines::IOException", ex);
                Failed?.Invoke(this, new MessageEventArgs(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Logger?.Log?.Error("HandleLines::InvalidOperationException", ex);
                Failed?.Invoke(this, new MessageEventArgs(ex.Message));
            }

            Disconnected?.Invoke(this, null!);

            await Task.Delay(1000);
        }
    }
}
