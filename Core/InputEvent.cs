using System;

namespace UsbInputMapper.Core
{
    public class InputEvent
    {
        public string DeviceIdentifier { get; set; }
        public int Type { get; set; } // 0:Mouse, 1:Keyboard, 2:HID, 10:PadBtn, 11:PadAxis, 12:POV, 5:Bezel
        
        public int Code { get; set; }
        public int Value { get; set; }
        public bool IsDown { get; set; }

        public int X { get; set; } 
        public int Y { get; set; }
        public byte[] HidData { get; set; }

        public long Timestamp { get; set; }

        // ラッパープロパティ
        public ushort VKey 
        { 
            get => (ushort)Code; 
            set => Code = value; 
        }
        
        public uint MouseButtonFlags 
        { 
            get => (uint)Code; 
            set => Code = (int)value; 
        }
        
        public bool IsKeyDown 
        { 
            get => IsDown; 
            set => IsDown = value; 
        }

        public override string ToString()
        {
            if (Type == 1)
                return $"KB: {(System.Windows.Forms.Keys)Code} ({(IsDown ? "Down" : "Up")})";
            else if (Type == 0)
                return $"MS: Code={Code} ({(IsDown ? "Down" : "Up")}) Pos=({X},{Y})";
            else if (Type == 5)
                return $"Bezel: Zone={Code} ({(IsDown ? "Down" : "Up")})";
            else if (Type == 2)
                return $"HID: Code={Code} Len={(HidData != null ? HidData.Length : 0)}";
            else
                return $"Pad: Type={Type} Code={Code} Val={Value}";
        }
    }
}
