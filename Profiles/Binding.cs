using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UsbInputMapper.Profiles
{
    public enum TriggerCondition { Normal, Hold, RapidFire, Release }

    public class TriggerKey
    {
        public string DeviceIdentifier { get; set; }
        public int Type { get; set; }
        public int Code { get; set; }
        public override string ToString() => Binding.GetCodeName(Type, Code);
    }

    public class Binding
    {
        public string Name { get; set; }
        public string DeviceIdentifier { get; set; }
        public int InputType { get; set; } // 10:DInputBtn, 11:DInputAxis
        public int InputCode { get; set; } 
        
        public List<TriggerKey> SubTriggers { get; set; }
        
        public TriggerCondition Condition { get; set; }
        public int ConditionParam { get; set; }
        public ActionDef Action { get; set; }
        
        public bool BlockOriginalInput { get; set; }
        
        // ★追加: アナログ入力の詳細設定
        public int DeadZone { get; set; } = 10;  // 0-50%
        public bool InvertAxis { get; set; } = false;

        public Binding()
        {
            Name = "新規アイテム";
            SubTriggers = new List<TriggerKey>();
            Condition = TriggerCondition.Normal;
            ConditionParam = 0;
            Action = new ActionDef();
            BlockOriginalInput = false;
        }

        public string GetTriggerString()
        {
            string mainTrigger = GetCodeName(InputType, InputCode);
            string sub = "";
            if (SubTriggers != null && SubTriggers.Count > 0)
                foreach (var t in SubTriggers) sub += t.ToString() + " + ";
            return $"{sub}{mainTrigger}";
        }

        public static string GetCodeName(int type, int code)
        {
            if (type == 1) return ((Keys)code).ToString();
            else if (type == 0) return $"MouseBtn:{code}";
            else if (type == 2) return $"HID:{code}";
            else if (type == 10) return $"DI_Btn:{code}";
            else if (type == 11) return $"DI_Axis:{code}";
            return "Unknown";
        }
    }
}
