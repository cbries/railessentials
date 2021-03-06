// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: CommandArgument.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace ecoslib.Utilities
{
    public class CommandArgument : ICommandArgument
    {
        public string Name { get; set; }
        public List<string> Parameter { get; set; } = new();

        public override string ToString()
        {
            if (Parameter.Count <= 0) return Name;
            var pps = new List<string>(Parameter);
            for (var i = 0; i < pps.Count; ++i)
            {
                var p = pps[i];
                if (p.IndexOf(" ", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    if (p[0] == '"' && p[p.Length - 1] == '"')
                        pps[i] = p;
                    else
                        pps[i] = "\"" + p + "\"";
                }
            }

            return $"{Name}[{string.Join(",", pps)}]";
        }

        public bool Parse(string argument, bool keepQuotes = false)
        {
            if (string.IsNullOrEmpty(argument)) return false;
            if (argument.IndexOf("[", StringComparison.OrdinalIgnoreCase) != -1
               && argument.IndexOf("]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                var index = argument.IndexOf("[", StringComparison.OrdinalIgnoreCase);
                Name = argument.Substring(0, index).Trim();
                var args = argument.Substring(index + 1).Trim().TrimEnd(']').Trim();
                if (string.IsNullOrEmpty(args))
                {
                    Parameter.Clear();
                }
                else
                {
                    Parameter = args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    for (var i = 0; i < Parameter.Count; ++i)
                    {
                        if (string.IsNullOrEmpty(Parameter[i])) continue;
                        if (!keepQuotes)
                            Parameter[i] = Parameter[i].Trim().Trim('"');
                        else
                        {
                            Parameter[i] = Parameter[i];
                        }
                    }
                }
            }
            else
            {
                Name = argument.Trim();
                Parameter.Clear();
            }

            return true;
        }
    }
}
