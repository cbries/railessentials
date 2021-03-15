// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Route.cs

using System.Collections.Generic;
using System.Linq;
using ecoslib.Entities;
using Newtonsoft.Json;

namespace railessentials.Plan
{
    public class Route
    {
        [JsonIgnore]
        public int NumberOfItems => Items.Count;
        public List<NextStep> Items { get; set; } = new();
        public PlanItem Start
        {
            get
            {
                if (Items == null || NumberOfItems == 0)
                    return null;
                return Items.First()?.Item;
            }
        }
        public PlanItem Target
        {
            get
            {
                if (Items == null || NumberOfItems == 0)
                    return null;
                return Items.Last()?.Item;
            }
        }

        public string DisplayName
        {
            get
            {
                var startIdentifier = Start.identifier;
                var targetIdentifier = Target.identifier;

                var xStart = Start.coord.x;
                var yStart = Start.coord.y;

                var nextItem = Items[1];
                var x0 = nextItem.Item.coord.x;
                var y0 = nextItem.Item.coord.y;

                var xTarget = Target.coord.x;
                var yTarget = Target.coord.y;

                var previousItem = Items[Items.Count - 2];
                var x1 = previousItem.Item.coord.x;
                var y1 = previousItem.Item.coord.y;

                bool? isLeavingPlus = null;
                if(xStart == x0)
                {
                    // check y;
                    if (y0 < yStart) isLeavingPlus = true;
                    else isLeavingPlus = false;
                }
                else if(yStart == y0)
                {
                    // check x
                    if (x0 < xStart) isLeavingPlus = true;
                    else isLeavingPlus = false;
                }

                bool? isEnteringPlus = null;
                if(xTarget == x1)
                {
                    // check y
                    if (y1 < yTarget) isEnteringPlus = true;
                    else isEnteringPlus = false;
                }
                else if(yTarget == y1)
                {
                    // check x
                    if (x1 < xTarget) isEnteringPlus = true;
                    else isEnteringPlus = false;
                }

                string startSuffix;
                string targetSuffix;
                
                if (isLeavingPlus == null) startSuffix = "[?]";
                else if (isLeavingPlus.Value) startSuffix = "[+]";
                else startSuffix = "[-]";

                if (isEnteringPlus == null) targetSuffix = "[?]";
                else if (isEnteringPlus.Value) targetSuffix = "[+]";
                else targetSuffix = "[-]";

                return $"{startIdentifier}{startSuffix}_{targetIdentifier}{targetSuffix}";
            }
        }

        /// <summary>
        /// NOTE: not thread-safe
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<PlanItem> Get(PlanItemT type)
        {
            var result = new List<PlanItem>();
            foreach(var it in Items)
            {
                var itItem = it?.Item;
                if (itItem == null) continue;
                if(type == PlanItemT.Block && itItem.IsBlock)
                    result.Add(itItem);
                else if(type == PlanItemT.Feedback && itItem.IsSensor)
                    result.Add(itItem);
                else if(type == PlanItemT.Signal && itItem.IsSignal)
                    result.Add(itItem);
                else if(type == PlanItemT.Switch && itItem.IsSwitch)
                    result.Add(itItem);
                else if(type == PlanItemT.Track && itItem.IsTrack)
                    result.Add(itItem);
            }
            return result;
        }

        /// <summary>
        /// NOTE: not thread-safe
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public PlanItem Get(string identifier)
        {
            foreach (var it in Items)
            {
                if (string.IsNullOrEmpty(it?.Item?.identifier))
                    continue;
                if (it.Item.identifier.Equals(identifier))
                    return it.Item;
            }

            return null;
        }

        /// <summary>
        /// NOTE: not thread-safe
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public NextStep GetStep(string identifier)
        {
            foreach (var it in Items)
            {
                if (string.IsNullOrEmpty(it?.Item?.identifier))
                    continue;
                if (it.Item.identifier.Equals(identifier))
                    return it;
            }

            return null;
        }

        public override string ToString()
        {
            var start = Start.coord;
            var target = Target.coord;

            return $"Start({start.x}, {start.y}) -> Target({target.x}, {target.y})";
        }
    }
}
