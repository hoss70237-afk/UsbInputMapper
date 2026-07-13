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
        
        public int InputType { get; set; } // 0:Mouse, 1:Keyboard
        public int InputCode { get; set; } 
        
        // ★追加: どのレイヤーにいる時だけ発動するか (0=通常, 1〜5=各レイヤー, -1=どこでも)
        public int TargetLayer { get; set; }

        // ★追加: 同時押しの条件 (0=なし, その他=一緒に押されている必要があるキーのInputCode)
        public int SubInputType { get; set; }
        public int SubInputCode { get; set; }

        public TriggerCondition Condition { get; set; }
        public int ConditionParam { get; set; }

        public ActionDef Action { get; set; }

        public Binding()
        {
            Name = "新規アイテム";
            TargetLayer = 0; // デフォルトは通常レイヤー
            SubInputCode = 0; // 同時押しなし
            Condition = TriggerCondition.Normal;
            ConditionParam = 0;
            Action = new ActionDef();
        }

        // リスト上で「Aキー」などの分かりやすい文字にするメソッド
        public string GetTriggerString()
        {
            string mainTrigger = GetCodeName(InputType, InputCode);
            string subTrigger = SubInputCode > 0 ? $" + {GetCodeName(SubInputType, SubInputCode)}" : "";
            string layerStr = TargetLayer > 0 ? $"[Layer{TargetLayer}] " : "";

            return $"{layerStr}{mainTrigger}{subTrigger}";
        }

        public static string GetCodeName(int type, int code)
        {
            if (type == 1) // Keyboard
            {
                return ((Keys)code).ToString();
            }
            else if (type == 0) // Mouse
            {
                switch (code)
                {
                    case 1: return "左クリック";
                    case 2: return "右クリック";
                    case 3: return "中クリック";
                    case 4: return "ホイール上";
                    case 5: return "ホイール下";
                    case 6: return "サイド進む(X1)";
                    case 7: return "サイド戻る(X2)";
                    default: return $"MouseBtn:{code}";
                }
            }
            return "Unknown";
        }
    }
}
