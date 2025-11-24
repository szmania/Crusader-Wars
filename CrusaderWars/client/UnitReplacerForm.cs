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
        public Dictionary<(string originalKey, bool isPlayerAlliance), (string replacementKey, bool isSiege)> Replacements { get; private set; }
        private List<TreeNode> _selectedCurrentNodes = new List<TreeNode>();

        public UnitReplacerForm(List<Unit> currentUnits, List<AvailableUnit> allAvailableUnits, Dictionary<(string originalKey, bool isPlayerAlliance), (string replacementKey, bool isSiege)> existingReplacements)
        {
            InitializeComponent();
            _currentUnits = currentUnits;
            _allAvailableUnits = allAvailableUnits;
            Replacements = new Dictionary<(string, bool), (string, bool)>(existingReplacements);
        }

        private void UnitReplacerForm_Load(object sender, EventArgs e)
        {
            PopulateCurrentUnitsTree();
            PopulateAvailableUnitsTree();
            // After populating, update visuals to show any existing replacements
            UpdateCurrentUnitsTreeVisuals();
            UnitReplacerForm_Resize(this, EventArgs.Empty); // Initial positioning
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
                        string displayName;
                        object nodeTag; // Declare nodeTag without an initial value
                        var regimentType = unit.GetRegimentType(); // Get the regiment type

                        if (regimentType == RegimentType.MenAtArms)
                        {
                            string maxCategory = UnitMappers_BETA.GetMenAtArmMaxCategory(unit.GetName());
                            if (!string.IsNullOrEmpty(maxCategory))
                            {
                                displayName = $"MAA {nameToShow} [{maxCategory}] [{attilaKey}] ({unit.GetSoldiers()} men)";
                            }
                            else
                            {
                                displayName = $"MAA {nameToShow} [{attilaKey}] ({unit.GetSoldiers()} men)";
                            }
                            // Store both key and type for multi-update logic
                            nodeTag = new { OriginalKey = attilaKey, TypeIdentifier = unit.GetName(), RegimentType = regimentType };
                        }
                        else if (regimentType == RegimentType.Commander || regimentType == RegimentType.Knight || regimentType == RegimentType.Levy)
                        {
                            displayName = $"{nameToShow} [{attilaKey}] ({unit.GetSoldiers()} men)";
                            nodeTag = new { OriginalKey = attilaKey, TypeIdentifier = regimentType.ToString(), RegimentType = regimentType };
                        }
                        else // for all other units (e.g., Garrison)
                        {
                            displayName = $"{nameToShow} [{attilaKey}] ({unit.GetSoldiers()} men)";
                            nodeTag = attilaKey;
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
                        if (unit.IsSiege)
                        {
                            displayText += " [SIEGE]";
                        }
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
            if (_selectedCurrentNodes.Count == 0 || tvAvailableUnits.SelectedNode == null)
            {
                MessageBox.Show("Please select one or more units from the 'Current Battle' list and one unit from the 'Available Replacements' list.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (tvAvailableUnits.SelectedNode.Tag == null)
            {
                MessageBox.Show("Please select a specific unit to replace with, not a category.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string replacementKey = tvAvailableUnits.SelectedNode.Tag.ToString();
            bool isSiege = UnitMappers_BETA.IsUnitKeySiege(replacementKey);

            foreach (var selectedNode in _selectedCurrentNodes)
            {
                bool isPlayerAlliance = selectedNode.Parent.Parent.Text == "Player's Alliance";

                if (selectedNode.Tag is string keyToReplace) // Handles individual replacements
                {
                    Replacements[(keyToReplace, isPlayerAlliance)] = (replacementKey, isSiege);
                }
                else // Handles group replacements (MenAtArms, Commander, Knight, Levy)
                {
                    dynamic tagObject = selectedNode.Tag;
                    RegimentType regimentType = tagObject.RegimentType;
                    string typeIdentifier = tagObject.TypeIdentifier;

                    IEnumerable<Unit> unitsToReplace;
                    if (regimentType == RegimentType.MenAtArms)
                    {
                        unitsToReplace = _currentUnits.Where(u => u.GetName() == typeIdentifier && u.IsPlayer() == isPlayerAlliance);
                    }
                    else
                    {
                        unitsToReplace = _currentUnits.Where(u => u.GetRegimentType() == regimentType && u.IsPlayer() == isPlayerAlliance);
                    }

                    foreach (var unit in unitsToReplace)
                    {
                        Replacements[(unit.GetAttilaUnitKey(), isPlayerAlliance)] = (replacementKey, isSiege);
                    }
                }
            }

            UpdateCurrentUnitsTreeVisuals();
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            Replacements.Clear();
            ClearNodeSelection();
            UpdateCurrentUnitsTreeVisuals();
        }

        private void UpdateCurrentUnitsTreeVisuals()
        {
            Action<TreeNodeCollection> TraverseNodes = null;
            TraverseNodes = (nodes) =>
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Tag is { } nodeTag)
                    {
                        string originalKey = (nodeTag is string s) ? s : (string)((dynamic)nodeTag).OriginalKey;
                        bool nodeIsPlayerAlliance = node.Parent?.Parent?.Text == "Player's Alliance";

                        int arrowIndex = node.Text.IndexOf(" ->");
                        if (arrowIndex > 0) node.Text = node.Text.Substring(0, arrowIndex);
                        node.ForeColor = tvCurrentUnits.ForeColor;

                        if (Replacements.TryGetValue((originalKey, nodeIsPlayerAlliance), out var r))
                        {
                            string replacementName = FindAvailableUnitNodeText(r.replacementKey);
                            node.Text += $" -> {replacementName}";
                            node.ForeColor = Color.MediumSeaGreen;
                        }
                    }
                    TraverseNodes(node.Nodes);
                }
            };
            TraverseNodes(tvCurrentUnits.Nodes);
        }

        private string FindAvailableUnitNodeText(string key)
        {
            foreach (TreeNode factionNode in tvAvailableUnits.Nodes)
            {
                foreach (TreeNode typeNode in factionNode.Nodes)
                {
                    foreach (TreeNode unitNode in typeNode.Nodes)
                    {
                        if (unitNode.Tag as string == key)
                        {
                            return unitNode.Text;
                        }
                    }
                }
            }
            return key;
        }

        private void tvCurrentUnits_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void tvCurrentUnits_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag == null) return;

            bool isCtrlPressed = (ModifierKeys & Keys.Control) == Keys.Control;
            bool isShiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;

            if (!isCtrlPressed && !isShiftPressed)
            {
                ClearNodeSelection();
                AddNodeToSelection(e.Node);
            }
            else if (isCtrlPressed)
            {
                if (_selectedCurrentNodes.Contains(e.Node))
                {
                    RemoveNodeFromSelection(e.Node);
                }
                else
                {
                    AddNodeToSelection(e.Node);
                }
            }
            else if (isShiftPressed)
            {
                TreeNode lastSelectedNode = _selectedCurrentNodes.LastOrDefault();
                ClearNodeSelection();

                if (lastSelectedNode != null && lastSelectedNode.TreeView == e.Node.TreeView)
                {
                    SelectRange(lastSelectedNode, e.Node);
                }
                else
                {
                    AddNodeToSelection(e.Node);
                }
            }
        }

        private void ClearNodeSelection()
        {
            foreach (var node in _selectedCurrentNodes)
            {
                node.BackColor = tvCurrentUnits.BackColor;
            }
            _selectedCurrentNodes.Clear();
        }

        private void AddNodeToSelection(TreeNode node)
        {
            if (node != null && !_selectedCurrentNodes.Contains(node) && node.Tag != null)
            {
                _selectedCurrentNodes.Add(node);
                node.BackColor = SystemColors.Highlight;
            }
        }

        private void RemoveNodeFromSelection(TreeNode node)
        {
            if (node != null && _selectedCurrentNodes.Contains(node))
            {
                _selectedCurrentNodes.Remove(node);
                node.BackColor = tvCurrentUnits.BackColor;
            }
        }

        private void SelectRange(TreeNode startNode, TreeNode endNode)
        {
            List<TreeNode> allNodes = new List<TreeNode>();
            Action<TreeNodeCollection> collectNodes = null;
            collectNodes = (nodes) => {
                foreach (TreeNode node in nodes)
                {
                    allNodes.Add(node);
                    collectNodes(node.Nodes);
                }
            };
            collectNodes(tvCurrentUnits.Nodes);

            int startIndex = allNodes.IndexOf(startNode);
            int endIndex = allNodes.IndexOf(endNode);

            if (startIndex > endIndex)
            {
                int temp = startIndex;
                startIndex = endIndex;
                endIndex = temp;
            }

            if (startIndex != -1 && endIndex != -1)
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    AddNodeToSelection(allNodes[i]);
                }
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

        private void UnitReplacerForm_Resize(object sender, EventArgs e)
        {
            // Calculate the center point between the two TreeViews
            int leftTreeViewRight = tvCurrentUnits.Left + tvCurrentUnits.Width;
            int rightTreeViewLeft = tvAvailableUnits.Left;
            int gapWidth = rightTreeViewLeft - leftTreeViewRight;

            // Calculate the new X position for the buttons to be centered in the gap
            int newButtonX = leftTreeViewRight + (gapWidth / 2) - (btnReplace.Width / 2);

            // Apply the new positions
            btnReplace.Left = newButtonX;
            btnUndo.Left = newButtonX;
        }
    }
}
