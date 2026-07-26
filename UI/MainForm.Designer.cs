namespace UsbInputMapper.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.TabPage tabProfile;
        private System.Windows.Forms.TabPage tabDiagnostic;
        
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
        private System.Windows.Forms.CheckBox chkOverlayMark;
        private System.Windows.Forms.CheckBox chkOverlayName;
        
        // 基本設定用のコントロール
        private System.Windows.Forms.CheckBox chkGlobalChattering;
        private System.Windows.Forms.NumericUpDown numGlobalChatterMs;
        private System.Windows.Forms.Label lblDoubleClick;
        private System.Windows.Forms.NumericUpDown numDoubleClick;
        private System.Windows.Forms.Label lblTripleClick;
        private System.Windows.Forms.NumericUpDown numTripleClick;
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.Button btnControllerBase;

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.CheckBox chkLog;
        private System.Windows.Forms.Label lblChatterCount; 
        private System.Windows.Forms.Button btnResetChatter; 

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.tabProfile = new System.Windows.Forms.TabPage();
            this.tabDiagnostic = new System.Windows.Forms.TabPage();

            this.lstProfiles = new System.Windows.Forms.ListBox(); this.lstBindings = new System.Windows.Forms.ListBox();
            this.btnAddProfile = new System.Windows.Forms.Button(); this.btnEditProfile = new System.Windows.Forms.Button();
            this.btnDuplicateProfile = new System.Windows.Forms.Button(); this.btnDeleteProfile = new System.Windows.Forms.Button();
            this.btnUpProfile = new System.Windows.Forms.Button(); this.btnDownProfile = new System.Windows.Forms.Button();
            this.btnAddBinding = new System.Windows.Forms.Button(); this.btnEditBinding = new System.Windows.Forms.Button();
            this.btnDuplicateBinding = new System.Windows.Forms.Button(); this.btnDeleteBinding = new System.Windows.Forms.Button();
            this.btnUpBinding = new System.Windows.Forms.Button(); this.btnDownBinding = new System.Windows.Forms.Button();
            this.lblProfiles = new System.Windows.Forms.Label(); this.lblBindings = new System.Windows.Forms.Label();
            
            this.chkEnableXInput = new System.Windows.Forms.CheckBox(); 
            this.chkOverlayMark = new System.Windows.Forms.CheckBox();
            this.chkOverlayName = new System.Windows.Forms.CheckBox();
            
            this.chkGlobalChattering = new System.Windows.Forms.CheckBox();
            this.numGlobalChatterMs = new System.Windows.Forms.NumericUpDown();
            this.lblDoubleClick = new System.Windows.Forms.Label();
            this.numDoubleClick = new System.Windows.Forms.NumericUpDown();
            this.lblTripleClick = new System.Windows.Forms.Label();
            this.numTripleClick = new System.Windows.Forms.NumericUpDown();
            this.chkStartup = new System.Windows.Forms.CheckBox(); 
            this.btnControllerBase = new System.Windows.Forms.Button();

            this.txtLog = new System.Windows.Forms.TextBox();
            this.chkLog = new System.Windows.Forms.CheckBox();
            this.lblChatterCount = new System.Windows.Forms.Label();
            this.btnResetChatter = new System.Windows.Forms.Button();

            this.tabControl1.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.tabProfile.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGlobalChatterMs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDoubleClick)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTripleClick)).BeginInit();
            this.tabDiagnostic.SuspendLayout();
            this.SuspendLayout();
            
            this.tabControl1.Controls.Add(this.tabGeneral);
            this.tabControl1.Controls.Add(this.tabProfile);
            this.tabControl1.Controls.Add(this.tabDiagnostic);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Size = new System.Drawing.Size(680, 420);
            
            // tabGeneral
            this.tabGeneral.Text = "基本設定";
            this.tabGeneral.Controls.Add(this.chkStartup);
            this.tabGeneral.Controls.Add(this.btnControllerBase); 
            this.tabGeneral.Controls.Add(this.chkGlobalChattering); 
            this.tabGeneral.Controls.Add(this.numGlobalChatterMs);
            this.tabGeneral.Controls.Add(this.lblDoubleClick);
            this.tabGeneral.Controls.Add(this.numDoubleClick);
            this.tabGeneral.Controls.Add(this.lblTripleClick);
            this.tabGeneral.Controls.Add(this.numTripleClick);
            
            this.chkStartup.AutoSize = true; this.chkStartup.Location = new System.Drawing.Point(20, 20); this.chkStartup.Text = "PC起動時にタスクトレイに起動"; this.chkStartup.CheckedChanged += new System.EventHandler(this.chkStartup_CheckedChanged);
            this.btnControllerBase.Location = new System.Drawing.Point(20, 50); this.btnControllerBase.Size = new System.Drawing.Size(200, 30); this.btnControllerBase.Text = "コントローラーベース設定..."; this.btnControllerBase.Click += new System.EventHandler(this.btnControllerBase_Click);
            
            this.chkGlobalChattering.AutoSize = true; this.chkGlobalChattering.Location = new System.Drawing.Point(20, 100); this.chkGlobalChattering.Text = "【全体】チャタリング防止有効化 (ms):"; this.chkGlobalChattering.CheckedChanged += new System.EventHandler(this.chkGlobalChattering_CheckedChanged);
            this.numGlobalChatterMs.Location = new System.Drawing.Point(220, 98); this.numGlobalChatterMs.Maximum = 1000; this.numGlobalChatterMs.Size = new System.Drawing.Size(60, 19); this.numGlobalChatterMs.ValueChanged += new System.EventHandler(this.numGlobalChatterMs_ValueChanged);
            
            this.lblDoubleClick.AutoSize = true; this.lblDoubleClick.Location = new System.Drawing.Point(20, 130); this.lblDoubleClick.Text = "ダブルクリック判定時間 (ms):";
            this.numDoubleClick.Location = new System.Drawing.Point(220, 128); this.numDoubleClick.Maximum = 1000; this.numDoubleClick.Size = new System.Drawing.Size(60, 19); this.numDoubleClick.ValueChanged += new System.EventHandler(this.numDoubleClick_ValueChanged);
            
            this.lblTripleClick.AutoSize = true; this.lblTripleClick.Location = new System.Drawing.Point(20, 160); this.lblTripleClick.Text = "トリプルクリック判定時間 (ms):";
            this.numTripleClick.Location = new System.Drawing.Point(220, 158); this.numTripleClick.Maximum = 1000; this.numTripleClick.Size = new System.Drawing.Size(60, 19); this.numTripleClick.ValueChanged += new System.EventHandler(this.numTripleClick_ValueChanged);

            // tabProfile
            this.tabProfile.Text = "プロファイル設定";
            this.tabProfile.Controls.Add(this.chkEnableXInput);
            this.tabProfile.Controls.Add(this.chkOverlayMark); this.tabProfile.Controls.Add(this.chkOverlayName);
            
            this.tabProfile.Controls.Add(this.btnDownBinding); this.tabProfile.Controls.Add(this.btnUpBinding); this.tabProfile.Controls.Add(this.btnDeleteBinding); this.tabProfile.Controls.Add(this.btnEditBinding); this.tabProfile.Controls.Add(this.btnAddBinding);
            this.tabProfile.Controls.Add(this.btnDownProfile); this.tabProfile.Controls.Add(this.btnUpProfile); this.tabProfile.Controls.Add(this.btnDeleteProfile); this.tabProfile.Controls.Add(this.btnDuplicateProfile); this.tabProfile.Controls.Add(this.btnEditProfile); this.tabProfile.Controls.Add(this.btnAddProfile);
            this.tabProfile.Controls.Add(this.lblBindings); this.tabProfile.Controls.Add(this.lblProfiles); 
            this.tabProfile.Controls.Add(this.lstBindings); this.tabProfile.Controls.Add(this.lstProfiles);

            this.lstProfiles.FormattingEnabled = true; this.lstProfiles.ItemHeight = 12; this.lstProfiles.Location = new System.Drawing.Point(6, 24); this.lstProfiles.Size = new System.Drawing.Size(220, 268); this.lstProfiles.SelectedIndexChanged += new System.EventHandler(this.lstProfiles_SelectedIndexChanged);
            this.lstBindings.FormattingEnabled = true; this.lstBindings.ItemHeight = 12; this.lstBindings.Location = new System.Drawing.Point(239, 70); this.lstBindings.Size = new System.Drawing.Size(420, 220); 
            
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
            this.lblBindings.AutoSize = true; this.lblBindings.Location = new System.Drawing.Point(237, 55); this.lblBindings.Text = "入力上書き設定 (プロファイル専用):";

            this.chkEnableXInput.AutoSize = true; this.chkEnableXInput.Location = new System.Drawing.Point(239, 9); this.chkEnableXInput.Text = "このプロファイルでベース出力有効化"; this.chkEnableXInput.CheckedChanged += new System.EventHandler(this.chkEnableXInput_CheckedChanged);
            
            this.chkOverlayMark.AutoSize = true; this.chkOverlayMark.Location = new System.Drawing.Point(239, 32); this.chkOverlayMark.Text = "切替時: アイコン通知"; this.chkOverlayMark.CheckedChanged += new System.EventHandler(this.chkOverlayMark_CheckedChanged);
            this.chkOverlayName.AutoSize = true; this.chkOverlayName.Location = new System.Drawing.Point(365, 32); this.chkOverlayName.Text = "切替時: 名前通知"; this.chkOverlayName.CheckedChanged += new System.EventHandler(this.chkOverlayName_CheckedChanged);
            
            // tabDiagnostic
            this.tabDiagnostic.Text = "入力テスト / 診断";
            this.tabDiagnostic.Controls.Add(this.chkLog);
            this.tabDiagnostic.Controls.Add(this.lblChatterCount);
            this.tabDiagnostic.Controls.Add(this.btnResetChatter);
            this.tabDiagnostic.Controls.Add(this.txtLog);
            
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
            this.tabGeneral.ResumeLayout(false); this.tabGeneral.PerformLayout();
            this.tabProfile.ResumeLayout(false); this.tabProfile.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numGlobalChatterMs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDoubleClick)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTripleClick)).EndInit();
            this.tabDiagnostic.ResumeLayout(false); this.tabDiagnostic.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
