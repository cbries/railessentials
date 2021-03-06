// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IListener.cs

using ecoslib.Entities;
using ecoslib.Sniffer;

namespace ecoslib
{
    public interface IListener
    {
        ILogger Logger { get; set; }
        bool IsStarted { get; }
        
        bool Start(bool simulate = false);
        void Stop();

        IDataProvider GetDataProvider();
        IDataProvider GetDataProviderS88();
        
        void StartWsServer();
        void StopWsServer();
        IWsServer GetWsServer();
    }
}
