// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoModeTaskCore.cs

using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ArrangeModifiersOrder

namespace railessentials.AutoMode
{
    public delegate void AutoModeTaskFinished(AutoModeTaskCore sender);

    public abstract class AutoModeTaskCore 
    {
        public event AutoModeTaskFinished Finished;

        protected AutoMode Ctx { get; set; }

        protected NextRouteInformation Route { get; set; }

        protected CancellationTokenSource CancelSource = new();

        public string RouteName => Route.Route.Name;

        public virtual async Task Run()
        {
            await Task.Run(() =>
            {
                // TBD
            });
        }
        
        protected void SendDebugMessage(string msg)
        {
            var dbg = Ctx?.GetClientHandler() as IDebugMessages;
            dbg?.SendDebugMessage($"{msg}");
        }

        public void Cancel()
        {
            CancelSource?.Cancel();
        }

        protected void TriggerFinished()
        {
            Finished?.Invoke(this);
        }
    }
}