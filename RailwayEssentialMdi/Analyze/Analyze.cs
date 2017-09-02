/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using RailwayEssentialMdi.ViewModels;

namespace RailwayEssentialMdi.Analyze
{

    public class Analyze
    {
        private readonly RailwayEssentialModel _model;
        

        public Analyze(RailwayEssentialModel model)
        {
            _model = model;
        }

        public AnalyzeResult Execute()
        {
            var m = new Map(_model);

            m.Build();

            var numberOfRoutes = m.GetRoutes();

            AnalyzeResult res = new AnalyzeResult
            {
                NumberOfRoutes = numberOfRoutes,
                Routes = m.Routes,
                RouteGroups = m.GetRouteGroups()
            };

            return res;
        }
    }
}
