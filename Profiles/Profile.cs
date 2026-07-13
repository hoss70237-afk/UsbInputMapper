using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public class Profile
    {
        public string Name { get; set; }
        
        // 複数の実行ファイル名を登録できるように変更 (例: "notepad.exe", "calc.exe")
        public List<string> TargetApplicationPaths { get; set; } 
        
        public bool IsDefault { get; set; }
        
        public List<Binding> Bindings { get; set; }

        public Profile()
        {
            TargetApplicationPaths = new List<string>();
            Bindings = new List<Binding>();
        }

        public override string ToString()
        {
            return Name + (IsDefault ? " (Default)" : "");
        }
    }
}
