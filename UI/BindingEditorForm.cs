using System;
using System.Collections.Generic;
using System.Linq;
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
                UpdateMainTriggerLabel();
                
                if (ResultBinding.SubTriggers != null)
                {
                    foreach (var st in ResultBinding.SubTriggers) lstSubTriggers.Items.Add(st);
                }

                cmbCondition.SelectedIndex = (int)existingBinding.Condition;
                numConditionParam.Value = existingBinding.ConditionParam;
                
                cmbActionType.SelectedItem = existingBinding.Action.ActionType;
                SetOutputTarget(existingBinding.Action);
                
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
            }
        }

        private void SetupComboBoxes()
        {
            cmbCondition.Items.Add("通常入力 (押した時)");
            cmbCondition.Items.Add("長押し (ミリ秒経過で発動)");
            cmbCondition.Items.Add("連打 (押している間ループ)");
            cmbCondition.Items.Add("離した時 (Key Up)");

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
            cmbActionType.Items.Add(ActionType.Macro);
        }

        private void UpdateMainTriggerLabel()
        {
            lblMainTrigger.Text = $"メイン入力: {UsbInputMapper.Profiles.Binding.GetCodeName(ResultBinding.InputType, ResultBinding.InputCode)}";
        }

        private void btnReCaptureMain_Click(object sender, EventArgs e)
        {
            using (var capture = new CaptureForm())
            {
                if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedEvent != null)
                {
                    var evt = capture.CapturedEvent;
                    ResultBinding.DeviceIdentifier = evt.DeviceIdentifier;
                    ResultBinding.InputType = evt.Type;
                    ResultBinding.InputCode = (evt.Type == 1) ? evt.VKey : (int)evt.MouseButtonFlags;
                    UpdateMainTriggerLabel();
                }
            }
        }

        private void btnAddSubTrigger_Click(object sender, EventArgs e)
        {
            using (var capture = new CaptureForm())
            {
                if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedEvent != null)
                {
                    var evt = capture.CapturedEvent;
                    var key = new TriggerKey
                    {
                        DeviceIdentifier = evt.DeviceIdentifier,
                        Type = evt.Type,
                        Code = (evt.Type == 1) ? evt.VKey : (int)evt.MouseButtonFlags
                    };
                    lstSubTriggers.Items.Add(key);
                }
            }
        }

        private void btnRemoveSubTrigger_Click(object sender, EventArgs e)
        {
            if (lstSubTriggers.SelectedIndex >= 0)
                lstSubTriggers.Items.RemoveAt(lstSubTriggers.SelectedIndex);
        }

        private void cmbCondition_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbCondition.SelectedIndex;
            lblParam.Visible = (idx == 1 || idx == 2);
            numConditionParam.Visible = (idx == 1 || idx == 2);
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
                    cmbKeyButton.Items.Add(new ComboItem { Text = "(None)", Value = 0 });
                    cmbKeyButton.Items.Add(new ComboItem { Text = "実際に入力 (同時押し対応)...", Value = -1 }); // ★追加
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
            }

            if (cmbKeyButton.Items.Count > 0) cmbKeyButton.SelectedIndex = 0;
            
            if (ResultBinding != null && ResultBinding.Action != null && ResultBinding.Action.ActionType == type)
            {
                SetOutputTarget(ResultBinding.Action);
            }
        }

        private void cmbKeyButton_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbKeyButton.SelectedItem is ComboItem cItem && cItem.Value == -1) // 実際に入力...
            {
                using (var capture = new CaptureForm(CaptureMode.MultiKeyboard))
                {
                    if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedKeys.Count > 0)
                    {
                        ResultBinding.Action.MultipleKeys = new List<int>(capture.CapturedKeys);
                        string keysStr = string.Join(" + ", capture.CapturedKeys.Select(k => ((Keys)k).ToString()));
                        var customItem = new ComboItem { Text = $"[キャプチャ] {keysStr}", Value = -2 };
                        cmbKeyButton.Items.Insert(0, customItem);
                        cmbKeyButton.SelectedIndex = 0;
                    }
                    else
                    {
                        cmbKeyButton.SelectedIndex = 0; // キャンセル時はNoneに戻る
                    }
                }
            }
        }

        private void SetOutputTarget(ActionDef action)
        {
            if (!cmbKeyButton.Visible) return;
            
            if (action.ActionType == ActionType.Keyboard || action.ActionType == ActionType.ToggleHold)
            {
                if (action.MultipleKeys != null && action.MultipleKeys.Count > 0)
                {
                    string keysStr = string.Join(" + ", action.MultipleKeys.Select(k => ((Keys)k).ToString()));
                    var customItem = new ComboItem { Text = $"[保存済] {keysStr}", Value = -2 };
                    cmbKeyButton.Items.Insert(0, customItem);
                    cmbKeyButton.SelectedIndex = 0;
                    return;
                }
            }

            for (int i = 0; i < cmbKeyButton.Items.Count; i++)
            {
                if (((ComboItem)cmbKeyButton.Items[i]).Value == action.ArgumentNum)
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
                MessageBox.Show("アイテムの名前を入力してください。"); return;
            }

            ResultBinding.Name = txtName.Text;
            ResultBinding.SubTriggers.Clear();
            foreach (TriggerKey t in lstSubTriggers.Items) ResultBinding.SubTriggers.Add(t);

            ResultBinding.Condition = (TriggerCondition)cmbCondition.SelectedIndex;
            ResultBinding.ConditionParam = (int)numConditionParam.Value;

            ResultBinding.Action.ActionType = (ActionType)cmbActionType.SelectedItem;

            if (cmbKeyButton.Visible && cmbKeyButton.SelectedItem is ComboItem cItem)
            {
                if (cItem.Value >= 0)
                {
                    ResultBinding.Action.ArgumentNum = cItem.Value;
                    ResultBinding.Action.MultipleKeys.Clear();
                    if (cItem.Value > 0 && (ResultBinding.Action.ActionType == ActionType.Keyboard || ResultBinding.Action.ActionType == ActionType.ToggleHold))
                    {
                        ResultBinding.Action.MultipleKeys.Add(cItem.Value);
                    }
                }
                // Value < 0 (-2等) の場合は、MultipleKeysに既に保存されているので何もしない
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
