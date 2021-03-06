// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IDataProvider.cs

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ecoslib.Entities
{
    public enum DataModeT { General, S88 }

    public interface IDataProvider
    {
        DataModeT Mode { get; }
        ILogger Logger { get; set; }

        IReadOnlyList<IItem> Objects { get; }

		void Append(IItem item);
        IItem GetObjectBy(int objectid);
        IItem GetAccessoryByAddress(int ecosAddress);
        IItem GetLocomotiveByAddress(int ecosAddress);
        bool HandleData(IBlock block);
	    bool Parse(JToken tkn);
		JObject ToJson();
        void ResetAllChangedStates();
    }
}
