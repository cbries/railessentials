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
            _mqtt?.Send("Haus/Switches/Off", string.Empty);
        }

        private void __resetRgbLight()
        {
            _mqtt?.Send("Haus/Railway/Sky/On", string.Empty);

            //var states = new JObject
            //{
            //    ["r"] = 255,
            //    ["g"] = 255,
            //    ["b"] = 255,
            //    ["w"] = 1023
            //};

            //var r = RelayCommandWebsocket.Send(TargetUriRgbStripes, states.ToString(Formatting.None));
            //if (!r.Result)
            //    SendDebug(DebugMessage.Instance($"{r.LastError}", DebugMessageLevel.Error));
        }
    }
}
