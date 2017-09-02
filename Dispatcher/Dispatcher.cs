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
using System.Threading.Tasks;
using Ecos2Core;
using RailwayEssentialCore;
using TrackInformation;
using TrackWeaver;
using Switch = TrackInformation.Switch;

namespace Dispatcher
{
    public class Dispatcher : IDispatcher
    {
        public event UpdateUiDelegate UpdateUi;
        public event EventHandler ReadyToPlay;

        public IConfiguration Configuration { get; set; }
        public ILogging Logger { get; set; }
        public IRailwayEssentialModel Model { get; set; }
        private readonly Communication _communication = null;
        private readonly DataProvider _dataProvider = new DataProvider();
        private TrackWeaver.TrackWeaver _trackWeaver;
        private TrackPlanParser.Track _track;

        public TrackWeaver.TrackWeaver Weaver => _trackWeaver;

        public Communication Communication => _communication;

        public DataProvider GetDataProvider()
        {
            return _dataProvider;
        }
        
        public Dispatcher(IConfiguration cfg)
        {
            Configuration = cfg;

            _communication = new Communication(Configuration);
            _communication.CommunicationStarted += CommunicationOnCommunicationStarted;
            _communication.CommunicationStopped += CommunicationOnCommunicationStopped;
            _communication.CommunicationFailed += CommunicationOnCommunicationFailed;
            _communication.BlocksReceived += CommunicationOnBlocksReceived;
        }

        public bool InitializeWeaving(TrackPlanParser.Track track, string weaveFilepath)
        {
            if(_track == null)
                _track = track;

            if (_trackWeaver == null)
                _trackWeaver = new TrackWeaver.TrackWeaver();
            else
            {
                _trackWeaver.WovenSeam.Clear();
            }
            
            TrackWeaveItems weaverItems = new TrackWeaveItems();
            if (!weaverItems.Load(weaveFilepath))
                return false;

            foreach (var item in weaverItems.Items)
            {
                if (item == null)
                    continue;

                switch (item.Type)
                {
                    case WeaveItemT.S88:
                    {
                        var s88Item = _dataProvider.GetObjectBy(item.ObjectId) as S88;

                        if (s88Item != null)
                        {
                            var trackObject = _track.Get(item.VisuX, item.VisuY);

                            if (trackObject != null)
                            {
                                _trackWeaver.Link(s88Item, trackObject,
                                    () =>
                                    {
                                        var res = s88Item.Pin((uint) item.Pin);
                                        return new TrackCheckResult {State = res};
                                    });
                                }
                        }
                    }
                        break;

                    case WeaveItemT.Switch:
                    {
                        var switchItem = _dataProvider.GetObjectBy(item.ObjectId) as Switch;

                        if (switchItem != null)
                        {
                            var trackObject = _track.Get(item.VisuX, item.VisuY);

                            switchItem.InvertCommand = item.InvertSwitch;

                            _trackWeaver.Link(switchItem, trackObject,
                                () => {

                                    TrackCheckResult.SwitchDirection direction = TrackCheckResult.SwitchDirection.Straight;

                                    if (switchItem.State == 0)
                                        direction = TrackCheckResult.SwitchDirection.Straight;
                                    else
                                        direction = TrackCheckResult.SwitchDirection.Turn;

                                    return new TrackCheckResult { Direction = direction };
                                });
                        }
                    }
                        break;

                    // TODO add other item weaves :-D
                }
            }
            
            return true;
        }

        public async Task ForwardCommands(IReadOnlyList<ICommand> commands)
        {
            if (!_communication.IsConnected)
            {
                if (Logger != null)
                {
                    foreach(var cmd in commands)
                        Logger.Log($"<DryRun> {cmd}\r\n");
                }
            }
            else
            {
                await _communication.SendCommands(commands);
            }
        }

        public async void SetRunMode(bool state)
        {
            if (state)
            {
                try
                {
                    _communication.Cfg = Configuration;
                    _communication.Logger = Logger;
                    _communication.Start();
                }
                catch (Exception ex)
                {
                    if(Logger != null)
                        Logger.LogNetwork("<Dispatcher> {0}", ex.Message);
                }
            }
            else
            {
                await UnloadViews();

                _communication.Shutdown();
            }
        }

        public bool GetRunMode()
        {
            if (_communication == null)
                return false;

            return _communication.IsConnected;
        }

        private async Task UnloadViews()
        {
            if (_dataProvider != null)
            {
                foreach (var data in _dataProvider.Objects)
                    data?.DisableView();
            }

            List<ICommand> initialCommands = new List<ICommand>
            {
                CommandFactory.Create("release(1, view)"),
                CommandFactory.Create("release(26, view)"),
                CommandFactory.Create("release(5, view)"),
                CommandFactory.Create("release(100, view)"),
                CommandFactory.Create("release(10, view)"),
                CommandFactory.Create("release(11, view)"),
            };

            await _communication.SendCommands(initialCommands);
        }

        private void CommunicationOnCommunicationStarted(object sender, EventArgs args)
        {
            // send initial commands
            List<ICommand> initialCommands = new List<ICommand>()
            {
                CommandFactory.Create("request(26, view)"),
                CommandFactory.Create("request(5, view)"),
                CommandFactory.Create("request(10, view)"),
                CommandFactory.Create("request(11, view, viewswitch)"),
                CommandFactory.Create("queryObjects(11, addr, protocol, type, addrext, mode, symbol, name1, name2, name3)"),
                CommandFactory.Create("queryObjects(10, addr, name, protocol)"),
                CommandFactory.Create("queryObjects(26, ports)"),
                CommandFactory.Create("get(1, info, status)"),
                CommandFactory.Create("request(1, view)")
            };

            _communication.SendCommands(initialCommands);

            if (Model != null)
            {
                Model.TriggerPropertyChanged("ConnectionState");
                Model.TriggerPropertyChanged("ConnectionStateIcon");
                Model.TriggerPropertyChanged("TogglePowerCaption");
            }

            if(ReadyToPlay != null)
                ReadyToPlay(this, EventArgs.Empty);
        }

        private void CommunicationOnCommunicationStopped(object sender, EventArgs eventArgs)
        {
            if (Model != null)
            {
                Model.TriggerPropertyChanged("ConnectionState");
                Model.TriggerPropertyChanged("ConnectionStateIcon");
                Model.TriggerPropertyChanged("TogglePowerCaption");
            }
        }

        private void CommunicationOnCommunicationFailed(object sender, EventArgs eventArgs)
        {
            var c = sender as Communication;
            if (c != null && c.HasError)
            {
                Logger?.Log($"<Dispatcher> Communication failed: {c.ErrorMessage}\r\n");
                Logger?.LogError($"Connection to ECoS2 failed: {c.ErrorMessage}");
            }
            
            if (Model != null)
            {
                Model.TriggerPropertyChanged("ConnectionState");
                Model.TriggerPropertyChanged("ConnectionStateIcon");
            }
        }

        private void CommunicationOnBlocksReceived(object sender, IReadOnlyList<IBlock> blocks)
        {
            if (Logger != null)
                Logger.Log($"<Dispatcher> Blocks received: {blocks.Count}\r\n");

            foreach (var blk in blocks)
            {
                if (blk == null)
                    continue;

                if (blk.StartLine.IndexOf("EVENT", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    if(Logger != null)
                        Logger.LogNetwork(blk.NativeBlock.Trim() + "\r\n");
                }

                if(_dataProvider != null)
                    _dataProvider.Add(blk);
            }

            if (UpdateUi != null)
                UpdateUi(this, _trackWeaver);
        }
    }
}
