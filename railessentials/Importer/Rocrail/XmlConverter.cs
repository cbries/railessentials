// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: XmlConverter.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Importer.Rocrail.Xml;
using railessentials.Plan;
using railessentials.Theming;

namespace railessentials.Importer.Rocrail
{
    public partial class XmlConverter
    {
        public string ThemeJsonPath { get; set; }

        private string _targetMetamodelPath = "";
        private string _targetFbEventsPath = "";

        private List<ThemeCategory> _theme;

        private void InitTheme()
        {
            var json = File.ReadAllText(ThemeJsonPath, Encoding.UTF8);
            _theme = JsonConvert.DeserializeObject<List<ThemeCategory>>(json);
        }

        private ThemeItem GetThemeObjectById(int id)
        {
            foreach(var itCat in _theme)
            {
                if (itCat == null) continue;
                foreach(var it in itCat.Objects)
                {
                    if (it == null) continue;
                    if (it.Id == id)
                        return it;
                }
            }

            return null;
        }
        
        private XmlDocument _doc;

        public List<TrackElement> TrackElements { get; } = new();
        public List<SignalElement> SignalElements { get; } = new();
        public List<FeedbackElement> FeedbackElements { get; } = new();
        public List<SwitchElement> SwitchElements { get; } = new();
        public List<BlockElement> BlockElements { get; } = new();
        public List<TextElement> TextElements { get; } = new();
        public List<CoElement> CoElements { get; } = new();

        public JObject Planfield { get; } = new JObject();

        public void Run(
            string xmlPath, 
            string resultJsonPath,
            string resultFbEventsPath)
        {
            _targetMetamodelPath = resultJsonPath;
            _targetFbEventsPath = resultFbEventsPath;

            InitTheme();

            _doc = new XmlDocument();
            _doc.Load(xmlPath);
            ParseTkList();
            ParseSgList();
            ParseFbList();
            ParseSwList();
            ParseBkList();
            ParseTxList();
            ParseCoList();

            Trace.WriteLine($"TrackElements: {TrackElements.Count}");
            Trace.WriteLine($"SignalElements: {SignalElements.Count}");
            Trace.WriteLine($"FeedbackElements: {FeedbackElements.Count}");
            Trace.WriteLine($"SwitchElements: {SwitchElements.Count}");
            Trace.WriteLine($"BlockElements: {BlockElements.Count}");
            Trace.WriteLine($"TextElements: {TextElements.Count}");
            Trace.WriteLine($"CoElements: {CoElements.Count}");

            var r0 = GenerateMetamodel(out var err0);
            if (!r0) Console.WriteLine("Generate metamodel failed: " + err0);

            var r1 = GenerateFbEvents(out var err1);
            if (!r1) Console.WriteLine("Generate events model failed: " + err1);
        }

        private string getNameByType(TrackElement el)
        {
            if (el == null) return string.Empty;
            if (el.ElementType == PlanItemT.Fb)
            {
                if (string.IsNullOrEmpty(el.Type))
                    return "Sensor";
            }
            else if(el.ElementType == PlanItemT.Bk)
            {
                if(string.IsNullOrEmpty(el.Type)
                 || el.Type.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    if (!(el is BlockElement elBk)) return "Block";

                    if (elBk.SmallSymbol)
                        return "Short Block";

                    return "Block";
                }
            }
            else if (el.ElementType == PlanItemT.Tx)
            {
                return "Text";
            }
            else if (el.ElementType == PlanItemT.Co)
            {
                // TBD probably more different types exists
                // TODO el.Value describes "enable" / "disable", i.e. different ui-icons to use
                return "Output A";
            }
            else
            {
                if(!string.IsNullOrEmpty(el.Type))
                {
                    switch (el.Type.ToLower())
                    {
                        // Track
                        case "straight": return "Straigth";
                        case "curve": return "Curve";
                        case "dir": return "Direction";
                        case "buffer": return "Buffer";
                        case "connector": return "Connector";
                        case "concurveright": return "Connector Curve Right";
                        case "concurveleft": return "Connector Curve Left";
                        // Direction (all)

                        // Signals
                        case "light":
                            {
                                if (!(el is SignalElement sgElement)) return "Main Signal A";

                                switch (sgElement.Signal)
                                {
                                    case "distant": return "Distant Signal";
                                    case "shunting": return "Shunting Signal";
                                    case "main": return "Main Signal A";
                                }

                                Trace.WriteLine($"Light Signal: {sgElement.Signal}");
                            }
                            break;
                        case "semaphore":
                            {
                                if (!(el is SignalElement sgElement)) return "Main Semaphore A";

                                switch (sgElement.Signal)
                                {
                                    case "main": return "Main Semaphore A";
                                    case "shunting": return "Shunting Signal";
                                }

                                Trace.WriteLine($"Semaphore Signal: {sgElement.Signal}");
                            }
                            break;

                        // Switches & Accessories
                        case "decoupler": return "Decoupler";
                        case "right": return "Turnout Right";
                        case "left": return "Turnout Left";
                        case "dcrossing":
                            {
                                if (el is SwitchElement switchEl)
                                {
                                    if (string.IsNullOrEmpty(switchEl.Dir))
                                        return "Crossing Left sr";

                                    if (switchEl.Dir.Equals("true", StringComparison.OrdinalIgnoreCase))
                                        return "Crossing Right sr";

                                    return "Crossing Left sr";
                                }
                            }
                            break;

                        case "accessory":
                            {
                                if (el is SwitchElement switchEl)
                                {
                                    switch(switchEl.AccNr)
                                    {
                                        case "12": return "Level Crossing C";
                                    }
                                }
                            }
                            break;
                    }

                }
            }
            return string.Empty;
        }

