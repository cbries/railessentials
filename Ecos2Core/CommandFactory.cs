/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;

namespace Ecos2Core
{
    public enum CommandT
    {
        Unknown = -1,
        Test,
        Get, Set, Create, Request, QueryObjects, Release
    }

    public enum ObjectT
    {
        Baseobject = 1,
        ProgrammingTrack = 5,
        LocoManager = 10,
        AccessoryManager = 11,
        FeedbackManager = 26,
        FeedbackModule = 100,
    }

    public static class CommandFactory
    {
        public static ICommand Create(string cmdline, bool keepQuotes=false)
        {
            if (string.IsNullOrEmpty(cmdline))
                return null;

            int cmdEnd = cmdline.IndexOf("(", StringComparison.OrdinalIgnoreCase);
            if (cmdEnd == -1)
                return null;

            string cmdName = cmdline.Substring(0, cmdEnd);
            if (cmdName.Length < 3)
                return null;

            ICommand cmd = null;
            if(cmdName.Equals(Commands.Get.N, StringComparison.OrdinalIgnoreCase))
                cmd = new Commands.Get();
            else if(cmdName.Equals(Commands.Set.N, StringComparison.OrdinalIgnoreCase))
                cmd = new Commands.Set();
            else if(cmdName.Equals(Commands.Create.N, StringComparison.OrdinalIgnoreCase))
                cmd = new Commands.Create();
            else if(cmdName.Equals(Commands.QueryObjects.N, StringComparison.OrdinalIgnoreCase))
                cmd = new Commands.QueryObjects();
            else if(cmdName.Equals(Commands.Release.N, StringComparison.OrdinalIgnoreCase))
                cmd = new Commands.Release();
            else if (cmdName.Equals(Commands.Request.N, StringComparison.OrdinalIgnoreCase))
                cmd = new Commands.Request();
            else if (cmdName.Equals(Commands.Test.N, StringComparison.OrdinalIgnoreCase))
                cmd = new Commands.Test();
            else
                cmd = new Commands.Unknown();

            cmd.NativeCommand = cmdline;
            cmd.Parse(keepQuotes);

            return cmd;
        }
    }
}
