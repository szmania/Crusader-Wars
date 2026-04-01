using System.Windows.Forms;

namespace CrusaderWars.client.LinuxSetup.Steps
{
    public partial class CompletionStepControl : UserControl
    {
        public CompletionStepControl()
        {
            InitializeComponent();
        }

        public void SetMessage(string message)
        {
            if (lblCompletionStatus.InvokeRequired)
            {
                lblCompletionStatus.Invoke(new MethodInvoker(() => lblCompletionStatus.Text = message));
            }
            else
            {
                lblCompletionStatus.Text = message;
            }
        }
    }
}
