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

        private ToggleOverlayForm(string text, bool isOn)
        {
            _text = text;
            _isOn = isOn;

            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            
            int initialStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, initialStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW);
            SetLayeredWindowAttributes(this.Handle, 0, 255, LWA_ALPHA);

            this.Size = new Size(300, 50);
            
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            
            // 画面中央下部に表示
            this.Location = new Point((screenWidth - this.Width) / 2, screenHeight - this.Height - 150);
            this.DoubleBuffered = true;

            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += FadeTimer_Tick;
            _fadeTimer.Start();
        }

        public static void ShowNotification(string text, bool isOn)
        {
            // 独立したUIスレッドでフォームを生成・表示（メインスレッドを阻害しない）
            Task.Run(() => {
                using (var frm = new ToggleOverlayForm(text, isOn)) {
                    frm.ShowDialog();
                }
            });
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed) return;

            _displayTicks++;
            if (_displayTicks > 60) // 約1秒間表示
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
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0))) { e.Graphics.FillRectangle(bgBrush, rect); }

            Color accentColor = _isOn ? Color.LimeGreen : Color.Tomato;
            using (Pen borderPen = new Pen(accentColor, 2)) { e.Graphics.DrawRectangle(borderPen, 1, 1, this.Width - 2, this.Height - 2); }

            using (Font f = new Font("Meiryo", 14, FontStyle.Bold))
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

        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const uint LWA_ALPHA = 0x2;
    }
}
