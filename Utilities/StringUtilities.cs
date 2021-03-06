// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: StringUtilities.cs

using System;
using System.IO;
using System.Text;

namespace Utilities
{
    public static class StringUtilities
    {
        /// <summary>
        /// You can get this or test it originally with: Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble())[0];
        /// But no need, this way we have a constant. As these three bytes `[239, 187, 191]` (a BOM) evaluate to a single C# char.
        /// </summary>
        public const char BomChar = (char)65279;

        public static string FixBomIfNeeded(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var hasBom = str[0] == BomChar;
            if (hasBom) str = str.Substring(1);
            return str;
        }

        public static bool WriteAllTextNoBom(string path, string content, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var utf8WithoutBom = new UTF8Encoding(false);
                File.WriteAllText(path, content.Trim(), utf8WithoutBom);
                return true;
            }
            catch(Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}