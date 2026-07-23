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
        // ★追加
        private System.Windows.Forms.CheckBox chkChattering;
        private System.Windows.Forms.NumericUpDown numChatterMs;
        private System.Windows.Forms.CheckBox chkOverlayMark;
        private System.Windows.Forms.CheckBox chkOverlayName;
        
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.Button btnControllerBase;

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.CheckBox chkLog;
        private System.Windows.Forms.Label lblChatterCount; // ★追加
        private System.Windows.Forms.Button btnResetChatter; // ★追加

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
            
            this.chkEnableXInput = new System.Windows.Forms.CheckBox(); 
            this.chkChattering = new System.Windows.Forms.CheckBox();
            this.numChatterMs = new System.Windows.Forms.NumericUpDown();
            this.chkOverlayMark = new System.Windows.Forms.CheckBox();
            this.chkOverlayName = new System.Windows.Forms.CheckBox();
            
            this.chkStartup = new System.Windows.Forms.CheckBox(); this.btnControllerBase = new System.Windows.Forms.Button();

            this.txtLog = new System.Windows.Forms.TextBox();
            this.chkLog = new System.Windows.Forms.CheckBox();
            this.lblChatterCount = new System.Windows.Forms.Label();
            this.btnResetChatter = new System.Windows.Forms.Button();

            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numChatterMs)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Size = new System.Drawing.Size(680, 420);
            
            this.tabPage1.Text = "設定";
            this.tabPage1.Controls.Add(this.chkStartup);
            this.tabPage1.Controls.Add(this.btnControllerBase); 
            
            this.tabPage1.Controls.Add(this.chkEnableXInput);
            this.tabPage1.Controls.Add(this.chkChattering); this.tabPage1.Controls.Add(this.numChatterMs);
            this.tabPage1.Controls.Add(this.chkOverlayMark); this.tabPage1.Controls.Add(this.chkOverlayName);
            
            this.tabPage1.Controls.Add(this.btnDownBinding); this.tabPage1.Controls.Add(this.btnUpBinding); this.tabPage1.Controls.Add(this.btnDeleteBinding); this.tabPage1.Controls.Add(this.btnEditBinding); this.tabPage1.Controls.Add(this.btnAddBinding);
            this.tabPage1.Controls.Add(this.btnDownProfile); this.tabPage1.Controls.Add(this.btnUpProfile); this.tabPage1.Controls.Add(this.btnDeleteProfile); this.tabPage1.Controls.Add(this.btnDuplicateProfile); this.tabPage1.Controls.Add(this.btnEditProfile); this.tabPage1.Controls.Add(this.btnAddProfile);
            this.tabPage1.Controls.Add(this.lblBindings); this.tabPage1.Controls.Add(this.lblProfiles); 
            this.tabPage1.Controls.Add(this.lstBindings); this.tabPage1.Controls.Add(this.lstProfiles);

            this.lstProfiles.FormattingEnabled = true; this.lstProfiles.ItemHeight = 12; this.lstProfiles.Location = new System.Drawing.Point(6, 24); this.lstProfiles.Size = new System.Drawing.Size(220, 268); this.lstProfiles.SelectedIndexChanged += new System.EventHandler(this.lstProfiles_SelectedIndexChanged);
            this.lstBindings.FormattingEnabled = true; this.lstBindings.ItemHeight = 12; this.lstBindings.Location = new System.Drawing.Point(239, 90); this.lstBindings.Size = new System.Drawing.Size(420, 208); 
            
            this.btnAddProfile.Location = new System.Drawing.Point(6, 300); this.btnAddProfile.Size = new System.Drawing.Size(50, 23); this.btnAddProfile.Text = "追加"; this.btnAddProfile.Click += new System.EventHandler(this.btnAddProfile_Click);
            this.btnEditProfile.Location = new System.Drawing.Point(62, 300); this.btnEditProfile.Size = new System.Drawing.Size(50, 23); this.btnEditProfile.Text = "編集"; this.btnEditProfile.Click += new System.EventHandler(this.btnEditProfile_Click);
            this.btnDuplicateProfile.Location = new System.Drawing.Point(118, 300); this.btnDuplicateProfile.Size = new System.Drawing.Size(50, 23); this.btnDuplicateProfile.Text = "複製"; this.btnDuplicateProfile.Click += new System.EventHandler(this.btnDuplicateProfile_Click);
            this.btnDeleteProfile.Location = new System.Drawing.Point(174, 300); this.btnDeleteProfile.Size = new System.Drawing.Size(52, 23); this.btnDeleteProfile.Text = "削除"; this.btnDeleteProfile.Click += new System.EventHandler(this.btnDeleteProfile_Click);
            this.btnUpProfile.Location = new System.Drawing.Point(6, 329); this.btnUpProfile.Size = new System.Drawing.Size(106, 23); this.btnUpProfile.Text = "▲ 上へ"; this.btnUpProfile.Click += new System.EventHandler(this.btnUpProfile_Click);
            this.btnDownProfile.Location = new System.Drawing.Point(118, 329); this.btnDownProfile.Size = new System.Drawing.Size(108, 23); this.btnDownProfile.Text = "▼ 下へ"; this.btnDownProfile.Click += new System.EventHandler(this.btnDownProfile_Click);
            
            this.btnAddBinding.Location = new System.Drawing.Point(239, 300); this.btnAddBinding.Size = new System.Drawing.Size(110, 23); this.btnAddBinding.Text = "入力上書き(追加)"; this.btnAddBinding.Click += new System.EventHandler(this.btnAddBinding_Click);
            this.btnEditBinding.Location = new System.Drawing.Point(355, 300); this.btnEditBinding.Size = new System.Drawing.Size(60, 23); this.btnEditBinding.Text = "編集"; this.btnEditBinding.Click += new System.EventHandler(this.btnEditBinding_Click);
            this.btnDeleteBinding.Location = new System.Drawing.Point(487, 300); this.btnDeleteBinding.Size = new System.Drawing.Size(60, 23); this.btnDeleteBinding.Text = "削除"; this.btnDeleteBinding.Click += new System.EventHandler(this.btnDeleteBinding_Click);
            this.btnUpBinding.Location = new System.Drawing.Point(553, 300); this.btnUpBinding.Size = new System.Drawing.Size(50, 23); this.btnUpBinding.Text = "▲"; this.btnUpBinding.Click += new System.EventHandler(this.btnUpBinding_Click);
            this.btnDownBinding.Location = new System.Drawing.Point(609, 300); this.btnDownBinding.Size = new System.Drawing.Size(50, 23); this.btnDownBinding.Text = "▼"; this.btnDownBinding.Click += new System.EventHandler(this.btnDownBinding_Click);
            
            this.lblProfiles.AutoSize = true; this.lblProfiles.Location = new System.Drawing.Point(6, 9); this.lblProfiles.Text = "プロファイル:";
            this.lblBindings.AutoSize = true; this.lblBindings.Location = new System.Drawing.Point(237, 75); this.lblBindings.Text = "入力上書き設定 (プロファイル専用):";

            this.chkEnableXInput.AutoSize = true; this.chkEnableXInput.Location = new System.Drawing.Point(239, 9); this.chkEnableXInput.Text = "このプロファイルでベース出力有効化"; this.chkEnableXInput.CheckedChanged += new System.EventHandler(this.chkEnableXInput_CheckedChanged);
            
            this.chkChattering.AutoSize = true; this.chkChattering.Location = new System.Drawing.Point(239, 32); this.chkChattering.Text = "チャタリング防止有効化 (ms):"; this.chkChattering.CheckedChanged += new System.EventHandler(this.chkChattering_CheckedChanged);
            this.numChatterMs.Location = new System.Drawing.Point(410, 30); this.numChatterMs.Maximum = 1000; this.numChatterMs.Size = new System.Drawing.Size(50, 19); this.numChatterMs.ValueChanged += new System.EventHandler(this.numChatterMs_ValueChanged);
            
            this.chkOverlayMark.AutoSize = true; this.chkOverlayMark.Location = new System.Drawing.Point(239, 55); this.chkOverlayMark.Text = "切替時: アイコン通知"; this.chkOverlayMark.CheckedChanged += new System.EventHandler(this.chkOverlayMark_CheckedChanged);
            this.chkOverlayName.AutoSize = true; this.chkOverlayName.Location = new System.Drawing.Point(365, 55); this.chkOverlayName.Text = "切替時: 名前通知"; this.chkOverlayName.CheckedChanged += new System.EventHandler(this.chkOverlayName_CheckedChanged);
            
            this.chkStartup.AutoSize = true; this.chkStartup.Location = new System.Drawing.Point(6, 365); this.chkStartup.Text = "PC起動時にタスクトレイに起動"; this.chkStartup.CheckedChanged += new System.EventHandler(this.chkStartup_CheckedChanged);
            this.btnControllerBase.Location = new System.Drawing.Point(487, 5); this.btnControllerBase.Size = new System.Drawing.Size(172, 23); this.btnControllerBase.Text = "コントローラーベース設定..."; this.btnControllerBase.Click += new System.EventHandler(this.btnControllerBase_Click);

            this.tabPage2.Text = "入力テスト / 診断";
            this.tabPage2.Controls.Add(this.chkLog);
            this.tabPage2.Controls.Add(this.lblChatterCount);
            this.tabPage2.Controls.Add(this.btnResetChatter);
            this.tabPage2.Controls.Add(this.txtLog);
            
            this.chkLog.AutoSize = true; this.chkLog.Location = new System.Drawing.Point(6, 12); this.chkLog.Text = "入力テスト(ログ取得)を有効にする"; this.chkLog.CheckedChanged += new System.EventHandler(this.chkLog_CheckedChanged);
            
            this.lblChatterCount.AutoSize = true; this.lblChatterCount.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold); this.lblChatterCount.ForeColor = System.Drawing.Color.Red;
            this.lblChatterCount.Location = new System.Drawing.Point(250, 10); this.lblChatterCount.Text = "ブロックしたチャタリング回数: 0 回";
            
            this.btnResetChatter.Location = new System.Drawing.Point(520, 7); this.btnResetChatter.Size = new System.Drawing.Size(100, 23); this.btnResetChatter.Text = "回数リセット"; this.btnResetChatter.Click += new System.EventHandler(this.btnResetChatter_Click);
            
            this.txtLog.Location = new System.Drawing.Point(6, 35); this.txtLog.Multiline = true; this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; this.txtLog.ReadOnly = true; this.txtLog.Size = new System.Drawing.Size(660, 345);

            this.ClientSize = new System.Drawing.Size(680, 420);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle; this.MaximizeBox = false; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen; this.Text = "UsbInputMapper - 設定";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false); this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numChatterMs)).EndInit();
            this.tabPage2.ResumeLayout(false); this.tabPage2.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
