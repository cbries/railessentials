// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: FeedbacksData.cs

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Analyzer;

namespace railessentials.Feedbacks
{
    public class FeedbacksData
    {
        public Metadata Ctx { get; set; }
        public string FeedbacksPath { get; private set; } = string.Empty;
        public List<Data> Entries { get; set; } = new();

        public FeedbacksData(Metadata ctx)
        {
            Ctx = ctx;
        }

        public Data GetByBlockId(string blockId, bool useStartsWith = false)
        {
            if (string.IsNullOrEmpty(blockId)) return null;
            foreach (var it in Entries)
            {
                if (it == null) continue;
                if (string.IsNullOrEmpty(it.BlockId)) continue;

                if (useStartsWith)
                {
                    if (it.BlockId.StartsWith(blockId, StringComparison.OrdinalIgnoreCase))
                        return it;
                }
                else
                {
                    if (it.BlockId.Equals(blockId, StringComparison.OrdinalIgnoreCase))
                        return it;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockId"></param>
        /// <param name="enterSide"></param>
        /// <returns>If enterSide:=None the identifier is checked by startsWith, otherwise the side is included in the identifier checks.</returns>
        public Data GetByBlockId(string blockId, SideMarker enterSide)
        {
            if (string.IsNullOrEmpty(blockId)) return null;
            foreach (var it in Entries)
            {
                if (it == null) continue;
                if (string.IsNullOrEmpty(it.BlockId)) continue;
                if (!it.BlockId.StartsWith(blockId, StringComparison.OrdinalIgnoreCase)) continue;

                if (enterSide == SideMarker.None)
                    return it;

                var pattern = string.Empty;
                if (enterSide == SideMarker.Minus) pattern = "[-]";
                else if (enterSide == SideMarker.Plus) pattern = "[+]";
                if (!it.BlockId.EndsWith(pattern, StringComparison.Ordinal)) continue;
                return it;
            }
            return null;
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(Entries, Formatting.Indented);
        }

        public JArray ToJson()
        {
            return JArray.Parse(ToJsonString());
        }

        public bool Load(string pathToFeedbacksmodel)
        {
            FeedbacksPath = pathToFeedbacksmodel;
            var json = Metadata.ReadJson(FeedbacksPath);
            Entries = JsonConvert.DeserializeObject<List<Data>>(json);
            return true;
        }

        /// <summary>
        /// Only blocks with "enter" and "in" sensors are valid.
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllValidBlockIds()
        {
            var blocks = new List<string>();

            foreach(var it in Entries)
            {
                if (it == null) continue;
                if (string.IsNullOrEmpty(it.FbEnter)) continue;
                if (string.IsNullOrEmpty(it.FbIn)) continue;
                if (string.IsNullOrEmpty(it.BlockId)) continue;
                blocks.Add(it.BlockId);
            }

            return blocks;
        }
    }
}
