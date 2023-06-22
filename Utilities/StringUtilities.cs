// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: StringUtilities.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static string RemoveInvalidFileNameChars(this string filename)
        {
            if (string.IsNullOrEmpty(filename)) return filename;
            var parts = filename.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return filename;

            var fname = parts.Last();
            var p = filename;
            p = p.Replace(fname, string.Empty);
            p = p.TrimEnd(new[] {'/'});

            fname = string.Join("_", fname.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(p, fname);
        }

        private static readonly List<string> ImageExtensions = new()
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".bmp",
            ".tif",
            ".tiff",
            ".ico",
            ".svg"
        };

        public static bool IsImageFileExtension(this string filename)
        {
            if (string.IsNullOrEmpty(filename)) return false;
            foreach(var it in ImageExtensions)
                if (filename.EndsWith(it, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
    }
}