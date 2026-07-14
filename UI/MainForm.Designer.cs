namespace UsbInputMapper.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstProfiles;
        private System.Windows.Forms.ListBox lstBindings;
        private System.Windows.Forms.Button btnAddProfile;
        private System.Windows.Forms.Button btnEditProfile;
        private System.Windows.Forms.Button btnDuplicateProfile;
        private System.Windows.Forms.Button btnDeleteProfile;
        private System.Windows.Forms.Button btnUpProfile;
        private System.Windows.Forms.Button btnDownProfile;
        
        private System.Windows.Forms.Button btnAddBinding;
        private System.Windows.Forms.Button btnEditBinding;
        private System.Windows.Forms.Button btnDuplicateBinding; // ★追加
        private System.Windows.Forms.Button btnDeleteBinding;
        private System.Windows.Forms.Button btnUpBinding;
        private System.Windows.Forms.Button btnDownBinding;
        
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.Label lblProfiles;
        private System.Windows.Forms.Label lblBindings;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lstProfiles = new System.Windows.Forms.ListBox();
            this.lstBindings = new System.Windows.Forms.ListBox();
            this.btnAddProfile = new System.Windows.Forms.Button();
            this.btnEditProfile = new System.Windows.Forms.Button();
            this.btnDuplicateProfile = new System.Windows.Forms.Button();
            this.btnDeleteProfile = new System.Windows.Forms.Button();
            this.btnUpProfile = new System.Windows.Forms.Button();
            this.btnDownProfile = new System.Windows.Forms.Button();
            
            this.btnAddBinding = new System.Windows.Forms.Button();
            this.btnEditBinding = new System.Windows.Forms.Button();
            this.btnDuplicateBinding = new System.Windows.Forms.Button();
            this.btnDeleteBinding = new System.Windows.Forms.Button();
            this.btnUpBinding = new System.Windows.Forms.Button();
            this.btnDownBinding = new System.Windows.Forms.Button();
            
            this.chkStartup = new System.Windows.Forms.CheckBox();
            this.lblProfiles = new System.Windows.Forms.Label();
            this.lblBindings = new System.Windows.Forms.Label();
            this.SuspendLayout();
            
            this.lstProfiles.FormattingEnabled = true; this.lstProfiles.ItemHeight = 12; this.lstProfiles.Location = new System.Drawing.Point(12, 24); this.lstProfiles.Size = new System.Drawing.Size(220, 220); this.lstProfiles.SelectedIndexChanged += new System.EventHandler(this.lstProfiles_SelectedIndexChanged);
            this.lstBindings.FormattingEnabled = true; this.lstBindings.ItemHeight = 12; this.lstBindings.Location = new System.Drawing.Point(245, 24); this.lstBindings.Size = new System.Drawing.Size(420, 220);
            
            this.btnAddProfile.Location = new System.Drawing.Point(12, 250); this.btnAddProfile.Size = new System.Drawing.Size(50, 23); this.btnAddProfile.Text = "追加"; this.btnAddProfile.Click += new System.EventHandler(this.btnAddProfile_Click);
            this.btnEditProfile.Location = new System.Drawing.Point(68, 250); this.btnEditProfile.Size = new System.Drawing.Size(50, 23); this.btnEditProfile.Text = "編集"; this.btnEditProfile.Click += new System.EventHandler(this.btnEditProfile_Click);
            this.btnDuplicateProfile.Location = new System.Drawing.Point(124, 250); this.btnDuplicateProfile.Size = new System.Drawing.Size(50, 23); this.btnDuplicateProfile.Text = "複製"; this.btnDuplicateProfile.Click += new System.EventHandler(this.btnDuplicateProfile_Click);
            this.btnDeleteProfile.Location = new System.Drawing.Point(180, 250); this.btnDeleteProfile.Size = new System.Drawing.Size(52, 23); this.btnDeleteProfile.Text = "削除"; this.btnDeleteProfile.Click += new System.EventHandler(this.btnDeleteProfile_Click);
            this.btnUpProfile.Location = new System.Drawing.Point(12, 279); this.btnUpProfile.Size = new System.Drawing.Size(106, 23); this.btnUpProfile.Text = "▲ 上へ"; this.btnUpProfile.Click += new System.EventHandler(this.btnUpProfile_Click);
            this.btnDownProfile.Location = new System.Drawing.Point(124, 279); this.btnDownProfile.Size = new System.Drawing.Size(108, 23); this.btnDownProfile.Text = "▼ 下へ"; this.btnDownProfile.Click += new System.EventHandler(this.btnDownProfile_Click);
            
            this.btnAddBinding.Location = new System.Drawing.Point(245, 250); this.btnAddBinding.Size = new System.Drawing.Size(75, 23); this.btnAddBinding.Text = "待機(追加)"; this.btnAddBinding.Click += new System.EventHandler(this.btnAddBinding_Click);
            this.btnEditBinding.Location = new System.Drawing.Point(326, 250); this.btnEditBinding.Size = new System.Drawing.Size(60, 23); this.btnEditBinding.Text = "編集"; this.btnEditBinding.Click += new System.EventHandler(this.btnEditBinding_Click);
            
            // ★追加: 複製ボタン
            this.btnDuplicateBinding.Location = new System.Drawing.Point(392, 250); this.btnDuplicateBinding.Size = new System.Drawing.Size(60, 23); this.btnDuplicateBinding.Text = "複製"; this.btnDuplicateBinding.Click += new System.EventHandler(this.btnDuplicateBinding_Click);
            
            this.btnDeleteBinding.Location = new System.Drawing.Point(458, 250); this.btnDeleteBinding.Size = new System.Drawing.Size(60, 23); this.btnDeleteBinding.Text = "削除"; this.btnDeleteBinding.Click += new System.EventHandler(this.btnDeleteBinding_Click);
            this.btnUpBinding.Location = new System.Drawing.Point(524, 250); this.btnUpBinding.Size = new System.Drawing.Size(65, 23); this.btnUpBinding.Text = "▲ 上へ"; this.btnUpBinding.Click += new System.EventHandler(this.btnUpBinding_Click);
            this.btnDownBinding.Location = new System.Drawing.Point(595, 250); this.btnDownBinding.Size = new System.Drawing.Size(70, 23); this.btnDownBinding.Text = "▼ 下へ"; this.btnDownBinding.Click += new System.EventHandler(this.btnDownBinding_Click);
            
            this.chkStartup.AutoSize = true; this.chkStartup.Location = new System.Drawing.Point(549, 283); this.chkStartup.Text = "Windows起動時に実行"; this.chkStartup.CheckedChanged += new System.EventHandler(this.chkStartup_CheckedChanged);
            this.lblProfiles.AutoSize = true; this.lblProfiles.Location = new System.Drawing.Point(12, 9); this.lblProfiles.Text = "プロファイル:";
            this.lblBindings.AutoSize = true; this.lblBindings.Location = new System.Drawing.Point(243, 9); this.lblBindings.Text = "入力一覧:";
            
            this.ClientSize = new System.Drawing.Size(680, 320);
            this.Controls.Add(this.btnDownBinding); this.Controls.Add(this.btnUpBinding); this.Controls.Add(this.btnDeleteBinding);
            this.Controls.Add(this.btnDuplicateBinding); this.Controls.Add(this.btnEditBinding); this.Controls.Add(this.btnAddBinding);
            this.Controls.Add(this.btnDownProfile); this.Controls.Add(this.btnUpProfile); this.Controls.Add(this.btnDeleteProfile);
            this.Controls.Add(this.btnDuplicateProfile); this.Controls.Add(this.btnEditProfile); this.Controls.Add(this.btnAddProfile);
            this.Controls.Add(this.lblBindings); this.Controls.Add(this.lblProfiles); this.Controls.Add(this.chkStartup);
            this.Controls.Add(this.lstBindings); this.Controls.Add(this.lstProfiles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen; // ★中央表示
            this.Text = "UsbInputMapper - 設定";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
