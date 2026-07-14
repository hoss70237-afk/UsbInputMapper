using System;
using System.Windows.Forms;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public partial class CaptureForm : Form
    {
        public static bool IsCapturing { get; private set; }
        public static CaptureForm CurrentInstance { get; private set; }
        
        public InputEvent CapturedEvent { get; private set; }

        public CaptureForm()
        {
            InitializeComponent();
        }

        private void CaptureForm_Load(object sender, EventArgs e)
        {
            IsCapturing = true;
            CurrentInstance = this;
        }

        private void CaptureForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            IsCapturing = false;
            CurrentInstance = null;
        }

        public void ProcessInput(InputEvent evt)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ProcessInput(evt)));
                return;
            }

            // キーボード、マウス、特殊HIDボタンのダウンイベントを取得
            if (evt.IsKeyDown)
            {
                CapturedEvent = evt;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
