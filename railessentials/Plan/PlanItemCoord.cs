// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanItemCoord.cs

using Newtonsoft.Json.Linq;
using Utilities;

// ReSharper disable InconsistentNaming

namespace railessentials.Plan
{
    public class PlanItemCoord
    {
        public int x { get; set; }
        public int y { get; set; }

        public bool Parse(JToken tkn)
        {
            var o = tkn as JObject;
            if (o == null) return false;

            x = o.GetInt("x", -1);
            y = o.GetInt("y", -1);

            if (x == -1 || y == -1)
                return false;

            return true;
        }

        public static PlanItemCoord GetInstance(JToken tkn)
        {
            var instance = new PlanItemCoord();
            if (instance.Parse(tkn))
                return instance;
            return null;
        }
    }
}
