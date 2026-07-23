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
        public bool NotifyProfileChangeVibration { get; set; } = false;
        
        // ★追加: チャタリングキャンセラー設定
        public bool EnableChatteringCanceler { get; set; } = false;
        public int ChatteringThresholdMs { get; set; } = 20;
        
        // ★追加: オーバーレイ設定
        public bool OverlayShowMark { get; set; } = true;
        public bool OverlayShowName { get; set; } = true;
        public int OverlayPosX { get; set; } = -1; // -1はデフォルト(右上)
        public int OverlayPosY { get; set; } = -1;
        public int OverlayDurationMs { get; set; } = 2000;

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
