using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UsbInputMapper.Core
{
    public class RawInputManager : NativeWindow, IDisposable
    {
        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        public event EventHandler<InputEvent> OnInputEvent;
        public event EventHandler OnDeviceChanged;

        private readonly ConcurrentDictionary<IntPtr, DeviceInfo> _devices = new ConcurrentDictionary<IntPtr, DeviceInfo>();
        private readonly ConcurrentDictionary<IntPtr, byte[]> _lastHidData = new ConcurrentDictionary<IntPtr, byte[]>();

        public RawInputManager()
        {
            CreateHandle(new CreateParams { Caption = "UsbInputMapper_RawInputMessageWindow", Parent = (IntPtr)(-3) });
            RegisterInputDevices();
        }

        private void RegisterInputDevices()
        {
            void TryRegister(ushort page, ushort usage)
            {
                var rid = new RawInputNative.RAWINPUTDEVICE[1];
                rid[0].usUsagePage = page;
                rid[0].usUsage = usage;
                rid[0].dwFlags = RawInputNative.RIDEV_INPUTSINK | RawInputNative.RIDEV_DEVNOTIFY;
                rid[0].hwndTarget = this.Handle;

                RawInputNative.RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(typeof(RawInputNative.RAWINPUTDEVICE)));
            }

            TryRegister(0x01, 0x02); // Mouse
            TryRegister(0x01, 0x06); // Keyboard
            TryRegister(0x0C, 0x01); // Consumer Control
            TryRegister(0x01, 0x05); // Gamepad
            TryRegister(0x01, 0x04); // Joystick
            TryRegister(0x01, 0x00); // Generic Desktop
            TryRegister(0xFF00, 0x01);
            TryRegister(0xFF00, 0x02);
            TryRegister(0xFF01, 0x01);
            TryRegister(0xFF01, 0x02);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == RawInputNative.WM_INPUT) ProcessRawInput(m.LParam);
            else if (m.Msg == RawInputNative.WM_INPUT_DEVICE_CHANGE)
            {
                OnDeviceChanged?.Invoke(this, EventArgs.Empty);
            }
            base.WndProc(ref m);
        }

        private void ProcessRawInput(IntPtr hRawInput)
        {
            uint dataSize = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RawInputNative.RAWINPUTHEADER));
            RawInputNative.GetRawInputData(hRawInput, RawInputNative.RID_INPUT, IntPtr.Zero, ref dataSize, headerSize);
            if (dataSize == 0) return;

            IntPtr pData = Marshal.AllocHGlobal((int)dataSize);
            try
            {
                if (RawInputNative.GetRawInputData(hRawInput, RawInputNative.RID_INPUT, pData, ref dataSize, headerSize) == dataSize)
                {
                    var header = (RawInputNative.RAWINPUTHEADER)Marshal.PtrToStructure(pData, typeof(RawInputNative.RAWINPUTHEADER));
                    var devInfo = GetOrAddDeviceInfo(header.hDevice);

                    InputEvent evt = new InputEvent 
                    { 
                        DeviceIdentifier = devInfo.GetIdentifier(), 
                        Type = (int)header.dwType,
                        Timestamp = (long)GetTickCount64()
                    };
                    
                    IntPtr pRawData = new IntPtr(pData.ToInt64() + headerSize);

                    if (header.dwType == RawInputNative.RIM_TYPEKEYBOARD)
                    {
                        var kb = (RawInputNative.RAWKEYBOARD)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWKEYBOARD));
                        evt.Code = kb.VKey;
                        evt.IsDown = (kb.Message == 0x0100 || kb.Message == 0x0104);
                        if (evt.Code == 255) return;
                        OnInputEvent?.Invoke(this, evt);
                    }
                    else if (header.dwType == RawInputNative.RIM_TYPEMOUSE)
                    {
                        var ms = (RawInputNative.RAWMOUSE)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWMOUSE));
                        
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0001, 0x0002, 1); 
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0004, 0x0008, 2); 
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0010, 0x0020, 3); 
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0040, 0x0080, 6); 
                        EmitMouseEvent(evt, ms.usButtonFlags, 0x0100, 0x0200, 7); 

                        // 垂直ホイール (0x0400)
                        if ((ms.usButtonFlags & 0x0400) != 0) 
                        {
                            short delta = ms.usButtonData;
                            evt.Code = delta > 0 ? 4 : 5; // 4:上, 5:下
                            evt.IsDown = true;
                            OnInputEvent?.Invoke(this, evt);
                        }
                        // 水平ホイール (0x0800)
                        else if ((ms.usButtonFlags & 0x0800) != 0)
                        {
                            short delta = ms.usButtonData;
                            evt.Code = delta > 0 ? 8 : 9; // 8:右, 9:左
                            evt.IsDown = true;
                            OnInputEvent?.Invoke(this, evt);
                        }
                    }
                    else if (header.dwType == RawInputNative.RIM_TYPEHID)
                    {
                        var hid = (RawInputNative.RAWHID)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWHID));
                        int size = (int)(hid.dwSizeHid * hid.dwCount);
                        
                        if (size > 0)
                        {
                            byte[] rawData = new byte[size];
                            IntPtr pHidData = new IntPtr(pRawData.ToInt64() + Marshal.SizeOf(typeof(RawInputNative.RAWHID)));
                            Marshal.Copy(pHidData, rawData, 0, size);

                            if (!_lastHidData.TryGetValue(header.hDevice, out byte[] lastData) || lastData.Length != size)
                            {
                                lastData = new byte[size];
                            }

                            for (int i = 0; i < size; i++)
                            {
                                if (rawData[i] != lastData[i])
                                {
                                    byte diff = (byte)(rawData[i] ^ lastData[i]);
                                    for (int b = 0; b < 8; b++)
                                    {
                                        if ((diff & (1 << b)) != 0)
                                        {
                                            int customCode = (i << 8) | b;
                                            bool isDown = (rawData[i] & (1 << b)) != 0;
                                            InputEvent hidEvt = new InputEvent 
                                            {
                                                DeviceIdentifier = evt.DeviceIdentifier, 
                                                Type = 2,
                                                Code = customCode, 
                                                IsDown = isDown, 
                                                HidData = rawData,
                                                Timestamp = evt.Timestamp
                                            };
                                            OnInputEvent?.Invoke(this, hidEvt);
                                        }
                                    }
                                }
                            }
                            
                            _lastHidData[header.hDevice] = (byte[])rawData.Clone();
                        }
                    }
                }
            }
            finally { Marshal.FreeHGlobal(pData); }
        }

        private void EmitMouseEvent(InputEvent baseEvt, uint currentFlags, uint downFlag, uint upFlag, int mappedCode)
        {
            if ((currentFlags & downFlag) != 0)
            {
                InputEvent evt = new InputEvent { DeviceIdentifier = baseEvt.DeviceIdentifier, Type = baseEvt.Type, Code = mappedCode, IsDown = true, Timestamp = baseEvt.Timestamp };
                OnInputEvent?.Invoke(this, evt);
            }
            else if ((currentFlags & upFlag) != 0)
            {
                InputEvent evt = new InputEvent { DeviceIdentifier = baseEvt.DeviceIdentifier, Type = baseEvt.Type, Code = mappedCode, IsDown = false, Timestamp = baseEvt.Timestamp };
                OnInputEvent?.Invoke(this, evt);
            }
        }

        private DeviceInfo GetOrAddDeviceInfo(IntPtr hDevice)
        {
            return _devices.GetOrAdd(hDevice, h => new DeviceInfo { Handle = hDevice });
        }

        public void Dispose() { DestroyHandle(); }
    }
}
