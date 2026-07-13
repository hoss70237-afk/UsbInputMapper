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
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
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
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numConditionParam)).BeginInit();
            this.SuspendLayout();
            // 
            // label0
            // 
            this.label0.AutoSize = true;
            this.label0.Location = new System.Drawing.Point(12, 15);
            this.label0.Name = "label0";
            this.label0.Size = new System.Drawing.Size(65, 12);
            this.label0.TabIndex = 0;
            this.label0.Text = "アイテム名:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(90, 12);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(220, 19);
            this.txtName.TabIndex = 1;
            // 
            // labelCond
            // 
            this.labelCond.AutoSize = true;
            this.labelCond.Location = new System.Drawing.Point(12, 50);
            this.labelCond.Name = "labelCond";
            this.labelCond.Size = new System.Drawing.Size(55, 12);
            this.labelCond.TabIndex = 2;
            this.labelCond.Text = "入力条件:";
            // 
            // cmbCondition
            // 
            this.cmbCondition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCondition.FormattingEnabled = true;
            this.cmbCondition.Location = new System.Drawing.Point(90, 47);
            this.cmbCondition.Name = "cmbCondition";
            this.cmbCondition.Size = new System.Drawing.Size(220, 20);
            this.cmbCondition.TabIndex = 3;
            this.cmbCondition.SelectedIndexChanged += new System.EventHandler(this.cmbCondition_SelectedIndexChanged);
            // 
            // lblParam
            // 
            this.lblParam.AutoSize = true;
            this.lblParam.Location = new System.Drawing.Point(12, 85);
            this.lblParam.Name = "lblParam";
            this.lblParam.Size = new System.Drawing.Size(58, 12);
            this.lblParam.TabIndex = 4;
            this.lblParam.Text = "パラメータ:";
            this.lblParam.Visible = false;
            // 
            // numConditionParam
            // 
            this.numConditionParam.Location = new System.Drawing.Point(90, 83);
            this.numConditionParam.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numConditionParam.Name = "numConditionParam";
            this.numConditionParam.Size = new System.Drawing.Size(100, 19);
            this.numConditionParam.TabIndex = 5;
            this.numConditionParam.Value = new decimal(new int[] { 500, 0, 0, 0 });
            this.numConditionParam.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 120);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 12);
            this.label1.TabIndex = 6;
            this.label1.Text = "エミュレート:";
            // 
            // cmbActionType
            // 
            this.cmbActionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbActionType.FormattingEnabled = true;
            this.cmbActionType.Location = new System.Drawing.Point(90, 117);
            this.cmbActionType.Name = "cmbActionType";
            this.cmbActionType.Size = new System.Drawing.Size(220, 20);
            this.cmbActionType.TabIndex = 7;
            this.cmbActionType.SelectedIndexChanged += new System.EventHandler(this.cmbActionType_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 155);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 12);
            this.label2.TabIndex = 8;
            this.label2.Text = "出力内容:";
            // 
            // cmbKeyButton
            // 
            this.cmbKeyButton.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbKeyButton.FormattingEnabled = true;
            this.cmbKeyButton.Location = new System.Drawing.Point(90, 152);
            this.cmbKeyButton.Name = "cmbKeyButton";
            this.cmbKeyButton.Size = new System.Drawing.Size(220, 20);
            this.cmbKeyButton.TabIndex = 9;
            // 
            // txtAppPath
            // 
            this.txtAppPath.Location = new System.Drawing.Point(90, 152);
            this.txtAppPath.Name = "txtAppPath";
            this.txtAppPath.Size = new System.Drawing.Size(180, 19);
            this.txtAppPath.TabIndex = 10;
            this.txtAppPath.Visible = false;
            // 
            // btnBrowseApp
            // 
            this.btnBrowseApp.Location = new System.Drawing.Point(276, 150);
            this.btnBrowseApp.Name = "btnBrowseApp";
            this.btnBrowseApp.Size = new System.Drawing.Size(34, 23);
            this.btnBrowseApp.TabIndex = 11;
            this.btnBrowseApp.Text = "...";
            this.btnBrowseApp.UseVisualStyleBackColor = true;
            this.btnBrowseApp.Visible = false;
            this.btnBrowseApp.Click += new System.EventHandler(this.btnBrowseApp_Click);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(154, 200);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 12;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(235, 200);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 13;
            this.btnCancel.Text = "キャンセル";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // BindingEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(334, 236);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnBrowseApp);
            this.Controls.Add(this.txtAppPath);
            this.Controls.Add(this.cmbKeyButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbActionType);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numConditionParam);
            this.Controls.Add(this.lblParam);
            this.Controls.Add(this.cmbCondition);
            this.Controls.Add(this.labelCond);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.label0);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BindingEditorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "入力アイテムの編集";
            ((System.ComponentModel.ISupportInitialize)(this.numConditionParam)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
