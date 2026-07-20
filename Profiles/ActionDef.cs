using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
        MouseMoveAbsoluteHoverWin, 
        MousePosSave,
        MousePosRestore,
        XboxController,
        XboxAxis,     
        XboxTrigger,  
        AppLaunch,
        FileOpen,         // ★追加: ファイルを開く
        AhkRun,           // ★追加: AHKスクリプトを実行
        Macro,
        ToggleHold,
        ProfileSwitch,
        StickToMouse, 
        RadialMenu,       
        BackgroundControl 
    }

    public enum MacroPlaybackMode { Sequence, Hold, Repeat, StepByStep }

    public class RadialMenuDirection
    {
        public int DirectionIndex { get; set; }
        public string Label { get; set; }
        public ActionDef Action { get; set; }
        public RadialMenuDirection() { Action = new ActionDef(); Label = ""; }
    }

    public class ActionDef
    {
        public ActionType ActionType { get; set; }
        public int ArgumentNum { get; set; }
        public List<int> MultipleKeys { get; set; }
        public string ArgumentStr { get; set; }
        public string ArgumentExtraStr { get; set; } // ★アプリ起動引数等に利用
        public int MouseX { get; set; }
        public int MouseY { get; set; }

        public List<MacroStep> MacroSteps { get; set; }
        public MacroPlaybackMode PlaybackMode { get; set; }
        public int StepTimeoutMs { get; set; }

        public int StickDeadZone { get; set; } = 15;
        public int StickMaxSpeed { get; set; } = 20;
        public int StickCurve { get; set; } = 0; 

        [JsonProperty("GestureSlices")]
        public int RadialMenuSlices { get; set; } = 8;
        
        [JsonProperty("GestureSize")]
        public int RadialMenuSize { get; set; } = 200;
        
        [JsonProperty("GestureMode")]
        public int RadialMenuMode { get; set; } = 0; 
        
        [JsonProperty("GestureDirections")]
        public List<RadialMenuDirection> RadialMenuDirections { get; set; }

        public string BgWindowName { get; set; }
        public string BgClassName { get; set; }
        public int BgControlId { get; set; }
        public int BgActionMode { get; set; } 

        public bool UseVibration { get; set; } = false;
        public int VibrateDuration { get; set; } = 200;
        public int VibrateTimes { get; set; } = 1;

        public ActionDef()
        {
            MultipleKeys = new List<int>();
            MacroSteps = new List<MacroStep>();
            RadialMenuDirections = new List<RadialMenuDirection>();
            PlaybackMode = MacroPlaybackMode.Sequence;
            StepTimeoutMs = 1000;
        }

        public override string ToString()
        {
            switch (ActionType)
            {
                case ActionType.None: return "アクションなし";
                case ActionType.Keyboard: 
                case ActionType.ToggleHold:
                    if (MultipleKeys != null && MultipleKeys.Count > 0) return "キーボード: " + string.Join("+", MultipleKeys);
                    return "キーボード: " + (System.Windows.Forms.Keys)ArgumentNum;
                case ActionType.MouseClick: return "マウスクリック: " + ArgumentNum;
                case ActionType.MouseMoveRelative: return $"マウス移動(相対): X={MouseX}, Y={MouseY}";
                case ActionType.MouseMoveAbsoluteDesk: return $"マウス絶対(デスク): X={MouseX}, Y={MouseY}";
                case ActionType.MouseMoveAbsoluteWin: return $"マウス絶対(アクティブ): X={MouseX}, Y={MouseY}";
                case ActionType.MouseMoveAbsoluteHoverWin: return $"マウス絶対(ポインタ下): X={MouseX}, Y={MouseY}";
                case ActionType.XboxController: return "Xboxボタン: " + ArgumentNum;
                case ActionType.XboxAxis: return "Xboxスティック軸: " + ArgumentNum;
                case ActionType.XboxTrigger: return "Xboxトリガー: " + ArgumentNum;
                case ActionType.AppLaunch: return "アプリ起動: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.FileOpen: return "ファイルを開く: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.AhkRun: return "AHKスクリプト実行: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.Macro: return "マクロ実行";
                case ActionType.ProfileSwitch: return "プロファイル切替: " + ArgumentStr;
                case ActionType.StickToMouse: return $"スティックマウス(最高速度:{StickMaxSpeed})";
                case ActionType.RadialMenu: return $"ラジアルメニュー({RadialMenuSlices}分割)";
                case ActionType.BackgroundControl: return $"バックグラウンド操作: {(string.IsNullOrEmpty(BgWindowName)?BgClassName:BgWindowName)}";
                default: return ActionType.ToString();
            }
        }
    }
}
