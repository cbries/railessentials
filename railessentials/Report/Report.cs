// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Report.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.UI.WebControls;
using ecoslib.Sniffer;
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

        public Report(Sniffer sniffer, Metadata metadata)
        {
            _sniffer = sniffer;
            _metadata = metadata;
        }

        private class __planItemExtended : Plan.PlanItem
        {
            public string Coord { get; set; }
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
                    sb.Append($"<td>{locObj.ObjectId}</td>");
                    sb.Append($"<td>{locObj.Name}</td>");
                    sb.Append($"<td>{locObj.Protocol}</td>");
                    sb.Append($"<td>{locObj.Addr}</td>");
                    sb.Append($"<td>{locObj.GetNumberOfSpeedsteps()}</td>");
                    sb.Append($"<td>{locObj.NrOfFunctions}</td>");
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

                    var addr = planItem.Addresses;
                    if(addr.Addr <= 0)
                    {
                        if (addr.Addr1 > 0 && addr.Port1 > 0)
                            planItem.Addresses.Addr = AddressUtilities.GetEcosAddress(addr.Addr1, addr.Port1);
                        else
                            planItem.Addresses.Addr = AddressUtilities.GetEcosAddress(addr.Addr2, addr.Port2);
                    }

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
                        sb.Append($"<td>{planItem.Coord}</td>");
                        sb.Append($"<td>{planItem.name}</td>");
                        sb.Append($"<td>{planItem.Addresses.Addr}</td>");
                        sb.Append("</tr>");

                        sensorHtml += sb.ToString();

                        continue;
                    }

                    if(true) // scope
                    {
                        var sb = new StringBuilder();
                        sb.Append("<tr>");
                        sb.Append($"<td>{planItem.Coord}</td>");
                        if (planItem.IsSignal) sb.Append("<td>Signal</td>");
                        else if (planItem.IsSwitch) sb.Append("<td>Switch</td>");
                        else if (planItem.IsButton) sb.Append("<td>Button</td>");
                        else if (planItem.IsDecoupler) sb.Append("<td>Decoupler</td>");
                        else continue;
                        sb.Append($"<td>{planItem.name}</td>");
                        sb.Append($"<td>{planItem.Addresses.Addr}</td>");
                        sb.Append($"<td>{planItem.Addresses.Port1}</td>");
                        sb.Append($"<td>{planItem.Addresses.Addr1}</td>");
                        sb.Append($"<td>{planItem.Addresses.Inverse1}</td>");
                        sb.Append($"<td>{planItem.Addresses.Port2}</td>");
                        sb.Append($"<td>{planItem.Addresses.Addr2}</td>");
                        sb.Append($"<td>{planItem.Addresses.Inverse2}</td>");
                        sb.Append($"<td>{planItem.IsMaintenance}</td>");
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
