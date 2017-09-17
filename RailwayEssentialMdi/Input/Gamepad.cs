using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using RailwayEssentialCore;
using SharpDX;
using SharpDX.DirectInput;

namespace RailwayEssentialMdi.Input
{
    public class Gamepad : IGamepad
    {
        public event EventHandler ButtonHandler;

        private Guid _guid;
        private readonly DirectInput _input;
        private Joystick _pad;
        private BackgroundWorker _worker;
        private bool _doRun;

        public List<Guid> GetAvailableDevices()
        {
            var m0 = GetGamepadGuids();
            var m1 = GetJoystickGuids();

            m0.AddRange(m1);

            return m0;
        }

        public List<Guid> GetGamepadGuids()
        {
            List<Guid> gamepadGuids = new List<Guid>();
            if (_input == null)
                return gamepadGuids;
            foreach (var deviceInstance in _input.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
            {
                var guid = deviceInstance.InstanceGuid;
                if (guid.Equals(Guid.Empty))
                    continue;
                if (!gamepadGuids.Contains(guid))
                    gamepadGuids.Add(guid);
            }
            return gamepadGuids;
        }

        public List<Guid> GetJoystickGuids()
        {
            List<Guid> joystickGuids = new List<Guid>();
            if (_input == null)
                return joystickGuids;
            foreach (var deviceInstance in _input.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
            {
                var guid = deviceInstance.InstanceGuid;
                if (guid.Equals(Guid.Empty))
                    continue;
                if (!joystickGuids.Contains(guid))
                    joystickGuids.Add(guid);
            }
            return joystickGuids;
        }

        public bool IsAvailable(Guid guid)
        {
            if (guid.Equals(Guid.Empty))
                return false;

            var pads = GetGamepadGuids();
            if (pads.Contains(guid))
                return true;
            var sticks = GetJoystickGuids();
            if (sticks.Contains(guid))
                return true;
            return false;
        }

        public Gamepad()
        {
            _input = new DirectInput();
        }

        public bool ActivateGamepad(Guid guid)
        {
            if (_pad != null)
                return true;

            if (!IsAvailable(guid))
                return false;

            _guid = guid;

            return Start();
        }

        public bool Close()
        {
            if (_pad == null)
                return true;

            _doRun = false;

            if (_worker != null)
            {
                _worker.CancelAsync();
                _worker.Dispose();
                _worker = null;
            }

            try
            {
                _pad.Unacquire();
            }
            catch
            {
                // ignore
            }

            try
            {
                _pad.Dispose();
            }
            catch
            {
                // ignore
            }
            finally
            {
                _pad = null;
            }

            return true;
        }

        private bool Start()
        {
            _pad = new Joystick(_input, _guid);
            _pad.Properties.BufferSize = 128;
            _pad.Acquire();

            if (_worker == null)
                _worker = new BackgroundWorker();

            _doRun = true;

            _worker.DoWork += WorkerOnDoWork;
            _worker.WorkerReportsProgress = false;
            _worker.RunWorkerAsync();

            return true;
        }

        private bool _leftStickStateLeftReleased = true;
        private bool _leftStickStateRightReleased = true;
        private bool _leftStickStateTopReleased = true;
        private bool _leftStickStateBottomReleased = true;

        private bool _rightStickStateLeftReleased = true;
        private bool _rightStickStateRightReleased = true;
        private bool _rightStickStateTopReleased = true;
        private bool _rightStickStateBottomReleased = true;

        private void WorkerOnDoWork(object sender, DoWorkEventArgs args)
        {
            bool inputLost = false;

            while (_doRun)
            {
                if (inputLost)
                {
                    try
                    {
                        _pad.Unacquire();
                        _pad.Acquire();

                        inputLost = false;
                    }
                    catch
                    {
                        Thread.Sleep(500);

                        continue;
                    }
                }

                try
                {
                    _pad.Poll();

                    var datas = _pad.GetBufferedData();

                    foreach (var state in datas)
                    {
                        if (state.Offset >= JoystickOffset.Buttons0 &&
                            state.Offset <= JoystickOffset.Buttons127)
                        {
                            if (state.Value == 0)
                            {
                                ButtonHandler?.Invoke(this, new GamepadEventArgs
                                {
                                    Data = state
                                });
                            }

                            continue;
                        }

                        if (state.Offset >= JoystickOffset.PointOfViewControllers0 &&
                            state.Offset <= JoystickOffset.PointOfViewControllers3)
                        {
                            if (state.Value == 0 ||
                                state.Value == 9000 ||
                                state.Value == 18000 ||
                                state.Value == 27000)
                            {
                                var dir = StickDirection.Unknown;
                                if (state.Value == 0) dir = StickDirection.Top;
                                else if (state.Value == 9000) dir = StickDirection.Right;
                                else if (state.Value == 18000) dir = StickDirection.Bottom;
                                else if (state.Value == 27000) dir = StickDirection.Left;

                                ButtonHandler?.Invoke(this, new GamepadEventArgs
                                {
                                    Data = state,
                                    Direction = dir
                                });
                            }

                            continue;
                        }

                        if (state.Offset == JoystickOffset.X ||
                            state.Offset == JoystickOffset.Y)
                        {
                            if (state.Offset == JoystickOffset.X)
                            {
                                if (state.Value <= 20000)
                                    _leftStickStateLeftReleased = false;
                                if (state.Value >= 50000)
                                    _leftStickStateRightReleased = false;

                                if (state.Value == 32767)
                                {
                                    if (!_leftStickStateLeftReleased)
                                    {
                                        _leftStickStateLeftReleased = true;

                                        ButtonHandler?.Invoke(this, new GamepadEventArgs
                                        {
                                            Data = state,
                                            Direction = StickDirection.Left,
                                            IsLeftStick = true
                                        });
                                    }

                                    if (!_leftStickStateRightReleased)
                                    {
                                        _leftStickStateRightReleased = true;

                                        ButtonHandler?.Invoke(this, new GamepadEventArgs
                                        {
                                            Data = state,
                                            Direction = StickDirection.Right,
                                            IsLeftStick = true
                                        });
                                    }
                                }
                            }

                            if (state.Offset == JoystickOffset.Y)
                            {
                                if (state.Value <= 20000)
                                    _leftStickStateTopReleased = false;
                                if (state.Value >= 50000)
                                    _leftStickStateBottomReleased = false;

                                if (state.Value == 32767)
                                {
                                    if (!_leftStickStateTopReleased)
                                    {
                                        _leftStickStateTopReleased = true;

                                        ButtonHandler?.Invoke(this, new GamepadEventArgs
                                        {
                                            Data = state,
                                            Direction = StickDirection.Top,
                                            IsLeftStick = true
                                        });
                                    }

                                    if (!_leftStickStateBottomReleased)
                                    {
                                        _leftStickStateBottomReleased = true;

                                        ButtonHandler?.Invoke(this, new GamepadEventArgs
                                        {
                                            Data = state,
                                            Direction = StickDirection.Bottom,
                                            IsLeftStick = true
                                        });
                                    }
                                }
                            }

                            continue;
                        }

                        if (state.Offset == JoystickOffset.Z ||
                            state.Offset == JoystickOffset.RotationZ)
                        {
                            if (state.Offset == JoystickOffset.Z)
                            {
                                if (state.Value <= 20000)
                                    _rightStickStateLeftReleased = false;
                                if (state.Value >= 50000)
                                    _rightStickStateRightReleased = false;

                                if (state.Value == 32767)
                                {
                                    if (!_rightStickStateLeftReleased)
                                    {
                                        _rightStickStateLeftReleased = true;

                                        ButtonHandler?.Invoke(this, new GamepadEventArgs
                                        {
                                            Data = state,
                                            Direction = StickDirection.Left,
                                            IsRightStick = true
                                        });
                                    }

                                    if (!_rightStickStateRightReleased)
                                    {
                                        _rightStickStateRightReleased = true;

                                        ButtonHandler?.Invoke(this, new GamepadEventArgs
                                        {
                                            Data = state,
                                            Direction = StickDirection.Right,
                                            IsRightStick = true
                                        });
                                    }
                                }
                            }

                            if (state.Offset == JoystickOffset.RotationZ)
                            {
                                if (state.Value <= 20000)
                                    _rightStickStateTopReleased = false;
                                if (state.Value >= 50000)
                                    _rightStickStateBottomReleased = false;

                                if (state.Value == 32767)
                                {
                                    if (!_rightStickStateTopReleased)
                                    {
                                        _rightStickStateTopReleased = true;

                                        ButtonHandler?.Invoke(this, new GamepadEventArgs
                                        {
                                            Data = state,
                                            Direction = StickDirection.Top,
                                            IsRightStick = true
                                        });
                                    }

                                    if (!_rightStickStateBottomReleased)
                                    {
                                        _rightStickStateBottomReleased = true;

                                        ButtonHandler?.Invoke(this, new GamepadEventArgs
                                        {
                                            Data = state,
                                            Direction = StickDirection.Bottom,
                                            IsRightStick = true
                                        });
                                    }
                                }
                            }

                            continue;

                        }

                        Console.WriteLine(state);
                    }

                    //var cs = _pad.GetCurrentState();
                    //Console.WriteLine(string.Join(", ", cs.PointOfViewControllers));
                    //Console.WriteLine("State: " + cs);

                    Thread.Sleep(50);
                }
                catch (SharpDXException ex)
                {
                    if (ex.Descriptor.ApiCode.Equals("InputLost", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_pad != null)
                            _pad.Unacquire();

                        inputLost = true;

                        Thread.Sleep(250);
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }
                catch (Exception ex)
                {
                    Thread.Sleep(500);
                }
            }
        }
    }
}
