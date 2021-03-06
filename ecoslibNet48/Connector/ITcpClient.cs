// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ITcpClient.cs

using System.Net;
using System.Threading.Tasks;

namespace ecoslib.Connector
{
    public interface ITcpClient
    {
        ILogger Logger { get; set; }
        System.Net.Sockets.TcpClient NativeClient { get; set; }
        System.Threading.Thread ThreadInstance { get; }
        IPAddress Ip { get; }
        int Port { get; }

        bool IsConnected { get; }
        void Disconnect();
        bool SendMessage(string msg);
        Task HandleLines();
    }
}
