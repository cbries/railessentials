using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

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
    }
}
