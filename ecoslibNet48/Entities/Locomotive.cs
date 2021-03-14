// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Locomotive.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ecoslib.Utilities;
using Newtonsoft.Json.Linq;
// ReSharper disable InconsistentNaming

namespace ecoslib.Entities
{
    public sealed class Locomotive : Item
    {
        public override string Caption => $"{ObjectId}: {Name}, {Addr}, {Speed}, " + (IsBackward ? "BACK" : "FORWARD");

        public enum ProtocolSteps
        {
            MMFKT = 14,
            MM14 = 14,
            DCC14 = 14,

            MM27 = 27,
            DCC28 = 28,

            MM128 = 128,
            DCC128 = 128,
            MFX = 128
        }

        public int GetSpeedByPercentage(float percentage)
        {
            var speed = GetNumberOfSpeedsteps(Protocol) * percentage;
            return (int) Math.Floor(speed);
        }

        public const int Typeid = 1;

        public override int TypeId() { return Typeid; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return $"Locomotive {Addr},{Protocol}";
            return Name;
        }

        public string Name { get; set; } = string.Empty;

        public static int GetNumberOfSpeedsteps(string protocol)
        {
            if (string.IsNullOrEmpty(protocol)) return 128;
            if (protocol.Equals("MM14", StringComparison.OrdinalIgnoreCase)) return 14;
            if (protocol.Equals("MM27", StringComparison.OrdinalIgnoreCase)) return 27;
            if (protocol.Equals("MM128", StringComparison.OrdinalIgnoreCase)) return 128;
            if (protocol.Equals("DCC14", StringComparison.OrdinalIgnoreCase)) return 14;
            if (protocol.Equals("DCC28", StringComparison.OrdinalIgnoreCase)) return 28;
            if (protocol.Equals("DCC128", StringComparison.OrdinalIgnoreCase)) return 128;
            if (protocol.Equals("MFX", StringComparison.OrdinalIgnoreCase)) return 128;
            if (protocol.Equals("MMFKT", StringComparison.OrdinalIgnoreCase)) return 14;
            return 128;
        }

        public int GetNumberOfSpeedsteps()
        {
            return GetNumberOfSpeedsteps(Protocol);
        }

        /// MM14, MM27, MM128, DCC14, DCC28, DCC128, SX32, MMFKT
        public string Protocol { get; set; } = string.Empty;

        /// <summary>
        /// describes the address to control any object
        /// </summary>
        public int Addr { get; set; } = -1;

        public int Speed { get; set; } = -1;

        public int Speedstep { get; set; } = -1;

        public int Direction { get; set; } = -1;

        public int NrOfFunctions { get; set; } = -1;

        public List<bool> Funcset { get; set; } = new(32);
        public string Profile { get; set; } = string.Empty;
        public Dictionary<int, int> Funcdesc { get; set; } = new();
        public bool IsBackward => Direction == 1;
        public bool IsForward => Direction == 0;

        public Locomotive()
        {
            if (Funcset.Count != 0) return;
            for (var i = 0; i < 32; ++i)
                Funcset.Add(false);
        }
        
