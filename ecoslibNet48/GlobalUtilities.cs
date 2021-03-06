// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: GlobalUtilities.cs

using System;
using System.IO;

namespace ecoslib
{
    public static class Utils
    {
        public static string Get(this Exception ex)
        {
            return $"Exception: {ex.Message}";
        }

        public static void Show(this Exception ex)
        {
            Console.WriteLine(ex.Get());
        }

        public static int ToInt(this string str, int defaultResult = 0)
        {
            if (string.IsNullOrEmpty(str)) return defaultResult;
            if (int.TryParse(str, out var iv))
                return iv;
            return defaultResult;
        }

        public static string GenerateUniqueName(this string fmt, string dirname = null)
        {
            for (var i = 0; i < 1000; ++i)
            {
                if (!string.IsNullOrEmpty(dirname))
                {
                    var name = Path.Combine(dirname, string.Format(fmt, i));
                    if (!File.Exists(name))
                        return name;
                }
                else
                {
                    var name = string.Format(fmt, i);
                    if (!File.Exists(name))
                        return name;
                }
            }

            return null;
        }
    }
}
