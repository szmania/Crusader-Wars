using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CrusaderWars.client.Options
{
    public partial class UC_CommandersAndKnightsOptions : UserControl
    {
        private List<NumericUpDown> commanderControls;
        private List<NumericUpDown> knightControls;

        public UC_CommandersAndKnightsOptions()
        {
            InitializeComponent();

            commanderControls = new List<NumericUpDown>
            {
                numCommanderWounded, numCommanderSeverelyInjured, numCommanderBrutallyMauled,
                numCommanderMaimed, numCommanderOneLegged, numCommanderOneEyed, numCommanderDisfigured
            };

            knightControls = new List<NumericUpDown>
            {
                numKnightWounded, numKnightSeverelyInjured, numKnightBrutallyMauled,
                numKnightMaimed, numKnightOneLegged, numKnightOneEyed, numKnightDisfigured
            };

            foreach (var control in commanderControls)
            {
                control.ValueChanged += (s, e) => UpdateTotal(commanderControls, lblCommanderTotal);
            }

            foreach (var control in knightControls)
            {
                control.ValueChanged += (s, e) => UpdateTotal(knightControls, lblKnightTotal);
            }
        }

        private void UpdateTotal(List<NumericUpDown> controls, Label totalLabel)
        {
            int total = controls.Sum(c => (int)c.Value);
            totalLabel.Text = $"Total: {total}%";
            totalLabel.ForeColor = total == 100 ? Color.White : Color.Red;
        }

        public void SetDefaults()
        {
            // Commander Defaults
            numCommanderWounded.Value = 50;
            numCommanderSeverelyInjured.Value = 20;
            numCommanderBrutallyMauled.Value = 20;
            numCommanderMaimed.Value = 3;
            numCommanderOneLegged.Value = 3;
            numCommanderOneEyed.Value = 3;
            numCommanderDisfigured.Value = 1;

            // Knight Defaults
            numKnightWounded.Value = 50;
            numKnightSeverelyInjured.Value = 20;
            numKnightBrutallyMauled.Value = 20;
            numKnightMaimed.Value = 3;
            numKnightOneLegged.Value = 3;
            numKnightOneEyed.Value = 3;
            numKnightDisfigured.Value = 1;

            UpdateTotal(commanderControls, lblCommanderTotal);
            UpdateTotal(knightControls, lblKnightTotal);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            SetDefaults();
        }
    }
}
