using System;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using UsbInputMapper.Util;

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

        private void CheckStartupStatus()
        {
            chkStartup.Checked = StartupRegistrar.IsRegistered();
        }

        private void LoadProfiles()
        {
            lstProfiles.Items.Clear();
            foreach (var profile in _profileManager.Profiles)
            {
                lstProfiles.Items.Add(profile);
            }
            if (lstProfiles.Items.Count > 0)
            {
                lstProfiles.SelectedIndex = 0;
            }
        }

        private void lstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshBindings();
        }

        private void RefreshBindings()
        {
            lstBindings.Items.Clear();
            if (lstProfiles.SelectedItem is Profile profile)
            {
                foreach (var b in profile.Bindings)
                {
                    string info = $"[{b.Name}] {b.Condition} => {b.Action.ActionType}";
                    lstBindings.Items.Add(new ListViewItem(info) { Tag = b });
                }
            }
        }

        // --- プロファイル操作 ---

        private void btnAddProfile_Click(object sender, EventArgs e)
        {
            var p = new Profile { Name = "新規プロファイル" };
            using (var editor = new ProfileEditorForm(p))
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    _profileManager.Profiles.Add(p);
                    _profileManager.Save();
                    LoadProfiles();
                }
            }
        }

        private void btnEditProfile_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile currentProfile)
            {
                using (var editor = new ProfileEditorForm(currentProfile))
                {
                    if (editor.ShowDialog() == DialogResult.OK)
                    {
                        _profileManager.Save();
                        // リストの表示を更新
                        int idx = lstProfiles.SelectedIndex;
                        lstProfiles.Items[idx] = lstProfiles.Items[idx];
                    }
                }
            }
        }

        // --- アイテム(Binding)操作 ---

        private void btnAddBinding_Click(object sender, EventArgs e)
        {
            if (!(lstProfiles.SelectedItem is Profile currentProfile)) return;

            using (var capture = new CaptureForm())
            {
                if (capture.ShowDialog() == DialogResult.OK && capture.CapturedEvent != null)
                {
                    var evt = capture.CapturedEvent;
                    int inputCode = 0;
                    if (evt.Type == 1) inputCode = evt.VKey;
                    else if (evt.Type == 0) inputCode = (int)evt.MouseButtonFlags;

                    using (var editor = new BindingEditorForm())
                    {
                        if (editor.ShowDialog() == DialogResult.OK)
                        {
                            var b = editor.ResultBinding;
                            b.DeviceIdentifier = evt.DeviceIdentifier;
                            b.InputType = evt.Type;
                            b.InputCode = inputCode;
                            
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
                    if (editor.ShowDialog() == DialogResult.OK)
                    {
                        _profileManager.Save();
                        RefreshBindings();
                    }
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

        private void chkStartup_CheckedChanged(object sender, EventArgs e)
        {
            StartupRegistrar.Register(chkStartup.Checked);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
}
