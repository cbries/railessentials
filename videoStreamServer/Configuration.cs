// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Configuration.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace videoStreamServer
{
    public class Configuration
    {
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

        public ConfigurationWebServer WebServer { get; set; }
    }

    public class ConfigurationWebServer
    {
        public List<string> Prefixes { get; set; } = new();
    }
}
