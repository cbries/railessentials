// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Accessory.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ecoslib.Utilities;
using Newtonsoft.Json.Linq;

namespace ecoslib.Entities
{
    public class Accessory : Item
    {
        public const int Typeid = 5;

        public override int TypeId() { return Typeid; }

        public override string Caption => $"{ObjectId}: {Name1}, {Addr}, " + string.Join("|", Addrext) + $", {State}, {Type}";

        private readonly string[] _names = { "", "", "" };

        public string Name1
        {
            get => _names[0];
            set
            {
                if (_names[0].Equals(value)) return;
                _names[0] = value;
                _hasChanged = true;
            }
        }

        public string Name2
        {
            get => _names[1];
            set
            {
                if (_names[1].Equals(value)) return;
                _names[1] = value;
                _hasChanged = true;
            }
        }

        public string Name3
        {
            get => _names[2];
            set
            {
                if (_names[2].Equals(value)) return;
                _names[2] = value;
                _hasChanged = true;
            }
        }

        public bool InvertCommand { get; set; } = false;

        public List<string> Addrext { get; set; } = new();

        public int Addr { get; set; } = -1;

        public string Protocol { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Mode { get; set; } = string.Empty;

        public int State { get; set; } = -1;

        public int Switching { get; set; } = -1;

        public override string ToString()
        {
            var ext = string.Join(", ", Addrext);
            return $"{Name1} ({ext}, {ObjectId})";
        }

        public override bool Parse(List<object> arguments)
        {
			foreach (var a in arguments)
			{
				var arg = a as ICommandArgument;
				if (arg == null) continue;

                if (arg.Name.Equals("name1", StringComparison.OrdinalIgnoreCase))
                    Name1 = arg.Parameter[0];
                else if (arg.Name.Equals("name2", StringComparison.OrdinalIgnoreCase))
                    Name2 = arg.Parameter[0];
                else if (arg.Name.Equals("name3", StringComparison.OrdinalIgnoreCase))
                    Name3 = arg.Parameter[0];
                else if (arg.Name.Equals("addrext", StringComparison.OrdinalIgnoreCase))
                {
                    if (Addrext.Count != 0 && Addrext.Equals(arg.Parameter)) continue;
                    _hasChanged = true;

                    Addrext = arg.Parameter;
                }
                else if (arg.Name.Equals("addr", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(arg.Parameter[0], out var v);
                    if (v == Addr) continue;
                    _hasChanged = true;

                    Addr = v;
                }
                else if (arg.Name.Equals("protocol", StringComparison.OrdinalIgnoreCase))
                {
                    if(Protocol.Equals(arg.Parameter[0])) continue;
                    _hasChanged = true;

                    Protocol = arg.Parameter[0];
                }
                else if (arg.Name.Equals("type", StringComparison.OrdinalIgnoreCase))
                {
                    if (Type.Equals(arg.Parameter[0])) continue;
                    _hasChanged = true;

                    Type = arg.Parameter[0];
                }
                else if (arg.Name.Equals("mode", StringComparison.OrdinalIgnoreCase))
                {
                    if (Mode.Equals(arg.Parameter[0])) continue;
                    _hasChanged = true;

                    Mode = arg.Parameter[0];
                }
                else if (arg.Name.Equals("state", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(arg.Parameter[0], out var v);
                    if(v == State) continue;
                    _hasChanged = true;

                    State = v;
                }
#if __ENABLE_SWITCHING
                else if (arg.Name.Equals("switching", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(arg.Parameter[0], out var v);
                    if(Switching == v) continue;
                    _hasChanged = true;

                    Switching = v;
                }
#endif
#if __ENABLE_SYMBOL
                else if (arg.Name.Equals("symbol", StringComparison.OrdinalIgnoreCase))
                {
                    // thats the graphical represenation on the ECoS hardware/display
                }
#endif
                else
                {
                    if (arg.Name.Equals("switching", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (arg.Name.Equals("symbol", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Trace.WriteLine("Unknown argument: " 
                                    + arg.Name + " -> " + string.Join(", ", arg.Parameter));
                }
            }

            return true;
        }

		public override void QueryState()
		{
			AddCmd(CommandFactory.Create($"get({ObjectId}, state)"));
		}

		public override JObject ToJson()
        {
            var o = new JObject
            {
                ["name1"] = Name1,
                ["name2"] = Name2,
                ["name3"] = Name3
            };
            var a0 = new JArray();
            foreach (var e in Addrext)
                a0.Add(e);
            o["objectId"] = ObjectId;
            o["addrext"] = a0;
            o["addr"] = Addr;
            o["protocol"] = Protocol;
            o["type"] = Type;
            o["mode"] = Mode;
            o["state"] = State;
            o["switching"] = Switching;
            o["hasChanged"] = HasChanged;
            return o;
        }

        public override void ParseJson(JObject o)
        {
            if (o == null)
                return;

            if (o["name1"] != null) Name1 = o["name1"].ToString();
            if (o["name2"] != null) Name2 = o["name2"].ToString();
            if (o["name3"] != null) Name3 = o["name3"].ToString();
            if (o["addrext"] != null)
            {
                var a = o["addrext"] as JArray;
                if (a != null)
                {
                    foreach (var e in a)
                        Addrext.Add(e.ToString());
                }
            }
            if (o["objectId"] != null) ObjectId = (int)o["objectId"];
            if (o["addr"] != null)  Addr = (int)o["addr"];
            if (o["protocol"] != null) Protocol = o["protocol"].ToString();
            if (o["type"] != null) Type = o["type"].ToString();
            if (o["mode"] != null)  Mode = o["mode"].ToString();
            if (o["state"] != null) State = (int)o["state"];
            if (o["switching"] != null) Switching = (int)o["switching"];
        }

	    public void Switch(int index)
	    {
		    var s = Addrext[index];
			AddCmd(CommandFactory.Create($"request(11, control, force)"));
		    AddCmd(CommandFactory.Create($"set(11, switch[{Protocol}{s}])"));
            AddCmd(CommandFactory.Create($"release(11, control)"));
	    }

        public void SwitchSimulation(int index)
        {
            _hasChanged = true;

            State = index;
        }
	}
}
