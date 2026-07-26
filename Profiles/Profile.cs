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
        
        // 切替時通知
        public bool NotifyProfileChangeVibration { get; set; } = false;
        public bool NotifyProfileChangeBeep { get; set; } = false;
        public bool NotifyProfileChangeTTS { get; set; } = false;
        
        // 個別チャタリング防止
        public bool OverrideGlobalChattering { get; set; } = false;
        public bool EnableChatteringCanceler { get; set; } = false;
        public int ChatteringThresholdMs { get; set; } = 20;
        
        // OSD通知
        public bool OverlayShowMark { get; set; } = true;
        public bool OverlayShowName { get; set; } = true;
        public int OverlayPosX { get; set; } = -1; // -1: デフォルト(右上)
        public int OverlayPosY { get; set; } = -1;
        public int OverlayDurationMs { get; set; } = 2000;

        public List<Binding> Bindings { get; set; }

        public Profile()
        {
            Name = "新規プロファイル";
            TargetApplicationPaths = new List<string>();
            Bindings = new List<Binding>();
        }

        public override string ToString()
        {
            return Name + (IsDefault ? " (Default)" : "");
        }
    }
}
