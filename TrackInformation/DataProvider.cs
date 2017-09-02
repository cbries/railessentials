/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Ecos2Core;
using Ecos2Core.Blocks;
using Ecos2Core.Replies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TrackInformation.Annotations;
using TrackInformationCore;

namespace TrackInformation
{    
    public class DataProvider : IDataProvider, INotifyPropertyChanged
    {
        public event DataChangedDelegator DataChanged;
        public event CommandsReadyDelegator CommandsReady;

        private readonly ObservableCollection<IItem> _objects = new ObservableCollection<IItem>();

        public ObservableCollection<IItem> Objects => _objects;

        public Ecos2 Baseobject { get; set; }

        public bool SaveObjects(string targetFilepath)
        {
            try
            {
                lock (_objects)
                {
                    JArray arLocomotives = new JArray();
                    JArray arSwitches = new JArray();
                    JArray arRoutes = new JArray();
                    JArray arS88 = new JArray();

                    foreach (var item in _objects)
                    {
                        if (item == null)
                            continue;

                        JObject jsonO = item.ToJson();

                        if (item is Locomotive && jsonO != null)
                            arLocomotives.Add(jsonO);
                        else if (item is Switch && jsonO != null)
                            arSwitches.Add(jsonO);
                        else if (item is Route && jsonO != null)
                            arRoutes.Add(jsonO);
                        else if (item is S88 && jsonO != null)
                            arS88.Add(jsonO);
                        else
                        {
                            // ignore
                        }
                    }

                    var o = new JObject
                    {
                        ["locomotives"] = arLocomotives,
                        ["switches"] = arSwitches,
                        ["routes"] = arRoutes,
                        ["s88"] = arS88
                    };

                    File.WriteAllText(targetFilepath, o.ToString(Formatting.Indented), Encoding.UTF8);
                }

                return true;
            }
            catch(Exception ex)
            {
                Trace.WriteLine("<DataProvider> " + ex.Message);
                return false;
            }
        }

