// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RouteList.cs

using System.Collections.Generic;

namespace railessentials.Plan
{
    public class RouteList : List<Route>
    {
        /// <summary>
        /// Queries all direct routes between two blocks.
        /// </summary>
        /// <param name="block0"></param>
        /// <param name="block1"></param>
        /// <returns></returns>
        public RouteList Get(PlanItem block0, PlanItem block1)
        {
            var res = new RouteList();
            if (block0 == null) return res;
            if (block1 == null) return res;
            foreach(var it in this)
            {
                if (it == null) continue;
                var s = it.Start;
                var e = it.Target;
                if(s == null || e == null) continue;

                if (!s.identifier.Equals(block0.identifier) && !s.identifier.Equals(block1.identifier)) continue;
                if (!e.identifier.Equals(block0.identifier) && !e.identifier.Equals(block1.identifier)) continue;

                // start of final checks
                if (s.identifier.Equals(block0.identifier) && e.identifier.Equals(block1.identifier))
                    res.Add(it);
                else if (e.identifier.Equals(block0.identifier) && s.identifier.Equals(block1.identifier))
                    res.Add(it);
                else if (s.identifier.Equals(block1.identifier) && e.identifier.Equals(block0.identifier))
                    res.Add(it);
                else if (e.identifier.Equals(block1.identifier) && s.identifier.Equals(block0.identifier))
                    res.Add(it);
            }

            return res;
        }
    }
}