        private int getThemeIdByType(TrackElement el)
        {
            if (el.ElementType == PlanItemT.Fb)
            {
                if (string.IsNullOrEmpty(el.Type))
                    return 200;
            }
            else if (el.ElementType == PlanItemT.Bk)
            {
                if (string.IsNullOrEmpty(el.Type) || el.Type.IndexOf("none", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    var elBk = el as BlockElement;
                    if (elBk == null) return 150;

                    if (elBk.SmallSymbol)
                        return 151;

                    return 150;
                }
            }
            else if(el.ElementType == PlanItemT.Tx)
            {
                return 1010;
            }
            else if(el.ElementType == PlanItemT.Co)
            {
                // TBD probably more different types exists
                return 71; // Output A
            }
            else
            {
                if (!string.IsNullOrEmpty(el.Type))
                {
                    switch (el.Type.ToLower())
                    {
                        // Track
                        case "straight": return 10;
                        case "curve": return 11;
                        case "dir": return 14;
                        case "buffer": return 13;
                        case "connector": return 17;
                        case "concurveright": return 18;
                        case "concurveleft": return 19;
                        // Direction (all)

                        // Signals
                        case "light":
                            {
                                var sgElement = el as SignalElement;
                                if (sgElement == null) return 101;

                                switch (sgElement.Signal)
                                {
                                    case "distant": return 100;
                                    case "shunting": return 104;
                                    case "main": return 101;
                                }
                            }
                            break;
                        case "semaphore":
                        {
                            var sgElement = el as SignalElement;
                            if (sgElement == null) return 106;

                            switch (sgElement.Signal)
                            {
                                case "main": return 106;
                                case "shunting": return 105;
                            }

                            Trace.WriteLine($"Semaphore Signal: {sgElement.Signal}");
                        }
                            break;

                        // Switches & Accessories
                        case "decoupler": return 70;
                        case "right": return 51;
                        case "left": return 50;
                        case "dcrossing":
                            {
                                if (el is SwitchElement switchEl)
                                {
                                    if (string.IsNullOrEmpty(switchEl.Dir))
                                        return 58;

                                    if (switchEl.Dir.Equals("true", StringComparison.OrdinalIgnoreCase))
                                        return 59;
                                        
                                    return 58;
                                }
                            }
                            break;

                        case "accessory":
                        {
                            if (el is SwitchElement switchEl)
                            {
                                switch (switchEl.AccNr)
                                {
                                    case "12": return 255;
                                }
                            }
                        }
                            break;
                    }
                }

                Trace.WriteLine($"Type: {el.Type?.ToLower()??"-"}  {el.ElementType}   {el.X} {el.Y}");
            }

            return 10;
        }

        private int GetRotationBy(TrackElement el)
        {
            var elType = getThemeIdByType(el);

            var orientation = el.Orientation;
            if (string.IsNullOrEmpty(orientation)) return 0;
            if (el.ElementType == PlanItemT.Bk || (elType == 58 || elType == 59))
            {
                if (orientation.Equals("east", StringComparison.OrdinalIgnoreCase))
                    orientation = "west";
                if (orientation.Equals("north", StringComparison.OrdinalIgnoreCase))
                    orientation = "south";
                if (orientation.Equals("west", StringComparison.OrdinalIgnoreCase)) return 0;
                if (orientation.Equals("south", StringComparison.OrdinalIgnoreCase)) return 1;
            }
            else
            {
                if (orientation.Equals("west", StringComparison.OrdinalIgnoreCase)) return 0;
                if (orientation.Equals("east", StringComparison.OrdinalIgnoreCase)) return 2;
                if (orientation.Equals("south", StringComparison.OrdinalIgnoreCase)) return 1;
                if (orientation.Equals("north", StringComparison.OrdinalIgnoreCase)) return 3;
            }
            return 0;
        }

        private void ParseSwList()
        {
            SwitchElements.Clear();

            var nodeSwList = _doc.SelectNodes("/plan/swlist");
            if (nodeSwList == null)
            {
                Trace.WriteLine("<swlist> is missing");
                return;
            }
            foreach (XmlNode node in nodeSwList)
            {
                var childs = node?.ChildNodes;
                if (childs == null || childs.Count == 0) continue;

                foreach (var itChild in childs)
                {
                    var el = new SwitchElement();
                    var res = el.ParseXmlNode(itChild as XmlNode);
                    if (!res) continue;

                    SwitchElements.Add(el);
                }
            }
        }

        private void ParseFbList()
        {
            FeedbackElements.Clear();

            var nodeFbList = _doc.SelectNodes("/plan/fblist");
            if (nodeFbList == null)
            {
                Trace.WriteLine("<fblist> is missing");
                return;
            }
            foreach (XmlNode node in nodeFbList)
            {
                var childs = node?.ChildNodes;
                if (childs == null || childs.Count == 0) continue;

                foreach (var itChild in childs)
                {
                    var el = new FeedbackElement();
                    var res = el.ParseXmlNode(itChild as XmlNode);
                    if (!res) continue;

                    FeedbackElements.Add(el);
                }
            }
        }

        private void ParseSgList()
        {
            SignalElements.Clear();

            var nodeSgList = _doc.SelectNodes("/plan/sglist");
            if (nodeSgList == null)
            {
                Trace.WriteLine("<sglist> is missing");
                return;
            }
            foreach (XmlNode node in nodeSgList)
            {
                var childs = node?.ChildNodes;
                if (childs == null || childs.Count == 0) continue;

                foreach (var itChild in childs)
                {
                    var el = new SignalElement();
                    var res = el.ParseXmlNode(itChild as XmlNode);
                    if (!res) continue;

                    SignalElements.Add(el);
                }
            }
        }

        private void ParseTkList()
        {
            TrackElements.Clear();

            var nodeTkList = _doc.SelectNodes("/plan/tklist");
            if (nodeTkList == null)
            {
                Trace.WriteLine("<tklist> is missing");
                return;
            }
            foreach (XmlNode node in nodeTkList)
            {
                var childs = node?.ChildNodes;
                if (childs == null || childs.Count == 0) continue;

                foreach(var itChild in childs)
                {
                    var el = new TrackElement();
                    var res = el.ParseXmlNode(itChild as XmlNode);
                    if (!res) continue;

                    TrackElements.Add(el);
                }
            }
        }

        private void ParseBkList()
        {
            BlockElements.Clear();

            var nodeBkList = _doc.SelectNodes("/plan/bklist");
            if (nodeBkList == null)
            {
                Trace.WriteLine("<bklist> is missing");
                return;
            }
            foreach (XmlNode node in nodeBkList)
            {
                var childs = node?.ChildNodes;
                if (childs == null || childs.Count == 0) continue;

                foreach (var itChild in childs)
                {
                    var el = new BlockElement();
                    var res = el.ParseXmlNode(itChild as XmlNode);
                    if (!res) continue;

                    BlockElements.Add(el);
                }
            }
        }

        private void ParseTxList()
        {
            TextElements.Clear();

            var nodeTxList = _doc.SelectNodes("/plan/txlist");
            if (nodeTxList == null)
            {
                Trace.WriteLine("<txlist> is missing");
                return;
            }
            foreach (XmlNode node in nodeTxList)
            {
                var childs = node?.ChildNodes;
                if (childs == null || childs.Count == 0) continue;

                foreach (var itChild in childs)
                {
                    var el = new TextElement();
                    var res = el.ParseXmlNode(itChild as XmlNode);
                    if (!res) continue;

                    TextElements.Add(el);
                }
            }
        }

        private void ParseCoList()
        {
            CoElements.Clear();

            var nodeCoList = _doc.SelectNodes("/plan/colist");
            if (nodeCoList == null)
            {
                Trace.WriteLine("<colist> is missing");
                return;
            }
            foreach (XmlNode node in nodeCoList)
            {
                var childs = node?.ChildNodes;
                if (childs == null || childs.Count == 0) continue;

                foreach (var itChild in childs)
                {
                    var el = new CoElement();
                    var res = el.ParseXmlNode(itChild as XmlNode);
                    if (!res) continue;

                    CoElements.Add(el);
                }
            }
        }
    }
}
