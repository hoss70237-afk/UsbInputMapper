using System;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Profiles;
using System.Drawing.Drawing2D;

namespace UsbInputMapper.UI
{
    public class StickMouseSetupForm : Form
    {
        private ActionDef _action;
        
        private NumericUpDown numDeadZone;
        private NumericUpDown numMaxSpeed;
        private ComboBox cmbCurve;
        
        private Panel pnlGraph;
        private Timer _renderTimer;

        // シミュレーション用変数（現在位置）
        private float _simInputX = 0f;
        private float _simInputY = 0f;
        private bool _isSimulating = false;

        public StickMouseSetupForm(ActionDef action)
        {
            _action = action;
            
            this.Text = "スティックマウス 詳細設定";
            this.Size = new Size(380, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblDz = new Label { Text = "デッドゾーン (%):", Location = new Point(15, 20), AutoSize = true };
            numDeadZone = new NumericUpDown { Location = new Point(120, 18), Size = new Size(60, 20), Maximum = 99 };
            numDeadZone.Value = _action.StickDeadZone;

            Label lblSpd = new Label { Text = "最高速度 (px):", Location = new Point(15, 50), AutoSize = true };
            numMaxSpeed = new NumericUpDown { Location = new Point(120, 48), Size = new Size(60, 20), Maximum = 100, Minimum = 1 };
            numMaxSpeed.Value = _action.StickMaxSpeed;

            Label lblCrv = new Label { Text = "加速度カーブ:", Location = new Point(15, 80), AutoSize = true };
            cmbCurve = new ComboBox { Location = new Point(120, 78), Size = new Size(100, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCurve.Items.AddRange(new[] { "リニア (一定)", "早め (Log)", "遅め (Exp)" });
            cmbCurve.SelectedIndex = _action.StickCurve;

            pnlGraph = new Panel { Location = new Point(15, 120), Size = new Size(330, 200), BorderStyle = BorderStyle.FixedSingle };
            pnlGraph.BackColor = Color.White;
            
            // ダブルバッファリング有効化
            typeof(Panel).InvokeMember("DoubleBuffered", 
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, 
                null, pnlGraph, new object[] { true });

            pnlGraph.Paint += PnlGraph_Paint;
            pnlGraph.MouseDown += (s, e) => { _isSimulating = true; UpdateSimPosition(e.X, e.Y); };
            pnlGraph.MouseMove += (s, e) => { if (_isSimulating) UpdateSimPosition(e.X, e.Y); };
            pnlGraph.MouseUp += (s, e) => { _isSimulating = false; _simInputX = 0; _simInputY = 0; };
            pnlGraph.MouseLeave += (s, e) => { _isSimulating = false; _simInputX = 0; _simInputY = 0; };

            Label lblHint = new Label { Text = "※グラフ上をドラッグして出力値をシミュレートできます", Location = new Point(15, 325), AutoSize = true, ForeColor = Color.Gray };

            Button btnOk = new Button { Text = "OK", Location = new Point(190, 350), Size = new Size(75, 23) };
            btnOk.Click += BtnOk_Click;
            Button btnCancel = new Button { Text = "キャンセル", Location = new Point(270, 350), Size = new Size(75, 23) };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblDz); this.Controls.Add(numDeadZone);
            this.Controls.Add(lblSpd); this.Controls.Add(numMaxSpeed);
            this.Controls.Add(lblCrv); this.Controls.Add(cmbCurve);
            this.Controls.Add(pnlGraph);
            this.Controls.Add(lblHint);
            this.Controls.Add(btnOk); this.Controls.Add(btnCancel);

            _renderTimer = new Timer { Interval = 16 };
            _renderTimer.Tick += (s, e) => pnlGraph.Invalidate();
            _renderTimer.Start();
        }

        private void UpdateSimPosition(int mouseX, int mouseY)
        {
            float centerX = pnlGraph.Width / 2f;
            float centerY = pnlGraph.Height / 2f;
            float radius = Math.Min(centerX, centerY);

            float dx = (mouseX - centerX) / radius;
            float dy = (mouseY - centerY) / radius;

            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist > 1.0f)
            {
                dx /= dist; dy /= dist;
            }

            _simInputX = dx;
            _simInputY = dy;
        }

        private void PnlGraph_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = pnlGraph.Width;
            int h = pnlGraph.Height;
            float cx = w / 2f;
            float cy = h / 2f;
            float radius = Math.Min(cx, cy) - 10;

            // 軸の描画
            using (Pen gridPen = new Pen(Color.LightGray))
            {
                g.DrawLine(gridPen, 0, cy, w, cy);
                g.DrawLine(gridPen, cx, 0, cx, h);
                g.DrawEllipse(gridPen, cx - radius, cy - radius, radius * 2, radius * 2);
            }

            // デッドゾーンの円を描画
            float dzPercent = (float)numDeadZone.Value / 100f;
            float dzRadius = radius * dzPercent;
            using (Pen dzPen = new Pen(Color.FromArgb(100, 255, 0, 0), 2))
            using (Brush dzBrush = new SolidBrush(Color.FromArgb(30, 255, 0, 0)))
            {
                g.FillEllipse(dzBrush, cx - dzRadius, cy - dzRadius, dzRadius * 2, dzRadius * 2);
                g.DrawEllipse(dzPen, cx - dzRadius, cy - dzRadius, dzRadius * 2, dzRadius * 2);
            }

            if (_isSimulating)
            {
                float inX = cx + _simInputX * radius;
                float inY = cy + _simInputY * radius;
                g.FillEllipse(Brushes.DodgerBlue, inX - 4, inY - 4, 8, 8);

                float inputDist = (float)Math.Sqrt(_simInputX * _simInputX + _simInputY * _simInputY);
                if (inputDist > 1f) inputDist = 1f;

                float outputRatio = 0f;
                if (inputDist > dzPercent)
                {
                    float range = 1f - dzPercent;
                    float normalized = (inputDist - dzPercent) / range;
                    
                    int curveType = cmbCurve.SelectedIndex;
                    if (curveType == 1) outputRatio = (float)Math.Sin(normalized * Math.PI / 2);
                    else if (curveType == 2) outputRatio = normalized * normalized;
                    else outputRatio = normalized;
                }

                if (outputRatio > 0)
                {
                    float maxSpd = (float)numMaxSpeed.Value;
                    float currentSpeed = outputRatio * maxSpd;

                    float dirX = _simInputX / inputDist;
                    float dirY = _simInputY / inputDist;

                    float outX = cx + dirX * outputRatio * radius;
                    float outY = cy + dirY * outputRatio * radius;

                    using (Pen outPen = new Pen(Color.MediumAquamarine, 2))
                    {
                        outPen.EndCap = LineCap.ArrowAnchor;
                        g.DrawLine(outPen, cx, cy, outX, outY);
                    }
                    g.FillEllipse(Brushes.MediumAquamarine, outX - 5, outY - 5, 10, 10);

                    string info = $"速度: {currentSpeed:F1} px/f";
                    g.DrawString(info, SystemFonts.DefaultFont, Brushes.Black, 5, 5);
                }
                else
                {
                    g.DrawString("デッドゾーン内 (出力なし)", SystemFonts.DefaultFont, Brushes.Red, 5, 5);
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            _action.StickDeadZone = (int)numDeadZone.Value;
            _action.StickMaxSpeed = (int)numMaxSpeed.Value;
            _action.StickCurve = cmbCurve.SelectedIndex;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _renderTimer != null)
            {
                _renderTimer.Stop();
                _renderTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
