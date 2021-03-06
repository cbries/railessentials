﻿// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ClientHandler.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ecoslib;
using ecoslib.Connector;
using ecoslib.Entities;
using ecoslib.Sniffer;
using ecoslib.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Feedbacks;
using railessentials.Plan;
using railessentials.Route;
using SuperWebSocket;
using Utilities;
using Data = railessentials.Locomotives.Data;
using Path = System.IO.Path;

// ReSharper disable InconsistentNaming
// ReSharper disable RedundantUsingDirective

namespace railessentials.ClientHandler
{
    public class ClientHandlerCfg
    {
        public string ThemeName { get; set; }
    }

    public partial class ClientHandler
    {
        private ClientHandlerCfg _cfg;
        internal Sniffer _sniffer;
        private JArray _themeData;
        internal Metadata _metadata;
        private readonly object _metadataLock = new();

        public ILogger Logger { get; set; }

        public bool IsSimulationMode()
        {
            if (_sniffer == null) return false;
            return _sniffer.IsSimulationMode;
        }

        public void SaveAll()
        {
            lock (_metadataLock) _metadata?.SaveAll();
        }

        public void Initialize(
            Sniffer sniffer,
            Metadata metadata,
            JArray themeData,
            ClientHandlerCfg cfg)
        {
            _cfg = cfg;
            _sniffer = sniffer;
            _metadata = metadata;
            _themeData = themeData;

            Logger = _sniffer?.Logger;

            if (_sniffer?.GetWsServer() is WsServer wsServer)
            {
                wsServer.MessageReceived += WsServerOnMessageReceived;
                wsServer.ClientConnected += WsServerOnClientConnected;
                wsServer.ClientDisconnected += WsServerOnClientDisconnected;
            }

            if (_sniffer is { IsSimulationMode: true })
            {
                InitializeSystemHandler();
            }
        }

