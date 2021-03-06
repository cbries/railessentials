// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: MessageEventArgs.cs

namespace ecoslib.Connector
{
    public class MessageEventArgs : System.EventArgs
    {
        public string Message { get; }
        public System.Exception Exception { get; }

        public MessageEventArgs(string msg, System.Exception ex = null)
        {
            Message = msg;
            Exception = ex;
        }

        public MessageEventArgs(string msg, params object[] args)
        {
            Message = string.Format(msg, args);
        }
    }
}
