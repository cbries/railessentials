// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Analyze.cs

using System;
using railessentials.Plan;

namespace railessentials.Analyzer
{
    public class Analyze
    {
        private readonly PlanField _planField;

        public Analyze(PlanField planField)
        {
            _planField = planField;
            _planField.InitContext();
        }

        public AnalyzeResult Execute(Action<int, int> progressCallback = null)
        {
            //var maxW = _planField.GetMaxWidth();
            //var maxH = _planField.GetMaxHeight();

            var allBlocks = _planField.GetBlocks();
            
            var res = new AnalyzeResult();

            var step = 0;
            var maxSteps = allBlocks.Count;

            foreach (var itBlock in allBlocks)
            {
                var routes = _planField.GetRoutes(itBlock);
                if (routes.Count == 0) continue;
                res.Routes.AddRange(routes);
                ++step;
                progressCallback?.Invoke(step, maxSteps);
            }

            return res;
        }
    }
}
