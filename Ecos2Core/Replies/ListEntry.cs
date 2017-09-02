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

namespace Ecos2Core.Replies
{
    public class ListEntry
    {
        private List<CommandArgument> _arguments = new List<CommandArgument>();

        public string OriginalLine { get; private set; }

        public int ObjectId { get; set; }

        public List<CommandArgument> Arguments
        {
            get { return _arguments; }
            set { _arguments = value; }
        }

        public bool Parse(string line)
        {
            OriginalLine = line;

            int n = line.IndexOf(" ", StringComparison.OrdinalIgnoreCase);
            if (n == -1)
                return false;

            string soid = line.Substring(0, n).Trim();
            int ioid = -1;
            if (int.TryParse(soid, out ioid))
                ObjectId = ioid;
            else
                return false;

            string lastPart = line.Substring(n).Trim().TrimEnd('\r', '\n');

            // cleanup
            for (int i = 0; i < 10; ++i)
                lastPart = lastPart.Replace("\" ", "\"");

            lastPart = lastPart.TrimEnd(System.Environment.NewLine.ToCharArray());

            if (lastPart.IndexOf("[", StringComparison.OrdinalIgnoreCase) != -1 &&
                lastPart.IndexOf("]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                var s = "";
                int quoteCount = 0;

                for (int i = 0; i < lastPart.Length; ++i)
                {
                    var c = lastPart[i];

                    if (c == '"')
                        ++quoteCount;

                    if (c == ' ')
                    {
                        if (quoteCount == 2)
                        {
                            quoteCount = 0;

                            CommandArgument arg = new CommandArgument();
                            arg.Parse(s.Trim());
                            Arguments.Add(arg);
                            s = "";
                        }
                        else if (quoteCount == 0)
                        {
                            CommandArgument arg = new CommandArgument();
                            arg.Parse(s.Trim());
                            Arguments.Add(arg);
                            s = "";
                        }
                    }

                    s += c;
                }

                CommandArgument lastArg = new CommandArgument();
                lastArg.Parse(s.Trim());
                Arguments.Add(lastArg);
                s = "";
            }
            else
            {
                var argParts = lastPart.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
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
