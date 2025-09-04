using System;
using System.ComponentModel;
using System.Media;
using System.Windows.Forms;
using System.IO; // Added for File.Exists

namespace CrusaderWars.client
{
    public partial class UC_Toggle : UserControl
    {
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(false)]
        public bool State { get; set; } = false;

        public UC_Toggle()
        {
            InitializeComponent();
            UpdateBackground();
        }

        void ChangeState()
        {
            State = !State;
            UpdateBackground();
        }

        public void SetState(bool state)
        {
            State = state;
            UpdateBackground();
        }

        private void UpdateBackground()
        {
            if (State)
                this.BackgroundImage = Properties.Resources.toggle_yes;
            else
                this.BackgroundImage = Properties.Resources.toggle_no;
        }

        private void UC_Toggle_Click(object sender, EventArgs e)
        {
            ChangeState();
            string soundPath = @".\data\sounds\metal-dagger-hit-185444.wav";
            if (File.Exists(soundPath))
            {
                try
                {
                    SoundPlayer sounds = new SoundPlayer(soundPath);
                    sounds.Play();
                }
                catch (Exception)
                {
                    // Log the error silently, as it's a non-critical UI sound.
                    // Program.Logger.Debug($"Error playing toggle sound: {ex.Message}"); // Optional logging
                }
            }
        }
    }
}
