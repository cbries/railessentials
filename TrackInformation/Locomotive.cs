/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ecos2Core;
using Newtonsoft.Json.Linq;
using TrackInformationCore;

namespace TrackInformation
{
    public sealed class Locomotive : Item
    {
        public static string GetFncTypename(FncTypes type)
        {
            switch (type)
            {
                case FncTypes.Light: return "Lights";
                case FncTypes.Smoke: return "Smoke";
                case FncTypes.SoundPipe: return "Pipe (Sound)";
                case FncTypes.SoundCruise: return "Cruise (Sound)";
            }
            return null;
        }

        public static List<string> GetFncTypenames()
        {
            int n = Enum.GetNames(typeof(FncTypes)).Length;

            List<string> names = new List<string>();
            for(int i=0; i < n; ++i)
                names.Add(GetFncTypename((FncTypes) i));
            return names;
        }

        public static int SpeedStop = 0;
        public static int SpeedNormal = 50;
        public static int SpeedBlockEntered = 35;

        public DateTime StartTime { get; private set; }
        public DateTime StopTime { get; private set; }

        public const int Typeid = 1;

        public override int TypeId() { return Typeid; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return $"Locomotive {Addr},{Protocol}";
            return Name;
        }

        public override int SubTitleHeight => 16;

        public bool InitQueryStateDone { get; set; }

