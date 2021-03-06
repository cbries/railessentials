// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Route.cs

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ecoslib.Entities
{
    public class Route : Item
    {
        private readonly string[] _names = new string[3];

        public const int Typeid = 3;

        public override int TypeId() { return Typeid; }

        public string Name1
        {
            get => _names[0];
            set => _names[0] = value;
        }

        public string Name2
        {
            get => _names[1];
            set => _names[1] = value;
        }

        public string Name3
        {
            get => _names[2];
            set => _names[2] = value;
        }

        public string Type { get; set; }

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
                else if (arg.Name.Equals("type", StringComparison.OrdinalIgnoreCase))
                    Type = arg.Parameter[0];
            }

            return true;
        }

        public override JObject ToJson()
        {
            var o = new JObject
            {
                ["objectId"] = ObjectId,
                ["name1"] = Name1,
                ["name2"] = Name2,
                ["name3"] = Name3,
                ["type"] = Type
            };
            return o;
        }

        public override void ParseJson(JObject o)
        {
            if (o == null)
                return;

            if (o["objectId"] != null)
                ObjectId = (int)o["objectId"];
            if (o["name1"] != null)
                Name1 = o["name1"].ToString();
            if (o["name2"] != null)
                Name1 = o["name2"].ToString();
            if (o["name3"] != null)
                Name1 = o["name3"].ToString();
            if (o["type"] != null)
                Type = o["type"].ToString();
        }
    }
}
