using System;

namespace UsbInputMapper.Core
{
    public class InputEvent
    {
        public string DeviceIdentifier { get; set; }
        public int Type { get; set; } // 0:Mouse, 1:Keyboard, 2:HID, 10:PadBtn, 11:PadAxis, 12:POV, 5:Bezel
        
        // VKeyやMouseButtonFlagsなどを一元管理するためのプロパティ
        public int Code { get; set; }
        
        // AxisやPOVの値用
        public int Value { get; set; }
        
        public bool IsDown { get; set; }

        public int X { get; set; } 
        public int Y { get; set; }
        public byte[] HidData { get; set; }

        // OSの起動ミリ秒（24.9日オーバーフロー対策済み）
        public long Timestamp { get; set; }

        // 後方互換性と利便性のためのラッパープロパティ
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
                return $"KB: {Code} ({(IsDown ? "Down" : "Up")})";
            else if (Type == 0)
                return $"MS: Code={Code} ({(IsDown ? "Down" : "Up")})";
            else if (Type == 2)
                return $"HID: DataLen={(HidData != null ? HidData.Length : 0)}";
            else
                return $"Pad: Type={Type} Code={Code} Val={Value}";
        }
    }
}
