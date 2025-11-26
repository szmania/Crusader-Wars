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
        private readonly Dictionary<string, string> _unitScreenNames;
        private List<TreeNode> _currentSearchResults = new List<TreeNode>();
        private int _currentSearchResultIndex = -1;
        private string _lastCurrentSearch = "";
        private List<TreeNode> _availableSearchResults = new List<TreeNode>();
        private int _availableSearchResultIndex = -1;
        private string _lastAvailableSearch = "";

        public UnitReplacerForm(List<Unit> currentUnits, List<AvailableUnit> allAvailableUnits, Dictionary<(string originalKey, bool isPlayerAlliance), (string replacementKey, bool isSiege)> existingReplacements, Dictionary<string, string> unitScreenNames)
        {
            InitializeComponent();
            _currentUnits = currentUnits;
            _allAvailableUnits = allAvailableUnits;
            Replacements = new Dictionary<(string, bool), (string, bool)>(existingReplacements);
            _unitScreenNames = unitScreenNames;
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

                    // --- Process Non-Levy Units ---
                    var nonLevyUnits = factionGroup.Where(u => u.GetRegimentType() != RegimentType.Levy);
                    var groupedForDisplay = nonLevyUnits
                        .GroupBy(u => {
                            var type = u.GetRegimentType();
                            string groupIdentifier = (type == RegimentType.MenAtArms) ? u.GetName() : type.ToString();
                            return new { RegimentType = type, Identifier = groupIdentifier };
                        })
                        .OrderBy(g => g.Key.RegimentType).ThenBy(g => g.Key.Identifier);

                    foreach (var unitGroup in groupedForDisplay)
                    {
                        int unitCount = unitGroup.Count();
                        int totalSoldiers = unitGroup.Sum(u => u.GetSoldiers());
                        string nameToShow = unitGroup.Key.Identifier;
                        var regimentType = unitGroup.Key.RegimentType;

                        string attilaKeyDisplay = "";
                        if (regimentType == RegimentType.MenAtArms || regimentType == RegimentType.Commander || regimentType == RegimentType.Knight)
                        {
                            string key = unitGroup.First().GetAttilaUnitKey();
                            if (!string.IsNullOrEmpty(key) && key != UnitMappers_BETA.NOT_FOUND_KEY)
                            {
                                attilaKeyDisplay = $" [{key}]";
                            }
                        }
                        else if (regimentType == RegimentType.Garrison)
                        {
                            var distinctKeys = new List<string>();
                            for (int level = 1; level <= 20; level++)
                            {
                                var garrisonTuples = UnitMappers_BETA.GetFactionGarrison(factionGroup.Key, level);
                                distinctKeys.AddRange(garrisonTuples.Select(g => g.unit_key));
                            }
                            distinctKeys = distinctKeys.Distinct().ToList();
                            if (distinctKeys.Any())
                            {
                                attilaKeyDisplay = $" [{string.Join(", ", distinctKeys)}]";
                            }
                        }

                        string displayName;
                        if (regimentType == RegimentType.MenAtArms)
                        {
                            string maxCategory = UnitMappers_BETA.GetMenAtArmMaxCategory(nameToShow) ?? "Unit";
                            displayName = $"MAA: {nameToShow}{attilaKeyDisplay} [{maxCategory}] ({unitCount} units, {totalSoldiers} men)";
                        }
                        else
                        {
                            displayName = $"{nameToShow}{attilaKeyDisplay} ({unitCount} units, {totalSoldiers} men)";
                        }

                        var groupNode = new TreeNode(displayName)
                        {
                            Tag = new { TypeIdentifier = nameToShow, RegimentType = regimentType, IsSplitLevyNode = false }
                        };
                        factionNode.Nodes.Add(groupNode);
                    }

                    // --- Process Levy Units Separately ---
                    var levyUnitsInFaction = factionGroup.Where(u => u.GetRegimentType() == RegimentType.Levy).ToList();
                    if (levyUnitsInFaction.Any())
                    {
                        int totalLevySoldiers = levyUnitsInFaction.Sum(u => u.GetSoldiers());
                        var (levyComposition, _) = UnitMappers_BETA.GetFactionLevies(factionGroup.Key);

                        if (levyComposition != null && levyComposition.Any())
                        {
                            int totalPercentage = levyComposition.Sum(l => l.porcentage);
                            if (totalPercentage > 0)
                            {
                                var soldiersPerKey = new Dictionary<string, int>();
                                int assignedSoldiers = 0;

                                // Calculate soldiers for each key based on percentage
                                foreach (var levy in levyComposition)
                                {
                                    int soldiersForKey = (int)Math.Round(totalLevySoldiers * ((double)levy.porcentage / totalPercentage));
                                    soldiersPerKey[levy.unit_key] = soldiersForKey;
                                    assignedSoldiers += soldiersForKey;
                                }

                                // Adjust for rounding errors
                                int remainder = totalLevySoldiers - assignedSoldiers;
                                if (remainder != 0 && soldiersPerKey.Any())
                                {
                                    var largestGroup = soldiersPerKey.OrderByDescending(kvp => kvp.Value).First();
                                    soldiersPerKey[largestGroup.Key] += remainder;
                                }

                                // Create a node for each levy type
                                foreach (var kvp in soldiersPerKey.Where(kvp => kvp.Value > 0).OrderBy(kvp => kvp.Key))
                                {
                                    string levyKey = kvp.Key;
                                    int soldierCount = kvp.Value;
                                    string displayName = $"Levy: [{levyKey}] ({soldierCount} men)";
                                    var levyNode = new TreeNode(displayName)
                                    {
                                        Tag = new { RegimentType = RegimentType.Levy, TypeIdentifier = levyKey, IsSplitLevyNode = true }
                                    };
                                    factionNode.Nodes.Add(levyNode);
                                }
                            }
                        }
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
                    // Correct "Levies" to "Levy" for display
                    string typeDisplayName = typeGroup.Key == "Levies" ? "Levy" : typeGroup.Key;
                    var typeNode = new TreeNode(typeDisplayName);
                    factionNode.Nodes.Add(typeNode);

                    foreach (var unit in typeGroup.OrderBy(u => u.DisplayName))
                    {
                        string displayText;

                        if (unit.UnitType == "MenAtArm")
                        {
                            displayText = unit.DisplayName; // This is the CK3 unit name
                            if (unit.IsSiege)
                            {
                                displayText += " [SIEGE]";
                            }
                            if (!string.IsNullOrEmpty(unit.MaxCategory))
                            {
                                displayText += $" [{unit.MaxCategory}]";
                            }
                            displayText += $" [{unit.AttilaUnitKey}]";
                        }
                        else // General, Knights, Levy, Garrison
                        {
                            displayText = unit.UnitType; // Start with the unit type (e.g., "General", "Knight", "Levy", "Garrison")
                            if (unit.IsSiege)
                            {
                                displayText += " [SIEGE]";
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
                        }

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
                MessageBox.Show("Please select one or more unit groups from the 'Current Battle' list and one unit from the 'Available Replacements' list.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                dynamic tagObject = selectedNode.Tag;
                RegimentType regimentType = tagObject.RegimentType;
                string typeIdentifier = tagObject.TypeIdentifier;
                string faction = selectedNode.Parent.Text; // Get faction from parent node
                bool isSplitLevyNode = tagObject.GetType().GetProperty("IsSplitLevyNode") != null && tagObject.IsSplitLevyNode;

                if (isSplitLevyNode)
                {
                    string originalKey = typeIdentifier; // For split levies, the identifier is the key
                    Replacements[(originalKey, isPlayerAlliance)] = (replacementKey, isSiege);
                }
                else if (regimentType == RegimentType.Garrison)
                {
                    for (int level = 1; level <= 20; level++)
                    {
                        var garrisonTuples = UnitMappers_BETA.GetFactionGarrison(faction, level);
                        foreach (var garrison in garrisonTuples)
                        {
                            Replacements[(garrison.unit_key, isPlayerAlliance)] = (replacementKey, isSiege);
                        }
                    }
                }
                else
                {
                    IEnumerable<Unit> unitsToReplace;
                    if (regimentType == RegimentType.MenAtArms)
                    {
                        unitsToReplace = _currentUnits.Where(u => u.GetName() == typeIdentifier && u.IsPlayer() == isPlayerAlliance && u.GetAttilaFaction() == faction);
                    }
                    else
                    {
                        unitsToReplace = _currentUnits.Where(u => u.GetRegimentType() == regimentType && u.IsPlayer() == isPlayerAlliance && u.GetAttilaFaction() == faction);
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
                        dynamic tag = nodeTag;
                        RegimentType regimentType = tag.RegimentType;
                        string typeIdentifier = tag.TypeIdentifier;
                        bool nodeIsPlayerAlliance = node.Parent?.Parent?.Text == "Player's Alliance";
                        string faction = node.Parent.Text; // Get faction from parent node
                        bool isSplitLevyNode = tag.GetType().GetProperty("IsSplitLevyNode") != null && tag.IsSplitLevyNode;

                        int arrowIndex = node.Text.IndexOf(" ->");
                        if (arrowIndex > 0) node.Text = node.Text.Substring(0, arrowIndex);
                        node.ForeColor = tvCurrentUnits.ForeColor;

                        bool isReplaced = false;
                        (string replacementKey, bool isSiege) replacementInfo = default;

                        if (isSplitLevyNode)
                        {
                            string originalKey = typeIdentifier;
                            if (Replacements.TryGetValue((originalKey, nodeIsPlayerAlliance), out replacementInfo))
                            {
                                isReplaced = true;
                            }
                        }
                        else if (regimentType == RegimentType.Garrison)
                        {
                            // Check for any garrison unit key from this faction and side
                            var garrisonTuples = UnitMappers_BETA.GetFactionGarrison(faction, 1); // Use level 1 as a representative
                            if (garrisonTuples.Any() && Replacements.TryGetValue((garrisonTuples.First().unit_key, nodeIsPlayerAlliance), out replacementInfo))
                            {
                                isReplaced = true;
                            }
                        }
                        else
                        {
                            Unit? representativeUnit = (regimentType == RegimentType.MenAtArms)
                                ? _currentUnits.FirstOrDefault(u => u.GetName() == typeIdentifier && u.IsPlayer() == nodeIsPlayerAlliance && u.GetAttilaFaction() == faction)
                                : _currentUnits.FirstOrDefault(u => u.GetRegimentType() == regimentType && u.IsPlayer() == nodeIsPlayerAlliance && u.GetAttilaFaction() == faction);

                            if (representativeUnit != null && !string.IsNullOrEmpty(representativeUnit.GetAttilaUnitKey()) && Replacements.TryGetValue((representativeUnit.GetAttilaUnitKey(), nodeIsPlayerAlliance), out replacementInfo))
                            {
                                isReplaced = true;
                            }
                        }

                        if (isReplaced)
                        {
                            string replacementName = FindAvailableUnitNodeText(replacementInfo.replacementKey);
                            node.Text += $" -> {replacementName}";
                            node.ForeColor = Color.MediumSeaGreen;
                        }
                    }
                    if (node.Nodes != null && node.Nodes.Count > 0)
                    {
                        TraverseNodes(node.Nodes);
                    }
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
