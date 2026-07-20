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
        private System.Windows.Forms.CheckBox chkUseDelay; // ★追加
        private System.Windows.Forms.NumericUpDown numDelay;
        private System.Windows.Forms.CheckBox chkUseFluctuation; // ★追加
        private System.Windows.Forms.NumericUpDown numFluctuation; // ★追加
        private System.Windows.Forms.Label lblPressState;
        private System.Windows.Forms.ComboBox cmbPressState;
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
            this.label2 = new System.Windows.Forms.Label(); this.cmbPlaybackMode = new System.Windows.Forms.ComboBox(); this.lblTimeout = new System.Windows.Forms.Label(); this.numTimeout = new System.Windows.Forms.NumericUpDown(); this.btnOK = new System.Windows.Forms.Button();
            this.chkRecord = new System.Windows.Forms.CheckBox(); this.cmbRecordMode = new System.Windows.Forms.ComboBox();
            
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numFluctuation)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit(); this.SuspendLayout();
            
            this.lstSteps.FormattingEnabled = true; this.lstSteps.ItemHeight = 12; this.lstSteps.Location = new System.Drawing.Point(12, 12); this.lstSteps.Size = new System.Drawing.Size(260, 244);
            
            this.chkUseDelay.AutoSize = true; this.chkUseDelay.Location = new System.Drawing.Point(280, 12); this.chkUseDelay.Text = "待機時間を使用する(ms)"; this.chkUseDelay.Checked = true;
            this.numDelay.Location = new System.Drawing.Point(280, 30); this.numDelay.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numDelay.Size = new System.Drawing.Size(80, 19); this.numDelay.Value = new decimal(new int[] { 50, 0, 0, 0 });
            
            this.chkUseFluctuation.AutoSize = true; this.chkUseFluctuation.Location = new System.Drawing.Point(280, 55); this.chkUseFluctuation.Text = "揺らぎを使用する(±ms)";
            this.numFluctuation.Location = new System.Drawing.Point(280, 73); this.numFluctuation.Maximum = new decimal(new int[] { 1000, 0, 0, 0 }); this.numFluctuation.Size = new System.Drawing.Size(80, 19); this.numFluctuation.Value = new decimal(new int[] { 15, 0, 0, 0 });

            this.lblPressState.AutoSize = true; this.lblPressState.Location = new System.Drawing.Point(280, 100); this.lblPressState.Text = "押下状態:";
            this.cmbPressState.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbPressState.Location = new System.Drawing.Point(280, 115); this.cmbPressState.Size = new System.Drawing.Size(80, 20);
            
            this.btnAdd.Location = new System.Drawing.Point(280, 145); this.btnAdd.Size = new System.Drawing.Size(80, 23); this.btnAdd.Text = "ステップ追加"; this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            this.btnEditStep.Location = new System.Drawing.Point(280, 175); this.btnEditStep.Size = new System.Drawing.Size(80, 23); this.btnEditStep.Text = "編集"; this.btnEditStep.Click += new System.EventHandler(this.btnEditStep_Click);
            this.btnRemove.Location = new System.Drawing.Point(280, 205); this.btnRemove.Size = new System.Drawing.Size(80, 23); this.btnRemove.Text = "削除"; this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            this.btnUpStep.Location = new System.Drawing.Point(280, 235); this.btnUpStep.Size = new System.Drawing.Size(80, 23); this.btnUpStep.Text = "▲ 上へ"; this.btnUpStep.Click += new System.EventHandler(this.btnUpStep_Click);
            this.btnDownStep.Location = new System.Drawing.Point(280, 265); this.btnDownStep.Size = new System.Drawing.Size(80, 23); this.btnDownStep.Text = "▼ 下へ"; this.btnDownStep.Click += new System.EventHandler(this.btnDownStep_Click);
            
            this.chkRecord.Appearance = System.Windows.Forms.Appearance.Button; this.chkRecord.Location = new System.Drawing.Point(12, 260); this.chkRecord.Size = new System.Drawing.Size(120, 24); this.chkRecord.Text = "レコーディング開始"; this.chkRecord.TextAlign = System.Drawing.ContentAlignment.MiddleCenter; this.chkRecord.CheckedChanged += new System.EventHandler(this.chkRecord_CheckedChanged);
            this.cmbRecordMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbRecordMode.Location = new System.Drawing.Point(135, 262); this.cmbRecordMode.Size = new System.Drawing.Size(137, 20);
            
            this.label2.AutoSize = true; this.label2.Location = new System.Drawing.Point(12, 293); this.label2.Text = "再生モード:";
            this.cmbPlaybackMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbPlaybackMode.Location = new System.Drawing.Point(80, 290); this.cmbPlaybackMode.Size = new System.Drawing.Size(190, 20); this.cmbPlaybackMode.SelectedIndexChanged += new System.EventHandler(this.cmbPlaybackMode_SelectedIndexChanged);
            
            this.lblTimeout.AutoSize = true; this.lblTimeout.Location = new System.Drawing.Point(12, 323); this.lblTimeout.Text = "タイムアウト:"; this.lblTimeout.Visible = false;
            this.numTimeout.Location = new System.Drawing.Point(80, 321); this.numTimeout.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numTimeout.Size = new System.Drawing.Size(80, 19); this.numTimeout.Visible = false;
            
            this.btnOK.Location = new System.Drawing.Point(285, 318); this.btnOK.Size = new System.Drawing.Size(75, 23); this.btnOK.Text = "完了"; this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            
            this.ClientSize = new System.Drawing.Size(424, 355);
            this.Controls.Add(this.btnOK); this.Controls.Add(this.numTimeout); this.Controls.Add(this.lblTimeout); this.Controls.Add(this.cmbPlaybackMode); this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbRecordMode); this.Controls.Add(this.chkRecord); this.Controls.Add(this.btnDownStep); this.Controls.Add(this.btnUpStep); this.Controls.Add(this.btnEditStep); this.Controls.Add(this.btnRemove); this.Controls.Add(this.btnAdd); 
            this.Controls.Add(this.cmbPressState); this.Controls.Add(this.lblPressState);
            this.Controls.Add(this.numFluctuation); this.Controls.Add(this.chkUseFluctuation); this.Controls.Add(this.numDelay); this.Controls.Add(this.chkUseDelay); this.Controls.Add(this.lstSteps);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; this.Text = "マクロエディタ"; this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MacroEditorForm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numFluctuation)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit(); this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
