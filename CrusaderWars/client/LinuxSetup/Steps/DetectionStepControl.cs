using System.Drawing;
using System.Windows.Forms;

namespace CrusaderWars.client.LinuxSetup.Steps
{
    public partial class DetectionStepControl : UserControl
    {
        public DetectionStepControl()
        {
            InitializeComponent();
        }

        public void SetDetectionResult(string item, string result, bool success)
        {
            Label? lblToUpdate = null;
            switch (item)
            {
                case "Linux":
                    lblToUpdate = lblLinuxCheck;
                    break;
                case "Wine":
                    lblToUpdate = lblWineCheck;
                    break;
                case "Steam":
                    lblToUpdate = lblSteamCheck;
                    break;
                case "Attila":
                    lblToUpdate = lblAttilaCheck;
                    break;
                case "Desktop":
                    lblToUpdate = lblDesktopEnvCheck;
                    break;
            }

            if (lblToUpdate != null)
            {
                lblToUpdate.Text = $"{item}: {result}";
                lblToUpdate.ForeColor = success ? Color.Green : Color.Red;
            }
        }
    }
}
