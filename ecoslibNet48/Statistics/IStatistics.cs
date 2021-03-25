// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IStatistics.cs

namespace ecoslib.Statistics
{
    public interface IStatistics
    {
        bool LocomotiveStart(int locomotiveId);
        bool LocomotiveStop(int locomotiveId);
        void IncrementTouchCount(int accessoryId);
    }
}
