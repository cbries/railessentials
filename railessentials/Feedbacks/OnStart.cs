// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: OnStart.cs

namespace railessentials.Feedbacks
{
    public class OnStart
    {
        public string Accessory { get; set; }
        public string State { get; set; }
        public StateAfterDelay StateAfterDelay { get; set; }
    }

    public class StateAfterDelay
    {
        public int Seconds { get; set; }
        public string State { get; set; }
    }
}
