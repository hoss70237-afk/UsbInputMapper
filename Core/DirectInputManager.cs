using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SharpDX;
using SharpDX.DirectInput;

namespace UsbInputMapper.Core
{
    public struct DirectInputEvent
    {
        public string DeviceIdentifier;
        public int Type; // 10: Button, 11: Axis, 12: POV(十字キー)
        public int Code;
        public int Value; 
        public bool IsDown => (Type == 12) ? Value != -1 : Value > 0; 
    }

    public class DirectInputManager : IDisposable
    {
        public event EventHandler<DirectInputEvent> OnInputEvent;
        private DirectInput _directInput;
        private Thread _pollingThread;
        private bool _isRunning;
        private class DeviceState { public Joystick Joystick { get; set; } public string Identifier { get; set; } }
        private List<DeviceState> _devices = new List<DeviceState>();

        // ★追加: 軸イベントの送信を制御するフラグ
        public bool HasAxisBindings { get; set; } = true;
        public bool ForceEnableAxisEvents { get; set; } = false;
        
        // ★追加: ジッター（微細な揺れ）フィルター用の前回値保存
        private Dictionary<string, int> _lastAxisValues = new Dictionary<string, int>();

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
                foreach (var instance in _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
                {
                    try
                    {
                        var joystick = new Joystick(_directInput, instance.InstanceGuid);
                        joystick.Properties.BufferSize = 128;
                        joystick.Acquire();
                        _devices.Add(new DeviceState { Joystick = joystick, Identifier = instance.InstanceGuid.ToString() });
                    } catch { }
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

                                    // ★追加: スティック割り当てが無い場合は完全にイベントを無視する（設定画面を開いている時を除く）
                                    if (!HasAxisBindings && !ForceEnableAxisEvents) continue;

                                    switch (data.Offset)
                                    {
                                        case JoystickOffset.X: code = 0; break; case JoystickOffset.Y: code = 1; break;
                                        case JoystickOffset.Z: code = 2; break; case JoystickOffset.RotationX: code = 3; break;
                                        case JoystickOffset.RotationY: code = 4; break; case JoystickOffset.RotationZ: code = 5; break;
                                        case JoystickOffset.Sliders0: code = 6; break; case JoystickOffset.Sliders1: code = 7; break;
                                    }

                                    // ★追加: ジッターフィルター（150未満の微小な値の揺れはノイズとして弾く）
                                    if (code != -1)
                                    {
                                        string axisKey = d.Identifier + "_" + code;
                                        if (_lastAxisValues.TryGetValue(axisKey, out int lastVal))
                                        {
                                            if (Math.Abs(lastVal - value) < 150) continue; 
                                        }
                                        _lastAxisValues[axisKey] = value;
                                    }
                                }

                                if (type != -1) OnInputEvent?.Invoke(this, new DirectInputEvent { DeviceIdentifier = d.Identifier, Type = type, Code = code, Value = value });
                            }
                        }
                        catch (SharpDXException e)
                        {
                            if (e.ResultCode == SharpDX.DirectInput.ResultCode.NotAcquired || e.ResultCode == SharpDX.DirectInput.ResultCode.InputLost)
                                try { d.Joystick.Acquire(); } catch { }
                        }
                        catch { }
                    }
                }
                // ★修正: 1msだと過剰にループが回るため、一般的なパッドのポーリングレート(200Hz)と同等の 5ms に緩和
                Thread.Sleep(5); 
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _pollingThread?.Join(500);
            lock (_devices) { foreach (var d in _devices) d.Joystick.Dispose(); }
            _directInput?.Dispose();
        }
    }
}
