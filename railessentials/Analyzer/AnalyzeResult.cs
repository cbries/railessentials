// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AnalyzeResult.cs

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace railessentials.Analyzer
{
    public enum SideMarker
    {
        Plus, Minus, None
    }

    public class AnalyzeResult
    {
        public int NumberOfRoutes => Routes.Count;
        public List<Plan.Route> Routes { get; set; } = new();

        private static void QueryInAndOut(Plan.Route route, out SideMarker outSide, out SideMarker inSide)
        {
            outSide = SideMarker.None;
            inSide = SideMarker.None;

            var startBlock = route.Items[0];
            var startBlockNext = route.Items[1];
            var xStart = startBlockNext.Item.coord.x - startBlock.Item.coord.x;
            var yStart = startBlockNext.Item.coord.y - startBlock.Item.coord.y;
            if (yStart == 0)
            {
                if (xStart == -1) 
                    outSide = SideMarker.Plus;
                else if (xStart > 1) 
                    outSide = SideMarker.Minus;
            }
            else if (xStart == 0)
            {
                if (yStart == -1) 
                    outSide = SideMarker.Plus;
                else if (yStart > 1) 
                    outSide = SideMarker.Minus;
            }

            var routeLength = route.NumberOfItems;
            var endBlock = route.Items[routeLength - 1];
            var endBlockPrevious = route.Items[routeLength - 2];
            var xEnd = endBlockPrevious.Item.coord.x - endBlock.Item.coord.x;
            var yEnd = endBlockPrevious.Item.coord.y - endBlock.Item.coord.y;
            if (yEnd == 0)
            {
                if (xEnd == -1) 
                    inSide = SideMarker.Plus;
                else if (xEnd > 1) 
                    inSide = SideMarker.Minus;
            }
            else if (xEnd == 0)
            {
                if (yEnd == -1) 
                    inSide = SideMarker.Plus;
                else if (yEnd > 1) 
                    inSide = SideMarker.Minus;
            }
        }

        public string ToJson()
        {
            var arRoutes = new JArray();

            foreach(var itRoute in Routes)
            {
                var itemTracks = new JArray();
                var itemSwitches = new JArray();
                var itemSensors = new JArray();
                var itemSignals = new JArray();
                var itemBlocks = new JArray();

                QueryInAndOut(itRoute, out var outSide, out var inSide);

                var startBlock = itRoute.Items.First();
                var endBlock = itRoute.Items.Last();

                foreach (var itItem in itRoute.Items)
                {
                    var item = itItem.Item;

                    var o = new JObject
                    {
                        ["x"] = item.coord.x,
                        ["y"] = item.coord.y
                    };

                    if(item.IsSwitch)
                    {
                        var oo = new JObject
                        {
                            ["from"] = new JObject
                            {
                                ["x"] = itItem.PreviousItemPath.From.coord.x,
                                ["y"] = itItem.PreviousItemPath.From.coord.y
                            },
                            ["to"] = new JObject
                            {
                                ["x"] = itItem.PreviousItemPath.To.coord.x,
                                ["y"] = itItem.PreviousItemPath.To.coord.y
                            },
                            ["state"] = itItem.GetThemeSwitchPrefix()
                        };

                        o["switch"] = oo;

                        itemSwitches.Add(o);

                    } else if(item.IsSensor)
                    {
                        itemSensors.Add(o);
                    }
                    else if(item.IsSignal)
                    {
                        itemSignals.Add(o);
                    }
                    else if(item.IsBlock)
                    {
                        if (item.identifier.Equals(startBlock.Item.identifier))
                        {
                            o["start"] = true;
                            o["side"] = outSide.ToString();
                        }
                        else if(item.identifier.Equals(endBlock.Item.identifier))
                        {
                            o["start"] = false;
                            o["side"] = inSide.ToString();
                        }

                        o["identifier"] = item.identifier;

                        itemBlocks.Add(o);
                    }
                    else
                    {
                        itemTracks.Add(o);
                    }
                }

                var or = new JObject
                {
                    ["name"] = $"{itRoute.DisplayName}", 
                    ["tracks"] = itemTracks,
                    ["switches"] = itemSwitches,
                    ["sensors"] = itemSensors,
                    ["signals"] = itemSignals,
                    ["blocks"] = itemBlocks
                };

                arRoutes.Add(or);
            }

            return arRoutes.ToString(Formatting.Indented);
        }
    }
}
