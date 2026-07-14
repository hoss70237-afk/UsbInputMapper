using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public enum StepPressState { Tap, Down, Up }

    public class MacroStep
    {
        public ActionType ActionType { get; set; }
        public int ArgumentNum { get; set; } 
        public List<int> MultipleKeys { get; set; }
        public string ArgumentStr { get; set; } 
        public int MouseX { get; set; }
        public int MouseY { get; set; }
        public int DelayMs { get; set; }
        
        public StepPressState PressState { get; set; } // ★追加: 押す/離す/タップ

        public MacroStep()
        {
            MultipleKeys = new List<int>();
            ActionType = ActionType.None;
            DelayMs = 50;
            PressState = StepPressState.Tap;
        }
    }
}
