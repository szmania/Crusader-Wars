using CrusaderWars.data.save_file;
using CrusaderWars.unit_mapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CrusaderWars.client
{
    public partial class UnitReplacerForm : Form
    {
        private readonly List<Unit> _currentUnits;
        private readonly List<AvailableUnit> _allAvailableUnits;
        public Dictionary<string, string> Replacements { get; private set; } = new Dictionary<string, string>();

        public UnitReplacerForm(List<Unit> currentUnits, List<AvailableUnit> allAvailableUnits)
        {
            InitializeComponent();
            _currentUnits = currentUnits;
            _allAvailableUnits = allAvailableUnits;
        }

        private void UnitReplacerForm_Load(object sender, EventArgs e)
        {
            PopulateCurrentUnitsTree();
            PopulateAvailableUnitsTree();
        }

        private void PopulateCurrentUnitsTree()
        {
            tvCurrentUnits.Nodes.Clear();

            var groupedUnits = _currentUnits
                .GroupBy(u => u.IsPlayer() ? "Player's Alliance" : "Enemy's Alliance")
                .OrderBy(g => g.Key);

            foreach (var sideGroup in groupedUnits)
            {
                var sideNode = new TreeNode(sideGroup.Key);
                tvCurrentUnits.Nodes.Add(sideNode);

                var unitsByFaction = sideGroup
                    .GroupBy(u => u.GetAttilaFaction())
                    .OrderBy(g => g.Key);

                foreach (var factionGroup in unitsByFaction)
                {
                    var factionNode = new TreeNode(factionGroup.Key);
                    sideNode.Nodes.Add(factionNode);

                    foreach (var unit in factionGroup.OrderBy(u => u.GetName()))
                    {
                        string nameToShow = string.IsNullOrEmpty(unit.GetLocName()) ? unit.GetName() : unit.GetLocName();
                        string attilaKey = unit.GetAttilaUnitKey();
                        string displayName = $"{nameToShow} [{attilaKey}] ({unit.GetSoldiers()} men)";
                        var unitNode = new TreeNode(displayName)
                        {
                            Tag = unit.GetAttilaUnitKey() // Store the key for replacement logic
                        };
                        factionNode.Nodes.Add(unitNode);
                    }
                }
            }
            tvCurrentUnits.ExpandAll();
        }

        private void PopulateAvailableUnitsTree()
        {
            tvAvailableUnits.Nodes.Clear();

            var unitsByFaction = _allAvailableUnits
                .GroupBy(u => u.FactionName)
                .OrderBy(g => g.Key);

            foreach (var factionGroup in unitsByFaction)
            {
                var factionNode = new TreeNode(factionGroup.Key);
                tvAvailableUnits.Nodes.Add(factionNode);

                var unitsByType = factionGroup
                    .GroupBy(u => u.UnitType)
                    .OrderBy(g => g.Key);

                foreach (var typeGroup in unitsByType)
                {
                    var typeNode = new TreeNode(typeGroup.Key);
                    factionNode.Nodes.Add(typeNode);

                    foreach (var unit in typeGroup.OrderBy(u => u.DisplayName))
                    {
                        var unitNode = new TreeNode(unit.DisplayName)
                        {
                            Tag = unit.AttilaUnitKey // Store the key
                        };
                        typeNode.Nodes.Add(unitNode);
                    }
                }
            }
        }

        private void btnReplace_Click(object sender, EventArgs e)
        {
            if (tvCurrentUnits.SelectedNode == null || tvAvailableUnits.SelectedNode == null)
            {
                MessageBox.Show("Please select one unit from the 'Current Battle' list and one unit from the 'Available Replacements' list.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // The replacement key must be a final unit node, not a category node
            if (tvAvailableUnits.SelectedNode.Tag == null)
            {
                MessageBox.Show("Please select a specific unit to replace with, not a category.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string replacementKey = tvAvailableUnits.SelectedNode.Tag.ToString();
            string replacementName = tvAvailableUnits.SelectedNode.Text;

            
            // Only act on final unit nodes
            if (tvCurrentUnits.SelectedNode.Tag != null)
            {
                string originalKey = tvCurrentUnits.SelectedNode.Tag.ToString();
                Replacements[originalKey] = replacementKey;

                // Visual feedback
                tvCurrentUnits.SelectedNode.ForeColor = Color.MediumSeaGreen;
                // Remove old replacement text if it exists
                int arrowIndex = tvCurrentUnits.SelectedNode.Text.IndexOf(" ->");
                if (arrowIndex > 0)
                {
                    tvCurrentUnits.SelectedNode.Text = tvCurrentUnits.SelectedNode.Text.Substring(0, arrowIndex);
                }
                tvCurrentUnits.SelectedNode.Text += $" -> {replacementName}";
            }
            
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
