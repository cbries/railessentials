// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Data.cs

using System.Collections.Generic;

namespace railessentials.Feedbacks
{
    public class Data
    {
        public string BlockId { get; set; }
        public string FbEnter { get; set; }
        public string FbIn { get; set; }
        public List<DeniedLocomotive> DeniedLocomotives { get; set; } = new();
        public Dictionary<string, bool> Settings { get; set; } = new();
    }
}
