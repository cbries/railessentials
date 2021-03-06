// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Command.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace ecoslib.Utilities
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
                if (Arguments.Count < 1) return -1;
                if (int.TryParse(Arguments[0].Name, out var vid))
                    return vid;
                return -1;
            }
        }

        public List<CommandArgument> Arguments { get; set; } = new();

        public virtual bool Parse(bool keepQuotes = false)
        {
            LastError = null;
            if (string.IsNullOrEmpty(NativeCommand))
            {
                LastError = "Command is empty";
                return false;
            }

            var nOpen = NativeCommand.Count(f => f == '(');
            var nClose = NativeCommand.Count(f => f == ')');
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

            var argumentParts = nativeArguments.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var p in argumentParts)
            {
                if (string.IsNullOrEmpty(p)) continue;
                var arg = new CommandArgument();
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
            if (string.IsNullOrEmpty(Name)) return string.Empty;
            if (Arguments.Count <= 0) return Name + "()";
            return Name + "(" + string.Join(", ", Arguments) + ")";
        }
    }
}
