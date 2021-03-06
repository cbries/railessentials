// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: XmlConverter.Generator.cs

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Importer.Rocrail.Xml;
using railessentials.Plan;
using Utilities;

namespace railessentials.Importer.Rocrail
{
    public partial class XmlConverter
    {
        private bool GenerateMetamodel(out string errorMessage)
        {
            var planfield = new JObject();

            var allElements = new List<TrackElement>();
            allElements.AddRange(TrackElements);
            allElements.AddRange(SignalElements);
            allElements.AddRange(FeedbackElements);
            allElements.AddRange(SwitchElements);
            allElements.AddRange(BlockElements);
            allElements.AddRange(TextElements);
            allElements.AddRange(CoElements);

            var trackCounter = 0;
            var signalCounter = 0;
            var feedbackCounter = 0;
            var switchCounter = 0;
            var blockCounter = 0;
            var textCounter = 0;
            var coCounter = 0;

            foreach (var it in allElements)
            {
                var targetKey = $"{it.X}x{it.Y}";

                var o = new JObject();
                var themeId = 0;
                string suffix;

                if (it.ElementType == PlanItemT.Tk)
                {
                    themeId = getThemeIdByType(it);
                    suffix = "TE_";
                    o["identifier"] = $"{suffix}{trackCounter}";
                    ++trackCounter;
                }
                else if (it.ElementType == PlanItemT.Fb)
                {
                    themeId = getThemeIdByType(it);
                    suffix = "FB_";
                    o["identifier"] = $"{suffix}{feedbackCounter}";
                    ++feedbackCounter;
                }
                else if (it.ElementType == PlanItemT.Sg)
                {
                    themeId = getThemeIdByType(it);
                    suffix = "SE_";
                    o["identifier"] = $"{suffix}{signalCounter}";
                    ++signalCounter;
                }
                else if (it.ElementType == PlanItemT.Sw)
                {
                    themeId = getThemeIdByType(it);
                    suffix = "SW_";
                    o["identifier"] = $"{suffix}{switchCounter}";
                    ++switchCounter;
                }
                else if (it.ElementType == PlanItemT.Bk)
                {
                    themeId = getThemeIdByType(it);
                    suffix = "BK_";
                    o["identifier"] = $"{suffix}{blockCounter}";
                    ++blockCounter;
                }
                else if (it.ElementType == PlanItemT.Tx)
                {
                    themeId = getThemeIdByType(it);
                    suffix = "TX_";
                    o["identifier"] = $"{suffix}{textCounter}";
                    ++textCounter;
                }
                else if (it.ElementType == PlanItemT.Co)
                {
                    themeId = getThemeIdByType(it);
                    suffix = "CO_";
                    o["identifier"] = $"{suffix}{coCounter}";
                    ++coCounter;
                }

                if (themeId == 0) continue;

                o["name"] = getNameByType(it);
                if (!string.IsNullOrEmpty(it.Identifier))
                    o["identifier"] = it.Identifier.Trim();

                var themeObject = GetThemeObjectById(themeId);
                o["basename"] = themeObject.BaseName;
                o["clickable"] = themeObject.Clickable;
                o["routes"] = JToken.Parse(JsonConvert.SerializeObject(themeObject.Routes, Formatting.Indented));
                o["states"] = JToken.Parse(JsonConvert.SerializeObject(themeObject.States, Formatting.Indented));
                if (themeObject.Dimensions.Count == 0)
                {
                    //var dimAr = new JArray();
                    //var noOfRoutes = themeObject.Routes.Count;
                    //if (noOfRoutes == 0)
                    //    noOfRoutes = 4;
                    //for(var r = 0; r < noOfRoutes; ++r)
                    //    dimAr.Add(new JObject { ["w"] = 1, ["h"] = 1 });
                    //o["dimensions"] = dimAr;
                }
                else
                {
                    var strDim = JsonConvert.SerializeObject(themeObject.Dimensions, Formatting.Indented);
                    strDim = strDim.Replace("W", "w");
                    strDim = strDim.Replace("H", "h");
                    o["dimensions"] = JToken.Parse(strDim);
                }
                o["coord"] = new JObject
                {
                    ["x"] = it.X,
                    ["y"] = it.Y
                };

                // add port/gate for switches/signals
                if (it.ElementType == PlanItemT.Sw)
                {
                    if (it is SwitchElement itSw)
                    {
                        o["addresses"] = JToken.Parse(JsonConvert.SerializeObject(itSw.Address, Formatting.Indented));
                    }
                }
                if (it.ElementType == PlanItemT.Signal)
                {
                    if (it is SignalElement itSg)
                    {
                        o["addresses"] = JToken.Parse(JsonConvert.SerializeObject(itSg.Address, Formatting.Indented));
                    }
                }
                if(it.ElementType == PlanItemT.Feedback)
                {
                    if (it is FeedbackElement itFb)
                    {
                        o["addresses"] = JToken.Parse(JsonConvert.SerializeObject(itFb.Address, Formatting.Indented));
                    }
                }

                var themeDimIdx = GetRotationBy(it);
                var hackToFixThemeDimIdx = new List<int>
                {
                    10, 200, 255
                };
                if (hackToFixThemeDimIdx.Contains(themeId))
                {
                    if (themeDimIdx == 2) themeDimIdx = 0;
                    else if (themeDimIdx == 3) themeDimIdx = 1;
                }

                var editor = new JObject
                {
                    ["themeId"] = themeId,
                    ["offsetX"] = 0,
                    ["offsetY"] = 0,
                    ["themeDimIdx"] = themeDimIdx
                };

                if (it is TextElement txtEl)
                {
                    //var txtSize = new JObject
                    //{
                    //    ["width"] = $"{txtEl.Width * 32}px", 
                    //    ["height"] = $"{txtEl.Height * 32}px"
                    //};

                    var txt = txtEl.Text;
                    if (txtEl.IsBold) txt = "<b>" + txt + "</b>";
                    if (txtEl.IsItalic) txt = "<i>" + txt + "</i>";
                    if (txtEl.IsUnderline) txt = "<u>" + txt + "</u>";

                    editor["innerHtml"] = txt;
                    editor["outerHtml"] = "<div class=\"elEditor\" style=\"font-size: 14px;\"></div>";
                }
                if (it.ConnectorId > 1)
                {
                    editor["connectorId"] = it.ConnectorId;
                }
                o["editor"] = editor;

                planfield[targetKey] = o;
            }

            var metamodel = new JObject
            {
                ["planField"] = planfield
            };

            var strJson = metamodel.ToString(Formatting.Indented);
            strJson = strJson.Replace("\"Default\"", "\"default\"");
            strJson = strJson.Replace("\"Occ\"", "\"occ\"");
            strJson = strJson.Replace("\"Route\"", "\"route\"");
            strJson.FixBomIfNeeded();
            return StringUtilities.WriteAllTextNoBom(_targetMetamodelPath, strJson, out errorMessage);
        }

