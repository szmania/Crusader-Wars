using CrusaderWars.data.save_file;
using CrusaderWars.unit_mapper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using CrusaderWars.twbattle;

namespace CrusaderWars.client
{
    [SupportedOSPlatform("windows")]
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
        private readonly Color _searchHighlightColor = Color.Yellow;
        private bool _programmaticSelect = false;
        private HashSet<string> _badUnitKeys = new HashSet<string>(StringComparer.Ordinal);
        // Candidate-preference signal only (never used for marking): keys the backend
        // reports as known-good are preferred when picking bulk replacements.
        private HashSet<string> _goodUnitKeys = new HashSet<string>(StringComparer.Ordinal);
        private const string BuggyMarker = " [BUGGY UNIT?]";
        // Owner-drawn spinner state (no image assets): rotating arc angle in degrees.
        private int _spinnerAngle = 0;

        public UnitReplacerForm(List<Unit> currentUnits, List<AvailableUnit> allAvailableUnits, Dictionary<(string originalKey, bool isPlayerAlliance), (string replacementKey, bool isSiege)> existingReplacements, Dictionary<string, string> unitScreenNames)
        {
            InitializeComponent();
            _currentUnits = currentUnits;
            _allAvailableUnits = allAvailableUnits;

            // Initialize the replacements dictionary
            Replacements = new Dictionary<(string originalKey, bool isPlayerAlliance), (string replacementKey, bool isSiege)>();

            // Initialize with existing replacements passed from BattleState
            if (existingReplacements != null)
            {
                foreach (var kvp in existingReplacements)
                {
                    Replacements[kvp.Key] = kvp.Value;
                }
            }

            _unitScreenNames = unitScreenNames ?? new Dictionary<string, string>();
            txtSearchCurrent.TextChanged += TxtSearchCurrent_TextChanged;
            txtSearchAvailable.TextChanged += TxtSearchAvailable_TextChanged;
            txtSearchCurrent.KeyDown += TxtSearchCurrent_KeyDown;
            txtSearchAvailable.KeyDown += TxtSearchAvailable_KeyDown;

            // Add event handlers for restoring search highlight on deselection
            tvCurrentUnits.BeforeSelect += TreeView_BeforeSelect;
            tvAvailableUnits.BeforeSelect += TreeView_BeforeSelect;
        }

        [SupportedOSPlatform("windows")]
        private async void UnitReplacerForm_Load(object sender, EventArgs e)
        {
            // Show the loading overlay + spinner immediately so the UI never looks frozen.
            ShowLoadingOverlay("Loading unit data…");

            // Start the telemetry fetches FIRST so their HTTP latency overlaps the tree build.
            // Both never throw and return empty lists on failure/telemetry-disabled.
            var badTask = TelemetryClient.GetBadUnitsAsync();
            var goodTask = TelemetryClient.GetGoodUnitsAsync();

            // Build both trees on a single background worker. All UnitMappers_BETA access happens
            // there (never concurrently with the UI thread) because its static caches are not
            // lock-protected. The workers read only immutable inputs and pure static accessors.
            List<TreeNode> currentRoots;
            List<TreeNode> availableRoots;
            try
            {
                var build = await Task.Run(() =>
                {
                    var current = new List<TreeNode>();
                    BuildCurrentUnitsTree(current);
                    var available = new List<TreeNode>();
                    BuildAvailableUnitsTree(available);
                    return (current, available);
                });
                currentRoots = build.current;
                availableRoots = build.available;
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"UnitReplacerForm: failed to build unit trees: {ex.Message}");
                currentRoots = new List<TreeNode>();
                availableRoots = new List<TreeNode>();
            }

            if (IsDisposed) return;

            // Attach on the UI thread under BeginUpdate/EndUpdate so it paints once.
            tvCurrentUnits.BeginUpdate();
            try
            {
                tvCurrentUnits.Nodes.Clear();
                tvCurrentUnits.Nodes.AddRange(currentRoots.ToArray());
                tvCurrentUnits.ExpandAll();
            }
            finally { tvCurrentUnits.EndUpdate(); }

            tvAvailableUnits.BeginUpdate();
            try
            {
                tvAvailableUnits.Nodes.Clear();
                tvAvailableUnits.Nodes.AddRange(availableRoots.ToArray());
            }
            finally { tvAvailableUnits.EndUpdate(); }

            // After populating, update visuals to show any existing replacements.
            UpdateCurrentUnitsTreeVisuals();
            UnitReplacerForm_Resize(this, EventArgs.Empty); // Initial positioning

            // Now await the telemetry results (already overlapped with the tree build).
            UpdateLoadingOverlayText("Loading buggy-unit data…");
            await Task.WhenAll(badTask, goodTask);
            if (IsDisposed) return;

            _badUnitKeys = new HashSet<string>(badTask.Result, StringComparer.Ordinal);
            _goodUnitKeys = new HashSet<string>(goodTask.Result, StringComparer.Ordinal);

            // Lightweight marker-only passes (NOT a full visual refresh): the full pass
            // re-runs per-garrison composition lookups and a full text rebuild, which is
            // what the user perceived as a hang on the telemetry call.
            ApplyBadUnitMarkersToCurrentTree();
            ApplyBadUnitMarkersToAvailableTree();

