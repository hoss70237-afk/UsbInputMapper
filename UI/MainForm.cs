using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using Microsoft.Win32;
using Newtonsoft.Json;
using UsbInputMapper.Profiles;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }

        private readonly ProfileManager _profileManager;
        private readonly DirectInputManager _diManager;
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private static List<UsbInputMapper.Profiles.Binding> _clipboardBindings = new List<UsbInputMapper.Profiles.Binding>();
        private ContextMenuStrip _bindingsContextMenu;
        
        private Timer _monitorTimer;
        private DiagnosticEvent _lastPhys = null;
        private DiagnosticEvent _lastVirt = null;
        private ListViewItem _lastDiagItem = null;

        public MainForm(ProfileManager profileManager, DirectInputManager diManager)
        {
            Instance = this;
            
            InitializeComponent();
            _profileManager = profileManager;
            _diManager = diManager;

            lvwBindings.MultiSelect = true;
            SetupContextMenu();
            LoadGlobalSettings();
            LoadProfiles();

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                chkStartup.Checked = (key.GetValue("UsbInputMapper") != null);
            }

            _monitorTimer = new Timer { Interval = 100 };
            _monitorTimer.Tick += MonitorTimer_Tick;
        }

        // 【追加】外部から入力イベントを受け取り、該当するバインディング行をハイライトさせるメソッド
        public void HighlightBinding(int type, int code, bool isDown)
        {
            if (this.IsDisposed || !this.Visible) return;
            
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => HighlightBinding(type, code, isDown)));
                return;
            }

            bool isUpdated = false;
            foreach (ListViewItem item in lvwBindings.Items)
            {
                if (item.Tag is UsbInputMapper.Profiles.Binding b)
                {
                    if (b.InputType == type && b.InputCode == code)
                    {
                        if (isDown)
                        {
                            item.BackColor = Color.LightSkyBlue;
                        }
                        else
                        {
                            item.BackColor = SystemColors.Window;
                        }
                        isUpdated = true;
                    }
                }
            }

            if (isUpdated)
            {
                lvwBindings.Invalidate();
            }
        }

        private void LoadGlobalSettings()
        {
            chkGlobalChattering.Checked = _profileManager.GlobalConfig.EnableChatteringCanceler;
            numGlobalChatterMs.Value = _profileManager.GlobalConfig.ChatteringThresholdMs;
            numDoubleClick.Value = _profileManager.GlobalConfig.DoubleClickTimeMs;
            numTripleClick.Value = _profileManager.GlobalConfig.TripleClickTimeMs;
        }

        private void chkLog_CheckedChanged(object sender, EventArgs e)
        {
            InputLogger.IsLoggingEnabled = chkLog.Checked;
            if (chkLog.Checked)
            {
                lvwDiagnostic.Items.Clear();
                _lastPhys = null; _lastVirt = null; _lastDiagItem = null;
                
                InputLogger.OnDiagnostic += ProcessDiagnostic;

                if (GlobalHookManager.Instance != null) {
                    GlobalHookManager.Instance.IsRecording = true;
                    GlobalHookManager.Instance.OnRecordedInput += GlobalHook_OnRecordedInput;
                }
                if (_diManager != null) {
                    _diManager.OnInputEvent += DiManager_OnInputEvent;
                }
                
                _monitorTimer.Start();
            }
            else
            {
                InputLogger.OnDiagnostic -= ProcessDiagnostic;

                if (GlobalHookManager.Instance != null) {
                    GlobalHookManager.Instance.IsRecording = false;
                    GlobalHookManager.Instance.OnRecordedInput -= GlobalHook_OnRecordedInput;
                }
                if (_diManager != null) {
                    _diManager.OnInputEvent -= DiManager_OnInputEvent;
                }
                
                _monitorTimer.Stop();
            }
        }

        private void GlobalHook_OnRecordedInput(object sender, GlobalHookManager.HookInputEvent e)
        {
            InputLogger.LogDiagnostic(new DiagnosticEvent { IsPhysical = true, Timestamp = e.Timestamp, Type = e.Type, Code = e.Code, IsDown = e.IsDown });
        }

        private void DiManager_OnInputEvent(object sender, DirectInputEvent e)
        {
            InputLogger.LogDiagnostic(new DiagnosticEvent { IsPhysical = true, Timestamp = Environment.TickCount, Type = e.Type, Code = e.Code, Value = e.Value, IsDown = e.IsDown });
        }

        private void ProcessDiagnostic(DiagnosticEvent e)
        {
            if (this.InvokeRequired) { this.BeginInvoke(new Action(() => ProcessDiagnostic(e))); return; }

            string timeStr = DateTime.Now.ToString("HH:mm:ss.fff");
            string state = e.Type == 11 || e.Type == 12 ? $"Val:{e.Value}" : (e.IsDown ? "Down" : "Up");
            string name = UsbInputMapper.Profiles.Binding.GetCodeName(e.Type, e.Code);
            string text = $"{name} ({state})";

            if (e.IsPhysical)
            {
                if (_lastPhys != null && _lastPhys.Type == e.Type && _lastPhys.Code == e.Code && _lastDiagItem != null)
                {
                    _lastDiagItem.SubItems[1].Text = text;
                    _lastDiagItem.Text = timeStr;
                }
                else
                {
                    _lastDiagItem = new ListViewItem(timeStr);
                    _lastDiagItem.SubItems.Add(text);
                    _lastDiagItem.SubItems.Add("");
                    lvwDiagnostic.Items.Add(_lastDiagItem);
                    lvwDiagnostic.EnsureVisible(lvwDiagnostic.Items.Count - 1);
                }
                _lastPhys = e;
            }
            else
            {
                if (_lastVirt != null && _lastVirt.Type == e.Type && _lastVirt.Code == e.Code && _lastDiagItem != null)
                {
                    _lastDiagItem.SubItems[2].Text = text;
                    _lastDiagItem.Text = timeStr;
                }
                else
                {
                    if (_lastDiagItem == null || _lastDiagItem.SubItems[2].Text != "")
                    {
                        _lastDiagItem = new ListViewItem(timeStr);
                        _lastDiagItem.SubItems.Add("");
                        _lastDiagItem.SubItems.Add(text);
                        lvwDiagnostic.Items.Add(_lastDiagItem);
                        lvwDiagnostic.EnsureVisible(lvwDiagnostic.Items.Count - 1);
                    }
                    else
                    {
                        _lastDiagItem.SubItems[2].Text = text;
                    }
                }
                _lastVirt = e;
            }
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            if (GlobalHookManager.Instance != null && GlobalHookManager.Instance.EnableChatteringCanceler)
            {
                lblChatterCount.Text = $"ブロックしたチャタリング回数: {GlobalHookManager.Instance.BlockedChatterCount} 回";
            }
        }

        private void SetupContextMenu()
        {
            _bindingsContextMenu = new ContextMenuStrip();
            var mnuCopy = new ToolStripMenuItem("コピー");
            mnuCopy.Click += (s, e) => { _clipboardBindings.Clear(); foreach (ListViewItem item in lvwBindings.SelectedItems) { string json = JsonConvert.SerializeObject(item.Tag); _clipboardBindings.Add(JsonConvert.DeserializeObject<UsbInputMapper.Profiles.Binding>(json)); } };
            var mnuPaste = new ToolStripMenuItem("貼り付け");
            mnuPaste.Click += (s, e) => { if (lstProfiles.SelectedItem is Profile p && _clipboardBindings.Count > 0) { foreach (var b in _clipboardBindings) { string json = JsonConvert.SerializeObject(b); p.Bindings.Add(JsonConvert.DeserializeObject<UsbInputMapper.Profiles.Binding>(json)); } _profileManager.Save(); RefreshBindings(); } };
            
            var mnuExportB64 = new ToolStripMenuItem("クリップボードにエクスポート (Base64)");
            mnuExportB64.Click += (s, e) => {
                if (lvwBindings.SelectedItems.Count > 0) {
                    var list = lvwBindings.SelectedItems.Cast<ListViewItem>().Select(x => x.Tag as UsbInputMapper.Profiles.Binding).ToList();
                    string json = JsonConvert.SerializeObject(list);
                    string b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
                    Clipboard.SetText("UIMB:" + b64);
                    MessageBox.Show("クリップボードにエクスポートしました。\n他のPCやプロファイルでインポートできます。", "エクスポート成功");
                }
            };
            
            var mnuImportB64 = new ToolStripMenuItem("クリップボードからインポート");
            mnuImportB64.Click += (s, e) => {
                try {
                    string text = Clipboard.GetText();
                    if (text.StartsWith("UIMB:")) {
                        string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(text.Substring(5)));
                        var list = JsonConvert.DeserializeObject<List<UsbInputMapper.Profiles.Binding>>(json);
                        if (lstProfiles.SelectedItem is Profile p) {
                            p.Bindings.AddRange(list);
                            _profileManager.Save();
                            RefreshBindings();
                            MessageBox.Show($"{list.Count} 件のアイテムをインポートしました。", "インポート成功");
                        }
                    } else {
                        MessageBox.Show("クリップボードに有効なデータがありません。", "エラー");
                    }
                } catch (Exception ex) { MessageBox.Show("インポートに失敗しました:\n" + ex.Message, "エラー"); }
            };

            var mnuDelete = new ToolStripMenuItem("削除");
            mnuDelete.Click += (s, e) => btnDeleteBinding_Click(this, EventArgs.Empty);
            var mnuSelectAll = new ToolStripMenuItem("全て選択");
            mnuSelectAll.Click += (s, e) => { foreach (ListViewItem item in lvwBindings.Items) item.Selected = true; };
            
            _bindingsContextMenu.Items.Add(mnuCopy); _bindingsContextMenu.Items.Add(mnuPaste); 
            _bindingsContextMenu.Items.Add(new ToolStripSeparator()); 
            _bindingsContextMenu.Items.Add(mnuExportB64); _bindingsContextMenu.Items.Add(mnuImportB64);
            _bindingsContextMenu.Items.Add(new ToolStripSeparator()); 
            _bindingsContextMenu.Items.Add(mnuDelete); _bindingsContextMenu.Items.Add(new ToolStripSeparator()); _bindingsContextMenu.Items.Add(mnuSelectAll);
            
            lvwBindings.ContextMenuStrip = _bindingsContextMenu;
            _bindingsContextMenu.Opening += (s, e) => { 
                mnuCopy.Enabled = lvwBindings.SelectedItems.Count > 0; 
                mnuPaste.Enabled = _clipboardBindings.Count > 0 && lstProfiles.SelectedItem != null; 
                mnuExportB64.Enabled = lvwBindings.SelectedItems.Count > 0;
                mnuDelete.Enabled = lvwBindings.SelectedItems.Count > 0; 
            };
        }

        private void LoadProfiles()
        {
            int selected = lstProfiles.SelectedIndex;
            lstProfiles.Items.Clear();
            foreach (var profile in _profileManager.Profiles) lstProfiles.Items.Add(profile);
            if (lstProfiles.Items.Count > 0) lstProfiles.SelectedIndex = (selected >= 0 && selected < lstProfiles.Items.Count) ? selected : 0;
        }

        private void lstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile p) 
            {
                chkEnableXInput.Checked = p.EnableXInput;
                chkOverlayMark.Checked = p.OverlayShowMark;
                chkOverlayName.Checked = p.OverlayShowName;
            }
            RefreshBindings();
        }

        private void chkEnableXInput_CheckedChanged(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { p.EnableXInput = chkEnableXInput.Checked; _profileManager.Save(); } }
        private void chkOverlayMark_CheckedChanged(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { p.OverlayShowMark = chkOverlayMark.Checked; _profileManager.Save(); } }
        private void chkOverlayName_CheckedChanged(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { p.OverlayShowName = chkOverlayName.Checked; _profileManager.Save(); } }

        private void chkGlobalChattering_CheckedChanged(object sender, EventArgs e) { _profileManager.GlobalConfig.EnableChatteringCanceler = chkGlobalChattering.Checked; _profileManager.Save(); }
        private void numGlobalChatterMs_ValueChanged(object sender, EventArgs e) { _profileManager.GlobalConfig.ChatteringThresholdMs = (int)numGlobalChatterMs.Value; _profileManager.Save(); }
        private void numDoubleClick_ValueChanged(object sender, EventArgs e) { _profileManager.GlobalConfig.DoubleClickTimeMs = (int)numDoubleClick.Value; _profileManager.Save(); }
        private void numTripleClick_ValueChanged(object sender, EventArgs e) { _profileManager.GlobalConfig.TripleClickTimeMs = (int)numTripleClick.Value; _profileManager.Save(); }
        private void chkStartup_CheckedChanged(object sender, EventArgs e) { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true)) { if (chkStartup.Checked) key.SetValue("UsbInputMapper", Application.ExecutablePath); else key.DeleteValue("UsbInputMapper", false); } }
        private void btnControllerBase_Click(object sender, EventArgs e) { using (var f = new ControllerBaseForm(_profileManager, _diManager)) { f.ShowDialog(this); } }

        private string GetFriendlyTriggerString(UsbInputMapper.Profiles.Binding b)
        {
            var parts = new List<string>();
            if (b.SubTriggers != null)
            {
                foreach (var t in b.SubTriggers)
                {
                    string name = UsbInputMapper.Profiles.Binding.GetCodeName(t.Type, t.Code);
                    name = name.Replace("キーボード: ", "").Replace("マウスボタン: ", "Mouse").Replace("ControlKey", "Ctrl").Replace("ShiftKey", "Shift").Replace("Menu", "Alt");
                    parts.Add(name);
                }
            }
            string mainName = UsbInputMapper.Profiles.Binding.GetCodeName(b.InputType, b.InputCode);
            mainName = mainName.Replace("キーボード: ", "").Replace("マウスボタン: ", "Mouse").Replace("パッドボタン: ", "Pad").Replace("パッド軸: ", "Axis").Replace("ControlKey", "Ctrl").Replace("ShiftKey", "Shift").Replace("Menu", "Alt");
            parts.Add(mainName);
            
            string prefix = b.ClickTriggerCount == 2 ? "ダブル " : (b.ClickTriggerCount == 3 ? "トリプル " : "シングル ");
            string layerStr = b.RequiredLayer > 0 ? $"[Layer {b.RequiredLayer}] " : "";
            return layerStr + prefix + string.Join(" + ", parts);
        }

        private string GetConditionString(UsbInputMapper.Profiles.Binding b)
        {
            switch (b.Condition)
            {
                case TriggerCondition.Normal: return "通常";
                case TriggerCondition.Hold: return $"長押し({b.ConditionParam}ms)";
                case TriggerCondition.RapidFire: return $"連打({b.ConditionParam}ms)";
                case TriggerCondition.Release: return "離した時";
                case TriggerCondition.Sync: return "同期(連動)";
                default: return "不明";
            }
        }

        private void RefreshBindings()
        {
            lvwBindings.Items.Clear();
            if (lstProfiles.SelectedItem is Profile profile) { 
                foreach (var b in profile.Bindings) {
                    var item = new ListViewItem(b.Name);
                    item.SubItems.Add(GetFriendlyTriggerString(b));
                    item.SubItems.Add(GetConditionString(b));
                    item.SubItems.Add(b.Action.ToString());
                    item.Tag = b;
                    lvwBindings.Items.Add(item);
                } 
            }
        }

        private void btnAddProfile_Click(object sender, EventArgs e) { var p = new Profile { Name = "新規プロファイル" }; using (var ed = new ProfileEditorForm(p, _profileManager.Profiles)) { if (ed.ShowDialog() == DialogResult.OK) { _profileManager.Profiles.Add(p); _profileManager.Save(); LoadProfiles(); lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1; } } }
        private void btnEditProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { using (var ed = new ProfileEditorForm(p)) { if (ed.ShowDialog() == DialogResult.OK) { _profileManager.Save(); LoadProfiles(); } } } }
        private void btnDuplicateProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { _profileManager.DuplicateProfile(p); LoadProfiles(); lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1; } }
        private void btnDeleteProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && !p.IsDefault) { _profileManager.Profiles.Remove(p); _profileManager.Save(); LoadProfiles(); } }
        private void btnUpProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedIndex > 0) { _profileManager.MoveProfile(lstProfiles.SelectedIndex, -1); LoadProfiles(); } }
        private void btnDownProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedIndex >= 0 && lstProfiles.SelectedIndex < lstProfiles.Items.Count - 1) { _profileManager.MoveProfile(lstProfiles.SelectedIndex, 1); LoadProfiles(); } }

        private void btnExportProfile_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile p)
            {
                using (var sfd = new SaveFileDialog { Filter = "プロファイル JSON (*.json)|*.json", FileName = $"{p.Name}.json" })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(p, Formatting.Indented));
                        MessageBox.Show("プロファイルをエクスポートしました。", "成功");
                    }
                }
            }
        }

        private void btnImportProfile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "プロファイル JSON (*.json)|*.json" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var p = JsonConvert.DeserializeObject<Profile>(File.ReadAllText(ofd.FileName));
                        p.Name += " (インポート)";
                        p.IsDefault = false;
                        _profileManager.Profiles.Add(p);
                        _profileManager.Save();
                        LoadProfiles();
                        lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1;
                        MessageBox.Show("プロファイルをインポートしました。", "成功");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("プロファイルの読み込みに失敗しました:\n" + ex.Message, "エラー");
                    }
                }
            }
        }

        private void btnAddBinding_Click(object sender, EventArgs e)
        {
            if (!(lstProfiles.SelectedItem is Profile p)) return;
            using (var capture = new CaptureForm())
            {
                var res = capture.ShowDialog(this);
                if (res == DialogResult.OK && capture.CapturedEvent != null)
                {
                    var evt = capture.CapturedEvent;
                    var newBinding = new UsbInputMapper.Profiles.Binding();
                    newBinding.DeviceIdentifier = evt.DeviceIdentifier;
                    newBinding.InputType = evt.Type;
                    newBinding.InputCode = (evt.Type == 1) ? evt.VKey : (int)evt.MouseButtonFlags;
                    newBinding.Name = UsbInputMapper.Profiles.Binding.GetCodeName(newBinding.InputType, newBinding.InputCode);

                    using (var ed = new BindingEditorForm(newBinding, _profileManager.Profiles.Select(x => x.Name).ToList()))
                    {
                        if (ed.ShowDialog(this) == DialogResult.OK) 
                        { 
                            p.Bindings.Add(ed.ResultBinding); 
                            _profileManager.Save(); 
                            RefreshBindings(); 
                        }
                    }
                }
                else if (res == DialogResult.Retry) 
                {
                    using (var geForm = new RadialMenuSetupForm(null, _profileManager.Profiles.Select(x => x.Name).ToList())) { 
                        if (geForm.ShowDialog(this) == DialogResult.OK) { 
                            p.Bindings.Add(geForm.ResultBinding); 
                            _profileManager.Save(); 
                            RefreshBindings(); 
                        } 
                    }
                }
            }
        }
        
        private void btnEditBinding_Click(object sender, EventArgs e) 
        { 
            if (lvwBindings.SelectedItems.Count > 0 && lvwBindings.SelectedItems[0].Tag is UsbInputMapper.Profiles.Binding b) 
            { 
                if (b.InputType == 5 || b.Action.ActionType == ActionType.RadialMenu) 
                { 
                    using (var geForm = new RadialMenuSetupForm(b, _profileManager.Profiles.Select(x => x.Name).ToList())) { 
                        if (geForm.ShowDialog(this) == DialogResult.OK) { 
                            _profileManager.Save(); 
                            RefreshBindings(); 
                        } 
                    } 
                }
                else 
                { 
                    using (var ed = new BindingEditorForm(b, _profileManager.Profiles.Select(x => x.Name).ToList())) { 
                        if (ed.ShowDialog(this) == DialogResult.OK) { 
                            _profileManager.Save(); 
                            RefreshBindings(); 
                        } 
                    } 
                }
            } 
        }

        private void btnDeleteBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lvwBindings.SelectedItems.Count > 0) { foreach (ListViewItem item in lvwBindings.SelectedItems.Cast<ListViewItem>().ToList()) { p.Bindings.Remove((UsbInputMapper.Profiles.Binding)item.Tag); } _profileManager.Save(); RefreshBindings(); } }
        private void btnUpBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lvwBindings.SelectedItems.Count == 1 && lvwBindings.SelectedIndices[0] > 0) { int idx = lvwBindings.SelectedIndices[0]; _profileManager.MoveBinding(p.Bindings, idx, -1); RefreshBindings(); lvwBindings.Items[idx - 1].Selected = true; } }
        private void btnDownBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lvwBindings.SelectedItems.Count == 1 && lvwBindings.SelectedIndices[0] >= 0 && lvwBindings.SelectedIndices[0] < lvwBindings.Items.Count - 1) { int idx = lvwBindings.SelectedIndices[0]; _profileManager.MoveBinding(p.Bindings, idx, 1); RefreshBindings(); lvwBindings.Items[idx + 1].Selected = true; } }

        private void btnResetChatter_Click(object sender, EventArgs e)
        {
            if (GlobalHookManager.Instance != null) GlobalHookManager.Instance.ResetChatterCount();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
            else { 
                InputLogger.OnDiagnostic -= ProcessDiagnostic;
                _monitorTimer?.Stop(); 
            }
        }
    }
}
