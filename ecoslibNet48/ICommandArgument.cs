// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ICommandArgument.cs

using System.Collections.Generic;

namespace ecoslib
{
    public interface ICommandArgument
    {
        string Name { get; set; }
        List<string> Parameter { get; set; }
    }
}
