// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanField.cs

using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable ForCanBeConvertedToForeach

namespace railessentials.Plan
{
    public class PlanField : Dictionary<string, PlanItem>
    {
        private readonly object _lock = new();

        public void InitContext()
        {
            lock (_lock)
            {
                foreach (var it in this)
                {
                    var v = it.Value;
                    v.Ctx = this;
                }
            }
        }

        public int GetMaxWidth()
        {
            lock (_lock)
            {
                var w = 0;

                foreach (var it in this)
                {
                    var item = it.Value;
                    if (item == null) continue;

                    if (item.coord.x > w)
                        w = item.coord.x;

                    var themeDimX = 0;
                    if (item.editor != null)
                        themeDimX = item.editor.themeDimIdx;

                    if (item.dimensions != null && item.dimensions.Count > themeDimX)
                    {
                        var dim = item.dimensions[themeDimX];
                        if (dim.W > 1)
                        {
                            var xw = item.coord.x + dim.W;
                            if (xw > w)
                                w = xw;
                        }
                    }
                }

                return w;
            }
        }

        public int GetMaxHeight()
        {
            lock (_lock)
            {
                var h = 0;

                foreach (var it in this)
                {
                    var item = it.Value;
                    if (item == null) continue;

                    if (item.coord.y > h)
                        h = item.coord.y;

                    var themeDimY = 0;
                    if (item.editor != null)
                        themeDimY = item.editor.themeDimIdx;

                    if (item.dimensions != null && item.dimensions.Count > themeDimY)
                    {
                        var dim = item.dimensions[themeDimY];
                        if (dim.H > 1)
                        {
                            var yh = item.coord.y + dim.H;
                            if (yh > h)
                                h = yh;
                        }
                    }
                }

                return h;
            }
        }

