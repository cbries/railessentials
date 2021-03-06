// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Metadata.cs

using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Feedbacks;
using railessentials.Locomotives;
using railessentials.Occ;
using railessentials.Plan;
using Utilities;
using RouteList = railessentials.Route.RouteList;
// ReSharper disable NotAccessedField.Local

namespace railessentials
{
    public class Metadata
    {
        private string _metamodelPath = "";
        private string _routePath = "";
        private string _ecosDataPath = "";

        public JObject Metamodel { get; set; }
        public JArray Routes { get; set; }
        public JObject EcosData { get; set; }
        public OccData Occ { get; set; }
        public LocomotivesData LocomotivesData { get; set; }
        public FeedbacksData FeedbacksData { get; set; }

        public Metadata()
        {
            Occ = new OccData(this);
            LocomotivesData = new LocomotivesData(this);
            FeedbacksData = new FeedbacksData(this);
        }

        public JObject GetMetamodelItem(PlanItemCoord coord)
        {
            if (coord == null) return null;
            if (coord.x == -1) return null;
            if (coord.y == -1) return null;

            var accessor = coord.x + "x" + coord.y;
            var planField = Metamodel["planField"] as JObject;
            return planField?[accessor] as JObject;
        }

        public JObject GetMetamodelItem(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return null;
            var planField = Metamodel["planField"] as JObject;
            if (planField == null) return null;
            foreach (var itItem in planField)
            {
                var obj = itItem.Value as JObject;
                if (obj == null) continue;
                var id = obj.GetString("identifier");
                if (string.IsNullOrEmpty(id)) continue;
                if (id.Equals(identifier))
                    return obj;
            }

            return null;
        }

        public bool LoadMetamodel(string pathToMetamodel)
        {
            _metamodelPath = pathToMetamodel;
            var o = ReadJsonObject(pathToMetamodel) ?? new JObject {["planField"] = new JObject()};
            Metamodel = o;
            return Metamodel != null;
        }

        public bool LoadRoutes(string pathToRoutemodel, bool resetToInitState = false)
        {
            _routePath = pathToRoutemodel;
            Routes = ReadJsonArray(pathToRoutemodel) ?? new JArray();
            if (!resetToInitState) return Routes != null;
            foreach (var it in Routes ?? new JArray())
                it["occupied"] = false;
            Save(SaveModelType.RouteData);
            return Routes != null;
        }

        public bool SetRoutes(RouteList routeList)
        {
            if(routeList == null || routeList.Count == 0)
            {
                Routes = new JArray();
                return true;
            }

            try
            {
                Routes = JArray.Parse(JsonConvert.SerializeObject(routeList, Formatting.Indented));
                return true;
            }
            catch
            {
                // ignore
            }

            return false;
        }

        public bool LoadEcosDataForSimulation(string pathToEcosData)
        {
            _ecosDataPath = pathToEcosData;
            EcosData = ReadJsonObject(pathToEcosData);
            return EcosData != null;
        }

        public bool LoadOccData(string pathToOccmodel, bool resetToInitState = false)
        {
            if (string.IsNullOrEmpty(pathToOccmodel)) return false;
            if (!File.Exists(pathToOccmodel)) return false;
            Occ = new OccData(this);
            var r = Occ.Load(pathToOccmodel);
            if (resetToInitState)
            {
                foreach (var it in Occ.Blocks)
                {
                    if (it == null) continue;
                    it.NextBlock = string.Empty;
                    it.FinalBlock = string.Empty;
                    it.RouteToNext = string.Empty;
                    it.RouteToFinal = string.Empty;
                    it.NextEntered = false;
                    it.FinalEntered = false;
                }
                Save(SaveModelType.OccData);
            }

            return r;
        }

        public bool LoadLocomotives(string pathToLocomotivesmodel)
        {
            if (string.IsNullOrEmpty(pathToLocomotivesmodel)) return false;
            if (!File.Exists(pathToLocomotivesmodel)) return false;
            LocomotivesData = new LocomotivesData(this);
            return LocomotivesData.Load(pathToLocomotivesmodel);
        }

        public bool LoadFeedbacks(string pathToFeedbacksmodel)
        {
            if (string.IsNullOrEmpty(pathToFeedbacksmodel)) return false;
            if (!File.Exists(pathToFeedbacksmodel)) return false;
            FeedbacksData = new FeedbacksData(this);
            return FeedbacksData.Load(pathToFeedbacksmodel);
        }

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

        internal static JArray ReadJsonArray(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (!File.Exists(path)) return null;
            try
            {

                var cnt = File.ReadAllText(path, Encoding.UTF8);
                return JArray.Parse(cnt);
            }
            catch
            {
                // ignore
            }
            return null;
        }

        internal static string ReadJson(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            if (!File.Exists(path)) return string.Empty;
            try
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
            return string.Empty;
        }

        public void SaveAll()
        {
            Save(SaveModelType.MetamodelData);
            Save(SaveModelType.OccData);
            Save(SaveModelType.RouteData);
            Save(SaveModelType.LocomotivesData);
            Save(SaveModelType.FeedbacksData);

            // TODO store EcosData ?
        }

        public enum SaveModelType
        {
            MetamodelData,
            OccData,
            RouteData,
            LocomotivesData,
            FeedbacksData
        }

        public void Save(SaveModelType type)
        {
            switch(type)
            {
                case SaveModelType.MetamodelData:
                    var strJson0 = Metamodel.ToString(Formatting.Indented);
                    strJson0.FixBomIfNeeded();
                    StringUtilities.WriteAllTextNoBom(_metamodelPath, strJson0, out _);
                    break;

                case SaveModelType.OccData:
                    Occ.Save();
                    break;

                case SaveModelType.RouteData:
                    var strJson2 = Routes.ToString(Formatting.Indented);
                    strJson2.FixBomIfNeeded();
                    StringUtilities.WriteAllTextNoBom(_routePath, strJson2, out _);
                    break;

                case SaveModelType.LocomotivesData:
                    var strJson3 = LocomotivesData.ToJsonString();
                    strJson3.FixBomIfNeeded();
                    StringUtilities.WriteAllTextNoBom(LocomotivesData.LocomotivesPath, strJson3, out _);
                    break;

                case SaveModelType.FeedbacksData:
                    var strJson4 = FeedbacksData.ToJsonString();
                    strJson4.FixBomIfNeeded();
                    StringUtilities.WriteAllTextNoBom(FeedbacksData.FeedbacksPath, strJson4, out _);
                    break;
            }
        }
    }
}