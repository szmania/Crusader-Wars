using System.Windows.Forms;

namespace CrusaderWars.client.Options
{
    public partial class UC_GeneralOptions : UserControl
    {
        public UC_Toggle ToggleSiegeEnginesInField => Toggle_SiegeEnginesInField;
        public UC_Toggle ToggleDeploymentZones => Toggle_DeploymentZones;
        public UC_Toggle ToggleArmiesControl => Toggle_ArmiesControl;
        public UC_Toggle ToggleShowPostBattleReport => Toggle_ShowPostBattleReport;

        // State tracking properties
        public bool SiegeEnginesInFieldState { get; private set; }
        public bool DeploymentZonesState { get; private set; }
        public bool ArmiesControlState { get; private set; }
        public bool ShowPostBattleReportState { get; private set; }


        public UC_GeneralOptions()
        {
            InitializeComponent();
            // Add event handlers to track state changes
            Toggle_SiegeEnginesInField.Click += (s, e) => SiegeEnginesInFieldState = !SiegeEnginesInFieldState;
            Toggle_DeploymentZones.Click += (s, e) => DeploymentZonesState = !DeploymentZonesState;
            Toggle_ArmiesControl.Click += (s, e) => ArmiesControlState = !ArmiesControlState;
            Toggle_ShowPostBattleReport.Click += (s, e) => ShowPostBattleReportState = !ShowPostBattleReportState;
        }

        // Methods to set initial state
        public void SetSiegeEnginesInFieldState(bool state)
        {
            Toggle_SiegeEnginesInField.SetState(state);
            SiegeEnginesInFieldState = state;
        }
        public void SetDeploymentZonesState(bool state)
        {
            Toggle_DeploymentZones.SetState(state);
            DeploymentZonesState = state;
        }
        public void SetArmiesControlState(bool state)
        {
            Toggle_ArmiesControl.SetState(state);
            ArmiesControlState = state;
        }
        public void SetShowPostBattleReportState(bool state)
        {
            Toggle_ShowPostBattleReport.SetState(state);
            ShowPostBattleReportState = state;
        }
    }
}
