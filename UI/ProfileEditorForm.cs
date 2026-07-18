using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using Newtonsoft.Json;

namespace UsbInputMapper.UI
{
    public partial class ProfileEditorForm : Form
    {
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(Point p);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public Profile TargetProfile { get; private set; }
        private bool _isDraggingTarget = false;
        private List<Profile> _existingProfiles;

        // ★追加: コピー元のプロファイル選択用
        private ComboBox _cmbCopyFrom;
        private Label _lblCopyFrom;

        public ProfileEditorForm(Profile profile, List<Profile> existingProfiles = null)
        {
            InitializeComponent();
            TargetProfile = profile;
            _existingProfiles = existingProfiles;

            txtName.Text = profile.Name;
            
            if (profile.IsDefault)
            {
                txtName.Enabled = false;
                lstApps.Enabled = false;
                btnAddApp.Enabled = false;
                btnRemoveApp.Enabled = false;
                lblTargetPicker.Enabled = false;
                lblStatus.Text = "デフォルトプロファイルは名前やアプリの制限を変更できません。";
            }
            else
            {
                foreach (var app in profile.TargetApplicationPaths) lstApps.Items.Add(app);
            }

            // 新規作成モードの場合、コピー元のドロップダウンを表示
            if (_existingProfiles != null && _existingProfiles.Count > 0)
            {
                _lblCopyFrom = new Label { Text = "コピー元:", Location = new Point(12, 182), AutoSize = true };
                _cmbCopyFrom = new ComboBox { Location = new Point(87, 179), Size = new Size(265, 20), DropDownStyle = ComboBoxStyle.DropDownList };
                _cmbCopyFrom.Items.Add("(空の状態で作成)");
                foreach (var p in _existingProfiles) _cmbCopyFrom.Items.Add(p.Name);
                _cmbCopyFrom.SelectedIndex = 0;

                this.Controls.Add(_lblCopyFrom);
                this.Controls.Add(_cmbCopyFrom);
                
                // フォームの高さを少し広げてボタンを下げる
                this.ClientSize = new Size(this.ClientSize.Width, 240);
                btnOK.Top = 205;
                btnCancel.Top = 205;
                lblStatus.Top = 210;
            }

            SetupTargetPicker();
        }

        private void SetupTargetPicker()
        {
            lblTargetPicker.MouseDown += (s, e) => { _isDraggingTarget = true; lblTargetPicker.Capture = true; Cursor.Current = Cursors.Cross; };
            lblTargetPicker.MouseMove += (s, e) => { if (_isDraggingTarget) Cursor.Current = Cursors.Cross; };
            lblTargetPicker.MouseUp += (s, e) => {
                if (_isDraggingTarget)
                {
                    _isDraggingTarget = false; lblTargetPicker.Capture = false; Cursor.Current = Cursors.Default;
                    Point pt = Cursor.Position; IntPtr hwnd = WindowFromPoint(pt);
                    if (hwnd != IntPtr.Zero)
                    {
                        GetWindowThreadProcessId(hwnd, out uint pid);
                        if (pid > 0)
                        {
                            string path = GetExecutablePathProcessId(pid);
                            if (!string.IsNullOrEmpty(path))
                            {
                                string exeName = Path.GetFileName(path);
                                if (!lstApps.Items.Contains(exeName)) { lstApps.Items.Add(exeName); lblStatus.Text = $"追加しました: {exeName}"; lblStatus.ForeColor = Color.Green; }
                            }
                            else { lblStatus.Text = "プロセスの取得に失敗しました。"; lblStatus.ForeColor = Color.Red; }
                        }
                    }
                }
            };
        }

        private string GetExecutablePathProcessId(uint pid)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProcess == IntPtr.Zero) return null;
            try { StringBuilder sb = new StringBuilder(1024); uint size = (uint)sb.Capacity; if (QueryFullProcessImageName(hProcess, 0, sb, ref size)) return sb.ToString(); }
            finally { CloseHandle(hProcess); }
            return null;
        }

        private void btnAddApp_Click(object sender, EventArgs e) { using (OpenFileDialog ofd = new OpenFileDialog { Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*" }) { if (ofd.ShowDialog() == DialogResult.OK) { string exeName = Path.GetFileName(ofd.FileName); if (!lstApps.Items.Contains(exeName)) lstApps.Items.Add(exeName); } } }
        private void btnRemoveApp_Click(object sender, EventArgs e) { if (lstApps.SelectedIndex >= 0) lstApps.Items.RemoveAt(lstApps.SelectedIndex); }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!TargetProfile.IsDefault)
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("プロファイル名を入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                TargetProfile.Name = txtName.Text;
                TargetProfile.TargetApplicationPaths.Clear();
                foreach (string app in lstApps.Items) TargetProfile.TargetApplicationPaths.Add(app);

                // ★追加: コピー元の設定が選ばれている場合はコピー
                if (_cmbCopyFrom != null && _cmbCopyFrom.SelectedIndex > 0)
                {
                    string selectedName = _cmbCopyFrom.SelectedItem.ToString();
                    var src = _existingProfiles.Find(p => p.Name == selectedName);
                    if (src != null)
                    {
                        // JSONを利用してディープコピー
                        string json = JsonConvert.SerializeObject(src.Bindings);
                        TargetProfile.Bindings = JsonConvert.DeserializeObject<List<UsbInputMapper.Profiles.Binding>>(json);
                        TargetProfile.EnableXInput = src.EnableXInput;
                    }
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}
