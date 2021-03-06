// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Create.cs

namespace ecoslib.Utilities.Commands
{
    public class Create : Command
    {
        public static string N = "create";
        public override CommandT Type => CommandT.Create;
        public override string Name => N;
    }
}
