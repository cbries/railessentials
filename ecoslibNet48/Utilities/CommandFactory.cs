// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: CommandFactory.cs

using System;

namespace ecoslib.Utilities
{
    public enum CommandT
    {
        Unknown = -1, Get, Set, Create, Request, QueryObjects, Release
    }

    public static class CommandFactory
    {
        public static ICommand Create(string cmdline, bool keepQuotes = false)
        {
            if (string.IsNullOrEmpty(cmdline)) return null;
            var cmdEnd = cmdline.IndexOf("(", StringComparison.OrdinalIgnoreCase);
            if (cmdEnd == -1) return null;
            var cmdName = cmdline.Substring(0, cmdEnd);
            if (cmdName.Length < 3) return null;

            var cmp = StringComparison.OrdinalIgnoreCase;
            ICommand cmd;
            if (cmdName.Equals(Commands.Get.N, cmp))
                cmd = new Commands.Get();
            else if (cmdName.Equals(Commands.Set.N, cmp))
                cmd = new Commands.Set();
            else if (cmdName.Equals(Commands.Create.N, cmp))
                cmd = new Commands.Create();
            else if (cmdName.Equals(Commands.QueryObjects.N, cmp))
                cmd = new Commands.QueryObjects();
            else if (cmdName.Equals(Commands.Release.N, cmp))
                cmd = new Commands.Release();
            else if (cmdName.Equals(Commands.Request.N, cmp))
                cmd = new Commands.Request();
            else
                cmd = new Commands.Unknown();

            cmd.NativeCommand = cmdline;
            cmd.Parse(keepQuotes);

            return cmd;
        }
    }
}
