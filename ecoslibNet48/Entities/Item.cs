// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Item.cs

using System.Collections.Generic;
using System.Text;
using ecoslib.Utilities;
using Newtonsoft.Json.Linq;

namespace ecoslib.Entities
{
    public abstract class Item
        : IItem
        , IItemView
        , IItemSerializer
		, ICommandsBase64
	{
        protected Item()
        {
            ObjectId = -1;
        }

        private readonly object _lockCommands = new();
        private readonly List<object> _commands = new();

        protected IReadOnlyList<object> Commands
        {
            get
            {
                lock (_lockCommands)
                {
                    return _commands;
                }
            }
        }

        public void AddCmd(ICommand cmd)
        {
            lock (_lockCommands)
            {
                _commands?.Add(cmd);
            }
        }

        #region IItem

        public int ObjectId { get; set; }
        
        public void ResetCmds()
        {
            lock (_lockCommands)
            {
                _commands?.Clear();
            }
        }

        public IReadOnlyList<object> GetCommands(bool cleanOriginal = true)
        {
            lock(_lockCommands)
            {
                var newList = new List<object>(_commands);
                if(cleanOriginal)
                    _commands.Clear();
                return newList;
            }
        }

        public virtual string Caption => "-.-";

        // ReSharper disable once InconsistentNaming
        protected bool _hasChanged = true;

        public bool HasChanged => _hasChanged;
        
        /// <summary>
        /// INTERNAL USE ONLY
        /// </summary>
        public void __simulateChange()
        {
            _hasChanged = true;
        }

        public void ResetChangedState()
        {
            _hasChanged = false;
        }

        public virtual int TypeId()
        {
            return -1;
        }

        public virtual bool Parse(List<object> arguments)
        {
            return false;
        }

        public virtual void QueryState()
        {

        }

        #endregion

        #region IItemView

        public bool HasView { get; private set; }

        public void EnableView()
        {
            if (HasView) return;
            AddCmd(CommandFactory.Create($"request({ObjectId}, view)"));
            HasView = true;
        }

        public void DisableView()
        {
            if (!HasView) return;
            AddCmd(CommandFactory.Create($"release({ObjectId}, view)"));
            HasView = false;
        }

        #endregion

        #region IItemSerializer

        public virtual JObject ToJson()
        {
            return null;
        }

        public virtual void ParseJson(JObject obj)
        {

        }

		#endregion

		#region ICommandsBase64

		public virtual string ToBase64()
		{
            lock (_commands)
            {
                if (Commands == null || Commands.Count == 0)
                    return null;

                var n = Commands.Count;
                var sb = new StringBuilder();
                for (var i = 0; i < n; ++i)
                {
                    var cmd = Commands[i] as ICommand;
                    if (cmd == null) continue;
                    sb.Append(cmd.NativeCommand);
                    sb.Append("\n");
                }

                return Encoding.UTF8.ToBase64(sb.ToString());
            }
		}

		#endregion
    }
}
