using System;

namespace UsbInputMapper.Profiles
{
    public class Binding
    {
        public string DeviceIdentifier { get; set; }
        
        // 0:Mouse, 1:Keyboard, 2:HID
        public int InputType { get; set; }
        
        // 押されたキーコードやボタンのフラグなど
        public int InputCode { get; set; } 
        
        public ActionDef Action { get; set; }

        public Binding()
        {
            Action = new ActionDef();
        }
    }
}
