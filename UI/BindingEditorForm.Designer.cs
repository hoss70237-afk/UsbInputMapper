namespace UsbInputMapper.UI
{
    partial class BindingEditorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label label0;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label labelCond;
        private System.Windows.Forms.ComboBox cmbCondition;
        private System.Windows.Forms.Label lblParam;
        private System.Windows.Forms.NumericUpDown numConditionParam;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbActionType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbKeyButton;
        private System.Windows.Forms.TextBox txtAppPath;
        private System.Windows.Forms.Button btnBrowseApp;
        
        // --- 新規追加コントロール ---
        private System.Windows.Forms.Panel pnlMouseMove;
        private System.Windows.Forms.Label lblMouseX;
        private System.Windows.Forms.NumericUpDown numMouseX;
        private System.Windows.Forms.Label lblMouseY;
        private System.Windows.Forms.NumericUpDown numMouseY;
        private System.Windows.Forms.CheckBox chkAbsolute;
        private System.Windows.Forms.Button btnEditMacro;
        
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.label0 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.labelCond = new System.Windows.Forms.Label();
            this.cmbCondition = new System.Windows.Forms.ComboBox();
            this.lblParam = new System.Windows.Forms.Label();
            this.numConditionParam = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbActionType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbKeyButton = new System.Windows.Forms.ComboBox();
            this.txtAppPath = new System.Windows.Forms.TextBox();
            this.btnBrowseApp = new System.Windows.Forms.Button();
            
            this.pnlMouseMove = new System.Windows.Forms.Panel();
            this.lblMouseX = new System.Windows.Forms.Label();
            this.numMouseX = new System.Windows.Forms.NumericUpDown();
            this.lblMouseY = new System.Windows.Forms.Label();
            this.numMouseY = new System.Windows.Forms.NumericUpDown();
            this.chkAbsolute = new System.Windows.Forms.CheckBox();
            this.btnEditMacro = new System.Windows.Forms.Button();
            
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            
            ((System.ComponentModel.ISupportInitialize)(this.numConditionParam)).BeginInit();
            this.pnlMouseMove.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMouseX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMouseY)).BeginInit();
            this.SuspendLayout();
            
            // (既存部品の配置)
            this.label0.AutoSize = true; this.label0.Location = new System.Drawing.Point(12, 15); this.label0.Text = "アイテム名:";
            this.txtName.Location = new System.Drawing.Point(90, 12); this.txtName.Size = new System.Drawing.Size(220, 19);
            
            this.labelCond.AutoSize = true; this.labelCond.Location = new System.Drawing.Point(12, 50); this.labelCond.Text = "入力条件:";
            this.cmbCondition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCondition.Location = new System.Drawing.Point(90, 47); this.cmbCondition.Size = new System.Drawing.Size(220, 20);
            this.cmbCondition.SelectedIndexChanged += new System.EventHandler(this.cmbCondition_SelectedIndexChanged);
            
            this.lblParam.AutoSize = true; this.lblParam.Location = new System.Drawing.Point(12, 85); this.lblParam.Text = "パラメータ:"; this.lblParam.Visible = false;
            this.numConditionParam.Location = new System.Drawing.Point(90, 83); this.numConditionParam.Maximum = new decimal(new int[] { 100000, 0, 0, 0 }); this.numConditionParam.Size = new System.Drawing.Size(100, 19); this.numConditionParam.Visible = false;
            
            this.label1.AutoSize = true; this.label1.Location = new System.Drawing.Point(12, 120); this.label1.Text = "エミュレート:";
            this.cmbActionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbActionType.Location = new System.Drawing.Point(90, 117); this.cmbActionType.Size = new System.Drawing.Size(220, 20);
            this.cmbActionType.SelectedIndexChanged += new System.EventHandler(this.cmbActionType_SelectedIndexChanged);
            
            this.label2.AutoSize = true; this.label2.Location = new System.Drawing.Point(12, 155); this.label2.Text = "出力内容:";
            this.cmbKeyButton.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbKeyButton.Location = new System.Drawing.Point(90, 152); this.cmbKeyButton.Size = new System.Drawing.Size(220, 20);
            
            this.txtAppPath.Location = new System.Drawing.Point(90, 152); this.txtAppPath.Size = new System.Drawing.Size(180, 19); this.txtAppPath.Visible = false;
            this.btnBrowseApp.Location = new System.Drawing.Point(276, 150); this.btnBrowseApp.Size = new System.Drawing.Size(34, 23); this.btnBrowseApp.Text = "..."; this.btnBrowseApp.Visible = false; this.btnBrowseApp.Click += new System.EventHandler(this.btnBrowseApp_Click);
            
            // --- マウス座標パネル ---
            this.pnlMouseMove.Location = new System.Drawing.Point(90, 150); this.pnlMouseMove.Size = new System.Drawing.Size(220, 50); this.pnlMouseMove.Visible = false;
            
            this.lblMouseX.AutoSize = true; this.lblMouseX.Location = new System.Drawing.Point(0, 5); this.lblMouseX.Text = "X:";
            this.numMouseX.Location = new System.Drawing.Point(20, 3); this.numMouseX.Minimum = new decimal(new int[] { 9999, 0, 0, -2147483648 }); this.numMouseX.Maximum = new decimal(new int[] { 9999, 0, 0, 0 }); this.numMouseX.Size = new System.Drawing.Size(60, 19);
            
            this.lblMouseY.AutoSize = true; this.lblMouseY.Location = new System.Drawing.Point(90, 5); this.lblMouseY.Text = "Y:";
            this.numMouseY.Location = new System.Drawing.Point(110, 3); this.numMouseY.Minimum = new decimal(new int[] { 9999, 0, 0, -2147483648 }); this.numMouseY.Maximum = new decimal(new int[] { 9999, 0, 0, 0 }); this.numMouseY.Size = new System.Drawing.Size(60, 19);
            
            this.chkAbsolute.AutoSize = true; this.chkAbsolute.Location = new System.Drawing.Point(0, 28); this.chkAbsolute.Text = "画面の絶対座標に移動する";
            
            this.pnlMouseMove.Controls.Add(this.lblMouseX); this.pnlMouseMove.Controls.Add(this.numMouseX);
            this.pnlMouseMove.Controls.Add(this.lblMouseY); this.pnlMouseMove.Controls.Add(this.numMouseY);
            this.pnlMouseMove.Controls.Add(this.chkAbsolute);
            
            // --- マクロボタン ---
            this.btnEditMacro.Location = new System.Drawing.Point(90, 150); this.btnEditMacro.Size = new System.Drawing.Size(220, 23); this.btnEditMacro.Text = "マクロエディタを開く"; this.btnEditMacro.Visible = false; this.btnEditMacro.Click += new System.EventHandler(this.btnEditMacro_Click);
            
            this.btnOK.Location = new System.Drawing.Point(154, 210); this.btnOK.Size = new System.Drawing.Size(75, 23); this.btnOK.Text = "OK"; this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            this.btnCancel.Location = new System.Drawing.Point(235, 210); this.btnCancel.Size = new System.Drawing.Size(75, 23); this.btnCancel.Text = "キャンセル"; this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            
            this.ClientSize = new System.Drawing.Size(334, 246);
            this.Controls.Add(this.btnCancel); this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnEditMacro); this.Controls.Add(this.pnlMouseMove);
            this.Controls.Add(this.btnBrowseApp); this.Controls.Add(this.txtAppPath); this.Controls.Add(this.cmbKeyButton);
            this.Controls.Add(this.label2); this.Controls.Add(this.cmbActionType); this.Controls.Add(this.label1);
            this.Controls.Add(this.numConditionParam); this.Controls.Add(this.lblParam);
            this.Controls.Add(this.cmbCondition); this.Controls.Add(this.labelCond);
            this.Controls.Add(this.txtName); this.Controls.Add(this.label0);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Text = "入力アイテムの編集";
            
            ((System.ComponentModel.ISupportInitialize)(this.numConditionParam)).EndInit();
            this.pnlMouseMove.ResumeLayout(false); this.pnlMouseMove.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMouseX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMouseY)).EndInit();
            this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
