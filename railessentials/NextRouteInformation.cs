// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: NextRouteInformation.cs

using ecoslib.Entities;
using railessentials.Occ;
using railessentials.Plan;
using railessentials.Route;

namespace railessentials
{
    public class NextRouteInformation
    {
        public Route.Route Route { get; set; }
        public PlanItem FbEnter { get; set; }
        public PlanItem FbIn { get; set; }
        public int LocomotiveObjectId { get; set; }
        public Locomotive Locomotive { get; set; }
        public Locomotives.Data LocomotivesData { get; set; }
        public DataProvider DataProvider { get; set; }
        public DataProvider DataProviderS88 { get; set; }
        public OccBlock OccBlock { get; set; }
        public RouteBlock FromBlock { get; set; }
        public RouteBlock TargetBlock { get; set; }

        public override string ToString()
        {
            return
                $"{Locomotive.Name} cruises on {Route.Name} -- Feedbacks({FbEnter} -> {FbIn}) -- Target: {Route.Blocks[1].identifier}";
        }
    }
}