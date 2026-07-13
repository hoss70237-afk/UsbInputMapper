using System;
using System.IO;
using System.Windows.Forms;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class ProfileEditorForm : Form
    {
        public Profile TargetProfile { get; private set; }

        public ProfileEditorForm(Profile profile)
        {
            InitializeComponent();
            TargetProfile = profile;

            txtName.Text = profile.Name;
            
            if (profile.IsDefault)
            {
                txtName.Enabled = false;
                lstApps.Enabled = false;
                btnAddApp.Enabled = false;
                btnRemoveApp.Enabled = false;
                lblStatus.Text = "デフォルトプロファイルは名前やアプリの制限を変更できません。";
            }
            else
            {
                foreach (var app in profile.TargetApplicationPaths)
                {
                    lstApps.Items.Add(app);
                }
            }
        }

        private void btnAddApp_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*";
                ofd.Title = "対象のアプリケーションを選択してください";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string exeName = Path.GetFileName(ofd.FileName);
                    if (!lstApps.Items.Contains(exeName))
                    {
                        lstApps.Items.Add(exeName);
                    }
                }
            }
        }

        private void btnRemoveApp_Click(object sender, EventArgs e)
        {
            if (lstApps.SelectedIndex >= 0)
            {
                lstApps.Items.RemoveAt(lstApps.SelectedIndex);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!TargetProfile.IsDefault)
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("プロファイル名を入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                TargetProfile.Name = txtName.Text;
                TargetProfile.TargetApplicationPaths.Clear();
                foreach (string app in lstApps.Items)
                {
                    TargetProfile.TargetApplicationPaths.Add(app);
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
