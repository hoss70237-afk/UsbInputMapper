using System;
using System.Drawing;
using System.Windows.Forms;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public class StickMouseSetupForm : Form
    {
        private NumericUpDown numDZ;
        private NumericUpDown numMaxSpd;
        private ComboBox cmbCurve;
        private ActionDef _action;

        public StickMouseSetupForm(ActionDef action)
        {
            _action = action;
            this.Text = "スティックマウス設定";
            this.Size = new Size(300, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false; this.MinimizeBox = false;

            Label l1 = new Label { Text = "デッドゾーン(%):", Location = new Point(20, 20), AutoSize = true };
            numDZ = new NumericUpDown { Location = new Point(140, 18), Size = new Size(100, 20), Maximum = 99 };
            numDZ.Value = _action.StickDeadZone;

            Label l2 = new Label { Text = "最高速度(px/10ms):", Location = new Point(20, 50), AutoSize = true };
            numMaxSpd = new NumericUpDown { Location = new Point(140, 48), Size = new Size(100, 20), Maximum = 200 };
            numMaxSpd.Value = _action.StickMaxSpeed;

            Label l3 = new Label { Text = "加速度カーブ:", Location = new Point(20, 80), AutoSize = true };
            cmbCurve = new ComboBox { Location = new Point(140, 78), Size = new Size(100, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCurve.Items.AddRange(new[] { "リニア", "早め", "遅め" });
            cmbCurve.SelectedIndex = _action.StickCurve;

            Button btnOk = new Button { Text = "OK", Location = new Point(80, 120), Size = new Size(75, 23) };
            btnOk.Click += (s, e) => {
                _action.StickDeadZone = (int)numDZ.Value;
                _action.StickMaxSpeed = (int)numMaxSpd.Value;
                _action.StickCurve = cmbCurve.SelectedIndex;
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            Button btnCancel = new Button { Text = "キャンセル", Location = new Point(165, 120), Size = new Size(75, 23) };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(l1); this.Controls.Add(numDZ);
            this.Controls.Add(l2); this.Controls.Add(numMaxSpd);
            this.Controls.Add(l3); this.Controls.Add(cmbCurve);
            this.Controls.Add(btnOk); this.Controls.Add(btnCancel);
        }
    }
}
