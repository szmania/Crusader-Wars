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
                numCommanderMaimed, numCommanderOneLegged, numCommanderOneEyed, numCommanderDisfigured, numCommanderDeathChance
            };

            knightControls = new List<NumericUpDown>
            {
                numKnightWounded, numKnightSeverelyInjured, numKnightBrutallyMauled,
                numKnightMaimed, numKnightOneLegged, numKnightOneEyed, numKnightDisfigured, numKnightDeathChance
            };

            // Initial subscription
            SubscribeEventHandlers();
            
            // Set default values
            SetDefaults();
        }

        private void UnsubscribeEventHandlers()
        {
            foreach (var control in commanderControls)
            {
                control.ValueChanged -= Commander_ValueChanged;
            }
            foreach (var control in knightControls)
            {
                control.ValueChanged -= Knight_ValueChanged;
            }
        }

        private void SubscribeEventHandlers()
        {
            foreach (var control in commanderControls)
            {
                control.ValueChanged += Commander_ValueChanged;
            }
            foreach (var control in knightControls)
            {
                control.ValueChanged += Knight_ValueChanged;
            }
        }

        private void Commander_ValueChanged(object? sender, EventArgs e)
        {
            if (sender is not NumericUpDown changedControl) return;
            
            int total = commanderControls.Sum(c => (int)c.Value);
            
            if (total > 100)
            {
                // Prevent the change by reducing the value that was just increased
                int excess = total - 100;
                changedControl.Value -= excess;
            }
            
            UpdateCommanderTotal();
        }

        private void Knight_ValueChanged(object? sender, EventArgs e)
        {
            if (sender is not NumericUpDown changedControl) return;
            
            int total = knightControls.Sum(c => (int)c.Value);
            
            if (total > 100)
            {
                // Prevent the change by reducing the value that was just increased
                int excess = total - 100;
                changedControl.Value -= excess;
            }
            
            UpdateKnightTotal();
        }

        public void UpdateCommanderTotal()
        {
            int total = commanderControls.Sum(c => (int)c.Value);
            lblCommanderTotal.Text = $"Total: {total}%";
            lblCommanderTotal.ForeColor = total == 100 ? Color.White : Color.Red;
        }

        public void UpdateKnightTotal()
        {
            int total = knightControls.Sum(c => (int)c.Value);
            lblKnightTotal.Text = $"Total: {total}%";
            lblKnightTotal.ForeColor = total == 100 ? Color.White : Color.Red;
        }

        public void SetDefaults()
        {
            // Commander Defaults
            numCommanderWounded.Value = 35;
            numCommanderSeverelyInjured.Value = 20;
            numCommanderBrutallyMauled.Value = 20;
            numCommanderMaimed.Value = 3;
            numCommanderOneLegged.Value = 3;
            numCommanderOneEyed.Value = 3;
            numCommanderDisfigured.Value = 1;
            numCommanderDeathChance.Value = 15;

            // Knight Defaults
            numKnightWounded.Value = 35;
            numKnightSeverelyInjured.Value = 20;
            numKnightBrutallyMauled.Value = 20;
            numKnightMaimed.Value = 3;
            numKnightOneLegged.Value = 3;
            numKnightOneEyed.Value = 3;
            numKnightDisfigured.Value = 1;
            numKnightDeathChance.Value = 15;

            UpdateCommanderTotal();
            UpdateKnightTotal();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            UnsubscribeEventHandlers(); // Temporarily disable event handlers
            SetDefaults();              // Set all default values
            SubscribeEventHandlers();   // Re-enable event handlers

            // Manually update totals to reflect the newly set defaults
            UpdateCommanderTotal();
            UpdateKnightTotal();
        }

        private void AdjustTableLayouts()
        {
            // Add tooltips to group boxes
            toolTip1.SetToolTip(groupCommanders, "Configure the wound chances for commanders and knights when they fall in battle"); // Updated tooltip
            
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
            toolTip1.SetToolTip(numCommanderDeathChance, "Chance for a commander to die when they fall in battle. This is included in the 100% total chance.");
            toolTip1.SetToolTip(numKnightDeathChance, "Chance for a knight to die when they fall in battle. This is included in the 100% total chance.");
            
            // Add tooltip to reset button
            toolTip1.SetToolTip(btnReset, "Reset all wound chance values to their default settings");
            
            // Add padding for better vertical alignment
            tableCommanders.Padding = new Padding(0, 5, 0, 0);
            
            // Ensure controls are properly anchored in the combined tableCommanders
            foreach (Control control in tableCommanders.Controls)
            {
                if (control is Label && control.Name != "lblCommanderTotal" && control.Name != "lblKnightTotal" && control.Name != "lblCommanderHeader" && control.Name != "lblKnightHeader") // Exclude total and header labels
                {
                    control.Anchor = AnchorStyles.Left;
                }
                else if (control.Name == "lblCommanderTotal" || control.Name == "lblKnightTotal") // Both total labels
                {
                    control.Anchor = AnchorStyles.Right;
                }
                else if (control.Name == "lblCommanderHeader" || control.Name == "lblKnightHeader") // Both header labels
                {
                    control.Anchor = AnchorStyles.Left; // CHANGE THIS LINE: from AnchorStyles.None to AnchorStyles.Left
                }
                else if (control is NumericUpDown)
                {
                    control.Anchor = AnchorStyles.Left;
                }
            }
        }

        public bool IsCommanderTotalValid()
        {
            return commanderControls.Sum(c => (int)c.Value) == 100;
        }

        public bool IsKnightTotalValid()
        {
            return knightControls.Sum(c => (int)c.Value) == 100;
        }

        public int GetCommanderTotal()
        {
            return commanderControls.Sum(c => (int)c.Value);
        }

        public int GetKnightTotal()
        {
            return knightControls.Sum(c => (int)c.Value);
        }

        public int GetCommanderDeathChance() => (int)numCommanderDeathChance.Value;
        public int GetKnightDeathChance() => (int)numKnightDeathChance.Value;
    }
}
