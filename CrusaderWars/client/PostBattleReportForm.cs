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
            var headerNode = new TreeNode("Unit | Deployed | Losses | Remaining | Kills");
            headerNode.ForeColor = Color.LightGray;
            //treeViewReport.Nodes.Add(headerNode); // Header as a node is tricky to align, will use column headers if switching to ListView

            // Attacker Side
            var attackerSideNode = new TreeNode(_report.AttackerSide.SideName);
            attackerSideNode.ForeColor = Color.Green;
            PopulateSide(attackerSideNode, _report.AttackerSide);
            treeViewReport.Nodes.Add(attackerSideNode);

            // Defender Side
            var defenderSideNode = new TreeNode(_report.DefenderSide.SideName);
            defenderSideNode.ForeColor = Color.Red;
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
                foreach (var unit in army.Units)
                {
                    string unitText = $"{unit.AttilaUnitName,-40} | {unit.Deployed,8} | {unit.Losses,6} | {unit.Remaining,9} | {unit.Kills,5}";
                    var unitNode = new TreeNode(unitText);
                    unitNode.Tag = unit; // Store the full unit report object
                    
                    // Add dummy node to make it expandable
                    if(!string.IsNullOrEmpty(unit.Ck3UnitType) || unit.Characters.Any())
                    {
                        unitNode.Nodes.Add(new TreeNode("..."));
                    }

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

                    if (!string.IsNullOrEmpty(unitReport.Ck3UnitType))
                    {
                        node.Nodes.Add(new TreeNode($"CK3 Unit Type: {unitReport.Ck3UnitType}") { ForeColor = Color.Cyan });
                        node.Nodes.Add(new TreeNode($"Attila Unit Key: {unitReport.AttilaUnitKey}") { ForeColor = Color.Cyan });
                    }

                    if (unitReport.Characters.Any())
                    {
                        var charactersNode = new TreeNode("Characters");
                        charactersNode.ForeColor = Color.Yellow;
                        foreach (var character in unitReport.Characters)
                        {
                            var charNode = new TreeNode($"{character.Name}: {character.Status}");
                            if(character.Status != "Unharmed")
                            {
                                charNode.Nodes.Add(new TreeNode(character.Details) { ForeColor = Color.LightCoral });
                            }
                            charactersNode.Nodes.Add(charNode);
                        }
                        node.Nodes.Add(charactersNode);
                        charactersNode.ExpandAll();
                    }
                }

                node.Toggle();
            }
        }


        private void btnContinue_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