            HideLoadingOverlay(_badUnitKeys.Any()
                ? $"{_badUnitKeys.Count} potentially-buggy unit key(s) loaded"
                : "No buggy-unit data");
        }

        // ------------------------------------------------------------------
        // Loading overlay + owner-drawn circular spinner (no image assets)
        // ------------------------------------------------------------------

        private void ShowLoadingOverlay(string message)
        {
            PositionLoadingOverlay();
            loadingOverlayLabel.Text = message;
            loadingOverlayPanel.Visible = true;
            loadingOverlayPanel.BringToFront();
            _spinnerAngle = 0;
            loadingSpinnerTimer.Start();
        }

        private void PositionLoadingOverlay()
        {
            if (loadingOverlayPanel.IsDisposed) return;
            // Centre the overlay over the tree area (both TreeViews span the same band).
            int treeTop = Math.Min(tvCurrentUnits.Top, tvAvailableUnits.Top);
            int treeHeight = Math.Max(tvCurrentUnits.Height, tvAvailableUnits.Height);
            loadingOverlayPanel.Left = (ClientSize.Width - loadingOverlayPanel.Width) / 2;
            loadingOverlayPanel.Top = treeTop + Math.Max(0, (treeHeight - loadingOverlayPanel.Height) / 2);
        }

        private void UpdateLoadingOverlayText(string message)
        {
            if (loadingOverlayPanel.IsDisposed || loadingOverlayLabel.IsDisposed) return;
            loadingOverlayLabel.Text = message;
        }

        private async void HideLoadingOverlay(string finalMessage)
        {
            loadingSpinnerTimer.Stop();
            if (loadingOverlayPanel.IsDisposed) return;
            loadingOverlayLabel.Text = finalMessage;
            // Brief completion beat so the user sees the final status before the overlay hides.
            await Task.Delay(1000);
            if (loadingOverlayPanel.IsDisposed) return;
            loadingOverlayPanel.Visible = false;
        }

        private void LoadingSpinnerTimer_Tick(object? sender, EventArgs e)
        {
            _spinnerAngle = (_spinnerAngle + 30) % 360;
            loadingOverlayPanel.Invalidate();
        }

        private void LoadingOverlayPanel_Paint(object? sender, PaintEventArgs e)
        {
            // Owner-drawn rotating arc: a 270-degree sweep so it reads as a spinner.
            var rect = new Rectangle(
                (loadingOverlayPanel.Width - 48) / 2,
                12,
                48, 48);
            using (var pen = new Pen(Color.SteelBlue, 4f))
            {
                e.Graphics.DrawArc(pen, rect, _spinnerAngle, 270);
            }
        }

        /// <summary>
        /// Builds the "Current Battle Units" root nodes. Runs on a background worker: touches only
        /// the immutable _currentUnits/_allAvailableUnits and pure static UnitMappers_BETA accessors.
        /// </summary>
        private void BuildCurrentUnitsTree(List<TreeNode> roots)
        {
            var groupedUnits = _currentUnits
                .GroupBy(u => u.IsPlayer() ? "Player's Alliance" : "Enemy's Alliance")
                .OrderBy(g => g.Key);

            foreach (var sideGroup in groupedUnits)
            {
                var sideNode = new TreeNode(sideGroup.Key);

                var unitsByFaction = sideGroup
                    .GroupBy(u => u.GetAttilaFaction())
                    .OrderBy(g => g.Key);

                foreach (var factionGroup in unitsByFaction)
                {
                    var factionNode = new TreeNode(factionGroup.Key);
                    sideNode.Nodes.Add(factionNode);

                    // --- Process Non-Levy, Non-Garrison Units ---
                    var nonLevyUnits = factionGroup.Where(u => u.GetRegimentType() != RegimentType.Levy && u.GetRegimentType() != RegimentType.Garrison);
                    var groupedForDisplay = nonLevyUnits
                        .GroupBy(u =>
                        {
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
                        string nodeAttilaKey = "";
                        if (regimentType == RegimentType.MenAtArms || regimentType == RegimentType.Commander || regimentType == RegimentType.Knight)
                        {
                            string originalAttilaKey = unitGroup.First().GetAttilaUnitKey();
                            (string replacementKey, bool isSiege) replacementInfo = default;
                            bool isReplaced = Replacements.TryGetValue((originalAttilaKey, unitGroup.First().IsPlayer()), out replacementInfo);

                            string keyToDisplay = isReplaced ? replacementInfo.replacementKey : originalAttilaKey;
                            nodeAttilaKey = keyToDisplay;

                            if (!string.IsNullOrEmpty(keyToDisplay) && keyToDisplay != UnitMappers_BETA.NOT_FOUND_KEY)
                            {
                                if (_unitScreenNames.TryGetValue(keyToDisplay, out var screenName))
                                {
                                    attilaKeyDisplay = $" [{screenName}] [{keyToDisplay}]";
                                }
                                else
                                {
                                    attilaKeyDisplay = $" [{keyToDisplay}]";
                                }
                            }
                        }

                        string displayName;
                        if (regimentType == RegimentType.MenAtArms)
                        {
                            string maxCategory = UnitMappers_BETA.GetMenAtArmMaxCategory(nameToShow) ?? "Unit";
                            displayName = $"MAA-{nameToShow}: {attilaKeyDisplay} [{maxCategory}] ({unitCount} units, {totalSoldiers} men)";
                        }
                        else
                        {
                            displayName = $"{nameToShow}:{attilaKeyDisplay} ({unitCount} units, {totalSoldiers} men)";
                        }

                        var groupNode = new TreeNode(displayName)
                        {
                            Tag = new { TypeIdentifier = nameToShow, RegimentType = regimentType, IsSplitLevyNode = false, AttilaKey = nodeAttilaKey }
                        };
                        factionNode.Nodes.Add(groupNode);
                    }

                    // --- Process Levy Units Separately ---
                    var levyUnitsInFaction = factionGroup.Where(u => u.GetRegimentType() == RegimentType.Levy).ToList();
                    if (levyUnitsInFaction.Any())
                    {
                        int totalLevySoldiers = levyUnitsInFaction.Sum(u => u.GetSoldiers());
                        List<(int percentage, string unit_key, string name, string max)>? levyComposition = null;
                        try
                        {
                            var (composition, _) = UnitMappers_BETA.GetFactionLevies(factionGroup.Key);
                            levyComposition = composition;
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.Debug($"Error getting faction levies for {factionGroup.Key}: {ex.Message}");
                            levyComposition = null;
                        }

                        if (levyComposition != null && levyComposition.Any())
                        {
                            int totalPercentage = levyComposition.Sum(l => l.percentage);
                            if (totalPercentage > 0)
                            {
                                var soldiersPerKey = new Dictionary<string, int>();
                                int assignedSoldiers = 0;

                                // Calculate soldiers for each key based on percentage
                                foreach (var levy in levyComposition)
                                {
                                    int soldiersForKey = (int)Math.Round(totalLevySoldiers * ((double)levy.percentage / totalPercentage));
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
                                    string screenNameDisplay = _unitScreenNames.TryGetValue(levyKey, out var sn) ? sn : levyKey;
                                    string displayName = $"Levy: [{screenNameDisplay}] [{levyKey}] ({kvp.Value} men)";
                                    var levyNode = new TreeNode(displayName)
                                    {
                                        Tag = new { RegimentType = RegimentType.Levy, TypeIdentifier = levyKey, IsSplitLevyNode = true, AttilaKey = levyKey }
                                    };
                                    factionNode.Nodes.Add(levyNode);
                                }
                            }
                        }
                    }

                    // --- Process Garrison Units Separately ---
                    var garrisonUnitsInFaction = factionGroup.Where(u => u.GetRegimentType() == RegimentType.Garrison).ToList();
                    if (garrisonUnitsInFaction.Any())
                    {
                        int totalGarrisonSoldiers = garrisonUnitsInFaction.Sum(u => u.GetSoldiers());
                        var garrisonComposition = new List<(string unit_key, string name)>();
                        try
                        {
                            // Single pass over the cached garrison definitions (was 20x GetFactionGarrison).
                            garrisonComposition = UnitMappers_BETA.GetFactionGarrisonComposition(factionGroup.Key);
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.Debug($"Error getting faction garrisons for {factionGroup.Key}: {ex.Message}");
                            garrisonComposition = null;
                        }

                        if (garrisonComposition != null && garrisonComposition.Any())
                        {
                            var soldiersPerKey = new Dictionary<string, int>();
                            int assignedSoldiers = 0;

                            // Distribute soldiers evenly among available garrison unit types
                            if (garrisonComposition.Count > 0)
                            {
                                int soldiersPerUnitType = totalGarrisonSoldiers / garrisonComposition.Count;
                                foreach (var garrison in garrisonComposition)
                                {
                                    soldiersPerKey[garrison.unit_key] = soldiersPerUnitType;
                                    assignedSoldiers += soldiersPerUnitType;
                                }

                                // Adjust for rounding errors
                                int remainder = totalGarrisonSoldiers - assignedSoldiers;
                                if (remainder != 0 && soldiersPerKey.Any())
                                {
                                    var firstKey = soldiersPerKey.Keys.First();
                                    soldiersPerKey[firstKey] += remainder;
                                }
                            }

                            // Create a node for each garrison type
                            foreach (var kvp in soldiersPerKey.Where(kvp => kvp.Value > 0).OrderBy(kvp => kvp.Key))
                            {
                                string garrisonKey = kvp.Key;
                                string screenNameDisplay = _unitScreenNames.TryGetValue(garrisonKey, out var sn) ? sn : garrisonKey;
                                string displayName = $"Garrison: [{screenNameDisplay}] [{garrisonKey}] ({kvp.Value} men)";
                                var garrisonNode = new TreeNode(displayName)
                                {
                                    Tag = new { RegimentType = RegimentType.Garrison, TypeIdentifier = garrisonKey, IsSplitLevyNode = true, AttilaKey = garrisonKey } // Re-using IsSplitLevyNode for similar behavior
                                };
                                factionNode.Nodes.Add(garrisonNode);
                            }
                        }
                    }
                }
                roots.Add(sideNode);
            }
        }

        /// <summary>
        /// Idempotently marks left-tree leaf nodes whose effective Attila unit key is
        /// known-bad with the buggy marker and red text. Leaf-only, no composition
        /// lookups, no text rebuild — safe to run after the async telemetry fetch.
        /// </summary>
        private void ApplyBadUnitMarkersToCurrentTree()
        {
            if (!_badUnitKeys.Any()) return;

            Action<TreeNodeCollection> traverse = null;
            traverse = (nodes) =>
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Tag is { } nodeTag)
                    {
                        dynamic tag = nodeTag;
                        string nodeAttilaKey = tag.AttilaKey;
                        if (!string.IsNullOrEmpty(nodeAttilaKey) && _badUnitKeys.Contains(nodeAttilaKey))
                        {
                            if (!node.Text.EndsWith(BuggyMarker, StringComparison.Ordinal)) node.Text += BuggyMarker;
                            node.ForeColor = Color.Red;
                        }
                    }
                    if (node.Nodes != null && node.Nodes.Count > 0) traverse(node.Nodes);
                }
            };
            traverse(tvCurrentUnits.Nodes);
        }

        /// <summary>
        /// Builds the "Available Replacement Units" root nodes. Runs on a background worker:
        /// touches only the immutable _allAvailableUnits and _unitScreenNames.
        /// </summary>
        private void BuildAvailableUnitsTree(List<TreeNode> roots)
        {
            var unitsByFaction = _allAvailableUnits
                .GroupBy(u => u.FactionName)
                .OrderBy(g => g.Key);

            foreach (var factionGroup in unitsByFaction)
            {
                var factionNode = new TreeNode(factionGroup.Key);

                var unitsByType = factionGroup
                    .GroupBy(u => u.UnitType)
                    .OrderBy(g => g.Key);

                foreach (var typeGroup in unitsByType)
                {
                    // Correct "Levies" to "Levy" for display
                    string typeDisplayName = typeGroup.Key == "Levies" ? "Levy" : typeGroup.Key;
                    var typeNode = new TreeNode(typeDisplayName);
                    factionNode.Nodes.Add(typeNode);

                    IOrderedEnumerable<AvailableUnit> sortedUnits;
                    switch (typeGroup.Key)
                    {
                        case "Garrison":
                            sortedUnits = typeGroup.OrderBy(u => u.Level);
                            break;
                        case "General":
                        case "Knights":
                            sortedUnits = typeGroup.OrderBy(u => u.Rank);
                            break;
                        default:
                            sortedUnits = typeGroup.OrderBy(u => u.DisplayName);
                            break;
                    }

                    foreach (var unit in sortedUnits)
                    {
                        string screenName = _unitScreenNames.TryGetValue(unit.AttilaUnitKey, out var sn) ? sn : unit.AttilaUnitKey;
                        string displayText;

                        if (unit.UnitType == "MenAtArm")
                        {
                            displayText = $"{unit.DisplayName} [{screenName}] [{unit.AttilaUnitKey}]";
                            if (!string.IsNullOrEmpty(unit.MaxCategory)) displayText += $" [{unit.MaxCategory}]";
                            if (unit.IsSiege) displayText += " [SIEGE]";
                        }
                        else // General, Knights, Levy, Garrison
                        {
                            displayText = $"{screenName} [{unit.AttilaUnitKey}]";
                            if (unit.IsSiege) displayText += " [SIEGE]";
                            if (unit.Rank.HasValue) displayText += $" [Rank: {unit.Rank.Value}]";
                            else if (unit.Level.HasValue) displayText += $" [Level: {unit.Level.Value}]";
                        }

                        var unitNode = new TreeNode(displayText)
                        {
                            Tag = unit.AttilaUnitKey // Store the key
                        };
                        if (_badUnitKeys.Contains(unit.AttilaUnitKey))
                        {
                            unitNode.Text += BuggyMarker;
                            unitNode.ForeColor = Color.Red;
                        }
                        typeNode.Nodes.Add(unitNode);
                    }
                }
                roots.Add(factionNode);
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

            string? replacementKey = tvAvailableUnits.SelectedNode.Tag.ToString();
            if (replacementKey is null) { return; }
            bool isSiege = UnitMappers_BETA.IsUnitKeySiege(replacementKey);

            foreach (var selectedNode in _selectedCurrentNodes)
            {
                bool isPlayerAlliance = selectedNode.Parent.Parent.Text == "Player's Alliance";
                dynamic? tagObject = selectedNode.Tag;
                if (tagObject is null) { continue; }
                RegimentType regimentType = tagObject.RegimentType;
                string typeIdentifier = tagObject.TypeIdentifier;
                string faction = selectedNode.Parent?.Text ?? ""; // Get faction from parent node
                bool isSplitLevyNode = tagObject.GetType().GetProperty("IsSplitLevyNode") != null && tagObject.IsSplitLevyNode;

                if (isSplitLevyNode)
                {
                    string originalKey = typeIdentifier; // For split levies, the identifier is the key
                    Replacements[(originalKey, isPlayerAlliance)] = (replacementKey, isSiege);
                }
                else if (regimentType == RegimentType.Garrison)
                    // Single pass over the cached garrison definitions (was 20x GetFactionGarrison).
                    // The union over levels 1..20 equals the full distinct composition, so the set
                    // of registered keys is unchanged.
                    foreach (var garrison in UnitMappers_BETA.GetFactionGarrisonComposition(faction))
                    {
                        Replacements[(garrison.unit_key, isPlayerAlliance)] = (replacementKey, isSiege);
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
            BattleState.ClearManualUnitReplacements();
            ClearNodeSelection();
            UpdateCurrentUnitsTreeVisuals();
        }

        private void btnReplaceAllBuggy_Click(object sender, EventArgs e)
        {
            ReplaceAllBuggyUnits();
        }

        /// <summary>
        /// Replaces every known-buggy unit group in the battle with a non-buggy unit of
        /// similar type. Fallback chain per buggy node: same faction + same unit type,
        /// then heritage-sibling factions, then "Default", then any faction. Registers
        /// via the existing Replacements dictionary (same semantics as btnReplace_Click).
        /// </summary>
        private void ReplaceAllBuggyUnits()
        {
            if (!_badUnitKeys.Any())
            {
                MessageBox.Show("No buggy-unit data is available. The backend may be unreachable or telemetry is disabled.", "No Buggy Units", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Collect all leaf nodes in the current tree whose EFFECTIVE key is known-bad.
            var buggyNodes = new List<TreeNode>();
            Action<TreeNodeCollection> collect = null;
            collect = (nodes) =>
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Tag is { } nodeTag)
                    {
                        dynamic tag = nodeTag;
                        string nodeAttilaKey = tag.AttilaKey;
                        bool isPlayerAlliance = node.Parent?.Parent?.Text == "Player's Alliance";

                        string faction = node.Parent?.Text ?? "";
                        bool isSplitLevyNode = tag.GetType().GetProperty("IsSplitLevyNode") != null && tag.IsSplitLevyNode;
                        RegimentType regimentType = tag.RegimentType;
                        string typeIdentifier = tag.TypeIdentifier;

                        // Effective key: post-replacement key if already replaced, else the node's own key.
                        string effectiveKey = nodeAttilaKey;
                        if (Replacements.TryGetValue((nodeAttilaKey, isPlayerAlliance), out var existing))
                        {
                            effectiveKey = existing.replacementKey;
                        }

                        if (!string.IsNullOrEmpty(effectiveKey) && effectiveKey != UnitMappers_BETA.NOT_FOUND_KEY && _badUnitKeys.Contains(effectiveKey))
                        {
                            buggyNodes.Add(node);
                        }
                    }
                    if (node.Nodes != null && node.Nodes.Count > 0) collect(node.Nodes);
                }
            };
            collect(tvCurrentUnits.Nodes);

            if (!buggyNodes.Any())
            {
                MessageBox.Show("No buggy units were found in the current battle.", "No Buggy Units", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var replaced = new List<string>();
            var unreplaceable = new List<string>();

            foreach (var node in buggyNodes)
            {
                dynamic tag = node.Tag;
                string nodeAttilaKey = tag.AttilaKey;
                bool isPlayerAlliance = node.Parent?.Parent?.Text == "Player's Alliance";
                string faction = node.Parent?.Text ?? "";
                bool isSplitLevyNode = tag.GetType().GetProperty("IsSplitLevyNode") != null && tag.IsSplitLevyNode;
                RegimentType regimentType = tag.RegimentType;
                string typeIdentifier = tag.TypeIdentifier;

                // Effective key (post-replacement) is what we are replacing away from.
                string effectiveKey = nodeAttilaKey;
                if (Replacements.TryGetValue((nodeAttilaKey, isPlayerAlliance), out var existing))
                {
                    effectiveKey = existing.replacementKey;
                }

                string unitType = regimentType switch
                {
                    RegimentType.Levy => "Levies",
                    RegimentType.Garrison => "Garrison",
                    RegimentType.Commander => "General",
                    RegimentType.Knight => "Knights",
                    _ => "MenAtArm"
                };

                // Determine the suspect AvailableUnit (for MaxCategory / Rank / Level / IsSiege context).
                AvailableUnit? suspect = _allAvailableUnits.FirstOrDefault(u =>
                    u.FactionName.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                    u.AttilaUnitKey.Equals(effectiveKey, StringComparison.Ordinal));

                AvailableUnit? replacement = PickBulkReplacement(faction, unitType, suspect, regimentType);
                if (replacement == null)
                {
                    unreplaceable.Add(effectiveKey);
                    continue;
                }

                bool isSiege = UnitMappers_BETA.IsUnitKeySiege(replacement.AttilaUnitKey);

                if (isSplitLevyNode)
                {
                    Replacements[(typeIdentifier, isPlayerAlliance)] = (replacement.AttilaUnitKey, isSiege);
                }
                else if (regimentType == RegimentType.Garrison)
                {
                    // Per-key replacement (preserves composition), matching the garrison
                    // per-key lookup in UpdateCurrentUnitsTreeVisuals.
                    Replacements[(nodeAttilaKey, isPlayerAlliance)] = (replacement.AttilaUnitKey, isSiege);
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

                    bool registered = false;
                    foreach (var unit in unitsToReplace)
                    {
                        if (string.IsNullOrEmpty(unit.GetAttilaUnitKey())) continue;
                        Replacements[(unit.GetAttilaUnitKey(), isPlayerAlliance)] = (replacement.AttilaUnitKey, isSiege);
                        registered = true;
                    }

                    if (!registered)
                    {
                        unreplaceable.Add(effectiveKey);
                        continue;
                    }
                }

                replaced.Add($"{effectiveKey} -> {replacement.AttilaUnitKey}");
            }

            UpdateCurrentUnitsTreeVisuals();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Replaced {replaced.Count} buggy unit group(s):");
            foreach (var line in replaced) sb.AppendLine($"  {line}");
            if (unreplaceable.Any())
            {
                sb.AppendLine();
                sb.AppendLine("Could not find a replacement for:");
                foreach (var key in unreplaceable.Distinct()) sb.AppendLine($"  {key}");
            }
            MessageBox.Show(sb.ToString(), "Replace All Buggy Units", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Picks the best non-buggy replacement for a suspect unit using the fallback chain:
        /// L1 same faction + same unit type, L2 heritage-sibling factions, L3 "Default",
        /// L4 any faction. Within the pool, prefer known-good keys, then type-specific
        /// tiebreaks, then alphabetical key.
        /// </summary>
        private AvailableUnit? PickBulkReplacement(string faction, string unitType, AvailableUnit? suspect, RegimentType regimentType)
        {
            var allOfType = _allAvailableUnits.Where(u => u.UnitType.Equals(unitType, StringComparison.OrdinalIgnoreCase)).ToList();

            // Build the fallback chain of candidate pools (stop at first non-empty).
            var pools = new List<List<AvailableUnit>>();

            var sameFaction = allOfType.Where(u => u.FactionName.Equals(faction, StringComparison.OrdinalIgnoreCase)).ToList();
            pools.Add(sameFaction);

            string heritage = UnitMappers_BETA.GetHeritageByFaction(faction);
            if (!string.IsNullOrEmpty(heritage))
            {
                var siblingFactions = new HashSet<string>(UnitMappers_BETA.GetFactionsByHeritage(heritage), StringComparer.OrdinalIgnoreCase);
                siblingFactions.Remove(faction);
                pools.Add(allOfType.Where(u => siblingFactions.Contains(u.FactionName)).ToList());
            }

            pools.Add(allOfType.Where(u => u.FactionName.Equals("Default", StringComparison.OrdinalIgnoreCase) || u.FactionName.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase)).ToList());
            pools.Add(allOfType);

            foreach (var pool in pools)
            {
                var nonBuggy = pool.Where(u => !_badUnitKeys.Contains(u.AttilaUnitKey)).ToList();
                if (!nonBuggy.Any()) continue;

                var best = PickBestFromPool(nonBuggy, suspect, regimentType);
                if (best != null) return best;
            }

            return null;
        }

        private AvailableUnit? PickBestFromPool(List<AvailableUnit> pool, AvailableUnit? suspect, RegimentType regimentType)
        {
            // Prefer known-good keys first.
            var good = pool.Where(u => _goodUnitKeys.Contains(u.AttilaUnitKey)).ToList();
            var candidates = good.Any() ? good : pool;

            switch (regimentType)
            {
                case RegimentType.MenAtArms:
                    return candidates
                        .OrderBy(u => u.IsSiege != (suspect?.IsSiege ?? false))
                        .ThenBy(u => string.Equals(u.MaxCategory, suspect?.MaxCategory, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                        .ThenBy(u => u.AttilaUnitKey)
                        .FirstOrDefault();

                case RegimentType.Commander:
                case RegimentType.Knight:
                    int requiredRank = 1;
                    if (regimentType == RegimentType.Commander)
                    {
                        var rep = _currentUnits.FirstOrDefault(u => u.GetRegimentType() == RegimentType.Commander);
                        int charRank = rep?.CharacterRank ?? 1;
                        requiredRank = charRank >= 4 ? 3 : (charRank == 3 ? 2 : 1);
                    }
                    else
                    {
                        var rep = _currentUnits.FirstOrDefault(u => u.GetRegimentType() == RegimentType.Knight);
                        requiredRank = rep?.CharacterRank ?? 1;
                    }

                    var suitable = candidates.Where(u => (u.Rank ?? 0) <= requiredRank).ToList();
                    if (suitable.Any())
                        return suitable.OrderByDescending(u => u.Rank).ThenBy(u => u.AttilaUnitKey).FirstOrDefault();
                    return candidates.OrderBy(u => u.Rank).ThenBy(u => u.AttilaUnitKey).FirstOrDefault();

                case RegimentType.Garrison:
                    int suspectLevel = suspect?.Level ?? 1;
                    var sameLevel = candidates.Where(u => (u.Level ?? 0) == suspectLevel).ToList();
                    if (sameLevel.Any())
                        return sameLevel.OrderBy(u => u.AttilaUnitKey).FirstOrDefault();
                    var atOrBelow = candidates.Where(u => (u.Level ?? 0) <= suspectLevel).ToList();
                    if (atOrBelow.Any())
                        return atOrBelow.OrderByDescending(u => u.Level).ThenBy(u => u.AttilaUnitKey).FirstOrDefault();
                    return candidates.OrderBy(u => u.Level).ThenBy(u => u.AttilaUnitKey).FirstOrDefault();

                default: // Levies
                    return candidates.OrderBy(u => u.AttilaUnitKey).FirstOrDefault();
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure you want to reset all saved unit replacements? This cannot be undone.",
                                             "Confirm Reset",
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Warning);
            if (confirmResult == DialogResult.Yes)
            {
                Replacements.Clear();
                BattleState.ClearManualUnitReplacements(); // This will clear and save
                UpdateCurrentUnitsTreeVisuals();
                MessageBox.Show("All saved unit replacements have been cleared.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateCurrentUnitsTreeVisuals()
        {
            ClearAvailableReplacementHighlights();
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
                        string nodeAttilaKey = tag.AttilaKey;
                        bool nodeIsPlayerAlliance = node.Parent?.Parent?.Text == "Player's Alliance";
                        string faction = node.Parent?.Text ?? ""; // Get faction from parent node
                        bool isSplitLevyNode = tag.GetType().GetProperty("IsSplitLevyNode") != null && tag.IsSplitLevyNode;

                        int markerIndex = node.Text.IndexOf(BuggyMarker, StringComparison.Ordinal);
                        if (markerIndex > 0) node.Text = node.Text.Substring(0, markerIndex);
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
                            // Per-key lookup on the node's OWN key (not a level-1 representative) so
                            // per-key garrison replacements from the bulk button show green arrows
                            // and drop the red marker. Pre-existing all-levels entries still hit
                            // because they set every level key.
                            if (!string.IsNullOrEmpty(nodeAttilaKey) && Replacements.TryGetValue((nodeAttilaKey, nodeIsPlayerAlliance), out replacementInfo))
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
                            var (replacementFaction, replacementName) = FindAvailableUnitNodeText(replacementInfo.replacementKey);
                            node.Text += $" -> [{replacementFaction}] {replacementName}";
                            node.ForeColor = Color.MediumSeaGreen;
                            HighlightAvailableUnitNode(replacementInfo.replacementKey);
                        }

                        // Red overrides green: mark nodes whose effective (post-replacement) key is known-bad.
                        string effectiveKey = isReplaced ? replacementInfo.replacementKey : nodeAttilaKey;
                        if (!string.IsNullOrEmpty(effectiveKey) && _badUnitKeys.Contains(effectiveKey))
                        {
                            if (!node.Text.EndsWith(BuggyMarker, StringComparison.Ordinal)) node.Text += BuggyMarker;
                            node.ForeColor = Color.Red;
                        }
                        }
                    if (node.Nodes != null && node.Nodes.Count > 0)
                    {
                        TraverseNodes(node.Nodes);
                    }
                }
            };
            TraverseNodes(tvCurrentUnits.Nodes);
            ApplyBadUnitMarkersToAvailableTree();
        }

        /// <summary>
        /// Idempotently marks right-tree leaf nodes whose Attila unit key is known-bad
        /// with the buggy marker and red text. Re-run after any full color reset.
        /// </summary>
        private void ApplyBadUnitMarkersToAvailableTree()
        {
            if (!_badUnitKeys.Any()) return;
            foreach (TreeNode factionNode in tvAvailableUnits.Nodes)
            {
                foreach (TreeNode typeNode in factionNode.Nodes)
                {
                    foreach (TreeNode unitNode in typeNode.Nodes)
                    {
                        if (unitNode.Tag is string key && _badUnitKeys.Contains(key))
                        {
                            if (!unitNode.Text.EndsWith(BuggyMarker, StringComparison.Ordinal)) unitNode.Text += BuggyMarker;
                            unitNode.ForeColor = Color.Red;
                        }
                    }
                }
            }
        }

        private (string faction, string unitText) FindAvailableUnitNodeText(string key)
        {
            foreach (TreeNode factionNode in tvAvailableUnits.Nodes)
            {
                foreach (TreeNode typeNode in factionNode.Nodes)
                {
                    foreach (TreeNode unitNode in typeNode.Nodes)
                    {
                        if (unitNode.Tag as string == key)
                        {
                            return (factionNode.Text, unitNode.Text.Replace(BuggyMarker, ""));
                        }
                    }
                }
            }
            return ("Unknown Faction", key);
        }

        private void HighlightAvailableUnitNode(string key)
        {
            foreach (TreeNode factionNode in tvAvailableUnits.Nodes)
            {
                foreach (TreeNode typeNode in factionNode.Nodes)
                {
                    foreach (TreeNode unitNode in typeNode.Nodes)
                    {
                        if (unitNode.Tag as string == key)
                        {
                            unitNode.ForeColor = Color.MediumSeaGreen;
                            return;
                        }
                    }
                }
            }
        }



        private void tvCurrentUnits_BeforeSelect(object? sender, TreeViewCancelEventArgs e)
        {
            // Allow programmatic selection for search navigation, but cancel manual user clicks
            // to preserve the custom multi-selection behavior.
            if (!_programmaticSelect)
            {
                e.Cancel = true;
            }
        }

        private void tvCurrentUnits_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
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

        private void ClearAvailableReplacementHighlights()
        {
            foreach (TreeNode factionNode in tvAvailableUnits.Nodes)
            {
                foreach (TreeNode typeNode in factionNode.Nodes)
                {
                    foreach (TreeNode unitNode in typeNode.Nodes)
                    {
                        unitNode.BackColor = tvAvailableUnits.BackColor;
                        unitNode.ForeColor = tvAvailableUnits.ForeColor;
                    }
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
            collectNodes = (nodes) =>
            {
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

        private void btnOK_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }


        private void UnitReplacerForm_Resize(object? sender, EventArgs e)
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
            btnReset.Left = newButtonX;
            btnReplaceAllBuggy.Left = newButtonX;

            // Keep the loading overlay centred over the tree area while the form resizes.
            PositionLoadingOverlay();
        }


        // SEARCH =================================================================================================

        private void btnNextCurrent_Click(object? sender, EventArgs e)
        {
            SearchInTreeView(tvCurrentUnits, txtSearchCurrent.Text, ref _currentSearchResults, ref _currentSearchResultIndex, ref _lastCurrentSearch);
            NavigateSearchResults(tvCurrentUnits, _currentSearchResults, ref _currentSearchResultIndex, true);
        }

        private void btnPrevCurrent_Click(object? sender, EventArgs e)
        {
            SearchInTreeView(tvCurrentUnits, txtSearchCurrent.Text, ref _currentSearchResults, ref _currentSearchResultIndex, ref _lastCurrentSearch);
            NavigateSearchResults(tvCurrentUnits, _currentSearchResults, ref _currentSearchResultIndex, false);
        }


        private void btnNextAvailable_Click(object? sender, EventArgs e)
        {
            SearchInTreeView(tvAvailableUnits, txtSearchAvailable.Text, ref _availableSearchResults, ref _availableSearchResultIndex, ref _lastAvailableSearch);
            NavigateSearchResults(tvAvailableUnits, _availableSearchResults, ref _availableSearchResultIndex, true);
        }

        private void btnPrevAvailable_Click(object? sender, EventArgs e)
        {
            SearchInTreeView(tvAvailableUnits, txtSearchAvailable.Text, ref _availableSearchResults, ref _availableSearchResultIndex, ref _lastAvailableSearch);
            NavigateSearchResults(tvAvailableUnits, _availableSearchResults, ref _availableSearchResultIndex, false);
        }

        private void SearchInTreeView(TreeView tv, string searchText, ref List<TreeNode> searchResults, ref int searchResultIndex, ref string lastSearch)
        {
            // If the search text is new, perform a new search
            if (searchText != lastSearch)
            {
                ClearSearchHighlight(tv, searchResults); // Clear previous search highlights
                lastSearch = searchText;
                searchResults.Clear();
                searchResultIndex = -1;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    return;
                }

                var foundNodes = new List<TreeNode>();
                // Recursive function to find all matching nodes
                Action<TreeNodeCollection> findNodes = null;
                findNodes = (nodes) =>
                {
                    foreach (TreeNode node in nodes)
                    {
                        if (node.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            foundNodes.Add(node);
                        }
                        if (node.Nodes != null && node.Nodes.Count > 0)
                        {
                            findNodes(node.Nodes);
                        }
                    }
                };

                findNodes(tv.Nodes);
                searchResults = foundNodes;

                // Highlight all found nodes
                foreach (var node in searchResults)
                {
                    node.BackColor = _searchHighlightColor;
                }

                if (!searchResults.Any())
                {
                    MessageBox.Show("No matches found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void NavigateSearchResults(TreeView tv, List<TreeNode> searchResults, ref int searchResultIndex, bool forward)
        {
            if (!searchResults.Any()) return;

            if (forward)
            {
                searchResultIndex++;
                if (searchResultIndex >= searchResults.Count)
                {
                    searchResultIndex = 0; // Wrap around
                }
            }
            else // Backward
            {
                searchResultIndex--;
                if (searchResultIndex < 0)
                {
                    searchResultIndex = searchResults.Count - 1; // Wrap around
                }
            }

            if (tv == tvCurrentUnits) _programmaticSelect = true;
            tv.SelectedNode = searchResults[searchResultIndex];
            if (tv == tvCurrentUnits) _programmaticSelect = false;

            searchResults[searchResultIndex].EnsureVisible();
        }

        private void TxtSearchCurrent_TextChanged(object? sender, EventArgs e)
        {
            // When search text is cleared, remove highlights from previous search
            if (string.IsNullOrWhiteSpace(txtSearchCurrent.Text) && !string.IsNullOrEmpty(_lastCurrentSearch))
            {
                ClearSearchHighlight(tvCurrentUnits, _currentSearchResults);
                _currentSearchResults.Clear();
                _lastCurrentSearch = "";
                _currentSearchResultIndex = -1;
            }

            if (btnSearchCurrent.Visible == false)
            {
                btnSearchCurrent.Visible = true;
                btnPrevCurrent.Visible = false;
                btnNextCurrent.Visible = false;
            }
        }

        private void TxtSearchAvailable_TextChanged(object? sender, EventArgs e)
        {
            // When search text is cleared, remove highlights from previous search
            if (string.IsNullOrWhiteSpace(txtSearchAvailable.Text) && !string.IsNullOrEmpty(_lastAvailableSearch))
            {
                ClearSearchHighlight(tvAvailableUnits, _availableSearchResults);
                _availableSearchResults.Clear();
                _lastAvailableSearch = "";
                _availableSearchResultIndex = -1;
            }

            if (btnSearchAvailable.Visible == false)
            {
                btnSearchAvailable.Visible = true;
                btnPrevAvailable.Visible = false;
                btnNextAvailable.Visible = false;
            }
        }

        private void btnSearchCurrent_Click(object? sender, EventArgs e)
        {
            SearchInTreeView(tvCurrentUnits, txtSearchCurrent.Text, ref _currentSearchResults, ref _currentSearchResultIndex, ref _lastCurrentSearch);
            if (_currentSearchResults.Any())
            {
                // Reset index to start from the beginning for navigation
                _currentSearchResultIndex = -1;
                NavigateSearchResults(tvCurrentUnits, _currentSearchResults, ref _currentSearchResultIndex, true);

                btnSearchCurrent.Visible = false;
                btnPrevCurrent.Visible = true;
                btnNextCurrent.Visible = true;
            }
        }

        private void btnSearchAvailable_Click(object? sender, EventArgs e)
        {
            SearchInTreeView(tvAvailableUnits, txtSearchAvailable.Text, ref _availableSearchResults, ref _availableSearchResultIndex, ref _lastAvailableSearch);
            if (_availableSearchResults.Any())
            {
                // Reset index to start from the beginning for navigation
                _availableSearchResultIndex = -1;
                NavigateSearchResults(tvAvailableUnits, _availableSearchResults, ref _availableSearchResultIndex, true);

                btnSearchAvailable.Visible = false;
                btnPrevAvailable.Visible = true;
                btnNextAvailable.Visible = true;
            }
        }

        private void TxtSearchCurrent_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearchCurrent_Click(sender, e);
                e.SuppressKeyPress = true; // Prevents the 'ding' sound
            }
        }

        private void TxtSearchAvailable_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearchAvailable_Click(sender, e);
                e.SuppressKeyPress = true; // Prevents the 'ding' sound
            }
        }

        private void ClearSearchHighlight(TreeView tv, List<TreeNode> searchResults)
        {
            if (searchResults == null || !searchResults.Any()) return;

            foreach (var node in searchResults)
            {
                // If the node is selected for replacement, restore its blue highlight,
                // otherwise restore the default background color.
                if (tv == tvCurrentUnits && _selectedCurrentNodes.Contains(node))
                {
                    node.BackColor = SystemColors.Highlight;
                }
                else
                {
                    node.BackColor = tv.BackColor;
                }
            }
        }

        private void TreeView_BeforeSelect(object? sender, TreeViewCancelEventArgs e)
        {
            var tv = sender as TreeView;
            if (tv == null || tv.SelectedNode == null) return;

            var searchResults = (tv == tvCurrentUnits) ? _currentSearchResults : _availableSearchResults;

            // When the selection is about to change, check if the currently selected node
            // is a search result. If so, restore its yellow highlight color so it remains
            // visible as a search match even after being deselected.
            if (searchResults.Contains(tv.SelectedNode))
            {
                // Do not change color if it's a node selected for replacement.
                if (tv == tvCurrentUnits && _selectedCurrentNodes.Contains(tv.SelectedNode))
                {
                    // It will remain highlighted blue by the selection logic.
                }
                else
                {
                    tv.SelectedNode.BackColor = _searchHighlightColor;
                }
            }
        }
    }
}
