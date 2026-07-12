using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace UsbInputMapper.Core
{
    public class RawInputManager : NativeWindow, IDisposable
    {
        public event EventHandler<InputEvent> OnInputEvent;
        public event EventHandler OnDeviceChanged;

        private readonly Dictionary<IntPtr, DeviceInfo> _devices = new Dictionary<IntPtr, DeviceInfo>();

        public RawInputManager()
        {
            CreateHandle(new CreateParams
            {
                Caption = "UsbInputMapper_RawInputMessageWindow",
                Style = 0,
                ExStyle = 0,
                ClassStyle = 0,
                Parent = (IntPtr)(-3) // HWND_MESSAGE
            });

            RegisterInputDevices();
        }

        private void RegisterInputDevices()
        {
            var rid = new RawInputNative.RAWINPUTDEVICE[3];

            // Mouse
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02;
            rid[0].dwFlags = RawInputNative.RIDEV_INPUTSINK | RawInputNative.RIDEV_DEVNOTIFY;
            rid[0].hwndTarget = this.Handle;

            // Keyboard
            rid[1].usUsagePage = 0x01;
            rid[1].usUsage = 0x06;
            rid[1].dwFlags = RawInputNative.RIDEV_INPUTSINK | RawInputNative.RIDEV_DEVNOTIFY;
            rid[1].hwndTarget = this.Handle;

            // Gamepad (Generic HID)
            rid[2].usUsagePage = 0x01;
            rid[2].usUsage = 0x05;
            rid[2].dwFlags = RawInputNative.RIDEV_INPUTSINK | RawInputNative.RIDEV_DEVNOTIFY;
            rid[2].hwndTarget = this.Handle;

            if (!RawInputNative.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RawInputNative.RAWINPUTDEVICE))))
            {
                throw new Exception("Failed to register RawInput devices.");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == RawInputNative.WM_INPUT)
            {
                ProcessRawInput(m.LParam);
            }
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

                    InputEvent inputEvent = new InputEvent
                    {
                        DeviceIdentifier = devInfo.GetIdentifier(),
                        Type = (int)header.dwType
                    };

                    bool is64Bit = IntPtr.Size == 8;
                    int dataOffset = is64Bit ? 24 : 16;
                    IntPtr pRawData = new IntPtr(pData.ToInt64() + dataOffset);

                    if (header.dwType == RawInputNative.RIM_TYPEKEYBOARD)
                    {
                        var kb = (RawInputNative.RAWKEYBOARD)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWKEYBOARD));
                        inputEvent.VKey = kb.VKey;
                        inputEvent.IsKeyDown = (kb.Message == 0x0100 || kb.Message == 0x0104); // WM_KEYDOWN or WM_SYSKEYDOWN
                    }
                    else if (header.dwType == RawInputNative.RIM_TYPEMOUSE)
                    {
                        var ms = (RawInputNative.RAWMOUSE)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWMOUSE));
                        inputEvent.MouseButtonFlags = ms.usFlags;
                        // For wheel data parsing, we need to inspect ulButtons. Omitting complex parsing for brevity but storing flags.
                        inputEvent.MouseData = (int)ms.ulButtons;
                    }
                    else if (header.dwType == RawInputNative.RIM_TYPEHID)
                    {
                        var hid = (RawInputNative.RAWHID)Marshal.PtrToStructure(pRawData, typeof(RawInputNative.RAWHID));
                        int size = (int)(hid.dwSizeHid * hid.dwCount);
                        IntPtr pHidData = new IntPtr(pRawData.ToInt64() + Marshal.SizeOf(typeof(RawInputNative.RAWHID)));
                        byte[] rawData = new byte[size];
                        Marshal.Copy(pHidData, rawData, 0, size);
                        inputEvent.HidData = rawData;
                    }

                    OnInputEvent?.Invoke(this, inputEvent);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }
        }

        private DeviceInfo GetOrAddDeviceInfo(IntPtr hDevice)
        {
            if (_devices.TryGetValue(hDevice, out DeviceInfo info))
            {
                return info;
            }

            info = new DeviceInfo { Handle = hDevice };
            
            uint size = 0;
            RawInputNative.GetRawInputDeviceInfo(hDevice, RawInputNative.RIDI_DEVICENAME, IntPtr.Zero, ref size);
            if (size > 0)
            {
                IntPtr pName = Marshal.AllocHGlobal((int)size * 2);
                try
                {
                    RawInputNative.GetRawInputDeviceInfo(hDevice, RawInputNative.RIDI_DEVICENAME, pName, ref size);
                    info.DevicePath = Marshal.PtrToStringUni(pName);
                }
                finally
                {
                    Marshal.FreeHGlobal(pName);
                }
            }

            size = (uint)Marshal.SizeOf(typeof(RawInputNative.RID_DEVICE_INFO));
            IntPtr pInfo = Marshal.AllocHGlobal((int)size);
            try
            {
                var ridInfo = new RawInputNative.RID_DEVICE_INFO();
                ridInfo.cbSize = size;
                Marshal.StructureToPtr(ridInfo, pInfo, false);

                RawInputNative.GetRawInputDeviceInfo(hDevice, RawInputNative.RIDI_DEVICEINFO, pInfo, ref size);
                ridInfo = (RawInputNative.RID_DEVICE_INFO)Marshal.PtrToStructure(pInfo, typeof(RawInputNative.RID_DEVICE_INFO));
                
                info.Type = (int)ridInfo.dwType;
                if (ridInfo.dwType == RawInputNative.RIM_TYPEHID)
                {
                    info.VendorId = ridInfo.hid.dwVendorId.ToString("X4");
                    info.ProductId = ridInfo.hid.dwProductId.ToString("X4");
                }
                else if (info.DevicePath != null && info.DevicePath.Contains("VID_"))
                {
                    // Fallback to parse VID/PID from device path
                    try
                    {
                        var parts = info.DevicePath.Split('#');
                        if (parts.Length > 1)
                        {
                            string idPart = parts[1];
                            var subs = idPart.Split('&');
                            foreach (var sub in subs)
                            {
                                if (sub.StartsWith("VID_")) info.VendorId = sub.Substring(4, 4);
                                if (sub.StartsWith("PID_")) info.ProductId = sub.Substring(4, 4);
                            }
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pInfo);
            }

            _devices[hDevice] = info;
            return info;
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }
}
