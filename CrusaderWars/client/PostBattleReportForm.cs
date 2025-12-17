using CrusaderWars.data.save_file;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrusaderWars.client
{
    public partial class PostBattleReportForm : Form
    {
        private readonly BattleReport _report;

        public PostBattleReportForm(BattleReport report)
        {
            InitializeComponent();
            _report = report;
            // Icon assignment removed - crusader_conflicts_logo is a Bitmap, not an Icon
        }

        private void PostBattleReportForm_Load(object sender, EventArgs e)
        {
            PopulateReport();
        }

        private void PopulateReport()
        {
            treeViewReport.BeginUpdate();
            treeViewReport.Nodes.Clear();

            // Add Battle Header Details
            lblBattleName.Text = $"Battle Name: {_report.BattleName}";
            lblBattleDate.Text = $"Battle Date: {_report.BattleDate}";
            lblLocationDetails.Text = $"Terrain: {_report.LocationDetails}";
            lblProvinceName.Text = $"Province: {_report.ProvinceName}";
            lblTimeOfDay.Text = $"Time of Day: {_report.TimeOfDay}";
            lblSeason.Text = $"Season: {_report.Season}";
            // lblWeather.Text = $"Weather: {_report.Weather}"; // Removed weather display

            // Add Header for units
            var headerNode = new TreeNode("Unit                                            | Deployed (Engines) | Losses (Engines) | Remaining (Engines) | Kills");
            headerNode.ForeColor = Color.LightGray;
            treeViewReport.Nodes.Add(headerNode);

            // Attacker Side
            var attackerSideNode = new TreeNode($"{_report.AttackerSide.SideName} (Deployed: {_report.AttackerSide.TotalDeployed}, Losses: {_report.AttackerSide.TotalLosses}, Remaining: {_report.AttackerSide.TotalRemaining}, Kills: {_report.AttackerSide.TotalKills})");
            attackerSideNode.ForeColor = Color.LightGreen;
            PopulateSide(attackerSideNode, _report.AttackerSide);
            treeViewReport.Nodes.Add(attackerSideNode);

            // Defender Side
            var defenderSideNode = new TreeNode($"{_report.DefenderSide.SideName} (Deployed: {_report.DefenderSide.TotalDeployed}, Losses: {_report.DefenderSide.TotalLosses}, Remaining: {_report.DefenderSide.TotalRemaining}, Kills: {_report.DefenderSide.TotalKills})");
            defenderSideNode.ForeColor = Color.OrangeRed;
            PopulateSide(defenderSideNode, _report.DefenderSide);
            treeViewReport.Nodes.Add(defenderSideNode);

            // Expand top-level nodes
            attackerSideNode.Expand();
            defenderSideNode.Expand();
            treeViewReport.EndUpdate();

            // Populate summary
            lblBattleResult.Text = $"Battle Result: {_report.BattleResult}";
            if(_report.BattleResult == "Victory") {
                lblBattleResult.ForeColor = Color.LightGreen;
            } else {
                lblBattleResult.ForeColor = Color.OrangeRed;
            }

            if(_report.SiegeResult != "N/A") {
                lblSiegeResult.Text = $"Siege Result: {_report.SiegeResult}";
                lblWallDamage.Text = $"Wall Damage: {_report.WallDamage}";
                lblSiegeResult.Visible = true;
                lblWallDamage.Visible = true;
            } else {
                lblSiegeResult.Visible = false;
                lblWallDamage.Visible = false;
            }
            
            // Add total battle statistics at the bottom
            int totalDeployed = _report.AttackerSide.TotalDeployed + _report.DefenderSide.TotalDeployed;
            int totalRemaining = _report.AttackerSide.TotalRemaining + _report.DefenderSide.TotalRemaining;
            int totalLosses = _report.AttackerSide.TotalLosses + _report.DefenderSide.TotalLosses;
            int totalKills = _report.AttackerSide.TotalKills + _report.DefenderSide.TotalKills;
            
            // Verify totals consistency
            if (totalLosses != totalKills)
            {
                totalKills = totalLosses; // Force consistency
            }

            // Update the labels that are now part of the form design
            lblTotalDeployed.Text = $"Total Deployed: {totalDeployed}";
            lblTotalLosses.Text = $"Total Losses: {totalLosses}";
            lblTotalRemaining.Text = $"Total Remaining: {totalRemaining}";
            lblTotalKills.Text = $"Total Kills: {totalKills}";

            // Make the labels visible
            lblTotalDeployed.Visible = true;
            lblTotalLosses.Visible = true;
            lblTotalRemaining.Visible = true;
            lblTotalKills.Visible = true;
        }

        private void PopulateSide(TreeNode sideNode, SideReport sideReport)
        {
            foreach (var army in sideReport.Armies)
            {
                var armyNode = new TreeNode($"{army.ArmyName} (Commander: {army.CommanderName}) (Deployed: {army.TotalDeployed}, Losses: {army.TotalLosses}, Remaining: {army.TotalRemaining}, Kills: {army.TotalKills})");
                
                // Group units for display, especially for knights
                var groupedUnits = army.Units
                    .GroupBy(u => u.Ck3UnitType == "Knight" ? new { Ck3UnitType = u.Ck3UnitType, AttilaUnitKey = "KNIGHT_GROUP", AttilaUnitName = "Knights (Combined)" } : new { u.Ck3UnitType, u.AttilaUnitKey, u.AttilaUnitName }) // Group by type, key, and formatted name
                    .Select(g => {
                        var firstUnit = g.First();
                        // Aggregate soldiers, losses, kills for grouped units
                        return new UnitReport
                        {
                            AttilaUnitName = firstUnit.Ck3UnitType == "Knight" ? "Knights (Combined)" : firstUnit.AttilaUnitName, // Use a generic name for the combined group
                            Deployed = g.Sum(u => u.Deployed),
                            Losses = g.Sum(u => u.Losses),
                            Remaining = g.Sum(u => u.Remaining),
                            Kills = g.Sum(u => u.Kills),
                            Ck3UnitType = firstUnit.Ck3UnitType,
                            AttilaUnitKey = firstUnit.AttilaUnitKey,
                            Ck3Heritage = firstUnit.Ck3Heritage,
                            Ck3Culture = firstUnit.Ck3Culture,
                            AttilaFaction = firstUnit.AttilaFaction,
                            Characters = g.SelectMany(u => u.Characters).DistinctBy(c => c.Name).ToList(), // Collect all characters
                            KnightDetails = g.SelectMany(u => u.KnightDetails).DistinctBy(k => k.Name).ToList(), // Collect all knight details
                            IsSiegeUnit = g.Any(u => u.IsSiegeUnit),
                            DeployedMachines = g.Sum(u => u.DeployedMachines),
                            RemainingMachines = g.Sum(u => u.RemainingMachines),
                            MachineLosses = g.Sum(u => u.MachineLosses)
                        };
                    })
                    .OrderByDescending(u => u.Ck3UnitType == "Commander")
                    .ThenByDescending(u => u.Ck3UnitType == "Knight")
                    .ThenByDescending(u => u.Ck3UnitType == "Garrison") // NEW: Prioritize Garrison units in display order
                    .ToList();

                foreach (var unit in groupedUnits)
                {
                    string unitText;
                    if (unit.IsSiegeUnit)
                    {
                        string deployedStr = $"{unit.Deployed} ({unit.DeployedMachines})";
                        string lossesStr = $"{unit.Losses} ({unit.MachineLosses})";
                        string remainingStr = $"{unit.Remaining} ({unit.RemainingMachines})";
                        unitText = $"{unit.AttilaUnitName.PadRight(47)} | {deployedStr.PadLeft(18)} | {lossesStr.PadLeft(6)} | {remainingStr.PadLeft(9)} | {unit.Kills.ToString().PadLeft(5)}";
                    }
                    else
                    {
                        unitText = String.Format("{0,-47} | {1,18} | {2,6} | {3,9} | {4,5}", 
                            unit.AttilaUnitName, unit.Deployed, unit.Losses, unit.Remaining, unit.Kills);
                    }
                    var unitNode = new TreeNode(unitText);
                    unitNode.Tag = unit; // Store the full unit report object
                    
                    // Always add dummy node to make ALL units expandable for detailed information
                    unitNode.Nodes.Add(new TreeNode("..."));

                    armyNode.Nodes.Add(unitNode);
                }

                // Add siege engines node if there are any
                if (army.SiegeEngines != null && army.SiegeEngines.Any())
                {
                    var siegeNode = new TreeNode("Siege Engines");
                    foreach (var siegeEngine in army.SiegeEngines)
                    {
                        siegeNode.Nodes.Add($"{siegeEngine.Name}: {siegeEngine.Quantity}");
                    }
                    armyNode.Nodes.Add(siegeNode);
                }

                sideNode.Nodes.Add(armyNode);
                armyNode.Expand();
            }
        }

        private void treeViewReport_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            // Check if it's a unit node with details
            if (node.Tag is UnitReport unitReport)
            {
                // If it has the dummy node, it hasn't been populated yet
                if (node.Nodes.Count == 1 && node.Nodes[0].Text == "...")
                {
                    node.Nodes.Clear(); // Remove dummy node

                    // Always show basic unit information
                    node.Nodes.Add(new TreeNode($"CK3 Unit Type: {unitReport.Ck3UnitType ?? "N/A"}") { ForeColor = Color.Cyan });
                    node.Nodes.Add(new TreeNode($"Attila Unit Key: {unitReport.AttilaUnitKey ?? "N/A"}") { ForeColor = Color.Cyan });
                    
                    if (unitReport.IsSiegeUnit)
                    {
                        node.Nodes.Add(new TreeNode($"Deployed: {unitReport.Deployed} ({unitReport.DeployedMachines} engines)") { ForeColor = Color.LightGray });
                        node.Nodes.Add(new TreeNode($"Losses: {unitReport.Losses} ({unitReport.MachineLosses} engines)") { ForeColor = Color.LightCoral });
                        node.Nodes.Add(new TreeNode($"Remaining: {unitReport.Remaining} ({unitReport.RemainingMachines} engines)") { ForeColor = Color.LightGreen });
                    }
                    else
                    {
                        node.Nodes.Add(new TreeNode($"Deployed: {unitReport.Deployed}") { ForeColor = Color.LightGray });
                        node.Nodes.Add(new TreeNode($"Losses: {unitReport.Losses}") { ForeColor = Color.LightCoral });
                        node.Nodes.Add(new TreeNode($"Remaining: {unitReport.Remaining}") { ForeColor = Color.LightGreen });
                    }
                    node.Nodes.Add(new TreeNode($"Kills: {unitReport.Kills}") { ForeColor = Color.Gold });

                    // Add CK3 heritage, culture, and Attila faction information if available
                    if (!string.IsNullOrEmpty(unitReport.Ck3Heritage))
                    {
                        node.Nodes.Add(new TreeNode($"CK3 Heritage: {unitReport.Ck3Heritage}") { ForeColor = Color.LightSteelBlue });
                    }
                    if (!string.IsNullOrEmpty(unitReport.Ck3Culture))
                    {
                        node.Nodes.Add(new TreeNode($"CK3 Culture: {unitReport.Ck3Culture}") { ForeColor = Color.LightSteelBlue });
                    }
                    if (!string.IsNullOrEmpty(unitReport.AttilaFaction))
                    {
                        node.Nodes.Add(new TreeNode($"Attila Faction: {unitReport.AttilaFaction}") { ForeColor = Color.LightSteelBlue });
                    }

                    // Add conversion information
                    if (unitReport.Ck3UnitType == "Levy")
                    {
                        node.Nodes.Add(new TreeNode("Note: This unit represents combined levies from CK3") { ForeColor = Color.LightBlue });
                    }
                    else if (unitReport.Ck3UnitType == "Knight")
                    {
                        node.Nodes.Add(new TreeNode("Note: This unit represents combined knights from CK3") { ForeColor = Color.LightBlue });
                        
                        // NEW: Add individual knight details
                        if (unitReport.KnightDetails.Any())
                        {
                            var knightsNode = new TreeNode("Individual Knights (Bodyguard Size | Kills | Status)");
                            knightsNode.ForeColor = Color.Yellow;
                            foreach (var knight in unitReport.KnightDetails.OrderByDescending(k => k.BodyguardSize))
                            {
                                var knightDetailNode = new TreeNode($"{knight.Name}: {knight.BodyguardSize} | {knight.Kills} | {knight.Status}");
                                if (knight.Fallen)
                                {
                                    knightDetailNode.ForeColor = Color.Red;
                                }
                                else if (knight.Status != "Unharmed")
                                {
                                    knightDetailNode.ForeColor = Color.Orange;
                                }
                                else
                                {
                                    knightDetailNode.ForeColor = Color.LightGreen;
                                }
                                knightsNode.Nodes.Add(knightDetailNode);
                            }
                            node.Nodes.Add(knightsNode);
                        }
                        
                        // Add Rank field for Knight units
                        if (unitReport.Rank > 0)
                        {
                            node.Nodes.Add(new TreeNode($"Rank: {unitReport.Rank}") { ForeColor = Color.LightSteelBlue });
                        }
                    }
                    else if (unitReport.Ck3UnitType == "Garrison") // NEW: Specific handling for Garrison units
                    {
                        node.Nodes.Add(new TreeNode("Note: This unit represents a garrison unit defending a settlement.") { ForeColor = Color.LightBlue });
                        if (!string.IsNullOrEmpty(unitReport.AttilaUnitKey) && unitReport.AttilaUnitKey != "N/A")
                        {
                            node.Nodes.Add(new TreeNode($"Attila Garrison Type: {unitReport.AttilaUnitKey}") { ForeColor = Color.LightSteelBlue });
                        }
                        // Add garrison level information if available
                        if (unitReport.GarrisonLevel > 0)
                        {
                            node.Nodes.Add(new TreeNode($"Garrison Level: {unitReport.GarrisonLevel}") { ForeColor = Color.LightSteelBlue });
                        }
                    }
                    else if (unitReport.Ck3UnitType == "Commander") // Add Rank field for Commander units
                    {
                        if (unitReport.Rank > 0)
                        {
                            node.Nodes.Add(new TreeNode($"Rank: {unitReport.Rank}") { ForeColor = Color.LightSteelBlue });
                        }
                    }

                    if (unitReport.Characters.Any())
                    {
                        var charactersNode = new TreeNode("Characters");
                        charactersNode.ForeColor = Color.Yellow;
                        foreach (var character in unitReport.Characters.OrderBy(c => c.Name))
                        {
                            var charNode = new TreeNode($"{character.Name}: {character.Status}");
                            if(character.Status != "Unharmed")
                            {
                                charNode.ForeColor = Color.LightCoral;
                                charNode.Nodes.Add(new TreeNode($"Details: {character.Details}") { ForeColor = Color.Tomato });
                            }
                            else
                            {
                                charNode.ForeColor = Color.LightGreen;
                            }
                            charactersNode.Nodes.Add(charNode);
                        }
                        node.Nodes.Add(charactersNode);
                    }
                }
            }
        }

        private void treeViewReport_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var node = e.Node;
            // Check if it's a unit node with details
            if (node.Tag is UnitReport unitReport)
            {
                // If it has the dummy node, it hasn't been populated yet
                if (node.Nodes.Count == 1 && node.Nodes[0].Text == "...")
                {
                    // Trigger expansion which will populate the node
                    node.Expand();
                }
                else if (node.Nodes.Count > 0)
                {
                    // If already populated, just toggle expansion
                    node.Toggle();
                }
            }
        }


        private void btnContinue_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private string GenerateClipboardContent()
        {
            var sb = new StringBuilder();
            
            // Battle Header Details
            sb.AppendLine("=== CRUSADER CONFLICTS BATTLE REPORT ===");
            sb.AppendLine($"Battle Name: {_report.BattleName}");
            sb.AppendLine($"Date: {_report.BattleDate}");
            sb.AppendLine($"Location: {_report.LocationDetails}");
            sb.AppendLine($"Province: {_report.ProvinceName}");
            sb.AppendLine($"Time of Day: {_report.TimeOfDay}");
            sb.AppendLine($"Season: {_report.Season}");
            sb.AppendLine();

            // Battle Result
            sb.AppendLine($"Result: {_report.BattleResult}");
            if (_report.SiegeResult != "N/A")
            {
                sb.AppendLine($"Siege Result: {_report.SiegeResult}");
                sb.AppendLine($"Wall Damage: {_report.WallDamage}");
            }
            sb.AppendLine();

            // Total Statistics
            sb.AppendLine("=== TOTAL STATISTICS ===");
            sb.AppendLine($"Total Deployed: {lblTotalDeployed.Text.Replace("Total Deployed: ", "")}");
            sb.AppendLine($"Total Losses: {lblTotalLosses.Text.Replace("Total Losses: ", "")}");
            sb.AppendLine($"Total Remaining: {lblTotalRemaining.Text.Replace("Total Remaining: ", "")}");
            sb.AppendLine($"Total Kills: {lblTotalKills.Text.Replace("Total Kills: ", "")}");
            sb.AppendLine();

            // Attacker Side Details
            sb.AppendLine("=== ATTACKER SIDE ===");
            sb.AppendLine($"Total: {_report.AttackerSide.TotalDeployed} deployed, {_report.AttackerSide.TotalLosses} losses, {_report.AttackerSide.TotalRemaining} remaining, {_report.AttackerSide.TotalKills} kills");
            AppendSideDetails(sb, _report.AttackerSide, "Attacker");

            // Defender Side Details
            sb.AppendLine("=== DEFENDER SIDE ===");
            sb.AppendLine($"Total: {_report.DefenderSide.TotalDeployed} deployed, {_report.DefenderSide.TotalLosses} losses, {_report.DefenderSide.TotalRemaining} remaining, {_report.DefenderSide.TotalKills} kills");
            AppendSideDetails(sb, _report.DefenderSide, "Defender");

            return sb.ToString();
        }

        private void AppendSideDetails(StringBuilder sb, SideReport side, string sideName)
        {
            foreach (var army in side.Armies)
            {
                sb.AppendLine();
                sb.AppendLine($"{sideName} Army: {army.ArmyName} (Commander: {army.CommanderName})");
                sb.AppendLine($"  Total: {army.TotalDeployed} deployed, {army.TotalLosses} losses, {army.TotalRemaining} remaining, {army.TotalKills} kills");

                foreach (var unit in army.Units)
                {
                    sb.AppendLine($"  Unit: {unit.AttilaUnitName}");
                    sb.AppendLine($"    Deployed: {unit.Deployed}, Losses: {unit.Losses}, Remaining: {unit.Remaining}, Kills: {unit.Kills}");
                    
                    if (unit.Characters.Any())
                    {
                        sb.AppendLine("    Characters:");
                        foreach (var character in unit.Characters)
                        {
                            sb.AppendLine($"      {character.Name}: {character.Status} - {character.Details}");
                        }
                    }
                }
            }
        }

        // Add the button click event handler
        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            try
            {
                string clipboardContent = GenerateClipboardContent();
                Clipboard.SetText(clipboardContent);
                MessageBox.Show("Battle report copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
