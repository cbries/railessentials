// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: LocomotivesData.cs

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace railessentials.Locomotives
{
    public class LocomotivesData
    {
        public Metadata Ctx { get; set; }
        public string LocomotivesPath { get; private set; } = string.Empty;
        public Dictionary<string, Data> Entries { get; set; } = new();

        public LocomotivesData(Metadata ctx)
        {
            Ctx = ctx;
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(Entries, Formatting.Indented);
        }

        public JObject ToJson()
        {
            return JObject.Parse(ToJsonString());
        }

        public bool Load(string pathToLocomotivesmodel)
        {
            LocomotivesPath = pathToLocomotivesmodel;
            var json = Metadata.ReadJson(LocomotivesPath);
            Entries = JsonConvert.DeserializeObject<Dictionary<string, Data>>(json);
            return true;
        }

        public Data GetData(int oid)
        {
            if (oid <= 0) return null;
            foreach(var it in Entries)
            {
                var oidKey = it.Key;
                if (oidKey.Equals($"{oid}", StringComparison.Ordinal))
                    return it.Value;
            }
            return null;
        }

        public void SetLocked(int oid, bool state)
        {
            if (oid <= 0) return;
            var data = GetData(oid);
            if(data != null)
                data.IsLocked = state;
        }
    }
}
