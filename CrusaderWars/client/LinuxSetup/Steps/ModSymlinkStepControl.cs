using System.Drawing;
using System.Windows.Forms;

namespace CrusaderWars.client.LinuxSetup.Steps
{
    public partial class ModSymlinkStepControl : UserControl
    {
        public ModSymlinkStepControl()
        {
            InitializeComponent();
        }

        public void SetStatus(string message, bool success)
        {
            if (lblModSymlinkStatus.InvokeRequired)
            {
                lblModSymlinkStatus.Invoke(new MethodInvoker(() => {
                    lblModSymlinkStatus.Text = message;
                    lblModSymlinkStatus.ForeColor = success ? Color.Green : Color.Red;
                }));
            }
            else
            {
                lblModSymlinkStatus.Text = message;
                lblModSymlinkStatus.ForeColor = success ? Color.Green : Color.Red;
            }
        }
    }
}
