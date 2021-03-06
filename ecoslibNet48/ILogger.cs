// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ILogger.cs

namespace ecoslib
{
    public interface ILogger
    {
        log4net.ILog Log { get; }
    }
}
