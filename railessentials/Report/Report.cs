// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Report.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ecoslib.Sniffer;
using ecoslib.Statistics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utilities;

namespace railessentials.Report
{
    public class Report
    {
        private static string TplInput = "Report\\Report.tpl.html";

        private readonly Sniffer _sniffer;
        private readonly Metadata _metadata;
        private readonly Statistics _statistics;

        public Report(
            Sniffer sniffer, 
            Metadata metadata,
            Statistics statistics)
        {
            _sniffer = sniffer;
            _metadata = metadata;
            _statistics = statistics;
        }

        private class __planItemExtended : Plan.PlanItem
        {
            public string Coord { get; set; }
            public ecoslib.Entities.Item EcosItem { get; set; }
        }

        public ecoslib.Entities.Item GetEcosItem(
            IReadOnlyList<ecoslib.IItem> availableItems,
            int ecosAddr
            )
        {
            foreach (var it in availableItems)
            {
                if (it == null) continue;
                
                if (it is ecoslib.Entities.Locomotive loc)
                    if (loc.Addr == ecosAddr)
                        return loc;

                if (it is ecoslib.Entities.Accessory acc)
                    if (acc.Addr == ecosAddr)
                        return acc;
            }

            return null;
        }

        private int GetSwitchCount(__planItemExtended item)
        {
            if (item?.EcosItem == null) return 0;
            var data = _statistics?.GetData(item.EcosItem.ObjectId);
            if (data == null) return 0;
            return data.Touches.Count;
        }

        private long GetLocomotiveDuration(int oid)
        {
            if (oid <= 0) return 0;
            var data = _statistics?.GetData(oid);
            if (data == null) return 0;
            long durationSeconds = 0;
            foreach(var it in data.Durations)
            {
                var start = it.Start;
                var end = it.Stop;
                var delta = end - start;
                if (delta.Seconds <= 0) continue;
                durationSeconds += delta.Seconds;
            }
            return durationSeconds;
        }

        private string SecondsHumanReadable(long seconds)
        {
            // see https://stackoverflow.com/a/463668
            var time = TimeSpan.FromSeconds(seconds);
            //here backslash is must to tell that colon is
            //not the part of format, it just a character that we want in output
            return time.ToString(@"hh\:mm\:ss\:fff");
        }

