// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Statistics.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Exception = System.Exception;

namespace ecoslib.Statistics
{
    public class Statistics : List<Entry>, IStatistics
    {
        private readonly object _lock = new();

        [JsonIgnore]
        public ILogger Logger { get; set; }

        [JsonIgnore]
        public string Path { get; set; }

        public static Statistics Instance(string loadPath, ILogger logger)
        {
            if (string.IsNullOrEmpty(loadPath))
                return new Statistics { Logger = logger, Path = loadPath };
            if (!File.Exists(loadPath))
                return new Statistics { Logger = logger, Path = loadPath };
            try
            {
                var cnt = File.ReadAllText(loadPath, Encoding.UTF8);
                var instance = JsonConvert.DeserializeObject<Statistics>(cnt) ?? new Statistics();
                instance.Path = loadPath;
                return instance;
            }
            catch(Exception ex)
            {
                logger?.Log?.Error(ex);
            }

            return new Statistics { Logger = logger, Path = loadPath };
        }

        public bool Save()
        {
            try
            {
                string json;
                lock(_lock)
                    json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(Path, json, Encoding.UTF8);
                return true;
            }
            catch(Exception ex)
            {
                Logger?.Log?.Error(ex);
                return false;
            }
        }

        public Entry GetData(int oid, bool createAndAdd)
        {
            var statData = GetData(oid);
            if (statData != null || !createAndAdd) return statData;
            statData = new Entry { ObjectId = oid };
            lock(_lock) Add(statData);
            return statData;
        }

        public Entry GetData(int objectId)
        {
            if (objectId <= 0) return null;

            lock (_lock)
            {
                foreach (var it in this.Where(it => it.ObjectId == objectId))
                    return it;
            }

            return null;
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

        public bool LocomotiveStart(int locomotiveId)
        {
            if (locomotiveId <= 0) return false;
            var locdata = GetData(locomotiveId, true);
            if (locdata == null) return false;
            if (locdata.IsStarted) return true;
            locdata.Started = DateTime.Now;
            locdata.IsStarted = true;
            return true;
        }

        public bool LocomotiveStop(int locomotiveId)
        {
            if (locomotiveId <= 0) return false;
            var locdata = GetData(locomotiveId, true);
            if (locdata == null) return false;

            // valid entries must be started first, otherwise we ignore them
            if (!locdata.IsStarted) return false;
            
            var r = AddDuration(locomotiveId, locdata.Started, DateTime.Now);
            locdata.IsStarted = false;
            locdata.Started = DateTime.MaxValue;
            return r;
        }

        public void IncrementTouchCount(int accessoryId)
        {
            if (accessoryId <= 0) return;
            var item = GetData(accessoryId);
            lock (_lock)
            {   
                if(item == null)
                {
                    var touchInstance = new Touch
                    {
                        DateTime = DateTime.Now
                    };

                    var entryInstance = new Entry
                    {
                        ObjectId = accessoryId
                    };
                    
                    entryInstance.Touches.Add(touchInstance);

                    return;
                }

                item.Touches.Add(new Touch{DateTime = DateTime.Now});
            }
        }

        public int GetTouchCount(int accessoryId)
        {
            if (accessoryId <= 0) return 0;
            var item = GetData(accessoryId);
            if (item == null) return 0;
            return item.Touches.Count;
        }
    }
}
