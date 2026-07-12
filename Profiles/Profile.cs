using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public class Profile
    {
        public string Name { get; set; }
        
        // 対象の実行ファイル名(例: "notepad.exe")
        public string TargetApplicationPath { get; set; } 
        
        public bool IsDefault { get; set; }
        
        public List<Binding> Bindings { get; set; }

        public Profile()
        {
            Bindings = new List<Binding>();
        }

        public override string ToString()
        {
            return Name + (IsDefault ? " (Default)" : "");
        }
    }
}
