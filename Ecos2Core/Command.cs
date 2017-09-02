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
using System.Collections.Generic;
using System.Linq;

namespace Ecos2Core
{
    public abstract class Command : ICommand
    {
        public string LastError { get; private set; }

        public bool HasError => !string.IsNullOrEmpty(LastError);

        public abstract CommandT Type { get; }
        public abstract string Name { get; }
        public string NativeCommand { get; set; }

        public int ObjectId
        {
            get
            {
                if (Arguments.Count < 1)
                    return -1;

                int vid;
                if (int.TryParse(Arguments[0].Name, out vid))
                    return vid;

                return -1;
            }
        }

        public List<CommandArgument> Arguments { get; set; } = new List<CommandArgument>();

        public virtual bool Parse(bool keepQuotes=false)
        {
            LastError = null;

            if (string.IsNullOrEmpty(NativeCommand))
            {
                LastError = "Command is empty";
                return false;
            }

            int nOpen = NativeCommand.Count(f => f == '(');
            int nClose = NativeCommand.Count(f => f == ')');
            if (nOpen < 1 || nClose < 1)
            {
                LastError = "Open or closing bracket is missing";
                return false;
            }

            var nativeArguments =
                NativeCommand.Substring(NativeCommand.IndexOf("(", StringComparison.OrdinalIgnoreCase) + 1);

            nativeArguments = nativeArguments.Trim();
            nativeArguments = nativeArguments.TrimEnd(')');
            nativeArguments = nativeArguments.Trim();

            var argumentParts = nativeArguments.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var p in argumentParts)
            {
                if (string.IsNullOrEmpty(p))
                    continue;

                CommandArgument arg = new CommandArgument();
                if (!arg.Parse(p, keepQuotes))
                {
                    LastError = "Parsing of argument list failed: " + p;
                    return false;
                }

                Arguments.Add(arg);
            }

            return true;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return "";

            if (Arguments.Count <= 0)
                return Name + "()";

            return Name + "(" + string.Join(", ", Arguments) + ")";
        }
    }
}
