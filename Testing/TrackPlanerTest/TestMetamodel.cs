using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using railessentials.Analyzer;
using railessentials.Plan;
using Path = railessentials.Plan.Path;

namespace TrackPlanerTest
{
    [TestClass]
    public class TestMetamodel
    {
        public static string WorkspaceRoot = @"..\..\..\..\..\Workspaces\";
        public static string ThemeRoot = @"..\..\..\..\..\EcosApp\theme\";

        private PlanField LoadPlanFieldFile(string path)
        {
            File.Exists(path).Should().BeTrue();
            var json = File.ReadAllText(path, Encoding.UTF8);
            var field = LoadPlanField(json);
            field.Should().NotBeNull();
            return field;
        }

        private PlanField LoadPlanField(string json)
        {
            var planfield = JsonConvert.DeserializeObject<Dictionary<string, PlanField>>(json);
            planfield.Count.Should().BeGreaterOrEqualTo(1);
            var field = planfield["planField"];
            field.Should().NotBeNull();
            field.InitContext();
            return field;
        }

        [TestMethod]
        public void TestCheckDimensionBlock()
        {
            var json = @"
{
  'planField': {
    '7x1': {
      'id': 150,
      'name': 'Block',
      'identifier': 'BK_283',
      'clickable': false,
      'routes': ['AC,CA','BD,DB'],
      'dimensions': [{ 'w': 4, 'h': 1 }, { 'w': 1, 'h': 4 } ],
      'coord': { 'x': 7, 'y': 1 },
      'editor': {
        'themeId': 150,
        'offsetX': 0,
        'offsetY': 0,
        'themeDimIdx': 1
      }
    }
  }
}
";
            var field = LoadPlanField(json);

            var bk = field["7x1"];
            bk.Should().NotBeNull();

            bk.Width().Should().Be(1);
            bk.Height().Should().Be(4);

            bk.StartCoord().x.Should().Be(7);
            bk.StartCoord().y.Should().Be(1);

            bk.EndCoord().x.Should().Be(7);
            bk.EndCoord().y.Should().Be(4);
        }

        [TestMethod]
        public void TestCheckDimensionTrack()
        {
            var json = @"
{
  'planField': {
    '7x1': {
      'id': 54,
      'name': 'Crossing A',
      'identifier': 'TK_0',
      'clickable': false,
      'routes': ['AC,CA,BD,DB,AB,BA,CD,DC','AC,CA,BD,DB,BC,CB,DA,AD'],
      'coord': { 'x': 7, 'y': 1 },
      'editor': {
        'themeId': 150,
        'offsetX': 0,
        'offsetY': 0,
        'themeDimIdx': 1
      }
    }
  }
}
";
            var field = LoadPlanField(json);

            var bk = field["7x1"];
            bk.Should().NotBeNull();

            bk.Width().Should().Be(1);
            bk.Height().Should().Be(1);

            bk.StartCoord().x.Should().Be(7);
            bk.StartCoord().y.Should().Be(1);

            bk.EndCoord().x.Should().Be(7);
            bk.EndCoord().y.Should().Be(1);
        }

        [TestMethod]
        public void TestSerializationPlanField()
        {
            var json = @" {
  'planField': {
    '14x7': {
      'id': 10,
      'name': 'Straigth',
      'identifier': 'TE_1',
      'routes': ['AC', 'BD'],
      'coord': {
        'x': 14,
        'y': 7
      },
      'editor': {
        'themeId': 10,
        'offsetX': 0,
        'offsetY': 0,
        'themeDimIdx': 2
      }
    },
    '15x7': {
      'id': 10,
      'name': 'Straigth',
      'identifier': 'TE_2',
      'routes': ['AC','BD'],
      'coord': {
        'x': 15,
        'y': 7
      },
      'editor': {
        'themeId': 10,
        'offsetX': 0,
        'offsetY': 0,
        'themeDimIdx': 3
      }
    }
  }
}
";

            var field = LoadPlanField(json);

            var te1 = field["14x7"];
            te1.Should().NotBeNull();
            te1.GetThemeDimensionIndex().Should().Be(0);
            te1.routes.Count.Should().Be(2);
            te1.identifier.Should().Be("TE_1");

            var te2 = field["15x7"];
            te2.Should().NotBeNull();
            te2.GetThemeDimensionIndex().Should().Be(1);
            te2.routes.Count.Should().Be(2);
            te2.identifier.Should().Be("TE_2");

