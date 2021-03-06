// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: OccBlock.cs

using System;

namespace railessentials.Occ
{
    public class OccBlock
    {
        public int Oid { get; set; }
        public string FromBlock { get; set; } = string.Empty;
        public string NextBlock { get; set; } = string.Empty;
        public string FinalBlock { get; set; } = string.Empty;
        
        public string RouteToNext { get; set; }
        public string RouteToFinal { get; set; }

        /// <summary>
        /// true:=when the fbEnter is reached
        /// </summary>
        public bool NextEntered { get; set; }

        /// <summary>
        /// true:=when the fbEnter is reached
        /// </summary>
        public bool FinalEntered { get; set; }

        public DateTime ReachedTime { get; set; }
        public int SecondsToWait { get; set; }
    }
}