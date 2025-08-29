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
                control.ValueChanged += (s, e) => UpdateTotal(commanderControls, lblCommanderTotal);
            }

            foreach (var control in knightControls)
            {
                control.ValueChanged += (s, e) => UpdateTotal(knightControls, lblKnightTotal);
            }

            // Set default values
            SetDefaults();
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
            
            // Adjust commanders table
            tableCommanders.RowStyles.Clear();
            for (int i = 0; i < 9; i++)
            {
                tableCommanders.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            }
            tableCommanders.RowCount = 9; // Ensure row count is explicitly set
            
            // Adjust knights table
            tableKnights.RowStyles.Clear();
            for (int i = 0; i < 9; i++)
            {
                tableKnights.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11F));
            }
            tableKnights.RowCount = 9; // Ensure row count is explicitly set
            
            // Set row positions for total labels
            tableCommanders.SetRow(lblCommanderTotal, 8);
            tableKnights.SetRow(lblKnightTotal, 8);
        }
    }
}
