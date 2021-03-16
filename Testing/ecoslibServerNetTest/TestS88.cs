using System.Collections.Generic;
using System.IO;
using System.Text;
using ecoslib.Entities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ecoslibServerNetTest
{
    [TestClass]
    public class TestS88
    {
        internal static JObject ReadJsonObject(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!File.Exists(path)) return null;
            try
            {
                var cnt = File.ReadAllText(path, Encoding.UTF8);
                return JObject.Parse(cnt);
            }
            catch
            {
                // ignore
            }
            return null;
        }

        [TestMethod]
        [DeploymentItem("Testfiles", "Testfiles")]
        public void TestPortPin()
        {
            var ecosData = ReadJsonObject(@"Testfiles\ecosJson.txt");
            var dp = new DataProvider(DataModeT.S88);
            dp.Parse(ecosData).Should().BeTrue();
            dp.Objects.Count.Should().Be(6);

            var ecosAddressesToTest = new Dictionary<int, bool>()
            {
                {1, false},
                {47, true},
                {48, true}
            };
            
            foreach(var it in ecosAddressesToTest)
            {
                var ecosAddr = it.Key;
                var targetState = it.Value;

                var res = dp.GetFeedbackByAddress(
                    ecosAddr,
                    out S88 item,
                    out int itemOffset,
                    out int itemPin);
                res.Should().BeTrue();
                item.Pin((uint)itemPin).Should().Be(targetState);
            }
        }
    }
}
