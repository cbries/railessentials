// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ListEntry.cs

using System;
using System.Collections.Generic;

namespace ecoslib.Utilities.Replies
{
    public class ListEntry
    {
        public string OriginalLine { get; private set; }
        public int ObjectId { get; set; }
        public List<object> Arguments { get; set; } = new();

        public bool Parse(string line)
        {
            OriginalLine = line;

            var n = line.IndexOf(" ", StringComparison.OrdinalIgnoreCase);
            if (n == -1)
                return false;

            var soid = line.Substring(0, n).Trim();
            if (int.TryParse(soid, out var ioid))
                ObjectId = ioid;
            else
                return false;

            var lastPart = line.Substring(n).Trim().TrimEnd('\r', '\n');

            // cleanup
            for (var i = 0; i < 10; ++i)
                lastPart = lastPart.Replace("\" ", "\"");

            lastPart = lastPart.TrimEnd(Environment.NewLine.ToCharArray());

            if (lastPart.IndexOf("[", StringComparison.OrdinalIgnoreCase) != -1 &&
                lastPart.IndexOf("]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                var s = string.Empty;
                var quoteCount = 0;

                for (var i = 0; i < lastPart.Length; ++i)
                {
                    var c = lastPart[i];

                    if (c == '"')
                        ++quoteCount;

                    if (c == ' ')
                    {
                        if (quoteCount == 2)
                        {
                            quoteCount = 0;

                            var arg = new CommandArgument();
                            arg.Parse(s.Trim());
                            Arguments.Add(arg);
                            s = "";
                        }
                        else if (quoteCount == 0)
                        {
                            var arg = new CommandArgument();
                            arg.Parse(s.Trim());
                            Arguments.Add(arg);
                            s = "";
                        }
                    }

                    s += c;
                }

                var lastArg = new CommandArgument();
                lastArg.Parse(s.Trim());
                Arguments.Add(lastArg);
            }
            else
            {
                var argParts = lastPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (argParts.Length > 0)
                {
                    foreach (var p in argParts)
                    {
                        if (string.IsNullOrEmpty(p))
                            continue;

                        var cmdArg = new CommandArgument();
                        cmdArg.Parse(p);
                        Arguments.Add(cmdArg);
                    }
                }
            }

            ObjectId = ioid;

            return true;
        }
    }
}
