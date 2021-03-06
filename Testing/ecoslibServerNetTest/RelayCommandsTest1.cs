using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.RelayCommand;

namespace ecoslibServerNetTest
{
    [TestClass]
    public class RelayCommandsTest1
    {
        private const string TargetUri = "ws://127.0.0.1:8989";

        [TestMethod]
        public void TestRelayWebsocket()
        {
            // NOTE
            // need running "Infrastructure\wsserver.exe" instance

            var states = new JObject {
                ["in1"] = false,
                ["in2"] = true,
                ["in3"] = false,
                ["in4"] = true
            };

            RelayCommandWebsocket.Send(TargetUri, states.ToString(Formatting.None));
        }

        [TestMethod]
        public void TestRelayWebsocketDelayed()
        {
            // NOTE
            // need running "Infrastructure\wsserver.exe" instance

            var states = new JObject
            {
                ["in1"] = false,
                ["in2"] = true,
                ["in3"] = false,
                ["in4"] = true
            };

            Console.WriteLine("Wait few seconds before execute: " + DateTime.Now);
            var task = RelayCommandWebsocket
                .SendDelayed(TargetUri, states.ToString(Formatting.None), 5);
            Console.WriteLine("Executed: " + DateTime.Now);
            task.Wait(TimeSpan.FromSeconds(10));
        }
    }
}
