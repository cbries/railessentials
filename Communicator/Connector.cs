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
using System.Threading;
using System.Threading.Tasks;
using Ecos2Core;
using PrimS.Telnet;
using RailwayEssentialCore;
using ThreadState = System.Threading.ThreadState;

namespace Communicator
{
    public delegate void StartedDelegator(object sender);
    public delegate void FailedDelegator(object sender, string message);
    public delegate void StoppedDelegator(object sender);
    public delegate void MessageReceivedDelegator(object sender, string msg);
    
    public class Connector
    {
        public event StartedDelegator Started;
        public event FailedDelegator Failed;
        public event StoppedDelegator Stopped;
        public event MessageReceivedDelegator MessageReceived;

        public ILogging Logger { get; set; }

        public IConfiguration Cfg { get; set; }

        private string IpAddr { get { return Cfg.IpAddress; } }
        private UInt16 Port { get { return Cfg.Port; } }

        private bool _run = false;
        private Thread _thread = null;
        private Client _clientConnection;

        public async Task<bool> SendMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return false;

            if (_clientConnection == null)
                return false;

            if (_clientConnection.IsConnected)
                await _clientConnection.WriteLine(msg.Trim());

            return false;
        }

        public bool Start()
        {
            if (_thread != null && _thread.IsAlive)
                return true;

            _thread = new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                await StartHandler();
            });

            _thread.Start();

            _run = _thread.ThreadState == ThreadState.Running || _thread.ThreadState == ThreadState.Background;

            if(_run)
                return true;

            try
            {
                _thread.Abort(null);
                _thread = null;
            }
            catch
            {
                // ignore
            }

            return false;
        }

        public bool Stop()
        { 
            _run = false;

            return true;
        }

        private async Task StartHandler()
        {
            string ipaddr = IpAddr;
            int port = Port;

            try
            {
                using (_clientConnection = new Client(new TcpByteStream(ipaddr, port), new CancellationToken()))
                {
                    if (_clientConnection.IsConnected)
                    {
                        if (Logger != null)
                            Logger.Log("<Connector> Connection established\r\n");

                        if (Started != null)
                            Started(this);
                    }
                    else
                    {
                        if (Logger != null)
                            Logger.LogError("<Connector> Connection failed\r\n");

                        if (Failed != null)
                            Failed(this, "Connection failed");
                    }

                    while (_run)
                    {
                        var msg = await _clientConnection.TerminatedReadAsync("\r\n", TimeSpan.FromMilliseconds(2500));

                        if (!string.IsNullOrEmpty(msg))
                        {
                            if (Logger != null)
                                Logger.Log($"<Connector> Message received, Length: {msg.Length}\r\n");

                            if (MessageReceived != null)
                                MessageReceived(this, msg);
                        }
                    }

                    if (Stopped != null)
                        Stopped(this);
                }
            }
            catch (Exception ex)
            {
                if (Failed != null)
                    Failed(this, ex.Message);
            }
        }
    }
}
