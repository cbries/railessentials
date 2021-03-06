// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: DebugMessage.cs

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace railessentials
{
    public enum DebugMessageLevel
    {
        Info = 1,
        Error = 2,
        Warning = 4,
        Debug = 8
    }

    public class DebugMessage
    {
        public DateTime Time { get; private set; }
        public string Message { get; private set; }
        public DebugMessageLevel Level { get; private set; }

        public override string ToString()
        {
            var tt = Time.ToString("HH:mm:ss");
            var o = new JObject
            {
                ["time"] = tt,
                ["level"] = Level.ToString(),
                ["message"] = Message?.Trim() ?? string.Empty
            };
            return o.ToString(Formatting.None);
        }

        public static DebugMessage Instance(string message, DebugMessageLevel level = DebugMessageLevel.Info)
        {
            var instance = new DebugMessage();
            message ??= string.Empty;
            instance.Message = message.Trim();
            instance.Level = level;
            instance.Time = DateTime.Now;
            return instance;
        }
    }
}
