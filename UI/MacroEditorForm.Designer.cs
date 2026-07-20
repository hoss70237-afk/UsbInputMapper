namespace UsbInputMapper.UI
{
    partial class MacroEditorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstSteps;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnEditStep;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnUpStep;
        private System.Windows.Forms.Button btnDownStep;
        private System.Windows.Forms.CheckBox chkUseDelay; 
        private System.Windows.Forms.NumericUpDown numDelay;
        private System.Windows.Forms.CheckBox chkUseFluctuation; 
        private System.Windows.Forms.NumericUpDown numFluctuation; 
        private System.Windows.Forms.Label lblPressState;
        private System.Windows.Forms.ComboBox cmbPressState;

        private System.Windows.Forms.CheckBox chkWaitForExit; // ★追加
        private System.Windows.Forms.Label lblWavStart; // ★追加
        private System.Windows.Forms.TextBox txtWavStart; // ★追加
        private System.Windows.Forms.Button btnWavStart; // ★追加
        private System.Windows.Forms.Label lblWavEnd; // ★追加
        private System.Windows.Forms.TextBox txtWavEnd; // ★追加
        private System.Windows.Forms.Button btnWavEnd; // ★追加

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbPlaybackMode;
        private System.Windows.Forms.Label lblTimeout;
        private System.Windows.Forms.NumericUpDown numTimeout;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.CheckBox chkRecord;
        private System.Windows.Forms.ComboBox cmbRecordMode;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.lstSteps = new System.Windows.Forms.ListBox(); this.btnAdd = new System.Windows.Forms.Button(); this.btnEditStep = new System.Windows.Forms.Button(); this.btnRemove = new System.Windows.Forms.Button(); this.btnUpStep = new System.Windows.Forms.Button(); this.btnDownStep = new System.Windows.Forms.Button();
            this.chkUseDelay = new System.Windows.Forms.CheckBox(); this.numDelay = new System.Windows.Forms.NumericUpDown();
            this.chkUseFluctuation = new System.Windows.Forms.CheckBox(); this.numFluctuation = new System.Windows.Forms.NumericUpDown();
            this.lblPressState = new System.Windows.Forms.Label(); this.cmbPressState = new System.Windows.Forms.ComboBox();
            
            this.chkWaitForExit = new System.Windows.Forms.CheckBox(); this.lblWavStart = new System.Windows.Forms.Label(); this.txtWavStart = new System.Windows.Forms.TextBox(); this.btnWavStart = new System.Windows.Forms.Button();
            this.lblWavEnd = new System.Windows.Forms.Label(); this.txtWavEnd = new System.Windows.Forms.TextBox(); this.btnWavEnd = new System.Windows.Forms.Button();

            this.label2 = new System.Windows.Forms.Label(); this.cmbPlaybackMode = new System.Windows.Forms.ComboBox(); this.lblTimeout = new System.Windows.Forms.Label(); this.numTimeout = new System.Windows.Forms.NumericUpDown(); this.btnOK = new System.Windows.Forms.Button();
            this.chkRecord = new System.Windows.Forms.CheckBox(); this.cmbRecordMode = new System.Windows.Forms.ComboBox();
            
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numFluctuation)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit(); this.SuspendLayout();
            
            this.lstSteps.FormattingEnabled = true; this.lstSteps.ItemHeight = 12; this.lstSteps.Location = new System.Drawing.Point(12, 12); this.lstSteps.Size = new System.Drawing.Size(260, 316);
            
            this.chkUseDelay.AutoSize = true; this.chkUseDelay.Location = new System.Drawing.Point(280, 12); this.chkUseDelay.Text = "待機時間を使用する(ms)"; this.chkUseDelay.Checked = true;
            this.numDelay.Location = new System.Drawing.Point(280, 30); this.numDelay.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numDelay.Size = new System.Drawing.Size(80, 19); this.numDelay.Value = new decimal(new int[] { 50, 0, 0, 0 });
            
            this.chkUseFluctuation.AutoSize = true; this.chkUseFluctuation.Location = new System.Drawing.Point(390, 12); this.chkUseFluctuation.Text = "揺らぎを使用する(±ms)";
            this.numFluctuation.Location = new System.Drawing.Point(390, 30); this.numFluctuation.Maximum = new decimal(new int[] { 1000, 0, 0, 0 }); this.numFluctuation.Size = new System.Drawing.Size(80, 19); this.numFluctuation.Value = new decimal(new int[] { 15, 0, 0, 0 });

            this.lblPressState.AutoSize = true; this.lblPressState.Location = new System.Drawing.Point(280, 60); this.lblPressState.Text = "押下状態:";
            this.cmbPressState.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbPressState.Location = new System.Drawing.Point(280, 75); this.cmbPressState.Size = new System.Drawing.Size(80, 20);
            
            this.chkWaitForExit.AutoSize = true; this.chkWaitForExit.Location = new System.Drawing.Point(390, 77); this.chkWaitForExit.Text = "AHK等終了まで待機";

            this.lblWavStart.AutoSize = true; this.lblWavStart.Location = new System.Drawing.Point(280, 105); this.lblWavStart.Text = "起動時に再生:";
            this.txtWavStart.Location = new System.Drawing.Point(280, 120); this.txtWavStart.Size = new System.Drawing.Size(160, 19);
            this.btnWavStart.Location = new System.Drawing.Point(445, 118); this.btnWavStart.Size = new System.Drawing.Size(30, 23); this.btnWavStart.Text = "..."; this.btnWavStart.Click += new System.EventHandler(this.btnWavStart_Click);

            this.lblWavEnd.AutoSize = true; this.lblWavEnd.Location = new System.Drawing.Point(280, 150); this.lblWavEnd.Text = "終了後に再生:";
            this.txtWavEnd.Location = new System.Drawing.Point(280, 165); this.txtWavEnd.Size = new System.Drawing.Size(160, 19);
            this.btnWavEnd.Location = new System.Drawing.Point(445, 163); this.btnWavEnd.Size = new System.Drawing.Size(30, 23); this.btnWavEnd.Text = "..."; this.btnWavEnd.Click += new System.EventHandler(this.btnWavEnd_Click);

            this.btnAdd.Location = new System.Drawing.Point(280, 205); this.btnAdd.Size = new System.Drawing.Size(85, 23); this.btnAdd.Text = "ステップ追加"; this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            this.btnEditStep.Location = new System.Drawing.Point(385, 205); this.btnEditStep.Size = new System.Drawing.Size(85, 23); this.btnEditStep.Text = "ステップ編集"; this.btnEditStep.Click += new System.EventHandler(this.btnEditStep_Click);
            
            this.btnRemove.Location = new System.Drawing.Point(280, 245); this.btnRemove.Size = new System.Drawing.Size(85, 23); this.btnRemove.Text = "削除"; this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            this.btnUpStep.Location = new System.Drawing.Point(280, 275); this.btnUpStep.Size = new System.Drawing.Size(85, 23); this.btnUpStep.Text = "▲ 上へ"; this.btnUpStep.Click += new System.EventHandler(this.btnUpStep_Click);
            this.btnDownStep.Location = new System.Drawing.Point(385, 275); this.btnDownStep.Size = new System.Drawing.Size(85, 23); this.btnDownStep.Text = "▼ 下へ"; this.btnDownStep.Click += new System.EventHandler(this.btnDownStep_Click);
            
            this.chkRecord.Appearance = System.Windows.Forms.Appearance.Button; this.chkRecord.Location = new System.Drawing.Point(12, 335); this.chkRecord.Size = new System.Drawing.Size(120, 24); this.chkRecord.Text = "レコーディング開始"; this.chkRecord.TextAlign = System.Drawing.ContentAlignment.MiddleCenter; this.chkRecord.CheckedChanged += new System.EventHandler(this.chkRecord_CheckedChanged);
            this.cmbRecordMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbRecordMode.Location = new System.Drawing.Point(135, 337); this.cmbRecordMode.Size = new System.Drawing.Size(137, 20);
            
            this.label2.AutoSize = true; this.label2.Location = new System.Drawing.Point(12, 373); this.label2.Text = "再生モード:";
            this.cmbPlaybackMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbPlaybackMode.Location = new System.Drawing.Point(80, 370); this.cmbPlaybackMode.Size = new System.Drawing.Size(190, 20); this.cmbPlaybackMode.SelectedIndexChanged += new System.EventHandler(this.cmbPlaybackMode_SelectedIndexChanged);
            
            this.lblTimeout.AutoSize = true; this.lblTimeout.Location = new System.Drawing.Point(12, 403); this.lblTimeout.Text = "タイムアウト:"; this.lblTimeout.Visible = false;
            this.numTimeout.Location = new System.Drawing.Point(80, 401); this.numTimeout.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numTimeout.Size = new System.Drawing.Size(80, 19); this.numTimeout.Visible = false;
            
            this.btnOK.Location = new System.Drawing.Point(395, 398); this.btnOK.Size = new System.Drawing.Size(75, 23); this.btnOK.Text = "完了"; this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            
            this.ClientSize = new System.Drawing.Size(500, 435);
            this.Controls.Add(this.btnOK); this.Controls.Add(this.numTimeout); this.Controls.Add(this.lblTimeout); this.Controls.Add(this.cmbPlaybackMode); this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbRecordMode); this.Controls.Add(this.chkRecord); this.Controls.Add(this.btnDownStep); this.Controls.Add(this.btnUpStep); this.Controls.Add(this.btnEditStep); this.Controls.Add(this.btnRemove); this.Controls.Add(this.btnAdd); 
            this.Controls.Add(this.cmbPressState); this.Controls.Add(this.lblPressState);
            this.Controls.Add(this.chkWaitForExit); this.Controls.Add(this.lblWavStart); this.Controls.Add(this.txtWavStart); this.Controls.Add(this.btnWavStart); this.Controls.Add(this.lblWavEnd); this.Controls.Add(this.txtWavEnd); this.Controls.Add(this.btnWavEnd);
            this.Controls.Add(this.numFluctuation); this.Controls.Add(this.chkUseFluctuation); this.Controls.Add(this.numDelay); this.Controls.Add(this.chkUseDelay); this.Controls.Add(this.lstSteps);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; this.Text = "マクロエディタ"; this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MacroEditorForm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numFluctuation)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit(); this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
