//using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using ecoslibServer;
//using Newtonsoft.Json;
//using TrackPlaner.Plan;
//using RouteList = ecoslibServer.RouteList;
//using System.Collections.Generic;
//using System.Diagnostics;
//using ecoslib.Entities;
//using ecoslibServer.Occ;

//namespace ecoslibServerNetTest
//{
//    [TestClass]
//    public class AutoModeTest
//    {
//        private Metadata _metadata;
//        private DataProvider _dataProvider;
//        private RouteList _routeList;
//        private PlanField _planfield;

//        [TestInitialize]
//        public void TestInitialize()
//        {
//            if (_metadata != null) return;

//            _metadata = new Metadata();
//            _metadata.LoadMetamodel(Globals.GetCfgDataPath("Metamodel"));
//            _metadata.LoadRoutes(Globals.GetCfgDataPath("Routes"));
//            _metadata.LoadLocomotives(Globals.GetCfgDataPath("Locomotives"));
//            _metadata.LoadFeedbacks(Globals.GetCfgDataPath("FbEvents"));
//            _metadata.LoadOccData(Globals.GetCfgDataPath("Occ"));
//            _metadata.LoadEcosDataForSimulation(Globals.ConstTargetEcosDataFilename);

//            _dataProvider = new DataProvider(DataModeT.General);
//            _dataProvider.Parse(_metadata.EcosData);

//            var nativeRouteData = _metadata.Routes.ToString();
//            _routeList = JsonConvert.DeserializeObject<RouteList>(nativeRouteData);
//            _planfield = GetPlanField(_metadata);
//        }

//        [TestMethod]
//        public void TestMethod1()
//        {
//            foreach (var itOccBlock in _metadata.Occ.Blocks)
//            {
//                var nextRoute = GetNextRoute(itOccBlock);
//                if (nextRoute == null) continue;

//                var fromBlock = nextRoute.Blocks[0];
//                var targetBlock = nextRoute.Blocks[1];

//                Trace.WriteLine($"Route: {nextRoute.Name}");
//                Trace.WriteLine($" > From: {fromBlock.identifier}");
//                Trace.WriteLine($" > To:   {targetBlock.identifier}");
//                Trace.WriteLine($" > {nextRoute.Occupied}");

//                itOccBlock.FinalBlock = targetBlock.identifier;
//                _metadata.Save(Metadata.SaveModelType.OccData);
//            }
//        }

//        public ecoslibServer.Route GetNextRoute(OccBlock occBlock)
//        {
//            var occFromBlock = occBlock.FromBlock;
//            if (string.IsNullOrEmpty(occFromBlock)) return null;

//            var occLocOid = occBlock.Oid;
//            var locDataEcos = _dataProvider.GetObjectBy(occLocOid) as Locomotive;
//            var locData = _metadata.LocomotivesData.GetData(occLocOid);

//            var sideToLeave = TrackPlaner.Analyzer.SideMarker.None;
//            if (locData.EnterBlockSide.IndexOf("+", StringComparison.Ordinal) != -1)
//                sideToLeave = TrackPlaner.Analyzer.SideMarker.Minus;
//            else
//                sideToLeave = TrackPlaner.Analyzer.SideMarker.Plus;

//            var routesFrom = _routeList.GetRoutesWithFromBlock(occFromBlock, sideToLeave);
//            var routesFromFiltered = routesFrom.FilterBy(locDataEcos, locData, _metadata.FeedbacksData);
//            var routesFromNotOccupied = routesFromFiltered.FilterNotOccupied(_metadata.Occ);
//            var idx = GetRndBetween(routesFromNotOccupied.Count);
//            return routesFromNotOccupied[idx];
//        }

//        #region Helper

//        private static int GetRndBetween(int max = int.MaxValue, int from = 0)
//        {
//            var r = new Random(Guid.NewGuid().GetHashCode());
//            return r.Next(from, max);
//        }

//        private PlanField GetPlanField(Metadata metadata)
//        {
//            var metamodel = metadata?.Metamodel;
//            if (metamodel == null) return null;
//            var planfield = JsonConvert.DeserializeObject<Dictionary<string, PlanField>>(metamodel.ToString(Formatting.None));
//            return planfield["planField"];
//        }

//        #endregion
//    }
//}
