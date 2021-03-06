// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IPersist.cs

namespace ecoslib
{
    public interface IPersist
    {
        bool Save();
        bool Save(string targetFilepath);
    }
}