            te1.StartCoord().x.Should().Be(14);
            te1.StartCoord().y.Should().Be(7);
            te1.EndCoord().x.Should().Be(14);
            te1.EndCoord().y.Should().Be(7);
            te1.Width().Should().Be(1);
            te1.Height().Should().Be(1);

            te1.Circumference.Should().Be(4);

            te1.IsTrack.Should().BeTrue();
            te1.IsBlock.Should().BeFalse();

            var te1Routes = te1.GetDimensionRoutes();
            te1Routes.Count.Should().Be(1);
            te1Routes[0].Should().Be("AC");

            var te2Routes = te2.GetDimensionRoutes();
            te2Routes.Count.Should().Be(1);
            te2Routes[0].Should().Be("BD");
        }

        [TestMethod]
        public void TestCheckTrack()
        {
            var path = WorkspaceRoot + @"RocrailDemo\metamodel.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["14x4"];
            var tr0ways = tr0.GetAllPossibleWays();
            tr0ways.Count.Should().Be(4);

            var tr1 = field["13x4"];
            var tr1ways = tr1.GetAllPossibleWays();
            tr1ways.Count.Should().Be(1);

            var tr2 = field["7x8"];
            var tr2ways = tr2.GetAllPossibleWays();
            tr2ways.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestCheckCross()
        {
            var json = @" {  'planField': {
    '16x9': {
      'id': 59,
      'name': 'Crossing Left sr',
      'identifier': 'SW_124',
      'clickable': false,
      'routes': [
        'A0|C0,A0|D1,B0|D1,B0|C0',
        'B0|D0,B0|A1,A1|C0,C0|D0'
      ],
      'dimensions': [ { 'w': 2, 'h': 1 }, { 'w': 1, 'h': 2 } ],
      'coord': { 'x': 16, 'y': 9 },
      'editor': { 'themeId': 59, 'offsetX': 0, 'offsetY': 0, 'themeDimIdx': 0 }
    } } } ";
            var field = LoadPlanField(json);

            var tr0 = field["16x9"];
            var tr0ways = tr0.GetAllPossibleWays();
            tr0ways.Count.Should().Be(4);

