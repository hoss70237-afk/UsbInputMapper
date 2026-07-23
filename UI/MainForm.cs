using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using UsbInputMapper.Profiles;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public partial class MainForm : Form
    {
        private readonly ProfileManager _profileManager;
        private readonly DirectInputManager _diManager;
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private static List<UsbInputMapper.Profiles.Binding> _clipboardBindings = new List<UsbInputMapper.Profiles.Binding>();
        private ContextMenuStrip _bindingsContextMenu;
        
        private Timer _monitorTimer;

        public MainForm(ProfileManager profileManager, DirectInputManager diManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            _diManager = diManager;

            lstBindings.SelectionMode = SelectionMode.MultiExtended;
            SetupContextMenu();
            LoadProfiles();

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                chkStartup.Checked = (key.GetValue("UsbInputMapper") != null);
            }

            InputLogger.OnLog += LogMessage;
            
            _monitorTimer = new Timer { Interval = 100 };
            _monitorTimer.Tick += MonitorTimer_Tick;
        }

        private void LogMessage(string msg)
        {
            if (this.IsDisposed) return;
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(() => LogMessage(msg)));
                return;
            }
            txtLog.AppendText(msg + Environment.NewLine);
        }

        private void chkLog_CheckedChanged(object sender, EventArgs e)
        {
            InputLogger.IsLoggingEnabled = chkLog.Checked;
            if (chkLog.Checked)
            {
                _monitorTimer.Start();
                txtLog.AppendText("=== モニター開始 ===" + Environment.NewLine);
            }
            else
            {
                _monitorTimer.Stop();
                txtLog.AppendText("=== モニター停止 ===" + Environment.NewLine);
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
            mnuCopy.Click += (s, e) => { _clipboardBindings.Clear(); foreach (ListViewItem item in lstBindings.SelectedItems) { string json = JsonConvert.SerializeObject(item.Tag); _clipboardBindings.Add(JsonConvert.DeserializeObject<UsbInputMapper.Profiles.Binding>(json)); } };
            var mnuPaste = new ToolStripMenuItem("貼り付け");
            mnuPaste.Click += (s, e) => { if (lstProfiles.SelectedItem is Profile p && _clipboardBindings.Count > 0) { foreach (var b in _clipboardBindings) { string json = JsonConvert.SerializeObject(b); p.Bindings.Add(JsonConvert.DeserializeObject<UsbInputMapper.Profiles.Binding>(json)); } _profileManager.Save(); RefreshBindings(); } };
            var mnuDelete = new ToolStripMenuItem("削除");
            mnuDelete.Click += (s, e) => btnDeleteBinding_Click(this, EventArgs.Empty);
            var mnuSelectAll = new ToolStripMenuItem("全て選択");
            mnuSelectAll.Click += (s, e) => { for (int i = 0; i < lstBindings.Items.Count; i++) lstBindings.SetSelected(i, true); };
            _bindingsContextMenu.Items.Add(mnuCopy); _bindingsContextMenu.Items.Add(mnuPaste); _bindingsContextMenu.Items.Add(new ToolStripSeparator()); _bindingsContextMenu.Items.Add(mnuDelete); _bindingsContextMenu.Items.Add(new ToolStripSeparator()); _bindingsContextMenu.Items.Add(mnuSelectAll);
            lstBindings.ContextMenuStrip = _bindingsContextMenu;
            _bindingsContextMenu.Opening += (s, e) => { mnuCopy.Enabled = lstBindings.SelectedItems.Count > 0; mnuPaste.Enabled = _clipboardBindings.Count > 0 && lstProfiles.SelectedItem != null; mnuDelete.Enabled = lstBindings.SelectedItems.Count > 0; };
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
                chkChattering.Checked = p.EnableChatteringCanceler;
                numChatterMs.Value = p.ChatteringThresholdMs;
                
                chkOverlayMark.Checked = p.OverlayShowMark;
                chkOverlayName.Checked = p.OverlayShowName;
            }
            RefreshBindings();
        }

        private void chkEnableXInput_CheckedChanged(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { p.EnableXInput = chkEnableXInput.Checked; _profileManager.Save(); } }
        private void chkChattering_CheckedChanged(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { p.EnableChatteringCanceler = chkChattering.Checked; p.ChatteringThresholdMs = (int)numChatterMs.Value; _profileManager.Save(); } }
        private void numChatterMs_ValueChanged(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { p.ChatteringThresholdMs = (int)numChatterMs.Value; _profileManager.Save(); } }
        private void chkOverlayMark_CheckedChanged(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { p.OverlayShowMark = chkOverlayMark.Checked; _profileManager.Save(); } }
        private void chkOverlayName_CheckedChanged(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { p.OverlayShowName = chkOverlayName.Checked; _profileManager.Save(); } }

        private void chkStartup_CheckedChanged(object sender, EventArgs e) { using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true)) { if (chkStartup.Checked) key.SetValue("UsbInputMapper", Application.ExecutablePath); else key.DeleteValue("UsbInputMapper", false); } }
        private void btnControllerBase_Click(object sender, EventArgs e) { using (var f = new ControllerBaseForm(_profileManager, _diManager)) { f.ShowDialog(this); } }

        private void RefreshBindings()
        {
            lstBindings.Items.Clear();
            if (lstProfiles.SelectedItem is Profile profile) { foreach (var b in profile.Bindings) { lstBindings.Items.Add(new ListViewItem($"[{b.Name}] {b.GetTriggerString()} => {b.Action.ToString()}") { Tag = b }); } }
        }

        private void btnAddProfile_Click(object sender, EventArgs e) { var p = new Profile { Name = "新規プロファイル" }; using (var ed = new ProfileEditorForm(p, _profileManager.Profiles)) { if (ed.ShowDialog() == DialogResult.OK) { _profileManager.Profiles.Add(p); _profileManager.Save(); LoadProfiles(); lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1; } } }
        private void btnEditProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { using (var ed = new ProfileEditorForm(p)) { if (ed.ShowDialog() == DialogResult.OK) { _profileManager.Save(); LoadProfiles(); } } } }
        private void btnDuplicateProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p) { _profileManager.DuplicateProfile(p); LoadProfiles(); lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1; } }
        private void btnDeleteProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && !p.IsDefault) { _profileManager.Profiles.Remove(p); _profileManager.Save(); LoadProfiles(); } }
        private void btnUpProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedIndex > 0) { _profileManager.MoveProfile(lstProfiles.SelectedIndex, -1); LoadProfiles(); } }
        private void btnDownProfile_Click(object sender, EventArgs e) { if (lstProfiles.SelectedIndex >= 0 && lstProfiles.SelectedIndex < lstProfiles.Items.Count - 1) { _profileManager.MoveProfile(lstProfiles.SelectedIndex, 1); LoadProfiles(); } }

        private void btnAddBinding_Click(object sender, EventArgs e)
        {
            if (!(lstProfiles.SelectedItem is Profile p)) return;
            using (var capture = new CaptureForm())
            {
                var res = capture.ShowDialog(this);
                if (res == DialogResult.OK && capture.CapturedEvent != null)
                {
                    var evt = capture.CapturedEvent;
                    using (var ed = new BindingEditorForm(null, _profileManager.Profiles.Select(x => x.Name).ToList()))
                    {
                        var b = ed.ResultBinding; b.DeviceIdentifier = evt.DeviceIdentifier; b.InputType = evt.Type; b.InputCode = (evt.Type == 1) ? evt.VKey : (int)evt.MouseButtonFlags;
                        if (ed.ShowDialog(this) == DialogResult.OK) { p.Bindings.Add(b); _profileManager.Save(); RefreshBindings(); }
                    }
                }
                else if (res == DialogResult.Retry) 
                {
                    using (var geForm = new RadialMenuSetupForm(null, _profileManager.Profiles.Select(x => x.Name).ToList())) { if (geForm.ShowDialog(this) == DialogResult.OK) { p.Bindings.Add(geForm.ResultBinding); _profileManager.Save(); RefreshBindings(); } }
                }
            }
        }
        
        private void btnEditBinding_Click(object sender, EventArgs e) 
        { 
            if (lstBindings.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b) 
            { 
                if (b.InputType == 4 || b.InputType == 5 || b.Action.ActionType == ActionType.RadialMenu) { using (var geForm = new RadialMenuSetupForm(b, _profileManager.Profiles.Select(x => x.Name).ToList())) { if (geForm.ShowDialog(this) == DialogResult.OK) { _profileManager.Save(); RefreshBindings(); } } }
                else { using (var ed = new BindingEditorForm(b, _profileManager.Profiles.Select(x => x.Name).ToList())) { if (ed.ShowDialog(this) == DialogResult.OK) { _profileManager.Save(); RefreshBindings(); } } }
            } 
        }

        private void btnDeleteBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedItems.Count > 0) { foreach (ListViewItem item in lstBindings.SelectedItems.Cast<ListViewItem>().ToList()) { p.Bindings.Remove((UsbInputMapper.Profiles.Binding)item.Tag); } _profileManager.Save(); RefreshBindings(); } }
        private void btnUpBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedItems.Count == 1 && lstBindings.SelectedIndex > 0) { int idx = lstBindings.SelectedIndex; _profileManager.MoveBinding(p.Bindings, idx, -1); RefreshBindings(); lstBindings.SetSelected(idx - 1, true); } }
        private void btnDownBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedItems.Count == 1 && lstBindings.SelectedIndex >= 0 && lstBindings.SelectedIndex < lstBindings.Items.Count - 1) { int idx = lstBindings.SelectedIndex; _profileManager.MoveBinding(p.Bindings, idx, 1); RefreshBindings(); lstBindings.SetSelected(idx + 1, true); } }

        private void btnResetChatter_Click(object sender, EventArgs e)
        {
            if (GlobalHookManager.Instance != null) GlobalHookManager.Instance.ResetChatterCount();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
            else { InputLogger.OnLog -= LogMessage; _monitorTimer?.Stop(); }
        }
    }
}
