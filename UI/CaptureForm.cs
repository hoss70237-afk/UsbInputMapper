using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public enum CaptureMode { SingleAny, MultiKeyboard }

    public partial class CaptureForm : Form
    {
        public static bool IsCapturing { get; private set; }
        public static CaptureForm CurrentInstance { get; private set; }
        
        public CaptureMode Mode { get; set; }
        public InputEvent CapturedEvent { get; private set; }
        public List<int> CapturedKeys { get; private set; } = new List<int>();

        private int _downCount = 0;

        public CaptureForm(CaptureMode mode = CaptureMode.SingleAny)
        {
            InitializeComponent();
            Mode = mode;
            if (Mode == CaptureMode.MultiKeyboard)
            {
                label1.Text = "キーボードのキーを押してください。\r\nすべてのキーを離すと確定します。";
            }
        }

        private void CaptureForm_Load(object sender, EventArgs e)
        {
            IsCapturing = true;
            CurrentInstance = this;
        }

        private void CaptureForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            IsCapturing = false;
            if (CurrentInstance == this) CurrentInstance = null;
        }

        public void ProcessInput(InputEvent e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ProcessInput(e)));
                return;
            }

            if (Mode == CaptureMode.SingleAny)
            {
                if (e.IsKeyDown)
                {
                    CapturedEvent = e;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            else if (Mode == CaptureMode.MultiKeyboard)
            {
                if (e.Type == 1) // Keyboardのみ
                {
                    if (e.IsKeyDown)
                    {
                        if (!CapturedKeys.Contains(e.VKey)) CapturedKeys.Add(e.VKey);
                        _downCount++;
                        string keysStr = string.Join(" + ", CapturedKeys.Select(k => ((Keys)k).ToString()));
                        label1.Text = $"取得中: {keysStr}";
                    }
                    else
                    {
                        _downCount--;
                        if (_downCount <= 0 && CapturedKeys.Count > 0)
                        {
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
