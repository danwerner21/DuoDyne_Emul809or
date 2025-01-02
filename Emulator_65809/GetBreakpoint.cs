using System;
using System.Drawing;
using System.Windows.Forms;

namespace Emul809or
{
    public partial class GetBreakpoint : Form
    {
        public ushort Address = 0;

        public GetBreakpoint()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Address = 0;
            this.Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                this.Address = Convert.ToUInt16(txtAddress.Text, 16);
                this.Close();
            }
            catch
            {
                txtAddress.BackColor = Color.Pink;
            }

        }
    }
}
