// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IItem.cs

using System.Collections.Generic;

namespace ecoslib
{
    public interface IItem
    {
        int ObjectId { get; set; }
        //IReadOnlyList<object> Commands { get; }
        string Caption { get; }
        bool HasChanged { get; }

        void ResetChangedState();

        void AddCmd(ICommand cmd);
        void ResetCmds();
        IReadOnlyList<object> GetCommands(bool cleanOriginal = true);

        int TypeId();

        bool Parse(List<object> arguments);
        void QueryState();
    }
}
