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
        public int Type; 
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
        private AutoResetEvent _stopEvent = new AutoResetEvent(false);
        
        private class DeviceState 
        { 
            public Joystick Joystick { get; set; } 
            public string Identifier { get; set; } 
            public Dictionary<int, int> LastAxisValues { get; set; } = new Dictionary<int, int>();
            public AutoResetEvent NotificationEvent { get; set; } // ★ イベント通知用
        }
        
        private List<DeviceState> _devices = new List<DeviceState>();
        private object _deviceLock = new object();

        public bool HasAxisBindings { get; set; } = true;
        public bool ForceEnableAxisEvents { get; set; } = false;

        public DirectInputManager()
        {
            _directInput = new DirectInput();
            RefreshDevices();
            _isRunning = true;
            _pollingThread = new Thread(EventWaitLoop) { IsBackground = true, Priority = ThreadPriority.Highest };
            _pollingThread.Start();
        }

        public void RefreshDevices()
        {
            lock (_deviceLock)
            {
                foreach (var d in _devices) 
                { 
                    try { d.Joystick.Unacquire(); } catch { }
                    d.Joystick.Dispose(); 
                    d.NotificationEvent.Dispose();
                }
                _devices.Clear();
                
                foreach (var instance in _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
                {
                    try
                    {
                        var joystick = new Joystick(_directInput, instance.InstanceGuid);
                        
                        // ★ バックグラウンドでの入力を取得するための協調レベル設定
                        joystick.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                        joystick.Properties.BufferSize = 128;
                        
                        var notifyEvent = new AutoResetEvent(false);
                        joystick.SetNotification(notifyEvent); // ★ コントローラーが入力された瞬間にシグナルを出す設定
                        joystick.Acquire();
                        
                        _devices.Add(new DeviceState { Joystick = joystick, Identifier = instance.InstanceGuid.ToString(), NotificationEvent = notifyEvent });
                    } catch { }
                }
                // デバイス構成が変わったら待機中のスレッドを起こしてWaitHandle配列を作り直させる
                _stopEvent.Set(); 
            }
        }

        // ★ Thread.Sleepを廃止し、OSからの入力シグナルを待機するゼロ負荷ループ
        private void EventWaitLoop()
        {
            while (_isRunning)
            {
                WaitHandle[] waitHandles;
                DeviceState[] activeDevices;
                
                lock (_deviceLock)
                {
                    activeDevices = _devices.ToArray();
                    waitHandles = new WaitHandle[activeDevices.Length + 1];
                    waitHandles[0] = _stopEvent; // 0番目は停止または再構築シグナル
                    for (int i = 0; i < activeDevices.Length; i++)
                    {
                        waitHandles[i + 1] = activeDevices[i].NotificationEvent;
                    }
                }

                // デバイスからの入力があるか、再構築シグナルが来るまでスレッドを完全にサスペンド（CPU 0%）
                int waitResult = WaitHandle.WaitAny(waitHandles, 2000); 

                if (!_isRunning) break;
                
                if (waitResult == WaitHandle.WaitTimeout)
                {
                    // タイムアウト時は何もしない。切断検知はホットプラグ側で行う
                    continue;
                }
                
                if (waitResult == 0)
                {
                    // 停止・再構築シグナル
                    continue;
                }

                // 1以上なら特定のコントローラーからの入力通知
                int deviceIndex = waitResult - 1;
                if (deviceIndex >= 0 && deviceIndex < activeDevices.Length)
                {
                    var d = activeDevices[deviceIndex];
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
                                    if (d.LastAxisValues.TryGetValue(code, out int lastVal)) { if (Math.Abs(lastVal - value) < 150) continue; }
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
                            try { d.Joystick.Acquire(); } catch { Thread.Sleep(100); }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _stopEvent.Set();
            _pollingThread?.Join(500);
            
            lock (_deviceLock) { 
                foreach (var d in _devices) { 
                    try { d.Joystick.Unacquire(); } catch { } 
                    d.Joystick.Dispose(); 
                    d.NotificationEvent.Dispose(); 
                } 
            }
            _stopEvent.Dispose();
            _directInput?.Dispose();
        }
    }
}
