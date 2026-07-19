namespace UsbInputMapper.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        
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
        private System.Windows.Forms.Button btnDuplicateBinding; 
        private System.Windows.Forms.Button btnDeleteBinding;
        private System.Windows.Forms.Button btnUpBinding;
        private System.Windows.Forms.Button btnDownBinding;
        private System.Windows.Forms.Label lblProfiles;
        private System.Windows.Forms.Label lblBindings;
        private System.Windows.Forms.CheckBox chkEnableXInput;
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.Button btnControllerBase;

        // ★ログ用
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.CheckBox chkLog;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();

            this.lstProfiles = new System.Windows.Forms.ListBox(); this.lstBindings = new System.Windows.Forms.ListBox();
            this.btnAddProfile = new System.Windows.Forms.Button(); this.btnEditProfile = new System.Windows.Forms.Button();
            this.btnDuplicateProfile = new System.Windows.Forms.Button(); this.btnDeleteProfile = new System.Windows.Forms.Button();
            this.btnUpProfile = new System.Windows.Forms.Button(); this.btnDownProfile = new System.Windows.Forms.Button();
            this.btnAddBinding = new System.Windows.Forms.Button(); this.btnEditBinding = new System.Windows.Forms.Button();
            this.btnDuplicateBinding = new System.Windows.Forms.Button(); this.btnDeleteBinding = new System.Windows.Forms.Button();
            this.btnUpBinding = new System.Windows.Forms.Button(); this.btnDownBinding = new System.Windows.Forms.Button();
            this.lblProfiles = new System.Windows.Forms.Label(); this.lblBindings = new System.Windows.Forms.Label();
            this.chkEnableXInput = new System.Windows.Forms.CheckBox(); this.chkStartup = new System.Windows.Forms.CheckBox(); this.btnControllerBase = new System.Windows.Forms.Button();

            this.txtLog = new System.Windows.Forms.TextBox();
            this.chkLog = new System.Windows.Forms.CheckBox();

            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            
            // tabControl1
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Size = new System.Drawing.Size(680, 360);
            
            // tabPage1 (設定)
            this.tabPage1.Text = "設定";
            this.tabPage1.Controls.Add(this.chkStartup);
            this.tabPage1.Controls.Add(this.btnControllerBase); this.tabPage1.Controls.Add(this.chkEnableXInput);
            this.tabPage1.Controls.Add(this.btnDownBinding); this.tabPage1.Controls.Add(this.btnUpBinding); this.tabPage1.Controls.Add(this.btnDeleteBinding); this.tabPage1.Controls.Add(this.btnEditBinding); this.tabPage1.Controls.Add(this.btnAddBinding);
            this.tabPage1.Controls.Add(this.btnDownProfile); this.tabPage1.Controls.Add(this.btnUpProfile); this.tabPage1.Controls.Add(this.btnDeleteProfile); this.tabPage1.Controls.Add(this.btnDuplicateProfile); this.tabPage1.Controls.Add(this.btnEditProfile); this.tabPage1.Controls.Add(this.btnAddProfile);
            this.tabPage1.Controls.Add(this.lblBindings); this.tabPage1.Controls.Add(this.lblProfiles); 
            this.tabPage1.Controls.Add(this.lstBindings); this.tabPage1.Controls.Add(this.lstProfiles);

            this.lstProfiles.FormattingEnabled = true; this.lstProfiles.ItemHeight = 12; this.lstProfiles.Location = new System.Drawing.Point(6, 24); this.lstProfiles.Size = new System.Drawing.Size(220, 220); this.lstProfiles.SelectedIndexChanged += new System.EventHandler(this.lstProfiles_SelectedIndexChanged);
            this.lstBindings.FormattingEnabled = true; this.lstBindings.ItemHeight = 12; this.lstBindings.Location = new System.Drawing.Point(239, 48); this.lstBindings.Size = new System.Drawing.Size(420, 196); 
            
            this.btnAddProfile.Location = new System.Drawing.Point(6, 250); this.btnAddProfile.Size = new System.Drawing.Size(50, 23); this.btnAddProfile.Text = "追加"; this.btnAddProfile.Click += new System.EventHandler(this.btnAddProfile_Click);
            this.btnEditProfile.Location = new System.Drawing.Point(62, 250); this.btnEditProfile.Size = new System.Drawing.Size(50, 23); this.btnEditProfile.Text = "編集"; this.btnEditProfile.Click += new System.EventHandler(this.btnEditProfile_Click);
            this.btnDuplicateProfile.Location = new System.Drawing.Point(118, 250); this.btnDuplicateProfile.Size = new System.Drawing.Size(50, 23); this.btnDuplicateProfile.Text = "複製"; this.btnDuplicateProfile.Click += new System.EventHandler(this.btnDuplicateProfile_Click);
            this.btnDeleteProfile.Location = new System.Drawing.Point(174, 250); this.btnDeleteProfile.Size = new System.Drawing.Size(52, 23); this.btnDeleteProfile.Text = "削除"; this.btnDeleteProfile.Click += new System.EventHandler(this.btnDeleteProfile_Click);
            this.btnUpProfile.Location = new System.Drawing.Point(6, 279); this.btnUpProfile.Size = new System.Drawing.Size(106, 23); this.btnUpProfile.Text = "▲ 上へ"; this.btnUpProfile.Click += new System.EventHandler(this.btnUpProfile_Click);
            this.btnDownProfile.Location = new System.Drawing.Point(118, 279); this.btnDownProfile.Size = new System.Drawing.Size(108, 23); this.btnDownProfile.Text = "▼ 下へ"; this.btnDownProfile.Click += new System.EventHandler(this.btnDownProfile_Click);
            
            this.btnAddBinding.Location = new System.Drawing.Point(239, 250); this.btnAddBinding.Size = new System.Drawing.Size(110, 23); this.btnAddBinding.Text = "入力上書き(追加)"; this.btnAddBinding.Click += new System.EventHandler(this.btnAddBinding_Click);
            this.btnEditBinding.Location = new System.Drawing.Point(355, 250); this.btnEditBinding.Size = new System.Drawing.Size(60, 23); this.btnEditBinding.Text = "編集"; this.btnEditBinding.Click += new System.EventHandler(this.btnEditBinding_Click);
            this.btnDeleteBinding.Location = new System.Drawing.Point(487, 250); this.btnDeleteBinding.Size = new System.Drawing.Size(60, 23); this.btnDeleteBinding.Text = "削除"; this.btnDeleteBinding.Click += new System.EventHandler(this.btnDeleteBinding_Click);
            this.btnUpBinding.Location = new System.Drawing.Point(553, 250); this.btnUpBinding.Size = new System.Drawing.Size(50, 23); this.btnUpBinding.Text = "▲"; this.btnUpBinding.Click += new System.EventHandler(this.btnUpBinding_Click);
            this.btnDownBinding.Location = new System.Drawing.Point(609, 250); this.btnDownBinding.Size = new System.Drawing.Size(50, 23); this.btnDownBinding.Text = "▼"; this.btnDownBinding.Click += new System.EventHandler(this.btnDownBinding_Click);
            
            this.lblProfiles.AutoSize = true; this.lblProfiles.Location = new System.Drawing.Point(6, 9); this.lblProfiles.Text = "プロファイル:";
            this.lblBindings.AutoSize = true; this.lblBindings.Location = new System.Drawing.Point(237, 9); this.lblBindings.Text = "入力上書き設定 (プロファイル専用):";

            this.chkEnableXInput.AutoSize = true; this.chkEnableXInput.Location = new System.Drawing.Point(239, 26); this.chkEnableXInput.Text = "このプロファイルでベース出力有効化"; this.chkEnableXInput.CheckedChanged += new System.EventHandler(this.chkEnableXInput_CheckedChanged);
            
            this.chkStartup.AutoSize = true; this.chkStartup.Location = new System.Drawing.Point(6, 305); this.chkStartup.Text = "PC起動時にタスクトレイに起動"; this.chkStartup.CheckedChanged += new System.EventHandler(this.chkStartup_CheckedChanged);
            
            this.btnControllerBase.Location = new System.Drawing.Point(487, 22); this.btnControllerBase.Size = new System.Drawing.Size(172, 23); this.btnControllerBase.Text = "コントローラーベース設定..."; this.btnControllerBase.Click += new System.EventHandler(this.btnControllerBase_Click);

            // tabPage2 (入力テスト)
            this.tabPage2.Text = "入力テスト";
            this.tabPage2.Controls.Add(this.chkLog);
            this.tabPage2.Controls.Add(this.txtLog);
            
            this.chkLog.AutoSize = true; this.chkLog.Location = new System.Drawing.Point(6, 12); this.chkLog.Text = "入力テスト(ログ取得)を有効にする"; this.chkLog.CheckedChanged += new System.EventHandler(this.chkLog_CheckedChanged);
            
            this.txtLog.Location = new System.Drawing.Point(6, 35); this.txtLog.Multiline = true; this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; this.txtLog.ReadOnly = true; this.txtLog.Size = new System.Drawing.Size(660, 285);

            this.ClientSize = new System.Drawing.Size(680, 360);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle; this.MaximizeBox = false; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen; this.Text = "UsbInputMapper - 設定";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false); this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false); this.tabPage2.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
