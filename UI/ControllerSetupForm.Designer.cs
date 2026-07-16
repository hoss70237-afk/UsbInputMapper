namespace UsbInputMapper.UI
{
    partial class ControllerSetupForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblInstruction;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.Button btnSkip;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblInstruction = new System.Windows.Forms.Label();
            this.lblProgress = new System.Windows.Forms.Label();
            this.btnSkip = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            
            this.lblInstruction.Font = new System.Drawing.Font("MS UI Gothic", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblInstruction.Location = new System.Drawing.Point(12, 40);
            this.lblInstruction.Name = "lblInstruction";
            this.lblInstruction.Size = new System.Drawing.Size(460, 60);
            this.lblInstruction.TabIndex = 0;
            this.lblInstruction.Text = "指示テキスト";
            this.lblInstruction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(12, 9);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(60, 12);
            this.lblProgress.TabIndex = 1;
            this.lblProgress.Text = "ステップ: 1/20";
            
            this.btnSkip.Location = new System.Drawing.Point(135, 120);
            this.btnSkip.Name = "btnSkip";
            this.btnSkip.Size = new System.Drawing.Size(100, 30);
            this.btnSkip.TabIndex = 2;
            this.btnSkip.Text = "スキップ";
            this.btnSkip.UseVisualStyleBackColor = true;
            this.btnSkip.Click += new System.EventHandler(this.btnSkip_Click);
            
            this.btnCancel.Location = new System.Drawing.Point(250, 120);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "キャンセル";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 171);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSkip);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.lblInstruction);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ControllerSetupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "XInput 一括セットアップウィザード";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ControllerSetupForm_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
