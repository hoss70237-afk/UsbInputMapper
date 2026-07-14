using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UsbInputMapper.Core
{
    public class RawInputManager : NativeWindow, IDisposable
    {
        public event EventHandler<InputEvent> OnInputEvent;
        private readonly Dictionary<IntPtr, DeviceInfo> _devices = new Dictionary<IntPtr, DeviceInfo>();
        
        // HIDデバイスの前回の生データを保持して変化を検知する
        private readonly Dictionary<IntPtr, byte[]> _lastHidData = new Dictionary<IntPtr, byte[]>();

        public RawInputManager()
        {
            CreateHandle(new CreateParams { Caption = "UsbInputMapper_RawInputMessageWindow", Parent = (IntPtr)(-3) });
            RegisterInputDevices();
        }

        private void RegisterInputDevices()
        {
            var rid = new RawInputNative.RAWINPUTDEVICE[3];
            for (int i = 0; i < 3; i++)
            {
                rid[i].usUsagePage = 0x01;
                rid[i].dwFlags = RawInputNative.RIDEV_INPUTSINK | RawInputNative.RIDEV_DEVNOTIFY;
                rid[i].hwndTarget = this.Handle;
            }
            rid[0].usUsage = 0x02; // Mouse
            rid[1].usUsage = 0x06; // Keyboard
            rid[2].usUsage = 0x00; // Generic HID (特殊ボタンマウス等の生データを全て拾うためにUsage 0を指定)

            RawInputNative.RegisterRawInputDevices(rid, 3, (uint)Marshal.SizeOf(typeof(RawInputNative.RAWINPUTDEVICE)));
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == RawInputNative.WM_INPUT) ProcessRawInput(m.LParam);
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
                        EmitMouseEvent(evt, ms.usFlags, 0x0001, 0x0002, 1);
                        EmitMouseEvent(evt, ms.usFlags, 0x0004, 0x0008, 2);
                        EmitMouseEvent(evt, ms.usFlags, 0x0010, 0x0020, 3);
                        EmitMouseEvent(evt, ms.usFlags, 0x0040, 0x0080, 6);
                        EmitMouseEvent(evt, ms.usFlags, 0x0100, 0x0200, 7);

                        if ((ms.usFlags & 0x0400) != 0)
                        {
                            short wheelData = (short)ms.ulButtons;
                            evt.MouseButtonFlags = (uint)(wheelData > 0 ? 4 : 5);
                            evt.IsKeyDown = true;
                            OnInputEvent?.Invoke(this, evt);
                        }
                    }
                    else if (header.dwType == RawInputNative.RIM_TYPEHID)
                    {
                        var hid = (RawInputNative.RAWHID)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWHID));
                        int size = (int)(hid.dwSizeHid * hid.dwCount);
                        byte[] rawData = new byte[size];
                        IntPtr pHidData = new IntPtr(pRawData.ToInt64() + Marshal.SizeOf(typeof(RawInputNative.RAWHID)));
                        Marshal.Copy(pHidData, rawData, 0, size);

                        // 特殊ボタンの変化を検知
                        if (_lastHidData.TryGetValue(header.hDevice, out byte[] lastData) && lastData.Length == size)
                        {
                            for (int i = 0; i < size; i++)
                            {
                                if (rawData[i] != lastData[i])
                                {
                                    // 変化があったバイトのインデックスと値を組み合わせて一意のInputCodeを作る
                                    int customCode = (i << 8) | rawData[i];
                                    
                                    evt.Type = 2; // Type=2 は特殊HIDボタン
                                    evt.MouseButtonFlags = (uint)customCode;
                                    // 値が0に戻ったらキーアップとみなす簡易判定
                                    evt.IsKeyDown = (rawData[i] != 0);
                                    
                                    OnInputEvent?.Invoke(this, evt);
                                    break; // 同時に複数ボタンが押された場合は先頭を優先
                                }
                            }
                        }
                        _lastHidData[header.hDevice] = rawData;
                    }
                }
            }
            finally { Marshal.FreeHGlobal(pData); }
        }

        private void EmitMouseEvent(InputEvent baseEvt, uint currentFlags, uint downFlag, uint upFlag, uint mappedCode)
        {
            if ((currentFlags & downFlag) != 0)
            {
                baseEvt.MouseButtonFlags = mappedCode;
                baseEvt.IsKeyDown = true;
                OnInputEvent?.Invoke(this, baseEvt);
            }
            else if ((currentFlags & upFlag) != 0)
            {
                baseEvt.MouseButtonFlags = mappedCode;
                baseEvt.IsKeyDown = false;
                OnInputEvent?.Invoke(this, baseEvt);
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
