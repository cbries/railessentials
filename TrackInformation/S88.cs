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
using Ecos2Core;
using Newtonsoft.Json.Linq;

namespace TrackInformation
{
    public class S88 : Item
    {
        public const int Typeid = 4;

        public override int TypeId() { return Typeid; }

        #region Properties

        private int _index;

        /// <summary> the position within the S88 bus </summary>
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        private int _ports = 16;

        public int Ports
        {
            get => _ports;
            set
            {
                _ports = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        private string _stateOriginal;

        public string StateOriginal
        {
            get => _stateOriginal;
            set
            {
                _stateOriginal = value;
                OnPropertyChanged();
                OnPropertyChanged("Title");
            }
        }

        public string StateBinary => ToBinary(StateOriginal);

        private string ToBinary(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                string m = "";
                for (int i = 0; i < _ports; ++i)
                    m += "0";
                return m;
            }

            return Convert.ToString(Convert.ToInt64(hex, 16), 2).PadLeft(16, '0');
        }

        public override void UpdateTitle()
        {
            Title = $"{ObjectId} {Index}:{Ports} {StateBinary}";
            OnPropertyChanged("Title");
        }

        public override void UpdateSubTitle()
        {
            SubTitle = $"";
            OnPropertyChanged("SubTitle");
        }

        #endregion

        public override string ToString()
        {
            return $"{ObjectId} {Index}:{Ports} {StateBinary}";
        }

        public bool Pin(uint nr)
        {
            if (_ports < nr)
                return false;

            try
            {
                var p = StateBinary[_ports - (int) nr];

                if (p.Equals('0'))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Parse(List<CommandArgument> arguments)
        {
            foreach (var arg in arguments)
            {
                if (arg == null)
                    continue;

                if (arg.Name.Equals("ports", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(arg.Parameter[0], out v))
                        Ports = v;
                    else
                        Ports = -1;
                }
            }
        }

        public override JObject ToJson()
        {

            JObject o = new JObject
            {
                ["objectId"] = ObjectId,
                ["index"] = _index,
                ["ports"] = _ports,
                ["stateOriginal"] = _stateOriginal
            };
            return o;
        }

        public override void ParseJson(JObject o)
        {
            if (o == null)
                return;

            if (o["objectId"] != null)
                ObjectId = (int)o["objectId"];
            if (o["index"] != null)
                Index = (int) o["index"];
            if (o["ports"] != null)
                Ports = (int) o["ports"];
            if (o["stateOriginal"] != null)
                StateOriginal = o["stateOriginal"].ToString();
        }
    }
}
