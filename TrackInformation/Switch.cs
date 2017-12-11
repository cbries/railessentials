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
using Ecos2Core;
using Newtonsoft.Json.Linq;

namespace TrackInformation
{
    public class Switch : Item
    {
        public const int Typeid = 5;

        public override int TypeId() { return Typeid; }

        public override int SubTitleHeight => 16;

        private readonly string[] _names = new string[3];

        public string Name1
        {
            get => _names[0];
            set
            {
                _names[0] = value;
                OnPropertyChanged();
            }
        }

        public string Name2
        {
            get => _names[1];
            set
            {
                _names[1] = value;
                OnPropertyChanged();
            }
        }

        public string Name3
        {
            get => _names[2];
            set
            {
                _names[2] = value;
                OnPropertyChanged();
            }
        }

        public bool InvertCommand { get; set; }

        private List<string> _addrext = new List<string>();

        public List<string> Addrext
        {
            get => _addrext;
            set
            {
                _addrext = value;
                OnPropertyChanged();
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
            }
        }

        private string _protocol;

        public string Protocol
        {
            get => _protocol;
            set
            {
                _protocol = value;
                OnPropertyChanged();
            }
        }

        private string _type;

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        private string _mode;

        public string Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                OnPropertyChanged();
            }
        }

        private int _state;

        public int State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged();
            }
        }

        private int _switching;

        public int Switching
        {
            get => _switching;
            set
            {
                _switching = value;
                OnPropertyChanged();
            }
        }

        public Switch() : base()
        {
        }

        public override void UpdateTitle()
        {
            if(!string.IsNullOrEmpty(Name1) && !string.IsNullOrEmpty(Name2) && !string.IsNullOrEmpty(Name3))
                Title = $"{Name1}, {Name2}, {Name3}";
            else if (!string.IsNullOrEmpty(Name1) && !string.IsNullOrEmpty(Name2))
                Title = $"{Name1}, {Name2}";
            else
                Title = $"{Name1}";
        }

        public override void UpdateSubTitle()
        {
            var ext = string.Join(", ", Addrext);
            var direction = State == 0 ? "Straight" : "Turn";

            SubTitle = $"{direction} - [{ext}]";
        }

        public override string ToString()
        {
            var ext = string.Join(", ", Addrext);

            return $"{Name1} ({ext}, {ObjectId})";
        }

        public override void Parse(List<CommandArgument> arguments)
        {
            foreach (var arg in arguments)
            {
                if (arg == null)
                    continue;

                if (arg.Name.Equals("name1", StringComparison.OrdinalIgnoreCase))
                    Name1 = arg.Parameter[0];
                else if (arg.Name.Equals("name2", StringComparison.OrdinalIgnoreCase))
                    Name2 = arg.Parameter[0];
                else if (arg.Name.Equals("name3", StringComparison.OrdinalIgnoreCase))
                    Name3 = arg.Parameter[0];
                else if (arg.Name.Equals("addrext", StringComparison.OrdinalIgnoreCase))
                    Addrext = arg.Parameter;
                else if (arg.Name.Equals("addr", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(arg.Parameter[0], out v))
                        Addr = v;
                    else
                        Addr = -1;
                }
                else if (arg.Name.Equals("protocol", StringComparison.OrdinalIgnoreCase))
                    Protocol = arg.Parameter[0];
                else if (arg.Name.Equals("type", StringComparison.OrdinalIgnoreCase))
                    Type = arg.Parameter[0];
                else if (arg.Name.Equals("mode", StringComparison.OrdinalIgnoreCase))
                    Mode = arg.Parameter[0];
                else if (arg.Name.Equals("state", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(arg.Parameter[0], out v))
                        State = v;
                    else
                        State = -1;
                }
                else if (arg.Name.Equals("switching", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(arg.Parameter[0], out v))
                        Switching = v;
                    else
                        Switching = -1;
                }
                else if (arg.Name.Equals("symbol", StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine($"Handled, but purpose is unknown for: {arg.Name} -> {arg.Parameter[0]}");
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
            JObject o = new JObject
            {
                ["name1"] = Name1,
                ["name2"] = Name2,
                ["name3"] = Name3
            };
            JArray a0 = new JArray();
            foreach (var e in Addrext)
                a0.Add(e);
            o["objectId"] = ObjectId;
            o["addrext"] = a0;
            o["addr"] = Addr;
            o["protocol"] = Protocol;
            o["type"] = Type;
            o["mode"] = Mode;
            o["state"] = State;
            o["switching"] = Switching;
            return o;
        }

        public override void ParseJson(JObject o)
        {
            if (o == null)
                return;

            if (o["name1"] != null)
                Name1 = o["name1"].ToString();
            if (o["name2"] != null)
                Name2 = o["name2"].ToString();
            if (o["name3"] != null)
                Name3 = o["name3"].ToString();
            if (o["addrext"] != null)
            {
                JArray a = o["addrext"] as JArray;
                if (a != null)
                {
                    foreach (var e in a)
                        Addrext.Add(e.ToString());
                }
            }
            if (o["objectId"] != null)
                ObjectId = (int) o["objectId"];
            if (o["addr"] != null)
                Addr = (int) o["addr"];
            if (o["protocol"] != null)
                Protocol = o["protocol"].ToString();
            if (o["type"] != null)
                Type = o["type"].ToString();
            if (o["mode"] != null)
                Mode = o["mode"].ToString();
            if (o["state"] != null)
                State = (int) o["state"];
            if (o["switching"] != null)
                Switching = (int) o["switching"];
        }

        public void ChangeDirection(int index)
        {
            string s = Addrext[index];

            List<ICommand> ctrlCmds = new List<ICommand>
            {
                CommandFactory.Create($"request(11, control, force)"),
                CommandFactory.Create($"set(11, switch[{Protocol}{s}])"),
                CommandFactory.Create($"release(11, control)")
            };

            OnCommandsReady(this, ctrlCmds);

            // get update
            ctrlCmds.Clear();

            ctrlCmds = new List<ICommand>
            {
                CommandFactory.Create($"get({ObjectId}, state)"),
            };

            OnCommandsReady(this, ctrlCmds);
        }
    }
}
