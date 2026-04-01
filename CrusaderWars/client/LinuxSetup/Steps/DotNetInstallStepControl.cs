using System.Drawing;
using System.Windows.Forms;

namespace CrusaderWars.client.LinuxSetup.Steps
{
    public partial class DotNetInstallStepControl : UserControl
    {
        public DotNetInstallStepControl()
        {
            InitializeComponent();
        }

        public void UpdateStatus(string message)
        {
            if (lblDotNetStatus.InvokeRequired)
            {
                lblDotNetStatus.Invoke(new MethodInvoker(() => lblDotNetStatus.Text = message));
            }
            else
            {
                lblDotNetStatus.Text = message;
            }
        }

        public void SetSuccess()
        {
            lblDotNetStatus.ForeColor = Color.Green;
        }

        public void SetError()
        {
            lblDotNetStatus.ForeColor = Color.Red;
        }
    }
}
