using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using railessentials.LocomotivesDuration;

namespace TrackPlanerTest
{
    [TestClass]
    public class TestDurations
    {
        [TestMethod]
        public void TestAddDuration()
        {
            var instance = new DurationsData(null);
            var data1004 = instance.GetData(1004);
            data1004.Should().BeNull();
            instance.AddDecelerateDuration(1004, "B01[+]", DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(15)).Should().BeTrue();
            instance.AddDuration(1004, DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(10)).Should().BeTrue();
            var jsonObj = instance.ToJsonString();
            jsonObj.IndexOf("B01[+]", StringComparison.OrdinalIgnoreCase).Should().BeGreaterThan(-1);
        }

        [TestMethod]
        public void TestDecelerationMax()
        {
            var instance = new DurationsData(null);
            var data1004 = instance.GetData(1004);
            data1004.Should().BeNull();
            const string blockName = "B01[+]";
            for(var i = 0; i < 100; ++i)
                instance.AddDecelerateDuration(1004, blockName, DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(15)).Should().BeTrue();
            instance.Entries.Count.Should().Be(1);
            instance.GetNoOfDecelerationsEntries(1004, blockName).Should().Be(Data.MaxDecelerateEntries);
            instance.CleanupDeceleration(1004, blockName).Should().BeTrue();
            instance.GetNoOfDecelerationsEntries(1004, blockName).Should().Be(0);
            instance.Remove(1004).Should().BeTrue();
            instance.Entries.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestDecelerationAverage()
        {
            var instance = new DurationsData(null);
            var data1004 = instance.GetData(1004);
            data1004.Should().BeNull();
            const string blockName = "B01[+]";
            for (var i = 0; i < 100; ++i)
                instance.AddDecelerateDuration(1004, blockName, DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(15)).Should().BeTrue();
            var avr = instance.GetAverageDecelerationTime(1004, blockName);
            avr.Should().BeInRange((15 - 0.001), 15 + 0.001);

            instance.GetAverageDecelerationTime(1000, "", 1337).Should().Be(1337);
            instance.GetAverageDecelerationTime(1004, "", 1337).Should().Be(1337);
            instance.GetAverageDecelerationTime(1004, "--", 1337).Should().Be(1337);
        }
    }
}
