namespace UsbInputMapper.UI
{
    partial class CaptureForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnRadialMenuEdge;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label(); this.btnCancel = new System.Windows.Forms.Button(); this.btnRadialMenuEdge = new System.Windows.Forms.Button();
            this.SuspendLayout();
            
            this.label1.AutoSize = true; this.label1.Font = new System.Drawing.Font("MS UI Gothic", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128))); this.label1.Location = new System.Drawing.Point(20, 20); this.label1.Size = new System.Drawing.Size(206, 16); this.label1.Text = "デバイスの入力を待機しています...";
            
            this.btnCancel.Location = new System.Drawing.Point(20, 70); this.btnCancel.Size = new System.Drawing.Size(90, 23); this.btnCancel.Text = "キャンセル"; this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            
            this.btnRadialMenuEdge.Location = new System.Drawing.Point(120, 70); this.btnRadialMenuEdge.Size = new System.Drawing.Size(140, 23); this.btnRadialMenuEdge.Text = "ラジアルメニュー / ベゼル..."; this.btnRadialMenuEdge.Click += new System.EventHandler(this.btnRadialMenuEdge_Click);
            
            this.ClientSize = new System.Drawing.Size(284, 111);
            this.Controls.Add(this.btnRadialMenuEdge); this.Controls.Add(this.btnCancel); this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; this.Text = "入力待機"; this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CaptureForm_FormClosed); this.Load += new System.EventHandler(this.CaptureForm_Load);
            this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
