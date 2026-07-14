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
            if (CurrentInstance == this) CurrentInstance = null;
        }

        // RawInputManagerから呼ばれるメソッド
        public void ProcessInput(InputEvent e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ProcessInput(e)));
                return;
            }

            // 押された瞬間のみをキャプチャし、離された時の信号は無視して確実に入力を捉える
            if (e.IsKeyDown)
            {
                CapturedEvent = e;
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
