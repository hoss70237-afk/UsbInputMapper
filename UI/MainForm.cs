using System;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public partial class MainForm : Form
    {
        private readonly ProfileManager _profileManager;
        private readonly DirectInputManager _diManager;

        public MainForm(ProfileManager profileManager, DirectInputManager diManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            _diManager = diManager;
            LoadProfiles();
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

        // ★ウィザードボタンに代わり、ベース画面を開く
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
                if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedEvent != null)
                {
                    var evt = capture.CapturedEvent;
                    using (var ed = new BindingEditorForm())
                    {
                        var b = ed.ResultBinding; b.DeviceIdentifier = evt.DeviceIdentifier; b.InputType = evt.Type; b.InputCode = (evt.Type == 1) ? evt.VKey : (int)evt.MouseButtonFlags;
                        if (ed.ShowDialog(this) == DialogResult.OK) { p.Bindings.Add(b); _profileManager.Save(); RefreshBindings(); }
                    }
                }
            }
        }
        
        private void btnEditBinding_Click(object sender, EventArgs e) { if (lstBindings.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b) { using (var ed = new BindingEditorForm(b)) { if (ed.ShowDialog(this) == DialogResult.OK) { _profileManager.Save(); RefreshBindings(); } } } }
        private void btnDuplicateBinding_Click(object sender, EventArgs e) { /* 省略 */ }
        private void btnDeleteBinding_Click(object sender, EventArgs e) { if (lstBindings.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b && lstProfiles.SelectedItem is Profile p) { p.Bindings.Remove(b); _profileManager.Save(); RefreshBindings(); } }
        private void btnUpBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedIndex > 0) { _profileManager.MoveBinding(p.Bindings, lstBindings.SelectedIndex, -1); RefreshBindings(); } }
        private void btnDownBinding_Click(object sender, EventArgs e) { if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedIndex >= 0 && lstBindings.SelectedIndex < lstBindings.Items.Count - 1) { _profileManager.MoveBinding(p.Bindings, lstBindings.SelectedIndex, 1); RefreshBindings(); } }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); } }
    }
}
