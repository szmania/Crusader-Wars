using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrusaderWars.client
{
    public partial class UC_Toggle : UserControl
    {
        private bool _state = false;
        
        public bool State 
        {  
            get { return _state; } 
            set 
            { 
                _state = value;
                SetState(_state);
            } 
        }
        
        public UC_Toggle()
        {
            InitializeComponent();
        }

        void ChangeState()
        {
            if (State == true)
            {
                State = false;
                this.BackgroundImage = Properties.Resources.toggle_no;
            }
            else
            {
                State = true;
                this.BackgroundImage = Properties.Resources.toggle_yes;
            }
        }

        public void SetState(bool state)
        {
            State = state;
            if (State == true)
            {
                this.BackgroundImage = Properties.Resources.toggle_yes;
            }
            else
            {
                this.BackgroundImage = Properties.Resources.toggle_no;
            }
        }

        private void UC_Toggle_Click(object sender, EventArgs e)
        {
            ChangeState();
            SoundPlayer sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
        }


    }
}
