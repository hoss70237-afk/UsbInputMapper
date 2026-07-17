namespace UsbInputMapper.UI
{
    partial class ControllerBaseForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstBase;
        private System.Windows.Forms.Button btnWizard;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnUp;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Label lblInfo;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.lstBase = new System.Windows.Forms.ListBox();
            this.btnWizard = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.lblInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();

            this.lblInfo.AutoSize = true; this.lblInfo.Location = new System.Drawing.Point(12, 12); this.lblInfo.Text = "※ここで設定したコントローラー設定は、XInputが有効なプロファイルでベースとして適用されます。";
            
            this.lstBase.FormattingEnabled = true; this.lstBase.ItemHeight = 12; this.lstBase.Location = new System.Drawing.Point(12, 35); this.lstBase.Size = new System.Drawing.Size(460, 220);
            
            this.btnWizard.Location = new System.Drawing.Point(12, 265); this.btnWizard.Size = new System.Drawing.Size(150, 25); this.btnWizard.Text = "一括セットアップウィザード"; this.btnWizard.Click += new System.EventHandler(this.btnWizard_Click);
            
            this.btnAdd.Location = new System.Drawing.Point(180, 265); this.btnAdd.Size = new System.Drawing.Size(60, 25); this.btnAdd.Text = "追加"; this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            this.btnEdit.Location = new System.Drawing.Point(245, 265); this.btnEdit.Size = new System.Drawing.Size(60, 25); this.btnEdit.Text = "編集"; this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            this.btnDelete.Location = new System.Drawing.Point(310, 265); this.btnDelete.Size = new System.Drawing.Size(60, 25); this.btnDelete.Text = "削除"; this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            this.btnUp.Location = new System.Drawing.Point(380, 265); this.btnUp.Size = new System.Drawing.Size(40, 25); this.btnUp.Text = "▲"; this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            this.btnDown.Location = new System.Drawing.Point(425, 265); this.btnDown.Size = new System.Drawing.Size(40, 25); this.btnDown.Text = "▼"; this.btnDown.Click += new System.EventHandler(this.btnDown_Click);

            this.ClientSize = new System.Drawing.Size(484, 301);
            this.Controls.Add(this.btnDown); this.Controls.Add(this.btnUp); this.Controls.Add(this.btnDelete); this.Controls.Add(this.btnEdit); this.Controls.Add(this.btnAdd); this.Controls.Add(this.btnWizard); this.Controls.Add(this.lstBase); this.Controls.Add(this.lblInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; this.Text = "コントローラーベース設定";
            this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
