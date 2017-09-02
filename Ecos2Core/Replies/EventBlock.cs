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

namespace Ecos2Core.Replies
{
    public class EventBlock : IBlock
    {
        public string StartLine { get; private set; }

        public string EndLine { get; private set; }

        public ICommand Command {  get { return null; } }

        public int? ObjectId { get; private set; }

        public string NativeBlock { get; set; }

        public ReplyResult Result { get; private set; }

        public List<ListEntry> ListEntries { get; private set; }

        public EventBlock()
        {
            ListEntries = new List<ListEntry>();
        }

        public bool Parse(IReadOnlyList<string> lines)
        {
            return Parse(string.Join("", lines));
        }

        public bool Parse(string block)
        {
            NativeBlock = block;

            if (block.IndexOf("<EVENT", StringComparison.OrdinalIgnoreCase) == -1)
                return false;
            if (block.IndexOf("<END", StringComparison.OrdinalIgnoreCase) == -1)
                return false;

            var lines = block.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = 0; i < lines.Count; ++i)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                lines[i] = lines[i].Trim();
            }

            StartLine = lines[0].Trim();
            ParseStart();

            EndLine = lines[lines.Count-1].Trim();
            ParseEnd();

            lines.RemoveAt(lines.Count - 1);
            lines.RemoveAt(0);

            if (lines.Count < 0)
                return true;

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                ListEntry entry = new ListEntry();
                if (entry.Parse(line))
                    ListEntries.Add(entry);
            }

            return true;
        }

        private void ParseStart()
        {
            try
            {
                if (string.IsNullOrEmpty(StartLine))
                    return;

                string s = StartLine;
                s = s.Replace("<EVENT ", "");
                s = s.Trim().TrimEnd('\r', '\n', '>');
                int oid;
                if (int.TryParse(s, out oid))
                    ObjectId = oid;
                else
                    ObjectId = null;
            }
            catch
            {
                // ignore
            }
        }

        private void ParseEnd()
        {
            try
            {
                if (string.IsNullOrEmpty(StartLine))
                    return;

                Result = new ReplyResult();
                Result.Parse(EndLine);
            }
            catch
            {
                // ignore
            }
        }
    }
}
