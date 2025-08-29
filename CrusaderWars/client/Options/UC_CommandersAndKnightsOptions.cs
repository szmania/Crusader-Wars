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

            // Adjust table layouts for better alignment and add tooltips
            AdjustTableLayouts();

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
                control.ValueChanged += Commander_ValueChanged;
            }

            foreach (var control in knightControls)
            {
                control.ValueChanged += Knight_ValueChanged;
            }

            // Set default values
            SetDefaults();
        }

        private void Commander_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown changedControl = (NumericUpDown)sender;
            int total = commanderControls.Sum(c => (int)c.Value);
            
            if (total > 100)
            {
                // Prevent the change by reducing the value that was just increased
                int excess = total - 100;
                changedControl.Value -= excess;
            }
            
            UpdateTotal(commanderControls, lblCommanderTotal);
        }

        private void Knight_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown changedControl = (NumericUpDown)sender;
            int total = knightControls.Sum(c => (int)c.Value);
            
            if (total > 100)
            {
                // Prevent the change by reducing the value that was just increased
                int excess = total - 100;
                changedControl.Value -= excess;
            }
            
            UpdateTotal(knightControls, lblKnightTotal);
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

        private void AdjustTableLayouts()
        {
            // Add tooltips to group boxes
            toolTip1.SetToolTip(groupCommanders, "Configure the wound chances for commanders when they fall in battle");
            toolTip1.SetToolTip(groupKnights, "Configure the wound chances for knights when they fall in battle");
            
            // Add tooltips to labels
            toolTip1.SetToolTip(lblCommanderWounded, "Chance for commander to be wounded when fallen in battle");
            toolTip1.SetToolTip(lblCommanderSeverelyInjured, "Chance for commander to be severely injured when fallen in battle");
            toolTip1.SetToolTip(lblCommanderBrutallyMauled, "Chance for commander to be brutally mauled when fallen in battle");
            toolTip1.SetToolTip(lblCommanderMaimed, "Chance for commander to be maimed when fallen in battle");
            toolTip1.SetToolTip(lblCommanderOneLegged, "Chance for commander to lose a leg when fallen in battle");
            toolTip1.SetToolTip(lblCommanderOneEyed, "Chance for commander to lose an eye when fallen in battle");
            toolTip1.SetToolTip(lblCommanderDisfigured, "Chance for commander to be disfigured when fallen in battle");
            
            toolTip1.SetToolTip(lblKnightWounded, "Chance for knight to be wounded when fallen in battle");
            toolTip1.SetToolTip(lblKnightSeverelyInjured, "Chance for knight to be severely injured when fallen in battle");
            toolTip1.SetToolTip(lblKnightBrutallyMauled, "Chance for knight to be brutally mauled when fallen in battle");
            toolTip1.SetToolTip(lblKnightMaimed, "Chance for knight to be maimed when fallen in battle");
            toolTip1.SetToolTip(lblKnightOneLegged, "Chance for knight to lose a leg when fallen in battle");
            toolTip1.SetToolTip(lblKnightOneEyed, "Chance for knight to lose an eye when fallen in battle");
            toolTip1.SetToolTip(lblKnightDisfigured, "Chance for knight to be disfigured when fallen in battle");
            
            // Add tooltip to reset button
            toolTip1.SetToolTip(btnReset, "Reset all wound chance values to their default settings");
            
            // Adjust commanders table
            tableCommanders.RowStyles.Clear();
            for (int i = 0; i < 8; i++)
            {
                tableCommanders.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            }
            
            // Adjust knights table
            tableKnights.RowStyles.Clear();
            for (int i = 0; i < 8; i++)
            {
                tableKnights.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5F));
            }
            
            // Add padding for better vertical alignment
            tableCommanders.Padding = new Padding(0, 5, 0, 0);
            tableKnights.Padding = new Padding(0, 5, 0, 0);
            
            // Ensure labels are properly anchored
            foreach (Control control in tableCommanders.Controls)
            {
                if (control is Label && control != lblCommanderTotal)
                {
                    control.Anchor = AnchorStyles.Left;
                }
                else if (control == lblCommanderTotal)
                {
                    control.Anchor = AnchorStyles.Right;
                }
            }
            
            foreach (Control control in tableKnights.Controls)
            {
                if (control is Label && control != lblKnightTotal)
                {
                    control.Anchor = AnchorStyles.Left;
                }
                else if (control == lblKnightTotal)
                {
                    control.Anchor = AnchorStyles.Right;
                }
            }
        }
    }
}
