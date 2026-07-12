using System;
using System.Windows.Forms;
using UsbInputMapper.Profiles;

namespace UsbInputMapper.UI
{
    public partial class BindingEditorForm : Form
    {
        public ActionDef ResultAction { get; private set; }

        public BindingEditorForm()
        {
            InitializeComponent();
            cmbActionType.DataSource = Enum.GetValues(typeof(ActionType));
        }

        private void cmbActionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var type = (ActionType)cmbActionType.SelectedItem;
            txtArgumentNum.Enabled = (type == ActionType.Keyboard || type == ActionType.XboxController);
            txtArgumentStr.Enabled = (type == ActionType.AppLaunch);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            ResultAction = new ActionDef
            {
                ActionType = (ActionType)cmbActionType.SelectedItem
            };

            if (int.TryParse(txtArgumentNum.Text, out int num))
            {
                ResultAction.ArgumentNum = num;
            }
            ResultAction.ArgumentStr = txtArgumentStr.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
