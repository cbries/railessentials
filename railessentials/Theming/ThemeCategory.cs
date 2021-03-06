// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ThemeCategory.cs

using System.Collections.Generic;

namespace railessentials.Theming
{
    public class ThemeCategory
    {
        public string Category { get; set; } = string.Empty;
        public List<ThemeItem> Objects { get; set; } = new();
    }
}
