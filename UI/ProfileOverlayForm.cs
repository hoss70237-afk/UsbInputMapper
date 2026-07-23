using System;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public class ProfileOverlayForm : Form
    {
        private Timer _fadeTimer;
        private int _alpha = 0;
        private bool _isFadingOut = false;
        private Profile _profile;

        public ProfileOverlayForm(Profile profile)
        {
            _profile = profile;
            
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            
            // クリックを透過する拡張スタイル
            int initialStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, initialStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            SetLayeredWindowAttributes(this.Handle, 0, 0, LWA_ALPHA);

            this.Size = new Size(300, 60);
            
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            
            int x = profile.OverlayPosX >= 0 ? profile.OverlayPosX : screenWidth - this.Width - 20;
            int y = profile.OverlayPosY >= 0 ? profile.OverlayPosY : 20;
            
            // 画面外にはみ出ないように補正
            if (x + this.Width > screenWidth) x = screenWidth - this.Width;
            if (y + this.Height > screenHeight) y = screenHeight - this.Height;
            
            this.Location = new Point(x, y);

            this.DoubleBuffered = true;

            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += FadeTimer_Tick;
            _fadeTimer.Start();
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            if (!_isFadingOut)
            {
                _alpha += 25;
                if (_alpha >= 200) // 最大不透明度
                {
                    _alpha = 200;
                    _isFadingOut = true;
                    _fadeTimer.Interval = _profile.OverlayDurationMs; // 指定時間待機
                }
            }
            else
            {
                _fadeTimer.Interval = 16;
                _alpha -= 15;
                if (_alpha <= 0)
                {
                    _fadeTimer.Stop();
                    this.Close();
                    return;
                }
            }
            SetLayeredWindowAttributes(this.Handle, 0, (byte)_alpha, LWA_ALPHA);
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(bgBrush, rect);
            }
            
            using (Pen borderPen = new Pen(Color.DodgerBlue, 2))
            {
                e.Graphics.DrawRectangle(borderPen, 1, 1, this.Width - 2, this.Height - 2);
            }

            int textX = 10;
            
            if (_profile.OverlayShowMark)
            {
                using (Font markFont = new Font("MS UI Gothic", 16, FontStyle.Bold))
                {
                    e.Graphics.DrawString("🎮", markFont, Brushes.White, textX, 15);
                }
                textX += 40;
            }

            if (_profile.OverlayShowName)
            {
                using (Font nameFont = new Font("Meiryo", 12, FontStyle.Bold))
                {
                    e.Graphics.DrawString($"Profile: {_profile.Name}", nameFont, Brushes.White, textX, 15);
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const uint LWA_ALPHA = 0x2;
    }
}
