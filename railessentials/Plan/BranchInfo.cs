// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: BranchInfo.cs

using System.Collections.Generic;
using System.Diagnostics;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode

namespace railessentials.Plan
{
    [DebuggerDisplay("{Item?.identifier} ({Item?.coord.x}, {Item?.coord.y} | {GetThemeSwitchPrefix()})")]
    public class NextStep
    {
        public PlanItem Item { get; set; } = new();

        /// <summary>
        /// During route analyzing this property provides
        /// information about the previous item path to go.
        /// </summary>
        public Path PreviousItemPath { get; set; } = new();

        public string GetThemeSwitchPrefix()
        {
            if (Item == null) return string.Empty;
            if (!Item.IsSwitch) return string.Empty;
            if (PreviousItemPath == null) return string.Empty;
            var fromSide = PreviousItemPath.FromSide;
            var toSide = PreviousItemPath.ToSide;

            var statesKey = "deg";
            if (Item.editor.themeDimIdx == 0) statesKey += "0";
            else if (Item.editor.themeDimIdx == 1) statesKey += "90";
            else if (Item.editor.themeDimIdx == 2) statesKey += "180";
            else if (Item.editor.themeDimIdx == 3) statesKey += "270";

            var itemSource = fromSide.ToString().ToLower();
            var itemTo = toSide.ToString().ToLower();

            if (itemSource == null) return string.Empty;
            if (itemTo == null) return string.Empty;

            if (!Item.states.ContainsKey(statesKey))
                return string.Empty;

            var states = Item.states[statesKey];

            foreach(var state in states ?? new List<PlanSwitchState>())
            {
                var stateSource = state.source.ToLower();
                var stateTo = state.to.ToLower();

                if (stateSource.Equals(itemSource) && stateTo.Equals(itemTo))
                    return state.state;
            }

            return string.Empty;
        }
    }

    public class BranchInfo
    {
        public NextStep Item { get; set; } = new();
        public List<NextStep> RecentItems { get; set; } = new();
        public List<NextStep> NextAllowedSteps { get; set; } = new();
    }
}
