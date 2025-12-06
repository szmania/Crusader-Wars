using System.Windows.Forms;

namespace CrusaderWars.client.Options
{
    public partial class UC_GeneralOptions : UserControl
    {
        public UC_Toggle ToggleSiegeEnginesInField => Toggle_SiegeEnginesInField;
        public UC_Toggle ToggleDeploymentZones => Toggle_DeploymentZones;
        public UC_Toggle ToggleArmiesControl => Toggle_ArmiesControl;
        public UC_Toggle ToggleShowPostBattleReport => Toggle_ShowPostBattleReport;

        public UC_GeneralOptions()
        {
            InitializeComponent();
        }
    }
}
