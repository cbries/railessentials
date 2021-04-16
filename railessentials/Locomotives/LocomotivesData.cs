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

        public void SetCleaner(int oid, bool state)
        {
            if (oid <= 0) return;
            var data = GetData(oid);
            if (data != null)
                data.IsCleaner = state;
        }

        public void SetLocked(int oid, bool state)
        {
            if (oid <= 0) return;
            var data = GetData(oid);
            if(data != null)
                data.IsLocked = state;
        }

        public void SetAllLocomotivesStopped()
        {
            if (Entries == null) return;
            foreach (var it in Entries)
                it.Value.IsStopped = true;
        }

        public Dictionary<string, int> GetSpeedLevels(int oid)
        {
            var locData = GetData(oid);
            var res = new Dictionary<string, int>();
            if (locData == null) return res;
            foreach(var it in locData.SpeedLevels)
            {
                if (res.ContainsKey(it.Key))
                    res[it.Key] = it.Value.Value;
                else
                    res.Add(it.Key, it.Value.Value);
            }

            return res;
        }

        public bool SetSpeedLevel(int oid, string level, int value)
        {
            var locData = GetData(oid);
            var speedLevels = locData?.SpeedLevels ?? new Dictionary<string, SpeedLevel>();
            if (!speedLevels.ContainsKey(level) || speedLevels[level] == null)
                speedLevels[level] = new SpeedLevel();
            speedLevels[level].Value = value;
            return true;
        }
    }
}
