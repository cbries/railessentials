// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IConnector.cs

using System;
using System.Collections.Generic;

namespace ecoslib.Connector
{
    public interface IConnector
    {
        ILogger Logger { get; set; }
        string IpAddress { get; set; }
        UInt16 Port { get; set; }

        bool Start();
        bool Stop();

        bool SendMessage(string commandMessage);
        bool SendCommand(object command);
        bool SendCommands(IReadOnlyList<object> commands);
    }
}