        private bool _locked;

        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                OnPropertyChanged();
                OnPropertyChanged("Locked");
            }
        }

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        private int _maxSpeedPercentage = 50;

        public int MaxSpeedPercentage
        {
            get => _maxSpeedPercentage;
            set
            {
                _maxSpeedPercentage = value;
                OnPropertyChanged();
                OnPropertyChanged("MaxSpeedPercentage");
            }
        }

        private int _blockSpeedPercentage = 50;

        public int BlockSpeedPercentage
        {
            get => _blockSpeedPercentage;
            set
            {
                _blockSpeedPercentage = value;
                OnPropertyChanged();
                OnPropertyChanged("BlockSpeedPercentage");
            }
        }

        private string _protocol;

        // MM14, MM27, MM28, DCC14, DCC28, DCC128, SX32, MMFKT
        public string Protocol
        {
            get => _protocol;
            set
            {
                _protocol = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        private int _addr;

        public int Addr
        {
            get => _addr;
            set
            {
                _addr = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        private int _speed; // percentage

        public int Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }
        
        private int _speedstep;

        public int Speedstep
        {
            get => _speedstep;
            set
            {
                _speedstep = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        private int _directon;

        public int Direction
        {
            get => _directon;
            set
            {
                _directon = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        private int _nrOfFunctions;

        public int NrOfFunctions
        {
            get => _nrOfFunctions;
            set
            {
                _nrOfFunctions = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        private List<bool> _funcset;

        public List<bool> Funcset
        {
            get => _funcset;
            set
            {
                _funcset = value;
                OnPropertyChanged();
            }
        }

        #region Function Types

        private List<string> _fncNames;
        private List<string> _fncTypeNames;
        private string _fncSelectedName = "--";
        private int _fncTypeIndex = 0;

        public List<string> FncNames
        {
            get => _fncNames;
            set
            {
                _fncNames = value;
                OnPropertyChanged("FncNames");
            }
        }

        public string FncSelectedName
        {
            get => _fncSelectedName;
            set
            {
                _fncSelectedName = value;

                if (_fncTypes.ContainsKey(value))
                    FncTypeIndex = (int) _fncTypes[value];
                else
                    FncTypeIndex = 0;

                OnPropertyChanged("FncSelectedName");
            }
        }

        public int FncTypeIndex
        {
            get => _fncTypeIndex;
            set
            {
                _fncTypeIndex = value;
                OnPropertyChanged("FncTypeIndex");
            }
        }

        public  List<string> FncTypeNames
        {
            get => _fncTypeNames;
            set
            {
                _fncTypeNames = value;
                OnPropertyChanged("FncTypeNames");
            }
        }

        private readonly Dictionary<string, FncTypes> _fncTypes = new Dictionary<string, FncTypes>();

        public void ApplyFncType()
        {
            if (string.IsNullOrEmpty(FncSelectedName))
                return;
            if (FncSelectedName.Trim().EndsWith("--", StringComparison.OrdinalIgnoreCase))
                return;

            if (_fncTypes.ContainsKey(FncSelectedName))
                _fncTypes[FncSelectedName] = (FncTypes)FncTypeIndex-1;
            else
                _fncTypes.Add(FncSelectedName, (FncTypes)FncTypeIndex-1);
        }

        #endregion

        #region locomotive image/icon

        private System.Drawing.Imaging.ImageFormat _imgLocBase64Format;
        private string _imgLocBase64;
        private string _thumbnailLocBase64;

        public string LocomotiveImageBase64
        {
            get => _imgLocBase64;
            set
            {
                _imgLocBase64 = value;
                OnPropertyChanged();
            }
        }

        public string LocomotiveThumbnailBase64
        {
            get => _thumbnailLocBase64;
            set
            {
                _thumbnailLocBase64 = value;
                OnPropertyChanged();
            }
        }

        public System.Drawing.Imaging.ImageFormat LocomotiveImageBase64Format
        {
            get => _imgLocBase64Format;
            set
            {
                _imgLocBase64Format = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public Locomotive()
        {
            StartTime = DateTime.MaxValue;
            StopTime = DateTime.MinValue;

            int n = Enum.GetNames(typeof(FncTypes)).Length;

            if (_fncTypeNames == null)
                _fncTypeNames = new List<string>();
            for (int i = 0; i < n; ++i)
                _fncTypeNames.Add(GetFncTypename((FncTypes)i));
            _fncTypeNames.Insert(0, "--");
            OnPropertyChanged(nameof(FncTypeNames));
            
            _funcset = new List<bool>(32);

            if (_funcset.Count == 0)
            {
                for (int i = 0; i < 32; ++i)
                    _funcset.Add(false);
            }
        }

        public override void UpdateTitle()
        {
            if (IsKnownByCommandStation)
                Title = $"{Name}";
            else
                Title = $"{Name} (NOT SYNC)";
        }

        public bool IsBackward => Direction == 1;
        public bool IsForward => Direction == 0;

        public override void UpdateSubTitle()
        {
            string v = IsBackward ? "Backward" : "Forward";

            SubTitle = $"V[{Speed}]->{v} ({Protocol}, {Addr}, {NrOfFunctions})";
        }

        public void ChangeName(string name)
        {
            List<ICommand> ctrlCmds = new List<ICommand>
            {
                CommandFactory.Create($"set({ObjectId}, name[\"{name}\"])", true),
                CommandFactory.Create($"get({ObjectId}, name)")
            };

            OnCommandsReady(this, ctrlCmds);
        }

        public void Stop()
        {
            List<ICommand> ctrlCmds = new List<ICommand>
            {
                CommandFactory.Create($"set({ObjectId}, stop)"),
            };

            StopTime = DateTime.Now;
            
            OnCommandsReady(this, ctrlCmds);
        }

        public void Restart()
        {
            var sp = this.Speed;

            StartTime = DateTime.Now;
            StopTime = DateTime.MaxValue;

            ChangeSpeed(sp);
        }

        public void ToggleFunctionType(int fncType, bool state)
        {
            List<int> fncs = new List<int>();

            foreach (var k in _fncTypes.Keys)
            {
                var v = (int)_fncTypes[k];

                if (v == fncType)
                {
                    var vv = k.TrimStart('F');
                    if (int.TryParse(vv, out int fidx))
                        fncs.Add(fidx);
                }
            }

            foreach (var idx in fncs)
                ToggleFunction((uint)idx, state);
        }

        public void ToggleFunction(uint nr, bool state)
        {
            int v = state ? 1 : 0;
            List<ICommand> ctrlCmds = new List<ICommand>
            {
                CommandFactory.Create($"request({ObjectId}, control, force)"),
                CommandFactory.Create($"set({ObjectId}, func[{nr}, {v}])"),
                CommandFactory.Create($"release({ObjectId}, control)")
            };

            Funcset[(int)nr] = state;

            OnCommandsReady(this, ctrlCmds);
        }

        public void ChangeDirection(bool backward)
        {
            int v = backward ? 1 : 0;
            Direction = v;
            List<ICommand> ctrlCmds = new List<ICommand>
            {
                CommandFactory.Create($"request({ObjectId}, control, force)"),
                CommandFactory.Create($"set({ObjectId}, dir[{v}])"),
                CommandFactory.Create($"release({ObjectId}, control)")
            };

            OnCommandsReady(this, ctrlCmds);
        }

        public void ChangeSpeed(int percentage)
        {
            if (_speed == 0 && percentage > 0)
            {
                StartTime = DateTime.Now;
                StopTime = DateTime.MinValue;
            }
            else
            {
                if (percentage <= 0)
                {
                    StartTime = DateTime.MaxValue;
                    StopTime = DateTime.Now;
                }
            }

            List<ICommand> ctrlCmds = new List<ICommand>
            {
                CommandFactory.Create($"request({ObjectId}, control, force)"),
                CommandFactory.Create($"set({ObjectId}, speed[{percentage}])"),
                CommandFactory.Create($"release({ObjectId}, control)")
            };

            Speed = percentage;

            OnCommandsReady(this, ctrlCmds);
        }

        public void QueryState()
        {
            List<ICommand> ctrlCmds = new List<ICommand>
            {
                CommandFactory.Create($"get({ObjectId}, speed, profile, protocol, name, addr, dir, funcset)"),
            };

            OnCommandsReady(this, ctrlCmds);
        }
        
        public override void Parse(List<CommandArgument> arguments)
        {
            foreach (var arg in arguments)
            {
                if (arg == null)
                    continue;

                if (arg.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                    Name = arg.Parameter[0];
                else if (arg.Name.Equals("protocol", StringComparison.OrdinalIgnoreCase))
                    Protocol = arg.Parameter[0];
                else if (arg.Name.Equals("addr", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Parameter[0], out int v))
                        Addr = v;
                    else
                        Addr = -1;
                }
                else if (arg.Name.Equals("speed", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Parameter[0], out int v))
                        Speed = v;
                    else
                        Speed = -1;
                }
                else if (arg.Name.Equals("speedstep", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Parameter[0], out int v))
                        Speedstep = v;
                    else
                        Speedstep = -1;
                }
                else if (arg.Name.Equals("dir", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Parameter[0], out int v))
                        Direction = v;
                    else
                        Direction = -1;
                }
                else if (arg.Name.Equals("funcdesc", StringComparison.OrdinalIgnoreCase))
                {
                    var sindex = arg.Parameter[0].Trim();
                    var stype = arg.Parameter[1].Trim();

                    if (!int.TryParse(sindex, out int index))
                        index = -1;
                    if (!int.TryParse(stype, out int type))
                        type = -1;

                    Trace.WriteLine("funcdesc: " + index + ", " + type);

                    //if (index != -1 && state != -1)
                    //    Funcset[index] = state == 1;
                }
                else if (arg.Name.Equals("funcset", StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine("funcset: " + arg.Parameter[0]);

                    NrOfFunctions = arg.Parameter[0].Length;

                    for (int i = 0; i < NrOfFunctions; ++i)
                        Funcset[i] = arg.Parameter[0][i] == '1' ? true : false;
                }
                else if (arg.Name.Equals("func", StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine($"Func {arg.Parameter[0]} to {arg.Parameter[1]}");
                }
                else
                {
                    Trace.WriteLine("Unknown argument: " + arg.Name + " -> " + string.Join(", ", arg.Parameter));
                }
            }

            OnPropertyChanged();
        }

        public override JObject ToJson()
        {
            string m = "";
            foreach (var f in _funcset)
            {
                if (f)
                    m += "1";
                else
                    m += "0";
            }

            JArray fncTypes = new JArray();
            foreach (var k in _fncTypes.Keys)
            {
                var vv = (int)_fncTypes[k];

                JObject oo = new JObject
                {
                    ["name"] = k,
                    ["fnc"] = vv
                };

                fncTypes.Add(oo);
            }

            JObject o = new JObject
            {
                ["objectId"] = ObjectId,
                ["name"] = _name,
                ["protocol"] = _protocol,
                ["addr"] = _addr,
                ["speed"] = _speed,
                ["speedstep"] = _speedstep,
                ["direction"] = _directon,                
                ["funcset"] = m,
                ["nrOfFunctions"] = NrOfFunctions,
                ["maxSpeedPercentage"] = MaxSpeedPercentage,
                ["blockSpeedPercentage"]  = BlockSpeedPercentage,
                ["locked"] = Locked,
                ["fncTypes"] = fncTypes,
                ["locimg"] = LocomotiveImageBase64,
                ["locthumbnail"] = LocomotiveThumbnailBase64
            };

            return o;
        }

        public override void ParseJson(JObject obj)
        {
            if (obj == null)
                return;

            if (obj["objectId"] != null)
                ObjectId = (int)obj["objectId"];
            if (obj["name"] != null)
                Name = obj["name"].ToString();
            if (obj["protocol"] != null)
                Protocol = obj["protocol"].ToString();
            if (obj["addr"] != null)
                Addr = (int) obj["addr"];
            if (obj["speed"] != null)
                Speed = (int) obj["speed"];
            if (obj["speedstep"] != null)
                Speedstep = (int) obj["speedstep"];
            if (obj["direction"] != null)
                Direction = (int) obj["direction"];
            if (obj["funcset"] != null)
            {
                string m = obj["funcset"].ToString();
                for (int i = 0; i < m.Length; ++i)
                    Funcset[i] = m[i] == '1';
            }
            if (obj["nrOfFunctions"] != null)
                NrOfFunctions = (int) obj["nrOfFunctions"];
            if (obj["maxSpeedPercentage"] != null)
                MaxSpeedPercentage = (int) obj["maxSpeedPercentage"];
            if (obj["blockSpeedPercentage"] != null)
                BlockSpeedPercentage = (int) obj["blockSpeedPercentage"];
            if (obj["locked"] != null)
                Locked = (bool) obj["locked"];

            if (obj["fncTypes"] != null)
            {
                if (obj["fncTypes"] is JArray ar)
                {
                    foreach (var oo in ar)
                    {
                        var ooo = oo as JObject;
                        if (ooo == null)
                            continue;

                        string name = "";
                        int fncType = -1;

                        if (ooo["name"] != null)
                            name = ooo["name"].ToString();
                        if (ooo["fnc"] != null)
                            fncType = (int) ooo["fnc"];

                        if (!string.IsNullOrEmpty(name) && fncType > 0)
                            _fncTypes.Add(name, (FncTypes) fncType);
                    }

                    OnPropertyChanged("FncNames");
                    OnPropertyChanged("FncTypeNames");
                }
            }

            if (obj["locimg"] != null)
                LocomotiveImageBase64 = obj["locimg"].ToString();

            if (obj["locthumbnail"] != null)
                LocomotiveThumbnailBase64 = obj["locthumbnail"].ToString();
        }
    }
}
