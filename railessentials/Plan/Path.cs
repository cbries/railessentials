// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Path.cs

namespace railessentials.Plan
{
    public class Path
    {
        public enum Side { None, Left, Top, Right, Bottom }

        public PlanItem From { get; set; }
        public PlanItem To { get; set; }

        public Side FromSide { get; internal set; } = Side.None;
        public Side ToSide { get; internal set; } = Side.None;

        public override string ToString()
        {
            var f = From?.identifier ?? "???";
            var t = To?.identifier ?? "???";
            
            return $"{f}({FromSide}) -> {t}({ToSide})";
        }
    }
}