        public void ChangeName(string name)
        {
            AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));
            AddCmd(CommandFactory.Create($@"set({ObjectId}, name[""{name}""])", true));
            AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
            AddCmd(CommandFactory.Create($"get({ObjectId}, name)"));    
        }

        public void Stop()
        {
            ChangeSpeedstep(0, true, true);
        }

        public void StopSimulation()
        {
            ChangeSpeedstepSimulation(0);
        }

	    public void ToggleFunctions(Dictionary<uint, bool> states)
	    {
		    AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));

		    foreach (var it in states)
		    {
			    var nr = it.Key;
			    var state = it.Value;
			    var v = state ? 1 : 0;
			    AddCmd(CommandFactory.Create($"set({ObjectId}, func[{nr}, {v}])"));
			    Funcset[(int)nr] = state;
		    }

			AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
		}
		
        public void ChangeDirectionSimulation(bool backward)
        {
            _hasChanged = true;
            var v = backward ? 1 : 0;
            Direction = v;
        }

        public void ChangeDirection(bool backward)
        {
            var v = backward ? 1 : 0;
            Direction = v;
            AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));
            AddCmd(CommandFactory.Create($"set({ObjectId}, dir[{v}])"));
            AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
        }

        /// <summary>
        /// Supported levels are:
        ///   [0] Level0
        ///   [1] Level1
        ///   [2] Level2
        ///   [3] Level3
        ///   [4] Level4
        /// </summary>
        /// <param name="level"></param>
        public void ChangeSpeedLevel(string level)
        {
            if (string.IsNullOrEmpty(level))
                level = "level0"; // for security reason we just stop on invalid command

            level = level.ToLower();

            // TODO change fixed values
            var levelValuesPercentage = new Dictionary<string, float>
                {
                    {"level0", 0 },
                    {"level1", 0.10f },
                    {"level2", 0.20f },
                    {"level3", 0.40f },
                    {"level4", 0.55f }
                };

            var percentage = levelValuesPercentage[level];
            var speed = GetSpeedByPercentage(percentage);

            ChangeSpeedstep(speed, true, true);
        }

        public void ChangeSpeedstep(int fahrstufe, bool isFirst = true, bool isLast = true)
        {
			if(isFirst)
            {
                AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));
            }
            
            AddCmd(CommandFactory.Create($"set({ObjectId}, speedstep[{fahrstufe}])"));
	        
            if (isLast)
	        {
		        AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
				AddCmd(CommandFactory.Create($"get({ObjectId}, speed, speedstep)"));
	        }

	        Speed = fahrstufe;
        }
        
        public void ChangeSpeedstepSimulation(int fahrstufe)
        {
            _hasChanged = true;

            Speedstep = fahrstufe;
        }

        public void IncreaseSpeed()
        {
            var currentSpeed = Speedstep;
            var maxSpeed = GetNumberOfSpeedsteps();
            if (currentSpeed >= maxSpeed)
                return;

            AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));

            ++currentSpeed;
            AddCmd(CommandFactory.Create($"set({ObjectId}, speedstep[{currentSpeed}])"));
            Speedstep = currentSpeed;

            AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
            AddCmd(CommandFactory.Create($"get({ObjectId}, speed, speedstep)"));
        }

        public void DecreaseSpeed()
        {
            var currentSpeed = Speedstep;
            if (currentSpeed == 0)
                return;

            AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));

            --currentSpeed;
            AddCmd(CommandFactory.Create($"set({ObjectId}, speedstep[{currentSpeed}])"));
            Speedstep = currentSpeed;

            AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
            AddCmd(CommandFactory.Create($"get({ObjectId}, speed, speedstep)"));
        }

        public override void QueryState()
        {
            AddCmd(CommandFactory.Create(
                $"get({ObjectId}, speed, speedstep, profile, protocol, name, addr, dir, funcset, funcdesc)"));
        }

        public override bool Parse(List<object> arguments)
        {
			foreach (var a in arguments)
			{
				var arg = a as ICommandArgument;
				if (arg == null) continue;

                if (arg.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                {
                    if (Name.Equals(arg.Parameter[0])) continue;
                    _hasChanged = true;

                    Name = arg.Parameter[0];
                }
                else if (arg.Name.Equals("protocol", StringComparison.OrdinalIgnoreCase))
                {
                    if (Protocol.Equals(arg.Parameter[0])) continue;
                    _hasChanged = true;

                    Protocol = arg.Parameter[0];
                }
                else if (arg.Name.Equals("addr", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(arg.Parameter[0], out var v);
                    if (v == Addr) continue;
                    _hasChanged = true;

                    Addr = v;
                }
                else if (arg.Name.Equals("speed", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(arg.Parameter[0], out var v);
                    if (v == Speed) continue;
                    _hasChanged = true;

                    Speed = v;
                }
                else if (arg.Name.Equals("speedstep", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(arg.Parameter[0], out var v);
                    if (v == Speedstep) continue;
                    _hasChanged = true;

                    Speedstep = v;
                }
                else if (arg.Name.Equals("dir", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(arg.Parameter[0], out var v);
                    if (v == Direction) continue;
                    _hasChanged = true;

                    Direction = v;
                }
                else if (arg.Name.Equals("funcdesc", StringComparison.OrdinalIgnoreCase))
                {
                    var sindex = arg.Parameter[0].Trim();
                    var stype = arg.Parameter[1].Trim();

                    if (!int.TryParse(sindex, out var index)) index = -1;
                    if (!int.TryParse(stype, out var type)) type = -1;

                    if (Funcdesc.ContainsKey(index))
                    {
                        Funcdesc[index] = type;
                    }
                    else
                    {
                        _hasChanged = true;

                        Funcdesc.Add(index, type);
                    }
                }
                else if (arg.Name.Equals("funcset", StringComparison.OrdinalIgnoreCase))
                {
                    NrOfFunctions = arg.Parameter[0].Length;
                    var usedFncs = 0;
                    for (var i = 0; i < Funcset.Count; ++i)
                    {
                        if (!IsValidFuncIndex(i))
                        {
                            Funcset[i] = false;
                        }
                        else
                        {
                            var currentFncState = Funcset[i];
                            var newFncState = arg.Parameter[0][usedFncs].Equals('1');

                            if (currentFncState != newFncState)
                            {
                                _hasChanged = true;

                                Funcset[i] = newFncState;
                            }

                            ++usedFncs;
                        }

                        if (usedFncs == NrOfFunctions)
                            break;
                    }
                }
                else if (arg.Name.Equals("func", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var idx = Convert.ToInt32(arg.Parameter[0]);
                        var currentValue = Funcset[idx];
                        var newValue = arg.Parameter[1].Equals("1", StringComparison.OrdinalIgnoreCase);
                        if (currentValue == newValue) continue;
                        _hasChanged = true;

                        Funcset[idx] = newValue;
                    }
                    catch
                    {
                        // ignore
                    }
                }
                else if (arg.Name.Equals("profile", StringComparison.OrdinalIgnoreCase))
                {
                    if (Profile.Equals(arg.Parameter[0])) continue;
                    _hasChanged = true;

                    Profile = arg.Parameter[0];
                }
                else
                {
                    Trace.WriteLine("Unknown argument: " + arg.Name + " -> " + string.Join(", ", arg.Parameter));
                }
            }

            return true;
        }

        private bool IsValidFuncIndex(int idx)
        {
            var maxIdx = Funcdesc.Count;
            if (idx < 0) return false;
            if (idx >= maxIdx) return false;
            var typeInfo = Funcdesc[idx];
            if (typeInfo == 0) return false;
            return true;
        }

        public override JObject ToJson()
        {
            var m = string.Empty;

            foreach (var f in Funcset)
            {
                if (f) m += "1";
                else m += "0";
            }

			var arFncDesc = new JArray();
	        foreach (var desc in Funcdesc)
	        {
		        var odesc = new JObject();
		        odesc["idx"] = desc.Key;
		        odesc["type"] = desc.Value;
				arFncDesc.Add(odesc);
	        }

            var o = new JObject
            {
                ["objectId"] = ObjectId,
                ["name"] = Name,
                ["protocol"] = Protocol,
                ["addr"] = Addr,
                ["speed"] = Speed,
                ["speedstep"] = Speedstep,
                ["speedstepMax"] = GetNumberOfSpeedsteps(),
                ["direction"] = Direction,
                ["funcset"] = m,
				["funcdesc"] = arFncDesc,
                ["nrOfFunctions"] = NrOfFunctions,
                ["profile"] = Profile
            };

            return o;
        }

        public override void ParseJson(JObject obj)
        {
            if (obj == null)
                return;

            if (obj["objectId"] != null) ObjectId = (int)obj["objectId"];
            if (obj["name"] != null) Name = obj["name"].ToString();
            if (obj["protocol"] != null) Protocol = obj["protocol"].ToString();
            if (obj["addr"] != null) Addr = (int)obj["addr"];
            if (obj["speed"] != null) Speed = (int)obj["speed"];
            if (obj["speedstep"] != null) Speedstep = (int)obj["speedstep"];
            if (obj["direction"] != null) Direction = (int)obj["direction"];
            if (obj["nrOfFunctions"] != null) NrOfFunctions = (int)obj["nrOfFunctions"];
            if (obj["profile"] != null) Profile = obj["profile"].ToString();

            if (obj["funcset"] != null)
            {
                var m = obj["funcset"].ToString();
                for (var i = 0; i < m.Length; ++i)
                    Funcset[i] = m[i] == '1';
            }

	        if (obj["funcdesc"] != null)
	        {
		        var ar = obj["funcdesc"] as JArray;
				for(var i = 0; i < ar.Count; ++i)
				{
					var oar = ar[i] as JObject;
                    if (oar?["idx"] != null && oar["type"] != null)
			        {
				        var idx = oar["idx"].ToString().ToInt(i);
				        var type = oar["type"].ToString().ToInt();
				        if (Funcdesc.ContainsKey(idx))
					        Funcdesc[idx] = type;
				        else
					        Funcdesc.Add(idx, type);
			        }
		        }
	        }
        }
    }
}
