using System;

namespace UsbInputMapper.Profiles
{
    public class MacroStep
    {
        public ActionType ActionType { get; set; }
        public int ArgumentNum { get; set; } // キーコードやボタンID
        public string ArgumentStr { get; set; } // 起動パスなど
        
        // マウス移動用
        public int MouseX { get; set; }
        public int MouseY { get; set; }
        public bool IsAbsolutePosition { get; set; } // true=デスクトップ絶対座標, false=相対移動

        // 待機時間 (一括再生時のディレイ)
        public int DelayMs { get; set; }

        public MacroStep()
        {
            ActionType = ActionType.None;
            DelayMs = 50;
        }
    }
}
