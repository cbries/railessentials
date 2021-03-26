﻿// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RouteAnalyzer.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using railessentials.Analyzer;
using railessentials.Plan;
using Utilities;

namespace railessentials.Route
{
    public delegate void RouteAnalyzerProgress(object sender, RouteAnalyzerState state);
    public delegate void RouteAnalyzerStarted(RouteAnalyzer sender);
    public delegate void RouteAnalyzerFinished(RouteAnalyzer sender);
    public delegate void RouteAnalyzerFailed(RouteAnalyzer sender, string reason);
    public delegate void RouteAnalyzerFailedEx(RouteAnalyzer sender, Exception reason);

    public class RouteAnalyzerState
    {
        public string Message { get; set; }
    }

    public class RouteAnalyzer
    {
        public event RouteAnalyzerProgress Progress;
        public event RouteAnalyzerStarted Started;
        public event RouteAnalyzerFinished Finished;
        public event RouteAnalyzerFailed Failed;
        public event RouteAnalyzerFailedEx FailedEx;

        public ClientHandler.ClientHandler Ctx { get; }

        public RouteAnalyzer(ClientHandler.ClientHandler ctx)
        {
            Ctx = ctx;
        }

        private static PlanField LoadPlanFieldFile(string path)
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var field = LoadPlanField(json);
            return field;
        }

        private static PlanField LoadPlanField(string json)
        {
            var planfield = JsonConvert.DeserializeObject<Dictionary<string, PlanField>>(json);
            var field = planfield["planField"];
            field.InitContext();
            return field;
        }

        public async Task Start(string inputMetamodelPath, string outputRoutePath)
        {
            Started?.Invoke(this);
            Progress?.Invoke(this, new RouteAnalyzerState { Message = "Started" });

            if(string.IsNullOrEmpty(inputMetamodelPath) )
            {
                Ctx?.SendDebugMessage($"Input metamodel file is not set.");
                return;
            }
            if(!File.Exists(inputMetamodelPath))
            {
                Ctx?.SendDebugMessage($"Input metamodel file does not exist: {inputMetamodelPath}");
                return;
            }

            var fname = System.IO.Path.GetFileName(inputMetamodelPath);

            await Task.Run(() =>
            {
                try
                {
                    var field = LoadPlanFieldFile(inputMetamodelPath);
                    if(field != null)
                        Ctx?.SendDebugMessage($"Field is loaded, items: {field.Count}");

                    var analyzer = new Analyze(field);
                    var analyzerResult = analyzer.Execute((step, maxSteps) =>
                    {
                        var m = GetProgressMessage(step, maxSteps, $"Analyzing {fname}");
                        Progress?.Invoke(this, new RouteAnalyzerState { Message = $"{m}" });
                    });
                    Ctx?.SendDebugMessage($"Found {analyzerResult.NumberOfRoutes} routes.");
                    var json = analyzerResult.ToJson();
                    if (string.IsNullOrEmpty(json))
                        Failed?.Invoke(this, $"Result of the analyze call is empty.");
                    Ctx?.SendDebugMessage($"Apply recent disabling states.");
                    json = ApplyRouteDisableStates(outputRoutePath, json);
                    json = ApplyRouteAdditionalBlockLocks(outputRoutePath, json);
                    json.FixBomIfNeeded();
                    StringUtilities.WriteAllTextNoBom(outputRoutePath, json, out _);

                    Finished?.Invoke(this);
                }
                catch (Exception ex)
                {
                    FailedEx?.Invoke(this, ex);
                }
            });
        }

        private static RouteList _getLatestRouteList(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!File.Exists(path)) return null;
            try
            {
                var originalCnt = File.ReadAllText(path, Encoding.UTF8);
                return JsonConvert.DeserializeObject<RouteList>(originalCnt);
            }
            catch
            {
                // ignore
            }
            return null;
        }

        private static string ApplyRouteDisableStates(string outputRoutePath, string json)
        {
            var originalJson = _getLatestRouteList(outputRoutePath);
            if (originalJson == null) return json;

            var disabledRoutes = new List<string>();
            foreach(var it in originalJson)
            {
                if(it.IsDisabled)
                    disabledRoutes.Add(it.Name);
            }

            var newJson = JsonConvert.DeserializeObject<RouteList>(json);
            foreach(var it in newJson)
            {
                var routeName = it?.Name;
                if (string.IsNullOrEmpty(routeName)) continue;
                if (disabledRoutes.Contains(routeName))
                    it.IsDisabled = true;
            }

            return JsonConvert.SerializeObject(newJson, Formatting.Indented);
        }

        private static string ApplyRouteAdditionalBlockLocks(string outputRoutePath, string json)
        {
            var originalJson = _getLatestRouteList(outputRoutePath);
            if (originalJson == null) return json;

            var listOfLockedBlocks = new Dictionary<string, List<string>>();
            foreach(var it in originalJson)
                listOfLockedBlocks.Add(it.Name, it.AdditionalBlockLocks);

            var newJson = JsonConvert.DeserializeObject<RouteList>(json);
            foreach(var it in listOfLockedBlocks)
            {
                if (string.IsNullOrEmpty(it.Key)) continue;
                if (it.Value == null || it.Value.Count == 0) continue;

                var route = newJson.GetByName(it.Key);
                if (route == null) continue;
                route.AdditionalBlockLocks = it.Value;
            }

            return JsonConvert.SerializeObject(newJson, Formatting.Indented);
        }

        private static string GetProgressMessage(int step, int maxStep, string msg)
        {
            return $"{msg} {(int)(step / (float)maxStep * 100.0)}%";
        }
    }
}
