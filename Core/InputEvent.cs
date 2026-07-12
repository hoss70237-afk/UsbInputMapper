using System;

namespace UsbInputMapper.Core
{
    public class InputEvent
    {
        public string DeviceIdentifier { get; set; }
        public int Type { get; set; } // 0:Mouse, 1:Keyboard, 2:HID
        
        // Keyboard
        public ushort VKey { get; set; }
        public bool IsKeyDown { get; set; }

        // Mouse
        public uint MouseButtonFlags { get; set; }
        public int MouseData { get; set; } // Wheel etc

        // HID
        public byte[] HidData { get; set; }

        public override string ToString()
        {
            if (Type == 1)
                return $"KB: {VKey} ({(IsKeyDown ? "Down" : "Up")})";
            else if (Type == 0)
                return $"MS: Flags={MouseButtonFlags}";
            else
                return $"HID: DataLen={(HidData != null ? HidData.Length : 0)}";
        }
    }
}
