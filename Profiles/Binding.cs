using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UsbInputMapper.Profiles
{
    // ★追加: Sync (同期入力)
    public enum TriggerCondition { Normal, Hold, RapidFire, Release, Sync }
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

        public int DeadZone { get; set; } = 15; 
        public bool InvertAxis { get; set; } = false;
        public int AxisRange { get; set; } = 0; 
        public int AccelerationCurve { get; set; } = 0; 
        
        public string PlayWavPath { get; set; }

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
            if (type == 1) return "キーボード: " + ((Keys)code).ToString();
            else if (type == 0) return $"マウスボタン: {code}";
            else if (type == 2) return $"HIDボタン: {code}"; 
            else if (type == 10) return $"パッドボタン: {code}";
            else if (type == 11) return $"パッド軸: {code}";
            else if (type == 12) 
            {
                if (code == 0) return "POV 上";
                if (code == 9000) return "POV 右";
                if (code == 18000) return "POV 下";
                if (code == 27000) return "POV 左";
                return $"POV: {code}";
            }
            else if (type == 5)
            {
                string[] bNames = { "左上隅", "上辺(左)", "上辺(中)", "上辺(右)", "右上隅", "右辺(上)", "右辺(中)", "右辺(下)", "右下隅", "下辺(右)", "下辺(中)", "下辺(左)", "左下隅", "左辺(下)", "左辺(中)", "左辺(上)" };
                if (code >= 0 && code < bNames.Length) return "ベゼルタッチ: " + bNames[code];
                return "ベゼルタッチ: 不明";
            }
            return "不明";
        }
    }
}
