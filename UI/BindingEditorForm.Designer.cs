namespace UsbInputMapper.UI
{
    partial class BindingEditorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label label0;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.CheckBox chkBlockOriginalInput;
        private System.Windows.Forms.Label lblMainTrigger;
        private System.Windows.Forms.Button btnReCaptureMain;
        private System.Windows.Forms.Button btnReflectName;
        private System.Windows.Forms.Label lblSubTriggers;
        private System.Windows.Forms.ListBox lstSubTriggers;
        private System.Windows.Forms.Button btnAddSubTrigger;
        private System.Windows.Forms.Button btnRemoveSubTrigger;
        private System.Windows.Forms.ComboBox cmbManualSubTrigger;
        private System.Windows.Forms.Button btnManualAddSub;
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
        private System.Windows.Forms.Label lblAppArgs; 
        private System.Windows.Forms.TextBox txtAppArgs;
        private System.Windows.Forms.Panel pnlMouseMove;
        private System.Windows.Forms.Label lblMouseX;
        private System.Windows.Forms.NumericUpDown numMouseX;
        private System.Windows.Forms.Label lblMouseY;
        private System.Windows.Forms.NumericUpDown numMouseY;
        private System.Windows.Forms.Button btnCaptureCoord;
        private System.Windows.Forms.ComboBox cmbProfileSwitchTarget;
        private System.Windows.Forms.ComboBox cmbProfileSwitchMode;
        private System.Windows.Forms.Button btnEditMacro;
        
        private System.Windows.Forms.Panel pnlBackground;
        private System.Windows.Forms.TextBox txtBgClassName;
        private System.Windows.Forms.TextBox txtBgWindowName;
        private System.Windows.Forms.NumericUpDown numBgControlId;
        private System.Windows.Forms.ComboBox cmbBgAction;
        private System.Windows.Forms.ComboBox cmbBgKey;
        private System.Windows.Forms.Label lblBgPicker;

        private System.Windows.Forms.Panel pnlVibration;
        private System.Windows.Forms.CheckBox chkVibrate;
        private System.Windows.Forms.NumericUpDown numVibrateDuration;
        private System.Windows.Forms.NumericUpDown numVibrateTimes;
        
        private System.Windows.Forms.Label lblWav;
        private System.Windows.Forms.TextBox txtWavPath;
        private System.Windows.Forms.Button btnBrowseWav;
        
        // ★追加UI
        private System.Windows.Forms.ComboBox cmbCursorVis;
        private System.Windows.Forms.Panel pnlCursorOffset;
        private System.Windows.Forms.NumericUpDown numOffsetX;
        private System.Windows.Forms.NumericUpDown numOffsetY;
        private System.Windows.Forms.Panel pnlSysMouse;
        private System.Windows.Forms.NumericUpDown numSysMouseSpd;
        private System.Windows.Forms.NumericUpDown numSysScroll;

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private void InitializeComponent()
        {
            this.label0 = new System.Windows.Forms.Label(); this.txtName = new System.Windows.Forms.TextBox(); this.chkBlockOriginalInput = new System.Windows.Forms.CheckBox();
            this.lblMainTrigger = new System.Windows.Forms.Label(); this.btnReCaptureMain = new System.Windows.Forms.Button(); this.btnReflectName = new System.Windows.Forms.Button();
            this.lblSubTriggers = new System.Windows.Forms.Label(); this.lstSubTriggers = new System.Windows.Forms.ListBox(); this.btnAddSubTrigger = new System.Windows.Forms.Button(); this.btnRemoveSubTrigger = new System.Windows.Forms.Button();
            this.cmbManualSubTrigger = new System.Windows.Forms.ComboBox(); this.btnManualAddSub = new System.Windows.Forms.Button();
            this.labelCond = new System.Windows.Forms.Label(); this.cmbCondition = new System.Windows.Forms.ComboBox(); this.lblParam = new System.Windows.Forms.Label(); this.numConditionParam = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label(); this.cmbActionType = new System.Windows.Forms.ComboBox(); this.label2 = new System.Windows.Forms.Label(); this.cmbKeyButton = new System.Windows.Forms.ComboBox();
            this.txtAppPath = new System.Windows.Forms.TextBox(); this.btnBrowseApp = new System.Windows.Forms.Button(); this.lblAppArgs = new System.Windows.Forms.Label(); this.txtAppArgs = new System.Windows.Forms.TextBox();
            this.pnlMouseMove = new System.Windows.Forms.Panel(); this.lblMouseX = new System.Windows.Forms.Label(); this.numMouseX = new System.Windows.Forms.NumericUpDown(); this.lblMouseY = new System.Windows.Forms.Label(); this.numMouseY = new System.Windows.Forms.NumericUpDown(); this.btnCaptureCoord = new System.Windows.Forms.Button();
            this.cmbProfileSwitchTarget = new System.Windows.Forms.ComboBox(); this.cmbProfileSwitchMode = new System.Windows.Forms.ComboBox(); this.btnEditMacro = new System.Windows.Forms.Button();
            
            this.pnlBackground = new System.Windows.Forms.Panel(); this.txtBgClassName = new System.Windows.Forms.TextBox(); this.txtBgWindowName = new System.Windows.Forms.TextBox(); this.numBgControlId = new System.Windows.Forms.NumericUpDown(); this.cmbBgAction = new System.Windows.Forms.ComboBox(); this.cmbBgKey = new System.Windows.Forms.ComboBox(); this.lblBgPicker = new System.Windows.Forms.Label();
            this.pnlVibration = new System.Windows.Forms.Panel(); this.chkVibrate = new System.Windows.Forms.CheckBox(); this.numVibrateDuration = new System.Windows.Forms.NumericUpDown(); this.numVibrateTimes = new System.Windows.Forms.NumericUpDown();
            
            this.lblWav = new System.Windows.Forms.Label(); this.txtWavPath = new System.Windows.Forms.TextBox(); this.btnBrowseWav = new System.Windows.Forms.Button();
            
            this.cmbCursorVis = new System.Windows.Forms.ComboBox();
            this.pnlCursorOffset = new System.Windows.Forms.Panel(); this.numOffsetX = new System.Windows.Forms.NumericUpDown(); this.numOffsetY = new System.Windows.Forms.NumericUpDown();
            this.pnlSysMouse = new System.Windows.Forms.Panel(); this.numSysMouseSpd = new System.Windows.Forms.NumericUpDown(); this.numSysScroll = new System.Windows.Forms.NumericUpDown();

            this.btnOK = new System.Windows.Forms.Button(); this.btnCancel = new System.Windows.Forms.Button();
            
            ((System.ComponentModel.ISupportInitialize)(this.numConditionParam)).BeginInit(); this.pnlMouseMove.SuspendLayout(); ((System.ComponentModel.ISupportInitialize)(this.numMouseX)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numMouseY)).BeginInit(); 
            this.pnlBackground.SuspendLayout(); ((System.ComponentModel.ISupportInitialize)(this.numBgControlId)).BeginInit();
            this.pnlVibration.SuspendLayout(); ((System.ComponentModel.ISupportInitialize)(this.numVibrateDuration)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numVibrateTimes)).BeginInit();
            this.pnlCursorOffset.SuspendLayout(); ((System.ComponentModel.ISupportInitialize)(this.numOffsetX)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numOffsetY)).BeginInit();
            this.pnlSysMouse.SuspendLayout(); ((System.ComponentModel.ISupportInitialize)(this.numSysMouseSpd)).BeginInit(); ((System.ComponentModel.ISupportInitialize)(this.numSysScroll)).BeginInit();
            this.SuspendLayout();
            
            this.label0.AutoSize = true; this.label0.Location = new System.Drawing.Point(12, 15); this.label0.Text = "アイテム名:"; this.txtName.Location = new System.Drawing.Point(90, 12); this.txtName.Size = new System.Drawing.Size(150, 19);
            this.chkBlockOriginalInput.AutoSize = true; this.chkBlockOriginalInput.Location = new System.Drawing.Point(245, 14); this.chkBlockOriginalInput.Text = "本来の入力をブロック";
            this.lblMainTrigger.AutoSize = true; this.lblMainTrigger.Location = new System.Drawing.Point(12, 45); this.lblMainTrigger.Text = "メイン入力: -"; 
            this.btnReflectName.Location = new System.Drawing.Point(170, 40); this.btnReflectName.Size = new System.Drawing.Size(85, 23); this.btnReflectName.Text = "アイテム名に反映"; this.btnReflectName.Click += new System.EventHandler(this.btnReflectName_Click);
            this.btnReCaptureMain.Location = new System.Drawing.Point(260, 40); this.btnReCaptureMain.Size = new System.Drawing.Size(70, 23); this.btnReCaptureMain.Text = "再登録"; this.btnReCaptureMain.Click += new System.EventHandler(this.btnReCaptureMain_Click);
            this.lblSubTriggers.AutoSize = true; this.lblSubTriggers.Location = new System.Drawing.Point(12, 75); this.lblSubTriggers.Text = "同時押し:"; this.lstSubTriggers.Location = new System.Drawing.Point(90, 75); this.lstSubTriggers.Size = new System.Drawing.Size(160, 52);
            this.btnAddSubTrigger.Location = new System.Drawing.Point(260, 75); this.btnAddSubTrigger.Size = new System.Drawing.Size(70, 23); this.btnAddSubTrigger.Text = "追加(待機)"; this.btnAddSubTrigger.Click += new System.EventHandler(this.btnAddSubTrigger_Click);
            this.btnRemoveSubTrigger.Location = new System.Drawing.Point(260, 131); this.btnRemoveSubTrigger.Size = new System.Drawing.Size(70, 23); this.btnRemoveSubTrigger.Text = "削除"; this.btnRemoveSubTrigger.Click += new System.EventHandler(this.btnRemoveSubTrigger_Click);
            this.cmbManualSubTrigger.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbManualSubTrigger.Location = new System.Drawing.Point(90, 133); this.cmbManualSubTrigger.Size = new System.Drawing.Size(100, 20); this.btnManualAddSub.Location = new System.Drawing.Point(195, 131); this.btnManualAddSub.Size = new System.Drawing.Size(55, 23); this.btnManualAddSub.Text = "手動"; this.btnManualAddSub.Click += new System.EventHandler(this.btnManualAddSub_Click);
            this.labelCond.AutoSize = true; this.labelCond.Location = new System.Drawing.Point(12, 170); this.labelCond.Text = "入力条件:"; this.cmbCondition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbCondition.Location = new System.Drawing.Point(90, 167); this.cmbCondition.Size = new System.Drawing.Size(240, 20); this.cmbCondition.SelectedIndexChanged += new System.EventHandler(this.cmbCondition_SelectedIndexChanged);
            this.lblParam.AutoSize = true; this.lblParam.Location = new System.Drawing.Point(12, 200); this.lblParam.Text = "パラメータ:"; this.lblParam.Visible = false; this.numConditionParam.Location = new System.Drawing.Point(90, 198); this.numConditionParam.Maximum = new decimal(new int[] { 100000, 0, 0, 0 }); this.numConditionParam.Size = new System.Drawing.Size(100, 19); this.numConditionParam.Visible = false;
            this.label1.AutoSize = true; this.label1.Location = new System.Drawing.Point(12, 230); this.label1.Text = "エミュレート:"; this.cmbActionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbActionType.Location = new System.Drawing.Point(90, 227); this.cmbActionType.Size = new System.Drawing.Size(240, 20); this.cmbActionType.SelectedIndexChanged += new System.EventHandler(this.cmbActionType_SelectedIndexChanged);
            this.label2.AutoSize = true; this.label2.Location = new System.Drawing.Point(12, 260); this.label2.Text = "出力内容:"; this.cmbKeyButton.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbKeyButton.Location = new System.Drawing.Point(90, 257); this.cmbKeyButton.Size = new System.Drawing.Size(240, 20); this.cmbKeyButton.SelectedIndexChanged += new System.EventHandler(this.cmbKeyButton_SelectedIndexChanged);
            
            this.txtAppPath.Location = new System.Drawing.Point(90, 257); this.txtAppPath.Size = new System.Drawing.Size(200, 19); this.txtAppPath.Visible = false; this.btnBrowseApp.Location = new System.Drawing.Point(296, 255); this.btnBrowseApp.Size = new System.Drawing.Size(34, 23); this.btnBrowseApp.Text = "..."; this.btnBrowseApp.Visible = false; this.btnBrowseApp.Click += new System.EventHandler(this.btnBrowseApp_Click);
            this.lblAppArgs.AutoSize = true; this.lblAppArgs.Location = new System.Drawing.Point(12, 285); this.lblAppArgs.Text = "引数:"; this.lblAppArgs.Visible = false;
            this.txtAppArgs.Location = new System.Drawing.Point(90, 282); this.txtAppArgs.Size = new System.Drawing.Size(240, 19); this.txtAppArgs.Visible = false;

            this.pnlMouseMove.Location = new System.Drawing.Point(90, 255); this.pnlMouseMove.Size = new System.Drawing.Size(360, 30); this.pnlMouseMove.Visible = false;
            this.lblMouseX.AutoSize = true; this.lblMouseX.Location = new System.Drawing.Point(0, 5); this.lblMouseX.Text = "X:"; this.numMouseX.Location = new System.Drawing.Point(15, 3); this.numMouseX.Minimum = new decimal(new int[] { 9999, 0, 0, -2147483648 }); this.numMouseX.Maximum = new decimal(new int[] { 9999, 0, 0, 0 }); this.numMouseX.Size = new System.Drawing.Size(55, 19);
            this.lblMouseY.AutoSize = true; this.lblMouseY.Location = new System.Drawing.Point(75, 5); this.lblMouseY.Text = "Y:"; this.numMouseY.Location = new System.Drawing.Point(90, 3); this.numMouseY.Minimum = new decimal(new int[] { 9999, 0, 0, -2147483648 }); this.numMouseY.Maximum = new decimal(new int[] { 9999, 0, 0, 0 }); this.numMouseY.Size = new System.Drawing.Size(55, 19);
            this.btnCaptureCoord.Location = new System.Drawing.Point(155, 1); this.btnCaptureCoord.Size = new System.Drawing.Size(195, 23); this.btnCaptureCoord.Text = "座標取得"; this.btnCaptureCoord.Click += new System.EventHandler(this.btnCaptureCoord_Click);
            this.pnlMouseMove.Controls.Add(this.lblMouseX); this.pnlMouseMove.Controls.Add(this.numMouseX); this.pnlMouseMove.Controls.Add(this.lblMouseY); this.pnlMouseMove.Controls.Add(this.numMouseY); this.pnlMouseMove.Controls.Add(this.btnCaptureCoord);
            
            this.cmbProfileSwitchTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbProfileSwitchTarget.Location = new System.Drawing.Point(90, 257); this.cmbProfileSwitchTarget.Size = new System.Drawing.Size(130, 20); this.cmbProfileSwitchTarget.Visible = false;
            this.cmbProfileSwitchMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbProfileSwitchMode.Location = new System.Drawing.Point(225, 257); this.cmbProfileSwitchMode.Size = new System.Drawing.Size(105, 20); this.cmbProfileSwitchMode.Visible = false;
            this.btnEditMacro.Location = new System.Drawing.Point(90, 255); this.btnEditMacro.Size = new System.Drawing.Size(240, 23); this.btnEditMacro.Text = "マクロエディタを開く"; this.btnEditMacro.Visible = false; this.btnEditMacro.Click += new System.EventHandler(this.btnEditMacro_Click);
            
            this.pnlBackground.Location = new System.Drawing.Point(90, 255); this.pnlBackground.Size = new System.Drawing.Size(360, 60); this.pnlBackground.Visible = false;
            this.lblBgPicker.AutoSize = true; this.lblBgPicker.Font = new System.Drawing.Font("MS UI Gothic", 16F); this.lblBgPicker.ForeColor = System.Drawing.Color.DodgerBlue; this.lblBgPicker.Location = new System.Drawing.Point(0, 5); this.lblBgPicker.Text = "◎"; this.lblBgPicker.Cursor = System.Windows.Forms.Cursors.Hand;
            this.txtBgClassName.Location = new System.Drawing.Point(30, 5); this.txtBgClassName.Size = new System.Drawing.Size(80, 19); this.txtBgWindowName.Location = new System.Drawing.Point(115, 5); this.txtBgWindowName.Size = new System.Drawing.Size(100, 19);
            this.numBgControlId.Location = new System.Drawing.Point(220, 5); this.numBgControlId.Maximum = 65535; this.numBgControlId.Size = new System.Drawing.Size(50, 19);
            this.cmbBgAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbBgAction.Location = new System.Drawing.Point(30, 30); this.cmbBgAction.Size = new System.Drawing.Size(80, 20); this.cmbBgAction.SelectedIndexChanged += new System.EventHandler(this.cmbBgAction_SelectedIndexChanged);
            this.cmbBgKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbBgKey.Location = new System.Drawing.Point(115, 30); this.cmbBgKey.Size = new System.Drawing.Size(100, 20); this.cmbBgKey.Visible = false;
            this.pnlBackground.Controls.Add(this.lblBgPicker); this.pnlBackground.Controls.Add(this.txtBgClassName); this.pnlBackground.Controls.Add(this.txtBgWindowName); this.pnlBackground.Controls.Add(this.numBgControlId); this.pnlBackground.Controls.Add(this.cmbBgAction); this.pnlBackground.Controls.Add(this.cmbBgKey);
            
            // ★追加UI群
            this.cmbCursorVis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; this.cmbCursorVis.Location = new System.Drawing.Point(90, 257); this.cmbCursorVis.Size = new System.Drawing.Size(120, 20); this.cmbCursorVis.Visible = false;
            
            this.pnlCursorOffset.Location = new System.Drawing.Point(90, 255); this.pnlCursorOffset.Size = new System.Drawing.Size(240, 30); this.pnlCursorOffset.Visible = false;
            System.Windows.Forms.Label lOffX = new System.Windows.Forms.Label { Text = "X補正:", Location = new System.Drawing.Point(0, 5), AutoSize = true };
            this.numOffsetX.Location = new System.Drawing.Point(40, 3); this.numOffsetX.Minimum = -9999; this.numOffsetX.Maximum = 9999; this.numOffsetX.Size = new System.Drawing.Size(50, 19);
            System.Windows.Forms.Label lOffY = new System.Windows.Forms.Label { Text = "Y補正:", Location = new System.Drawing.Point(100, 5), AutoSize = true };
            this.numOffsetY.Location = new System.Drawing.Point(140, 3); this.numOffsetY.Minimum = -9999; this.numOffsetY.Maximum = 9999; this.numOffsetY.Size = new System.Drawing.Size(50, 19);
            this.pnlCursorOffset.Controls.Add(lOffX); this.pnlCursorOffset.Controls.Add(this.numOffsetX); this.pnlCursorOffset.Controls.Add(lOffY); this.pnlCursorOffset.Controls.Add(this.numOffsetY);
            
            this.pnlSysMouse.Location = new System.Drawing.Point(90, 255); this.pnlSysMouse.Size = new System.Drawing.Size(240, 30); this.pnlSysMouse.Visible = false;
            System.Windows.Forms.Label lSpd = new System.Windows.Forms.Label { Text = "ポインタ速度:", Location = new System.Drawing.Point(0, 5), AutoSize = true };
            this.numSysMouseSpd.Location = new System.Drawing.Point(70, 3); this.numSysMouseSpd.Minimum = 1; this.numSysMouseSpd.Maximum = 20; this.numSysMouseSpd.Value = 10; this.numSysMouseSpd.Size = new System.Drawing.Size(40, 19);
            System.Windows.Forms.Label lScr = new System.Windows.Forms.Label { Text = "スクロール:", Location = new System.Drawing.Point(120, 5), AutoSize = true };
            this.numSysScroll.Location = new System.Drawing.Point(180, 3); this.numSysScroll.Minimum = 1; this.numSysScroll.Maximum = 100; this.numSysScroll.Value = 3; this.numSysScroll.Size = new System.Drawing.Size(40, 19);
            this.pnlSysMouse.Controls.Add(lSpd); this.pnlSysMouse.Controls.Add(this.numSysMouseSpd); this.pnlSysMouse.Controls.Add(lScr); this.pnlSysMouse.Controls.Add(this.numSysScroll);

            this.lblWav.AutoSize = true; this.lblWav.Location = new System.Drawing.Point(12, 365); this.lblWav.Text = "発動時WAV:";
            this.txtWavPath.Location = new System.Drawing.Point(90, 362); this.txtWavPath.Size = new System.Drawing.Size(200, 19);
            this.btnBrowseWav.Location = new System.Drawing.Point(296, 360); this.btnBrowseWav.Size = new System.Drawing.Size(34, 23); this.btnBrowseWav.Text = "..."; this.btnBrowseWav.Click += new System.EventHandler(this.btnBrowseWav_Click);

            this.pnlVibration.Location = new System.Drawing.Point(12, 385); this.pnlVibration.Size = new System.Drawing.Size(360, 30);
            this.chkVibrate.AutoSize = true; this.chkVibrate.Location = new System.Drawing.Point(0, 5); this.chkVibrate.Text = "実行時にコントローラー振動";
            System.Windows.Forms.Label lblV1 = new System.Windows.Forms.Label { Text = "時間(ms):", Location = new System.Drawing.Point(155, 6), AutoSize = true };
            this.numVibrateDuration.Location = new System.Drawing.Point(210, 4); this.numVibrateDuration.Maximum = 2000; this.numVibrateDuration.Size = new System.Drawing.Size(50, 19);
            System.Windows.Forms.Label lblV2 = new System.Windows.Forms.Label { Text = "回数:", Location = new System.Drawing.Point(265, 6), AutoSize = true };
            this.numVibrateTimes.Location = new System.Drawing.Point(300, 4); this.numVibrateTimes.Maximum = 10; this.numVibrateTimes.Size = new System.Drawing.Size(40, 19);
            this.pnlVibration.Controls.Add(this.chkVibrate); this.pnlVibration.Controls.Add(lblV1); this.pnlVibration.Controls.Add(this.numVibrateDuration); this.pnlVibration.Controls.Add(lblV2); this.pnlVibration.Controls.Add(this.numVibrateTimes);

            this.btnOK.Location = new System.Drawing.Point(264, 430); this.btnOK.Size = new System.Drawing.Size(75, 23); this.btnOK.Text = "OK"; this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            this.btnCancel.Location = new System.Drawing.Point(345, 430); this.btnCancel.Size = new System.Drawing.Size(75, 23); this.btnCancel.Text = "キャンセル"; this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            
            this.ClientSize = new System.Drawing.Size(460, 465);
            this.Controls.Add(this.chkBlockOriginalInput); this.Controls.Add(this.lblMainTrigger); this.Controls.Add(this.btnReflectName); this.Controls.Add(this.btnReCaptureMain);
            this.Controls.Add(this.lblSubTriggers); this.Controls.Add(this.lstSubTriggers); this.Controls.Add(this.btnAddSubTrigger); this.Controls.Add(this.btnRemoveSubTrigger);
            this.Controls.Add(this.cmbManualSubTrigger); this.Controls.Add(this.btnManualAddSub); this.Controls.Add(this.btnCancel); this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnEditMacro); this.Controls.Add(this.pnlMouseMove); this.Controls.Add(this.pnlBackground); this.Controls.Add(this.pnlVibration);
            
            // ★追加UI配置
            this.Controls.Add(this.cmbCursorVis); this.Controls.Add(this.pnlCursorOffset); this.Controls.Add(this.pnlSysMouse);

            this.Controls.Add(this.cmbProfileSwitchTarget); this.Controls.Add(this.cmbProfileSwitchMode);
            this.Controls.Add(this.lblAppArgs); this.Controls.Add(this.txtAppArgs);
            this.Controls.Add(this.btnBrowseApp); this.Controls.Add(this.txtAppPath); this.Controls.Add(this.cmbKeyButton);
            this.Controls.Add(this.label2); this.Controls.Add(this.cmbActionType); this.Controls.Add(this.label1);
            this.Controls.Add(this.numConditionParam); this.Controls.Add(this.lblParam); this.Controls.Add(this.cmbCondition); this.Controls.Add(this.labelCond);
            this.Controls.Add(this.lblWav); this.Controls.Add(this.txtWavPath); this.Controls.Add(this.btnBrowseWav);
            this.Controls.Add(this.txtName); this.Controls.Add(this.label0);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog; this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; this.Text = "入力アイテムの編集";
            
            ((System.ComponentModel.ISupportInitialize)(this.numConditionParam)).EndInit(); this.pnlMouseMove.ResumeLayout(false); this.pnlMouseMove.PerformLayout(); ((System.ComponentModel.ISupportInitialize)(this.numMouseX)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numMouseY)).EndInit(); 
            this.pnlBackground.ResumeLayout(false); this.pnlBackground.PerformLayout(); ((System.ComponentModel.ISupportInitialize)(this.numBgControlId)).EndInit();
            this.pnlVibration.ResumeLayout(false); this.pnlVibration.PerformLayout(); ((System.ComponentModel.ISupportInitialize)(this.numVibrateDuration)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numVibrateTimes)).EndInit();
            this.pnlCursorOffset.ResumeLayout(false); this.pnlCursorOffset.PerformLayout(); ((System.ComponentModel.ISupportInitialize)(this.numOffsetX)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numOffsetY)).EndInit();
            this.pnlSysMouse.ResumeLayout(false); this.pnlSysMouse.PerformLayout(); ((System.ComponentModel.ISupportInitialize)(this.numSysMouseSpd)).EndInit(); ((System.ComponentModel.ISupportInitialize)(this.numSysScroll)).EndInit();
            this.ResumeLayout(false); this.PerformLayout();
        }
    }
}
