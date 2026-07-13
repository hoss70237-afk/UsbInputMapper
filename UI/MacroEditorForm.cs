using System;
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
                case ActionType.Keyboard: return $"KB Key: {step.ArgumentNum}";
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
            // 簡易的に BindingEditorForm を流用してステップ内容を作成させる
            var dummyBinding = new Binding();
            using (var editor = new BindingEditorForm(dummyBinding))
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    var a = editor.ResultBinding.Action;
                    var step = new MacroStep
                    {
                        ActionType = a.ActionType,
                        ArgumentNum = a.ArgumentNum,
                        ArgumentStr = a.ArgumentStr,
                        MouseX = a.MouseX,
                        MouseY = a.MouseY,
                        IsAbsolutePosition = a.IsAbsolutePosition,
                        DelayMs = (int)numDelay.Value
                    };
                    _action.MacroSteps.Add(step);
                    RefreshMacroList();
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            int idx = lstSteps.SelectedIndex;
            if (idx >= 0)
            {
                _action.MacroSteps.RemoveAt(idx);
                RefreshMacroList();
            }
        }

        private void cmbPlaybackMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isStepMode = (cmbPlaybackMode.SelectedIndex == 1);
            lblTimeout.Visible = isStepMode;
            numTimeout.Visible = isStepMode;
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
