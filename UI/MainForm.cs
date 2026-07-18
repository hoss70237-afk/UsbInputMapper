using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using UsbInputMapper.Profiles;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public partial class MainForm : Form
    {
        private readonly ProfileManager _profileManager;
        private readonly DirectInputManager _diManager;
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public MainForm(ProfileManager profileManager, DirectInputManager diManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            _diManager = diManager;
            LoadProfiles();

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                chkStartup.Checked = (key.GetValue("UsbInputMapper") != null);
            }
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
            if (lstProfiles.SelectedItem is Profile p) chkEnableXInput.Checked = p.EnableXInput;
            RefreshBindings();
        }

        private void chkEnableXInput_CheckedChanged(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile p) { p.EnableXInput = chkEnableXInput.Checked; _profileManager.Save(); }
        }

        private void chkStartup_CheckedChanged(object sender, EventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (chkStartup.Checked) key.SetValue("UsbInputMapper", Application.ExecutablePath);
                else key.DeleteValue("UsbInputMapper", false);
            }
        }

        private void btnControllerBase_Click(object sender, EventArgs e)
        {
            using (var f = new ControllerBaseForm(_profileManager, _diManager)) { f.ShowDialog(this); }
        }

        private void RefreshBindings()
        {
            lstBindings.Items.Clear();
            if (lstProfiles.SelectedItem is Profile profile)
            {
                foreach (var b in profile.Bindings)
                {
                    string info = $"[{b.Name}] {b.GetTriggerString()} => {b.Action.ToString()}";
                    lstBindings.Items.Add(new ListViewItem(info) { Tag = b });
                }
            }
        }

        private void btnAddProfile_Click(object sender, EventArgs e) { var p = new Profile { Name = "新規プロファイル" }; using (var ed = new ProfileEditorForm(p)) { if (ed.ShowDialog() == DialogResult.OK) { _profileManager.Profiles.Add(p); _profileManager.Save(); LoadProfiles(); lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1; } } }
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
                else if (res == DialogResult.Retry) // ★ジェスチャー・ベゼル設定
                {
                    using (var geForm = new GestureEdgeSetupForm(null, _profileManager.Profiles.Select(x => x.Name).ToList()))
                    {
                        if (geForm.ShowDialog(this) == DialogResult.OK) { p.Bindings.Add(geForm.ResultBinding); _profileManager.Save(); RefreshBindings(); }
                    }
                }
            }
        }
        
        private void btnEditBinding_Click(object sender, EventArgs e) 
        { 
            if (lstBindings.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b) 
            { 
                if (b.InputType == 4 || b.InputType == 5 || b.Action.ActionType == ActionType.Gesture) // ジェスチャー/ベゼル
                {
                    using (var geForm = new GestureEdgeSetupForm(b, _profileManager.Profiles.Select(x => x.Name).ToList()))
                    {
                        if (geForm.ShowDialog(this) == DialogResult.OK) { _profileManager.Save(); RefreshBindings(); }
                    }
                }
                else
                {
                    using (var ed = new BindingEditorForm(b, _profileManager.Profiles.Select(x => x.Name).ToList())) 
                    { 
                        if (ed.ShowDialog(this) == DialogResult.OK) { _profileManager.Save(); RefreshBindings(); } 
                    } 
                }
            } 
        }
        private void btnDeleteBinding_Click(object sender, EventArgs e) { if (lstBindings.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b && lstProfiles.SelectedItem is Profile p) { p.Bindings.Remove(b); _profileManager.Save(); RefreshBindings(); } }
        private void btnUpBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedIndex > 0) { _profileManager.MoveBinding(p.Bindings, lstBindings.SelectedIndex, -1); RefreshBindings(); } }
        private void btnDownBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedIndex >= 0 && lstBindings.SelectedIndex < lstBindings.Items.Count - 1) { _profileManager.MoveBinding(p.Bindings, lstBindings.SelectedIndex, 1); RefreshBindings(); } }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); } }
    }
}
