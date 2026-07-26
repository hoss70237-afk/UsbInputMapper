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
        private NumericUpDown _numSize;

        private int _triggerType = -1;
        private int _triggerCode = -1;
        private string _triggerDevId = "Any";

        public RadialMenuSetupForm(UsbInputMapper.Profiles.Binding existingBinding = null, List<string> profileNames = null)
        {
            _profileNames = profileNames ?? new List<string>();
            this.Text = "ラジアルメニュー / ベゼル設定";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            _tabs = new TabControl { Location = new Point(10, 10), Size = new Size(360, 200) };
            _tabRadialMenu = new TabPage("ラジアルメニュー");
            _tabBezel = new TabPage("ベゼルタッチ");
            _tabs.TabPages.Add(_tabRadialMenu); _tabs.TabPages.Add(_tabBezel);

            SetupRadialMenuUI();
            SetupBezelUI();

            Button btnOk = new Button { Text = "OK", Location = new Point(210, 220), Size = new Size(75, 23) };
            btnOk.Click += BtnOk_Click;
            Button btnCancel = new Button { Text = "キャンセル", Location = new Point(295, 220), Size = new Size(75, 23) };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(_tabs); this.Controls.Add(btnOk); this.Controls.Add(btnCancel);

            if (existingBinding != null)
            {
                ResultBinding = existingBinding;
                if (existingBinding.InputType == 5)
                {
                    _tabs.SelectedTab = _tabBezel;
                }
                else
                {
                    _tabs.SelectedTab = _tabRadialMenu;
                    _triggerType = existingBinding.InputType; 
                    _triggerCode = existingBinding.InputCode; 
                    _triggerDevId = existingBinding.DeviceIdentifier;
                    // 名前空間を明示的に指定
                    _lblTrigger.Text = $"開始ボタン: {UsbInputMapper.Profiles.Binding.GetCodeName(_triggerType, _triggerCode)}";
                    _chkBlockOriginalInput.Checked = existingBinding.BlockOriginalInput;
                    
                    _cmbSlices.SelectedIndex = existingBinding.Action.RadialMenuSlices == 12 ? 1 : 0;
                    _numSize.Value = existingBinding.Action.RadialMenuSize;
                }
            }
            else
            {
                // 名前空間を明示的に指定
                ResultBinding = new UsbInputMapper.Profiles.Binding();
            }
        }

        private void SetupRadialMenuUI()
        {
            _btnCaptureTrigger = new Button { Text = "開始ボタン登録", Location = new Point(10, 15), Size = new Size(100, 25) };
            _lblTrigger = new Label { Text = "開始ボタン: 未設定", Location = new Point(120, 20), AutoSize = true };
            _btnCaptureTrigger.Click += (s, e) => {
                using(var cap = new CaptureForm(CaptureMode.SingleAny)) {
                    if (cap.ShowDialog(this) == DialogResult.OK && cap.CapturedEvent != null) {
                        var ev = cap.CapturedEvent; 
                        _triggerDevId = ev.DeviceIdentifier; 
                        _triggerType = ev.Type; 
                        _triggerCode = (ev.Type == 1) ? ev.VKey : (int)ev.MouseButtonFlags;
                        // 名前空間を明示的に指定
                        _lblTrigger.Text = $"開始ボタン: {UsbInputMapper.Profiles.Binding.GetCodeName(_triggerType, _triggerCode)}";
                    }
                }
            };

            _chkBlockOriginalInput = new CheckBox { Text = "本来の入力をブロック", Location = new Point(15, 60), AutoSize = true };
            
            Label lblSlices = new Label { Text = "分割数:", Location = new Point(15, 100), AutoSize = true };
            _cmbSlices = new ComboBox { Location = new Point(65, 97), Size = new Size(60, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbSlices.Items.Add("8分割"); _cmbSlices.Items.Add("12分割");
            _cmbSlices.SelectedIndex = 0;

            Label lblSize = new Label { Text = "サイズ:", Location = new Point(145, 100), AutoSize = true };
            _numSize = new NumericUpDown { Location = new Point(195, 97), Size = new Size(60, 20), Minimum = 100, Maximum = 1000, Value = 200 };

            _tabRadialMenu.Controls.Add(_btnCaptureTrigger);
            _tabRadialMenu.Controls.Add(_lblTrigger);
            _tabRadialMenu.Controls.Add(_chkBlockOriginalInput);
            _tabRadialMenu.Controls.Add(lblSlices);
            _tabRadialMenu.Controls.Add(_cmbSlices);
            _tabRadialMenu.Controls.Add(lblSize);
            _tabRadialMenu.Controls.Add(_numSize);
        }

        private void SetupBezelUI()
        {
            Label lblNotice = new Label { Text = "※現在ベゼルタッチ機能は未実装です", Location = new Point(10, 10), AutoSize = true, ForeColor = Color.Red };
            _tabBezel.Controls.Add(lblNotice);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_tabs.SelectedTab == _tabRadialMenu)
            {
                if (_triggerType == -1) { MessageBox.Show("開始ボタンを設定してください。"); return; }
                ResultBinding.InputType = _triggerType;
                ResultBinding.InputCode = _triggerCode;
                ResultBinding.DeviceIdentifier = _triggerDevId;
                ResultBinding.BlockOriginalInput = _chkBlockOriginalInput.Checked;
                ResultBinding.Name = "ラジアルメニュー起動";
                
                ResultBinding.Action.ActionType = ActionType.RadialMenu;
                ResultBinding.Action.RadialMenuSlices = _cmbSlices.SelectedIndex == 0 ? 8 : 12;
                ResultBinding.Action.RadialMenuSize = (int)_numSize.Value;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
