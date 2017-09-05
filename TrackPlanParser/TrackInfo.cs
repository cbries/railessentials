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
using Newtonsoft.Json.Linq;

namespace TrackPlanParser
{
    public class TrackInfo
    {
        public Func<bool> CheckState { get; set; }

        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int ThemeId { get; set; }
        public string Orientation { get; set; }
        public string Description { get; set; }
        public JObject Options { get; set; }

        public TrackInfo()
        {
            X = -1;
            Y = -1;
            ThemeId = -1;
            Orientation = "rot0";
            Options = new JObject();
        }

        public JObject ToObject()
        {
            JObject o = new JObject
            {
                ["name"] = Name,
                ["x"] = X,
                ["y"] = Y,
                ["themeId"] = ThemeId,
                ["orientation"] = Orientation,
                ["description"] = Description,
                ["options"] = Options
            };
            return o;
        }

        public void Parse(JObject o)
        {
            if (o == null)
                return;

            if (o["name"] != null)
                Name = o["name"].ToString();
            if (o["x"] != null)
                X = (int) o["x"];
            if (o["y"] != null)
                Y = (int) o["y"];
            if (o["themeId"] != null)
                ThemeId = (int) o["themeId"];
            if (o["orientation"] != null)
                Orientation = o["orientation"].ToString();
            if (o["description"] != null)
                Description = o["description"].ToString();
            if(o["options"] != null)
                Options = o["options"] as JObject;
        }

        public override string ToString()
        {
            return $"{X}:{Y} -> {ThemeId}";
        }

        public void SetOption(string name, string value)
        {
            if (Options == null)
                Options = new JObject();

            if (string.IsNullOrEmpty(value))
            {
                try
                {
                    Options.Remove(name);
                }
                catch
                {
                    Options[name] = "";
                }

                return;
            }

            Options[name] = value;
        }

        public string GetOption(string name)
        {
            if (Options == null)
                return null;

            if (Options[name] == null)
                return null;

            return Options[name].ToString();
        }

        #region valid for Blocks

        public int GetLocomotiveObjectId()
        {
            var vv = GetOption("blockCurrentLocomotive");
            if (string.IsNullOrEmpty(vv))
                return -1;

            if (int.TryParse(vv, out var objectId))
                return objectId;

            return -1;
        }

        public void SetLocomotiveObjectId(int objectId)
        {
            if (objectId == -1)
                SetOption("blockCurrentLocomotive", "");
            else
                SetOption("blockCurrentLocomotive", $"{objectId}");
        }

        public int GetLocomotivePreviewObjectId()
        {
            var vv = GetOption("blockPreviewLocomotive");
            if (string.IsNullOrEmpty(vv))
                return -1;

            if (int.TryParse(vv, out var objectId))
                return objectId;

            return -1;
        }

        public void SetLocomotivePreviewObjectId(int objectId)
        {
            if(objectId == -1)
                SetOption("blockPreviewLocomotive", "");
            else
                SetOption("blockPreviewLocomotive", $"{objectId}");
        }

        #endregion
    }
}
