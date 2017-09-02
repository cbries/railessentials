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

namespace TrackInformation
{
    public class Ecos2 : Item
    {
        public enum State
        {
            Stop,
            Go,
            Shutdown,
            Unknown
        }

        public string Name => "ECoS2";

        public const int Typeid = 2;

        public override int TypeId() { return Typeid; }

        private readonly string[] _fields = new string[4];

        public string ProtocolVersion
        {
            get => _fields[0];
            set
            {
                _fields[0] = value;
                OnPropertyChanged();
            }
        }

        public string ApplicationVersion
        {
            get => _fields[1];
            set
            {
                _fields[1] = value;
                OnPropertyChanged();
            }
        }

        public string HardwareVersion
        {
            get => _fields[2];
            set
            {
                _fields[2] = value;
                OnPropertyChanged();
            }

        }

        public string Status
        {
            get => _fields[3];
            set
            {
                _fields[3] = value;
                OnPropertyChanged();
            }

        }

        public State CurrentState
        {
            get
            {
                if(string.IsNullOrEmpty(Status))
                    return State.Unknown;

                if(Status.Equals("go", StringComparison.OrdinalIgnoreCase))
                    return State.Go;

                if(Status.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    return State.Stop;

                if(Status.Equals("shutdown", StringComparison.OrdinalIgnoreCase))
                    return State.Shutdown;

                return State.Unknown;
            }
        }

        public override void Parse(List<CommandArgument> arguments)
        {
            foreach (var arg in arguments)
            {
                if (arg == null)
                    continue;

                if (arg.Name.Equals("status", StringComparison.OrdinalIgnoreCase))
                    Status = arg.Parameter[0];
                else if (arg.Name.Equals("ProtocolVersion", StringComparison.OrdinalIgnoreCase))
                    ProtocolVersion = arg.Parameter[0];
                else if (arg.Name.Equals("ApplicationVersion", StringComparison.OrdinalIgnoreCase))
                    ApplicationVersion = arg.Parameter[0];
                else if (arg.Name.Equals("HardwareVersion", StringComparison.OrdinalIgnoreCase))
                    HardwareVersion = arg.Parameter[0];
            }

        }
    }
}
