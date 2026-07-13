using System;
using System.Collections.Generic;
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
                
                cmbActionType.SelectedItem = existingBinding.Action.ActionType;
                
                SetOutputTarget(existingBinding.Action.ActionType, existingBinding.Action.ArgumentNum);
                txtAppPath.Text = existingBinding.Action.ArgumentStr;
            }
            else
            {
                ResultBinding = new UsbInputMapper.Profiles.Binding();
                cmbCondition.SelectedIndex = 0;
                cmbActionType.SelectedIndex = 1; // Keyboardをデフォルトに
            }
        }

        private void SetupComboBoxes()
        {
            cmbCondition.Items.Add("通常入力 (押した時)");
            cmbCondition.Items.Add("長押し (ミリ秒経過で発動)");
            cmbCondition.Items.Add("連打 (押している間ループ)");

            // アクションタイプリスト (最新の定義に合わせる)
            cmbActionType.Items.Add(ActionType.None);
            cmbActionType.Items.Add(ActionType.Keyboard);
            cmbActionType.Items.Add(ActionType.MouseClick);
            cmbActionType.Items.Add(ActionType.MouseMove);
            cmbActionType.Items.Add(ActionType.XboxController);
            cmbActionType.Items.Add(ActionType.AppLaunch);
        }

        private void cmbCondition_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cmbCondition.SelectedIndex;
            if (idx == 0)
            {
                lblParam.Visible = false;
                numConditionParam.Visible = false;
            }
            else if (idx == 1)
            {
                lblParam.Text = "長押し時間 (ms):";
                lblParam.Visible = true;
                numConditionParam.Visible = true;
            }
            else if (idx == 2)
            {
                lblParam.Text = "連打間隔 (ms):";
                lblParam.Visible = true;
                numConditionParam.Visible = true;
            }
        }

        private void cmbActionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var type = (ActionType)cmbActionType.SelectedItem;
            
            cmbKeyButton.Items.Clear();
            cmbKeyButton.Visible = true;
            txtAppPath.Visible = false;
            btnBrowseApp.Visible = false;

            if (type == ActionType.Keyboard)
            {
                foreach (Keys key in Enum.GetValues(typeof(Keys)))
                {
                    cmbKeyButton.Items.Add(new ComboItem { Text = key.ToString(), Value = (int)key });
                }
            }
            else if (type == ActionType.MouseClick)
            {
                cmbKeyButton.Items.Add(new ComboItem { Text = "左クリック", Value = 1 });
                cmbKeyButton.Items.Add(new ComboItem { Text = "右クリック", Value = 2 });
                cmbKeyButton.Items.Add(new ComboItem { Text = "中クリック", Value = 3 });
                cmbKeyButton.Items.Add(new ComboItem { Text = "ホイール 上", Value = 4 });
                cmbKeyButton.Items.Add(new ComboItem { Text = "ホイール 下", Value = 5 });
            }
            else if (type == ActionType.MouseMove)
            {
                // マウス移動の場合の仮UI（本来はX/Y座標入力ボックス等が必要だが、ここでは簡略化して選択肢とする）
                cmbKeyButton.Items.Add(new ComboItem { Text = "上に移動 (-50)", Value = 1 });
                cmbKeyButton.Items.Add(new ComboItem { Text = "下に移動 (+50)", Value = 2 });
                cmbKeyButton.Items.Add(new ComboItem { Text = "左に移動 (-50)", Value = 3 });
                cmbKeyButton.Items.Add(new ComboItem { Text = "右に移動 (+50)", Value = 4 });
            }
            else if (type == ActionType.XboxController)
            {
                string[] xboxBtns = { "A", "B", "X", "Y", "LB", "RB", "Back", "Start", "Lスティック押込", "Rスティック押込", "上(D-Pad)", "下(D-Pad)", "左(D-Pad)", "右(D-Pad)", "Guide" };
                for (int i = 0; i < xboxBtns.Length; i++)
                {
                    cmbKeyButton.Items.Add(new ComboItem { Text = xboxBtns[i], Value = i + 1 });
                }
            }
            else if (type == ActionType.AppLaunch)
            {
                cmbKeyButton.Visible = false;
                txtAppPath.Visible = true;
                btnBrowseApp.Visible = true;
            }
            else
            {
                cmbKeyButton.Visible = false;
            }

            if (cmbKeyButton.Items.Count > 0) cmbKeyButton.SelectedIndex = 0;
        }

        private void SetOutputTarget(ActionType type, int value)
        {
            if (type == ActionType.AppLaunch || type == ActionType.None) return;

            for (int i = 0; i < cmbKeyButton.Items.Count; i++)
            {
                var item = (ComboItem)cmbKeyButton.Items[i];
                if (item.Value == value)
                {
                    cmbKeyButton.SelectedIndex = i;
                    break;
                }
            }
        }

        private void btnBrowseApp_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtAppPath.Text = ofd.FileName;
                }
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
            ResultBinding.Condition = (TriggerCondition)cmbCondition.SelectedIndex;
            ResultBinding.ConditionParam = (int)numConditionParam.Value;

            ResultBinding.Action.ActionType = (ActionType)cmbActionType.SelectedItem;

            if (cmbKeyButton.Visible && cmbKeyButton.SelectedItem is ComboItem cItem)
            {
                ResultBinding.Action.ArgumentNum = cItem.Value;

                // 簡易的なマウス移動の処理変換（1=上, 2=下, 3=左, 4=右）
                if (ResultBinding.Action.ActionType == ActionType.MouseMove)
                {
                    ResultBinding.Action.MouseX = (cItem.Value == 3) ? -50 : (cItem.Value == 4) ? 50 : 0;
                    ResultBinding.Action.MouseY = (cItem.Value == 1) ? -50 : (cItem.Value == 2) ? 50 : 0;
                    ResultBinding.Action.IsAbsolutePosition = false;
                }
            }

            ResultBinding.Action.ArgumentStr = txtAppPath.Text;

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
