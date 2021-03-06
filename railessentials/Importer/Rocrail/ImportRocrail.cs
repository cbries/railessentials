// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Program.cs

using System;
using System.IO;

namespace railessentials.Importer.Rocrail
{
    public class ImportRocrail
    {
        public string ThemePath { get; set; }
        
        public bool Execute(string inputPlanFile, string outputDirectory, out string errorMessage)
        {
            if (string.IsNullOrEmpty(ThemePath))
            {
                errorMessage = "ThemePath is not set.";
                return false;
            }

            if(!File.Exists(ThemePath))
            {
                errorMessage = $"ThemePath does not exist: {ThemePath}";
                return false;
            }

            if (string.IsNullOrEmpty(inputPlanFile))
            {
                errorMessage = $"Input file is not set.";
                return false;
            }

            if (!File.Exists(inputPlanFile))
            {
                errorMessage = $"Input file does not exist: {inputPlanFile}";
                return false;
            }

            if (string.IsNullOrEmpty(outputDirectory))
            {
                errorMessage = "Output directory is not set";
                return false;
            }

            try
            {
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }

            errorMessage = string.Empty;

            try
            {
                var converter = new XmlConverter {ThemeJsonPath = ThemePath};

                converter.Run(
                    inputPlanFile,
                    Path.Combine(outputDirectory, "metamodel.json"),
                    Path.Combine(outputDirectory, "fbevents.json"));

                return true;

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