        public bool Generate(
            string outputFile,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (_metadata == null)
            {
                errorMessage = "Metadata is not provided.";
                return false;
            }
            if (string.IsNullOrEmpty(outputFile))
            {
                errorMessage = "Output file is not set.";
                return false;
            }

            string tplHtml;
            try
            {
                tplHtml = File.ReadAllText(TplInput, Encoding.UTF8);
                if (string.IsNullOrEmpty(tplHtml))
                {
                    errorMessage = "Template file is empty.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }

            try
            {
                var dp = _sniffer.GetDataProvider();
                if (dp == null)
                {
                    errorMessage = "DataProvider is missing.";
                    return false;
                }

                const string css = "style=\"text-align: center;\"";

                //
                // generate locomotive table
                //
                var locomotivesHtml = string.Empty;
                var locObjects = new List<ecoslib.Entities.Locomotive>();
                foreach (var it in dp.Objects)
                {
                    var locObj = it as ecoslib.Entities.Locomotive;
                    if (locObj == null) continue;
                    locObjects.Add(locObj);
                }

                locObjects = locObjects.OrderBy(x => x.ObjectId).ToList();

                foreach (var locObj in locObjects)
                {
                    var sb = new StringBuilder();
                    sb.Append("<tr>");
                    sb.Append($"<td {css}>{locObj.ObjectId}</td>");
                    sb.Append($"<td>{locObj.Name}</td>");
                    sb.Append($"<td {css}>{locObj.Protocol}</td>");
                    sb.Append($"<td {css}>{locObj.Addr}</td>");
                    sb.Append($"<td {css}>{locObj.GetNumberOfSpeedsteps()}</td>");
                    sb.Append($"<td {css}>{locObj.NrOfFunctions}</td>");

                    var duration = GetLocomotiveDuration(locObj.ObjectId);
                    var durationHuman = SecondsHumanReadable(duration);
                    if(duration <= 0)
                        sb.Append($"<td {css}>-.-</td>");
                    else
                        sb.Append($"<td {css}>{durationHuman}</td>");

                    sb.Append("</tr>");
                    locomotivesHtml += sb.ToString();
                }
                tplHtml = tplHtml.Replace("{{LOCOMOTIVE_ROWS}}", locomotivesHtml);

                //
                // generate accessory table
                //
                var planField = _metadata.Metamodel["planField"] as JObject;
                if (planField == null)
                {
                    errorMessage = "Plan information not available.";
                    return false;
                }

                var sensorHtml = string.Empty;
                var accessoryHtml = string.Empty;
                
                var planItems = new List<__planItemExtended>();
                
                //
                // filter list to have only accessory
                // and calculate ecos address for any accessory
                //
                foreach (var it in planField)
                {
                    var coord = it.Key;
                    var data = it.Value;
                    if (data == null) continue;
                    var planItem = JsonConvert.DeserializeObject<__planItemExtended>(data.ToString(Formatting.Indented));
                    if (planItem.IsBlock) continue;
                    if (planItem.IsConnector) continue;
                    if (planItem.IsDirection) continue;
                    if (planItem.IsTrack) continue;
                    if (planItem.IsLabel) continue;

                    var addr = planItem.Addresses;
                    if(addr.Addr <= 0)
                    {
                        if (addr.Addr1 > 0 && addr.Port1 > 0)
                            planItem.Addresses.Addr = AddressUtilities.GetEcosAddress(addr.Addr1, addr.Port1);
                        else
                            planItem.Addresses.Addr = AddressUtilities.GetEcosAddress(addr.Addr2, addr.Port2);
                    }

                    planItem.EcosItem = GetEcosItem(dp.Objects, planItem.Addresses.Addr);

                    planItem.Coord = coord;
                    planItems.Add(planItem);
                }

                planItems = planItems.OrderBy(x => x.Addresses.Addr).ToList();

                foreach (var planItem in planItems)
                {
                    if (planItem == null) continue;

                    if(planItem.IsSensor)
                    {
                        var sb = new StringBuilder();
                        sb.Append("<tr>");
                        sb.Append($"<td {css}>{planItem.Coord}</td>");
                        sb.Append($"<td>{planItem.name}</td>");
                        sb.Append($"<td {css}>{planItem.Addresses.Addr}</td>");
                        sb.Append("</tr>");

                        sensorHtml += sb.ToString();

                        continue;
                    }

                    if(true) // scope
                    {
                        var sb = new StringBuilder();
                        sb.Append("<tr>");
                        sb.Append($"<td>{planItem.Coord}</td>");

                        if (planItem.EcosItem != null)
                        {
                            if (planItem.EcosItem is ecoslib.Entities.Accessory acc)
                                sb.Append($"<td {css}>{acc.ObjectId}</td>");
                            else
                                sb.Append($"<td {css}>-.-</td>");
                        }
                        else
                        {
                            sb.Append($"<td {css}>-.-</td>");
                        }

                        if (planItem.IsSignal) sb.Append("<td {css}>Signal</td>");
                        else if (planItem.IsSwitch) sb.Append("<td {css}>Switch</td>");
                        else if (planItem.IsButton) sb.Append("<td {css}>Button</td>");
                        else if (planItem.IsDecoupler) sb.Append("<td {css}>Decoupler</td>");
                        else continue;
                        sb.Append($"<td>{planItem.name}</td>");
                        sb.Append($"<td {css}>{planItem.Addresses.Addr}</td>");

                        if (planItem.EcosItem != null)
                        {
                            if (planItem.EcosItem is ecoslib.Entities.Accessory acc)
                                sb.Append($"<td {css}>{string.Join(", ", acc.Addrext)}</td>");
                            else
                                sb.Append($"<td {css}>-.-</td>");
                        }
                        else
                        {
                            sb.Append($"<td {css}>-.-</td>");
                        }

                        sb.Append($"<td {css}>{planItem.Addresses.Port1}</td>");
                        sb.Append($"<td {css}>{planItem.Addresses.Addr1}</td>");
                        sb.Append($"<td {css}>{planItem.Addresses.Inverse1}</td>");
                        sb.Append($"<td {css}>{planItem.Addresses.Port2}</td>");
                        sb.Append($"<td {css}>{planItem.Addresses.Addr2}</td>");
                        sb.Append($"<td {css}>{planItem.Addresses.Inverse2}</td>");

                        if (planItem.EcosItem != null)
                        {
                            if (planItem.EcosItem is ecoslib.Entities.Accessory acc)
                                sb.Append($"<td {css}>{string.Join(", ", acc.Protocol)}</td>");
                            else
                                sb.Append($"<td {css}>-.-</td>");
                        }
                        else
                        {
                            sb.Append($"<td {css}>-.-</td>");
                        }

                        sb.Append($"<td {css}>{GetSwitchCount(planItem)}</td>");
                        sb.Append($"<td {css}>{planItem.IsMaintenance}</td>");
                        sb.Append("</tr>");

                        accessoryHtml += sb.ToString();
                    }
                }

                tplHtml = tplHtml.Replace("{{SENSOR_ROWS}}", sensorHtml);
                tplHtml = tplHtml.Replace("{{ACCESSORY_ROWS}}", accessoryHtml);

                // save generated stuff
                File.WriteAllText(outputFile, tplHtml, Encoding.UTF8);

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            return false;
        }
    }
}
