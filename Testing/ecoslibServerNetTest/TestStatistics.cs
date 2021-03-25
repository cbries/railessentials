using System;
using System.IO;
using ecoslib.Statistics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ecoslibServerNetTest
{
    [TestClass]
    public class TestStatistics
    {
        [TestMethod]
        [DeploymentItem("Testfiles", "Testfiles")]
        public void TestSaveStats()
        {
            var path = @"Testfiles\statistics.json";
            Directory.CreateDirectory("Testfiles");
            var statInstance = Statistics.Instance(path, null);
            statInstance.GetData(0).Should().BeNull();
            statInstance.Save().Should().BeTrue();
            statInstance.AddDuration(1, DateTime.Now - TimeSpan.FromSeconds(30), DateTime.Now);
            statInstance.Save().Should().BeTrue();
        }
    }
}
