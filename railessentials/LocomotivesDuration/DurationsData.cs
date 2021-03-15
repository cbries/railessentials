// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: LocomotivesDuration.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace railessentials.LocomotivesDuration
{
    public class DurationsData
    {
        public Metadata Ctx { get; set; }
        public string DurationsPath { get; private set; } = string.Empty;
        public Dictionary<string, Data> Entries { get; set; } = new();

        public DurationsData(Metadata ctx)
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

        public string ToJsonDecelerationDurations()
        {
            var avr = new AverageDurations();
            
            foreach(var it in Entries)
            {
                var slocId = it.Key;
                if (avr.ContainsKey(slocId))
                    continue;

                if (!int.TryParse(slocId, out var locId))
                    continue;

                var entries = new List<AverageDurationEntry>();
                foreach (var itt in it.Value.DecelerateDurations)
                {
                    var blockId = itt.Key;
                    var blockDuration = GetAverageDecelerationTime(locId, blockId);

                    var entry = new AverageDurationEntry
                    {
                        BlockId = blockId,
                        Duration = blockDuration
                    };

                    entries.Add(entry);
                }

                avr[slocId] = entries;
            }

            return JsonConvert.SerializeObject(avr);
        }

        public bool Load(string pathToDurationsModel)
        {
            DurationsPath = pathToDurationsModel;
            var json = Metadata.ReadJson(DurationsPath);
            Entries = JsonConvert.DeserializeObject<Dictionary<string, Data>>(json);
            return true;
        }

        public Data GetData(int oid)
        {
            if (oid <= 0) return null;
            foreach (var it in Entries)
            {
                var oidKey = it.Key;
                if (oidKey.Equals($"{oid}", StringComparison.Ordinal))
                    return it.Value;
            }
            return null;
        }

        public Data GetData(int oid, bool createAndAdd)
        {
            var locdata = GetData(oid);
            if (locdata == null && createAndAdd)
            {
                locdata = new Data();
                Entries.Add($"{oid}", locdata);
            }
            return locdata;
        }

        public bool AddDuration(int locomotiveId, DateTime start, DateTime stop)
        {
            if (locomotiveId < 0) return false;
            if (stop <= start) return false;

            var locdata = GetData(locomotiveId, true);
            var currentNo = locdata.Durations.Count;
            var targetDuration = new Duration
            {
                Start = start,
                Stop = stop
            };
            locdata.Durations.Add(targetDuration);
            return locdata.Durations.Count > currentNo;
        }

        public bool AddDecelerateDuration(int locomotiveId, string blockId, DateTime start, DateTime stop)
        {
            if (locomotiveId < 0) return false;
            if (string.IsNullOrEmpty(blockId)) return false;
            if (stop <= start) return false;

            var locdata = GetData(locomotiveId, true);
            var currentNo = locdata.DecelerateDurations.Count;

            List<Duration> targetDurations = null;
            var decDurations = locdata.DecelerateDurations;
            foreach (var it in decDurations)
            {
                if (string.IsNullOrEmpty(it.Key)) continue;
                if (it.Key.Equals(blockId, StringComparison.OrdinalIgnoreCase))
                {
                    targetDurations = it.Value;
                    break;
                }
            }

            if (targetDurations == null)
                targetDurations = new List<Duration>();

            var newDuration = new Duration
            {
                Start = start,
                Stop = stop
            };
            
            while(targetDurations.Count >= Data.MaxDecelerateEntries)
                targetDurations.RemoveAt(0);
            targetDurations.Add(newDuration);
            
            if (locdata.DecelerateDurations.ContainsKey(blockId))
                locdata.DecelerateDurations[blockId] = targetDurations;
            else
                locdata.DecelerateDurations.Add(blockId, targetDurations);

            var blockDecl = locdata.DecelerateDurations[blockId];

            var res = blockDecl.Count > currentNo 
                      || blockDecl.Count == Data.MaxDecelerateEntries;

            return res;
        }

        public bool CleanupDeceleration(int locomotiveId, string blockId)
        {
            if (locomotiveId <= 0) return false;
            var locdata = GetData(locomotiveId, false);
            if (locdata == null) return true;
            if (!locdata.DecelerateDurations.ContainsKey(blockId)) return true;
            var blockDecl = locdata.DecelerateDurations[blockId];
            if (blockDecl == null) return true;
            locdata.DecelerateDurations[blockId] = new List<Duration>();
            return true;
        }

        public bool Remove(int locomotiveId)
        {
            if (locomotiveId <= 0) return false;
            if (!Entries.ContainsKey($"{locomotiveId}")) return true;
            return Entries.Remove($"{locomotiveId}");
        }

        public int GetNoOfDecelerationsEntries(int locomotiveId, string blockId)
        {
            if (locomotiveId <= 0) return -1;
            if (string.IsNullOrEmpty(blockId)) return -1;
            var locdata = GetData(locomotiveId, true);
            var blockDecl = locdata.DecelerateDurations[blockId];
            if (blockDecl == null) return -1;
            var currentNo = blockDecl.Count;
            return currentNo;
        }

        public double GetAverageDecelerationTime(int locomotiveId, string blockId, int defaultSeconds = 10)
        {
            if (locomotiveId <= 0) return defaultSeconds;
            if (string.IsNullOrEmpty(blockId)) return defaultSeconds;
            var locData = GetData(locomotiveId, false);
            if (locData == null) return defaultSeconds;
            if (!locData.DecelerateDurations.ContainsKey(blockId)) return defaultSeconds;
            var blockDecl = locData.DecelerateDurations[blockId];
            if (blockDecl == null) return defaultSeconds;
            var noOfEntries = 0;
            var cummu = 0.0;
            foreach(var it in blockDecl)
            {
                if (it.Start == it.Stop) continue;
                if (it.Stop < it.Start) continue;

                ++noOfEntries;
                var delta = it.Stop - it.Start;
                var seconds = delta.TotalSeconds;
                cummu += seconds;
            }
            return cummu / noOfEntries;
        }
    }
}
