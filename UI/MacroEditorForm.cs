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

            RefreshMacroList();
        }

        private void RefreshMacroList()
        {
            lstSteps.Items.Clear();
            foreach (var step in _action.MacroSteps)
            {
                string info = $"[{step.DelayMs}ms待機] {GetStepInfo(step)}";
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
                case ActionType.MouseMove: return $"Mouse Move: {(step.IsAbsolutePosition ? "Abs" : "Rel")} X:{step.MouseX} Y:{step.MouseY}";
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
                        ActionType = a.ActionType, ArgumentNum = a.ArgumentNum, MultipleKeys = a.MultipleKeys, ArgumentStr = a.ArgumentStr,
                        MouseX = a.MouseX, MouseY = a.MouseY, IsAbsolutePosition = a.IsAbsolutePosition, DelayMs = (int)numDelay.Value
                    };
                    _action.MacroSteps.Add(step);
                    RefreshMacroList();
                }
            }
        }

        // ★追加: 編集機能
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
                dummyBinding.Action.IsAbsolutePosition = step.IsAbsolutePosition;
                
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
                        step.IsAbsolutePosition = a.IsAbsolutePosition;
                        step.DelayMs = (int)numDelay.Value;
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

        // ★追加: 並び替え (上へ / 下へ)
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
            bool isStepMode = (cmbPlaybackMode.SelectedIndex == 3);
            lblTimeout.Visible = isStepMode; numTimeout.Visible = isStepMode;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            _action.PlaybackMode = (MacroPlaybackMode)cmbPlaybackMode.SelectedIndex;
            _action.StepTimeoutMs = (int)numTimeout.Value;
            this.DialogResult = DialogResult.OK; this.Close();
        }
    }
}
