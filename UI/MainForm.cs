using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using UsbInputMapper.Util;
using UsbInputMapper.Core;
using Newtonsoft.Json;

namespace UsbInputMapper.UI
{
    public partial class MainForm : Form
    {
        private readonly ProfileManager _profileManager;
        private readonly DirectInputManager _diManager;
        private List<string> _profileNames => _profileManager.Profiles.Select(p => p.Name).ToList();

        public MainForm(ProfileManager profileManager, DirectInputManager diManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            _diManager = diManager;
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

        private void lstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile p)
            {
                chkEnableXInput.Checked = p.EnableXInput;
            }
            RefreshBindings();
        }

        private void chkEnableXInput_CheckedChanged(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile p)
            {
                p.EnableXInput = chkEnableXInput.Checked;
                _profileManager.Save();
            }
        }

        private void btnSetupWizard_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem is Profile p)
            {
                using (var wizard = new ControllerSetupForm(p, _diManager))
                {
                    if (wizard.ShowDialog(this) == DialogResult.OK)
                    {
                        _profileManager.Save();
                        chkEnableXInput.Checked = p.EnableXInput;
                        RefreshBindings();
                    }
                }
            }
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
        
        // 既存のプロファイルやBindingの追加・編集・削除の処理は文字数都合で省略せず維持する前提で実装
        // ... (btnAddProfile_Click, btnEditProfile_Click など既存と同じ処理)
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
        }
    }
}
