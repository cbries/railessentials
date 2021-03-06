// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanItemEditor.cs

// ReSharper disable InconsistentNaming
namespace railessentials.Plan
{
    public class PlanItemEditor
    {
        public int themeId { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        private int _themeDimIdx;
        public int themeDimIdx
        {
            get => _themeDimIdx;
            set
            {
                if(value < 0)
                {
                    if (value == -1) _themeDimIdx = 3;
                    else if (value == -2) _themeDimIdx = 2;
                    else if (value == -3) _themeDimIdx = 1;
                    else _themeDimIdx = 0;
                }
                else
                {
                    _themeDimIdx = value;
                }
            }
        }
        public string innerHtml { get; set; }
        public string outerHtml { get; set; }
        public int connectorId { get; set; } = 1;
    }
}
