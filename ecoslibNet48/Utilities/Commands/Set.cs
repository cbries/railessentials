// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Set.cs

namespace ecoslib.Utilities.Commands
{
    public class Set : Command
    {
        public static string N = "set";
        public override CommandT Type => CommandT.Set;
        public override string Name => N;
    }
}
