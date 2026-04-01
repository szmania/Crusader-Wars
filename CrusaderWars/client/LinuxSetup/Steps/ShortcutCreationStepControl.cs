using System.Drawing;
using System.Windows.Forms;

namespace CrusaderWars.client.LinuxSetup.Steps
{
    public partial class ShortcutCreationStepControl : UserControl
    {
        public ShortcutCreationStepControl()
        {
            InitializeComponent();
        }

        public void SetStatus(string message, bool success)
        {
            if (lblShortcutStatus.InvokeRequired)
            {
                lblShortcutStatus.Invoke(new MethodInvoker(() => {
                    lblShortcutStatus.Text = message;
                    lblShortcutStatus.ForeColor = success ? Color.Green : Color.Red;
                }));
            }
            else
            {
                lblShortcutStatus.Text = message;
                lblShortcutStatus.ForeColor = success ? Color.Green : Color.Red;
            }
        }
    }
}
