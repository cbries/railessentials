using System;
using System.Collections.Generic;
using SharpDX.DirectInput;

namespace RailwayEssentialCore
{
    public enum StickDirection
    {
        Left,
        Right,
        Top,
        Bottom,
        Unknown
    }

    public class GamepadEventArgs : EventArgs
    {
        public JoystickUpdate Data { get; set; }
        public bool IsRightStick { get; set; }
        public bool IsLeftStick { get; set; }
        public StickDirection Direction { get; set; }

        public GamepadEventArgs()
        {
            Direction = StickDirection.Unknown;
        }
    }

    public interface IGamepad
    {
        event EventHandler ButtonHandler;

        List<Guid> GetAvailableDevices();
        List<Guid> GetGamepadGuids();
        List<Guid> GetJoystickGuids();
        bool IsAvailable(Guid guid);
        bool ActivateGamepad(Guid guid);
        bool Close();
    }
}
