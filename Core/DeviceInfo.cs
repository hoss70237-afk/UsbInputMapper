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
            string vid = string.IsNullOrEmpty(VendorId) ? "0000" : VendorId;
            string pid = string.IsNullOrEmpty(ProductId) ? "0000" : ProductId;
            string path = string.IsNullOrEmpty(DevicePath) ? Handle.ToString() : DevicePath;
            
            return $"{vid}_{pid}_{path}";
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Nickname)) return Nickname;
            return $"Device [{VendorId ?? "VID"}:{ProductId ?? "PID"}] ({Handle})";
        }
    }
}
