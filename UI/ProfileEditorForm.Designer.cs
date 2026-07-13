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

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

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
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "プロファイル名:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(87, 12);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(265, 19);
            this.txtName.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "対象アプリケーション:";
            // 
            // lstApps
            // 
            this.lstApps.FormattingEnabled = true;
            this.lstApps.ItemHeight = 12;
            this.lstApps.Location = new System.Drawing.Point(14, 63);
            this.lstApps.Name = "lstApps";
            this.lstApps.Size = new System.Drawing.Size(257, 88);
            this.lstApps.TabIndex = 3;
            // 
            // btnAddApp
            // 
            this.btnAddApp.Location = new System.Drawing.Point(277, 63);
            this.btnAddApp.Name = "btnAddApp";
            this.btnAddApp.Size = new System.Drawing.Size(75, 23);
            this.btnAddApp.TabIndex = 4;
            this.btnAddApp.Text = "参照追加";
            this.btnAddApp.UseVisualStyleBackColor = true;
            this.btnAddApp.Click += new System.EventHandler(this.btnAddApp_Click);
            // 
            // btnRemoveApp
            // 
            this.btnRemoveApp.Location = new System.Drawing.Point(277, 92);
            this.btnRemoveApp.Name = "btnRemoveApp";
            this.btnRemoveApp.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveApp.TabIndex = 5;
            this.btnRemoveApp.Text = "削除";
            this.btnRemoveApp.UseVisualStyleBackColor = true;
            this.btnRemoveApp.Click += new System.EventHandler(this.btnRemoveApp_Click);
            // 
            // lblTargetPicker
            // 
            this.lblTargetPicker.AutoSize = true;
            this.lblTargetPicker.Font = new System.Drawing.Font("MS UI Gothic", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblTargetPicker.ForeColor = System.Drawing.Color.DodgerBlue;
            this.lblTargetPicker.Location = new System.Drawing.Point(293, 122);
            this.lblTargetPicker.Name = "lblTargetPicker";
            this.lblTargetPicker.Size = new System.Drawing.Size(47, 33);
            this.lblTargetPicker.TabIndex = 9;
            this.lblTargetPicker.Text = "◎";
            this.lblTargetPicker.Cursor = System.Windows.Forms.Cursors.Hand;
            // 
            // lblTargetDesc
            // 
            this.lblTargetDesc.AutoSize = true;
            this.lblTargetDesc.Font = new System.Drawing.Font("MS UI Gothic", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblTargetDesc.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblTargetDesc.Location = new System.Drawing.Point(12, 154);
            this.lblTargetDesc.Name = "lblTargetDesc";
            this.lblTargetDesc.Size = new System.Drawing.Size(262, 11);
            this.lblTargetDesc.TabIndex = 10;
            this.lblTargetDesc.Text = "※◎アイコンを起動中のゲーム画面にドラッグすると自動登録";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(196, 178);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(277, 178);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "キャンセル";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 183);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 12);
            this.lblStatus.TabIndex = 8;
            // 
            // ProfileEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(364, 211);
            this.Controls.Add(this.lblTargetDesc);
            this.Controls.Add(this.lblTargetPicker);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnRemoveApp);
            this.Controls.Add(this.btnAddApp);
            this.Controls.Add(this.lstApps);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProfileEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "プロファイル編集";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
