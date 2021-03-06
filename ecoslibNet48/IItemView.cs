// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IItemView.cs

namespace ecoslib
{
    public interface IItemView
    {
        bool HasView { get; }

        void EnableView();
        void DisableView();
    }
}
