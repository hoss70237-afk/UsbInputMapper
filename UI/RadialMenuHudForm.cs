using System;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public class RadialMenuHudForm : Form
    {
        private ActionDef _actionDef;
        public int SelectedDirectionIndex { get; private set; } = -1;
        private Point _centerPoint;
        private Timer _drawTimer;
        
        private int _lastIndex = -2;

        public RadialMenuHudForm(ActionDef actionDef)
        {
            _actionDef = actionDef;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;

            int size = _actionDef.RadialMenuSize;
            this.Size = new Size(size, size);

            if (SendInputNative.GetCursorPos(out var pt))
            {
                int targetX = pt.X - size / 2;
                int targetY = pt.Y - size / 2;
                Screen screen = Screen.FromPoint(new Point(pt.X, pt.Y));
                Rectangle bounds = screen.Bounds;
                if (targetX < bounds.Left) targetX = bounds.Left;
                if (targetY < bounds.Top) targetY = bounds.Top;
                if (targetX + size > bounds.Right) targetX = bounds.Right - size;
                if (targetY + size > bounds.Bottom) targetY = bounds.Bottom - size;

                this.Location = new Point(targetX, targetY);
                _centerPoint = new Point(targetX + size / 2, targetY + size / 2);
            }

            this.DoubleBuffered = true;
            _drawTimer = new Timer { Interval = 16 };
            _drawTimer.Tick += UpdateSelection;
            _drawTimer.Start();
        }

        private void UpdateSelection(object sender, EventArgs e)
        {
            if (this.IsDisposed) return;
            if (!SendInputNative.GetCursorPos(out var pt)) return;

            int slices = _actionDef.RadialMenuSlices;
            int radius = this.Width / 2;
            int cancelRadius = (int)(radius * 0.3);

            int dx = pt.X - _centerPoint.X;
            int dy = pt.Y - _centerPoint.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            int newIndex = -1;

            if (dist > cancelRadius)
            {
                double angle = Math.Atan2(dy, dx);
                angle += Math.PI / 2.0;

                if (angle < 0) angle += 2 * Math.PI;
                if (angle >= 2 * Math.PI) angle -= 2 * Math.PI;

                double sliceAngle = 2 * Math.PI / slices;
                
                angle += sliceAngle / 2.0;
                if (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
                
                newIndex = (int)(angle / sliceAngle);
            }

            if (newIndex != _lastIndex)
            {
                SelectedDirectionIndex = newIndex;
                _lastIndex = newIndex;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.IsDisposed) return;
            base.OnPaint(e);
            
            try
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int slices = _actionDef.RadialMenuSlices;
                int radius = this.Width / 2;
                int cancelRadius = (int)(radius * 0.3); 

                Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                using (Brush bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0))) 
                { 
                    e.Graphics.FillEllipse(bgBrush, rect); 
                }

                float sweep = 360f / slices;
                using (Font f = new Font("MS UI Gothic", 9, FontStyle.Bold))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    for (int i = 0; i < slices; i++)
                    {
                        float startAngle = i * sweep - sweep / 2f - 90f;
                        
                        if (i == SelectedDirectionIndex)
                        {
                            using (Brush selBrush = new SolidBrush(Color.FromArgb(180, 50, 150, 255)))
                            {
                                e.Graphics.FillPie(selBrush, rect, startAngle, sweep);
                            }
                        }
                        
                        using (Pen p = new Pen(Color.FromArgb(200, 255, 255, 255), 1))
                        {
                            e.Graphics.DrawPie(p, rect, startAngle, sweep);
                        }

                        double midAngle = (i * sweep - 90.0) * Math.PI / 180.0;
                        float tx = radius + (float)(Math.Cos(midAngle) * radius * 0.65);
                        float ty = radius + (float)(Math.Sin(midAngle) * radius * 0.65);
                        
                        // ★ ラベル未設定時はアクション名を自動表示
                        string label = "";
                        if (i < _actionDef.RadialMenuDirections.Count)
                        {
                            var dir = _actionDef.RadialMenuDirections[i];
                            if (!string.IsNullOrEmpty(dir.Label)) label = dir.Label;
                            else if (dir.Action != null && dir.Action.ActionType != ActionType.None) label = dir.Action.ToString();
                        }

                        if (!string.IsNullOrEmpty(label))
                        {
                            e.Graphics.DrawString(label, f, Brushes.White, tx, ty, sf);
                        }
                    }

                    Rectangle cancelRect = new Rectangle(radius - cancelRadius, radius - cancelRadius, cancelRadius * 2, cancelRadius * 2);
                    using (Brush cancelBrush = new SolidBrush(Color.FromArgb(200, 60, 60, 60))) { e.Graphics.FillEllipse(cancelBrush, cancelRect); }
                    using (Pen cancelPen = new Pen(Color.FromArgb(200, 255, 255, 255), 2)) { e.Graphics.DrawEllipse(cancelPen, cancelRect); }
                    using (Font fCancel = new Font("MS UI Gothic", 9, FontStyle.Bold)) { e.Graphics.DrawString("Cancel", fCancel, Brushes.White, radius, radius, sf); }
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { 
                if (_drawTimer != null) {
                    _drawTimer.Stop(); 
                    _drawTimer.Dispose(); 
                }
            }
            base.Dispose(disposing);
        }
    }
}
