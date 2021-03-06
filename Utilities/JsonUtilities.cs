// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: JsonUtilities.cs

using Newtonsoft.Json.Linq;

namespace Utilities
{
    public static class JsonUtilities
    {
        public static int GetInt(this JObject obj, string key, int def = 0)
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                if (int.TryParse(obj[key].ToString(), out var v))
                    return v;
            }
            return def;
        }

        public static string GetString(this JObject obj, string key, string def = "")
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                var v = obj[key]?.ToString();
                return v;
            }
            return def;
        }

        public static bool GetBool(this JObject obj, string key, bool def = false)
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                var v = obj[key]?.ToString();
                if (bool.TryParse(v, out var vv))
                    return vv;
            }
            return def;
        }
    }
}
