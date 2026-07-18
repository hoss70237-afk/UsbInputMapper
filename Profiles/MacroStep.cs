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
        
        public bool UseDelay { get; set; } = true;    // ★追加: 待機時間を使用するか
        public int DelayMs { get; set; }
        public bool UseFluctuation { get; set; } = false; // ★追加: 揺らぎを使用するか
        public int FluctuationMs { get; set; } = 15;      // ★追加: 揺らぎ幅

        public StepPressState PressState { get; set; } 

        public MacroStep()
        {
            MultipleKeys = new List<int>();
            ActionType = ActionType.None;
            DelayMs = 50;
            PressState = StepPressState.Tap;
        }
    }
}
