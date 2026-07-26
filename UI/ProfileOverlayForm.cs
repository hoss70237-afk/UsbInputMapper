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

        private Brush _bgBrush;
        private Pen _borderPen;
        private Font _markFont;
        private Font _nameFont;

        // ★ ゲーム等のアクティブウィンドウからフォーカスを奪わない設定
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
            }
        }

        public ProfileOverlayForm(Profile profile)
        {
            _profile = profile;
            
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            
            SetLayeredWindowAttributes(this.Handle, 0, 0, LWA_ALPHA);

            this.Size = new Size(300, 60);
            
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            
            int x = profile.OverlayPosX >= 0 ? profile.OverlayPosX : screenWidth - this.Width - 20;
            int y = profile.OverlayPosY >= 0 ? profile.OverlayPosY : 20;
            
            if (x + this.Width > screenWidth) x = screenWidth - this.Width;
            if (y + this.Height > screenHeight) y = screenHeight - this.Height;
            
            this.Location = new Point(x, y);
            this.DoubleBuffered = true;

            _bgBrush = new SolidBrush(Color.FromArgb(180, 20, 20, 20));
            _borderPen = new Pen(Color.DodgerBlue, 2);
            _markFont = new Font("MS UI Gothic", 16, FontStyle.Bold);
            _nameFont = new Font("Meiryo", 12, FontStyle.Bold);

            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += FadeTimer_Tick;
            _fadeTimer.Start();
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed)
            {
                _fadeTimer?.Stop();
                return;
            }

            try
            {
                if (!_isFadingOut)
                {
                    _alpha += 25;
                    if (_alpha >= 220) 
                    {
                        _alpha = 220;
                        _isFadingOut = true;
                        _fadeTimer.Interval = Math.Max(500, _profile.OverlayDurationMs); 
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
            catch
            {
                _fadeTimer?.Stop();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.IsDisposed) return;
            base.OnPaint(e);
            
            try
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                e.Graphics.FillRectangle(_bgBrush, rect);
                e.Graphics.DrawRectangle(_borderPen, 1, 1, this.Width - 2, this.Height - 2);

                int textX = 15;
                
                if (_profile.OverlayShowMark)
                {
                    e.Graphics.DrawString("🎮", _markFont, Brushes.DodgerBlue, textX, 15);
                    textX += 35;
                }

                if (_profile.OverlayShowName)
                {
                    e.Graphics.DrawString($"Profile: {_profile.Name}", _nameFont, Brushes.White, textX, 16);
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_fadeTimer != null)
                {
                    _fadeTimer.Stop();
                    _fadeTimer.Dispose();
                }
                _bgBrush?.Dispose();
                _borderPen?.Dispose();
                _markFont?.Dispose();
                _nameFont?.Dispose();
            }
            base.Dispose(disposing);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const uint LWA_ALPHA = 0x2;
    }
}
