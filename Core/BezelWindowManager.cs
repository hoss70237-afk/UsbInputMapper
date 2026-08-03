using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.Core
{
    public class BezelWindowManager
    {
        public static BezelWindowManager Instance { get; } = new BezelWindowManager();

        private List<BezelEdgeWindow> _windows = new List<BezelEdgeWindow>();
        public event EventHandler<int> OnBezelFired;

        private BezelWindowManager() { }

        public void UpdateBezelWindows(Profile profile)
        {
            foreach (var w in _windows)
            {
                w.Close();
                w.Dispose();
            }
            _windows.Clear();

            bool hasBezel = false;
            foreach (var b in profile.Bindings)
            {
                if (b.InputType == 5) { hasBezel = true; break; }
            }

            if (!hasBezel) return;

            foreach (var screen in Screen.AllScreens)
            {
                var b = screen.Bounds;

                bool hasLeftNeighbor = false;
                bool hasRightNeighbor = false;

                foreach (var other in Screen.AllScreens)
                {
                    if (other.DeviceName == screen.DeviceName) continue;
                    var ob = other.Bounds;
                    if (ob.Right == b.Left && ob.Bottom > b.Top && ob.Top < b.Bottom) hasLeftNeighbor = true;
                    if (ob.Left == b.Right && ob.Bottom > b.Top && ob.Top < b.Bottom) hasRightNeighbor = true;
                }

                // 太さを1ピクセルにし、座標を正確に設定
                var topWin = new BezelEdgeWindow(this, new Rectangle(b.Left, b.Top, b.Width, 1), "Top");
                _windows.Add(topWin);

                if (!hasLeftNeighbor)
                {
                    var leftWin = new BezelEdgeWindow(this, new Rectangle(b.Left, b.Top, 1, b.Height), "Left");
                    _windows.Add(leftWin);
                }

                if (!hasRightNeighbor)
                {
                    var rightWin = new BezelEdgeWindow(this, new Rectangle(b.Right - 1, b.Top, 1, b.Height), "Right");
                    _windows.Add(rightWin);
                }
            }

            foreach (var w in _windows)
            {
                w.Show();
            }
        }

        internal void FireBezel(int code)
        {
            OnBezelFired?.Invoke(this, code);
        }
    }

    public class BezelEdgeWindow : Form
    {
        private BezelWindowManager _manager;
        private string _edgeType;
        private System.Windows.Forms.Timer _timer;
        private bool _fired = false;
        private Point _lastMousePos;

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        public BezelEdgeWindow(BezelWindowManager manager, Rectangle bounds, string edgeType)
        {
            _manager = manager;
            _edgeType = edgeType;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = bounds;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;

            _timer = new System.Windows.Forms.Timer { Interval = 50 }; 
            _timer.Tick += Timer_Tick;

            this.MouseEnter += BezelEdgeWindow_MouseEnter;
            this.MouseMove += BezelEdgeWindow_MouseMove;
            this.MouseLeave += BezelEdgeWindow_MouseLeave;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Alpha = 1 で、目に見えないレベルの透明度にする（0にするとマウスイベントが透過してしまうため）
            SetLayeredWindowAttributes(this.Handle, 0, 1, 0x2); 
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private void BezelEdgeWindow_MouseEnter(object sender, EventArgs e)
        {
            if (!_fired)
            {
                _lastMousePos = this.PointToClient(Cursor.Position);
                _timer.Stop();
                _timer.Start();
            }
        }

        private void BezelEdgeWindow_MouseMove(object sender, MouseEventArgs e)
        {
            _lastMousePos = e.Location;
        }

        private void BezelEdgeWindow_MouseLeave(object sender, EventArgs e)
        {
            _timer.Stop();
            _fired = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            if (_fired) return;

            int code = -1;
            int x = _lastMousePos.X;
            int y = _lastMousePos.Y;
            int w = this.Width;
            int h = this.Height;

            if (_edgeType == "Top")
            {
                if (x < 4) code = 0; 
                else if (x >= w - 4) code = 4; 
                else
                {
                    if (x < w / 3) code = 1;
                    else if (x < (w * 2) / 3) code = 2;
                    else code = 3;
                }
            }
            else if (_edgeType == "Left")
            {
                if (y < 4) code = 0; 
                else if (y >= h - 4) code = 12; 
                else
                {
                    if (y < h / 3) code = 15;
                    else if (y < (h * 2) / 3) code = 14;
                    else code = 13;
                }
            }
            else if (_edgeType == "Right")
            {
                if (y < 4) code = 4; 
                else if (y >= h - 4) code = 8; 
                else
                {
                    if (y < h / 3) code = 5;
                    else if (y < (h * 2) / 3) code = 6;
                    else code = 7;
                }
            }

            if (code != -1)
            {
                _manager.FireBezel(code);
                _fired = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Stop();
                _timer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
