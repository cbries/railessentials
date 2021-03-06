// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Request.cs

namespace ecoslib.Utilities.Commands
{
    public class Request : Command
    {
        public static string N = "request";
        public override CommandT Type => CommandT.Request;
        public override string Name => N;
    }
}
