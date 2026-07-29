using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public class RadialMenuSetupForm : Form
    {
        public UsbInputMapper.Profiles.Binding ResultBinding { get; private set; }
        private List<string> _profileNames;

        private TabControl _tabs;
        private TabPage _tabRadialMenu, _tabBezel;

        private Button _btnCaptureTrigger;
        private Label _lblTrigger;
        private CheckBox _chkBlockOriginalInput;
        private ComboBox _cmbSlices;
        private ComboBox _cmbMode; 
        
        // 任意確定ボタン用UI
        private Button _btnCaptureConfirm;
        private Label _lblConfirm;
        private int _confirmType = -1;
        private int _confirmCode = -1;

        private NumericUpDown _numSize;
        private ListBox _lstDirections;
        private Button _btnEditDirectionAction;
        
        private Label lblSlices;
        private Label lblSize;
        private Label lblDirs;

        private ComboBox _cmbBezelArea;
        private CheckBox _chkBezelBlock;
        private Button _btnEditBezelAction;
        private Label _lblBezelStatus;

        private int _triggerType = -1;
        private int _triggerCode = -1;
        private string _triggerDevId = "Any";

        private static readonly string[] BezelNames = {
            "0: 左上隅", "1: 上辺(左)", "2: 上辺(中)", "3: 上辺(右)",
            "4: 右上隅", "5: 右辺(上)", "6: 右辺(中)", "7: 右辺(下)",
            "8: 右下隅", "9: 下辺(右)", "10: 下辺(中)", "11: 下辺(左)",
            "12: 左下隅", "13: 左辺(下)", "14: 左辺(中)", "15: 左辺(上)"
        };

        public RadialMenuSetupForm(UsbInputMapper.Profiles.Binding existingBinding = null, List<string> profileNames = null)
        {
            _profileNames = profileNames ?? new List<string>();
            this.Text = "ラジアルメニュー / ベゼル設定";
            this.Size = new Size(460, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            ResultBinding = existingBinding ?? new UsbInputMapper.Profiles.Binding();

            _tabs = new TabControl { Location = new Point(10, 10), Size = new Size(424, 380) };
            _tabRadialMenu = new TabPage("ラジアルメニュー");
            _tabBezel = new TabPage("ベゼルタッチ");
            _tabs.TabPages.Add(_tabRadialMenu); _tabs.TabPages.Add(_tabBezel);

            SetupRadialMenuUI();
            SetupBezelUI();

            Button btnOk = new Button { Text = "OK", Location = new Point(270, 400), Size = new Size(75, 25) };
            btnOk.Click += BtnOk_Click;
            Button btnCancel = new Button { Text = "キャンセル", Location = new Point(355, 400), Size = new Size(75, 25) };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(_tabs); this.Controls.Add(btnOk); this.Controls.Add(btnCancel);

            LoadBindingData();
        }

        private void SetupRadialMenuUI()
        {
            _btnCaptureTrigger = new Button { Text = "開始ボタン登録", Location = new Point(10, 15), Size = new Size(110, 25) };
            _lblTrigger = new Label { Text = "開始ボタン: 未設定", Location = new Point(130, 20), AutoSize = true };
            _btnCaptureTrigger.Click += (s, e) => {
                using(var cap = new CaptureForm(CaptureMode.SingleAny)) {
                    if (cap.ShowDialog(this) == DialogResult.OK && cap.CapturedEvent != null) {
                        var ev = cap.CapturedEvent; 
                        _triggerDevId = ev.DeviceIdentifier; 
                        _triggerType = ev.Type; 
                        _triggerCode = (ev.Type == 1) ? ev.VKey : (int)ev.MouseButtonFlags;
                        _lblTrigger.Text = $"開始ボタン: {UsbInputMapper.Profiles.Binding.GetCodeName(_triggerType, _triggerCode)}";
                    }
                }
            };

            _chkBlockOriginalInput = new CheckBox { Text = "本来の入力をブロック", Location = new Point(10, 48), AutoSize = true };
            
            Label lblMode = new Label { Text = "起動モード:", Location = new Point(10, 75), AutoSize = true };
            _cmbMode = new ComboBox { Location = new Point(80, 72), Size = new Size(200, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbMode.Items.Add("ホールド (開始ボタンを離して確定)");
            _cmbMode.Items.Add("ボタン確定 (任意のボタンを押して確定)");
            _cmbMode.SelectedIndex = 0;

            _btnCaptureConfirm = new Button { Text = "確定ボタン登録", Location = new Point(10, 100), Size = new Size(110, 25), Visible = false };
            _lblConfirm = new Label { Text = "確定ボタン: 未設定", Location = new Point(130, 105), AutoSize = true, Visible = false };
            _btnCaptureConfirm.Click += (s, e) => {
                using(var cap = new CaptureForm(CaptureMode.SingleAny)) {
                    if (cap.ShowDialog(this) == DialogResult.OK && cap.CapturedEvent != null) {
                        var ev = cap.CapturedEvent; 
                        _confirmType = ev.Type; 
                        _confirmCode = (ev.Type == 1) ? ev.VKey : (int)ev.MouseButtonFlags;
                        _lblConfirm.Text = $"確定ボタン: {UsbInputMapper.Profiles.Binding.GetCodeName(_confirmType, _confirmCode)}";
                    }
                }
            };

            lblSlices = new Label { Text = "分割数:", Location = new Point(10, 105), AutoSize = true };
            _cmbSlices = new ComboBox { Location = new Point(60, 102), Size = new Size(70, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbSlices.Items.Add("8分割"); _cmbSlices.Items.Add("12分割");
            _cmbSlices.SelectedIndex = 0;
            _cmbSlices.SelectedIndexChanged += (s, e) => RebuildDirectionsList();

            lblSize = new Label { Text = "サイズ:", Location = new Point(150, 105), AutoSize = true };
            _numSize = new NumericUpDown { Location = new Point(200, 102), Size = new Size(60, 20), Minimum = 100, Maximum = 1000, Value = 200 };

            lblDirs = new Label { Text = "各方向のアクション設定:", Location = new Point(10, 135), AutoSize = true };
            _lstDirections = new ListBox { Location = new Point(10, 155), Size = new Size(280, 150) };
            
            _btnEditDirectionAction = new Button { Text = "アクション編集...", Location = new Point(300, 155), Size = new Size(100, 30) };
            _btnEditDirectionAction.Click += BtnEditDirectionAction_Click;

            _cmbMode.SelectedIndexChanged += (s, e) => {
                bool isButtonMode = _cmbMode.SelectedIndex == 1;
                _btnCaptureConfirm.Visible = isButtonMode;
                _lblConfirm.Visible = isButtonMode;
                
                int offset = isButtonMode ? 35 : 0;
                lblSlices.Top = 105 + offset;
                _cmbSlices.Top = 102 + offset;
                lblSize.Top = 105 + offset;
                _numSize.Top = 102 + offset;
                lblDirs.Top = 135 + offset;
                _lstDirections.Top = 155 + offset;
                _btnEditDirectionAction.Top = 155 + offset;
            };

            _tabRadialMenu.Controls.Add(_btnCaptureTrigger);
            _tabRadialMenu.Controls.Add(_lblTrigger);
            _tabRadialMenu.Controls.Add(_chkBlockOriginalInput);
            _tabRadialMenu.Controls.Add(lblMode);
            _tabRadialMenu.Controls.Add(_cmbMode);
            _tabRadialMenu.Controls.Add(_btnCaptureConfirm);
            _tabRadialMenu.Controls.Add(_lblConfirm);
            _tabRadialMenu.Controls.Add(lblSlices);
            _tabRadialMenu.Controls.Add(_cmbSlices);
            _tabRadialMenu.Controls.Add(lblSize);
            _tabRadialMenu.Controls.Add(_numSize);
            _tabRadialMenu.Controls.Add(lblDirs);
            _tabRadialMenu.Controls.Add(_lstDirections);
            _tabRadialMenu.Controls.Add(_btnEditDirectionAction);
        }

        private void SetupBezelUI()
        {
            Label lblArea = new Label { Text = "ベゼル領域 (画面端25px):", Location = new Point(15, 20), AutoSize = true };
            _cmbBezelArea = new ComboBox { Location = new Point(160, 17), Size = new Size(180, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var bName in BezelNames) _cmbBezelArea.Items.Add(bName);
            _cmbBezelArea.SelectedIndex = 0;

            _chkBezelBlock = new CheckBox { Text = "画面端クリックを元のアプリからブロックする", Location = new Point(15, 55), AutoSize = true, Checked = true };

            _btnEditBezelAction = new Button { Text = "発動アクションを設定...", Location = new Point(15, 90), Size = new Size(180, 30) };
            _btnEditBezelAction.Click += BtnEditBezelAction_Click;

            _lblBezelStatus = new Label { Text = "アクション: なし", Location = new Point(15, 135), AutoSize = true, ForeColor = Color.Blue };

            _tabBezel.Controls.Add(lblArea);
            _tabBezel.Controls.Add(_cmbBezelArea);
            _tabBezel.Controls.Add(_chkBezelBlock);
            _tabBezel.Controls.Add(_btnEditBezelAction);
            _tabBezel.Controls.Add(_lblBezelStatus);
        }

        private void LoadBindingData()
        {
            if (ResultBinding.InputType == 5)
            {
                _tabs.SelectedTab = _tabBezel;
                _cmbBezelArea.SelectedIndex = Math.Min(15, Math.Max(0, ResultBinding.InputCode));
                _chkBezelBlock.Checked = ResultBinding.BlockOriginalInput;
                UpdateBezelStatusText();
            }
            else
            {
                _tabs.SelectedTab = _tabRadialMenu;
                _triggerType = ResultBinding.InputType;
                _triggerCode = ResultBinding.InputCode;
                _triggerDevId = ResultBinding.DeviceIdentifier;
                
                if (_triggerType != -1)
                    _lblTrigger.Text = $"開始ボタン: {UsbInputMapper.Profiles.Binding.GetCodeName(_triggerType, _triggerCode)}";

                _chkBlockOriginalInput.Checked = ResultBinding.BlockOriginalInput;
                
                _confirmType = ResultBinding.Action.RadialMenuConfirmType;
                _confirmCode = ResultBinding.Action.RadialMenuConfirmCode;
                if (_confirmType != -1)
                    _lblConfirm.Text = $"確定ボタン: {UsbInputMapper.Profiles.Binding.GetCodeName(_confirmType, _confirmCode)}";

                _cmbMode.SelectedIndex = (ResultBinding.Action.RadialMenuMode == 1) ? 1 : 0;
                _cmbSlices.SelectedIndex = (ResultBinding.Action.RadialMenuSlices == 12) ? 1 : 0;
                _numSize.Value = Math.Max(100, ResultBinding.Action.RadialMenuSize);

                RebuildDirectionsList();
            }
        }

        private void RebuildDirectionsList()
        {
            int slices = _cmbSlices.SelectedIndex == 0 ? 8 : 12;
            ResultBinding.Action.RadialMenuSlices = slices;

            while (ResultBinding.Action.RadialMenuDirections.Count < slices)
            {
                int idx = ResultBinding.Action.RadialMenuDirections.Count;
                ResultBinding.Action.RadialMenuDirections.Add(new RadialMenuDirection { DirectionIndex = idx, Label = $"方向 {idx}" });
            }

            _lstDirections.Items.Clear();
            for (int i = 0; i < slices; i++)
            {
                var dir = ResultBinding.Action.RadialMenuDirections[i];
                string actText = dir.Action != null ? dir.Action.ToString() : "なし";
                _lstDirections.Items.Add($"[{i}] ({dir.Label}) => {actText}");
            }
        }

        private void BtnEditDirectionAction_Click(object sender, EventArgs e)
        {
            int idx = _lstDirections.SelectedIndex;
            if (idx < 0 || idx >= ResultBinding.Action.RadialMenuDirections.Count)
            {
                MessageBox.Show("編集する方向を選択してください。", "案内");
                return;
            }

            var dir = ResultBinding.Action.RadialMenuDirections[idx];
            var dummyBinding = new UsbInputMapper.Profiles.Binding { Action = dir.Action };

            using (var ed = new BindingEditorForm(dummyBinding, _profileNames))
            {
                if (ed.ShowDialog(this) == DialogResult.OK)
                {
                    dir.Action = ed.ResultBinding.Action;
                    dir.Label = ed.ResultBinding.Name;
                    RebuildDirectionsList();
                    _lstDirections.SelectedIndex = idx;
                }
            }
        }

        private void BtnEditBezelAction_Click(object sender, EventArgs e)
        {
            var dummyBinding = new UsbInputMapper.Profiles.Binding { Action = ResultBinding.Action };
            using (var ed = new BindingEditorForm(dummyBinding, _profileNames))
            {
                if (ed.ShowDialog(this) == DialogResult.OK)
                {
                    ResultBinding.Action = ed.ResultBinding.Action;
                    UpdateBezelStatusText();
                }
            }
        }

        private void UpdateBezelStatusText()
        {
            _lblBezelStatus.Text = $"アクション: {ResultBinding.Action.ToString()}";
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_tabs.SelectedTab == _tabRadialMenu)
            {
                if (_triggerType == -1) { MessageBox.Show("開始ボタンを設定してください。"); return; }
                if (_cmbMode.SelectedIndex == 1 && _confirmType == -1) { MessageBox.Show("任意のボタン確定モードの場合、確定ボタンを設定してください。"); return; }
                
                ResultBinding.InputType = _triggerType;
                ResultBinding.InputCode = _triggerCode;
                ResultBinding.DeviceIdentifier = _triggerDevId;
                ResultBinding.BlockOriginalInput = _chkBlockOriginalInput.Checked;
                ResultBinding.Name = "ラジアルメニュー起動";
                
                ResultBinding.Action.ActionType = ActionType.RadialMenu;
                ResultBinding.Action.RadialMenuMode = _cmbMode.SelectedIndex;
                ResultBinding.Action.RadialMenuConfirmType = _confirmType;
                ResultBinding.Action.RadialMenuConfirmCode = _confirmCode;
                
                ResultBinding.Action.RadialMenuSlices = _cmbSlices.SelectedIndex == 0 ? 8 : 12;
                ResultBinding.Action.RadialMenuSize = (int)_numSize.Value;
            }
            else
            {
                ResultBinding.InputType = 5;
                ResultBinding.InputCode = _cmbBezelArea.SelectedIndex;
                ResultBinding.DeviceIdentifier = "SystemBezel";
                ResultBinding.BlockOriginalInput = _chkBezelBlock.Checked;
                ResultBinding.Name = $"ベゼル: {BezelNames[_cmbBezelArea.SelectedIndex]}";
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
