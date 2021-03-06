// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Sniffer.Connector.cs

using System;
using System.Collections.Generic;
using ecoslib.Connector;
using ecoslib.Utilities;

namespace ecoslib.Sniffer
{
    public partial class Sniffer
    {
        public void SendCommandsToEcosStation()
        {
			SendCommandsOfObjects();
        }

		private void HandleBlocks(IEnumerable<IBlock> blocks)
	    {
		    foreach (var block in blocks)
		    {
			    if (block == null) continue;
			    if (string.IsNullOrEmpty(block.NativeBlock)) continue;
			    GetDataProvider().HandleData(block);
		    }
			SendCommandsOfObjects();
			DataReceived?.Invoke(this, GetDataProvider());
	    }

	    private void HandleBlocksS88(IEnumerable<IBlock> blocks)
	    {
		    foreach (var block in blocks)
		    {
			    if (block == null) continue;
			    if (string.IsNullOrEmpty(block.NativeBlock)) continue;
			    GetDataProviderS88().HandleData(block);
		    }
		    SendCommandsOfObjectsS88();
            DataReceived?.Invoke(this, GetDataProviderS88());
	    }

		public void SendCommandToEcos(ICommand cmd)
        {

        }

		private void SendCommandsOfObjects()
	    {
		    var dp = GetDataProvider();
		    var objs = dp.Objects;
		    var con = GetConnector();
		    if (con == null) return;

		    lock (objs)
		    {
			    foreach (var o in objs)
			    {
				    if (o == null) continue;

					var cmds = o.GetCommands();
                    var n = cmds.Count;
					if (cmds.Count == 0) continue;
					
                    for (var i = 0; i < n; ++i)
					{
						var cmd = cmds[i];
						con.SendCommand(cmd);
                    }

					o.ResetCmds();
                }
			}
	    }

	    private void SendCommandsOfObjectsS88()
	    {
		    var dp = GetDataProviderS88();
		    var objs = dp.Objects;
		    var con = GetS88Connector();
		    if (con == null) return;

		    lock (objs)
		    {
                foreach (var o in objs)
                {
                    if (o == null) continue;

                    var cmds = o.GetCommands();
                    var n = cmds.Count;
                    if (cmds.Count == 0) continue;

                    for (var i = 0; i < n; ++i)
                    {
                        var cmd = cmds[i];
                        con.SendCommand(cmd);
                    }

                    o.ResetCmds();
                }
			}
	    }

		private void ConnectorOnFailed(object sender, MessageEventArgs ev)
        {
            Stop(); 
            Logger?.Log?.Error($"<Sniffer> {ev.Message}", ev.Exception);
        }

        private void ConnectorOnStopped(object sender, EventArgs ev)
        {
            Stop();
            Logger?.Log?.Info("<Sniffer> ECoS connection stopped");
        }

        private void ConnectorOnStarted(object sender, EventArgs ev)
        {
            Logger?.Log?.Info("<Sniffer> ECoS connection established");

			var initialCommands = new List<ICommand>
            {
                CommandFactory.Create($"request({Globals.ID_EV_BASEOBJECT}, view)"),
                CommandFactory.Create($"get({Globals.ID_EV_BASEOBJECT}, info, status)"),
                CommandFactory.Create($"request(5, view)"),
                CommandFactory.Create($"request({Globals.ID_EV_LOCOMOTIVES}, view)"),
                CommandFactory.Create($"request({Globals.ID_EV_ACCESSORIES}, view)"), // viewswitch
                CommandFactory.Create($"queryObjects({Globals.ID_EV_ACCESSORIES}, addr, protocol, type, addrext, mode, symbol, name1, name2, name3)"),
                CommandFactory.Create($"queryObjects({Globals.ID_EV_LOCOMOTIVES}, addr, name, protocol)")
            };

            Connector.SendCommands(initialCommands);
        }

	    private readonly List<string> _lines = new();

		private void ConnectorOnMessageReceived(object sender, MessageEventArgs ev)
		{
#if DEBUG
			var sw = new StopWatch();
			sw.Start();
#endif
			if (string.IsNullOrEmpty(ev.Message)) return;

	        if (BlockUtils.HasAnyBlock(ev.Message))
            {
                _lines.Clear();
                _lines.AddRange(ev.Message.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                _lines.Add(ev.Message.Trim());
            }

            if (BlockUtils.HasAnyBlock(_lines))
            {
                var blocks = BlockUtils.GetBlocks(_lines);
                HandleBlocks(blocks);
                _lines.Clear();
            }
#if DEBUG
			sw.Stop();
#endif
		}
		
	    private void S88COnFailed(object sender, MessageEventArgs ev)
	    {
		    Stop();
            Logger?.Log?.Error($"{ev.Message}");
	    }

	    private void S88COnStopped(object sender, EventArgs e)
	    {
		    Stop();
            Logger?.Log?.Info("<Sniffer> ECoS connection for S88 stopped");
	    }

		private void S88COnStarted(object sender, EventArgs e)
	    {
            Logger?.Log?.Info("<Sniffer> ECoS connection for S88 established");

		    var initialCommands = new List<ICommand>
		    {
				CommandFactory.Create($"queryObjects({Globals.ID_EV_S88}, ports)"),
			    CommandFactory.Create($"request({Globals.ID_EV_S88}, view)")
		    };

		    S88Connector.SendCommands(initialCommands);
		}

	    private readonly List<string> _s88Lines = new();

		private void S88COnMessageReceived(object sender, MessageEventArgs ev)
		{
		    if (string.IsNullOrEmpty(ev.Message)) return;

		    if (BlockUtils.HasAnyBlock(ev.Message))
		    {
			    _s88Lines.Clear();
			    _s88Lines.AddRange(ev.Message.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries));
		    }
		    else
		    {
			    _s88Lines.Add(ev.Message.Trim());
		    }

		    if (BlockUtils.HasAnyBlock(_s88Lines))
		    {
			    var blocks = BlockUtils.GetBlocks(_s88Lines);
			    HandleBlocksS88(blocks);
			    _s88Lines.Clear();
		    }
		}
	}
}
