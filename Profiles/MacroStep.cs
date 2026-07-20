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
        public string ArgumentExtraStr { get; set; } // ★引数追加
        public int MouseX { get; set; }
        public int MouseY { get; set; }
        public int BgActionMode { get; set; }
        public string BgClassName { get; set; }
        public int BgControlId { get; set; }
        public string BgWindowName { get; set; }

        public bool UseDelay { get; set; }
        public int DelayMs { get; set; }
        public bool UseFluctuation { get; set; }
        public int FluctuationMs { get; set; }
        public StepPressState PressState { get; set; }

        public string PlayWavPathStart { get; set; } // ★WAV開始
        public string PlayWavPathEnd { get; set; }   // ★WAV終了
        public bool WaitForExit { get; set; }        // ★プロセス終了待機 (AHKなど)

        public MacroStep()
        {
            MultipleKeys = new List<int>();
        }
    }
}
