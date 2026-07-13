using System;
using System.Windows.Forms;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class BindingEditorForm : Form
    {
        public UsbInputMapper.Profiles.Binding ResultBinding { get; private set; }

        private class ComboItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public override string ToString() => Text;
        }

        public BindingEditorForm(UsbInputMapper.Profiles.Binding existingBinding = null)
        {
            InitializeComponent();
            SetupComboBoxes();

            if (existingBinding != null)
            {
                ResultBinding = existingBinding;
                txtName.Text = existingBinding.Name;
                cmbCondition.SelectedIndex = (int)existingBinding.Condition;
                numConditionParam.Value = existingBinding.ConditionParam;
                
                numLayer.Value = existingBinding.TargetLayer;
                
                // 同時押し設定の復元（キーボード前提の簡易UI）
                if (existingBinding.SubInputCode > 0 && existingBinding.SubInputType == 1)
                {
                    chkCombo.Checked = true;
                    SetComboTarget(existingBinding.SubInputCode);
                }
                
                cmbActionType.SelectedItem = existingBinding.Action.ActionType;
                SetOutputTarget(existingBinding.Action.ActionType, existingBinding.Action.ArgumentNum);
                txtAppPath.Text = existingBinding.Action.ArgumentStr;

                numMouseX.Value = existingBinding.Action.MouseX;
                numMouseY.Value = existingBinding.Action.MouseY;
                chkAbsolute.Checked = existingBinding.Action.IsAbsolutePosition;
            }
            else
            {
                ResultBinding = new UsbInputMapper.Profiles.Binding();
                cmbCondition.SelectedIndex = 0;
                cmbActionType.SelectedIndex = 1;
                numLayer.Value = 0;
            }
        }

        private void SetupComboBoxes()
        {
            cmbCondition.Items.Add("通常入力 (押した時)");
            cmbCondition.Items.Add("長押し (ミリ秒経過で発動)");
            cmbCondition.Items.Add("連打 (押している間ループ)");

            cmbActionType.Items.Add(ActionType.None);
            cmbActionType.Items.Add(ActionType.Keyboard);
            cmbActionType.Items.Add(ActionType.MouseClick);
            cmbActionType.Items.Add(ActionType.MouseMove);
            cmbActionType.Items.Add(ActionType.MouseContinuousMove);
            cmbActionType.Items.Add(ActionType.MousePosSave);
            cmbActionType.Items.Add(ActionType.MousePosRestore);
            cmbActionType.Items.Add(ActionType.XboxController);
            cmbActionType.Items.Add(ActionType.AppLaunch);
            cmbActionType.Items.Add(ActionType.ToggleHold);
            cmbActionType.Items.Add(ActionType.LayerShift);
            cmbActionType.Items.Add(ActionType.Macro);

            // コンボ用（修飾キー系を中心に）
            cmbComboKey.Items.Add(new ComboItem { Text = "Ctrl", Value = 162 });
            cmbComboKey.Items.Add(new ComboItem { Text = "Shift", Value = 160 });
            cmbComboKey.Items.Add(new ComboItem { Text = "Alt", Value = 164 });
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
                cmbComboKey.Items.Add(new ComboItem { Text = key.ToString(), Value = (int)key });
            
            cmbComboKey.SelectedIndex = 0;
        }

        private void chkCombo_CheckedChanged(object sender, EventArgs e)
        {
            cmbComboKey.Enabled = chkCombo.Checked;
        }

        private void SetComboTarget(int value)
        {
            for (int i = 0; i < cmbComboKey.Items.Count; i++)
            {
                if (((ComboItem)cmbComboKey.Items[i]).Value == value)
                {
                    cmbComboKey.SelectedIndex = i;
                    break;
                }
            }
        }

        private void cmbCondition_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbCondition.SelectedIndex;
            lblParam.Visible = (idx > 0);
            numConditionParam.Visible = (idx > 0);
            if (idx == 1) lblParam.Text = "長押し時間 (ms):";
            if (idx == 2) lblParam.Text = "連打間隔 (ms):";
        }

        private void cmbActionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var type = (ActionType)cmbActionType.SelectedItem;
            
            cmbKeyButton.Visible = false;
            txtAppPath.Visible = false;
            btnBrowseApp.Visible = false;
            pnlMouseMove.Visible = false;
            btnEditMacro.Visible = false;
            cmbKeyButton.Items.Clear();

            switch (type)
            {
                case ActionType.Keyboard:
                case ActionType.ToggleHold:
                    cmbKeyButton.Visible = true;
                    foreach (Keys key in Enum.GetValues(typeof(Keys)))
                        cmbKeyButton.Items.Add(new ComboItem { Text = key.ToString(), Value = (int)key });
                    break;
                case ActionType.MouseClick:
                    cmbKeyButton.Visible = true;
                    cmbKeyButton.Items.Add(new ComboItem { Text = "左クリック", Value = 1 });
                    cmbKeyButton.Items.Add(new ComboItem { Text = "右クリック", Value = 2 });
                    cmbKeyButton.Items.Add(new ComboItem { Text = "中クリック", Value = 3 });
                    cmbKeyButton.Items.Add(new ComboItem { Text = "ホイール上", Value = 4 });
                    cmbKeyButton.Items.Add(new ComboItem { Text = "ホイール下", Value = 5 });
                    cmbKeyButton.Items.Add(new ComboItem { Text = "サイド進む(X1)", Value = 6 });
                    cmbKeyButton.Items.Add(new ComboItem { Text = "サイド戻る(X2)", Value = 7 });
                    break;
                case ActionType.XboxController:
                    cmbKeyButton.Visible = true;
                    string[] xboxBtns = { "A", "B", "X", "Y", "LB", "RB", "Back", "Start", "Lスティック押込", "Rスティック押込", "上(D-Pad)", "下(D-Pad)", "左(D-Pad)", "右(D-Pad)", "Guide" };
                    for (int i = 0; i < xboxBtns.Length; i++)
                        cmbKeyButton.Items.Add(new ComboItem { Text = xboxBtns[i], Value = i + 1 });
                    break;
                case ActionType.MouseMove:
                case ActionType.MouseContinuousMove:
                    pnlMouseMove.Visible = true;
                    chkAbsolute.Visible = (type == ActionType.MouseMove);
                    break;
                case ActionType.AppLaunch:
                    txtAppPath.Visible = true;
                    btnBrowseApp.Visible = true;
                    break;
                case ActionType.Macro:
                    btnEditMacro.Visible = true;
                    break;
                case ActionType.LayerShift:
                    cmbKeyButton.Visible = true;
                    for (int i = 1; i <= 5; i++)
                        cmbKeyButton.Items.Add(new ComboItem { Text = $"レイヤー {i} に切り替え", Value = i });
                    break;
            }

            if (cmbKeyButton.Items.Count > 0) cmbKeyButton.SelectedIndex = 0;
            
            if (ResultBinding != null && ResultBinding.Action != null && ResultBinding.Action.ActionType == type)
            {
                SetOutputTarget(type, ResultBinding.Action.ArgumentNum);
            }
        }

        private void SetOutputTarget(ActionType type, int value)
        {
            if (!cmbKeyButton.Visible) return;
            for (int i = 0; i < cmbKeyButton.Items.Count; i++)
            {
                if (((ComboItem)cmbKeyButton.Items[i]).Value == value)
                {
                    cmbKeyButton.SelectedIndex = i;
                    break;
                }
            }
        }

        private void btnBrowseApp_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "実行ファイル|*.exe|全て|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK) txtAppPath.Text = ofd.FileName;
            }
        }

        private void btnEditMacro_Click(object sender, EventArgs e)
        {
            using (var editor = new MacroEditorForm(ResultBinding.Action))
            {
                editor.ShowDialog(this);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("アイテムの名前を入力してください。");
                return;
            }

            ResultBinding.Name = txtName.Text;
            ResultBinding.TargetLayer = (int)numLayer.Value;
            
            if (chkCombo.Checked && cmbComboKey.SelectedItem is ComboItem comboItem)
            {
                ResultBinding.SubInputType = 1; // 簡易的にKeyboard専用
                ResultBinding.SubInputCode = comboItem.Value;
            }
            else
            {
                ResultBinding.SubInputCode = 0;
            }

            ResultBinding.Condition = (TriggerCondition)cmbCondition.SelectedIndex;
            ResultBinding.ConditionParam = (int)numConditionParam.Value;

            ResultBinding.Action.ActionType = (ActionType)cmbActionType.SelectedItem;

            if (cmbKeyButton.Visible && cmbKeyButton.SelectedItem is ComboItem cItem)
            {
                ResultBinding.Action.ArgumentNum = cItem.Value;
            }

            ResultBinding.Action.ArgumentStr = txtAppPath.Text;
            ResultBinding.Action.MouseX = (int)numMouseX.Value;
            ResultBinding.Action.MouseY = (int)numMouseY.Value;
            ResultBinding.Action.IsAbsolutePosition = chkAbsolute.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
