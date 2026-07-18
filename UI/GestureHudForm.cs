using System;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using UsbInputMapper.Core;

namespace UsbInputMapper.UI
{
    public class GestureHudForm : Form
    {
        private ActionDef _actionDef;
        public int SelectedDirectionIndex { get; private set; } = -1;
        private Point _centerPoint;
        private Timer _drawTimer;

        public GestureHudForm(ActionDef actionDef)
        {
            _actionDef = actionDef;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            
            // 背景を透過
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;

            int size = _actionDef.GestureSize;
            this.Size = new Size(size, size);

            if (SendInputNative.GetCursorPos(out var pt))
            {
                _centerPoint = new Point(pt.X, pt.Y);
                this.Location = new Point(pt.X - size / 2, pt.Y - size / 2);
            }

            this.DoubleBuffered = true;

            // 60FPS程度で描画更新
            _drawTimer = new Timer { Interval = 16 };
            _drawTimer.Tick += (s, e) => this.Invalidate();
            _drawTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int slices = _actionDef.GestureSlices;
            int radius = this.Width / 2;

            if (!SendInputNative.GetCursorPos(out var pt)) return;

            int dx = pt.X - _centerPoint.X;
            int dy = pt.Y - _centerPoint.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            SelectedDirectionIndex = -1;
            if (dist > 10) // センターのデッドゾーン
            {
                double angle = Math.Atan2(dy, dx);
                if (angle < 0) angle += 2 * Math.PI;
                
                // 右(0度)を中心としてスライスが配置されるようにオフセット
                double sliceAngle = 2 * Math.PI / slices;
                angle += sliceAngle / 2.0;
                if (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
                SelectedDirectionIndex = (int)(angle / sliceAngle);
            }

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);

            // 背景の半透明円
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
            {
                e.Graphics.FillEllipse(bgBrush, rect);
            }

            float sweep = 360f / slices;
            using (Font f = new Font("MS UI Gothic", 9))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                for (int i = 0; i < slices; i++)
                {
                    float startAngle = i * sweep - sweep / 2f;
                    
                    // 選択中のスライスをハイライト
                    if (i == SelectedDirectionIndex)
                    {
                        using (Brush selBrush = new SolidBrush(Color.FromArgb(180, 50, 150, 255)))
                        {
                            e.Graphics.FillPie(selBrush, rect, startAngle, sweep);
                        }
                    }
                    
                    // 境界線を描画
                    using (Pen p = new Pen(Color.FromArgb(200, 255, 255, 255), 1))
                    {
                        e.Graphics.DrawPie(p, rect, startAngle, sweep);
                    }

                    // テキスト(ラベル)を描画
                    double midAngle = (i * sweep) * Math.PI / 180.0;
                    float tx = radius + (float)(Math.Cos(midAngle) * radius * 0.7);
                    float ty = radius + (float)(Math.Sin(midAngle) * radius * 0.7);
                    
                    string label = "";
                    if (i < _actionDef.GestureDirections.Count) label = _actionDef.GestureDirections[i].Label;
                    if (!string.IsNullOrEmpty(label))
                    {
                        e.Graphics.DrawString(label, f, Brushes.White, tx, ty, sf);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _drawTimer?.Stop();
                _drawTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
