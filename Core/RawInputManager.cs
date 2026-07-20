using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UsbInputMapper.Core
{
    public class RawInputManager : NativeWindow, IDisposable
    {
        public event EventHandler<InputEvent> OnInputEvent;
        public event EventHandler OnDeviceChanged;

        private readonly Dictionary<IntPtr, DeviceInfo> _devices = new Dictionary<IntPtr, DeviceInfo>();
        private readonly Dictionary<IntPtr, byte[]> _lastHidData = new Dictionary<IntPtr, byte[]>();

        public RawInputManager()
        {
            // ★ 一番最初のコードと完全一致のハンドル作成方法に戻しました
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
                rid[0].dwFlags = RawInputNative.RIDEV_INPUTSINK | RawInputNative.RIDEV_DEVNOTIFY; // WM_INPUT_DEVICE_CHANGE受信用
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

                    InputEvent evt = new InputEvent { DeviceIdentifier = devInfo.GetIdentifier(), Type = (int)header.dwType };
                    IntPtr pRawData = new IntPtr(pData.ToInt64() + (IntPtr.Size == 8 ? 24 : 16));

                    if (header.dwType == RawInputNative.RIM_TYPEKEYBOARD)
                    {
                        var kb = (RawInputNative.RAWKEYBOARD)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWKEYBOARD));
                        evt.VKey = kb.VKey;
                        evt.IsKeyDown = (kb.Message == 0x0100 || kb.Message == 0x0104);
                        if (evt.VKey == 255) return;
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

                        if ((ms.usButtonFlags & 0x0400) != 0) 
                        {
                            evt.MouseButtonFlags = (uint)(ms.usButtonData > 0 ? 4 : 5);
                            evt.IsKeyDown = true;
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
                                                DeviceIdentifier = evt.DeviceIdentifier, Type = 2,
                                                MouseButtonFlags = (uint)customCode, IsKeyDown = isDown, HidData = rawData
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

        private void EmitMouseEvent(InputEvent baseEvt, uint currentFlags, uint downFlag, uint upFlag, uint mappedCode)
        {
            if ((currentFlags & downFlag) != 0)
            {
                InputEvent evt = new InputEvent { DeviceIdentifier = baseEvt.DeviceIdentifier, Type = baseEvt.Type, MouseButtonFlags = mappedCode, IsKeyDown = true };
                OnInputEvent?.Invoke(this, evt);
            }
            else if ((currentFlags & upFlag) != 0)
            {
                InputEvent evt = new InputEvent { DeviceIdentifier = baseEvt.DeviceIdentifier, Type = baseEvt.Type, MouseButtonFlags = mappedCode, IsKeyDown = false };
                OnInputEvent?.Invoke(this, evt);
            }
        }

        private DeviceInfo GetOrAddDeviceInfo(IntPtr hDevice)
        {
            if (!_devices.TryGetValue(hDevice, out var info))
            {
                info = new DeviceInfo { Handle = hDevice };
                _devices[hDevice] = info;
            }
            return info;
        }

        public void Dispose() { DestroyHandle(); }
    }
}
