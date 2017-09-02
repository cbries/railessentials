using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TrackPlanParserTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string testfile0 = @"C:\Users\ChristianRi\Desktop\Github\modeling\RailwayEssential\TrackPlanParserTest\Testfiles\Schattenbahnhof-unten.track";
            TrackPlanParser.TrackPlanParser parser = new TrackPlanParser.TrackPlanParser(testfile0);
            parser.Parse().Should().BeTrue();
        }
    }
}
