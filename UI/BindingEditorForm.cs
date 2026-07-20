using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using UsbInputMapper.Core;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class BindingEditorForm : Form
    {
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point p);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] private static extern int GetDlgCtrlID(IntPtr hwnd);

        public UsbInputMapper.Profiles.Binding ResultBinding { get; private set; }
        private List<string> _profileNames;

        private class ComboItem { public string Text { get; set; } public int Value { get; set; } public override string ToString() => Text; }

        private Panel pnlAnalog;
        private ComboBox cmbAxisRange;
        private NumericUpDown numDeadZone;
        private ComboBox cmbCurve;
        private Button btnSetupStickMouse;
        private bool _isDraggingBg = false;

        public BindingEditorForm(UsbInputMapper.Profiles.Binding existingBinding = null, List<string> profileNames = null)
        {
            InitializeComponent();
            _profileNames = profileNames ?? new List<string>();

            pnlAnalog = new Panel { Location = new Point(90, 315), Size = new Size(360, 40), Visible = false };
            pnlAnalog.Controls.Add(new Label { Text = "半軸:", Location = new Point(0, 5), AutoSize = true });
            cmbAxisRange = new ComboBox { Location = new Point(35, 3), Size = new Size(70, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAxisRange.Items.AddRange(new[] { "Full", "正(>0)", "負(<0)" }); cmbAxisRange.SelectedIndex = 0;
            pnlAnalog.Controls.Add(cmbAxisRange);

            pnlAnalog.Controls.Add(new Label { Text = "DZ(%):", Location = new Point(115, 5), AutoSize = true });
            numDeadZone = new NumericUpDown { Location = new Point(160, 3), Size = new Size(40, 20), Maximum = 50 };
            pnlAnalog.Controls.Add(numDeadZone);

            pnlAnalog.Controls.Add(new Label { Text = "カーブ:", Location = new Point(210, 5), AutoSize = true });
            cmbCurve = new ComboBox { Location = new Point(255, 3), Size = new Size(80, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCurve.Items.AddRange(new[] { "リニア", "早め", "遅め" }); cmbCurve.SelectedIndex = 0;
            pnlAnalog.Controls.Add(cmbCurve);

            this.Controls.Add(pnlAnalog);
            
            btnSetupStickMouse = new Button { Location = new Point(90, 255), Size = new Size(240, 23), Text = "スティックマウス設定...", Visible = false };
            btnSetupStickMouse.Click += (s, e) => { using (var smf = new StickMouseSetupForm(ResultBinding.Action)) { smf.ShowDialog(this); } };
            this.Controls.Add(btnSetupStickMouse);

            lblBgPicker.MouseDown += (s, e) => { _isDraggingBg = true; lblBgPicker.Capture = true; Cursor.Current = Cursors.Cross; };
            lblBgPicker.MouseMove += (s, e) => { if (_isDraggingBg) Cursor.Current = Cursors.Cross; };
            lblBgPicker.MouseUp += (s, e) => {
                if (_isDraggingBg) {
                    _isDraggingBg = false; lblBgPicker.Capture = false; Cursor.Current = Cursors.Default;
                    Point pt = Cursor.Position; IntPtr hwnd = WindowFromPoint(pt);
                    if (hwnd != IntPtr.Zero) {
                        StringBuilder sbClass = new StringBuilder(256); GetClassName(hwnd, sbClass, sbClass.Capacity);
                        StringBuilder sbText = new StringBuilder(256); GetWindowText(hwnd, sbText, sbText.Capacity);
                        txtBgClassName.Text = sbClass.ToString(); txtBgWindowName.Text = sbText.ToString();
                        numBgControlId.Value = GetDlgCtrlID(hwnd);
                    }
                }
            };

            SetupComboBoxes();

            if (existingBinding != null)
            {
                ResultBinding = existingBinding;
                txtName.Text = existingBinding.Name;
                chkBlockOriginalInput.Checked = existingBinding.BlockOriginalInput;
                UpdateMainTriggerLabel();
                if (ResultBinding.SubTriggers != null) foreach (var st in ResultBinding.SubTriggers) lstSubTriggers.Items.Add(st);
                
                cmbCondition.SelectedIndex = (int)existingBinding.Condition;
                numConditionParam.Value = existingBinding.ConditionParam;
                
                SetActionTypeCombo(existingBinding.Action.ActionType);
                SetOutputTarget(existingBinding.Action);
                
                txtAppPath.Text = existingBinding.Action.ArgumentStr;
                txtAppArgs.Text = existingBinding.Action.ArgumentExtraStr;
                numMouseX.Value = existingBinding.Action.MouseX; numMouseY.Value = existingBinding.Action.MouseY;

                txtBgClassName.Text = existingBinding.Action.BgClassName; txtBgWindowName.Text = existingBinding.Action.BgWindowName;
                numBgControlId.Value = existingBinding.Action.BgControlId; cmbBgAction.SelectedIndex = existingBinding.Action.BgActionMode;

                cmbAxisRange.SelectedIndex = existingBinding.AxisRange;
                numDeadZone.Value = existingBinding.DeadZone;
                cmbCurve.SelectedIndex = existingBinding.AccelerationCurve;

                txtWavPath.Text = existingBinding.PlayWavPath;
                chkVibrate.Checked = existingBinding.Action.UseVibration;
                numVibrateDuration.Value = existingBinding.Action.VibrateDuration;
                numVibrateTimes.Value = existingBinding.Action.VibrateTimes;
            }
            else
            {
                ResultBinding = new UsbInputMapper.Profiles.Binding();
                cmbCondition.SelectedIndex = 0; cmbActionType.SelectedIndex = 1; 
                numDeadZone.Value = 15; cmbBgAction.SelectedIndex = 0;
            }
        }

        private void SetupComboBoxes()
        {
            cmbCondition.Items.Add("通常入力 (押した時)"); cmbCondition.Items.Add("長押し (ミリ秒経過で発動)"); cmbCondition.Items.Add("連打 (押している間ループ)"); cmbCondition.Items.Add("離した時 (Key Up)");
            cmbActionType.Items.Add(new ComboItem { Text = "(None)", Value = (int)ActionType.None });
            cmbActionType.Items.Add(new ComboItem { Text = "キーボード入力", Value = (int)ActionType.Keyboard });
            cmbActionType.Items.Add(new ComboItem { Text = "マウスクリック", Value = (int)ActionType.MouseClick });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス移動 (相対)", Value = (int)ActionType.MouseMoveRelative });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス絶対座標 (デスクトップ)", Value = (int)ActionType.MouseMoveAbsoluteDesk });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス絶対座標 (アクティブウィンドウ)", Value = (int)ActionType.MouseMoveAbsoluteWin });
            cmbActionType.Items.Add(new ComboItem { Text = "マウス絶対座標 (ポインター下ウィンドウ)", Value = (int)ActionType.MouseMoveAbsoluteHoverWin });
            cmbActionType.Items.Add(new ComboItem { Text = "Xboxコントローラー入力", Value = (int)ActionType.XboxController });
            cmbActionType.Items.Add(new ComboItem { Text = "Xboxアナログスティック", Value = (int)ActionType.XboxAxis });
            cmbActionType.Items.Add(new ComboItem { Text = "Xboxアナログトリガー", Value = (int)ActionType.XboxTrigger });
            cmbActionType.Items.Add(new ComboItem { Text = "スティックでマウス移動", Value = (int)ActionType.StickToMouse });
            cmbActionType.Items.Add(new ComboItem { Text = "アプリケーション起動", Value = (int)ActionType.AppLaunch });
            cmbActionType.Items.Add(new ComboItem { Text = "ファイルを開く", Value = (int)ActionType.FileOpen });
            cmbActionType.Items.Add(new ComboItem { Text = "AHKスクリプト実行", Value = (int)ActionType.AhkRun });
            cmbActionType.Items.Add(new ComboItem { Text = "バックグラウンド操作", Value = (int)ActionType.BackgroundControl });
            cmbActionType.Items.Add(new ComboItem { Text = "トグル維持", Value = (int)ActionType.ToggleHold });
            cmbActionType.Items.Add(new ComboItem { Text = "マクロ実行", Value = (int)ActionType.Macro });
            cmbActionType.Items.Add(new ComboItem { Text = "プロファイル切り替え", Value = (int)ActionType.ProfileSwitch });
            
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "左クリック", Value = 0x0001 });
            cmbManualSubTrigger.Items.Add(new ComboItem { Text = "右クリック", Value = 0x0002 });
            foreach (Keys key in Enum.GetValues(typeof(Keys))) cmbManualSubTrigger.Items.Add(new ComboItem { Text = key.ToString(), Value = 0x010000 | (int)key });
            cmbManualSubTrigger.SelectedIndex = 0;

            foreach (var pName in _profileNames) cmbProfileSwitchTarget.Items.Add(pName);
            if (cmbProfileSwitchTarget.Items.Count > 0) cmbProfileSwitchTarget.SelectedIndex = 0;
            
            cmbProfileSwitchMode.Items.Add("トグル (押す度に切り替え)");
            cmbProfileSwitchMode.Items.Add("ホールド (押している間だけ)");
            cmbProfileSwitchMode.SelectedIndex = 0;

            cmbBgAction.Items.Add("クリック");
            cmbBgAction.Items.Add("キー送信");
        }

        private void SetActionTypeCombo(ActionType type) { for (int i = 0; i < cmbActionType.Items.Count; i++) if (((ComboItem)cmbActionType.Items[i]).Value == (int)type) { cmbActionType.SelectedIndex = i; break; } }
        private void UpdateMainTriggerLabel() { lblMainTrigger.Text = $"メイン入力: {UsbInputMapper.Profiles.Binding.GetCodeName(ResultBinding.InputType, ResultBinding.InputCode)}"; pnlAnalog.Visible = (ResultBinding.InputType == 11); }

        private string GetRawCodeName(int type, int code)
        {
            if (type == 1) return ((Keys)code).ToString();
            else if (type == 0) return $"Mouse{code}";
            else if (type == 2) return $"HID{code}";
            else if (type == 10) return $"PadBtn{code}";
            else if (type == 11) return $"PadAxis{code}";
            else if (type == 12) {
                if (code == 0) return "POV_Up";
                if (code == 9000) return "POV_Right";
                if (code == 18000) return "POV_Down";
                if (code == 27000) return "POV_Left";
                return $"POV{code}";
            }
            else if (type == 5) return $"Bezel{code}";
            return "Unknown";
        }

        private void btnReflectName_Click(object sender, EventArgs e)
        {
            var subs = new List<string>();
            foreach (var item in lstSubTriggers.Items) if (item is TriggerKey tk) subs.Add(GetRawCodeName(tk.Type, tk.Code));
            string name = subs.Count > 0 ? string.Join(" + ", subs) + " + " : "";
            name += GetRawCodeName(ResultBinding.InputType, ResultBinding.InputCode);
            txtName.Text = name;
        }

        private void cmbActionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(cmbActionType.SelectedItem is ComboItem actItem)) return;
            var type = (ActionType)actItem.Value;
            
            cmbKeyButton.Visible = txtAppPath.Visible = btnBrowseApp.Visible = txtAppArgs.Visible = lblAppArgs.Visible = pnlMouseMove.Visible = pnlBackground.Visible = btnEditMacro.Visible = cmbProfileSwitchTarget.Visible = cmbProfileSwitchMode.Visible = btnSetupStickMouse.Visible = false;
            cmbKeyButton.Items.Clear();
            cmbKeyButton.SelectedIndexChanged -= cmbKeyButton_SelectedIndexChanged;

            switch (type)
            {
                case ActionType.Keyboard: case ActionType.ToggleHold:
                    cmbKeyButton.Visible = true; cmbKeyButton.Items.Add(new ComboItem { Text = "(None)", Value = 0 }); cmbKeyButton.Items.Add(new ComboItem { Text = "実際に入力 (同時押し対応)...", Value = -1 });
                    foreach (Keys key in Enum.GetValues(typeof(Keys))) cmbKeyButton.Items.Add(new ComboItem { Text = key.ToString(), Value = (int)key });
                    break;
                case ActionType.MouseClick:
                    cmbKeyButton.Visible = true; cmbKeyButton.Items.Add(new ComboItem { Text = "左クリック", Value = 1 }); cmbKeyButton.Items.Add(new ComboItem { Text = "右クリック", Value = 2 }); cmbKeyButton.Items.Add(new ComboItem { Text = "中クリック", Value = 3 }); cmbKeyButton.Items.Add(new ComboItem { Text = "ホイール上", Value = 4 }); cmbKeyButton.Items.Add(new ComboItem { Text = "ホイール下", Value = 5 });
                    break;
                case ActionType.XboxController:
                    cmbKeyButton.Visible = true; string[] xboxBtns = { "A", "B", "X", "Y", "LB", "RB", "Back", "Start", "L3", "R3", "上", "下", "左", "右" };
                    for (int i = 0; i < xboxBtns.Length; i++) cmbKeyButton.Items.Add(new ComboItem { Text = xboxBtns[i], Value = i + 1 });
                    break;
                case ActionType.XboxAxis:
                    cmbKeyButton.Visible = true; cmbKeyButton.Items.Add(new ComboItem { Text = "左スティック X軸", Value = 1 }); cmbKeyButton.Items.Add(new ComboItem { Text = "左スティック Y軸", Value = 2 }); cmbKeyButton.Items.Add(new ComboItem { Text = "右スティック X軸", Value = 3 }); cmbKeyButton.Items.Add(new ComboItem { Text = "右スティック Y軸", Value = 4 });
                    break;
                case ActionType.XboxTrigger:
                    cmbKeyButton.Visible = true; cmbKeyButton.Items.Add(new ComboItem { Text = "左トリガー (LT)", Value = 1 }); cmbKeyButton.Items.Add(new ComboItem { Text = "右トリガー (RT)", Value = 2 });
                    break;
                case ActionType.MouseMoveRelative: case ActionType.MouseMoveContinuous: case ActionType.MouseMoveAbsoluteDesk: case ActionType.MouseMoveAbsoluteWin: case ActionType.MouseMoveAbsoluteHoverWin:
                    pnlMouseMove.Visible = true; btnCaptureCoord.Visible = (type != ActionType.MouseMoveContinuous); break;
                case ActionType.AppLaunch: case ActionType.FileOpen: case ActionType.AhkRun:
                    txtAppPath.Visible = true; btnBrowseApp.Visible = true; txtAppArgs.Visible = true; lblAppArgs.Visible = true; break;
                case ActionType.BackgroundControl: pnlBackground.Visible = true; break;
                case ActionType.Macro: btnEditMacro.Visible = true; break;
                case ActionType.ProfileSwitch: cmbProfileSwitchTarget.Visible = true; cmbProfileSwitchMode.Visible = true; break;
                case ActionType.StickToMouse: btnSetupStickMouse.Visible = true; break;
            }
            if (cmbKeyButton.Items.Count > 0) cmbKeyButton.SelectedIndex = 0;
            if (ResultBinding != null && ResultBinding.Action != null && ResultBinding.Action.ActionType == type) SetOutputTarget(ResultBinding.Action);
            cmbKeyButton.SelectedIndexChanged += cmbKeyButton_SelectedIndexChanged;
        }

        private void SetOutputTarget(ActionDef action)
        {
            if (!cmbKeyButton.Visible) return;
            if (action.ActionType == ActionType.Keyboard || action.ActionType == ActionType.ToggleHold || (action.ActionType == ActionType.BackgroundControl && action.BgActionMode == 1))
            {
                if (action.MultipleKeys != null && action.MultipleKeys.Count > 0)
                {
                    string keysStr = string.Join(" + ", action.MultipleKeys.Select(k => ((Keys)k).ToString()));
                    var customItem = new ComboItem { Text = $"[保存済] {keysStr}", Value = -2 };
                    cmbKeyButton.SelectedIndexChanged -= cmbKeyButton_SelectedIndexChanged;
                    cmbKeyButton.Items.Insert(0, customItem); cmbKeyButton.SelectedIndex = 0;
                    cmbKeyButton.SelectedIndexChanged += cmbKeyButton_SelectedIndexChanged;
                    return;
                }
            }
            for (int i = 0; i < cmbKeyButton.Items.Count; i++) if (((ComboItem)cmbKeyButton.Items[i]).Value == action.ArgumentNum) { cmbKeyButton.SelectedIndex = i; break; }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) return;
            ResultBinding.Name = txtName.Text; ResultBinding.BlockOriginalInput = chkBlockOriginalInput.Checked;
            ResultBinding.Condition = (TriggerCondition)cmbCondition.SelectedIndex; ResultBinding.ConditionParam = (int)numConditionParam.Value;
            ResultBinding.AxisRange = cmbAxisRange.SelectedIndex; ResultBinding.DeadZone = (int)numDeadZone.Value; ResultBinding.AccelerationCurve = cmbCurve.SelectedIndex;

            if (cmbActionType.SelectedItem is ComboItem actItem) ResultBinding.Action.ActionType = (ActionType)actItem.Value;
            
            if (cmbKeyButton.Visible && cmbKeyButton.SelectedItem is ComboItem cItem && cItem.Value >= 0) 
            { 
                ResultBinding.Action.ArgumentNum = cItem.Value; 
                ResultBinding.Action.MultipleKeys.Clear(); 
                if (cItem.Value > 0 && (ResultBinding.Action.ActionType == ActionType.Keyboard || ResultBinding.Action.ActionType == ActionType.ToggleHold)) 
                    ResultBinding.Action.MultipleKeys.Add(cItem.Value); 
            }

            if (ResultBinding.Action.ActionType == ActionType.ProfileSwitch) { ResultBinding.Action.ArgumentStr = cmbProfileSwitchTarget.SelectedItem?.ToString(); ResultBinding.Action.ArgumentNum = cmbProfileSwitchMode.SelectedIndex; }
            else if (ResultBinding.Action.ActionType == ActionType.BackgroundControl) {
                ResultBinding.Action.BgClassName = txtBgClassName.Text; ResultBinding.Action.BgWindowName = txtBgWindowName.Text;
                ResultBinding.Action.BgControlId = (int)numBgControlId.Value; ResultBinding.Action.BgActionMode = cmbBgAction.SelectedIndex;
                if (cmbBgAction.SelectedIndex == 1 && cmbBgKey.SelectedItem is ComboItem ki) ResultBinding.Action.ArgumentNum = ki.Value;
            }
            else { ResultBinding.Action.ArgumentStr = txtAppPath.Text; ResultBinding.Action.ArgumentExtraStr = txtAppArgs.Text; }

            ResultBinding.Action.MouseX = (int)numMouseX.Value; ResultBinding.Action.MouseY = (int)numMouseY.Value;
            ResultBinding.Action.UseVibration = chkVibrate.Checked; ResultBinding.Action.VibrateDuration = (int)numVibrateDuration.Value; ResultBinding.Action.VibrateTimes = (int)numVibrateTimes.Value;
            ResultBinding.PlayWavPath = txtWavPath.Text;

            this.DialogResult = DialogResult.OK; this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }

        private void btnReCaptureMain_Click(object sender, EventArgs e) { using (var capture = new CaptureForm(CaptureMode.SingleAny)) { if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedEvent != null) { var evt = capture.CapturedEvent; ResultBinding.DeviceIdentifier = evt.DeviceIdentifier; ResultBinding.InputType = evt.Type; ResultBinding.InputCode = (evt.Type == 1) ? evt.VKey : (int)evt.MouseButtonFlags; UpdateMainTriggerLabel(); } } }
        private void btnAddSubTrigger_Click(object sender, EventArgs e) { using (var capture = new CaptureForm(CaptureMode.SingleAny)) { if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedEvent != null) { var evt = capture.CapturedEvent; var key = new TriggerKey { DeviceIdentifier = evt.DeviceIdentifier, Type = evt.Type, Code = (evt.Type == 1) ? evt.VKey : (int)evt.MouseButtonFlags }; lstSubTriggers.Items.Add(key); } } }
        private void btnRemoveSubTrigger_Click(object sender, EventArgs e) { if (lstSubTriggers.SelectedIndex >= 0) lstSubTriggers.Items.RemoveAt(lstSubTriggers.SelectedIndex); }
        private void btnManualAddSub_Click(object sender, EventArgs e) { if (cmbManualSubTrigger.SelectedItem is ComboItem item) { int type = (item.Value & 0x010000) != 0 ? 1 : 0; var key = new TriggerKey { DeviceIdentifier = "Any", Type = type, Code = item.Value & 0xFFFF }; lstSubTriggers.Items.Add(key); } }
        private void cmbCondition_SelectedIndexChanged(object sender, EventArgs e) { int idx = cmbCondition.SelectedIndex; lblParam.Visible = numConditionParam.Visible = (idx == 1 || idx == 2); if (idx == 1) lblParam.Text = "長押し(ms):"; if (idx == 2) lblParam.Text = "連打(ms):"; }
        private void cmbKeyButton_SelectedIndexChanged(object sender, EventArgs e) { if (cmbKeyButton.SelectedItem is ComboItem cItem && cItem.Value == -1) { using (var capture = new CaptureForm(CaptureMode.MultiKeyboard)) { if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedKeys.Count > 0) { ResultBinding.Action.MultipleKeys = new List<int>(capture.CapturedKeys); string keysStr = string.Join(" + ", capture.CapturedKeys.Select(k => ((Keys)k).ToString())); var customItem = new ComboItem { Text = $"[保存済] {keysStr}", Value = -2 }; cmbKeyButton.SelectedIndexChanged -= cmbKeyButton_SelectedIndexChanged; cmbKeyButton.Items.Insert(0, customItem); cmbKeyButton.SelectedIndex = 0; cmbKeyButton.SelectedIndexChanged += cmbKeyButton_SelectedIndexChanged; } else cmbKeyButton.SelectedIndex = 0; } } }
        private void btnBrowseApp_Click(object sender, EventArgs e) { using (var ofd = new OpenFileDialog { Filter = "実行ファイル等|*.exe;*.ahk;*.bat;*.cmd;*.vbs|全て|*.*" }) { if (ofd.ShowDialog() == DialogResult.OK) txtAppPath.Text = ofd.FileName; } }
        private void btnBrowseWav_Click(object sender, EventArgs e) { using (var ofd = new OpenFileDialog { Filter = "WAVファイル|*.wav|全て|*.*" }) { if (ofd.ShowDialog() == DialogResult.OK) txtWavPath.Text = ofd.FileName; } }
        private void btnEditMacro_Click(object sender, EventArgs e) { using (var editor = new MacroEditorForm(ResultBinding.Action, _profileNames)) { editor.ShowDialog(this); } }

        private void cmbBgAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbBgKey.Visible = (cmbBgAction.SelectedIndex == 1);
            if (cmbBgKey.Visible && cmbBgKey.Items.Count == 0) {
                foreach (Keys key in Enum.GetValues(typeof(Keys))) cmbBgKey.Items.Add(new ComboItem { Text = key.ToString(), Value = (int)key });
                if (ResultBinding != null && ResultBinding.Action.BgActionMode == 1) {
                    for (int i = 0; i < cmbBgKey.Items.Count; i++) if (((ComboItem)cmbBgKey.Items[i]).Value == ResultBinding.Action.ArgumentNum) { cmbBgKey.SelectedIndex = i; break; }
                } else cmbBgKey.SelectedIndex = 0;
            }
        }

        private void btnCaptureCoord_Click(object sender, EventArgs e) {
            if (GlobalHookManager.Instance == null) return;
            var type = (ActionType)((ComboItem)cmbActionType.SelectedItem).Value;
            bool isRelative = (type == ActionType.MouseMoveRelative); bool isWindow = (type == ActionType.MouseMoveAbsoluteWin || type == ActionType.MouseMoveAbsoluteHoverWin);
            GlobalHookManager.POINT startPt = new GlobalHookManager.POINT();
            GlobalHookManager.Instance.StartCoordinateCapture((pt, canceled) => {
                if (canceled) return;
                if (isRelative) { startPt = pt; GlobalHookManager.Instance.StartCoordinateCapture((pt2, canceled2) => { if (canceled2) return; this.BeginInvoke(new Action(() => { numMouseX.Value = pt2.x - startPt.x; numMouseY.Value = pt2.y - startPt.y; })); }); }
                else {
                    int targetX = pt.x; int targetY = pt.y;
                    if (isWindow) { IntPtr hwnd = WindowFromPoint(new Point(pt.x, pt.y)); IntPtr root = GetAncestor(hwnd, 2); if (root != IntPtr.Zero) { Point ptScreen = new Point(pt.x, pt.y); ScreenToClient(root, ref ptScreen); targetX = ptScreen.X; targetY = ptScreen.Y; } }
                    this.BeginInvoke(new Action(() => { numMouseX.Value = targetX; numMouseY.Value = targetY; }));
                }
            });
        }
    }
}