        private bool PersistEcosData(JObject jsonObj, out string errorMessage)
        {
            try
            {
                var p = Program.Cfg.SimulationData;
                var pd = Path.GetDirectoryName(p);
                if (string.IsNullOrEmpty(pd))
                {
                    errorMessage = "Invalid directory path for Ecos simulation data.";
                    return false;
                }
                if (!Directory.Exists(pd))
                    Directory.CreateDirectory(pd);

                var prettyJsonStr = jsonObj.ToString(Formatting.Indented);
                File.WriteAllText(Program.Cfg.SimulationData, prettyJsonStr, Encoding.UTF8);

                errorMessage = string.Empty;

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public async void HandleFeedbackData()
        {
            if (_autoMode == null) return;
            if (!_autoMode.IsStarted()) return;
            await _autoMode.HandleFeedbacks();
        }

        public void HandleEcosData()
        {
            if (IsSimulationMode())
            {
                lock (_metadataLock)
                {
                    PersistEcosData(_metadata.EcosData, out _);
                }
            }

            SendModelToClients(ModelType.UpdateEcos);
        }

        private void WsServerOnClientConnected(object sender, WebSocketSession session)
        {
            SendModelToClient(session, ModelType.Initialization);
            SendModelToClient(session, ModelType.UpdateFeedbacks);
            SendModelToClient(session, ModelType.UpdateOcc);
            SendModelToClient(session, ModelType.UpdateLocomotivesData);

            if (_autoMode != null && _autoMode.IsStarted())
                _autoMode.SendRouteToClients();

            SendFakeInitStateForNewClientsToRedrawVisualization();
        }

        private void SendFakeInitStateForNewClientsToRedrawVisualization()
        {
            var dp = _sniffer?.GetDataProvider();
            if (dp == null) return;

            foreach (var it in dp.Objects)
            {
                var acc = it as Accessory;
                acc?.__simulateChange();
            }

            _sniffer?.TriggerDataProviderModifiedForSimulation();
        }

        private void WsServerOnMessageReceived(object sender, MessageEventArgs eventargs)
        {
            _sniffer?.Logger?.Log?.Debug($"Received: {eventargs.Message}");
            var cmdMsg = eventargs.Message;
            var json = JObject.Parse(cmdMsg);

            var wsServer = _sniffer?.GetWsServer();

            switch (json["command"]?.ToString())
            {
                case "initializeSystem":
                    {
                        InitializeSystemHandler();
                    }
                    break;

                case "analyzeRoutes":
                    {
                        InitializeRouteAnalyzerHandler();
                    }
                    break;

                case "shutdownSystem":
                    {
                        ShutdownHandler();
                    }
                    break;

                case "update":
                    {
                        var coord = json["itemData"]?["coord"] as JObject;
                        var itemData = json["itemData"] as JObject;
                        var res = UpdateItem(coord, itemData);
                        if (!res)
                            _sniffer?.Logger?.Log.Error($"Remove of {coord} failed.");

                        if (sender is WsClient wsClient)
                            SendModelToClient(wsClient.NativeSession, ModelType.Update);

                        if (_sniffer != null && _sniffer.SaveOnEveryPlanfieldChange)
                            SaveAll();
                    }
                    break;

                case "remove":
                    {
                        var itemIdentifier = json["itemId"]?.ToString();
                        var res = RemoveItem(itemIdentifier);

                        wsServer?.SendMessage(new JObject
                        {
                            ["command"] = "result",
                            ["result"] = new JObject
                            {
                                ["command"] = "planitemRemove",
                                ["identifier"] = itemIdentifier,
                                ["result"] = res
                            }
                        });

                        if (_sniffer.SaveOnEveryPlanfieldChange)
                            SaveAll();
                    }
                    break;

                case "accessory":
                    {
                        var cmddata = GetCmdData(json);
                        HandleAccessoryCommand(cmddata);
                    }
                    break;

                case "locomotive":
                    {
                        var cmddata = GetCmdData(json);
                        HandleLocomotiveCommand(cmddata);
                    }
                    break;

                case "routing":
                    {
                        var cmddata = GetCmdData(json);
                        HandleRouteCommand(cmddata);
                    }
                    break;

                case "setting":
                    {
                        var cmddata = GetCmdData(json);
                        HandleSettingCommand(cmddata);
                    }
                    break;

                case "relayCommand":
                    {
                        var cmddata = GetCmdData(json);
                        HandleRelayCommand(cmddata);
                    }
                    break;

                case "autoMode":
                    {
                        var cmddata = GetCmdData(json);
                        HandleAutoModeCommand(cmddata);
                    }
                    break;
            }
        }

        private void WsServerOnClientDisconnected(object sender, WebSocketSession session, int reason)
        {
            SaveAll();
        }

        private static JObject GetCmdData(JObject json)
        {
            return json?["cmddata"] as JObject;
        }

        internal PlanField GetPlanField()
        {
            lock (_metadataLock)
            {
                var metamodel = _metadata?.Metamodel;
                if (metamodel == null) return null;
                var planfield = JsonConvert.DeserializeObject<Dictionary<string, PlanField>>(metamodel.ToString(Formatting.None));
                return planfield["planField"];
            }
        }

        private Locomotive GetLocomotiveByOid(int oid)
        {
            var dp = _sniffer.GetDataProvider();
            if (dp == null) return null;

            var locomotiveItem = dp.GetObjectBy(oid) as Locomotive;
            if (locomotiveItem == null)
            {
                _sniffer?.Logger?.Log?.Error($"Locomotive({oid}) is missing.");
                return null;
            }

            return locomotiveItem;
        }

        private bool CancelIfAutoModeIsRunning()
        {
            if (_autoMode == null) return false;
            if (!_autoMode.IsStarted()) return false;
            SendDebugMessage("AutoMode is started, command can not be executed.");
            return true;
        }

        private void HandleAutoModeCommand(JObject cmddata)
        {
            var targetState = cmddata.GetBool("state");
            if (targetState) // start AutoMode
            {
                StartAutoMode();
            }
            else // stop AutoMode
            {
                StopAutoMode();
            }
        }

        private void HandleLocomotiveCommand(JObject cmddata)
        {
            var mode = cmddata["mode"]?.ToString();
            if (string.IsNullOrEmpty(mode)) return;

            var oid = cmddata.GetInt("oid", -1);
            if (oid == -1) return;

            var locomotiveItem = GetLocomotiveByOid(oid);
            if (locomotiveItem == null) return;

            switch (mode)
            {
                case "function":
                    {
                        var fncIdx = cmddata.GetInt("fncIdx", -1);
                        if (fncIdx == -1) return;
                        var currentFncState = locomotiveItem.Funcset[fncIdx];
                        locomotiveItem.ToggleFunctions(new Dictionary<uint, bool>
                        {
                            {(uint)fncIdx, !currentFncState}
                        });
                    }
                    break;

                case "speedstep":
                    {
                        var v = cmddata.GetString("speedstep");
                        if (!string.IsNullOrEmpty(v) && v.IndexOf("level", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            locomotiveItem.ChangeSpeedLevel(v);
                        }
                        else if (!string.IsNullOrEmpty(v) && v.Equals("++", StringComparison.Ordinal))
                        {
                            locomotiveItem.IncreaseSpeed();
                        }
                        else if (!string.IsNullOrEmpty(v) && v.Equals("--", StringComparison.Ordinal))
                        {
                            locomotiveItem.DecreaseSpeed();
                        }
                        else
                        {
                            var speed = cmddata.GetInt("speedstep");
                            //locomotiveItem.ChangeSpeedstep(speed);
                            LocomotiveChangeSpeedstep(locomotiveItem, speed);
                        }
                    }
                    break;

                case "direction":
                    {
                        var force = cmddata.GetString("force", "toggle");
                        if (force.Equals("toggle", StringComparison.OrdinalIgnoreCase))
                        {
                            locomotiveItem.ChangeSpeedstep(0);
                            var backward = locomotiveItem.Direction == 1;
                            locomotiveItem.ChangeDirection(!backward);
                        }
                        else if (force.Equals("backward", StringComparison.OrdinalIgnoreCase))
                        {
                            locomotiveItem.ChangeSpeedstep(0);
                            locomotiveItem.ChangeDirection(true);
                        }
                        else if (force.Equals("forward", StringComparison.OrdinalIgnoreCase))
                        {
                            locomotiveItem.ChangeSpeedstep(0);
                            locomotiveItem.ChangeDirection(false);
                        }
                    }
                    break;
            }

            _sniffer?.SendCommandsToEcosStation();
        }

        internal void LocomotiveChangeSpeedstep(Locomotive item, int speed)
        {
            if (IsSimulationMode())
            {
                item.ChangeSpeedstepSimulation(speed);
                SaveAll();
                _sniffer?.TriggerDataProviderModifiedForSimulation();
            }
            else
            {
                item.ChangeSpeedstep(speed);
                _sniffer?.SendCommandsToEcosStation();
            }
        }

        private void HandleRouteCommand_GoToBlock(JObject cmddata)
        {
            var oid = cmddata.GetInt("oid");
            if (oid == 0) return;
            var fromBlockCoord = PlanItemCoord.GetInstance(cmddata["fromBlock"]);
            if (fromBlockCoord == null) return;
            var toBlockCoord = PlanItemCoord.GetInstance(cmddata["toBlock"]);
            if (toBlockCoord == null) return;

            var locItem = GetLocomotiveByOid(oid);
            if (locItem == null)
            {
                _sniffer?.Logger?.Log?.Error($"Locomotive {oid} does not exist.");
                SendDebugMessage($"Locomotive {oid} does not exist.", DebugMessageLevel.Warning);
                return;
            }

            lock (_metadataLock)
            {
                var fromBlockItem = _metadata.GetMetamodelItem(fromBlockCoord);
                if (fromBlockItem == null)
                {
                    SendDebugMessage($"Block (from) at {fromBlockCoord} does not exist.", DebugMessageLevel.Warning);
                    return;
                }

                var toBlockItem = _metadata.GetMetamodelItem(toBlockCoord);
                if (toBlockItem == null)
                {
                    SendDebugMessage($"Block (to) at {toBlockCoord} does not exist.", DebugMessageLevel.Warning);
                    return;
                }

                var res = _metadata.Occ.GoToBlock(locItem, fromBlockItem, toBlockItem);
                if (!res)
                {
                    // tbd
                }
            }

            SendModelToClients(ModelType.UpdateOcc);
        }

        private void HandleRouteCommand_AssignToBlock(JObject cmddata)
        {
            var oid = cmddata.GetInt("oid");
            if (oid == 0) return;

            var blockCoord = PlanItemCoord.GetInstance(cmddata["coord"]);
            if (blockCoord == null) return;

            var locItem = GetLocomotiveByOid(oid);
            if (locItem == null)
            {
                _sniffer?.Logger?.Log?.Error($"Locomotive {oid} does not exist.");
                SendDebugMessage($"Locomotive {oid} does not exist.", DebugMessageLevel.Warning);
                return;
            }

            lock (_metadataLock)
            {
                _metadata.LocomotivesData.SetLocked(oid, true);

                var metamodelItem = _metadata.GetMetamodelItem(blockCoord);
                if (metamodelItem == null)
                {
                    SendDebugMessage($"Block at {blockCoord} does not exist.", DebugMessageLevel.Warning);
                    return;
                }

                var occModel = _metadata.Occ;
                var res = occModel.AssignLocomotive(locItem, metamodelItem);
                if (!res)
                {
                    // tbd
                }
            }

            ResetBlockEnterSideOf(oid, Globals.ConstDefaultEnterSide);

            SaveAll();

            SendModelToClients(ModelType.UpdateLocomotivesData);
            SendModelToClients(ModelType.UpdateOcc);
        }

        private void HandleRouteCommand_ResetAssignment(JObject cmddata)
        {
            var oid = cmddata.GetInt("oid");
            if (oid == 0)
            {
                SendDebugMessage($"Locomotive {oid} does not exist.", DebugMessageLevel.Warning);
                return;
            }

            if (IsAutoModeStarted())
                _autoMode?.ResetRouteFor(oid);

            var submode = cmddata.GetString("submode", "all");
            lock (_metadataLock)
            {
                var occModel = _metadata.Occ;
                var res = occModel.ResetAssignment(oid, submode);
                if (!res)
                {
                    // tbd
                }

                _metadata.Save(Metadata.SaveModelType.OccData);
            }

            ResetBlockEnterSideOf(oid);

            SendModelToClients(ModelType.UpdateLocomotivesData);
            SendModelToClients(ModelType.UpdateOcc);
        }

        private void HandleRouteCommand_Analyze()
        {
            if (CancelIfAutoModeIsRunning()) return;
            // ...
        }

        private class LocalSwitchDataHelper
        {
            public int EcosAddr { get; set; }
            public bool Inverse { get; set; }
            public string TargetState { get; set; }
        }

        internal void ApplyRouteCommandForSwitches(List<RouteSwitch> switches)
        {
            if (switches == null) return;
            if (switches.Count == 0) return;

            var field = GetPlanField();
            if (field == null) return;

            var dp = _sniffer.GetDataProvider();
            if (dp == null) return;

            foreach (var itRouteSwitch in switches)
            {
                if (itRouteSwitch == null) continue;
                var coordX = itRouteSwitch.x;
                var coordY = itRouteSwitch.y;
                Utilities.GetEcosAddress(field, coordX, coordY,
                    out var ecosAddr1, out var ecosAddr2,
                    out var ecosAddr1Inverse, out var ecosAddr2Inverse);

                var addrs = new List<LocalSwitchDataHelper>();

                if (ecosAddr1 > 0)
                    addrs.Add(new LocalSwitchDataHelper { EcosAddr = ecosAddr1.Value, Inverse = ecosAddr1Inverse });
                if (ecosAddr2 > 0)
                    addrs.Add(new LocalSwitchDataHelper { EcosAddr = ecosAddr2.Value, Inverse = ecosAddr2Inverse });

                var targetState = itRouteSwitch.Switch.State;
                var identifier = $"Switch({itRouteSwitch.x},{itRouteSwitch.y})";

                if (addrs.Count == 2)
                {
                    var pp = targetState.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    addrs[1].TargetState = pp[0].Trim();
                    addrs[0].TargetState = pp[1].Trim();
                }
                else
                {
                    addrs[0].TargetState = targetState;
                }

                foreach (var itAddr in addrs)
                {
                    var ecosAddrInverse = itAddr.Inverse;
                    var ecosAddr = itAddr.EcosAddr;
                    if (ecosAddr == -1) continue;

                    var ecosSwitch = dp.GetAccessoryByAddress(ecosAddr) as Accessory;
                    if (ecosSwitch == null) continue;

                    var ts = itAddr.TargetState;

                    var stateIndex = -1;
                    if (!string.IsNullOrEmpty(ts))
                    {
                        if (ts.Equals("straight", StringComparison.OrdinalIgnoreCase))
                            stateIndex = 0;
                        else if (ts.Equals("turn", StringComparison.OrdinalIgnoreCase))
                            stateIndex = 1;
                        else if (ts.Equals("turnright", StringComparison.OrdinalIgnoreCase))
                            stateIndex = 1;
                        else if (ts.Equals("turnleft", StringComparison.OrdinalIgnoreCase))
                            stateIndex = 1;
                        else
                        {
                            SendDebugMessage($"Unknown target state '{ts}' for {identifier}", DebugMessageLevel.Warning);
                        }
                    }
                    if (stateIndex == -1) continue;

                    if (ecosAddrInverse)
                    {
                        stateIndex = stateIndex switch
                        {
                            0 => 1,
                            1 => 0,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                    }

                    SendDebugMessage($"{identifier} switches to '{ts}'.");

                    if (IsSimulationMode())
                        ecosSwitch.SwitchSimulation(stateIndex);
                    else
                        ecosSwitch.Switch(stateIndex);
                }
            }
        }

        private void HandleRouteCommand_CheckRoute(JObject cmddata)
        {
            var native = cmddata?["native"] as JObject;
            var switches = native?["switches"] as JArray;
            if (switches == null) return;

            var field = GetPlanField();
            if (field == null) return;

            var dp = _sniffer.GetDataProvider();
            if (dp == null) return;

            foreach (var itSw in switches)
            {
                if (itSw == null) continue;
                var coordX = (int)itSw["x"];
                var coordY = (int)itSw["y"];
                Utilities.GetEcosAddress(field, coordX, coordY, out var ecosAddr1, out var ecosAddr2, out _, out _);

                var ecosAddr = -1;
                if (ecosAddr1 != null) ecosAddr = ecosAddr1.Value;
                else if (ecosAddr2 != null) ecosAddr = ecosAddr2.Value;
                if (ecosAddr == -1) continue;

                var ecosSwitch = dp.GetAccessoryByAddress(ecosAddr) as Accessory;
                if (ecosSwitch == null) continue;

                var targetState = string.Empty;
                if (itSw["switch"] is JObject sw)
                    targetState = sw["state"]?.ToString();

                var stateIndex = -1;
                if (!string.IsNullOrEmpty(targetState))
                {
                    if (targetState.Equals("straight", StringComparison.OrdinalIgnoreCase))
                        stateIndex = 0;
                    else if (targetState.Equals("turn", StringComparison.OrdinalIgnoreCase))
                        stateIndex = 1;
                    else if (targetState.Equals("turnright", StringComparison.OrdinalIgnoreCase))
                        stateIndex = 1;
                    else if (targetState.Equals("turnleft", StringComparison.OrdinalIgnoreCase))
                        stateIndex = 1;
                    else
                    {
                        SendDebugMessage($"Unknown target state '{targetState}' for {cmddata.GetString("identifier")}", DebugMessageLevel.Warning);
                    }
                }
                if (stateIndex == -1) continue;

                SendDebugMessage($"{cmddata.GetString("identifier")} switches to '{targetState}'.");

                if (IsSimulationMode())
                    ecosSwitch.SwitchSimulation(stateIndex);
                else
                    ecosSwitch.Switch(stateIndex);
            }
        }

        private void HandleRouteCommand(JObject cmddata)
        {
            var mode = cmddata["mode"]?.ToString();
            if (string.IsNullOrEmpty(mode)) return;

            switch (mode.ToLower())
            {
                case "checkroute":
                    HandleRouteCommand_CheckRoute(cmddata);
                    break;

                case "gotoblock":
                    HandleRouteCommand_GoToBlock(cmddata);
                    break;

                case "assigntoblock":
                    HandleRouteCommand_AssignToBlock(cmddata);
                    break;

                case "resetassignment":
                    HandleRouteCommand_ResetAssignment(cmddata);
                    break;

                case "analyze":
                    HandleRouteCommand_Analyze();
                    break;
            }

            _sniffer?.SendCommandsToEcosStation();
        }

        private void HandleAccessoryCommand(JObject cmddata)
        {
            var coord = cmddata?["coord"] as JObject;
            if (coord?["x"] == null || coord["y"] == null) return;

            var field = GetPlanField();
            if (field == null) return;

            var coordX = int.Parse(coord["x"].ToString().Trim());
            var coordY = int.Parse(coord["y"].ToString().Trim());
            Utilities.GetEcosAddress(field, coordX, coordY, out var ecosAddr1, out var ecosAddr2, out _, out _);

            var planItem = field.Get(coordX, coordY);
            if (planItem.IsSensor)
            {
                _sniffer?.Logger?.Log?.Info($"Sensor/Feedback manually triggered: {planItem.identifier}");
                SendDebugMessage($"Sensor/Feedback manually triggered: {planItem.identifier}");

                if (IsSimulationMode())
                {
                    var dpS88 = _sniffer.GetDataProviderS88();
                    if (dpS88 == null) return;

                    var feedbacks = new List<S88>();
                    foreach (var it in dpS88.Objects)
                    {
                        if (it.TypeId() == S88.Typeid)
                            feedbacks.Add(it as S88);
                    }

                    var feedbackAddr = planItem.Addresses.Addr;

                    var offsetEnd = 0;
                    foreach (var fb in feedbacks)
                    {
                        if (fb == null) continue;
                        var offsetStart = offsetEnd;
                        offsetEnd = offsetStart + fb.Ports;
                        if (feedbackAddr <= offsetStart || feedbackAddr > offsetEnd) continue;

                        // fbIdx range:  [1 .. 16] real world
                        // fbIdx range:  [0 .. 15] programming world
                        var fbIdx = feedbackAddr - offsetStart; // we have "real world" index

                        fb.ChangeStateSimulation(fbIdx - 1);

                        break;
                    }

                    SaveAll();

                    _sniffer?.TriggerDataProviderModifiedForSimulation();
                }

                return;
            }

            var dp = _sniffer.GetDataProvider();
            if (dp == null) return;

            if (ecosAddr1 != null && ecosAddr2 != null)
            {
                var ecosAcc1 = dp.GetAccessoryByAddress(ecosAddr1.Value) as Accessory;
                var ecosAcc2 = dp.GetAccessoryByAddress(ecosAddr2.Value) as Accessory;

                if (ecosAcc1 == null || ecosAcc2 == null)
                {
                    _sniffer?.Logger?.Log?.Warn($"One or two accessories are missing: {ecosAddr1} and {ecosAddr2}");
                    SendDebugMessage($"One or two accessories are missing: {ecosAddr1} and {ecosAddr2}", DebugMessageLevel.Warning);
                }
                else
                {
                    var currentState1 = ecosAcc1.State;
                    var currentState2 = ecosAcc2.State;

                    int idx;
                    if (currentState1 == 0 && currentState2 == 0)
                        idx = 0;
                    else if (currentState1 == 0 && currentState2 == 1)
                        idx = 1;
                    else if (currentState1 == 1 && currentState2 == 0)
                        idx = 2;
                    else if (currentState1 == 1 && currentState2 == 1)
                        idx = 3;
                    else
                        return;

                    ++idx;

                    if (idx > 3) idx = 0;

                    switch (idx)
                    {
                        case 0:
                            {
                                currentState1 = 0;
                                currentState2 = 0;
                            }
                            break;

                        case 1:
                            {
                                currentState1 = 0;
                                currentState2 = 1;
                            }
                            break;

                        case 2:
                            {
                                currentState1 = 1;
                                currentState2 = 0;
                            }
                            break;

                        case 3:
                            {
                                currentState1 = 1;
                                currentState2 = 1;
                            }
                            break;
                    }

                    SendDebugMessages(new List<string>
                    {
                        $"{planItem.identifier} switches {ecosAcc1} to '{currentState1}'.",
                        $"{planItem.identifier} switches {ecosAcc2} to '{currentState2}'."
                    });

                    _sniffer?.SendCommandsToEcosStation();

                    if (IsSimulationMode())
                    {
                        ecosAcc1.SwitchSimulation(currentState1);
                        ecosAcc2.SwitchSimulation(currentState2);
                        SaveAll();
                        _sniffer?.TriggerDataProviderModifiedForSimulation();
                    }
                    else
                    {
                        ecosAcc1.Switch(currentState1);
                        ecosAcc2.Switch(currentState2);
                        _sniffer?.SendCommandsToEcosStation();
                    }
                }
            }
            else
            {
                var ecosAddr = -1;
                if (ecosAddr1 != null) ecosAddr = ecosAddr1.Value;
                else if (ecosAddr2 != null) ecosAddr = ecosAddr2.Value;
                if (ecosAddr == -1)
                {
                    _sniffer?.Logger?.Log?.Warn($"Accessory has no address: {cmddata.GetString("ctrlId", "unknown")}");
                    SendDebugMessage($"Accessory has no address: {cmddata.GetString("ctrlId", "unknown")}", DebugMessageLevel.Warning);
                    return;
                }

                var ecosAcc = dp.GetAccessoryByAddress(ecosAddr) as Accessory;
                if (ecosAcc == null) return;

                var currentState = ecosAcc.State;
                var maxState = ecosAcc.Addrext.Count;
                ++currentState;
                if (currentState >= maxState) currentState = 0;

                SendDebugMessage($"{planItem.identifier} switches {ecosAddr} to '{currentState}'.");

                if (IsSimulationMode())
                {
                    ecosAcc.SwitchSimulation(currentState);
                    SaveAll();
                    _sniffer?.TriggerDataProviderModifiedForSimulation();
                }
                else
                {
                    ecosAcc.Switch(currentState);
                    _sniffer?.SendCommandsToEcosStation();
                }
            }
        }

        private void HandleSettingCommand(JObject cmddata)
        {
            var mode = cmddata.GetString("mode")?.ToLower();
            if (string.IsNullOrEmpty(mode)) return;

            var cmd = cmddata.GetString("cmd")?.ToLower();
            if (string.IsNullOrEmpty(cmd)) return;

            var value = cmddata["value"];

            switch (mode)
            {
                case "locomotive":
                    {
                        HandleSettingLocomotive(cmd, value);
                    }
                    break;

                case "ecos":
                    {
                        HandleSettingEcos(cmd);
                    }
                    break;

                case "block":
                    {
                        HandleSettingBlock(cmd, value);
                    }
                    break;

                case "accessory":
                    {
                        HandleSettingAccessory(cmd, value);
                    }
                    break;

                case "route":
                    {
                        HandleSettingRoute(cmd, value);
                    }
                    break;
            }

            _sniffer?.SendCommandsToEcosStation();
        }

        private void HandleRelayCommand(JObject cmddata)
        {
            var mode = cmddata.GetString("mode")?.ToLower();
            if (string.IsNullOrEmpty(mode)) return;

            if (mode.Equals("websocket", StringComparison.OrdinalIgnoreCase))
            {
                HandleRelayCommand_WebSocket(cmddata);
                return;
            }

            SendDebug(DebugMessage.Instance($"RelayCommand mode '{mode}' is unknown.", DebugMessageLevel.Warning));
        }

        private void HandleRelayCommand_WebSocket(JObject cmddata)
        {
            var target = cmddata.GetString("target");
            if (string.IsNullOrEmpty(target)) return;

            var contentType = cmddata.GetString("contentType");
            if (string.IsNullOrEmpty(contentType)) return;

            var data = cmddata["data"] as JObject;
            if (data == null) return;

            var dataToSend = data.ToString(Formatting.None).Trim().Replace(" ", string.Empty);
            _sniffer?.Logger?.Log?.Info($"<Relay> {target} : {dataToSend}");
            var res = RelayCommand.RelayCommandWebsocket.Send(target, dataToSend);
            if (!res.Result)
            {
                _sniffer?.Logger?.Log?.Warn($"<Relay> {res.LastError}");
                if (res.Ex != null)
                    _sniffer?.Logger?.Log?.Warn("<Relay>", res.Ex);

                SendDebug(new List<DebugMessage>
                {
                    DebugMessage.Instance($"{res.LastError}", DebugMessageLevel.Error),
                    DebugMessage.Instance($"{res.Ex?.Message ?? "Exception"}", DebugMessageLevel.Error)
                });
            }
        }

        private void HandleSettingLocomotive(string cmd, JToken value)
        {
            if (string.IsNullOrEmpty(cmd)) return;
            var data = value as JObject;
            if (data == null) return;
            var oid = data.GetInt("oid");
            if (oid == 0) return;
            var locomotiveItem = GetLocomotiveByOid(oid);
            if (locomotiveItem == null) return;

            switch (cmd.ToLower())
            {
                case "rename":
                    {
                        var name = data.GetString("name");
                        if (string.IsNullOrEmpty(name)) return;
                        locomotiveItem.ChangeName(name);
                    }
                    break;

                case "start":
                    {
                        var isStarted = data.GetBool("state");

                        lock (_metadataLock)
                        {
                            var locData = _metadata.LocomotivesData.GetData(oid);
                            locData.IsStopped = !isStarted;
                            _metadata?.Save(Metadata.SaveModelType.LocomotivesData);
                        }

                        if (IsAutoModeStarted())
                            _autoMode?.StartLocomotive(oid);

                        SendModelToClients(ModelType.UpdateLocomotivesData);
                    }
                    break;

                case "finalize":
                    {
                        var isStopped = data.GetBool("state");

                        lock (_metadataLock)
                        {
                            var locData = _metadata.LocomotivesData.GetData(oid);
                            locData.IsStopped = isStopped;
                            _metadata?.Save(Metadata.SaveModelType.LocomotivesData);
                        }

                        if (IsAutoModeStarted())
                            _autoMode?.FinalizeLocomotive(oid);

                        SendModelToClients(ModelType.UpdateLocomotivesData);
                    }
                    break;

                case "lock":
                    {
                        var isLocked = data.GetBool("locked");

                        if (isLocked)
                        {
                            if (IsAutoModeStarted())
                            {
                                _autoMode?.StopLocomotive(oid);
                                _autoMode?.ResetRouteFor(oid);
                            }
                        }

                        lock (_metadataLock)
                        {
                            _metadata.LocomotivesData.SetLocked(oid, isLocked);
                            _metadata?.Save(Metadata.SaveModelType.LocomotivesData);
                        }

                        SendModelToClients(ModelType.UpdateLocomotivesData);
                    }
                    break;

                case "locomotivedata":
                    {
                        Dictionary<string, bool> checkboxSettings = null;

                        var dataCheckboxSettings = data["checkboxSettings"];
                        if (dataCheckboxSettings != null)
                            checkboxSettings =
                                JsonConvert.DeserializeObject<Dictionary<string, bool>>(dataCheckboxSettings.ToString());

                        lock (_metadataLock)
                        {
                            var enterSide = data.GetString("blockEnterSide", string.Empty);

                            var enterBlockSideChanged = false;

                            var locExtraData = _metadata.LocomotivesData.GetData(oid);
                            if (locExtraData != null)
                            {
                                if (checkboxSettings != null)
                                {
                                    locExtraData.Settings = checkboxSettings;
                                }

                                enterBlockSideChanged = !locExtraData.EnterBlockSide.Equals(enterSide, StringComparison.OrdinalIgnoreCase);
                                locExtraData.EnterBlockSide = enterSide;
                            }
                            else
                            {
                                var data0 = new Data
                                {
                                    EnterBlockSide = enterSide
                                };

                                enterBlockSideChanged = true;

                                if (checkboxSettings != null)
                                    data0.Settings = checkboxSettings;

                                _metadata.LocomotivesData.Entries.Add($"{oid}", data0);
                            }

                            if (enterBlockSideChanged)
                            {
                                if (IsAutoModeStarted())
                                    _autoMode?.ResetRouteFor(oid);
                            }

                            _metadata?.Save(Metadata.SaveModelType.LocomotivesData);
                        }

                        SendModelToClients(ModelType.UpdateLocomotivesData);
                    }
                    break;
            }
        }

        private void HandleSettingEcos(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) return;

            var dp = _sniffer.GetDataProvider();
            if (dp == null) return;

            switch (cmd.ToLower())
            {
                case "power":
                    {
                        var ecosBase = dp.GetObjectBy(1) as Ecos2;
                        if (ecosBase == null) return;
                        switch (ecosBase.CurrentState)
                        {
                            case Ecos2.State.Go: // do stop
                                ecosBase.AddCmd(CommandFactory.Create("set(1, stop)"));
                                break;

                            case Ecos2.State.Stop: // do go
                                ecosBase.AddCmd(CommandFactory.Create("set(1, go)"));
                                break;
                        }
                        ecosBase.AddCmd(CommandFactory.Create("get(1, status)"));
                        ecosBase.AddCmd(CommandFactory.Create("get(1, status)"));
                    }
                    break;

                case "stop":
                    {
                        var objs = dp.Objects;
                        foreach (var it in objs)
                        {
                            var itLoc = it as Locomotive;
                            itLoc?.Stop();
                        }
                    }
                    break;
            }
        }

        private void HandleSettingAccessory(string cmd, JToken value)
        {
            if (string.IsNullOrEmpty(cmd)) return;
            var data = value as JObject;
            if (data == null) return;

            switch (cmd.ToLower())
            {
                case "address":
                    {
                        var accIdentifier = data.GetString("identifier", string.Empty);
                        if (string.IsNullOrEmpty(accIdentifier)) return;

                        lock (_metadataLock)
                        {
                            // save the values
                            var acc = _metadata?.GetMetamodelItem(accIdentifier);
                            if (acc == null) return;
                            PlanItem recentPlanItem = null;
                            if (acc["addresses"] != null)
                                recentPlanItem = JsonConvert.DeserializeObject<PlanItem>(acc["addresses"].ToString());

                            acc["addresses"] = new JObject
                            {
                                ["Addr"] = recentPlanItem?.Addresses.Addr ?? 0,

                                ["Addr1"] = data.GetInt("Addr1"),
                                ["Port1"] = data.GetInt("Port1"),
                                ["Inverse1"] = data.GetBool("Inverse1"),

                                ["Addr2"] = data.GetInt("Addr2"),
                                ["Port2"] = data.GetInt("Port2"),
                                ["Inverse2"] = data.GetBool("Inverse2")
                            };

                            _metadata?.Save(Metadata.SaveModelType.MetamodelData);
                        }

                        SendModelToClients(ModelType.UpdateEcos);
                    }
                    break;
            }
        }

        private void HandleSettingBlock(string cmd, JToken value)
        {
            if (string.IsNullOrEmpty(cmd)) return;
            var data = value as JObject;
            if (data == null) return;

            switch (cmd.ToLower())
            {
                case "blockdata":
                    {
                        // apply these two data to "metamodel.xyz.json"
                        FbData fbEnter = null;
                        FbData fbIn = null;
                        if (data["fbEnter"] != null)
                            fbEnter = JsonConvert.DeserializeObject<FbData>(data["fbEnter"].ToString());
                        if (data["fbIn"] != null)
                            fbIn = JsonConvert.DeserializeObject<FbData>(data["fbIn"].ToString());
                        if (fbEnter != null)
                            UpdateFeedbackEcosAddr(fbEnter);
                        if (fbIn != null)
                            UpdateFeedbackEcosAddr(fbIn);

                        List<DeniedLocomotive> deniedLocomotives = null;
                        Dictionary<string, bool> checkboxSettings = null;

                        var dataDeniedLocomotives = data["deniedLocomotives"];
                        if (dataDeniedLocomotives != null)
                            deniedLocomotives =
                                JsonConvert.DeserializeObject<List<DeniedLocomotive>>(dataDeniedLocomotives.ToString());

                        var dataCheckboxSettings = data["checkboxSettings"];
                        if (dataCheckboxSettings != null)
                            checkboxSettings =
                                JsonConvert.DeserializeObject<Dictionary<string, bool>>(dataCheckboxSettings.ToString());

                        var blockId = data.GetString("blockIdentifier");

                        lock (_metadataLock)
                        {
                            // save the values
                            var availableBlock = _metadata?.FeedbacksData.GetByBlockId(blockId);
                            if (availableBlock != null)
                            {
                                availableBlock.DeniedLocomotives = deniedLocomotives;
                                availableBlock.Settings = checkboxSettings;
                            }

                            _metadata?.Save(Metadata.SaveModelType.FeedbacksData);
                        }

                        SendModelToClients(ModelType.UpdateFeedbacks);
                    }
                    break;
            }
        }

        private void HandleSettingRoute(string cmd, JToken value)
        {
            if (string.IsNullOrEmpty(cmd)) return;
            var data = value as JObject;
            if (data == null) return;

            switch (cmd.ToLower())
            {
                case "disable":
                    {
                        var routeName = data.GetString("routeName", null);
                        if (string.IsNullOrEmpty(routeName))
                        {
                            SendDebugMessage($"No route set to enable/disable it.");
                            return;
                        }

                        var routeDisabled = data.GetBool("disableState", true);
                        var routeExist = false;
                        lock (_metadataLock)
                        {
                            var routes = _metadata.Routes;
                            foreach (var it in routes)
                            {
                                var itName = it?["name"]?.ToString();
                                if (string.IsNullOrEmpty(itName)) continue;
                                if (itName.Equals(routeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    it["isDisabled"] = routeDisabled;
                                    routeExist = true;
                                    break;
                                }
                            }

                            if (routeExist)
                                _metadata?.Save(Metadata.SaveModelType.RouteData);
                        }

                        if (routeExist)
                        {
                            if (_autoMode != null && _autoMode.IsStarted())
                                _autoMode.ApplyRouteDisableState(routeName, routeDisabled);

                            SendModelToClients(ModelType.UpdateRoutes);
                        }
                        else
                        {
                            SendDebugMessage($"Route {routeName} does not exist.");
                        }
                    }
                    break;
            }
        }

        private void UpdateFeedbackEcosAddr(FbData data)
        {
            if (data == null) return;
            if (string.IsNullOrEmpty(data.Id)) return;

            try
            {
                lock (_metadataLock)
                {
                    var planField = _metadata.Metamodel["planField"] as JObject;
                    if (planField == null) return;

                    foreach (var it in planField)
                    {
                        var itemObj = it.Value as JObject;
                        if (itemObj == null) continue;
                        var id = itemObj["identifier"]?.ToString();
                        if (string.IsNullOrEmpty(id)) continue;
                        if (id.Equals(data.Id))
                        {
                            // found, change address
                            if (itemObj["addresses"] is JObject addr)
                            {
                                var dccAddr = AddressUtilities.GetDccAddr(data.EcosAddr);
                                var dccPort = AddressUtilities.GetDccPort(data.EcosAddr);

                                addr["Addr"] = data.EcosAddr;

                                addr["Addr1"] = dccAddr;
                                addr["Port1"] = dccPort;

                                addr["Addr2"] = 0;
                                addr["Port2"] = 0;

                                itemObj["addresses"] = addr;
                            }

                            return;
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private void ResetBlockEnterSideOf(int oid, string resetValue = "")
        {
            if (_metadata == null) return;

            lock (_metadataLock)
            {
                var locExtraData = _metadata.LocomotivesData.GetData(oid);
                if (locExtraData != null)
                {
                    locExtraData.EnterBlockSide = resetValue;
                }
                else
                {
                    _metadata.LocomotivesData.Entries.Add($"{oid}", new Data
                    {
                        EnterBlockSide = resetValue
                    });
                }

                _metadata?.Save(Metadata.SaveModelType.LocomotivesData);
            }
        }

        private bool RemoveItem(string itemIdentifier)
        {
            if (string.IsNullOrEmpty(itemIdentifier)) return false;

            lock (_metadataLock)
            {
                var keysToRemove = new List<string>();

                var planField = _metadata.Metamodel["planField"] as JObject;
                if (planField == null) return false;

                foreach (var it in planField)
                {
                    var itemObj = it.Value as JObject;
                    if (itemObj == null) continue;
                    var id = itemObj["identifier"]?.ToString();
                    if (string.IsNullOrEmpty(id)) continue;
                    if (id.Equals(itemIdentifier))
                        keysToRemove.Add(it.Key);
                }

                keysToRemove.ForEach(s => planField.Remove(s));

                return true;
            }
        }

        private bool UpdateItem(JObject coord, JObject data)
        {
            lock (_metadataLock)
            {
                var accessor = coord["x"] + "x" + coord["y"];
                var planField = _metadata.Metamodel["planField"] as JObject;
                if (planField == null) return false;
                planField[accessor] = data;
                return planField[accessor] != null;
            }
        }

        [Flags]
        internal enum ModelType
        {
            Update = 1,
            UpdateMetamodel = 2,
            UpdateEcos = 4,
            UpdateRoutes = 8,
            UpdateOcc = 16,
            UpdateLocomotivesData = 32,
            UpdateFeedbacks = 64,
            Initialization = 128
        }

        internal void SendModelToClient(WebSocketSession session, ModelType modelType)
        {
            if (session == null) return;
            if (session.InClosing) return;
            if (!session.Connected) return;

            // ModelType.UpdateOcc only
            if ((modelType & ModelType.UpdateOcc) != 0)
            {
                lock (_metadataLock)
                {
                    var data = new JObject
                    {
                        ["command"] = "occ",
                        ["data"] = _metadata.Occ.ToJson()
                    };

                    session.Send(data.ToString(Formatting.Indented));
                }

                return;
            }

            // ModelType.UpdateLocomotivesData only
            if ((modelType & ModelType.UpdateLocomotivesData) != 0)
            {
                lock (_metadataLock)
                {
                    var data = new JObject
                    {
                        ["command"] = "locomotivesData",
                        ["data"] = _metadata.LocomotivesData.ToJson()
                    };

                    session.Send(data.ToString(Formatting.Indented));
                }

                return;
            }

            // ModelType.UpdateFeedbacks only
            if ((modelType & ModelType.UpdateFeedbacks) != 0)
            {
                lock (_metadataLock)
                {
                    var data = new JObject
                    {
                        ["command"] = "feedbacksData",
                        ["data"] = _metadata.FeedbacksData.ToJson()
                    };

                    session.Send(data.ToString(Formatting.Indented));
                }

                return;
            }

            lock (_metadataLock)
            {
                string cmd;
                var isInit = false;

                if ((modelType & ModelType.Initialization) != 0)
                {
                    cmd = "initialization";
                    isInit = true;
                }
                else
                {
                    cmd = "update";
                }

                if (string.IsNullOrEmpty(cmd))
                    return;

                var data = new JObject
                {
                    ["command"] = cmd
                };

                if ((modelType & ModelType.Initialization) != 0)
                {
                    data["metamodel"] = _metadata?.Metamodel;
                }
                else if ((modelType & ModelType.Update) != 0)
                {
                    data["metamodel"] = _metadata?.Metamodel;
                }

                if ((modelType & ModelType.Initialization) != 0 || (modelType & ModelType.Update) != 0)
                {
                    data["routes"] = _metadata?.Routes;

                    var m = _metadata?.EcosData;
                    if (m != null && (modelType & ModelType.Initialization) != 0)
                    {
                        // force refresh of all visualizations during initialization
                        if (m["ecosbase"] != null) m["ecosbaseChanged"] = true;
                        if (m["locomotives"] != null) m["locomotivesChanged"] = true;
                        if (m["accessories"] != null) m["accessoriesChanged"] = true;
                        if (m["feedbacks"] != null) m["feedbacksChanged"] = true;
                    }
                    data["ecosData"] = m;
                }
                else
                {
                    if ((modelType & ModelType.UpdateEcos) != 0) data["ecosData"] = _metadata?.EcosData;
                    if ((modelType & ModelType.UpdateMetamodel) != 0) data["metamodel"] = _metadata?.Metamodel;
                    if ((modelType & ModelType.UpdateRoutes) != 0) data["routes"] = _metadata?.Routes;
                }

                if (isInit)
                {
                    data["themeData"] = _themeData;
                    data["settings"] = new JObject
                    {
                        ["themeName"] = _cfg.ThemeName
                    };
                }

                session.Send(data.ToString(Formatting.None));
            }
        }

        internal void SendModelToClients(ModelType modelType)
        {
            var wsServer = _sniffer?.GetWsServer();
            if (wsServer == null) return;

            foreach (var itClient in wsServer.ConnectedClients)
                SendModelToClient(itClient?.NativeSession, modelType);
        }

        internal void SendCommandToClients(JObject obj)
        {
            var wsServer = _sniffer?.GetWsServer();
            if (wsServer == null) return;

            foreach (var itClient in wsServer.ConnectedClients)
                itClient?.NativeSession?.Send(obj.ToString(Formatting.None));
        }
    }
}
