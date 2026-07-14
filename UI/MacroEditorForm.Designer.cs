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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numDelay;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbPlaybackMode;
        private System.Windows.Forms.Label lblTimeout;
        private System.Windows.Forms.NumericUpDown numTimeout;
        private System.Windows.Forms.Button btnOK;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lstSteps = new System.Windows.Forms.ListBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEditStep = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnUpStep = new System.Windows.Forms.Button();
            this.btnDownStep = new System.Windows.Forms.Button();
            
            this.label1 = new System.Windows.Forms.Label();
            this.numDelay = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbPlaybackMode = new System.Windows.Forms.ComboBox();
            this.lblTimeout = new System.Windows.Forms.Label();
            this.numTimeout = new System.Windows.Forms.NumericUpDown();
            this.btnOK = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit();
            this.SuspendLayout();
            
            this.lstSteps.FormattingEnabled = true; this.lstSteps.ItemHeight = 12; this.lstSteps.Location = new System.Drawing.Point(12, 12); this.lstSteps.Size = new System.Drawing.Size(260, 220);
            
            this.label1.AutoSize = true; this.label1.Location = new System.Drawing.Point(280, 15); this.label1.Text = "実行前待機(ms):";
            this.numDelay.Location = new System.Drawing.Point(280, 30); this.numDelay.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numDelay.Size = new System.Drawing.Size(80, 19); this.numDelay.Value = new decimal(new int[] { 50, 0, 0, 0 });
            
            this.btnAdd.Location = new System.Drawing.Point(280, 60); this.btnAdd.Size = new System.Drawing.Size(80, 23); this.btnAdd.Text = "ステップ追加"; this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            this.btnEditStep.Location = new System.Drawing.Point(280, 90); this.btnEditStep.Size = new System.Drawing.Size(80, 23); this.btnEditStep.Text = "編集"; this.btnEditStep.Click += new System.EventHandler(this.btnEditStep_Click);
            this.btnRemove.Location = new System.Drawing.Point(280, 120); this.btnRemove.Size = new System.Drawing.Size(80, 23); this.btnRemove.Text = "削除"; this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            this.btnUpStep.Location = new System.Drawing.Point(280, 150); this.btnUpStep.Size = new System.Drawing.Size(80, 23); this.btnUpStep.Text = "▲ 上へ"; this.btnUpStep.Click += new System.EventHandler(this.btnUpStep_Click);
            this.btnDownStep.Location = new System.Drawing.Point(280, 180); this.btnDownStep.Size = new System.Drawing.Size(80, 23); this.btnDownStep.Text = "▼ 下へ"; this.btnDownStep.Click += new System.EventHandler(this.btnDownStep_Click);
            
            this.label2.AutoSize = true; this.label2.Location = new System.Drawing.Point(12, 245); this.label2.Text = "再生モード:";
            this.cmbPlaybackMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbPlaybackMode.Location = new System.Drawing.Point(80, 242); this.cmbPlaybackMode.Size = new System.Drawing.Size(190, 20); this.cmbPlaybackMode.SelectedIndexChanged += new System.EventHandler(this.cmbPlaybackMode_SelectedIndexChanged);
            
            this.lblTimeout.AutoSize = true; this.lblTimeout.Location = new System.Drawing.Point(12, 275); this.lblTimeout.Text = "タイムアウト:"; this.lblTimeout.Visible = false;
            this.numTimeout.Location = new System.Drawing.Point(80, 273); this.numTimeout.Maximum = new decimal(new int[] { 60000, 0, 0, 0 }); this.numTimeout.Size = new System.Drawing.Size(80, 19); this.numTimeout.Visible = false;
            
            this.btnOK.Location = new System.Drawing.Point(285, 270); this.btnOK.Size = new System.Drawing.Size(75, 23); this.btnOK.Text = "完了"; this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            
            this.ClientSize = new System.Drawing.Size(374, 311);
            this.Controls.Add(this.btnOK); this.Controls.Add(this.numTimeout); this.Controls.Add(this.lblTimeout);
            this.Controls.Add(this.cmbPlaybackMode); this.Controls.Add(this.label2);
            this.Controls.Add(this.btnDownStep); this.Controls.Add(this.btnUpStep); this.Controls.Add(this.btnEditStep); this.Controls.Add(this.btnRemove); this.Controls.Add(this.btnAdd); this.Controls.Add(this.numDelay);
            this.Controls.Add(this.label1); this.Controls.Add(this.lstSteps);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; // ★中央表示
            this.Text = "マクロエディタ";
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit();
            this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
