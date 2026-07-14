using System;
using System.Linq;
using System.Windows.Forms;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class MacroEditorForm : Form
    {
        private readonly ActionDef _action;

        public MacroEditorForm(ActionDef action)
        {
            InitializeComponent();
            _action = action;

            cmbPlaybackMode.Items.Clear();
            cmbPlaybackMode.Items.Add("一括再生 (離しても最後まで)");
            cmbPlaybackMode.Items.Add("順次再生 (離すと中断)");
            cmbPlaybackMode.Items.Add("リピート再生 (押している間ループ)");
            cmbPlaybackMode.Items.Add("ステップ再生 (押す度に1つ進む)");

            cmbPlaybackMode.SelectedIndex = (int)_action.PlaybackMode;
            numTimeout.Value = _action.StepTimeoutMs;

            cmbPressState.Items.Clear();
            cmbPressState.Items.Add("タップ (押してすぐ離す)");
            cmbPressState.Items.Add("押す (Down)");
            cmbPressState.Items.Add("離す (Up)");
            cmbPressState.SelectedIndex = 0;

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

                string info = $"[{step.DelayMs}ms待機] {stateStr} {GetStepInfo(step)}";
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
                default: return step.ActionType.ToString();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var dummyBinding = new UsbInputMapper.Profiles.Binding();
            using (var editor = new BindingEditorForm(dummyBinding))
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
                
                // BindingEditorに渡すためにActionDefの形式に合わせる
                var comboItem = cmbActionTypeToEditor(step.ActionType);
                dummyBinding.Action.ActionType = step.ActionType;
                dummyBinding.Action.ArgumentNum = step.ArgumentNum;
                dummyBinding.Action.MultipleKeys = step.MultipleKeys;
                dummyBinding.Action.ArgumentStr = step.ArgumentStr;
                dummyBinding.Action.MouseX = step.MouseX;
                dummyBinding.Action.MouseY = step.MouseY;
                
                numDelay.Value = step.DelayMs;
                cmbPressState.SelectedIndex = (int)step.PressState;
                
                using (var editor = new BindingEditorForm(dummyBinding))
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
        
        // 補助メソッド
        private ActionType cmbActionTypeToEditor(ActionType type) => type;

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

            // ステップ再生以外になった瞬間、すでに登録されているステップの Down/Up を Tap に強制変換する（競合回避）
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
            bool isStepMode = (cmbPlaybackMode.SelectedIndex == 3); // 3: StepByStep
            lblTimeout.Visible = isStepMode; 
            numTimeout.Visible = isStepMode;
            
            cmbPressState.Enabled = isStepMode;
            if (!isStepMode) cmbPressState.SelectedIndex = 0; // タップ固定
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
