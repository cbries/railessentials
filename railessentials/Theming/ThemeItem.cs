// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ThemeItem.cs

using System.Collections.Generic;

namespace railessentials.Theming
{
    public class ThemeItem
    {
        public int Id { get; set; } = -1;
        public string Name { get; set; } = string.Empty;
        public string BaseName { get; set; } = string.Empty;
        public bool Clickable { get; set; } = false;
        public List<string> Routes { get; set; } = new();

        public List<ThemeDimension> Dimensions { get; set; } = new();
        public Dictionary<string, List<ThemeSwitchState>> States { get; set; } = new();
    }
}
