using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public enum ActionType
    {
        None,
        Keyboard,
        MouseClick,
        MouseMoveRelative,
        MouseMoveContinuous,
        MouseMoveAbsoluteDesk,
        MouseMoveAbsoluteWin,
        MousePosSave,
        MousePosRestore,
        XboxController,
        XboxAxis,     // ★アナログスティック用追加 (ArgumentNum: 1=LX, 2=LY, 3=RX, 4=RY)
        XboxTrigger,  // ★アナログトリガー用追加 (ArgumentNum: 1=LT, 2=RT)
        AppLaunch,
        Macro,
        ToggleHold,
        ProfileSwitch
    }

    public enum MacroPlaybackMode { Sequence, Hold, Repeat, StepByStep }

    public class ActionDef
    {
        public ActionType ActionType { get; set; }
        public int ArgumentNum { get; set; }
        public List<int> MultipleKeys { get; set; }
        public string ArgumentStr { get; set; }
        public string ArgumentExtraStr { get; set; }
        public int MouseX { get; set; }
        public int MouseY { get; set; }

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

        public override string ToString() => ActionType.ToString();
    }
}
