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
            // メインスレッドでUI操作と状態保存を行う
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ProcessInput(evt)));
                return;
            }

            // 何らかのキー・ボタンが押されたものを対象とする
            if (evt.Type == 1 && evt.IsKeyDown)
            {
                CapturedEvent = evt;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else if (evt.Type == 0 && evt.MouseButtonFlags > 0)
            {
                CapturedEvent = evt;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            // HIDの場合は条件を別途定義
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
