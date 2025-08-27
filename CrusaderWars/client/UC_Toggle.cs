using System;
using System.ComponentModel;
using System.Media;
using System.Windows.Forms;

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
            SoundPlayer sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
        }
    }
}