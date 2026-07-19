namespace UsbInputMapper.UI
{
    partial class ProfileEditorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox lstApps;
        private System.Windows.Forms.Button btnAddApp;
        private System.Windows.Forms.Button btnRemoveApp;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblTargetPicker;
        private System.Windows.Forms.Label lblTargetDesc;
        private System.Windows.Forms.CheckBox chkNotifyVibration; // ★追加

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lstApps = new System.Windows.Forms.ListBox();
            this.btnAddApp = new System.Windows.Forms.Button();
            this.btnRemoveApp = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblTargetPicker = new System.Windows.Forms.Label();
            this.lblTargetDesc = new System.Windows.Forms.Label();
            this.chkNotifyVibration = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            
            this.label1.AutoSize = true; this.label1.Location = new System.Drawing.Point(12, 15); this.label1.Text = "プロファイル名:";
            this.txtName.Location = new System.Drawing.Point(87, 12); this.txtName.Size = new System.Drawing.Size(265, 19);
            this.label2.AutoSize = true; this.label2.Location = new System.Drawing.Point(12, 48); this.label2.Text = "対象アプリケーション:";
            this.lstApps.FormattingEnabled = true; this.lstApps.ItemHeight = 12; this.lstApps.Location = new System.Drawing.Point(14, 63); this.lstApps.Size = new System.Drawing.Size(257, 88);
            this.btnAddApp.Location = new System.Drawing.Point(277, 63); this.btnAddApp.Size = new System.Drawing.Size(75, 23); this.btnAddApp.Text = "参照追加"; this.btnAddApp.Click += new System.EventHandler(this.btnAddApp_Click);
            this.btnRemoveApp.Location = new System.Drawing.Point(277, 92); this.btnRemoveApp.Size = new System.Drawing.Size(75, 23); this.btnRemoveApp.Text = "削除"; this.btnRemoveApp.Click += new System.EventHandler(this.btnRemoveApp_Click);
            
            this.lblTargetPicker.AutoSize = true; this.lblTargetPicker.Font = new System.Drawing.Font("MS UI Gothic", 24F); this.lblTargetPicker.ForeColor = System.Drawing.Color.DodgerBlue; this.lblTargetPicker.Location = new System.Drawing.Point(293, 122); this.lblTargetPicker.Text = "◎"; this.lblTargetPicker.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblTargetDesc.AutoSize = true; this.lblTargetDesc.Font = new System.Drawing.Font("MS UI Gothic", 8.25F); this.lblTargetDesc.ForeColor = System.Drawing.SystemColors.ControlDarkDark; this.lblTargetDesc.Location = new System.Drawing.Point(12, 154); this.lblTargetDesc.Text = "※◎アイコンを起動中のゲーム画面にドラッグすると自動登録";
            
            this.chkNotifyVibration.AutoSize = true; this.chkNotifyVibration.Location = new System.Drawing.Point(14, 168); this.chkNotifyVibration.Text = "このプロファイルに切り替わった時、コントローラーを振動させる";

            this.btnOK.Location = new System.Drawing.Point(196, 188); this.btnOK.Size = new System.Drawing.Size(75, 23); this.btnOK.Text = "OK"; this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            this.btnCancel.Location = new System.Drawing.Point(277, 188); this.btnCancel.Size = new System.Drawing.Size(75, 23); this.btnCancel.Text = "キャンセル"; this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            this.lblStatus.AutoSize = true; this.lblStatus.Location = new System.Drawing.Point(12, 193);
            
            this.ClientSize = new System.Drawing.Size(364, 221);
            this.Controls.Add(this.chkNotifyVibration);
            this.Controls.Add(this.lblTargetDesc); this.Controls.Add(this.lblTargetPicker); this.Controls.Add(this.lblStatus); this.Controls.Add(this.btnCancel); this.Controls.Add(this.btnOK); this.Controls.Add(this.btnRemoveApp); this.Controls.Add(this.btnAddApp); this.Controls.Add(this.lstApps); this.Controls.Add(this.label2); this.Controls.Add(this.txtName); this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; this.MaximizeBox = false; this.MinimizeBox = false; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; this.Text = "プロファイル編集";
            this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
