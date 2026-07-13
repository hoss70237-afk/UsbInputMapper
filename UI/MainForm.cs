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
                    string info = $"Dev: {b.DeviceIdentifier} | Type: {b.InputType} | Code: {b.InputCode} => {b.Action}";
                    lstBindings.Items.Add(new ListViewItem(info) { Tag = b });
                }
            }
        }

        private void btnAddProfile_Click(object sender, EventArgs e)
        {
            var p = new Profile { Name = "New Profile" };
            _profileManager.Profiles.Add(p);
            _profileManager.Save();
            LoadProfiles();
        }

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
                            // 修正箇所1: どちらの Binding かをフルネームで明確に指定
                            var b = new UsbInputMapper.Profiles.Binding
                            {
                                DeviceIdentifier = evt.DeviceIdentifier,
                                InputType = evt.Type,
                                InputCode = inputCode,
                                Action = editor.ResultAction
                            };
                            currentProfile.Bindings.Add(b);
                            _profileManager.Save();
                            RefreshBindings();
                        }
                    }
                }
            }
        }

        private void btnDeleteBinding_Click(object sender, EventArgs e)
        {
            // 修正箇所2: こちらもフルネームで指定
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
