using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using railessentials.Plan;
using railessentials.Theming;

namespace TrackPlanerTest
{
    [TestClass]
    public class TestSerializations
    {

        [TestMethod]
        public void TestSerializationPlanField()
        {
            var path = @"..\..\..\..\rocrail2ecosApp\Demos\metamodel.RocrailDemo.json";
            File.Exists(path).Should().BeTrue();

            var json = File.ReadAllText(path, Encoding.UTF8);
            var planfield = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, PlanItem>>>(json);
            planfield.Count.Should().BeGreaterOrEqualTo(1);
        }

        [TestMethod]
        public void TestDeserializationOfTheme()
        {
            var path = @"..\..\..\..\EcosApp\theme\RailwayEssential.json";
            File.Exists(path).Should().BeTrue();

            var json = File.ReadAllText(path, Encoding.UTF8);
            json = json.Replace("const themeObject =", string.Empty);
            var itm = JsonConvert.DeserializeObject<List<ThemeCategory>>(json);
            itm.Count.Should().Be(6);
        }
    }
}
