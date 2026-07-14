using System;
using System.Windows.Forms;

namespace UsbInputMapper.Profiles
{
    public enum TriggerCondition
    {
        Normal,
        Hold,
        RapidFire
    }

    public class Binding
    {
        public string Name { get; set; }
        public string DeviceIdentifier { get; set; }
        
        // 0:Mouse, 1:Keyboard, 2:HID(独自ボタン)
        public int InputType { get; set; } 
        public int InputCode { get; set; } 
        
        public int TargetLayer { get; set; }
        public int SubInputType { get; set; }
        public int SubInputCode { get; set; }

        public TriggerCondition Condition { get; set; }
        public int ConditionParam { get; set; }

        public ActionDef Action { get; set; }

        public Binding()
        {
            Name = "新規アイテム";
            TargetLayer = 0;
            SubInputCode = 0;
            Condition = TriggerCondition.Normal;
            ConditionParam = 0;
            Action = new ActionDef();
        }

        public string GetTriggerString()
        {
            string mainTrigger = GetCodeName(InputType, InputCode);
            string subTrigger = SubInputCode > 0 ? $" + {GetCodeName(SubInputType, SubInputCode)}" : "";
            string layerStr = TargetLayer > 0 ? $"[Layer{TargetLayer}] " : "";

            return $"{layerStr}{mainTrigger}{subTrigger}";
        }

        public static string GetCodeName(int type, int code)
        {
            if (type == 1) 
            {
                return ((Keys)code).ToString();
            }
            else if (type == 0) 
            {
                switch (code)
                {
                    case 1: return "左クリック"; case 2: return "右クリック"; case 3: return "中クリック";
                    case 4: return "ホイール上"; case 5: return "ホイール下";
                    case 6: return "サイド進む(X1)"; case 7: return "サイド戻る(X2)";
                    default: return $"MouseBtn:{code}";
                }
            }
            else if (type == 2)
            {
                // バイトのインデックスとビットのインデックスに復元して表示
                int byteIndex = code >> 8;
                int bitIndex = code & 0xFF;
                return $"HID特殊ボタン (Byte {byteIndex}, Bit {bitIndex})";
            }
            return "Unknown";
        }
    }
}
