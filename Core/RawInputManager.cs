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
            rid[2].usUsage = 0x05; // Gamepad

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
                        if (evt.VKey == 255) return; // 無効キー無視
                        OnInputEvent?.Invoke(this, evt);
                    }
                    else if (header.dwType == RawInputNative.RIM_TYPEMOUSE)
                    {
                        var ms = (RawInputNative.RAWMOUSE)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWMOUSE));
                        
                        // マウスボタンを正規化された InputCode に変換して発行
                        EmitMouseEvent(evt, ms.usFlags, 0x0001, 0x0002, 1); // Left
                        EmitMouseEvent(evt, ms.usFlags, 0x0004, 0x0008, 2); // Right
                        EmitMouseEvent(evt, ms.usFlags, 0x0010, 0x0020, 3); // Middle
                        EmitMouseEvent(evt, ms.usFlags, 0x0040, 0x0080, 6); // XBtn1
                        EmitMouseEvent(evt, ms.usFlags, 0x0100, 0x0200, 7); // XBtn2

                        // ホイール (ダウン・アップがないので Down として送る)
                        if ((ms.usFlags & 0x0400) != 0)
                        {
                            short wheelData = (short)ms.ulButtons;
                            evt.MouseButtonFlags = (uint)(wheelData > 0 ? 4 : 5);
                            evt.IsKeyDown = true;
                            OnInputEvent?.Invoke(this, evt);
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
