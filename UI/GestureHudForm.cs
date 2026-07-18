using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public class GestureEdgeSetupForm : Form
    {
        public UsbInputMapper.Profiles.Binding ResultBinding { get; private set; }
        private List<string> _profileNames;

        private TabControl _tabs;
        private TabPage _tabGesture, _tabBezel;

        // Gesture UI
        private Button _btnCaptureTrigger;
        private Label _lblTrigger;
        private ComboBox _cmbSlices;
        private ListBox _lstDirs;
        private Button _btnEditDir;
        private NumericUpDown _numSize;

        private int _triggerType = -1;
        private int _triggerCode = -1;
        private string _triggerDevId = "Any";

        // Bezel UI
        private ComboBox _cmbBezelPos;
        private NumericUpDown _numHoverTime;
        private Label _lblBezelAction;
        private Button _btnEditBezelAction;
        private ActionDef _bezelAction;

        public GestureEdgeSetupForm(UsbInputMapper.Profiles.Binding existingBinding = null, List<string> profileNames = null)
        {
            _profileNames = profileNames ?? new List<string>();
            this.Text = "ジェスチャー / ベゼル設定";
            this.Size = new Size(400, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            _tabs = new TabControl { Location = new Point(10, 10), Size = new Size(360, 260) };
            _tabGesture = new TabPage("ジェスチャー(HUD)");
            _tabBezel = new TabPage("ベゼルタッチ");
            _tabs.TabPages.Add(_tabGesture); _tabs.TabPages.Add(_tabBezel);

            SetupGestureUI();
            SetupBezelUI();

            Button btnOk = new Button { Text = "OK", Location = new Point(210, 280), Size = new Size(75, 23) };
            btnOk.Click += BtnOk_Click;
            Button btnCancel = new Button { Text = "キャンセル", Location = new Point(295, 280), Size = new Size(75, 23) };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(_tabs); this.Controls.Add(btnOk); this.Controls.Add(btnCancel);

            if (existingBinding != null)
            {
                ResultBinding = existingBinding;
                if (existingBinding.InputType == 5) // Bezel
                {
                    _tabs.SelectedTab = _tabBezel;
                    _cmbBezelPos.SelectedIndex = existingBinding.InputCode;
                    _numHoverTime.Value = existingBinding.ConditionParam;
                    _bezelAction = existingBinding.Action;
                    _lblBezelAction.Text = _bezelAction.ToString();
                }
                else // Gesture
                {
                    _tabs.SelectedTab = _tabGesture;
                    _triggerType = existingBinding.InputType; _triggerCode = existingBinding.InputCode; _triggerDevId = existingBinding.DeviceIdentifier;
                    _lblTrigger.Text = $"開始ボタン: {UsbInputMapper.Profiles.Binding.GetCodeName(_triggerType, _triggerCode)}";
                    _cmbSlices.SelectedIndex = existingBinding.Action.GestureSlices == 24 ? 1 : 0;
                    _numSize.Value = existingBinding.Action.GestureSize;
                    
                    // ★修正: ジェスチャーの編集時にベゼルアクションが null になってクラッシュするのを防ぐ
                    _bezelAction = new ActionDef(); 
                }
            }
            else
            {
                ResultBinding = new UsbInputMapper.Profiles.Binding();
                _bezelAction = new ActionDef();
            }
        }

        private void SetupGestureUI()
        {
            _btnCaptureTrigger = new Button { Text = "開始ボタン登録", Location = new Point(10, 10), Size = new Size(100, 23) };
            _lblTrigger = new Label { Text = "開始ボタン: 未設定", Location = new Point(120, 15), AutoSize = true };
            _btnCaptureTrigger.Click += (s, e) => {
                using(var cap = new CaptureForm(CaptureMode.SingleAny)) {
                    if (cap.ShowDialog(this) == DialogResult.OK && cap.CapturedEvent != null) {
                        var ev = cap.CapturedEvent; _triggerDevId = ev.DeviceIdentifier; _triggerType = ev.Type; _triggerCode = ev.Type == 1 ? ev.VKey : (int)ev.MouseButtonFlags;
                        _lblTrigger.Text = $"開始ボタン: {UsbInputMapper.Profiles.Binding.GetCodeName(_triggerType, _triggerCode)}";
                    }
                }
            };

            Label l1 = new Label { Text = "分割数:", Location = new Point(10, 45), AutoSize = true };
            _cmbSlices = new ComboBox { Location = new Point(60, 42), Size = new Size(60, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbSlices.Items.AddRange(new[] { "8", "24" }); _cmbSlices.SelectedIndex = 0;
            _cmbSlices.SelectedIndexChanged += (s, e) => RefreshDirList();

            Label l2 = new Label { Text = "サイズ:", Location = new Point(140, 45), AutoSize = true };
            _numSize = new NumericUpDown { Location = new Point(180, 42), Size = new Size(60, 20), Maximum = 1000, Value = 200 };

            _lstDirs = new ListBox { Location = new Point(10, 70), Size = new Size(250, 148) };
            _btnEditDir = new Button { Text = "割当編集", Location = new Point(270, 70), Size = new Size(70, 23) };
            _btnEditDir.Click += BtnEditDir_Click;

            _tabGesture.Controls.Add(_btnCaptureTrigger); _tabGesture.Controls.Add(_lblTrigger);
            _tabGesture.Controls.Add(l1); _tabGesture.Controls.Add(_cmbSlices); _tabGesture.Controls.Add(l2); _tabGesture.Controls.Add(_numSize);
            _tabGesture.Controls.Add(_lstDirs); _tabGesture.Controls.Add(_btnEditDir);
            RefreshDirList();
        }

        private void RefreshDirList()
        {
            if (ResultBinding == null || ResultBinding.Action == null) return;
            int slices = _cmbSlices.SelectedIndex == 1 ? 24 : 8;
            if (ResultBinding.Action.GestureDirections.Count != slices)
            {
                var newList = new List<GestureDirection>();
                for (int i = 0; i < slices; i++)
                {
                    var existing = ResultBinding.Action.GestureDirections.FirstOrDefault(x => x.DirectionIndex == i);
                    newList.Add(existing ?? new GestureDirection { DirectionIndex = i, Label = $"Dir {i}" });
                }
                ResultBinding.Action.GestureDirections = newList;
            }
            int idx = _lstDirs.SelectedIndex;
            _lstDirs.Items.Clear();
            foreach (var d in ResultBinding.Action.GestureDirections) _lstDirs.Items.Add($"[{d.DirectionIndex}] {d.Label} -> {d.Action.ToString()}");
            if (idx >= 0 && idx < _lstDirs.Items.Count) _lstDirs.SelectedIndex = idx;
        }

        private void BtnEditDir_Click(object sender, EventArgs e)
        {
            int idx = _lstDirs.SelectedIndex;
            if (idx >= 0)
            {
                var dir = ResultBinding.Action.GestureDirections[idx];
                var dummyBinding = new UsbInputMapper.Profiles.Binding { Action = dir.Action, Name = dir.Label };
                using (var editor = new BindingEditorForm(dummyBinding, _profileNames))
                {
                    if (editor.ShowDialog(this) == DialogResult.OK)
                    {
                        dir.Action = editor.ResultBinding.Action;
                        dir.Label = editor.ResultBinding.Name;
                        RefreshDirList();
                    }
                }
            }
        }

        private void SetupBezelUI()
        {
            Label l1 = new Label { Text = "タッチ位置:", Location = new Point(20, 20), AutoSize = true };
            _cmbBezelPos = new ComboBox { Location = new Point(90, 18), Size = new Size(120, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbBezelPos.Items.AddRange(new[] { "左上隅", "上辺(左)", "上辺(中)", "上辺(右)", "右上隅", "右辺(上)", "右辺(中)", "右辺(下)", "右下隅", "下辺(右)", "下辺(中)", "下辺(左)", "左下隅", "左辺(下)", "左辺(中)", "左辺(上)" });
            _cmbBezelPos.SelectedIndex = 0;

            Label l2 = new Label { Text = "待機(ms):", Location = new Point(20, 50), AutoSize = true };
            _numHoverTime = new NumericUpDown { Location = new Point(90, 48), Size = new Size(80, 20), Maximum = 10000, Value = 0 };

            _lblBezelAction = new Label { Text = "アクションなし", Location = new Point(20, 90), AutoSize = true };
            _btnEditBezelAction = new Button { Text = "アクション設定", Location = new Point(20, 110), Size = new Size(120, 23) };
            _btnEditBezelAction.Click += (s, e) => {
                var dummyBinding = new UsbInputMapper.Profiles.Binding { Action = _bezelAction };
                using (var editor = new BindingEditorForm(dummyBinding, _profileNames))
                {
                    if (editor.ShowDialog(this) == DialogResult.OK) { _bezelAction = editor.ResultBinding.Action; _lblBezelAction.Text = _bezelAction.ToString(); }
                }
            };

            _tabBezel.Controls.Add(l1); _tabBezel.Controls.Add(_cmbBezelPos);
            _tabBezel.Controls.Add(l2); _tabBezel.Controls.Add(_numHoverTime);
            _tabBezel.Controls.Add(_lblBezelAction); _tabBezel.Controls.Add(_btnEditBezelAction);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_tabs.SelectedTab == _tabGesture)
            {
                if (_triggerType == -1) { MessageBox.Show("開始ボタンを設定してください。"); return; }
                ResultBinding.InputType = _triggerType; ResultBinding.InputCode = _triggerCode; ResultBinding.DeviceIdentifier = _triggerDevId;
                ResultBinding.Name = "ジェスチャー";
                ResultBinding.Action.ActionType = ActionType.Gesture;
                ResultBinding.Action.GestureSlices = _cmbSlices.SelectedIndex == 1 ? 24 : 8;
                ResultBinding.Action.GestureSize = (int)_numSize.Value;
            }
            else
            {
                ResultBinding.InputType = 5; // Bezel
                ResultBinding.InputCode = _cmbBezelPos.SelectedIndex;
                ResultBinding.DeviceIdentifier = "Any";
                ResultBinding.Name = "ベゼルタッチ";
                ResultBinding.Condition = TriggerCondition.Hold;
                ResultBinding.ConditionParam = (int)_numHoverTime.Value;
                ResultBinding.Action = _bezelAction;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
