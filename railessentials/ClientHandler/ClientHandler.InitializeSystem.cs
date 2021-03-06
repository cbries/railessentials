// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ClientHandler.InitializeSystem.cs

using System;
using System.Collections.Generic;

namespace railessentials.ClientHandler
{
    public partial class ClientHandler
    {
        private const string InitializerLabel = "Initializer";

        private async void InitializeSystemHandler()
        {
            var initializer = new InitializeSystem(this);
            initializer.Started += InitializerOnStarted;
            initializer.Finished += InitializerOnFinished;
            initializer.Failed += InitializerOnFailed;
            initializer.FailedEx += InitializerOnFailedEx;
            initializer.Progress += InitializerOnProgress;
            await initializer.Start();
        }

        private void InitializerOnProgress(object sender, InitializeSystemState state)
        {
            SendDebug(new List<DebugMessage> {
                DebugMessage.Instance(state.Message)
            });
        }

        private void InitializerOnFailedEx(object sender, Exception reason)
        {
            _sniffer?.Logger?.Log?.Info($"{InitializerLabel} failed: {reason.Message}");
        }

        private void InitializerOnFailed(object sender, string reason)
        {
            _sniffer?.Logger?.Log?.Info($"{InitializerLabel} failed: {reason}");
        }

        private void InitializerOnFinished(object sender)
        {
            _sniffer?.Logger?.Log?.Info($"{InitializerLabel} finished");
        }

        private void InitializerOnStarted(object sender)
        {
            _sniffer?.Logger?.Log?.Info($"{InitializerLabel} started");
        }
    }
}
