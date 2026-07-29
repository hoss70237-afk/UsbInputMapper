using System;
using System.Collections.Generic;
using System.Linq;
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
        FolderOpen,       
        AhkRun,           
        Macro,
        ToggleHold,
        ProfileSwitch,
        StickToMouse, 
        RadialMenu,       
        BackgroundControl,
        CursorVisibility,    
        SystemMouseSettings,
        LayerShift          
    }

    public enum MacroPlaybackMode { Sequence, Hold, Repeat, StepByStep }

    public class RadialMenuDirection
    {
        public int DirectionIndex { get; set; }
        public string Label { get; set; }
        public ActionDef Action { get; set; }
        public RadialMenuDirection() { Action = new ActionDef(); Label = ""; }
    }

    public class RadialMenuConfirmKey
    {
        public int Type { get; set; }
        public int Code { get; set; }
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
        
        public int ActionState { get; set; } = 0; 
        
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
        public int RadialMenuMode { get; set; } = 0; // 0: 離して確定(ホールド), 1: 任意のボタン群で確定
        
        [JsonProperty("RadialConfirmKeys")]
        public List<RadialMenuConfirmKey> RadialMenuConfirmKeys { get; set; }

        [JsonProperty("GestureDirections")]
        public List<RadialMenuDirection> RadialMenuDirections { get; set; }

        public string BgWindowName { get; set; }
        public string BgClassName { get; set; }
        public int BgControlId { get; set; }
        public int BgActionMode { get; set; } 

        public bool UseVibration { get; set; } = false;
        public int VibrateDuration { get; set; } = 200;
        public int VibrateTimes { get; set; } = 1;

        public int CursorVisMode { get; set; } = 1; 
        public int SystemMouseSpeed { get; set; } = 10; 
        public int SystemScrollType { get; set; } = 0;  
        public int SystemScrollLines { get; set; } = 3; 
        public int SystemHorizontalScroll { get; set; } = 3;
        
        public int LayerIndex { get; set; } = 1;

        public ActionDef()
        {
            MultipleKeys = new List<int>();
            MacroSteps = new List<MacroStep>();
            RadialMenuDirections = new List<RadialMenuDirection>();
            RadialMenuConfirmKeys = new List<RadialMenuConfirmKey>();
            PlaybackMode = MacroPlaybackMode.Sequence;
            StepTimeoutMs = 1000;
        }

        public ActionDef Clone()
        {
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<ActionDef>(json);
        }

        public override string ToString()
        {
            switch (ActionType)
            {
                case ActionType.None: return "アクションなし";
                case ActionType.Keyboard: 
                case ActionType.ToggleHold:
                    string kState = ActionState == 1 ? " [押す]" : ActionState == 2 ? " [離す]" : "";
                    string prefix = (ActionType == ActionType.ToggleHold ? "トグル: " : "");
                    if (MultipleKeys != null && MultipleKeys.Count > 0) 
                        return prefix + string.Join("+", MultipleKeys.Select(k => ((System.Windows.Forms.Keys)k).ToString())) + kState;
                    return prefix + (System.Windows.Forms.Keys)ArgumentNum + kState;
                case ActionType.MouseClick: 
                    string mState = ActionState == 1 ? " [押す]" : ActionState == 2 ? " [離す]" : "";
                    string mBtn = ArgumentNum == 1 ? "左" : ArgumentNum == 2 ? "右" : ArgumentNum == 3 ? "中" : ArgumentNum == 4 ? "ホイール上" : ArgumentNum == 5 ? "ホイール下" : ArgumentNum.ToString();
                    return "マウスクリック: " + mBtn + mState;
                case ActionType.MouseMoveRelative: return $"マウス相対移動: X={MouseX}, Y={MouseY}" + (JiggleCursor?" [揺らし]":"");
                case ActionType.MouseMoveAbsoluteDesk: return $"マウス絶対(デスク): X={MouseX}, Y={MouseY}" + (JiggleCursor?" [揺らし]":"");
                case ActionType.MouseMoveAbsoluteWin: return $"マウス絶対(アクティブ): X={MouseX}, Y={MouseY}" + (JiggleCursor?" [揺らし]":"");
                case ActionType.MouseMoveAbsoluteHoverWin: return $"マウス絶対(ポインタ下): X={MouseX}, Y={MouseY}" + (JiggleCursor?" [揺らし]":"");
                case ActionType.XboxController: 
                    string xState = ActionState == 1 ? " [押す]" : ActionState == 2 ? " [離す]" : "";
                    return "Xboxボタン: " + ArgumentNum + xState;
                case ActionType.XboxAxis: return "Xboxスティック軸: " + ArgumentNum;
                case ActionType.XboxTrigger: return "Xboxトリガー: " + ArgumentNum;
                case ActionType.AppLaunch: return "アプリ起動: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.FileOpen: return "ファイル実行: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.FolderOpen: return "フォルダ開く: " + ArgumentStr;
                case ActionType.AhkRun: return "AHK実行: " + System.IO.Path.GetFileName(ArgumentStr);
                case ActionType.Macro: return $"マクロ実行({MacroSteps?.Count ?? 0}ステップ)";
                case ActionType.ProfileSwitch: return "プロファイル切替: " + ArgumentStr;
                case ActionType.StickToMouse: return $"スティックマウス(最高速度:{StickMaxSpeed})";
                case ActionType.RadialMenu: 
                    string modeStr = RadialMenuMode == 1 ? "任意ボタンで確定" : "離して確定";
                    return $"ラジアルメニュー({RadialMenuSlices}分割 / {modeStr})";
                case ActionType.BackgroundControl: return $"バックグラウンド操作: {(string.IsNullOrEmpty(BgWindowName)?BgClassName:BgWindowName)}";
                case ActionType.CursorVisibility: 
                    return CursorVisMode == 0 ? "カーソル非表示" : CursorVisMode == 1 ? "カーソル表示" : "カーソル表示トグル";
                case ActionType.SystemMouseSettings: return $"OSマウス設定適用";
                case ActionType.LayerShift: return $"レイヤーシフト: Layer {LayerIndex}";
                default: return ActionType.ToString();
            }
        }
    }
}
