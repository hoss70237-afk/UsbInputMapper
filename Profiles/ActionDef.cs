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
        MouseContinuousMove,
        MousePosSave,
        MousePosRestore,
        XboxController,
        AppLaunch,
        Macro,
        ToggleHold
    }

    public enum MacroPlaybackMode
    {
        Sequence, Hold, Repeat, StepByStep
    }

    public class ActionDef
    {
        public ActionType ActionType { get; set; }
        public int ArgumentNum { get; set; }
        public List<int> MultipleKeys { get; set; } // ★追加: 同時押しキーボード出力用
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
            MultipleKeys = new List<int>();
            MacroSteps = new List<MacroStep>();
            PlaybackMode = MacroPlaybackMode.Sequence;
            StepTimeoutMs = 1000;
        }

        public override string ToString()
        {
            switch (ActionType)
            {
                case ActionType.Keyboard: 
                    if (MultipleKeys.Count > 1) return "KB 同時押し (" + MultipleKeys.Count + "キー)";
                    return $"KB Key: {ArgumentNum}";
                case ActionType.AppLaunch: return $"起動: {System.IO.Path.GetFileName(ArgumentStr)}";
                case ActionType.XboxController: return $"Xbox Btn: {ArgumentNum}";
                case ActionType.Macro: return $"マクロ ({MacroSteps.Count} steps, {PlaybackMode})";
                case ActionType.MouseMove: return $"マウス移動 X:{MouseX} Y:{MouseY}";
                case ActionType.MouseContinuousMove: return $"マウス連続移動 X:{MouseX} Y:{MouseY}";
                case ActionType.MouseClick: return $"マウスクリック: {ArgumentNum}";
                case ActionType.ToggleHold: return $"トグル維持";
                default: return ActionType.ToString();
            }
        }
    }
}
