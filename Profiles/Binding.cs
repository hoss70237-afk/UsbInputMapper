using System;

namespace UsbInputMapper.Profiles
{
    public enum TriggerCondition
    {
        Normal,
        Hold,
        RapidFire // 連打
    }

    public class Binding
    {
        // ユーザーが任意に付ける名前
        public string Name { get; set; }

        public string DeviceIdentifier { get; set; }
        
        // 0:Mouse, 1:Keyboard, 2:HID
        public int InputType { get; set; }
        public int InputCode { get; set; } 
        
        // 入力条件（長押し、連打など）
        public TriggerCondition Condition { get; set; }
        
        // 長押し時間(ms)や連打の判定回数など
        public int ConditionParam { get; set; }

        public ActionDef Action { get; set; }

        public Binding()
        {
            Name = "新規アイテム";
            Condition = TriggerCondition.Normal;
            ConditionParam = 0;
            Action = new ActionDef();
        }
    }
}
