// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Sniffer.WsServer.cs

namespace ecoslib.Sniffer
{


    public partial class Sniffer
    {
        private IWsServer _wsServer;
        
        public bool WsServerEnabled { get; private set; }

        public void StartWsServer()
        {
            InitWsServerInstance();
            if (!WsServerEnabled)
                _wsServer?.Listen();
            WsServerEnabled = true;
        }

        public void StopWsServer()
        {
            InitWsServerInstance();
            if (WsServerEnabled)
                _wsServer?.Stop();
            WsServerEnabled = false;
        }

        public IWsServer GetWsServer()
        {
            return _wsServer ??= new WsServer
            {
                Logger = Logger
            };
        }

        private void InitWsServerInstance()
        {
            GetWsServer();
        }
    }
}
