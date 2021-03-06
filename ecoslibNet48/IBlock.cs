// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IBlock.cs

using System.Collections.Generic;
using ecoslib.Utilities.Replies;

namespace ecoslib
{
    public interface IBlock
    {
        ICommand Command { get; }
        int? ObjectId { get; }
        string NativeBlock { get; set; }
        string StartLine { get; }
        string EndLine { get; }
        ReplyResult Result { get; }
        List<ListEntry> ListEntries { get; }

        bool Parse(IReadOnlyList<string> lines);
        bool Parse(string block);
    }
}
