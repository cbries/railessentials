// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ICommand.cs

using System.Collections.Generic;
using ecoslib.Utilities;

namespace ecoslib
{
    public interface ICommand
    {
        CommandT Type { get; }
        string Name { get; }
        string NativeCommand { get; set; }
        int ObjectId { get; }
        List<CommandArgument> Arguments { get; set; }

        bool Parse(bool keepQuotes = false);
    }
}
