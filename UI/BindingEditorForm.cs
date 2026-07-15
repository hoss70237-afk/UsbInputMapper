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
    public partial class BindingEditorForm : Form
    {
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point p);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out OutputDispatcher.RECT lpRect);

        public UsbInputMapper.Profiles.Binding ResultBinding { get; private set; }
        private List<string> _profileNames;

        private class ComboItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public override string ToString() => Text;
        }

        public BindingEditorForm(UsbInputMapper.Profiles.Binding existingBinding = null, List<string> profileNames = null)
        {
            InitializeComponent();
            _profileNames = profileNames ?? new List<string>();
            SetupComboBoxes();

            if (existingBinding != null)
            {
                ResultBinding = existingBinding;
                txtName.Text = existingBinding.Name;
                chkBlockOriginalInput.Checked = existingBinding.BlockOriginalInput;
                UpdateMainTriggerLabel();
                
                if (ResultBinding.SubTriggers != null)
                {
                    foreach (var st in ResultBinding.SubTriggers) lstSubTriggers.Items.Add(st);
                }

                cmbCondition.SelectedIndex = (int)existingBinding.Condition;
                numConditionParam.Value = existingBinding.ConditionParam;
                
                SetActionTypeCombo(existingBinding.Action.ActionType);
                SetOutputTarget(existingBinding.Action);
                
                txtAppPath.Text = existingBinding.Action.ArgumentStr;
                numMouseX.Value = existingBinding.Action.MouseX;
                numMouseY.Value = existingBinding.Action.MouseY;

                if (existingBinding.Action.ActionType == ActionType.ProfileSwitch)
                {
                    cmbProfileSwitchTarget.SelectedItem = existingBinding.Action.ArgumentStr;
                    cmbProfileSwitchMode.SelectedIndex = existingBinding.Action.ArgumentNum;
                }
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

            cmbActionType.Items.Add(new ComboItem { Text = "(None)", Value = (int)ActionType.None });
            cmbActionType.Items.Add(new ComboItem { Text = "キーボード入力", Value = (int)ActionType.Keyboard });
            cmbActionType.Items.Add(new ComboItem { Text = "マウスクリック", Value = (int)ActionType.MouseClick });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (相対: 現在地から指定座標分)", Value = (int)ActionType.MouseMoveRelative });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (スピード: 指定方向に移動)", Value = (int)ActionType.MouseMoveContinuous });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (絶対: デスクトップ座標へ)", Value = (int)ActionType.MouseMoveAbsoluteDesk });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (絶対: ウィンドウ座標へ)", Value = (int)ActionType.MouseMoveAbsoluteWin });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス座標を保存", Value = (int)ActionType.MousePosSave });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス座標を復元", Value = (int)ActionType.MousePosRestore });
            cmbActionType.Items.Add(new ComboItem { Text = "Xboxコントローラー入力", Value = (int)ActionType.XboxController });
            cmbActionType.Items.Add(new ComboItem { Text = "アプリケーション起動", Value = (int)ActionType.AppLaunch });
            cmbActionType.Items.Add(new ComboItem { Text = "トグル維持", Value = (int)ActionType.ToggleHold });
            cmbActionType.Items.Add(new ComboItem { Text = "マクロ実行", Value = (int)ActionType.Macro });
            cmbActionType.Items.Add(new ComboItem { Text = "プロファイル切り替え", Value = (int)ActionType.ProfileSwitch });

            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "左クリック", Value = 0x0001 });
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "右クリック", Value = 0x0002 });
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "中クリック", Value = 0x0003 });
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "ホイール上", Value = 0x0004 });
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "ホイール下", Value = 0x0005 });
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                cmbManualSubTrigger.Items.Add(new ComboItem { Text = key.ToString(), Value = 0x010000 | (int)key });
            }
            cmbManualSubTrigger.SelectedIndex = 0;

            foreach (var pName in _profileNames) cmbProfileSwitchTarget.Items.Add(pName);
            if (cmbProfileSwitchTarget.Items.Count > 0) cmbProfileSwitchTarget.SelectedIndex = 0;
            
            cmbProfileSwitchMode.Items.Add("トグル (押す度に切り替え)");
            cmbProfileSwitchMode.Items.Add("ホールド (押している間だけ)");
            cmbProfileSwitchMode.SelectedIndex = 0;
        }

        private void SetActionTypeCombo(ActionType type)
        {
            for (int i = 0; i < cmbActionType.Items.Count; i++)
            {
                if (((ComboItem)cmbActionType.Items[i]).Value == (int)type)
                {
                    cmbActionType.SelectedIndex = i;
                    break;
                }
            }
        }

        private void UpdateMainTriggerLabel()
        {
            lblMainTrigger.Text = $"メイン入力: {UsbInputMapper.Profiles.Binding.GetCodeName(ResultBinding.InputType, ResultBinding.InputCode)}";
        }

        private void btnReCaptureMain_Click(object sender, EventArgs e)
        {
            using (var capture = new CaptureForm(CaptureMode.SingleAny))
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
            using (var capture = new CaptureForm(CaptureMode.SingleAny))
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
            if (lstSubTriggers.SelectedIndex >= 0) lstSubTriggers.Items.RemoveAt(lstSubTriggers.SelectedIndex);
        }

        private void btnManualAddSub_Click(object sender, EventArgs e)
        {
            if (cmbManualSubTrigger.SelectedItem is ComboItem item)
            {
                int type = (item.Value & 0x010000) != 0 ? 1 : 0;
                int code = item.Value & 0xFFFF;
                var key = new TriggerKey { DeviceIdentifier = "Any", Type = type, Code = code };
                lstSubTriggers.Items.Add(key);
            }
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
            if (!(cmbActionType.SelectedItem is ComboItem actItem)) return;
            var type = (ActionType)actItem.Value;
            
            cmbKeyButton.Visible = false;
            txtAppPath.Visible = false;
            btnBrowseApp.Visible = false;
            pnlMouseMove.Visible = false;
            btnEditMacro.Visible = false;
            cmbProfileSwitchTarget.Visible = false;
            cmbProfileSwitchMode.Visible = false;
            
            cmbKeyButton.Items.Clear();
            cmbKeyButton.SelectedIndexChanged -= cmbKeyButton_SelectedIndexChanged;

            switch (type)
            {
                case ActionType.Keyboard:
                case ActionType.ToggleHold:
                    cmbKeyButton.Visible = true;
                    cmbKeyButton.Items.Add(new ComboItem { Text = "(None)", Value = 0 });
                    cmbKeyButton.Items.Add(new ComboItem { Text = "実際に入力 (同時押し対応)...", Value = -1 });
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
                case ActionType.MouseMoveRelative:
                case ActionType.MouseMoveContinuous:
                case ActionType.MouseMoveAbsoluteDesk:
                case ActionType.MouseMoveAbsoluteWin:
                    pnlMouseMove.Visible = true;
                    btnCaptureCoord.Visible = (type != ActionType.MouseMoveContinuous); // 連続移動は座標取得無効
                    break;
                case ActionType.AppLaunch:
                    txtAppPath.Visible = true;
                    btnBrowseApp.Visible = true;
                    break;
                case ActionType.Macro:
                    btnEditMacro.Visible = true;
                    break;
                case ActionType.ProfileSwitch:
                    cmbProfileSwitchTarget.Visible = true;
                    cmbProfileSwitchMode.Visible = true;
                    break;
            }

            if (cmbKeyButton.Items.Count > 0) cmbKeyButton.SelectedIndex = 0;
            
            if (ResultBinding != null && ResultBinding.Action != null && ResultBinding.Action.ActionType == type)
            {
                SetOutputTarget(ResultBinding.Action);
            }
            
            cmbKeyButton.SelectedIndexChanged += cmbKeyButton_SelectedIndexChanged;
        }

        private void cmbKeyButton_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbKeyButton.SelectedItem is ComboItem cItem && cItem.Value == -1)
            {
                using (var capture = new CaptureForm(CaptureMode.MultiKeyboard))
                {
                    if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedKeys.Count > 0)
                    {
                        ResultBinding.Action.MultipleKeys = new List<int>(capture.CapturedKeys);
                        string keysStr = string.Join(" + ", capture.CapturedKeys.Select(k => ((Keys)k).ToString()));
                        var customItem = new ComboItem { Text = $"[キャプチャ] {keysStr}", Value = -2 };
                        
                        cmbKeyButton.SelectedIndexChanged -= cmbKeyButton_SelectedIndexChanged;
                        cmbKeyButton.Items.Insert(0, customItem);
                        cmbKeyButton.SelectedIndex = 0;
                        cmbKeyButton.SelectedIndexChanged += cmbKeyButton_SelectedIndexChanged;
                    }
                    else
                    {
                        cmbKeyButton.SelectedIndex = 0;
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
                    
                    cmbKeyButton.SelectedIndexChanged -= cmbKeyButton_SelectedIndexChanged;
                    cmbKeyButton.Items.Insert(0, customItem);
                    cmbKeyButton.SelectedIndex = 0;
                    cmbKeyButton.SelectedIndexChanged += cmbKeyButton_SelectedIndexChanged;
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
            using (var editor = new MacroEditorForm(ResultBinding.Action, _profileNames))
            {
                editor.ShowDialog(this);
            }
        }

        private void btnCaptureCoord_Click(object sender, EventArgs e)
        {
            if (GlobalHookManager.Instance == null) return;
            var type = (ActionType)((ComboItem)cmbActionType.SelectedItem).Value;
            bool isRelative = (type == ActionType.MouseMoveRelative);
            bool isWindow = (type == ActionType.MouseMoveAbsoluteWin);

            int clickCount = 0;
            GlobalHookManager.POINT startPt = new GlobalHookManager.POINT();
            
            this.Hide();

            GlobalHookManager.Instance.StartCoordinateCapture(pt => {
                if (isRelative)
                {
                    if (clickCount == 0) { startPt = pt; clickCount++; }
                    else if (clickCount == 1) {
                        this.BeginInvoke(new Action(() => {
                            numMouseX.Value = pt.x - startPt.x;
                            numMouseY.Value = pt.y - startPt.y;
                            GlobalHookManager.Instance.StopCoordinateCapture();
                            this.Show();
                        }));
                    }
                }
                else
                {
                    int targetX = pt.x;
                    int targetY = pt.y;
                    if (isWindow)
                    {
                        IntPtr hwnd = WindowFromPoint(new Point(pt.x, pt.y));
                        IntPtr root = GetAncestor(hwnd, 2); // GA_ROOT
                        if (root != IntPtr.Zero && GetWindowRect(root, out OutputDispatcher.RECT rect))
                        {
                            targetX = pt.x - rect.Left;
                            targetY = pt.y - rect.Top;
                        }
                    }
                    this.BeginInvoke(new Action(() => {
                        numMouseX.Value = targetX;
                        numMouseY.Value = targetY;
                        GlobalHookManager.Instance.StopCoordinateCapture();
                        this.Show();
                    }));
                }
            });
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("アイテムの名前を入力してください。"); return;
            }

            ResultBinding.Name = txtName.Text;
            ResultBinding.BlockOriginalInput = chkBlockOriginalInput.Checked;
            
            ResultBinding.SubTriggers.Clear();
            foreach (TriggerKey t in lstSubTriggers.Items) ResultBinding.SubTriggers.Add(t);

            ResultBinding.Condition = (TriggerCondition)cmbCondition.SelectedIndex;
            ResultBinding.ConditionParam = (int)numConditionParam.Value;

            if (cmbActionType.SelectedItem is ComboItem actItem)
            {
                ResultBinding.Action.ActionType = (ActionType)actItem.Value;
            }

            if (cmbKeyButton.Visible && cmbKeyButton.SelectedItem is ComboItem cItem && cItem.Value >= 0)
            {
                ResultBinding.Action.ArgumentNum = cItem.Value;
                ResultBinding.Action.MultipleKeys.Clear();
                if (cItem.Value > 0 && (ResultBinding.Action.ActionType == ActionType.Keyboard || ResultBinding.Action.ActionType == ActionType.ToggleHold))
                {
                    ResultBinding.Action.MultipleKeys.Add(cItem.Value);
                }
            }

            if (ResultBinding.Action.ActionType == ActionType.ProfileSwitch)
            {
                ResultBinding.Action.ArgumentStr = cmbProfileSwitchTarget.SelectedItem?.ToString();
                ResultBinding.Action.ArgumentNum = cmbProfileSwitchMode.SelectedIndex;
            }
            else
            {
                ResultBinding.Action.ArgumentStr = txtAppPath.Text;
            }

            ResultBinding.Action.MouseX = (int)numMouseX.Value;
            ResultBinding.Action.MouseY = (int)numMouseY.Value;

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
