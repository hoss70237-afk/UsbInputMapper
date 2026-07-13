using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public enum ActionType
    {
        None,
        Keyboard,           // スキャンコードでのハードウェア出力
        MouseClick,
        MouseMove,          // 速度指定や絶対/相対移動
        MousePosSave,       // 現在の座標を保存
        MousePosRestore,    // 保存した座標に復元
        XboxController,
        AppLaunch,
        Macro,              // 複数キー連続・同時押し・複雑な動作
        LayerShift,         // 押している間別の設定に切り替え
        ToggleHold          // 1回押すと押しっぱなし、もう1回で離す
    }

    public enum MacroPlaybackMode
    {
        Sequence,       // 一括再生 (タイムライン通りに全再生)
        StepByStep      // 押されるたびに1ステップずつ進む
    }

    public class ActionDef
    {
        public ActionType ActionType { get; set; }
        
        // 単一アクション用パラメータ
        public int ArgumentNum { get; set; }
        public string ArgumentStr { get; set; }
        public string ArgumentExtraStr { get; set; }

        // マウス移動用
        public int MouseX { get; set; }
        public int MouseY { get; set; }
        public bool IsAbsolutePosition { get; set; }

        // マクロ用パラメータ
        public List<MacroStep> MacroSteps { get; set; }
        public MacroPlaybackMode PlaybackMode { get; set; }
        public int StepTimeoutMs { get; set; } // ステップ再生時、この時間が経過すると最初に戻る

        public ActionDef()
        {
            MacroSteps = new List<MacroStep>();
            PlaybackMode = MacroPlaybackMode.Sequence;
            StepTimeoutMs = 1000;
        }

        public override string ToString()
        {
            switch (ActionType)
            {
                case ActionType.Keyboard: return $"KB Key: {ArgumentNum}";
                case ActionType.AppLaunch: return $"Launch: {ArgumentStr}";
                case ActionType.XboxController: return $"Xbox Button: {ArgumentNum}";
                case ActionType.Macro: return $"Macro ({MacroSteps.Count} steps)";
                case ActionType.MouseMove: return $"Mouse Move: {(IsAbsolutePosition ? "Abs" : "Rel")} {MouseX}, {MouseY}";
                case ActionType.MousePosSave: return "Save Mouse Pos";
                case ActionType.MousePosRestore: return "Restore Mouse Pos";
                default: return ActionType.ToString();
            }
        }
    }
}
