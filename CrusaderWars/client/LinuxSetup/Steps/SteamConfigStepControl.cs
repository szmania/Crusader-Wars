using System.Drawing;
using System.Windows.Forms;

namespace CrusaderWars.client.LinuxSetup.Steps
{
    public partial class SteamConfigStepControl : UserControl
    {
        public SteamConfigStepControl()
        {
            InitializeComponent();
        }

        public void SetStatus(string message)
        {
            if (lblSteamConfigStatus.InvokeRequired)
            {
                lblSteamConfigStatus.Invoke(new MethodInvoker(() => lblSteamConfigStatus.Text = message));
            }
            else
            {
                lblSteamConfigStatus.Text = message;
            }
        }
    }
}