            var allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(4);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.crossing.json")]
        public void TestCrossingRoutes()
        {
            var path = @"Testmodels\metamodel.crossing.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["8x8"];
            tr0.IsSwitch.Should().BeTrue();

            var allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(4);

            var allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(8);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.crossing.json")]
        public void TestTrackWays0()
        {
            var path = @"Testmodels\metamodel.crossing.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["8x9"];

            var allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(4);

            var allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(4);

            foreach(var it in allAllowedPath)
                Trace.WriteLine(it);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.crossing.json")]
        public void TestTrackWays1()
        {
            var path = @"Testmodels\metamodel.crossing.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["10x9"];

            var allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(2);

            var allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(0);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.crossing.json")]
        public void TestTrackWays2()
        {
            var path = @"Testmodels\metamodel.crossing.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["9x7"];
            tr0.IsSwitch.Should().BeFalse();
            tr0.IsTrack.Should().BeTrue();

            var allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(2);

            var allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(2);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.crossing.json")]
        public void TestConnectors()
        {
            var path = @"Testmodels\metamodel.crossing.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["11x7"];
            tr0.IsConnector.Should().BeTrue();

            var allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(1);

            var allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(2);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.crossing.json")]
        public void TestElementTypes()
        {
            var path = @"Testmodels\metamodel.crossing.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["6x8"];
            tr0.IsConnector.Should().BeFalse();
            tr0.IsSignal.Should().BeTrue();

            var tr1 = field["11x8"];
            tr1.IsConnector.Should().BeFalse();
            tr1.IsSignal.Should().BeFalse();
            tr1.IsDirection.Should().BeTrue();

            var tr2 = field["13x8"];
            tr2.IsConnector.Should().BeFalse();
            tr2.IsSignal.Should().BeFalse();
            tr2.IsDirection.Should().BeFalse();
            tr2.IsSensor.Should().BeTrue();
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.crossing.json")]
        public void TestCheckCurves()
        {
            var path = @"Testmodels\metamodel.singleroute.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["14x6"];
            var allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(2);
            var allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(2);

            tr0 = field["14x2"];
            allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(2);
            allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(2);

            tr0 = field["1x2"];
            allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(2);
            allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(2);

            tr0 = field["1x6"];
            allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(2);
            allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(2);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.singleroute.json")]
        public void TestRoutingA()
        {
            var path = @"Testmodels\metamodel.singleroute.json";
            var field = LoadPlanFieldFile(path);

            var bkTop = field["6x2"];
            bkTop.IsBlock.Should().BeTrue();
            bkTop.IsTrack.Should().BeFalse();

            var bkBottom = field["6x6"];
            bkBottom.IsBlock.Should().BeTrue();
            bkBottom.IsTrack.Should().BeFalse();

            var blocks = field.GetBlocks();
            blocks.Count.Should().Be(2);

            var routesTop = field.GetRoutes(bkTop);
            routesTop.Count.Should().Be(2);
            
            var routesBottom = field.GetRoutes(bkBottom);
            routesBottom.Count.Should().Be(2);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.multipleroute.json")]
        public void TestRoutingB()
        {
            var path = @"Testmodels\metamodel.multipleroute.json";
            var field = LoadPlanFieldFile(path);

            var bkLeft = field["2x3"];
            var bkRight = field["15x3"];

            var blocks = field.GetBlocks();
            blocks.Count.Should().Be(2);

            var routesLeft = field.GetRoutes(bkLeft);
            routesLeft.Count.Should().Be(2);
            ShowRoutes("routesLeft", routesLeft);

            var routesRight = field.GetRoutes(bkRight);
            routesRight.Count.Should().Be(2);
            ShowRoutes("routesRight", routesRight);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.multipleroute2.json")]
        public void TestRoutingC()
        {
            var path = @"Testmodels\metamodel.multipleroute2.json";
            var field = LoadPlanFieldFile(path);

            var bkLeft = field["2x3"];
            var bkRight = field["15x3"];

            var blocks = field.GetBlocks();
            blocks.Count.Should().Be(2);

            var routesLeft = field.GetRoutes(bkLeft);
            routesLeft.Count.Should().Be(3);

            var routesRight = field.GetRoutes(bkRight);
            routesRight.Count.Should().Be(3);
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.multipleroute3.json")]
        public void TestRoutingD()
        {
            var path = @"Testmodels\metamodel.multipleroute3.json";
            var field = LoadPlanFieldFile(path);

            var bkLeft = field["2x3"];
            var bkRight = field["15x3"];
            var bkBottom = field["9x5"];

            var blocks = field.GetBlocks();
            blocks.Count.Should().Be(3);

            var routesLeft = field.GetRoutes(bkLeft);
            routesLeft.Count.Should().Be(2);
            ShowRoutes("routesLeft", routesLeft);

            var routesRight = field.GetRoutes(bkRight);
            routesRight.Count.Should().Be(3);
            ShowRoutes("routesRight", routesRight);

            var routesBottom = field.GetRoutes(bkBottom);
            routesBottom.Count.Should().Be(2);
            ShowRoutes("routesBottom", routesBottom);
        }

        [TestMethod]
        public void TestRoutingRocrailDemo()
        {
            var path = WorkspaceRoot + @"RocrailDemo\metamodel.json";
            var field = LoadPlanFieldFile(path);

            var bk5_1 = field["5x1"];
            var bk9_8 = field["9x8"];
            var bk7_11 = field["7x11"];

            var blocks = field.GetBlocks();
            blocks.Count.Should().Be(8);

            var routes9_8 = field.GetRoutes(bk9_8);
            routes9_8.Count.Should().Be(0);
            ShowRoutes("routes9_8", routes9_8);

            var routes7_11 = field.GetRoutes(bk7_11);
            routes7_11.Count.Should().Be(1);
            ShowRoutes("routes7_11", routes7_11);

            var routes5_1 = field.GetRoutes(bk5_1);
            routes5_1.Count.Should().Be(3);
            ShowRoutes("routes7_11", routes5_1);

            var tracks = routes5_1[0].Get(PlanItemT.Track);
            tracks.Count.Should().BeGreaterThan(0);

            var block0 = field["5x1"];
            var block1 = field["9x8"];
            var r0 = routes5_1.Get(block0, block1);
            r0.Should().NotBeNull();
            r0.Count.Should().Be(1);
            // TODO add checks for valid found

            var sw117 = field["2x1"];
            sw117.Should().NotBeNull();
            sw117.IsSwitch.Should().BeTrue();
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.connector.json")]
        public void TestRoutingConnectorDemo()
        {
            var path = @"Testmodels\metamodel.connector.json";
            var field = LoadPlanFieldFile(path);

            var bk4_2 = field["4x2"];
            var bk4_4 = field["4x4"];

            var blocks = field.GetBlocks();
            blocks.Count.Should().Be(2);

            var routes4_2 = field.GetRoutes(bk4_2);
            routes4_2.Count.Should().Be(1);
            ShowRoutes("routes4_2", routes4_2);

            var routes4_4 = field.GetRoutes(bk4_4);
            routes4_4.Count.Should().Be(1);
            ShowRoutes("routes4_4", routes4_4);
        }

        [TestMethod]
        public void TestRoutingBasementDemo()
        {
            var path = WorkspaceRoot + @"BasementTest\metamodel.json";
            var field = LoadPlanFieldFile(path);

            var startBlock = field["7x8"];
            var r8_9 = field.GetRoutes(startBlock);
            var targetBlock = field["21x18"];
            foreach (var itR in r8_9)
            {
                if (itR.Target.identifier == targetBlock.identifier)
                {
                    Trace.WriteLine("DEBUG");

                    var sw0 = itR.Get("SK2");
                    sw0.Should().NotBeNull();

                    var sw0step = itR.GetStep("SK2");
                    sw0step.Should().NotBeNull();

                    sw0step.PreviousItemPath.FromSide.Should().Be(Path.Side.Top);
                    sw0step.PreviousItemPath.ToSide.Should().Be(Path.Side.Left);
                }
            }

            var allBlocks = field.GetBlocks();
            allBlocks.Count.Should().Be(21);

            var step = 0;
            var maxStep = allBlocks.Count;
            var totalRoutes = 0;

            foreach (var itBlock in allBlocks)
            {
                itBlock.Should().NotBeNull();
                var routes = field.GetRoutes(itBlock);
                routes.Count.Should().BeGreaterThan(0);
                //ShowRoutes(itBlock.name, routes);
                totalRoutes += routes.Count;
                ++step;
                ShowProgress(step, maxStep, $"Analyzing Basement (Current routes: {totalRoutes})");
            }

            totalRoutes.Should().Be(210);

            Trace.WriteLine($"Found {totalRoutes} routes.");
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.switch.json")]
        public void TestCrossingSwitch()
        {
            var path = @"Testmodels\metamodel.switch.json";
            var field = LoadPlanFieldFile(path);

            var tr0 = field["8x2"];
            tr0.IsSwitch.Should().BeTrue();

            var allPossibleWays = tr0.GetAllPossibleWays();
            allPossibleWays.Count.Should().Be(4);

            var allAllowedPath = tr0.GetAllowedPath();
            allAllowedPath.Count.Should().Be(4);

            var bkLeft = field["3x2"];
            bkLeft.Should().NotBeNull();
            var routes = field.GetRoutes(bkLeft);
            routes.Count.Should().Be(2);
            ShowRoutes("bkLeft", routes);

            var r0 = routes[0];
            r0.Should().NotBeNull();
            r0.Items.Count.Should().Be(5);
            var r0switch = r0.Items[2];
            r0switch.Item.IsSwitch.Should().BeTrue();
            (r0switch.PreviousItemPath.FromSide == Path.Side.Left).Should().BeTrue();
            (r0switch.PreviousItemPath.ToSide == Path.Side.Right).Should().BeTrue();
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.switchDirections.json")]
        public void TestCrossingSwitchDirections()
        {
            var path = @"Testmodels\metamodel.switchDirections.json";
            var field = LoadPlanFieldFile(path);

            var bkLeft = field["1x1"];
            bkLeft.Should().NotBeNull();
            var routes = field.GetRoutes(bkLeft);
            routes.Count.Should().Be(2);
            ShowRoutes("bkLeft", routes);

            var bkRight = field["8x1"];
            bkRight.Should().NotBeNull();
            var routes1 = field.GetRoutes(bkRight);
            routes1.Count.Should().Be(1);
            ShowRoutes("bkRight", routes1);
            routes1[0].Items[2].GetThemeSwitchPrefix().Should().Be("straight");

            var bkRight2 = field["8x2"];
            bkRight2.Should().NotBeNull();
            var routes2 = field.GetRoutes(bkRight2);
            routes2.Count.Should().Be(1);
            ShowRoutes("bkRight2", routes2);
            var r2 = routes2[0];
            r2.Should().NotBeNull();
            var r2switch = r2.Items[3];
            r2switch.Should().NotBeNull();
            r2switch.Item.IsSwitch.Should().BeTrue();
            r2switch.GetThemeSwitchPrefix().Should().Be("turnright");

            //var r0 = routes[0];
            //r0.Should().NotBeNull();
            //r0.Items.Count.Should().Be(5);
            //var r0switch = r0.Items[2];
            //r0switch.Item.IsSwitch.Should().BeTrue();
            //(r0switch.PreviousItemPath.FromSide == Path.Side.Left).Should().BeTrue();
            //(r0switch.PreviousItemPath.ToSide == Path.Side.Right).Should().BeTrue();
        }

        [TestMethod]
        [DeploymentItem(@"Testmodels\metamodel.switchDirections2.json")]
        public void TestCrossingSwitchDirections2()
        {
            var path = @"Testmodels\metamodel.switchDirections2.json";
            var field = LoadPlanFieldFile(path);

            var bkLeft = field["2x2"];
            bkLeft.Should().NotBeNull();
            var routes = field.GetRoutes(bkLeft);
            routes.Count.Should().Be(3);
            ShowRoutes("bkLeft", routes);

            var bk69 = field["6x9"];
            bk69.Should().NotBeNull();
            var routesBk69 = field.GetRoutes(bk69);
            routesBk69.Count.Should().Be(1);
            ShowRoutes("bk69", routesBk69);
            routesBk69[0].Items[2].GetThemeSwitchPrefix().Should().Be("straight");
            routesBk69[0].Items[4].GetThemeSwitchPrefix().Should().Be("turnleft");
            routesBk69[0].Items[6].GetThemeSwitchPrefix().Should().Be("straight");

            var bk6_10 = field["6x10"];
            bk6_10.Should().NotBeNull();
            var routes6_10 = field.GetRoutes(bk6_10);
            routes6_10.Count.Should().Be(1);
            routes6_10[0].Items[3].GetThemeSwitchPrefix().Should().Be("turnright");
            routes6_10[0].Items[5].GetThemeSwitchPrefix().Should().Be("turnleft");
            routes6_10[0].Items[7].GetThemeSwitchPrefix().Should().Be("straight");
        }

        [TestMethod]
        public void TestAnalyzer()
        {
            var path = WorkspaceRoot + @"BasementTest\metamodel.json";
            var field = LoadPlanFieldFile(path);

            var analyzer = new Analyze(field);
            var analyzerResult = analyzer.Execute((step, maxSteps) =>
            {
                ShowProgress(step, maxSteps, "Analyzing Basement");
            });
            analyzerResult.NumberOfRoutes.Should().Be(210);

            var json = analyzerResult.ToJson();
            json.Length.Should().BeGreaterThan(0);
        }

        private static void ShowProgress(int step, int maxStep, string msg)
        {
            Trace.WriteLine($"{msg} {(int) (step/(float)maxStep*100.0)}%");
        }

        private static void ShowRoutes(string name, IReadOnlyList<Route> routes)
        {
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);

            if (routes.Count == 0)
            {
                Trace.WriteLine($"Route({name}) is empty.");
            }
            else
            {
                foreach (var it in routes)
                {
                    Trace.Write($"Route({name}) Steps({it.Items.Count}) |");
                    foreach (var itt in it.Items)
                    {
                        if (itt.PreviousItemPath != null && itt.PreviousItemPath.FromSide != Path.Side.None && itt.PreviousItemPath.ToSide != Path.Side.None)
                        {
                            Trace.Write($" {itt.Item.identifier} " +
                                        $"({itt.PreviousItemPath.FromSide}->{itt.PreviousItemPath.ToSide}, " +
                                        $"{itt.GetThemeSwitchPrefix()}) | ");
                        }
                        else
                        {
                            Trace.Write($" {itt.Item.identifier} | ");
                        }
                    }
                    Trace.WriteLine("-|");
                }
            }

            Trace.WriteLine(string.Empty);
        }
    }
}
