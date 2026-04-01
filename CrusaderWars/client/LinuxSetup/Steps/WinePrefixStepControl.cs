using System.Drawing;
using System.Windows.Forms;

namespace CrusaderWars.client.LinuxSetup.Steps
{
    public partial class WinePrefixStepControl : UserControl
    {
        public WinePrefixStepControl()
        {
            InitializeComponent();
        }

        public void SetStatus(string message, bool success)
        {
            lblWinePrefixStatus.Text = message;
            lblWinePrefixStatus.ForeColor = success ? Color.Green : Color.Black;
        }
    }
}
