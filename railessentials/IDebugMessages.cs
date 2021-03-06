// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IDebugMessages.cs

using System.Collections.Generic;

namespace railessentials
{
    public interface IDebugMessages
    {
        void SendDebugMessages(List<string> msgs, DebugMessageLevel level = DebugMessageLevel.Info);
        void SendDebugMessage(string msg, DebugMessageLevel level = DebugMessageLevel.Info);
        void SendDebug(DebugMessage message);
        void SendDebug(List<DebugMessage> messages);
    }
}
