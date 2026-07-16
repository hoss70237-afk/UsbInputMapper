using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SharpDX; // ★追加: SharpDXException を処理するために必要
using SharpDX.DirectInput;

namespace UsbInputMapper.Core
{
    public class DirectInputEvent
    {
        public string DeviceIdentifier { get; set; }
        public int Type { get; set; } // 10: DInput Button, 11: DInput Axis
        public int Code { get; set; } // Button Index or Axis Index (0:X, 1:Y, 2:Z, 3:Rx, 4:Ry, 5:Rz, 6:Slider0, 7:Slider1)
        public int Value { get; set; } 
        public bool IsDown => Value > 0; 
    }

    public class DirectInputManager : IDisposable
    {
        public event EventHandler<DirectInputEvent> OnInputEvent;
        private DirectInput _directInput;
        private Thread _pollingThread;
        private bool _isRunning;
        
        private class DeviceState
        {
            public Joystick Joystick { get; set; }
            public string Identifier { get; set; }
        }

        private List<DeviceState> _devices = new List<DeviceState>();

        public DirectInputManager()
        {
            _directInput = new DirectInput();
            RefreshDevices();
            _isRunning = true;
            _pollingThread = new Thread(PollLoop) { IsBackground = true, Priority = ThreadPriority.Highest };
            _pollingThread.Start();
        }

        public void RefreshDevices()
        {
            lock (_devices)
            {
                foreach (var d in _devices) d.Joystick.Dispose();
                _devices.Clear();

                var instances = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly).ToList();
                
                foreach (var instance in instances)
                {
                    try
                    {
                        var joystick = new Joystick(_directInput, instance.InstanceGuid);
                        joystick.Properties.BufferSize = 128;
                        joystick.Acquire();
                        
                        _devices.Add(new DeviceState 
                        { 
                            Joystick = joystick, 
                            Identifier = instance.InstanceGuid.ToString()
                        });
                    }
                    catch { }
                }
            }
        }

        private void PollLoop()
        {
            while (_isRunning)
            {
                lock (_devices)
                {
                    foreach (var d in _devices)
                    {
                        try
                        {
                            d.Joystick.Poll();
                            var datas = d.Joystick.GetBufferedData();
                            if (datas == null) continue;

                            foreach (var data in datas)
                            {
                                int type = -1;
                                int code = -1;
                                int value = data.Value;

                                if (data.Offset >= JoystickOffset.Buttons0 && data.Offset <= JoystickOffset.Buttons127)
                                {
                                    type = 10;
                                    code = data.Offset - JoystickOffset.Buttons0;
                                    value = (data.Value > 0) ? 1 : 0;
                                }
                                else
                                {
                                    type = 11;
                                    switch (data.Offset)
                                    {
                                        case JoystickOffset.X: code = 0; break;
                                        case JoystickOffset.Y: code = 1; break;
                                        case JoystickOffset.Z: code = 2; break;
                                        case JoystickOffset.RotationX: code = 3; break;
                                        case JoystickOffset.RotationY: code = 4; break;
                                        case JoystickOffset.RotationZ: code = 5; break;
                                        case JoystickOffset.Sliders0: code = 6; break;
                                        case JoystickOffset.Sliders1: code = 7; break;
                                    }
                                }

                                if (type != -1)
                                {
                                    OnInputEvent?.Invoke(this, new DirectInputEvent
                                    {
                                        DeviceIdentifier = d.Identifier,
                                        Type = type,
                                        Code = code,
                                        Value = value
                                    });
                                }
                            }
                        }
                        catch (SharpDXException e)
                        {
                            // ウィンドウフォーカスが外れた際などに発生する InputLost を検知し、再取得する
                            if (e.ResultCode == SharpDX.DirectInput.ResultCode.NotAcquired || e.ResultCode == SharpDX.DirectInput.ResultCode.InputLost)
                            {
                                try { d.Joystick.Acquire(); } catch { }
                            }
                        }
                        catch { }
                    }
                }
                Thread.Sleep(1); 
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _pollingThread?.Join(500);
            lock (_devices)
            {
                foreach (var d in _devices) d.Joystick.Dispose();
            }
            _directInput?.Dispose();
        }
    }
}
