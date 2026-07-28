using System;
using System.Collections.Generic;
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
        private volatile bool _isRunning;
        private volatile bool _refreshRequested;
        private AutoResetEvent _stopEvent = new AutoResetEvent(false);
        
        private class DeviceState 
        { 
            public Joystick Joystick { get; set; } 
            public string Identifier { get; set; } 
            public Dictionary<int, int> LastAxisValues { get; set; } = new Dictionary<int, int>();
            public AutoResetEvent NotificationEvent { get; set; }
        }
        
        private List<DeviceState> _devices = new List<DeviceState>();
        private DeviceState[] _activeDevicesCache = new DeviceState[0];
        
        // 【最適化】毎ループ発生していた配列のアロケーションを排除するためのキャッシュ
        private WaitHandle[] _waitHandlesCache;

        public bool HasAxisBindings { get; set; } = true;
        public bool ForceEnableAxisEvents { get; set; } = false;

        public DirectInputManager()
        {
            _waitHandlesCache = new WaitHandle[] { _stopEvent };

            try
            {
                _directInput = new DirectInput();
                _refreshRequested = true;
                _isRunning = true;
                _pollingThread = new Thread(EventWaitLoop) { IsBackground = true, Priority = ThreadPriority.Normal, Name = "DirectInputPollingThread" };
                _pollingThread.Start();
            }
            catch (Exception ex)
            {
                InputLogger.LogError("Failed to initialize DirectInputManager", ex);
            }
        }

        public void RefreshDevices()
        {
            _refreshRequested = true;
            _stopEvent.Set();
        }

        private void RebuildDevices()
        {
            foreach (var d in _devices) 
            { 
                try { d.Joystick.Unacquire(); } catch { }
                try { d.Joystick.Dispose(); } catch { }
                try { d.NotificationEvent.Dispose(); } catch { }
            }
            _devices.Clear();
            
            try
            {
                foreach (var instance in _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly))
                {
                    try
                    {
                        var joystick = new Joystick(_directInput, instance.InstanceGuid);
                        joystick.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                        joystick.Properties.BufferSize = 128;
                        
                        var notifyEvent = new AutoResetEvent(false);
                        joystick.SetNotification(notifyEvent);
                        joystick.Acquire();
                        
                        _devices.Add(new DeviceState { Joystick = joystick, Identifier = instance.InstanceGuid.ToString(), NotificationEvent = notifyEvent });
                    } 
                    catch (Exception ex)
                    {
                        InputLogger.LogError($"Failed to acquire device {instance.InstanceGuid}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                InputLogger.LogError("Error enumerating DirectInput devices", ex);
            }

            // 【最適化】デバイス更新時のみ配列キャッシュを再作成し、ループ内は無確保とする
            _activeDevicesCache = _devices.ToArray();
            
            var newWaitHandles = new WaitHandle[_devices.Count + 1];
            newWaitHandles[0] = _stopEvent;
            for (int i = 0; i < _devices.Count; i++)
            {
                newWaitHandles[i + 1] = _devices[i].NotificationEvent;
            }
            _waitHandlesCache = newWaitHandles;
        }

        private void EventWaitLoop()
        {
            while (_isRunning)
            {
                try
                {
                    if (_refreshRequested)
                    {
                        RebuildDevices();
                        _refreshRequested = false;
                    }

                    DeviceState[] activeDevices = _activeDevicesCache;
                    WaitHandle[] waitHandles = _waitHandlesCache;

                    if (activeDevices.Length == 0)
                    {
                        if (WaitHandle.WaitAny(new WaitHandle[] { _stopEvent }, 1000) == 0) break;
                        continue;
                    }

                    int waitResult = WaitHandle.WaitAny(waitHandles, 2000); 

                    if (!_isRunning) break;
                    if (waitResult == WaitHandle.WaitTimeout || waitResult == 0) continue;

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

                                if (type != -1) 
                                    OnInputEvent?.Invoke(this, new DirectInputEvent { DeviceIdentifier = d.Identifier, Type = type, Code = code, Value = value });
                            }
                        }
                        catch (SharpDXException e)
                        {
                            if (e.ResultCode == SharpDX.DirectInput.ResultCode.NotAcquired || e.ResultCode == SharpDX.DirectInput.ResultCode.InputLost)
                            {
                                try { d.Joystick.Acquire(); } catch { Thread.Sleep(100); }
                            }
                            else
                            {
                                InputLogger.LogError("DirectInput polling error", e);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    InputLogger.LogError("DirectInput EventWaitLoop unexpected error", ex);
                    Thread.Sleep(1000);
                }
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            _stopEvent.Set();
            _pollingThread?.Join(1000);
            
            foreach (var d in _devices) 
            { 
                try { d.Joystick.Unacquire(); } catch { } 
                try { d.Joystick.Dispose(); } catch { }
                try { d.NotificationEvent?.Dispose(); } catch { }
            } 
            _devices.Clear();
            _activeDevicesCache = new DeviceState[0];
            _waitHandlesCache = new WaitHandle[0];
            _stopEvent.Dispose();
            _directInput?.Dispose();
        }
    }
}
