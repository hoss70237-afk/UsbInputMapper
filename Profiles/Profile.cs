using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public class Profile
    {
        public string Name { get; set; }
        public List<string> TargetApplicationPaths { get; set; } 
        public bool IsDefault { get; set; }
        
        // ★追加: プロファイルごとのXInput有効・無効フラグ
        // 仮想コントローラー自体は切断せず、出力だけをスルーする。
        public bool EnableXInput { get; set; } = false;
        
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