        public PlanItem Get(string identifier)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(identifier)) return null;
                foreach (var it in this)
                {
                    var item = it.Value;
                    if (string.IsNullOrEmpty(item?.identifier)) continue;

                    if (item.identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        return item;
                }

                return null;
            }
        }

        public PlanItem Get(int x, int y)
        {
            lock (_lock)
            {
                foreach (var it in this)
                {
                    var item = it.Value;
                    if (item == null) continue;

                    var startCoord = item.StartCoord();
                    var endCoord = item.EndCoord();

                    if (x >= startCoord.x
                        && x <= endCoord.x
                        && y >= startCoord.y
                        && y <= endCoord.y)
                    {
                        return item;
                    }
                }

                return null;
            }
        }

        public PlanItem[] GetConnectors(int connectorId)
        {
            lock (_lock)
            {
                if (connectorId == -1) return null;
                var items = new PlanItem[2];
                var index = 0;

                foreach (var it in this)
                {
                    var item = it.Value;
                    if (!item.IsConnector) continue;
                    if (item.editor.connectorId <= 1) continue;
                    if (item.editor.connectorId == connectorId)
                    {
                        items[index] = item;
                        index++;

                        if (index == 2)
                            return items;
                    }
                }

                return items;
            }
        }

        public List<PlanItem> GetByGroupName(string groupName)
        {
            lock (_lock)
            {
                var result = new List<PlanItem>();

                foreach (var it in this)
                {
                    var v = it.Value;
                    if (v == null) continue;
                    var grpName = v.GroupName;
                    if (string.IsNullOrEmpty(grpName)) continue;
                    if (grpName.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                        result.Add(v);
                }

                return result;
            }
        }

        public PlanItem GetSensorByAddr(int addr)
        {
            lock (_lock)
            {
                foreach (var it in this)
                {
                    var v = it.Value;
                    if (v == null) continue;
                    if (!v.IsSensor) continue;

                    var itemAddr = v.Addresses;
                    if (itemAddr == null) continue;
                    if (itemAddr.Addr <= 0) continue;

                    if (itemAddr.Addr == addr)
                        return v;
                }

                return null;
            }
        }

        public List<PlanItem> GetBlocks()
        {
            lock (_lock)
            {
                var result = new List<PlanItem>();

                foreach (var it in this)
                {
                    var v = it.Value;
                    if (v == null) continue;
                    if (v.IsBlock)
                        result.Add(v);
                }

                return result;
            }
        }

        private readonly Stack<BranchInfo> _branches = new();

        /// <summary>
        /// This method is NOT thread-safe !!
        /// Searches all posible routes with `startBlock` as starting point.
        /// Only between blocks routes are allowed.
        /// Keep in mind, any call of this method calculates the route
        /// from the beginning and can have a deep impact on application
        /// performance when periodically called.
        /// </summary>
        /// <param name="startBlock"></param>
        /// <returns></returns>
        public RouteList GetRoutes(PlanItem startBlock)
        {
            var res = new RouteList();
            if (!startBlock.IsBlock) return res;

            var allowedPath = startBlock.GetAllowedPath();
            if (allowedPath.Count == 0) return res;

            foreach (var it in allowedPath)
            {
                var startStep = new NextStep { Item = startBlock };
                var result = new List<NextStep> { startStep };
                var r = Walk(startBlock, new NextStep { Item = it.To }, ref result);
                if (r)
                {
                    var route = new Route();
                    result.ForEach(itr => route.Items.Add(itr));
                    res.Add(route);
                }
            }

            // check for branches
            if (_branches.Count > 0)
            {
                BranchInfo branch;

                while ((branch = _branches.Pop()) != null)
                {
                    for (var j = 0; j < branch.NextAllowedSteps.Count; ++j)
                    {
                        var nextStep = branch.NextAllowedSteps[j];

                        // ## IMPORTANT IMPLEMENTATION NOTICE ##
                        // we have to make a copy of the PreviousItemPath
                        // attribute because it is a ref-value and any change
                        // will modify any previously analyzed route and
                        // all states are reseted, i.e. the last change
                        // would be always applied to any route -- that
                        // is absolutly incorrect and not acceptable
                        var copyOfRecentItems = new List<NextStep>();
                        foreach (var itRecent in branch.RecentItems)
                        {
                            if (itRecent == null) continue;
                            var instance = new NextStep
                            {
                                Item = itRecent.Item,
                                PreviousItemPath = new Path
                                {
                                    FromSide = itRecent.PreviousItemPath.FromSide,
                                    ToSide = itRecent.PreviousItemPath.ToSide,
                                    From = itRecent.PreviousItemPath.From,
                                    To = itRecent.PreviousItemPath.To
                                }
                            };
                            copyOfRecentItems.Add(instance);
                        }

                        var previousPath = nextStep.PreviousItemPath;
                        var lastRecentStep = copyOfRecentItems.Last();
                        lastRecentStep.PreviousItemPath = previousPath;
                        nextStep.PreviousItemPath = new Path();
                        var lastRecentItem = lastRecentStep.Item;

                        var resBranch = Walk(lastRecentItem, nextStep, ref copyOfRecentItems);
                        if (resBranch)
                        {
                            var route = new Route();
                            copyOfRecentItems.ForEach(itr => route.Items.Add(itr));
                            res.Add(route);
                        }
                    }

                    branch.NextAllowedSteps.Clear();

                    if (_branches.Count == 0)
                        break;
                }
            }

            // reset routes path information, 
            // only allowed for switches
            foreach (var it in res)
            {
                if (it == null) continue;
                foreach (var itItem in it.Items)
                {
                    if (itItem?.Item == null) continue;
                    if (itItem.Item.IsSwitch) continue;
                    itItem.PreviousItemPath = new Path();
                }
            }

            return res;
        }

        private bool Walk(PlanItem previousItem, NextStep currentStep, ref List<NextStep> result)
        {
            try
            {
                var currentItem = currentStep.Item;

                if (previousItem.identifier.Equals(currentItem.identifier))
                    return false;

                result.Add(currentStep);

                if (currentItem.IsBlock)
                {
                    // final stop, a block reached
                    return true;
                }

                var nextAllowedPath = currentItem.GetAllowedPath(out var connectors);
                if (nextAllowedPath.Count == 0)
                {
                    // final stop, no additional destinations available
                    return false;
                }

                var nextSteps = new List<NextStep>();
                var previousId = previousItem.identifier;
                foreach (var it in nextAllowedPath)
                {
                    if (previousId.Equals(it.From.identifier))
                    {
                        var step = new NextStep
                        {
                            Item = it.To,
                            PreviousItemPath = it
                        };

                        nextSteps.Add(step);
                    }
                }

                if (nextSteps.Count > 1)
                {
                    var branchInfo = new BranchInfo
                    {
                        Item = currentStep
                    };
                    result.ForEach(it => branchInfo.RecentItems.Add(it));
                    nextSteps.ForEach(it => branchInfo.NextAllowedSteps.Add(it));
                    _branches.Push(branchInfo);

                    // walk stop, branch has been detected
                    return false;
                }

                if (nextSteps.Count == 0)
                    return false;

                if (currentItem.IsConnector)
                {
                    if (connectors != null && connectors.Length == 2)
                    {
                        var fakeCurrentItem = connectors[1];
                        result.Add(new NextStep
                        {
                            Item = fakeCurrentItem
                        });
                        var r = Walk(fakeCurrentItem, nextSteps.First(), ref result);

                        return r;
                    }
                }
                else
                {
                    var nextStep = nextSteps.First();

                    // in case only one step if there to decide which 
                    // way we can go AND in case the current item is
                    // a switch, remember the way for later use during 
                    // auto-route and auto-switching
                    if (currentItem.IsSwitch)
                    {
                        var nextStepItemPath = nextStep.PreviousItemPath;

                        currentStep.PreviousItemPath = new Path
                        {
                            FromSide = nextStepItemPath.FromSide,
                            ToSide = nextStepItemPath.ToSide,
                            From = nextStep.PreviousItemPath.From,
                            To = nextStep.PreviousItemPath.To
                        };
                    }

                    var r = Walk(currentItem, nextStep, ref result);

                    return r;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }
    }
}
