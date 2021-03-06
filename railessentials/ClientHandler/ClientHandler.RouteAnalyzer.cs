// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ClientHandler.RouteAnalyzer.cs

using System;
using System.Collections.Generic;
using railessentials.Route;

namespace railessentials.ClientHandler
{
    public partial class ClientHandler
    {
        private const string RouteAnalyerLabel = "RouteAnalyzer";

        private async void InitializeRouteAnalyzerHandler()
        {
            var analyzer = new RouteAnalyzer(this);
            analyzer.Started += AnalyzerOnStarted;
            analyzer.Finished += AnalyzerOnFinished;
            analyzer.Failed += AnalyzerOnFailed;
            analyzer.FailedEx += AnalyzerOnFailedEx;
            analyzer.Progress += AnalyzerOnProgress;

            var metamodelFilePath = Globals.GetCfgDataPath("Metamodel");
            var routeFilePath = Globals.GetCfgDataPath("Routes");
            await analyzer.Start(metamodelFilePath, routeFilePath);
        }

        private void AnalyzerOnProgress(object sender, RouteAnalyzerState state)
        {
            SendDebug(new List<DebugMessage> {
                DebugMessage.Instance(state.Message)
            });
        }

        private void AnalyzerOnFailedEx(RouteAnalyzer sender, Exception reason)
        {
            _sniffer?.Logger?.Log?.Info($"{RouteAnalyerLabel} failed: {reason.Message}");
        }

        private void AnalyzerOnFailed(RouteAnalyzer sender, string reason)
        {
            _sniffer?.Logger?.Log?.Info($"{RouteAnalyerLabel} failed: {reason}");
        }

        private void AnalyzerOnFinished(RouteAnalyzer sender)
        {
            _sniffer?.Logger?.Log?.Info($"{RouteAnalyerLabel} finished");

            lock (_metadataLock)
            {
                _metadata.LoadRoutes(Globals.GetCfgDataPath("Routes"));
            }

            SendModelToClients(ModelType.UpdateRoutes);
            SendModelToClients(ModelType.UpdateOcc);
            SendModelToClients(ModelType.UpdateFeedbacks);
        }

        private void AnalyzerOnStarted(RouteAnalyzer sender)
        {
            _sniffer?.Logger?.Log?.Info($"{RouteAnalyerLabel} started");
        }
    }
}
