using System;
using System.Diagnostics;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Crusader_Wars.client.BETAUPDATE_Message
{
    public partial class BETAUPDATE_Message : Form
    {
        public BETAUPDATE_Message()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.logo;
        }

        private void SetMessage(string message)
        {
            Text_Warning.Text = message;
        }

        public static void ShowWarningMessage(string message)
        {
            var warning_message = new BETAUPDATE_Message();
            warning_message.SetMessage(message);
            SystemSounds.Exclamation.Play();
            warning_message.ShowDialog();

        }


        private void btnOK_Click(object sender, EventArgs e)
        {
            //Internet Connection
            try
            {
                Process.Start("https://crusaderwars.com/#download-page");
                this.Close();
            }
            //Offline
            catch 
            {
                MessageBox.Show("No Internet connection. Your ck3 mod version is for the new beta release, if you don't download the new the CW launcher from the website it may cause crashes!", "Crusader Wars: Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                this.Close();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
