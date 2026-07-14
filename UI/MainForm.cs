using System;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using UsbInputMapper.Util;
using Newtonsoft.Json;

namespace UsbInputMapper.UI
{
    public partial class MainForm : Form
    {
        private readonly ProfileManager _profileManager;

        public MainForm(ProfileManager profileManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            LoadProfiles();
            CheckStartupStatus();
        }

        private void CheckStartupStatus() { chkStartup.Checked = StartupRegistrar.IsRegistered(); }

        private void LoadProfiles()
        {
            int selected = lstProfiles.SelectedIndex;
            lstProfiles.Items.Clear();
            foreach (var profile in _profileManager.Profiles) lstProfiles.Items.Add(profile);
            if (lstProfiles.Items.Count > 0) lstProfiles.SelectedIndex = (selected >= 0 && selected < lstProfiles.Items.Count) ? selected : 0;
        }

        private void lstProfiles_SelectedIndexChanged(object sender, EventArgs e) { RefreshBindings(); }

        private void RefreshBindings()
        {
            lstBindings.Items.Clear();
            if (lstProfiles.SelectedItem is Profile profile)
            {
                foreach (var b in profile.Bindings)
                {
                    string conditionStr = b.Condition == TriggerCondition.Normal ? "" : $" ({b.Condition})";
                    string info = $"[{b.Name}] {b.GetTriggerString()}{conditionStr} => {b.Action.ToString()}";
                    lstBindings.Items.Add(new ListViewItem(info) { Tag = b });
                }
            }
        }

        // --- プロファイル操作 ---
        private void btnAddProfile_Click(object sender, EventArgs e) { /* 既存のまま（省略防止のため実装） */
            var p = new Profile { Name = "新規プロファイル" };
            using (var editor = new ProfileEditorForm(p)) {
                if (editor.ShowDialog() == DialogResult.OK) { _profileManager.Profiles.Add(p); _profileManager.Save(); LoadProfiles(); lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1; }
            }
        }
        private void btnEditProfile_Click(object sender, EventArgs e) {
            if (lstProfiles.SelectedItem is Profile currentProfile) {
                using (var editor = new ProfileEditorForm(currentProfile)) {
                    if (editor.ShowDialog() == DialogResult.OK) { _profileManager.Save(); LoadProfiles(); }
                }
            }
        }
        private void btnDuplicateProfile_Click(object sender, EventArgs e) {
            if (lstProfiles.SelectedItem is Profile currentProfile) { _profileManager.DuplicateProfile(currentProfile); LoadProfiles(); lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1; }
        }
        private void btnDeleteProfile_Click(object sender, EventArgs e) {
            if (lstProfiles.SelectedItem is Profile p && !p.IsDefault) {
                if (MessageBox.Show("本当に削除しますか？", "確認", MessageBoxButtons.YesNo) == DialogResult.Yes) { _profileManager.Profiles.Remove(p); _profileManager.Save(); LoadProfiles(); }
            }
        }
        private void btnUpProfile_Click(object sender, EventArgs e) {
            int idx = lstProfiles.SelectedIndex;
            if (idx > 0) { _profileManager.MoveProfile(idx, -1); LoadProfiles(); lstProfiles.SelectedIndex = idx - 1; }
        }
        private void btnDownProfile_Click(object sender, EventArgs e) {
            int idx = lstProfiles.SelectedIndex;
            if (idx >= 0 && idx < lstProfiles.Items.Count - 1) { _profileManager.MoveProfile(idx, 1); LoadProfiles(); lstProfiles.SelectedIndex = idx + 1; }
        }

        // --- アイテム(Binding)操作 ---
        private void btnAddBinding_Click(object sender, EventArgs e)
        {
            if (!(lstProfiles.SelectedItem is Profile currentProfile)) return;
            using (var capture = new CaptureForm())
            {
                if (capture.ShowDialog(this) == DialogResult.OK && capture.CapturedEvent != null)
                {
                    var evt = capture.CapturedEvent;
                    int inputCode = (evt.Type == 1) ? evt.VKey : (int)evt.MouseButtonFlags; 
                    using (var editor = new BindingEditorForm())
                    {
                        var b = editor.ResultBinding;
                        b.DeviceIdentifier = evt.DeviceIdentifier;
                        b.InputType = evt.Type;
                        b.InputCode = inputCode;
                        
                        if (editor.ShowDialog(this) == DialogResult.OK)
                        {
                            currentProfile.Bindings.Add(b);
                            _profileManager.Save();
                            RefreshBindings();
                        }
                    }
                }
            }
        }

        private void btnEditBinding_Click(object sender, EventArgs e)
        {
            if (lstBindings.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b)
            {
                using (var editor = new BindingEditorForm(b))
                {
                    if (editor.ShowDialog(this) == DialogResult.OK)
                    {
                        _profileManager.Save();
                        RefreshBindings();
                    }
                }
            }
        }

        // ★追加: アイテム複製機能
        private void btnDuplicateBinding_Click(object sender, EventArgs e)
        {
            if (lstBindings.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b)
            {
                if (lstProfiles.SelectedItem is Profile p)
                {
                    var json = JsonConvert.SerializeObject(b);
                    var clone = JsonConvert.DeserializeObject<UsbInputMapper.Profiles.Binding>(json);
                    clone.Name += " のコピー";
                    p.Bindings.Add(clone);
                    _profileManager.Save();
                    RefreshBindings();
                }
            }
        }

        private void btnDeleteBinding_Click(object sender, EventArgs e)
        {
            if (lstBindings.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b)
            {
                if (lstProfiles.SelectedItem is Profile p)
                {
                    p.Bindings.Remove(b);
                    _profileManager.Save();
                    RefreshBindings();
                }
            }
        }

        private void btnUpBinding_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedIndex > 0)
            {
                int idx = lstBindings.SelectedIndex;
                _profileManager.MoveBinding(p, idx, -1);
                RefreshBindings();
                lstBindings.SelectedIndex = idx - 1;
            }
        }

        private void btnDownBinding_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile p && lstBindings.SelectedIndex >= 0 && lstBindings.SelectedIndex < lstBindings.Items.Count - 1)
            {
                int idx = lstBindings.SelectedIndex;
                _profileManager.MoveBinding(p, idx, 1);
                RefreshBindings();
                lstBindings.SelectedIndex = idx + 1;
            }
        }

        private void chkStartup_CheckedChanged(object sender, EventArgs e) { StartupRegistrar.Register(chkStartup.Checked); }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
        }
    }
}
