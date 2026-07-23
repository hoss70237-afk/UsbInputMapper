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
        FileOpen,         
        FolderOpen,       // ★追加: フォルダを開く
        AhkRun,           
        Macro,
        ToggleHold,
        ProfileSwitch,
        StickToMouse, 
        RadialMenu,       
        BackgroundControl,
        CursorVisibility,    
        CursorOffset,        
        SystemMouseSettings  
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
        public string ArgumentExtraStr { get; set; }
        public int MouseX { get; set; }
        public int MouseY { get; set; }
        
        // ★追加: 動作モード (0:入力(連動), 1:押す(Downのみ), 2:離す(Upのみ))
        public int ActionState { get; set; } = 0;
        
        // ★追加: マウス移動後の揺らし
        public bool JiggleCursor { get; set; } = false;

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

        // ★変更: カーソル制御・OSマウス設定用
        public int CursorVisMode { get; set; } = 1; // 0:非表示, 1:表示, 2:トグル
        public int CursorOffsetX { get; set; } = 0;
        public int CursorOffsetY { get; set; } = 0;
        public int SystemMouseSpeed { get; set; } = 10; 
        public int SystemScrollType { get; set; } = 0;  // 0:行数, 1:1画面ずつ
        public int SystemScrollLines { get; set; } = 3; 
        public int SystemHorizontalScroll { get; set; } = 3;

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
                    string kState = ActionState == 1 ? " [押す]" : ActionState == 2 ? " [離す]" : "";
                    if (MultipleKeys != null && MultipleKeys.Count > 0) return "キーボード: " + string.Join("+", MultipleKeys) + kState;
                    return "キーボード: " + (System.Windows.Forms.Keys)ArgumentNum + kState;
                case ActionType.MouseClick: 
                    string mState = ActionState == 1 ? " [押す]" : ActionState == 2 ? " [離す]" : "";
                    return "マウスクリック: " + ArgumentNum + mState;
                case ActionType.MouseMoveRelative: return $"マウス移動(相対): X={MouseX}, Y={MouseY}" + (JiggleCursor?" [揺らす]":"");
                case ActionType.MouseMoveAbsoluteDesk: return $"マウス絶対(デスク): X={MouseX}, Y={MouseY}" + (JiggleCursor?" [揺らす]":"");
                case ActionType.MouseMoveAbsoluteWin: return $"マウス絶対(アクティブ): X={MouseX}, Y={MouseY}" + (JiggleCursor?" [揺らす]":"");
                case ActionType.MouseMoveAbsoluteHoverWin: return $"マウス絶対(ポインタ下): X={MouseX}, Y={MouseY}" + (JiggleCursor?" [揺らす]":"");
                case ActionType.XboxController: 
                    string xState = ActionState == 1 ? " [押す]" : ActionState == 2 ? " [離す]" : "";
                    return "Xboxボタン: " + ArgumentNum + xState;
                case ActionType.XboxAxis: return "Xboxスティック軸: " + ArgumentNum;
                case ActionType.XboxTrigger: return "Xboxトリガー: " + ArgumentNum;
                case ActionType.AppLaunch: return "アプリ起動: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.FileOpen: return "ファイルを開く: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.FolderOpen: return "フォルダを開く: " + ArgumentStr;
                case ActionType.AhkRun: return "AHKスクリプト実行: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.Macro: return "マクロ実行";
                case ActionType.ProfileSwitch: return "プロファイル切替: " + ArgumentStr;
                case ActionType.StickToMouse: return $"スティックマウス(最高速度:{StickMaxSpeed})";
                case ActionType.RadialMenu: return $"ラジアルメニュー({RadialMenuSlices}分割)";
                case ActionType.BackgroundControl: return $"バックグラウンド操作: {(string.IsNullOrEmpty(BgWindowName)?BgClassName:BgWindowName)}";
                
                case ActionType.CursorVisibility: 
                    return CursorVisMode == 0 ? "カーソル: 非表示" : CursorVisMode == 1 ? "カーソル: 表示" : "カーソル: トグル";
                case ActionType.CursorOffset: return $"カーソルずらし: X={CursorOffsetX}, Y={CursorOffsetY}";
                case ActionType.SystemMouseSettings: return $"OSマウス設定";
                
                default: return ActionType.ToString();
            }
        }
    }
}
