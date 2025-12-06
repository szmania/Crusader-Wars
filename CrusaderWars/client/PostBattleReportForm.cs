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

            // Add Header
            var headerNode = new TreeNode("Unit                                            | Deployed | Losses | Remaining | Kills");
            headerNode.ForeColor = Color.LightGray;
            treeViewReport.Nodes.Add(headerNode);

            // Attacker Side
            var attackerSideNode = new TreeNode(_report.AttackerSide.SideName);
            attackerSideNode.ForeColor = Color.LightGreen;
            PopulateSide(attackerSideNode, _report.AttackerSide);
            treeViewReport.Nodes.Add(attackerSideNode);

            // Defender Side
            var defenderSideNode = new TreeNode(_report.DefenderSide.SideName);
            defenderSideNode.ForeColor = Color.OrangeRed;
            PopulateSide(defenderSideNode, _report.DefenderSide);
            treeViewReport.Nodes.Add(defenderSideNode);

            // Expand top-level nodes
            attackerSideNode.Expand();
            defenderSideNode.Expand();

            treeViewReport.EndUpdate();

            // Populate summary
            lblBattleResult.Text = $"Battle Result: {_report.BattleResult}";
            if(_report.BattleResult == "Victory") { lblBattleResult.ForeColor = Color.LightGreen; } else { lblBattleResult.ForeColor = Color.OrangeRed; }

            if(_report.SiegeResult != "N/A")
            {
                lblSiegeResult.Text = $"Siege Result: {_report.SiegeResult}";
                lblWallDamage.Text = $"Wall Damage: {_report.WallDamage}";
                lblSiegeResult.Visible = true;
                lblWallDamage.Visible = true;
            }
            else
            {
                lblSiegeResult.Visible = false;
                lblWallDamage.Visible = false;
            }
        }

        private void PopulateSide(TreeNode sideNode, SideReport sideReport)
        {
            foreach (var army in sideReport.Armies)
            {
                var armyNode = new TreeNode($"{army.ArmyName} (Commander: {army.CommanderName})");
                foreach (var unit in army.Units.OrderByDescending(u => u.Ck3UnitType == "Commander").ThenByDescending(u => u.Ck3UnitType == "Knight"))
                {
                    string unitText = String.Format("{0,-47} | {1,8} | {2,6} | {3,9} | {4,5}", 
                        unit.AttilaUnitName, unit.Deployed, unit.Losses, unit.Remaining, unit.Kills);
                    var unitNode = new TreeNode(unitText);
                    unitNode.Tag = unit; // Store the full unit report object
                    
                    // Always add dummy node to make ALL units expandable for detailed information
                    unitNode.Nodes.Add(new TreeNode("..."));

                    armyNode.Nodes.Add(unitNode);
                }
                sideNode.Nodes.Add(armyNode);
                armyNode.Expand();
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
                    node.Nodes.Clear(); // Remove dummy node

                    // Always show basic unit information
                    node.Nodes.Add(new TreeNode($"CK3 Unit Type: {unitReport.Ck3UnitType ?? "N/A"}") { ForeColor = Color.Cyan });
                    node.Nodes.Add(new TreeNode($"Attila Unit Key: {unitReport.AttilaUnitKey ?? "N/A"}") { ForeColor = Color.Cyan });
                    node.Nodes.Add(new TreeNode($"Deployed: {unitReport.Deployed}") { ForeColor = Color.LightGray });
                    node.Nodes.Add(new TreeNode($"Losses: {unitReport.Losses}") { ForeColor = Color.LightCoral });
                    node.Nodes.Add(new TreeNode($"Remaining: {unitReport.Remaining}") { ForeColor = Color.LightGreen });
                    node.Nodes.Add(new TreeNode($"Kills: {unitReport.Kills}") { ForeColor = Color.Gold });

                    // Add conversion information
                    if (unitReport.Ck3UnitType == "Levy")
                    {
                        node.Nodes.Add(new TreeNode("Note: This unit represents combined levies from CK3") { ForeColor = Color.LightBlue });
                    }
                    else if (unitReport.Ck3UnitType == "Knight")
                    {
                        node.Nodes.Add(new TreeNode("Note: This unit represents combined knights from CK3") { ForeColor = Color.LightBlue });
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
    }
}
