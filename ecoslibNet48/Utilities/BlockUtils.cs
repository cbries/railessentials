// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: BlockUtils.cs

using System;
using System.Collections.Generic;
using ecoslib.Utilities.Replies;

namespace ecoslib.Utilities
{
    public static class BlockUtils
    {
        public const char CR = '\r';
        public const char LF = '\n';
        public const string CRLF = "\r\n";

        public static bool HasAnyBlock(string msg)
        {
            var cmp = StringComparison.Ordinal;
            if (string.IsNullOrEmpty(msg)) return false;
            if (msg.IndexOf("<END ", cmp) == -1) return false;
            if (msg.IndexOf("<REPLY ", cmp) != -1) return true;
            if (msg.IndexOf("<EVENT ", cmp) != -1) return true;
            return false;
        }

        public static bool HasAnyBlock(IList<string> lines)
        {
            if (lines == null) return false;
            if (lines.Count < 2) return false;
            var msg = string.Join(CRLF, lines);
            return HasAnyBlock(msg);
        }

        public static IReadOnlyList<IBlock> GetBlocks(string msg)
        {
            var lines = msg.Split(new[] { LF }, StringSplitOptions.RemoveEmptyEntries);
            return GetBlocks(lines);
        }

        public static IReadOnlyList<IBlock> GetBlocks(IList<string> lines)
        {
            var blocks = new List<IBlock>();
            var blockLines = new List<string>();
            foreach (var currentLine in lines)
            {
                if (string.IsNullOrEmpty(currentLine)) continue;
                var line = currentLine.TrimStart(CR, LF);
                if (!line.ToUpper().StartsWith("<END "))
                {
                    blockLines.Add(line + CRLF);
                    continue;
                }

                blockLines.Add(line + CRLF);

                var firstLine = blockLines[0].ToUpper();
                IBlock instance;
                if (firstLine.StartsWith("<EVENT "))
                    instance = new EventBlock();
                else if (firstLine.StartsWith("<REPLY "))
                    instance = new ReplyBlock();
                else continue;

                if (!instance.Parse(blockLines)) return null;
                blocks.Add(instance);
                blockLines.Clear();
            }

            return blocks;
        }
    }
}
