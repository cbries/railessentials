﻿// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ClientHandler.AutoMode.cs

namespace railessentials.ClientHandler
{
    public partial class ClientHandler
    {
        private AutoMode.AutoMode _autoMode;

        private void InitAutoMode()
        {
            if (_autoMode != null) return;
            _autoMode = new AutoMode.AutoMode(this);
        }

        private void CleanupHandler()
        {
            if (_autoMode == null) return;
            if (_autoMode.IsStarted()) return;
            _autoMode.Started -= AutoModeOnStarted;
            _autoMode.Stopped -= AutoModeOnStopped;
        }

        private void StopAutoMode()
        {
            InitAutoMode();
            _autoMode.Stop();
            CleanupHandler();
        }

        private void SetAllLocomotiveToStopped()
        {
            lock (_metadataLock)
            {
                _metadata.LocomotivesData.SetAllLocomotivesStopped();
                _metadata?.Save(Metadata.SaveModelType.LocomotivesData);
            }

            SendModelToClients(ModelType.UpdateLocomotivesData);
        }

        private async void StartAutoMode()
        {
            //
            // when AutoMode is started, no Locomotive should start on-demand
            // this is the most savest way for not destroying some expensive hardware
            //
            SetAllLocomotiveToStopped();

            InitAutoMode();
            if (_autoMode.IsStarted()) return;
            CleanupHandler();

            _autoMode.Started += AutoModeOnStarted;
            _autoMode.Stopped += AutoModeOnStopped;

            await _autoMode.Run();
        }

        private bool IsAutoModeStarted()
        {
            if (_autoMode == null) return false;
            return _autoMode.IsStarted();
        }

        private void AutoModeOnStopped(AutoMode.AutoMode sender)
        {
            _sniffer?.Logger?.Log?.Info("AutoMode stopped");
        }

        private void AutoModeOnStarted(AutoMode.AutoMode sender)
        {
            _sniffer?.Logger?.Log?.Info("AutoMode started");
        }
    }
}
