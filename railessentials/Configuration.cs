// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Configuration.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Utilities;

namespace railessentials
{
    public class Configuration
    {
        public bool SimulationMode { get; set; }
        public string SimulationData { get; set; }
        public string RootWorkspace { get; set; }
        public string RecentWorkspace { get; set; }
        public ConfigurationEcos Ecos { get; set; } = new();
        public ConfigurationWsServer WsServer { get; set; } = new();
        public ConfigurationWebServer WebServer { get; set; } = new();
        public ConfigurationTheme Theme { get; set; } = new();
        public bool SaveOnEveryPlanfieldChange { get; set; } = false;
        public int InitializeDelay { get; set; } = 500;

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
        public string HttpRoot { get; set; }
        public string HttpRootFallback { get; set; }
        public List<string> Prefixes { get; set; } = new();
    }

    public class ConfigurationTheme
    {
        public string Root { get; set; }
        public string Name { get; set; }
    }
}
