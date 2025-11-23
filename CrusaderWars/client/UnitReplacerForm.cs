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
                        object nodeTag = attilaKey; // Default tag is just the key

                        if (unit.GetRegimentType() == RegimentType.MenAtArms)
                        {
                            string maxCategory = UnitMappers_BETA.GetMenAtArmMaxCategory(unit.GetName());
                            if (!string.IsNullOrEmpty(maxCategory))
                            {
                                displayName = $"{nameToShow} [{maxCategory}] [{attilaKey}] ({unit.GetSoldiers()} men)";
                            }
                            // Store both key and type for multi-update logic
                            nodeTag = new { OriginalKey = attilaKey, MaAType = unit.GetName() };
                        }

                        var unitNode = new TreeNode(displayName)
                        {
                            Tag = nodeTag
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
                        string displayText = unit.DisplayName;
                        if (unit.UnitType == "MenAtArm" && !string.IsNullOrEmpty(unit.MaxCategory))
                        {
                            displayText += $" [{unit.MaxCategory}]";
                        }
                        if (unit.Rank.HasValue)
                        {
                            displayText += $" [Rank: {unit.Rank.Value}]";
                        }
                        else if (unit.Level.HasValue)
                        {
                            displayText += $" [Level: {unit.Level.Value}]";
                        }
                        displayText += $" [{unit.AttilaUnitKey}]";

                        var unitNode = new TreeNode(displayText)
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


            // This helper function will recursively find all nodes in a tree
            Action<TreeNodeCollection, Action<TreeNode>> TraverseNodes = null;
            TraverseNodes = (nodes, action) =>
            {
                foreach (TreeNode node in nodes)
                {
                    action(node);
                    TraverseNodes(node.Nodes, action);
                }
            };

            // We can replace a single unit, or a whole type of MenAtArm
            if (tvCurrentUnits.SelectedNode.Tag is { } tag)
            {
                string keyToReplace = "";
                string maaTypeToReplace = null;

                if (tag is string s) // It's a General, Knight, etc.
                {
                    keyToReplace = s;
                }
                else // It's a MenAtArm with our complex object
                {
                    dynamic tagObject = tag;
                    keyToReplace = tagObject.OriginalKey;
                    maaTypeToReplace = tagObject.MaAType;
                }

                Replacements[keyToReplace] = replacementKey;

                // Now, update all matching nodes visually
                TraverseNodes(tvCurrentUnits.Nodes, node =>
                {
                    if (node.Tag is { } nodeTag)
                    {
                        bool match = false;
                        if (!string.IsNullOrEmpty(maaTypeToReplace))
                        {
                            // Match by MenAtArm type
                            if (!(nodeTag is string))
                            {
                                dynamic nodeTagObject = nodeTag;
                                if (nodeTagObject.MaAType == maaTypeToReplace)
                                {
                                    match = true;
                                    // Also update the replacement dictionary for this specific key if it's different
                                    if (Replacements.ContainsKey(nodeTagObject.OriginalKey) == false)
                                    {
                                        Replacements[nodeTagObject.OriginalKey] = replacementKey;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Match by simple key (for Generals, Knights)
                            if (nodeTag is string nodeKey && nodeKey == keyToReplace)
                            {
                                match = true;
                            }
                        }

                        if (match)
                        {
                            node.ForeColor = Color.MediumSeaGreen;
                            // Remove old replacement text if it exists
                            int arrowIndex = node.Text.IndexOf(" ->");
                            if (arrowIndex > 0)
                            {
                                node.Text = node.Text.Substring(0, arrowIndex);
                            }
                            node.Text += $" -> {replacementName}";
                        }
                    }
                });
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
