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
        LayerShift,
        ToggleHold
    }

    public enum MacroPlaybackMode
    {
        Sequence,   // 1. 一括再生 (1回押すとキーを離しても最後まで再生)
        Hold,       // 2. 順次再生 (押している間のみ再生し、離すと中断)
        Repeat,     // 3. リピート (押している間ループ再生)
        StepByStep  // 4. ステップ (押す度に1つ進む、タイムアウトでリセット)
    }

    public class ActionDef
    {
        public ActionType ActionType { get; set; }
        public int ArgumentNum { get; set; }
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
                case ActionType.Macro: return $"マクロ ({MacroSteps.Count} steps, {PlaybackMode})";
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
