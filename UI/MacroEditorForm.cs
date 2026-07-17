using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class MacroEditorForm : Form
    {
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point p);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        private readonly ActionDef _action;
        private List<string> _profileNames;

        private long _lastRecordTime = 0;

        public MacroEditorForm(ActionDef action, List<string> profileNames = null)
        {
            InitializeComponent();
            _action = action;
            _profileNames = profileNames ?? new List<string>();

            cmbPlaybackMode.Items.Clear();
            cmbPlaybackMode.Items.Add("一括再生 (離しても最後まで)");
            cmbPlaybackMode.Items.Add("順次再生 (離すと中断)");
            cmbPlaybackMode.Items.Add("リピート再生 (押している間ループ)");
            cmbPlaybackMode.Items.Add("ステップ再生 (押す度に1つ進む)");

            cmbPressState.Items.Clear();
            cmbPressState.Items.Add("タップ (押してすぐ離す)");
            cmbPressState.Items.Add("押す (Down)");
            cmbPressState.Items.Add("離す (Up)");

            cmbRecordMode.Items.Clear();
            cmbRecordMode.Items.Add("入力のみ記録 (固定50msディレイ)");
            cmbRecordMode.Items.Add("実際の経過時間を記録");

            cmbPressState.SelectedIndex = 0;
            cmbRecordMode.SelectedIndex = 0;
            numTimeout.Value = _action.StepTimeoutMs;

            cmbPlaybackMode.SelectedIndex = (int)_action.PlaybackMode;

            RefreshMacroList();
            UpdateControlsByMode();
        }

        private void RefreshMacroList()
        {
            lstSteps.Items.Clear();
            foreach (var step in _action.MacroSteps)
            {
                string stateStr = "";
                if (step.PressState == StepPressState.Down) stateStr = "[押す]";
                else if (step.PressState == StepPressState.Up) stateStr = "[離す]";

                string info = $"[{step.DelayMs}ms] {stateStr} {GetStepInfo(step)}";
                lstSteps.Items.Add(info);
            }
        }

        private string GetStepInfo(MacroStep step)
        {
            switch (step.ActionType)
            {
                case ActionType.Keyboard:
                    if (step.MultipleKeys != null && step.MultipleKeys.Count > 1) return "KB 同時押し (" + step.MultipleKeys.Count + "キー)";
                    return $"KB Key: {step.ArgumentNum}";
                case ActionType.MouseClick: return $"Mouse Click: {step.ArgumentNum}";
                case ActionType.MouseMoveRelative: return $"Mouse Move Rel X:{step.MouseX} Y:{step.MouseY}";
                case ActionType.MouseMoveContinuous: return $"Mouse Move Speed X:{step.MouseX} Y:{step.MouseY}";
                case ActionType.MouseMoveAbsoluteDesk: return $"Mouse Move Abs(Desk) X:{step.MouseX} Y:{step.MouseY}";
                case ActionType.MouseMoveAbsoluteWin: return $"Mouse Move Abs(Win) X:{step.MouseX} Y:{step.MouseY}";
                case ActionType.MousePosSave: return "Save Mouse Pos";
                case ActionType.MousePosRestore: return "Restore Mouse Pos";
                case ActionType.XboxController: return $"Xbox Btn: {step.ArgumentNum}";
                case ActionType.ProfileSwitch: return $"Profile Switch: {step.ArgumentStr}";
                default: return step.ActionType.ToString();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var dummyBinding = new UsbInputMapper.Profiles.Binding();
            using (var editor = new BindingEditorForm(dummyBinding, _profileNames))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    var a = editor.ResultBinding.Action;
                    var step = new MacroStep {
                        ActionType = a.ActionType, 
                        ArgumentNum = a.ArgumentNum, 
                        MultipleKeys = a.MultipleKeys, 
                        ArgumentStr = a.ArgumentStr,
                        MouseX = a.MouseX, 
                        MouseY = a.MouseY, 
                        DelayMs = (int)numDelay.Value,
                        PressState = (StepPressState)cmbPressState.SelectedIndex
                    };
                    _action.MacroSteps.Add(step);
                    RefreshMacroList();
                }
            }
        }

        private void btnEditStep_Click(object sender, EventArgs e)
        {
            int idx = lstSteps.SelectedIndex;
            if (idx >= 0)
            {
                var step = _action.MacroSteps[idx];
                var dummyBinding = new UsbInputMapper.Profiles.Binding();
                
                dummyBinding.Action.ActionType = step.ActionType;
                dummyBinding.Action.ArgumentNum = step.ArgumentNum;
                dummyBinding.Action.MultipleKeys = step.MultipleKeys;
                dummyBinding.Action.ArgumentStr = step.ArgumentStr;
                dummyBinding.Action.MouseX = step.MouseX;
                dummyBinding.Action.MouseY = step.MouseY;
                
                numDelay.Value = step.DelayMs;
                cmbPressState.SelectedIndex = (int)step.PressState;
                
                using (var editor = new BindingEditorForm(dummyBinding, _profileNames))
                {
                    if (editor.ShowDialog(this) == DialogResult.OK)
                    {
                        var a = editor.ResultBinding.Action;
                        step.ActionType = a.ActionType;
                        step.ArgumentNum = a.ArgumentNum;
                        step.MultipleKeys = a.MultipleKeys;
                        step.ArgumentStr = a.ArgumentStr;
                        step.MouseX = a.MouseX;
                        step.MouseY = a.MouseY;
                        step.DelayMs = (int)numDelay.Value;
                        step.PressState = (StepPressState)cmbPressState.SelectedIndex;
                        RefreshMacroList();
                    }
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            int idx = lstSteps.SelectedIndex;
            if (idx >= 0) { _action.MacroSteps.RemoveAt(idx); RefreshMacroList(); }
        }

        private void btnUpStep_Click(object sender, EventArgs e)
        {
            int idx = lstSteps.SelectedIndex;
            if (idx > 0)
            {
                var item = _action.MacroSteps[idx];
                _action.MacroSteps.RemoveAt(idx);
                _action.MacroSteps.Insert(idx - 1, item);
                RefreshMacroList();
                lstSteps.SelectedIndex = idx - 1;
            }
        }

        private void btnDownStep_Click(object sender, EventArgs e)
        {
            int idx = lstSteps.SelectedIndex;
            if (idx >= 0 && idx < _action.MacroSteps.Count - 1)
            {
                var item = _action.MacroSteps[idx];
                _action.MacroSteps.RemoveAt(idx);
                _action.MacroSteps.Insert(idx + 1, item);
                RefreshMacroList();
                lstSteps.SelectedIndex = idx + 1;
            }
        }

        private void cmbPlaybackMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlsByMode();
            bool isStepMode = (cmbPlaybackMode.SelectedIndex == 3);
            if (!isStepMode)
            {
                bool changed = false;
                foreach (var step in _action.MacroSteps)
                {
                    if (step.PressState != StepPressState.Tap)
                    {
                        step.PressState = StepPressState.Tap;
                        changed = true;
                    }
                }
                if (changed) RefreshMacroList();
            }
        }

        private void UpdateControlsByMode()
        {
            bool isStepMode = (cmbPlaybackMode.SelectedIndex == 3);
            lblTimeout.Visible = isStepMode; 
            numTimeout.Visible = isStepMode;
            cmbPressState.Enabled = isStepMode;
            if (!isStepMode && cmbPressState.Items.Count > 0) cmbPressState.SelectedIndex = 0;
        }

        private void chkRecord_CheckedChanged(object sender, EventArgs e)
        {
            if (GlobalHookManager.Instance == null)
            {
                chkRecord.Checked = false;
                MessageBox.Show("フックマネージャが初期化されていません。");
                return;
            }

            if (chkRecord.Checked)
            {
                chkRecord.Text = "レコーディング停止";
                _lastRecordTime = Environment.TickCount;
                GlobalHookManager.Instance.OnRecordedInput += Hook_OnRecordedInput;
                GlobalHookManager.Instance.IsRecording = true;
            }
            else
            {
                chkRecord.Text = "レコーディング開始";
                GlobalHookManager.Instance.IsRecording = false;
                GlobalHookManager.Instance.OnRecordedInput -= Hook_OnRecordedInput;
                
                RemoveLastMouseClickAndMove();
                RefreshMacroList();
            }
        }

        private void RemoveLastMouseClickAndMove()
        {
            for (int i = _action.MacroSteps.Count - 1; i >= 0; i--)
            {
                var step = _action.MacroSteps[i];
                if (step.ActionType == ActionType.MouseClick)
                {
                    _action.MacroSteps.RemoveAt(i);
                }
                else if (step.ActionType == ActionType.MouseMoveAbsoluteWin)
                {
                    _action.MacroSteps.RemoveAt(i);
                    break;
                }
                else
                {
                    break; 
                }
            }
        }

        private void Hook_OnRecordedInput(object sender, GlobalHookManager.HookInputEvent e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Hook_OnRecordedInput(sender, e)));
                return;
            }

            long now = e.Timestamp;
            int delay = 50;
            if (cmbRecordMode.SelectedIndex == 1)
            {
                delay = (int)(now - _lastRecordTime);
                if (delay < 0) delay = 0;
            }

            if (e.Type == 1 && (now - _lastRecordTime) <= 50)
            {
                RemoveLastMouseClickAndMove();
                RefreshMacroList();
                delay = (cmbRecordMode.SelectedIndex == 1) ? 0 : 50; 
            }

            if (e.Type == 1) // Keyboard
            {
                if (cmbPlaybackMode.SelectedIndex != 3 && !e.IsDown)
                {
                    _lastRecordTime = now;
                    return; 
                }

                var step = new MacroStep { DelayMs = delay, PressState = e.IsDown ? StepPressState.Down : StepPressState.Up, ActionType = ActionType.Keyboard, ArgumentNum = e.Code };
                if (cmbPlaybackMode.SelectedIndex != 3) step.PressState = StepPressState.Tap;
                _action.MacroSteps.Add(step);
            }
            else // Mouse
            {
                if (e.IsDown)
                {
                    int targetX = e.X;
                    int targetY = e.Y;
                    IntPtr hwnd = WindowFromPoint(new Point(e.X, e.Y));
                    IntPtr root = GetAncestor(hwnd, 2);
                    if (root != IntPtr.Zero)
                    {
                        // ★ 枠線ズレ防止: 正確なクライアント領域座標を取得
                        Point ptScreen = new Point(e.X, e.Y);
                        ScreenToClient(root, ref ptScreen);
                        targetX = ptScreen.X;
                        targetY = ptScreen.Y;
                    }

                    var moveStep = new MacroStep { DelayMs = delay, PressState = StepPressState.Tap, ActionType = ActionType.MouseMoveAbsoluteWin, MouseX = targetX, MouseY = targetY };
                    _action.MacroSteps.Add(moveStep);
                    
                    var clickStep = new MacroStep { DelayMs = 0, PressState = StepPressState.Down, ActionType = ActionType.MouseClick, ArgumentNum = e.Code };
                    if (cmbPlaybackMode.SelectedIndex != 3) clickStep.PressState = StepPressState.Tap;
                    _action.MacroSteps.Add(clickStep);
                }
                else
                {
                    if (cmbPlaybackMode.SelectedIndex == 3) 
                    {
                        var clickStep = new MacroStep { DelayMs = delay, PressState = StepPressState.Up, ActionType = ActionType.MouseClick, ArgumentNum = e.Code };
                        _action.MacroSteps.Add(clickStep);
                    }
                }
            }

            _lastRecordTime = now;
            RefreshMacroList();
            lstSteps.SelectedIndex = lstSteps.Items.Count - 1;
        }

        private void MacroEditorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (GlobalHookManager.Instance != null && GlobalHookManager.Instance.IsRecording)
            {
                GlobalHookManager.Instance.IsRecording = false;
                GlobalHookManager.Instance.OnRecordedInput -= Hook_OnRecordedInput;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            _action.PlaybackMode = (MacroPlaybackMode)cmbPlaybackMode.SelectedIndex;
            _action.StepTimeoutMs = (int)numTimeout.Value;
            this.DialogResult = DialogResult.OK; 
            this.Close();
        }
    }
}
