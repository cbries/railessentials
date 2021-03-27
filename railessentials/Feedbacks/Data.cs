// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Data.cs

using System.Collections.Generic;

namespace railessentials.Feedbacks
{
    public class Data
    {
        public string BlockId { get; set; } = string.Empty;
        public string FbEnter { get; set; } = string.Empty;
        public string FbIn { get; set; } = string.Empty;
        public List<OnStart> OnStart { get; set; } = new();
        public List<OnStop> OnStop { get; set; } = new();
        public List<DeniedLocomotive> DeniedLocomotives { get; set; } = new();
        public Dictionary<string, bool> Settings { get; set; }
        public List<string> AdditionalBlockLocks { get; set; } = new();
        public string LockedByBlock { get; set; } = string.Empty;

        public Data()
        {
            Settings = new Dictionary<string, bool> {
                {"BlockEnabled", true},
                {"OptionWait", true},
                {"OptionDirection", false},
                {"OptionMainline", true},
                {"OptionBbt", false},
                {"TypeOthers", true},
                {"TypeLocal", true},
                {"TypeIntercity", false},
                {"TypeFreight", true},
                {"TypeShunting", true},
                {"TypeBranchLine", true},
                {"TypeBranchLineFreight", false},
            };
        }
    }
}
