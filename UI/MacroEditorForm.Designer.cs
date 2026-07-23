namespace UsbInputMapper.UI
{
    partial class MacroEditorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstSteps;
        private System.Windows.Forms.Panel pnlTimeline; // ★追加
        private System.Windows.Forms.Button btnToggleTimeline; // ★追加
        
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnEditStep;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnUpStep;
        private System.Windows.Forms.Button btnDownStep;
        
        private System.Windows.Forms.Panel pnlStepDetails;
        private System.Windows.Forms.CheckBox chkUseDelay; 
        private System.Windows.Forms.NumericUpDown numDelay;
        private System.Windows.Forms.CheckBox chkUseFluctuation; 
        private System.Windows.Forms.NumericUpDown numFluctuation; 
        private System.Windows.Forms.Label lblPressState;
        private System.Windows.Forms.ComboBox cmbPressState;
        private System.Windows.Forms.Label lblWavStart;
        private System.Windows.Forms.TextBox txtWavStart;
        private System.Windows.Forms.Button btnBrowseWavStart;
        private System.Windows.Forms.Label lblWavEnd;
        private System.Windows.Forms.TextBox txtWavEnd;
        private System.Windows.Forms.Button btnBrowseWavEnd;
        private System.Windows.Forms.CheckBox chkWaitForExit;

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
            this.lstSteps = new System.Windows.Forms.ListBox(); 
            this.pnlTimeline = new System.Windows.Forms.Panel();
            this.btnToggleTimeline = new System.Windows.Forms.Button();
            
            this.btnAdd = new System.Windows.Forms.Button(); this.btnEditStep = new System.Windows.Forms.Button(); this.btnRemove = new System.Windows.Forms.Button(); this.btnUpStep = new System.Windows.Forms.Button(); this.btnDownStep = new System.Windows.Forms.Button();
            
            this.pnlStepDetails = new System.Windows.Forms.Panel();
            this.chkUseDelay = new System.Windows.Forms.CheckBox(); this.numDelay = new System.Windows.Forms.NumericUpDown();
            this.chkUseFluctuation = new System.Windows.Forms.CheckBox(); this.numFluctuation = new System.Windows.Forms.NumericUpDown();
            this.lblPressState = new System.Windows.Forms.Label(); this.cmbPressState = new System.Windows.Forms.ComboBox();
            this.lblWavStart = new System.Windows.Forms.Label(); this.txtWavStart = new System.Windows.Forms.TextBox(); this.btnBrowseWavStart = new System.Windows.Forms.Button();
            this.lblWavEnd = new System.Windows.Forms.Label(); this.txtWavEnd = new System.Windows.Forms.TextBox(); this.btnBrowseWavEnd = new System.Windows.Forms.Button();
            this.chkWaitForExit = new System.Windows.Forms.CheckBox();

            this.label2 = new System.Windows.Forms.Label(); this.cmbPlaybackMode = new System.Windows.Forms.ComboBox(); this.lblTimeout = new System.Windows.Forms.Label(); this.numTimeout = new System.Windows.Forms.NumericUpDown(); this.btnOK = new System.Windows.Forms.Button();
            this.chkRecord = new System.Windows.Forms.CheckBox(); this.cmbRecordMode = new System.Windows.Forms.ComboBox();
            
            this.pnlStepDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numFluctuation)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit(); this.SuspendLayout();
            
            this.lstSteps.FormattingEnabled = true; this.lstSteps.ItemHeight = 12; this.lstSteps.Location = new System.Drawing.Point(12, 40); this.lstSteps.Size = new System.Drawing.Size(260, 244);
            
            this.pnlTimeline.Location = new System.Drawing.Point(12, 40); this.pnlTimeline.Size = new System.Drawing.Size(260, 244); this.pnlTimeline.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle; this.pnlTimeline.Visible = false;
            
            this.btnToggleTimeline.Location = new System.Drawing.Point(12, 12); this.btnToggleTimeline.Size = new System.Drawing.Size(260, 23); this.btnToggleTimeline.Text = "タイムライン編集 (絶対時間)"; this.btnToggleTimeline.Click += new System.EventHandler(this.btnToggleTimeline_Click);
            
            this.pnlStepDetails.Location = new System.Drawing.Point(280, 40); this.pnlStepDetails.Size = new System.Drawing.Size(200, 244);
            this.pnlStepDetails.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            this.chkUseDelay.AutoSize = true; this.chkUseDelay.Location = new System.Drawing.Point(10, 10); this.chkUseDelay.Text = "待機時間を使用する(ms)"; this.chkUseDelay.Checked = true;
            this.numDelay.Location = new System.Drawing.Point(10, 28); this.numDelay.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numDelay.Size = new System.Drawing.Size(80, 19); this.numDelay.Value = new decimal(new int[] { 50, 0, 0, 0 });
            
            this.chkUseFluctuation.AutoSize = true; this.chkUseFluctuation.Location = new System.Drawing.Point(10, 53); this.chkUseFluctuation.Text = "揺らぎを使用する(±ms)";
            this.numFluctuation.Location = new System.Drawing.Point(10, 71); this.numFluctuation.Maximum = new decimal(new int[] { 1000, 0, 0, 0 }); this.numFluctuation.Size = new System.Drawing.Size(80, 19); this.numFluctuation.Value = new decimal(new int[] { 15, 0, 0, 0 });

            this.lblPressState.AutoSize = true; this.lblPressState.Location = new System.Drawing.Point(10, 98); this.lblPressState.Text = "押下状態:";
            this.cmbPressState.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbPressState.Location = new System.Drawing.Point(70, 95); this.cmbPressState.Size = new System.Drawing.Size(120, 20);
            
            this.lblWavStart.AutoSize = true; this.lblWavStart.Location = new System.Drawing.Point(10, 125); this.lblWavStart.Text = "開始WAV:";
            this.txtWavStart.Location = new System.Drawing.Point(10, 142); this.txtWavStart.Size = new System.Drawing.Size(145, 19);
            this.btnBrowseWavStart.Location = new System.Drawing.Point(160, 140); this.btnBrowseWavStart.Size = new System.Drawing.Size(30, 23); this.btnBrowseWavStart.Text = "..."; this.btnBrowseWavStart.Click += new System.EventHandler(this.btnBrowseWavStart_Click);

            this.lblWavEnd.AutoSize = true; this.lblWavEnd.Location = new System.Drawing.Point(10, 167); this.lblWavEnd.Text = "終了WAV:";
            this.txtWavEnd.Location = new System.Drawing.Point(10, 184); this.txtWavEnd.Size = new System.Drawing.Size(145, 19);
            this.btnBrowseWavEnd.Location = new System.Drawing.Point(160, 182); this.btnBrowseWavEnd.Size = new System.Drawing.Size(30, 23); this.btnBrowseWavEnd.Text = "..."; this.btnBrowseWavEnd.Click += new System.EventHandler(this.btnBrowseWavEnd_Click);

            this.chkWaitForExit.AutoSize = true; this.chkWaitForExit.Location = new System.Drawing.Point(10, 215); this.chkWaitForExit.Text = "処理が終わるまで次を発火しない"; this.chkWaitForExit.Visible = false;

            this.pnlStepDetails.Controls.Add(this.chkUseDelay); this.pnlStepDetails.Controls.Add(this.numDelay);
            this.pnlStepDetails.Controls.Add(this.chkUseFluctuation); this.pnlStepDetails.Controls.Add(this.numFluctuation);
            this.pnlStepDetails.Controls.Add(this.lblPressState); this.pnlStepDetails.Controls.Add(this.cmbPressState);
            this.pnlStepDetails.Controls.Add(this.lblWavStart); this.pnlStepDetails.Controls.Add(this.txtWavStart); this.pnlStepDetails.Controls.Add(this.btnBrowseWavStart);
            this.pnlStepDetails.Controls.Add(this.lblWavEnd); this.pnlStepDetails.Controls.Add(this.txtWavEnd); this.pnlStepDetails.Controls.Add(this.btnBrowseWavEnd);
            this.pnlStepDetails.Controls.Add(this.chkWaitForExit);
            
            this.btnAdd.Location = new System.Drawing.Point(490, 40); this.btnAdd.Size = new System.Drawing.Size(80, 23); this.btnAdd.Text = "追加/編集..."; this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            this.btnEditStep.Location = new System.Drawing.Point(490, 70); this.btnEditStep.Size = new System.Drawing.Size(80, 23); this.btnEditStep.Text = "アクション変更"; this.btnEditStep.Click += new System.EventHandler(this.btnEditStep_Click);
            this.btnRemove.Location = new System.Drawing.Point(490, 100); this.btnRemove.Size = new System.Drawing.Size(80, 23); this.btnRemove.Text = "削除"; this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            this.btnUpStep.Location = new System.Drawing.Point(490, 130); this.btnUpStep.Size = new System.Drawing.Size(80, 23); this.btnUpStep.Text = "▲ 上へ"; this.btnUpStep.Click += new System.EventHandler(this.btnUpStep_Click);
            this.btnDownStep.Location = new System.Drawing.Point(490, 160); this.btnDownStep.Size = new System.Drawing.Size(80, 23); this.btnDownStep.Text = "▼ 下へ"; this.btnDownStep.Click += new System.EventHandler(this.btnDownStep_Click);
            
            this.chkRecord.Appearance = System.Windows.Forms.Appearance.Button; this.chkRecord.Location = new System.Drawing.Point(12, 290); this.chkRecord.Size = new System.Drawing.Size(120, 24); this.chkRecord.Text = "レコーディング開始"; this.chkRecord.TextAlign = System.Drawing.ContentAlignment.MiddleCenter; this.chkRecord.CheckedChanged += new System.EventHandler(this.chkRecord_CheckedChanged);
            this.cmbRecordMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbRecordMode.Location = new System.Drawing.Point(135, 292); this.cmbRecordMode.Size = new System.Drawing.Size(137, 20);
            
            this.label2.AutoSize = true; this.label2.Location = new System.Drawing.Point(12, 323); this.label2.Text = "再生モード:";
            this.cmbPlaybackMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbPlaybackMode.Location = new System.Drawing.Point(80, 320); this.cmbPlaybackMode.Size = new System.Drawing.Size(190, 20); this.cmbPlaybackMode.SelectedIndexChanged += new System.EventHandler(this.cmbPlaybackMode_SelectedIndexChanged);
            
            this.lblTimeout.AutoSize = true; this.lblTimeout.Location = new System.Drawing.Point(12, 353); this.lblTimeout.Text = "タイムアウト:"; this.lblTimeout.Visible = false;
            this.numTimeout.Location = new System.Drawing.Point(80, 351); this.numTimeout.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numTimeout.Size = new System.Drawing.Size(80, 19); this.numTimeout.Visible = false;
            
            this.btnOK.Location = new System.Drawing.Point(495, 348); this.btnOK.Size = new System.Drawing.Size(75, 23); this.btnOK.Text = "完了"; this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            
            this.ClientSize = new System.Drawing.Size(584, 385);
            this.Controls.Add(this.btnOK); this.Controls.Add(this.numTimeout); this.Controls.Add(this.lblTimeout); this.Controls.Add(this.cmbPlaybackMode); this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbRecordMode); this.Controls.Add(this.chkRecord); this.Controls.Add(this.btnDownStep); this.Controls.Add(this.btnUpStep); this.Controls.Add(this.btnEditStep); this.Controls.Add(this.btnRemove); this.Controls.Add(this.btnAdd); 
            this.Controls.Add(this.pnlStepDetails); this.Controls.Add(this.btnToggleTimeline); this.Controls.Add(this.pnlTimeline); this.Controls.Add(this.lstSteps);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; this.Text = "マクロエディタ"; this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MacroEditorForm_FormClosed);
            
            this.pnlStepDetails.ResumeLayout(false); this.pnlStepDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numFluctuation)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit(); this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
