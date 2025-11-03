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
                numCommanderMaimed, numCommanderOneLegged, numCommanderOneEyed, numCommanderDisfigured, numCommanderSlain
            };

            knightControls = new List<NumericUpDown>
            {
                numKnightWounded, numKnightSeverelyInjured, numKnightBrutallyMauled,
                numKnightMaimed, numKnightOneLegged, numKnightOneEyed, numKnightDisfigured, numKnightSlain
            };

            // Initial subscription
            SubscribeEventHandlers();
            
            // Set default values
            SetDefaults();
        }

        public void UnsubscribeEventHandlers()
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

        public void SubscribeEventHandlers()
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
            numCommanderWounded.Value = 65;
            numCommanderSeverelyInjured.Value = 10;
            numCommanderBrutallyMauled.Value = 5;
            numCommanderMaimed.Value = 5;
            numCommanderOneLegged.Value = 2;
            numCommanderOneEyed.Value = 3;
            numCommanderDisfigured.Value = 2;
            numCommanderSlain.Value = 8;
            numCommanderPrisoner.Value = 25;

            // Knight Defaults
            numKnightWounded.Value = 65;
            numKnightSeverelyInjured.Value = 10;
            numKnightBrutallyMauled.Value = 5;
            numKnightMaimed.Value = 5;
            numKnightOneLegged.Value = 2;
            numKnightOneEyed.Value = 3;
            numKnightDisfigured.Value = 2;
            numKnightSlain.Value = 8;
            numKnightPrisoner.Value = 25;

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
            toolTip1.SetToolTip(numCommanderSlain, "Chance for a commander to be slain when they fall in battle. This is included in the 100% total chance.");
            toolTip1.SetToolTip(numKnightSlain, "Chance for a knight to be slain when they fall in battle. This is included in the 100% total chance.");
            toolTip1.SetToolTip(lblCommanderPrisoner, "Chance for a character to be taken prisoner if they fall and survive the battle. This is a separate roll and is NOT part of the 100% total for wounds/death.\nCharacters on the losing side have the full chance shown here.\nCharacters on the winning side have a reduced chance (25% of this value).");
            toolTip1.SetToolTip(numCommanderPrisoner, "Chance for a character to be taken prisoner if they fall and survive the battle. This is a separate roll and is NOT part of the 100% total for wounds/death.\nCharacters on the losing side have the full chance shown here.\nCharacters on the winning side have a reduced chance (25% of this value).");
            toolTip1.SetToolTip(lblKnightPrisoner, "Chance for a character to be taken prisoner if they fall and survive the battle. This is a separate roll and is NOT part of the 100% total for wounds/death.\nCharacters on the losing side have the full chance shown here.\nCharacters on the winning side have a reduced chance (25% of this value).");
            toolTip1.SetToolTip(numKnightPrisoner, "Chance for a character to be taken prisoner if they fall and survive the battle. This is a separate roll and is NOT part of the 100% total for wounds/death.\nCharacters on the losing side have the full chance shown here.\nCharacters on the winning side have a reduced chance (25% of this value).");
            
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

        public int GetCommanderSlainChance() => (int)numCommanderSlain.Value;
        public int GetKnightSlainChance() => (int)numKnightSlain.Value;
        public int GetCommanderPrisonerChance() => (int)numCommanderPrisoner.Value;
        public int GetKnightPrisonerChance() => (int)numKnightPrisoner.Value;
    }
}
