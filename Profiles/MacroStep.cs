using System;
using System.Collections.Generic;

namespace UsbInputMapper.Profiles
{
    public class MacroStep
    {
        public ActionType ActionType { get; set; }
        public int ArgumentNum { get; set; } 
        public List<int> MultipleKeys { get; set; } // ★追加
        public string ArgumentStr { get; set; } 
        public int MouseX { get; set; }
        public int MouseY { get; set; }
        public bool IsAbsolutePosition { get; set; } 
        public int DelayMs { get; set; }

        public MacroStep()
        {
            MultipleKeys = new List<int>();
            ActionType = ActionType.None;
            DelayMs = 50;
        }
    }
}
