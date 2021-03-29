// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: InitializeSystem.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using railessentials.Plan;

namespace railessentials
{
    public delegate void InitializeSystemProgress(object sender, InitializeSystemState state);
    public delegate void InitializeSystemStarted(object sender);
    public delegate void InitializeSystemFailed(object sender, string reason);
    public delegate void InitializeSystemFailedEx(object sender, Exception reason);
    public delegate void InitializeSystemFinished(object sender);

    public class InitializeSystemState
    {
        public string Message { get; set; }
    }

    public class InitializeSystem
    {
        public event InitializeSystemProgress Progress;
        public event InitializeSystemStarted Started;
        public event InitializeSystemFailed Failed;
        public event InitializeSystemFailedEx FailedEx;
        public event InitializeSystemFinished Finished;

        public ClientHandler.ClientHandler Ctx { get; }

        private int InitializeDelay => Ctx?._sniffer?.InitializeDelay ?? 250;

        public InitializeSystem(ClientHandler.ClientHandler ctx)
        {
            Ctx = ctx;
        }
        
        public async Task Start()
        {
            Started?.Invoke(this);
            Progress?.Invoke(this, new InitializeSystemState {Message = "Started" });

            await Task.Run(() =>
            {
                try
                {
                    // inititalize accessories
                    var planField = Ctx?.GetPlanField();
                    if (planField == null) throw new Exception("Planfield instance is missing.");
                    var relevantItems = new Dictionary<string, PlanItem>();
                    foreach (var it in planField)
                    {
                        var k = it.Key;
                        var item = it.Value;
                        if(item == null) continue;
                        if (!item.IsSignal && !item.IsSwitch) continue;
                        relevantItems.Add(k, item);
                    }

                    Progress?.Invoke(this, new InitializeSystemState
                    {
                        Message = $"Found {relevantItems.Count} accessories."
                    });

                    var dp = Ctx._sniffer.GetDataProvider();
                    if (dp == null)
                    {
                        Failed?.Invoke(this, "No datatype provider.");
                        return;
                    }

                    foreach (var it in relevantItems)
                    {
                        var parts = it.Key.Split('x');
                        if (parts.Length != 2) continue;
                        var x = int.Parse(parts[0]);
                        var y = int.Parse(parts[1]);

                        Utilities.GetEcosAddress(planField, x, y, out var ecosAddr1, out var ecosAddr2, out _, out _);

                        ecoslib.Entities.Accessory item0 = null;
                        if (ecosAddr1 != null)
                            item0 = dp.GetAccessoryByAddress(ecosAddr1.Value) as ecoslib.Entities.Accessory;

                        ecoslib.Entities.Accessory item1 = null;
                        if (ecosAddr2 != null)
                            item1 = dp.GetAccessoryByAddress(ecosAddr2.Value) as ecoslib.Entities.Accessory;

                        if (item0 != null)
                        {
                            Progress?.Invoke(this, new InitializeSystemState
                            {
                                Message = $"Switch {item0.Caption}"
                            });

                            if (Ctx._sniffer != null && Ctx._sniffer.IsSimulationMode)
                            {
                                item0.SwitchSimulation(0);
                                Ctx._sniffer?.TriggerDataProviderModifiedForSimulation();
                                item0.SwitchSimulation(1);
                                Ctx._sniffer?.TriggerDataProviderModifiedForSimulation();
                            }
                            else
                            {
                                item0.Switch(0);
                                SendAndWait();

                                item0.Switch(1);
                                SendAndWait();
                            }
                        }

                        if(item1 != null)
                        {
                            Progress?.Invoke(this, new InitializeSystemState
                            {
                                Message = $"Switch {item1.Caption}"
                            });

                            if (Ctx._sniffer != null && Ctx._sniffer.IsSimulationMode)
                            {
                                item1.SwitchSimulation(0);
                                Ctx._sniffer?.TriggerDataProviderModifiedForSimulation();
                                item1.SwitchSimulation(1);
                                Ctx._sniffer?.TriggerDataProviderModifiedForSimulation();
                            }
                            else
                            {
                                item1.Switch(0);
                                SendAndWait();

                                item1.Switch(1);
                                SendAndWait();
                            }
                        }
                    }

                    Finished?.Invoke(this);
                    Progress?.Invoke(this, new InitializeSystemState { Message = "Finished" });
                }
                catch (Exception ex)
                {
                    FailedEx?.Invoke(this, ex);
                }
            });
        }

        private void SendAndWait()
        {
            Ctx?._sniffer?.SendCommandsToEcosStation();
            System.Threading.Thread.Sleep(InitializeDelay);
        }
    }
}
