// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Ecos2.cs

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ecoslib.Entities
{
    public class Ecos2 : Item
    {
        public enum State
        {
            Stop,
            Go,
            Shutdown,
            Unknown
        }

        public override string Caption => $"{CurrentState}, {Name}, {ProtocolVersion}, {ApplicationVersion}, {HardwareVersion}";
		
        public string Name => "ECoS2";

        public const int Typeid = 2;

        public override int TypeId() { return Typeid; }

	    public Ecos2()
	    {
		    ObjectId = 1;
	    }

		private readonly string[] _fields = new string[4];

        public string ProtocolVersion
        {
            get => _fields[0];
            set => _fields[0] = value;
        }

        public string ApplicationVersion
        {
            get => _fields[1];
            set => _fields[1] = value;
        }

        public string HardwareVersion
        {
            get => _fields[2];
            set => _fields[2] = value;
        }

        public string Status
        {
            get => _fields[3];
            set => _fields[3] = value;
        }

        public State CurrentState
        {
            get
            {
                if (string.IsNullOrEmpty(Status))
                    return State.Unknown;

                if (Status.Equals("go", StringComparison.OrdinalIgnoreCase))
                    return State.Go;

                if (Status.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    return State.Stop;

                if (Status.Equals("shutdown", StringComparison.OrdinalIgnoreCase))
                    return State.Shutdown;

                return State.Unknown;
            }
        }

        public override bool Parse(List<object> arguments)
        {
            foreach (var a in arguments)
            {
				var arg = a as ICommandArgument;
				if(arg == null) continue;

				if (arg.Name.Equals("status", StringComparison.OrdinalIgnoreCase))
                    Status = arg.Parameter[0];
                else if (arg.Name.Equals("ProtocolVersion", StringComparison.OrdinalIgnoreCase))
                    ProtocolVersion = arg.Parameter[0];
                else if (arg.Name.Equals("ApplicationVersion", StringComparison.OrdinalIgnoreCase))
                    ApplicationVersion = arg.Parameter[0];
                else if (arg.Name.Equals("HardwareVersion", StringComparison.OrdinalIgnoreCase))
                    HardwareVersion = arg.Parameter[0];
            }

            return true;
        }

        public override JObject ToJson()
        {
            var o = new JObject
            {
                ["status"] = Status,
                ["name"] = Name,
                ["protocolVersion"] = ProtocolVersion,
                ["applicationVersion"] = ApplicationVersion,
                ["hardwareVersion"] = HardwareVersion
            };
            return o;
        }

	    public override void ParseJson(JObject obj)
	    {
		    if (obj == null) return;
		    if (obj["status"] != null)
			    Status = obj["status"].ToString();
		    if (obj["protocolVersion"] != null)
			    ProtocolVersion = obj["protocolVersion"].ToString();
		    if (obj["applicationVersion"] != null)
			    ApplicationVersion = obj["applicationVersion"].ToString();
		    if (obj["hardwareVersion"] != null)
			    HardwareVersion = obj["hardwareVersion"].ToString();
		}
	}
}
