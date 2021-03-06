// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IItemSerializer.cs

using Newtonsoft.Json.Linq;

namespace ecoslib
{
    public interface IItemSerializer
    {
        JObject ToJson();
        void ParseJson(JObject obj);
    }
}
