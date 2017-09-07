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
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RailwayEssentialCore;

namespace RailwayEssentialWeb
{
    public class WebGenerator : IWebGenerator
    {
        private string _targetFilepath;

        public int Rows { get; set; }
        public int Columns { get; set; }

        public int TileWidth { get; set; }
        public int TileHeight { get; set; }

        public Theme.Theme Theme { get; }

        public WebGenerator(Theme.Theme theme)
        {
            Theme = theme;

            Rows = 50;
            Columns = 50;

            TileWidth = 24;
            TileHeight = 24;
        }

        private string AbsoluteThemeDirname => Path.Combine(Theme.ThemeDirectory, Theme.ThemeName);

        private List<string> ThemeFiles => Directory.GetFiles(AbsoluteThemeDirname, "*.svg", SearchOption.TopDirectoryOnly).ToList();

        private string _selectCategory = "";
        private Dictionary<string, string> _selectHtml = new Dictionary<string, string>();

        private void CreateSymbolList(out List<string> physicalSymbols)
        {
            physicalSymbols = new List<string>();

            foreach (var e in ThemeFiles)
            {
                if (string.IsNullOrEmpty(e))
                    continue;

                var p = new Uri(e).AbsoluteUri;

                physicalSymbols.Add(p);
            }
        }

        private void CreateSymbolSelection()
        {
            // categories
            // [key:=category name, value:=selector list entries]
            string mhtmlCategories = "";

            var catnames = Theme.CategoryNames;

            foreach (var k in catnames)
                mhtmlCategories += "<option value=\"" + k + "\">" + k + "</option>\r\n";
            _selectCategory = mhtmlCategories;

            if (catnames == null || catnames.Count == 0)
                return;

            foreach (var k in catnames)
            {
                string html = $"<div id=\"webmenuDiv{k}\" style=\"width: 400px; vertical-align: middle;\">\r\n<select name=\"webmenu{k}\" id=\"webmenu{k}\" style=\"width: 400px; vertical-align: middle;\">\r\n";

                var symbolsOfCategory = Theme.GetDefaultForCategory(k);

                foreach (var symbol in symbolsOfCategory)
                {                   
                    foreach (var e in ThemeFiles)
                    {
                        if(string.IsNullOrEmpty(e))
                            continue;

                        var checkE = Path.GetFileNameWithoutExtension(e);

                        if (checkE.EndsWith(symbol.Value, StringComparison.OrdinalIgnoreCase) && checkE.Length == symbol.Value.Length)
                        {
                            var symbolName = Path.GetFileNameWithoutExtension(e);
                            if (string.IsNullOrEmpty(symbolName))
                                continue;

                            var themeItem = Theme.Get(k, symbol.Value);

                            var p = new Uri(e).AbsoluteUri;

                            html += $"<option value=\"{symbolName}\" data-railway-themeid=\"{themeItem.UniqueIdentifier}\" data-image=\"{p}\">{symbol.Key}</option>\r\n";

                            break;
                        }
                    }

                }

                html += "</select>\r\n</div>\r\n";

                if (_selectHtml.ContainsKey(k))
                    _selectHtml[k] = html;
                else
                    _selectHtml.Add(k, html);
            }
        }

        private string CreateBase()
        {
            var fname = @"Trackplans\Webeditor\template.html.keep".ExpandRailwayEssential();
            try
            {
                return File.ReadAllText(fname, Encoding.UTF8);
            }
            catch
            {
                return "";
            }
        }

        public bool Update()
        {
            if (string.IsNullOrEmpty(_targetFilepath))
                return false;

            return Generate(_targetFilepath);
        }

        public bool Generate(string targetFilepath)
        {
            _targetFilepath = targetFilepath;

            StringBuilder oSb = new StringBuilder();

            oSb.Append("<table cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"border: 0;\">");
            for (int y = 0; y < Rows; ++y)
            {
                oSb.Append("<tr class=\"row\">");
                for (int x = 0; x < Columns; ++x)
                {
                    var cellInfo = $"title=\"X={x+1}, Y={y+1}\"";
                    var cellId = $"id=\"td_{x+1}_{y+1}\"";

                    oSb.Append($"<td {cellId} class=\"cell\" {cellInfo}><div class=\"overflow\"></div></td></td>");
                }
                oSb.Append("</tr>\r\n");
            }
            oSb.Append("</table>");

            string css = ""; //string.Format("table {{width:{0}px; height:{1}px;}}", Columns * TileWidth, Rows * TileHeight);

            try
            {
                List<string> physicalSymbols;
                CreateSymbolList(out physicalSymbols);
                CreateSymbolSelection();

                Dictionary<string, string> base64Images = new Dictionary<string, string>();

                var jsCode = "var themeDirectory='" + new Uri(AbsoluteThemeDirname.Replace("\\", "/")).AbsoluteUri + "';";
                jsCode += "var symbolFiles = [";
                foreach (var e in physicalSymbols)
                {
                    if (string.IsNullOrEmpty(e))
                        continue;

                    jsCode += $"'{e}',";

                    Uri u = new Uri(e);
                    var upath = u.AbsolutePath.Replace("%20", " ");
                    if (File.Exists(upath))
                    {
                        string cnt = File.ReadAllText(upath);

                        var base64Cnt = Encoding.UTF8.ToBase64(cnt);
                        string accessName = Path.GetFileNameWithoutExtension(upath);
                        if(!base64Images.ContainsKey(accessName))
                            base64Images.Add(accessName, base64Cnt);
                    }
                }
                jsCode = jsCode.TrimEnd(',');
                jsCode += "]; var svgCache = {}; var counter = 0; var total = symbolFiles.length; preloadSvgs();";

                JObject ar = new JObject();
                foreach (var k in base64Images.Keys)
                    ar[k] = base64Images[k];
                var base64Json = "var symbolFilesBase64 = " + ar.ToString(Formatting.None) + ";";

                jsCode += base64Json;

                var b = CreateBase();
                    b = b.Replace("{{GLOBALJS}}", jsCode);
                    b = b.Replace("{{GLOBALCSS}}", css);
                    b = b.Replace("{{TRACKTABLE}}", oSb.ToString());
                    b = b.Replace("{{TRACKSYMBOLCATEGORIES}}", _selectCategory);
                    b = b.Replace("{{TRACKSYMBOLS_Track}}", _selectHtml["Track"]);
                    b = b.Replace("{{TRACKSYMBOLS_Switch}}", _selectHtml["Switch"]);
                    b = b.Replace("{{TRACKSYMBOLS_Signal}}", _selectHtml["Signal"]);
                    b = b.Replace("{{TRACKSYMBOLS_Block}}", _selectHtml["Block"]);
                    b = b.Replace("{{TRACKSYMBOLS_Sensor}}", _selectHtml["Sensor"]);
                    b = b.Replace("{{TRACKSYMBOLS_Accessory}}", _selectHtml["Accessory"]);

                var dname = Path.GetDirectoryName(targetFilepath);
                if (!string.IsNullOrEmpty(dname))
                {
                    var filesToRemove = Directory.GetFiles(dname, "*_track.html", SearchOption.TopDirectoryOnly);

                    try
                    {
                        foreach (var fname in filesToRemove)
                            File.Delete(fname);
                    }
                    catch
                    {
                        // ignore
                    }

                }
                File.WriteAllText(targetFilepath, b, Encoding.UTF8);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
