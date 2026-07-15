using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class MacroEditorForm : Form
    {
        private readonly ActionDef _action;
        private List<string> _profileNames;

        private long _lastRecordTime = 0;
        private MacroStep _lastMouseClickStep = null;

        public MacroEditorForm(ActionDef action, List<string> profileNames = null)
        {
            InitializeComponent();
            _action = action;
            _profileNames = profileNames ?? new List<string>();

            // 1. まずすべての ComboBox に Items を追加する
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

            // 2. Items の追加が終わってから SelectedIndex を設定する
            cmbPressState.SelectedIndex = 0;
            cmbRecordMode.SelectedIndex = 0;
            numTimeout.Value = _action.StepTimeoutMs;

            // これによりイベントが発火しても、他のComboBoxの準備ができているためエラーになりません
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
                _lastMouseClickStep = null;
                GlobalHookManager.Instance.OnRecordedInput += Hook_OnRecordedInput;
                GlobalHookManager.Instance.IsRecording = true;
            }
            else
            {
                chkRecord.Text = "レコーディング開始";
                GlobalHookManager.Instance.IsRecording = false;
                GlobalHookManager.Instance.OnRecordedInput -= Hook_OnRecordedInput;
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

            // ★50ms以内のキーボード入力検知によるマウスクリック破棄 (ソフトキーボード対策)
            if (e.Type == 1 && _lastMouseClickStep != null && (now - _lastRecordTime) <= 50)
            {
                _action.MacroSteps.Remove(_lastMouseClickStep);
                RefreshMacroList();
                _lastMouseClickStep = null;
                delay = (cmbRecordMode.SelectedIndex == 1) ? 0 : 50;
            }

            var step = new MacroStep { DelayMs = delay, PressState = e.IsDown ? StepPressState.Down : StepPressState.Up };
            if (cmbPlaybackMode.SelectedIndex != 3) step.PressState = StepPressState.Tap; // Stepモード以外は強制タップ

            if (e.Type == 1)
            {
                step.ActionType = ActionType.Keyboard;
                step.ArgumentNum = e.Code;
                _lastMouseClickStep = null;
            }
            else
            {
                step.ActionType = ActionType.MouseClick;
                step.ArgumentNum = e.Code;
                if (e.IsDown) _lastMouseClickStep = step;
                else _lastMouseClickStep = null;
            }

            // StepMode以外の場合、DownとUpが別々に来てもTapで済ませるため、Upの記録を省く(簡易化)
            if (cmbPlaybackMode.SelectedIndex != 3 && !e.IsDown)
            {
                _lastRecordTime = now;
                return;
            }

            _action.MacroSteps.Add(step);
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
