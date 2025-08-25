using System;
using System.Windows.Forms;

namespace CrusaderWars
{
    public partial class LoadingScreen : Form
    {
        public LoadingScreen()
        {
            InitializeComponent();

            //Icon
            this.Icon = Properties.Resources.logo;
            flowLayoutPanel2.Hide();
        }

        public void ChangeMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(ChangeMessage), new object[] { message });
            }
            else
            {
                Label_Message.Text = message;
            }
        }

        public void ChangeUnitMapperMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(ChangeUnitMapperMessage), new object[] { message });
            }
            else
            {
                Label_UnitMapper.Text = message;
                flowLayoutPanel2.Show();
            }
        }

        private void LoadingScreen_Shown(object sender, EventArgs e)
        {
            this.TopMost = false;
        }

    }
}

