using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public class Profile
    {
        public string Name { get; set; }
        public List<string> TargetApplicationPaths { get; set; } 
        public bool IsDefault { get; set; }
        
        public bool EnableXInput { get; set; } = false;
        public bool NotifyProfileChangeVibration { get; set; } = false; // ★追加: 振動通知
        
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
