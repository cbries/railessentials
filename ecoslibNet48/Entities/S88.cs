// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: S88.cs

using System;
using System.Collections.Generic;
using ecoslib.Utilities;
using Newtonsoft.Json.Linq;

namespace ecoslib.Entities
{
    public class S88 : Item
    {
        public const int Typeid = 4;

        public override string Caption => $"{ObjectId}: {StateBinary} ({StateOriginal})";

        public override int TypeId() { return Typeid; }

        private int _index;

        public int Index
        {
            get => _index;
            set => _index = value;
        }

        private int _ports = 16;

        public int Ports
        {
            get => _ports;
            set => _ports = value;
        }

        private string _stateOriginal;

        public string StateOriginal
        {
            get => _stateOriginal;
            set => _stateOriginal = value;
        }

        public string StateBinary => ToBinary(StateOriginal);

        private string ToBinary(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                var m = "";
                for (var i = 0; i < _ports; ++i)
                    m += "0";
                return m;
            }

            return Convert.ToString(Convert.ToInt64(hex, 16), 2).PadLeft(16, '0');
        }

        public override string ToString()
        {
            return $"{ObjectId} {Index}:{Ports} {StateBinary}";
        }

        public bool Pin(uint nr)
        {
            if (_ports < nr) return false;

            try
            {
                var binary = StateBinary;
                binary = Reverse(binary);
                var p = binary[(int)nr];
                return !p.Equals('0');
            }
            catch
            {
                return false;
            }
        }

        public override void QueryState()
        {
            AddCmd(CommandFactory.Create($"get({ObjectId}, state)"));
        }

        public override bool Parse(List<object> arguments)
        {
			foreach (var a in arguments)
			{
				var arg = a as ICommandArgument;
				if (arg == null) continue;

				if (arg.Name.Equals("ports", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(arg.Parameter[0], out v))
                        Ports = v;
                    else
                        Ports = -1;
                }
                else if (arg.Name.Equals("state", StringComparison.OrdinalIgnoreCase))
                {
                    StateOriginal = arg.Parameter[0];
                }
            }

            return true;
        }

        public override JObject ToJson()
        {
            var o = new JObject
            {
                ["objectId"] = ObjectId,
                ["index"] = _index,
                ["ports"] = _ports,
                ["stateOriginal"] = _stateOriginal
            };

            return o;
        }

        public override void ParseJson(JObject o)
        {
            if (o == null) return;

            if (o["objectId"] != null) ObjectId = (int)o["objectId"];
            if (o["index"] != null) Index = (int) o["index"];
            if (o["ports"] != null) Ports = (int) o["ports"];
            if (o["stateOriginal"] != null) StateOriginal = o["stateOriginal"].ToString();
        }

        public void ChangeStateSimulation(int idx)
        {
            if (idx < 0) return;
            if (idx >= _ports) return;

            _hasChanged = true;

            // SET: intValue = intValue | (1 << bitPosition);
            // RESET: intValue &= ~(1 << bitPosition);

            var binary = StateBinary;
            binary = Reverse(binary);
            
            var current = binary[idx];
            if (current == '0')
            {
                binary = binary.Remove(idx, 1);
                binary = binary.Insert(idx, "1");
            }
            else
            {
                binary = binary.Remove(idx, 1);
                binary = binary.Insert(idx, "0");
            }
            
            binary = Reverse(binary);

            var result = Convert.ToUInt16(binary, 2);
            StateOriginal = "0x" + result.ToString("X2");
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
