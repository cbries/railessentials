// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Release.cs

namespace ecoslib.Utilities.Commands
{
    public class Release : Command
    {
        public static string N = "release";
        public override CommandT Type => CommandT.Release;
        public override string Name => N;
    }
}
