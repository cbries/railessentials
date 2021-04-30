// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Configuration.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utilities;

namespace railessentials
{
    public class Configuration
    {
        public bool SimulationMode { get; set; }
        public string SimulationData { get; set; }
        public string RootWorkspace { get; set; }
        public string RecentWorkspace { get; set; }
        public List<ConfigurationWebCam> Webcams { get; set; }
        public ConfigurationEcos Ecos { get; set; } = new();
        public ConfigurationWsServer WsServer { get; set; } = new();
        public ConfigurationWebServer WebServer { get; set; } = new();
        public ConfigurationTheme Theme { get; set; } = new();
        public bool SaveOnEveryPlanfieldChange { get; set; } = false;
        public int InitializeDelay { get; set; } = 500;
        public bool GhostDetection { get; set; }

        [JsonProperty(PropertyName = "Occ")]
        public OccWaitModes OccWait { get; set; } = new();

        private static string _cfgPath;

        public static Configuration Load(string pathToCfg, out string errorMessage)
        {
            _cfgPath = pathToCfg;

            errorMessage = string.Empty;

            try
            {
                var cnt = File.ReadAllText(pathToCfg, Encoding.UTF8);
                var instance = JsonConvert.DeserializeObject<Configuration>(cnt);
                return instance;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\n" + ex.InnerException.Message;
            }

            return null;
        }

        public static bool Save(Configuration cfg, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var json = JsonConvert.SerializeObject(cfg, Formatting.Indented);
                json = json.FixBomIfNeeded();
                File.WriteAllText(_cfgPath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\n" + ex.InnerException.Message;
            }

            return false;
        }

        public void UpdateWebcam(JObject obj)
        {
            if (obj == null) return;
            try
            {
                var cnt = File.ReadAllText(_cfgPath, Encoding.UTF8);
                var json = JObject.Parse(cnt);
                
                var url = obj["url"]?.ToString();
                if (string.IsNullOrEmpty(url)) return;
                var questionIdx = url.IndexOf("?", StringComparison.OrdinalIgnoreCase);
                if (questionIdx != -1)
                    url = url.Substring(0, questionIdx);

                var webcams = json["Webcams"] as JArray;
                if (webcams == null || webcams.Count == 0) return;

                foreach(var it in webcams)
                {
                    var itUrl = it["Url"]?.ToString();
                    if(string.IsNullOrEmpty(itUrl)) continue;
                    if(itUrl.Equals(url, StringComparison.OrdinalIgnoreCase))
                    {
                        it["Width"] = (int)obj["w"];
                        it["Height"] = (int)obj["h"];
                        it["X"] = (int)obj["x"];
                        it["Y"] = (int)obj["y"];
                    }
                }

                File.WriteAllText(_cfgPath, json.ToString(Formatting.Indented), Encoding.UTF8);

                // update internal memory footprint of the webcams
                var newCfg = Load(_cfgPath, out _);
                if (newCfg != null)
                    Webcams = newCfg.Webcams;
            }
            catch
            {
                // ignore
            }
        }
    }

    public class OccWaitModeStatic
    {
        public bool Enabled { get; set; } = true;
        public int Seconds { get; set; } = 10;
    }

    public class OccWaitModeRandom
    {
        public bool Enabled { get; set; } = false;
        public int SecondsMin { get; set; } = 5;
        public int SecondsMax { get; set; } = 15;
    }

    public class OccWaitModes
    {
        public OccWaitModeStatic WaitModeStatic { get; set; } = new();
        public OccWaitModeRandom WaitModeRandom { get; set; } = new();
    }

    public class ConfigurationEcos
    {
        public string Ip { get; set; }
        public ushort Port { get; set; }

        public override string ToString()
        {
            return $"{Ip}:{Port}";
        }
    }

    public class ConfigurationWsServer
    {
        public string Ip { get; set; }
        public ushort Port { get; set; }

        public IPAddress GetIpAddress()
        {
            if (string.IsNullOrEmpty(Ip))
                return IPAddress.Any;
            if (Ip.Equals("Any", StringComparison.OrdinalIgnoreCase))
                return IPAddress.Any;
            return IPAddress.Parse(Ip);
        }
    }

    public class ConfigurationWebServer
    {
        public string HttpRootDebug { get; set; }
        public string HttpRoot { get; set; }
        public string HttpRootFallback { get; set; }
        public List<string> Prefixes { get; set; } = new();
    }

    public class ConfigurationWebCam
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Fps { get; set; }
        public string Caption { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class ConfigurationTheme
    {
        public string Root { get; set; }
        public string Name { get; set; }
        public string PlanBackground { get; set; }
    }
}
