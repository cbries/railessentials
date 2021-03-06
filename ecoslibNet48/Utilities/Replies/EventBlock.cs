// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: EventBlock.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace ecoslib.Utilities.Replies
{
    public class EventBlock : IBlock
    {
        public string StartLine { get; private set; }

        public string EndLine { get; private set; }

        public ICommand Command => null;

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
            return Parse(string.Join(string.Empty, lines));
        }

        public bool Parse(string block)
        {
            NativeBlock = block;

            if (block.IndexOf("<EVENT", StringComparison.OrdinalIgnoreCase) == -1)
                return false;
            if (block.IndexOf("<END", StringComparison.OrdinalIgnoreCase) == -1)
                return false;

            var lines = block.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (var i = 0; i < lines.Count; ++i)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                lines[i] = lines[i].Trim();
            }

            StartLine = lines[0].Trim();
            ParseStart();

            EndLine = lines[lines.Count - 1].Trim();
            ParseEnd();

            lines.RemoveAt(lines.Count - 1);
            lines.RemoveAt(0);

            if (lines.Count < 0)
                return true;

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                var entry = new ListEntry();
                if (entry.Parse(line))
                    ListEntries.Add(entry);
            }

            return true;
        }

        private void ParseStart()
        {
            try
            {
                if (string.IsNullOrEmpty(StartLine)) return;
                var s = StartLine;
                s = s.Replace("<EVENT ", "");
                s = s.Trim().TrimEnd('\r', '\n', '>');
                if (int.TryParse(s, out var oid))
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
