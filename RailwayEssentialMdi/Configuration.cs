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
namespace RailwayEssentialMdi
{
    using System;
    using RailwayEssentialCore;

    public class Configuration : Bases.ViewModelBase, IConfiguration
    {
        private string _ipAddress;
        private UInt16 _port;
        private int _designerColumns;
        private int _designerRows;

        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                RaisePropertyChanged("IpAddress");
            }
        }

        public UInt16 Port
        {
            get => _port;
            set
            {
                _port = value;
                RaisePropertyChanged("Port");
            }
        }

        public int DesignerColumns
        {
            get => _designerColumns;
            set
            {
                _designerColumns = value;
                RaisePropertyChanged("DesignerColumns");
            }
        }

        public int DesignerRows
        {
            get => _designerRows;
            set
            {
                _designerRows = value;
                RaisePropertyChanged("DesignerRows");
            }
        }

        public string ThemeName { get; set; }

        public Configuration()
        {
#if DEBUG
            IpAddress = "192.168.178.61";
#else
            IpAddress = "127.0.0.1";
#endif
            Port = 15471;
            DesignerColumns = 50;
            DesignerRows = 50;
            ThemeName = "RailwayEssential";
        }
    }
}
