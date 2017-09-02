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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecos2Core
{
    public class CommandArgument
    {
        private List<string> _parameter = new List<string>();

        public string Name { get; set; }

        public List<string> Parameter
        {
            get { return _parameter; }
            set { _parameter = value; }
        }

        public override string ToString()
        {
            if (Parameter.Count <= 0)
                return Name;

            for(int i=0; i < Parameter.Count; ++i)
            {
                var p = Parameter[i];
                if (p.IndexOf(" ", StringComparison.OrdinalIgnoreCase) != -1)
                    Parameter[i] = "\"" + p + "\"";
            }

            return string.Format("{0}[{1}]", Name, string.Join(",", Parameter));
        }

        public bool Parse(string argument, bool keepQuotes=false)
        {
            if (string.IsNullOrEmpty(argument))
                return false;

            if(argument.IndexOf("[", StringComparison.OrdinalIgnoreCase) != -1 && argument.IndexOf("]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                int index = argument.IndexOf("[", StringComparison.OrdinalIgnoreCase);

                Name = argument.Substring(0, index).Trim();
                var args = argument.Substring(index + 1).Trim().TrimEnd(']').Trim();
                if (string.IsNullOrEmpty(args))
                    Parameter.Clear();
                else
                {
                    Parameter = args.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
                    for (int i = 0; i < Parameter.Count; ++i)
                    {
                        if (string.IsNullOrEmpty(Parameter[i]))
                            continue;

                        if(!keepQuotes)
                            Parameter[i] = Parameter[i].Trim().Trim('"');
                    }
                }
            }
            else
            {
                Name = argument.Trim();
                Parameter.Clear();
            }

            return true;
        }
    }
}
