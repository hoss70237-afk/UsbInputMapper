using System;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public partial class ControllerBaseForm : Form
    {
        private ProfileManager _profileManager;
        private DirectInputManager _diManager;

        public ControllerBaseForm(ProfileManager profileManager, DirectInputManager diManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            _diManager = diManager;
            RefreshList();
        }

        private void RefreshList()
        {
            lstBase.Items.Clear();
            foreach (var b in _profileManager.ControllerBaseBindings)
            {
                lstBase.Items.Add(new ListViewItem($"[{b.Name}] {b.GetTriggerString()} => {b.Action.ToString()}") { Tag = b });
            }
        }

        private void btnWizard_Click(object sender, EventArgs e)
        {
            using (var w = new ControllerSetupForm(_profileManager, _diManager))
            {
                if (w.ShowDialog(this) == DialogResult.OK) { _profileManager.Save(); RefreshList(); }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using (var ed = new BindingEditorForm())
            {
                if (ed.ShowDialog(this) == DialogResult.OK) { _profileManager.ControllerBaseBindings.Add(ed.ResultBinding); _profileManager.Save(); RefreshList(); }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (lstBase.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b)
            {
                using (var ed = new BindingEditorForm(b))
                {
                    if (ed.ShowDialog(this) == DialogResult.OK) { _profileManager.Save(); RefreshList(); }
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lstBase.SelectedItem is ListViewItem item && item.Tag is UsbInputMapper.Profiles.Binding b)
            {
                _profileManager.ControllerBaseBindings.Remove(b); _profileManager.Save(); RefreshList();
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (lstBase.SelectedIndex > 0) { _profileManager.MoveBinding(_profileManager.ControllerBaseBindings, lstBase.SelectedIndex, -1); RefreshList(); }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if (lstBase.SelectedIndex >= 0 && lstBase.SelectedIndex < lstBase.Items.Count - 1) { _profileManager.MoveBinding(_profileManager.ControllerBaseBindings, lstBase.SelectedIndex, 1); RefreshList(); }
        }
    }
}
