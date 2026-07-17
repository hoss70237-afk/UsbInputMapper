using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UsbInputMapper.Profiles
{
    public enum TriggerCondition { Normal, Hold, RapidFire, Release }
    public class TriggerKey { public string DeviceIdentifier { get; set; } public int Type { get; set; } public int Code { get; set; } public override string ToString() => Binding.GetCodeName(Type, Code); }

    public class Binding
    {
        public string Name { get; set; }
        public string DeviceIdentifier { get; set; }
        public int InputType { get; set; } 
        public int InputCode { get; set; } 
        
        public List<TriggerKey> SubTriggers { get; set; }
        
        public TriggerCondition Condition { get; set; }
        public int ConditionParam { get; set; }
        public ActionDef Action { get; set; }
        public bool BlockOriginalInput { get; set; }

        // ★追加: アナログ＆トリガー用の詳細設定
        public int DeadZone { get; set; } = 15; 
        public bool InvertAxis { get; set; } = false;
        public int AxisRange { get; set; } = 0; // 0: フル軸, 1: 正の半軸, 2: 負の半軸
        public int AccelerationCurve { get; set; } = 0; // 0: リニア, 1: 早め, 2: 遅め

        public Binding()
        {
            Name = "新規アイテム";
            SubTriggers = new List<TriggerKey>();
            Condition = TriggerCondition.Normal;
            Action = new ActionDef();
        }

        public string GetTriggerString()
        {
            string mainTrigger = GetCodeName(InputType, InputCode);
            string sub = "";
            if (SubTriggers != null && SubTriggers.Count > 0) foreach (var t in SubTriggers) sub += t.ToString() + " + ";
            return $"{sub}{mainTrigger}";
        }

        public static string GetCodeName(int type, int code)
        {
            if (type == 1) return ((Keys)code).ToString();
            else if (type == 0) return $"MouseBtn:{code}";
            else if (type == 10) return $"Btn:{code}";
            else if (type == 11) return $"Axis:{code}";
            else if (type == 12) 
            {
                if (code == 0) return "POV 上";
                if (code == 9000) return "POV 右";
                if (code == 18000) return "POV 下";
                if (code == 27000) return "POV 左";
                return $"POV:{code}";
            }
            return "Unknown";
        }
    }
}
