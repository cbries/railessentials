using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Newtonsoft.Json;
using railessentials.Plan;

namespace UtilitiesTest
{
    [TestClass]
    public class UnitAddressUtilities
    {
        [TestMethod]
        public void TestDccEcosAddressing1()
        {
            var dccAddr = 16;
            var dccPort = 1;
            var ecosAddr = 61;

            Utilities.AddressUtilities.GetEcosAddress(dccAddr, dccPort).Should().Be(ecosAddr);
            Utilities.AddressUtilities.GetDccAddr(ecosAddr).Should().Be(dccAddr);
            Utilities.AddressUtilities.GetDccPort(ecosAddr).Should().Be(dccPort);
        }

        [TestMethod]
        public void TestDccEcosAddressing2()
        {
            var dccAddr = 9;
            var dccPort = 4;
            var ecosAddr = 36;

            Utilities.AddressUtilities.GetEcosAddress(dccAddr, dccPort).Should().Be(ecosAddr);
            Utilities.AddressUtilities.GetDccAddr(ecosAddr).Should().Be(dccAddr);
            Utilities.AddressUtilities.GetDccPort(ecosAddr).Should().Be(dccPort);
        }

        [TestMethod]
        public void TestEcosDoubleAddresses()
        {
            var json = @"{""identifier"": ""Signal_B12_rechts"",
      ""name"": ""Main Signal A"",
      ""basename"": ""semaphoremain-r"",
      ""clickable"": true,
      ""routes"": [
        ""AC"",
        ""BD"",
        ""AC"",
        ""BD""
      ],
      ""states"": {},
      ""coord"": {
        ""x"": 23,
        ""y"": 15
      },
      ""addresses"": {
        ""Addr"": 0,
        ""Addr1"": 21,
        ""Port1"": 1,
        ""Inverse1"": true,
        ""Addr2"": 0,
        ""Port2"": 0,
        ""Inverse2"": false
      },
      ""editor"": {
        ""themeId"": 101,
        ""offsetX"": 0,
        ""offsetY"": 0,
        ""themeDimIdx"": 2
      }}";
            var planItemInstance = JsonConvert.DeserializeObject<PlanItem>(json);
            railessentials.Utilities.GetAccessoryEcosAddresses(planItemInstance, out var addr1, out var addr2);
            addr1.Should().Be(81);
            addr2.Should().Be(0);
        }
    }
}
