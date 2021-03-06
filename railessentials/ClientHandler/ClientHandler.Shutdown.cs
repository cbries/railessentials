// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ClientHandler.Shutdown.cs

using ecoslib.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.RelayCommand;

namespace railessentials.ClientHandler
{
    public partial class ClientHandler
    {
        private const string TargetUriRgbStripes = "ws://192.168.178.66:81";
        private const string TargetUriSteckdosen = "ws://192.168.178.62:81";

        private void ShutdownHandler()
        {
            // hardcoded stuff
            __switchOffSteckdosen();
            __resetRgbLight();

            // shutdown the ECoS
            _sniffer?.SendCommandToEcos(CommandFactory.Create("set(1, shutdown)"));
        }

        private void __switchOffSteckdosen()
        {
            var states = new JObject
            {
                ["in1"] = false,
                ["in2"] = false,
                ["in3"] = false,
                ["in4"] = false
            };

            var r = RelayCommandWebsocket.Send(TargetUriSteckdosen, states.ToString(Formatting.None));
            if(!r.Result)
                SendDebug(DebugMessage.Instance($"{r.LastError}", DebugMessageLevel.Error));
        }

        private void __resetRgbLight()
        {
            var states = new JObject
            {
                ["r"] = 255,
                ["g"] = 255,
                ["b"] = 255,
                ["w"] = 1023
            };

            var r = RelayCommandWebsocket.Send(TargetUriRgbStripes, states.ToString(Formatting.None));
            if (!r.Result)
                SendDebug(DebugMessage.Instance($"{r.LastError}", DebugMessageLevel.Error));
        }
    }
}
