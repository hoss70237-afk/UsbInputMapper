using System;

namespace UsbInputMapper.Core
{
    public class DeviceInfo
    {
        public IntPtr Handle { get; set; }
        public string DevicePath { get; set; }
        public int Type { get; set; } // 0:Mouse, 1:Keyboard, 2:HID
        public string VendorId { get; set; }
        public string ProductId { get; set; }
        public string Nickname { get; set; }

        public string GetIdentifier()
        {
            // デバイスを一意に識別するためのID
            return $"{VendorId}_{ProductId}_{DevicePath}";
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Nickname) ? $"Device [{VendorId}:{ProductId}]" : Nickname;
        }
    }
}
