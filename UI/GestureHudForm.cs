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
            
            // 背景透過
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;

            int size = _actionDef.GestureSize;
            this.Size = new Size(size, size);

            if (SendInputNative.GetCursorPos(out var pt))
            {
                // ホイールの中心をマウスポインターの位置にするための基本座標
                int targetX = pt.X - size / 2;
                int targetY = pt.Y - size / 2;

                // 現在マウスポインターがあるスクリーンの作業領域を取得
                Screen screen = Screen.FromPoint(new Point(pt.X, pt.Y));
                Rectangle bounds = screen.Bounds;

                // 画面外にはみ出さないようにクランプ処理（画面内に収める）
                if (targetX < bounds.Left) targetX = bounds.Left;
                if (targetY < bounds.Top) targetY = bounds.Top;
                if (targetX + size > bounds.Right) targetX = bounds.Right - size;
                if (targetY + size > bounds.Bottom) targetY = bounds.Bottom - size;

                this.Location = new Point(targetX, targetY);
                
                // 角度計算の基準となる中心座標も、実際に表示されたHUDの中心に合わせる
                _centerPoint = new Point(targetX + size / 2, targetY + size / 2);
            }

            this.DoubleBuffered = true;
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
            int cancelRadius = (int)(radius * 0.3); // 中心30%をキャンセル領域とする

            if (!SendInputNative.GetCursorPos(out var pt)) return;

            int dx = pt.X - _centerPoint.X;
            int dy = pt.Y - _centerPoint.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            SelectedDirectionIndex = -1;
            
            // キャンセル領域（デッドゾーン）の外にいる場合のみ選択
            if (dist > cancelRadius)
            {
                double angle = Math.Atan2(dy, dx);
                if (angle < 0) angle += 2 * Math.PI;
                double sliceAngle = 2 * Math.PI / slices;
                angle += sliceAngle / 2.0;
                if (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
                SelectedDirectionIndex = (int)(angle / sliceAngle);
            }

            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);

            // 背景の半透明円
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
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

                    double midAngle = (i * sweep) * Math.PI / 180.0;
                    // ラベル位置（キャンセル領域を避けるため外側に寄せる）
                    float tx = radius + (float)(Math.Cos(midAngle) * radius * 0.65);
                    float ty = radius + (float)(Math.Sin(midAngle) * radius * 0.65);
                    
                    string label = "";
                    if (i < _actionDef.GestureDirections.Count) label = _actionDef.GestureDirections[i].Label;
                    if (!string.IsNullOrEmpty(label))
                    {
                        e.Graphics.DrawString(label, f, Brushes.White, tx, ty, sf);
                    }
                }

                // 中心キャンセル領域の描画
                Rectangle cancelRect = new Rectangle(radius - cancelRadius, radius - cancelRadius, cancelRadius * 2, cancelRadius * 2);
                using (Brush cancelBrush = new SolidBrush(Color.FromArgb(200, 60, 60, 60)))
                {
                    e.Graphics.FillEllipse(cancelBrush, cancelRect);
                }
                using (Pen cancelPen = new Pen(Color.FromArgb(200, 255, 255, 255), 2))
                {
                    e.Graphics.DrawEllipse(cancelPen, cancelRect);
                }
                using (Font fCancel = new Font("MS UI Gothic", 9, FontStyle.Bold))
                {
                    e.Graphics.DrawString("Cancel", fCancel, Brushes.White, radius, radius, sf);
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