        public bool LoadObjects(string targetPath)
        {
            try
            {
                if (!File.Exists(targetPath))
                    return false;

                var cnt = File.ReadAllText(targetPath, Encoding.UTF8);
                JObject o = JObject.Parse(cnt);
                if (o == null)
                    return false;

                if (o["locomotives"] != null)
                {
                    JArray ar = o["locomotives"] as JArray;
                    if (ar != null)
                    {
                        foreach (var arItem in ar)
                        {
                            if (arItem == null)
                                continue;

                            var e = new Locomotive();
                            e.ParseJson(arItem as JObject);
                            e.CommandsReady += CommandsReady;
                            _objects.Add(e);
                            DataChanged?.Invoke(this);
                        }
                    }
                }

                if (o["switches"] != null)
                {
                    JArray ar = o["switches"] as JArray;
                    if (ar != null)
                    {
                        foreach (var arItem in ar)
                        {
                            if (arItem == null)
                                continue;

                            var e = new Switch();
                            e.ParseJson(arItem as JObject);
                            e.CommandsReady += CommandsReady;
                            _objects.Add(e);
                            DataChanged?.Invoke(this);
                        }
                    }
                }

                if (o["routes"] != null)
                {
                    JArray ar = o["routes"] as JArray;
                    if (ar != null)
                    {
                        foreach (var arItem in ar)
                        {
                            if (arItem == null)
                                continue;

                            var e = new Route();
                            e.ParseJson(arItem as JObject);
                            e.CommandsReady += CommandsReady;
                            _objects.Add(e);
                            DataChanged?.Invoke(this);
                        }
                    }
                }

                if (o["s88"] != null)
                {
                    JArray ar = o["s88"] as JArray;
                    if (ar != null)
                    {
                        foreach (var arItem in ar)
                        {
                            if (arItem == null)
                                continue;

                            var e = new S88();
                            e.ParseJson(arItem as JObject);
                            e.CommandsReady += CommandsReady;
                            _objects.Add(e);
                            DataChanged?.Invoke(this);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("<DataProvider> " + ex.Message);
                return false;
            }
        }

        public IItem GetObjectBy(int objectid)
        {
            if (objectid <= 0)
                return null;

            foreach (var o in _objects)
            {
                if (o == null)
                    continue;

                if (o.ObjectId == objectid)
                    return o;
            }

            return null;
        }

        private bool HandleEvent(IBlock block)
        {
            if (block == null)
                return false;

            RegisterS88.S88Info s88Info;
            if (RegisterS88.IsRegisterS88(block, out s88Info))
            {
                if (s88Info != null && !s88Info.Appended)
                {
                    RemoveObjectWithId((uint)s88Info.ObjectId);

                    return true;
                }
                else if(s88Info != null)
                {
                    if (DoesObjectIdExist((uint) s88Info.ObjectId))
                        RemoveObjectWithId((uint) s88Info.ObjectId);

                    int n = _objects.Count(x => x is S88);

                    if (n + 1 != s88Info.NewSize)
                    {
                        Trace.WriteLine("<Error> S88 BusSize Mismatch");

                        return false;
                    }

                    var s88 = new S88 { ObjectId = s88Info.ObjectId, Index = n };
                    s88.CommandsReady += CommandsReady;
                    _objects.Add(s88);
                    DataChanged?.Invoke(this);
                    s88.EnableView();

                    return true;
                }
            }

            foreach (var e in block.ListEntries)
            {
                if (e == null)
                    continue;

                IItem item = GetObjectBy(e.ObjectId);

                if (HandleEventS88(item, e))
                    continue;

                if (HandleEventSwitch(item, e))
                    continue;

                if (HandleEventLocomotive(item, e))
                    continue;
            }

            return true;
        }

        private bool HandleEventLocomotive(IItem item, ListEntry listEntry)
        {
            Locomotive e = item as Locomotive;
            if (e == null)
                return false;

            var itemLocomotive = GetObjectBy(e.ObjectId) as Locomotive;

            if (itemLocomotive != null)
            {
                itemLocomotive.Parse(listEntry.Arguments);
                itemLocomotive.UpdateTitle();
                itemLocomotive.UpdateSubTitle();
            }

            return true;
        }

        private bool HandleEventSwitch(IItem item, ListEntry listEntry)
        {
            Switch e = item as Switch;
            if (e == null)
                return false;

            var itemSwitch = GetObjectBy(e.ObjectId) as Switch;

            if (itemSwitch != null)
            {
                itemSwitch.Parse(listEntry.Arguments);
                itemSwitch.UpdateTitle();
                itemSwitch.UpdateSubTitle();
            }

            return true;
        }

        private bool HandleEventS88(IItem item, ListEntry listEntry)
        {
            S88 e = item as S88;
            if (e == null)
                return false;

            // 1101 0000 1101 0110
            //                   0
            // 16

            string hex = listEntry.Arguments[0].Parameter[0];

            var itemS88 = GetObjectBy(e.ObjectId) as S88;
            if (itemS88 != null)
            {
                itemS88.StateOriginal = hex;
                itemS88.UpdateTitle();
                itemS88.UpdateSubTitle();
            }

            return true;
        }

        public bool Add(IBlock block)
        {
            if (block == null)
                return false;

            if (block is EventBlock)
            {
                if (block.ObjectId.HasValue && block.ObjectId.Value == 1)
                    return HandleBaseobject(block);

                return HandleEvent(block);
            }

            if (block.Command == null)
                return false;

            if (block.Command.Type != CommandT.QueryObjects && block.Command.Type != CommandT.Get)
                return false;

            var objectId = block.Command.ObjectId;

            var sid = $"{block.Command.ObjectId}";
            if (sid.Length >= 3)
            {
                if (sid.StartsWith("10", StringComparison.OrdinalIgnoreCase))
                    objectId = 10;
            }

            switch(objectId)
            {
                case 1: // baseobject
                    return HandleBaseobject(block);

                case 11: // switch or route
                    return AddSwitchOrRoute(block);

                case 10: // locomotive
                    return AddLocomotive(block);

                case 26: // s88 ports
                    return AddS88(block);
            }

            return false;
        }

        public bool DoesObjectIdExist(uint objectId)
        {
            foreach (var o in _objects)
            {
                if (o?.ObjectId == objectId)
                    return true;
            }
            return false;
        }

        public void RemoveObjectWithId(uint objectId)
        {
            for (int index = 0; index < _objects.Count; ++index)
            {
                if (_objects[index].ObjectId == objectId)
                {
                    _objects.RemoveAt(index);

                    return;
                }
            }
        }

        private bool HandleBaseobject(IBlock block)
        {
            if (block == null)
                return false;

            foreach (var e in block.ListEntries)
            {
                if (e == null)
                    continue;

                switch (e.ObjectId)
                {
                    case Globals.ID_EV_BASEOBJECT:
                    {
                        if (Baseobject == null)
                            Baseobject = new Ecos2 {ObjectId = e.ObjectId};

                        Baseobject.Parse(e.Arguments);
                        if (!DoesObjectIdExist((uint) e.ObjectId))
                            _objects.Add(Baseobject);
                        DataChanged?.Invoke(this);                            
                    }
                        break;
                }
            }

            return true;
        }

        private bool AddSwitchOrRoute(IBlock block)
        {
            if (block == null)
                return false;

            foreach (var e in block.ListEntries)
            {
                if (e == null)
                    continue;

                string sid = $"{e.ObjectId}";

                if (sid.StartsWith("20", StringComparison.OrdinalIgnoreCase))
                {
                    var sw = GetObjectBy(e.ObjectId);
                    if(sw == null)
                    { 
                        sw = new Switch { ObjectId = e.ObjectId };
                        sw.Parse(e.Arguments);
                        sw.CommandsReady += CommandsReady;
                        _objects.Add(sw);
                        DataChanged?.Invoke(this);
                        sw.EnableView();
                    }
                    else
                    {
                        sw.Parse(e.Arguments);
                        sw.CommandsReady -= CommandsReady;
                        sw.CommandsReady += CommandsReady;
                        DataChanged?.Invoke(this);
                        if (!sw.HasView)
                            sw.EnableView();
                    }
                }
                else if (sid.StartsWith("30", StringComparison.OrdinalIgnoreCase))
                {
                    var r = GetObjectBy(e.ObjectId);
                    if (r == null)
                    {
                        r = new Route {ObjectId = e.ObjectId};
                        r.Parse(e.Arguments);
                        r.CommandsReady += CommandsReady;
                        _objects.Add(r);
                        DataChanged?.Invoke(this);
                        r.EnableView();
                    }
                    else
                    {
                        r.Parse(e.Arguments);
                        r.CommandsReady -= CommandsReady;
                        r.CommandsReady += CommandsReady;
                        if (!r.HasView)
                            r.EnableView();
                        DataChanged?.Invoke(this);
                    }
                }
            }

            return true;
        }

        private bool AddLocomotive(IBlock block)
        {
            if (block == null)
                return false;

            foreach (var e in block.ListEntries)
            {
                if (e == null)
                    continue;

                string sid = $"{e.ObjectId}";

                if (sid.StartsWith("10", StringComparison.OrdinalIgnoreCase))
                {
                    if (!DoesObjectIdExist((uint) e.ObjectId))
                    {
                        var l = new Locomotive { ObjectId = e.ObjectId };
                        l.Parse(e.Arguments);
                        l.CommandsReady += CommandsReady;
                        _objects.Add(l);
                        DataChanged?.Invoke(this);
                        l.EnableView();
                        l.QueryState();
                    }
                    else
                    {
                        var l = GetObjectBy(e.ObjectId) as Locomotive;
                        if (l != null)
                        {
                            l.Parse(e.Arguments);
                            l.CommandsReady -= CommandsReady;
                            l.CommandsReady += CommandsReady;

                            if (!l.HasView)
                                l.EnableView();

                            if (!l.InitQueryStateDone)
                            {
                                l.QueryState();
                                l.InitQueryStateDone = true;
                            }
                            DataChanged?.Invoke(this);
                        }
                    }
                }
            }

            return true;
        }

        private bool AddS88(IBlock block)
        {
            if (block == null)
                return false;

            foreach (var e in block.ListEntries)
            {
                if (e == null)
                    continue;

                string sid = $"{e.ObjectId}";

                if (sid.StartsWith("10", StringComparison.OrdinalIgnoreCase))
                {
                    int n = _objects.Count(x => x is S88);

                    var s88 = GetObjectBy(e.ObjectId) as S88;
                    if (s88 == null)
                    {
                        s88 = new S88 {ObjectId = e.ObjectId, Index = n};
                        s88.Parse(e.Arguments);
                        s88.CommandsReady += CommandsReady;
                        _objects.Add(s88);
                        DataChanged?.Invoke(this);
                        s88.EnableView();
                    }
                    else
                    {
                        s88.Parse(e.Arguments);
                        s88.CommandsReady -= CommandsReady;
                        s88.CommandsReady += CommandsReady;
                        if (!s88.HasView)
                            s88.EnableView();
                        DataChanged?.Invoke(this);
                    }
                }
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)

        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