        public bool GenerateFbEvents(out string errorMessage)
        {
            var events = new JArray();

            var defaultSettings = new JObject
            {
                ["BlockEnabled"] = true,
                ["OptionWait"] = true,
                ["OptionDirection"] = false,
                ["OptionMainline"] = true,
                ["OptionBbt"] = false,
                ["TypeOthers"] = true,
                ["TypeLocal"] = true,
                ["TypeIntercity"] = false,
                ["TypeFreight"] = true,
                ["TypeShunting"] = true,
                ["TypeRegional"] = true,
                ["TypeBranchLine"] = true,
                ["TypeBranchLineFreight"] = false
            };

            var deniedLocomotives = new JArray();

            foreach (var itBlock in BlockElements)
            {
                if (itBlock == null) continue;

                var fromFbs = new List<FbEvent>();
                var toFbs = new List<FbEvent>();

                foreach (var itFbEvent in itBlock.FbEvents)
                {
                    if (itFbEvent == null) continue;

                    var blockId = itFbEvent.Owner.Id;
                    var from = itFbEvent.From;

                    if (string.IsNullOrEmpty(blockId)) continue;
                    if (string.IsNullOrEmpty(from)) continue;
                    
                    if (from.Equals("all", StringComparison.OrdinalIgnoreCase))
                        fromFbs.Add(itFbEvent);
                    else if(from.Equals("all-reverse", StringComparison.OrdinalIgnoreCase))
                        toFbs.Add(itFbEvent);
                }

                var oplus = new JObject
                {
                    ["BlockId"] = itBlock.Id + "[+]",
                    ["DeniedLocomotives"] = deniedLocomotives,
                    ["Settings"] = defaultSettings
                };

                var ominus = new JObject
                { 
                    ["BlockId"] = itBlock.Id + "[-]",
                    ["DeniedLocomotives"] = deniedLocomotives,
                    ["Settings"] = defaultSettings
                };

                if(fromFbs.Count == 2 && toFbs.Count == 2)
                {
                    if (fromFbs[0].Action.Equals("enter", StringComparison.OrdinalIgnoreCase))
                    {
                        oplus["FbEnter"] = fromFbs[0].FbId;
                        oplus["FbIn"] = fromFbs[1].FbId;
                    }
                    else
                    {
                        oplus["FbIn"] = fromFbs[0].FbId;
                        oplus["FbEnter"] = fromFbs[1].FbId;
                    }

                    if (toFbs[0].Action.Equals("in", StringComparison.OrdinalIgnoreCase))
                    {
                        ominus["FbIn"] = toFbs[0].FbId;
                        ominus["FbEnter"] = toFbs[1].FbId;
                    }
                    else
                    {
                        ominus["FbEnter"] = toFbs[0].FbId;
                        ominus["FbIn"] = toFbs[1].FbId;
                    }
                }

                events.Add(oplus);
                events.Add(ominus);
            }

            var strJson = events.ToString(Formatting.Indented);
            strJson.FixBomIfNeeded();
            return StringUtilities.WriteAllTextNoBom(_targetFbEventsPath, strJson, out errorMessage);
        }
    }
}
