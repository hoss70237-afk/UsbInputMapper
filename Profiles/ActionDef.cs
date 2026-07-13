using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public enum ActionType
    {
        None,
        Keyboard,
        MouseClick,
        MouseMove,
        MouseContinuousMove, // ★追加: 押している間/指定時間 連続移動
        MousePosSave,
        MousePosRestore,
        XboxController,
        AppLaunch,
        Macro,
        LayerShift,
        ToggleHold
    }

    public enum MacroPlaybackMode
    {
        Sequence,
        StepByStep
    }

    public class ActionDef
    {
        public ActionType ActionType { get; set; }
        
        public int ArgumentNum { get; set; } // キーコード、持続時間(ms)など
        public string ArgumentStr { get; set; }
        public string ArgumentExtraStr { get; set; }

        public int MouseX { get; set; }
        public int MouseY { get; set; }
        public bool IsAbsolutePosition { get; set; }

        public List<MacroStep> MacroSteps { get; set; }
        public MacroPlaybackMode PlaybackMode { get; set; }
        public int StepTimeoutMs { get; set; }

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
                case ActionType.AppLaunch: return $"起動: {System.IO.Path.GetFileName(ArgumentStr)}";
                case ActionType.XboxController: return $"Xbox Btn: {ArgumentNum}";
                case ActionType.Macro: return $"マクロ ({MacroSteps.Count} steps)";
                case ActionType.MouseMove: return $"マウス移動 ({(IsAbsolutePosition ? "絶対" : "相対")}) X:{MouseX} Y:{MouseY}";
                case ActionType.MouseContinuousMove: return $"マウス連続移動 X:{MouseX} Y:{MouseY}";
                case ActionType.MouseClick: return $"マウスクリック: {ArgumentNum}";
                case ActionType.LayerShift: return $"レイヤー {ArgumentNum} へシフト";
                case ActionType.ToggleHold: return $"トグル維持 (KB Key: {ArgumentNum})";
                default: return ActionType.ToString();
            }
        }
    }
}
