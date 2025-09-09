using System.Windows.Forms;
using System.Drawing;
using CrusaderWars.client; // For Program.Logger

namespace CrusaderWars.client
{
    public partial class LoadingScreen : Form
    {
        private Label messageLabel;
        private Label unitMapperLabel;

        public LoadingScreen()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.BackgroundImageLayout = ImageLayout.Stretch; // Default image, will be changed by HomePage

            messageLabel = new Label();
            messageLabel.AutoSize = false;
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;
            messageLabel.Dock = DockStyle.Bottom;
            messageLabel.Height = 50;
            messageLabel.ForeColor = Color.White;
            messageLabel.Font = new Font("Arial", 14, FontStyle.Bold);
            messageLabel.Text = "Loading...";
            this.Controls.Add(messageLabel);

            unitMapperLabel = new Label();
            unitMapperLabel.AutoSize = false;
            unitMapperLabel.TextAlign = ContentAlignment.MiddleCenter;
            unitMapperLabel.Dock = DockStyle.Top;
            unitMapperLabel.Height = 30;
            unitMapperLabel.ForeColor = Color.LightGray;
            unitMapperLabel.Font = new Font("Arial", 10);
            unitMapperLabel.Text = "";
            this.Controls.Add(unitMapperLabel);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // LoadingScreen
            //
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "LoadingScreen";
            this.ResumeLayout(false);
        }

        public void ChangeMessage(string message)
        {
            if (messageLabel.InvokeRequired)
            {
                messageLabel.Invoke(new System.Action(() => messageLabel.Text = message));
            }
            else
            {
                messageLabel.Text = message;
            }
        }

        public void ChangeUnitMapperMessage(string message)
        {
            if (unitMapperLabel.InvokeRequired)
            {
                unitMapperLabel.Invoke(new System.Action(() => unitMapperLabel.Text = message));
            }
            else
            {
                unitMapperLabel.Text = message;
            }
        }
    }
}
