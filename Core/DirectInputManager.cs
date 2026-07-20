using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using SharpDX.DirectInput;

namespace UsbInputMapper.Core
{
    public struct DirectInputEvent
    {
        public string DeviceIdentifier;
        public int Type; // 10: Button, 11: Axis, 12: POV
        public int Code;
        public int Value; 
        public bool IsDown => (Type == 12) ? Value != -1 : Value > 0; 
    }

    public class DirectInputManager : IDisposable
    {
        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uMilliseconds);

        public event EventHandler<DirectInputEvent> OnInputEvent;
        private DirectInput _directInput;
        private Thread _pollingThread;
        private bool _isRunning;
        private IntPtr _hwnd; // ★バックグラウンド取得用ウィンドウハンドル
        
        private class DeviceState 
        { 
            public Joystick Joystick { get; set; } 
            public string Identifier { get; set; } 
            public Dictionary<int, int> LastAxisValues { get; set; } = new Dictionary<int, int>();
            public long NextAcquireTime { get; set; } = 0;
        }
        private List<DeviceState> _devices = new List<DeviceState>();

        public bool HasAxisBindings { get; set; } = true;
        public bool ForceEnableAxisEvents { get; set; } = false;

        public DirectInputManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
            timeBeginPeriod(1);

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
                foreach (var instance in _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
                {
                    try
                    {
                        var joystick = new Joystick(_directInput, instance.InstanceGuid);
                        joystick.Properties.BufferSize = 128;
                        // ★バックグラウンド時でも入力を取得するため CooperativeLevel を設定
                        joystick.SetCooperativeLevel(_hwnd, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                        joystick.Acquire();
                        _devices.Add(new DeviceState { Joystick = joystick, Identifier = instance.InstanceGuid.ToString() });
                    } catch (Exception ex) {
                        InputLogger.Log($"[DirectInput] Failed to acquire joystick: {ex.Message}");
                    }
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
                        if (Environment.TickCount < d.NextAcquireTime) continue; 

                        try
                        {
                            d.Joystick.Poll();
                            var datas = d.Joystick.GetBufferedData();
                            if (datas == null) continue;

                            foreach (var data in datas)
                            {
                                int type = -1, code = -1, value = data.Value;

                                if (data.Offset >= JoystickOffset.Buttons0 && data.Offset <= JoystickOffset.Buttons127)
                                {
                                    type = 10; code = data.Offset - JoystickOffset.Buttons0; value = (data.Value > 0) ? 1 : 0;
                                }
                                else if (data.Offset >= JoystickOffset.PointOfViewControllers0 && data.Offset <= JoystickOffset.PointOfViewControllers3)
                                {
                                    type = 12; code = data.Offset - JoystickOffset.PointOfViewControllers0; value = data.Value;
                                }
                                else
                                {
                                    type = 11;
                                    if (!HasAxisBindings && !ForceEnableAxisEvents) continue;

                                    switch (data.Offset)
                                    {
                                        case JoystickOffset.X: code = 0; break; case JoystickOffset.Y: code = 1; break;
                                        case JoystickOffset.Z: code = 2; break; case JoystickOffset.RotationX: code = 3; break;
                                        case JoystickOffset.RotationY: code = 4; break; case JoystickOffset.RotationZ: code = 5; break;
                                        case JoystickOffset.Sliders0: code = 6; break; case JoystickOffset.Sliders1: code = 7; break;
                                    }

                                    if (code != -1)
                                    {
                                        if (d.LastAxisValues.TryGetValue(code, out int lastVal))
                                        {
                                            if (Math.Abs(lastVal - value) < 150) continue; 
                                        }
                                        d.LastAxisValues[code] = value;
                                    }
                                }

                                if (type != -1) OnInputEvent?.Invoke(this, new DirectInputEvent { DeviceIdentifier = d.Identifier, Type = type, Code = code, Value = value });
                            }
                        }
                        catch (SharpDXException e)
                        {
                            if (e.ResultCode == SharpDX.DirectInput.ResultCode.NotAcquired || e.ResultCode == SharpDX.DirectInput.ResultCode.InputLost)
                            {
                                try { d.Joystick.Acquire(); } 
                                catch { d.NextAcquireTime = Environment.TickCount + 1000; } 
                            }
                        }
                        catch { d.NextAcquireTime = Environment.TickCount + 1000; }
                    }
                }
                Thread.Sleep(5); 
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _pollingThread?.Join(500);
            lock (_devices) { foreach (var d in _devices) d.Joystick.Dispose(); }
            _directInput?.Dispose();
            
            timeEndPeriod(1);
        }
    }
}
