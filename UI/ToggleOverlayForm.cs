using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace UsbInputMapper.UI
{
    public class ToggleOverlayForm : Form
    {
        private Timer _fadeTimer;
        private int _alpha = 255;
        private int _displayTicks = 0;
        private string _text;
        private bool _isOn;

        // ★ フォーカス非奪取設定
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        private ToggleOverlayForm(string text, bool isOn)
        {
            _text = text;
            _isOn = isOn;

            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            
            SetLayeredWindowAttributes(this.Handle, 0, 255, LWA_ALPHA);

            this.Size = new Size(320, 50);
            
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            
            // 画面中央下部に配置
            this.Location = new Point((screenWidth - this.Width) / 2, screenHeight - this.Height - 120);
            this.DoubleBuffered = true;

            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += FadeTimer_Tick;
            _fadeTimer.Start();
        }

        public static void ShowNotification(string text, bool isOn)
        {
            Task.Run(() => {
                try
                {
                    using (var frm = new ToggleOverlayForm(text, isOn)) {
                        Application.Run(frm);
                    }
                }
                catch { }
            });
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed) return;

            _displayTicks++;
            if (_displayTicks > 50) // 約0.8秒表示後にフェードアウト
            {
                _alpha -= 15;
                if (_alpha <= 0)
                {
                    _fadeTimer.Stop();
                    this.Close();
                    return;
                }
                SetLayeredWindowAttributes(this.Handle, 0, (byte)_alpha, LWA_ALPHA);
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.IsDisposed) return;
            base.OnPaint(e);
            
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, 20, 20, 20))) { e.Graphics.FillRectangle(bgBrush, rect); }

            Color accentColor = _isOn ? Color.LimeGreen : Color.Tomato;
            using (Pen borderPen = new Pen(accentColor, 2)) { e.Graphics.DrawRectangle(borderPen, 1, 1, this.Width - 2, this.Height - 2); }

            using (Font f = new Font("Meiryo", 13, FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                string stateStr = _isOn ? "[ ON ]" : "[ OFF ]";
                e.Graphics.DrawString($"{_text} {stateStr}", f, new SolidBrush(accentColor), rect, sf);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _fadeTimer != null) { _fadeTimer.Stop(); _fadeTimer.Dispose(); }
            base.Dispose(disposing);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const uint LWA_ALPHA = 0x2;
    }
}
