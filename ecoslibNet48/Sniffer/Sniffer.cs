// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Sniffer.cs

using System.Collections.Generic;
using ecoslib.Connector;
using ecoslib.Entities;
using ecoslib.Statistics;

namespace ecoslib.Sniffer
{
    public delegate void DataReceived(object sender, IDataProvider dataProvider, bool forceChange = false);
	
	public partial class Sniffer : IListener
	{
		public event DataReceived DataReceived;
		
		public ILogger Logger { get; set; }

        private readonly string _ipaddr;
		private readonly ushort _port;
		
		public bool IsSimulationMode { get; set; }
        public int InitializeDelay { get; set; }
		public bool SaveOnEveryPlanfieldChange { get; set; }
		public bool IsStarted { get; private set; }
		public IStatistics Statistics { get; set; }

        internal ConnectorFaster Connector { get; set; }
		internal ConnectorFaster S88Connector { get; set; }
		internal IDataProvider DataProvider { get; private set; }
		internal IDataProvider DataProviderS88 { get; private set; }

		public Sniffer(string ipaddr, ushort port)
		{
			_ipaddr = ipaddr;
			_port = port;
		}

		private ConnectorFaster GetConnector()
        {
            return Connector ??= new ConnectorFaster
            {
                IpAddress = _ipaddr,
                Port = _port,
                Logger = Logger
            };
        }

        private ConnectorFaster GetS88Connector()
        {
            return S88Connector ??= new ConnectorFaster
            {
                IpAddress = _ipaddr,
                Port = _port,
                Logger = Logger
            };
        }

		public IDataProvider GetDataProvider()
		{
            if (DataProvider != null) return DataProvider;

            DataProvider = new DataProvider(DataModeT.General)
            {
                Logger = Logger,
                Statistics = Statistics
            };
            var dp = (DataProvider)DataProvider;
            dp.Commands += DpOnCommands;
            dp.Modified += DpOnModified;
            return DataProvider;
		}

		public IDataProvider GetDataProviderS88()
		{
            if (DataProviderS88 != null) return DataProviderS88;

            DataProviderS88 = new DataProvider(DataModeT.S88)
            {
				Logger = Logger
			};
            var dp = (DataProvider)DataProviderS88;
            dp.Commands += DpOnCommandsS88;
            dp.Modified += DpOnModified;
            return DataProviderS88;
		}

		public void TriggerDataProviderModifiedForSimulation()
        {
            DataReceived?.Invoke(this, DataProvider, true);
            DataReceived?.Invoke(this, DataProviderS88, true);
        }

        private void DpOnModified(object sender)
		{
            DataReceived?.Invoke(this, sender as IDataProvider);
		}

		private void DpOnCommands(object sender, IReadOnlyList<ICommand> commands)
		{
			Connector?.SendCommands(commands);
		}

		private void DpOnCommandsS88(object sender, IReadOnlyList<ICommand> s88Commands)
		{
			S88Connector?.SendCommands(s88Commands);
		}

		public bool Start(bool simulate = false)
		{
            Logger?.Log?.Info("Sniffer started");

			if (simulate)
			{
				Logger?.Log?.Warn("*************** Simulation: no ECoS connection established");
				IsStarted = true;
				return true;
			}

			var c = GetConnector();
			c.Started += ConnectorOnStarted;
			c.Stopped += ConnectorOnStopped;
			c.Failed += ConnectorOnFailed;
			c.MessageReceived += ConnectorOnMessageReceived;

			var s88C = GetS88Connector();
			s88C.Started += S88COnStarted;
			s88C.Stopped += S88COnStopped;
			s88C.Failed += S88COnFailed;
			s88C.MessageReceived += S88COnMessageReceived;

			var r0 = c.Start();
			var r1 = s88C.Start();
            
            IsStarted = r0 && r1;

            return IsStarted;
		}

		public void Stop()
		{
			IsStarted = false;

			Connector?.Stop();
			Connector = null;

			S88Connector?.Stop();
			S88Connector = null;
		}
	}
}
