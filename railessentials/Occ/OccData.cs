// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: OccData.cs

using System;
using System.Collections.Generic;
using ecoslib.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utilities;

namespace railessentials.Occ
{
    public class OccData
    {
        public Metadata Ctx { get; set; }
        public string OccPath { get; private set; } = string.Empty;
        public List<OccBlock> Blocks { get; set; } = new();

        public OccData(Metadata ctx)
        {
            Ctx = ctx;
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(Blocks, Formatting.Indented);
        }

        public JArray ToJson()
        {
            return JArray.Parse(ToJsonString());
        }

        public bool Save()
        {
            try
            {
                var strJson1 = ToJsonString();
                strJson1.FixBomIfNeeded();
                StringUtilities.WriteAllTextNoBom(OccPath, strJson1, out _);
                return true;
            }
            catch
            {
                // ignore
            }

            return false;
        }

        public bool Load(string pathToOccmodel)
        {
            OccPath = pathToOccmodel;
            var json = Metadata.ReadJson(OccPath);
            Blocks = JsonConvert.DeserializeObject<List<OccBlock>>(json);
            return true;
        }

        public bool AssignLocomotive(Locomotive locItem, JObject blockData)
        {
            foreach(var it in Blocks)
            {
                if (it == null) continue;
                if (it.Oid == locItem.ObjectId) 
                    return false; // already assigned
            }

            var blockInstance = new OccBlock
            {
                Oid = locItem.ObjectId,
                FromBlock = blockData["identifier"].ToString()
            };

            Blocks.Add(blockInstance);

            return true;
        }

        public bool ResetAssignment(int oid, string submode = "all")
        {
            if (oid <= 0) return false;

            if (submode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var removeIdx = new List<int>();
                var idx = 0;
                foreach (var it in Blocks)
                {
                    if (it == null) continue;
                    if (it.Oid == oid)
                        removeIdx.Add(idx);
                    idx++;
                }

                removeIdx.Reverse();
                removeIdx.ForEach(i => Blocks.RemoveAt(i));
            }
            else
            {
                foreach (var it in Blocks)
                {
                    if (it == null) continue;
                    if (it.Oid != oid) continue;
                    if (submode.Equals("next", StringComparison.OrdinalIgnoreCase))
                    {
                        it.NextBlock = string.Empty;
                        it.NextEntered = false;
                    }
                    else if (submode.Equals("final", StringComparison.OrdinalIgnoreCase))
                    {
                        it.FinalBlock = string.Empty;
                        it.FinalEntered = false;
                    }
                }
            }

            return true;
        }

        public bool GoToBlock(Locomotive locItem, JObject fromBlock, JObject toBlock)
        {
            ResetAssignment(locItem.ObjectId);

            var blockInstance = new OccBlock
            {
                Oid = locItem.ObjectId,
                FromBlock = fromBlock["identifier"].ToString(),
                FinalBlock = toBlock["identifier"].ToString()
            };

            Blocks.Add(blockInstance);

            return true;
        }
    }
}
