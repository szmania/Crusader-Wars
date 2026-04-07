using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace CrusaderWars.client
{
    [SupportedOSPlatform("windows")]
    public partial class UC_GeneralOptions : UserControl
    {
        public UC_GeneralOptions()
        {
            InitializeComponent();
        }

        // Methods to get values from controls
        [SupportedOSPlatform("windows")]
        public string GetCloseCK3() => OptionSelection_CloseCK3.SelectedItem?.ToString() ?? "Enabled";
        [SupportedOSPlatform("windows")]
        public string GetCloseAttila() => OptionSelection_CloseAttila.SelectedItem?.ToString() ?? "Enabled";
        [SupportedOSPlatform("windows")]
        public string GetFullArmies() => OptionSelection_FullArmies.SelectedItem?.ToString() ?? "Disabled";
        [SupportedOSPlatform("windows")]
        public string GetTimeLimit() => OptionSelection_TimeLimit.SelectedItem?.ToString() ?? "Enabled";
        [SupportedOSPlatform("windows")]
        public string GetBattleMapsSize() => OptionSelection_BattleMapsSize.SelectedItem?.ToString() ?? "Dynamic";
        [SupportedOSPlatform("windows")]
        public string GetDefensiveDeployables() => OptionSelection_DefensiveDeployables.SelectedItem?.ToString() ?? "Enabled";
        [SupportedOSPlatform("windows")]
        public string GetUnitCards() => OptionSelection_UnitCards.SelectedItem?.ToString() ?? "Enabled";
        [SupportedOSPlatform("windows")]
        public string GetSeparateArmies() => OptionSelection_SeparateArmies.SelectedItem?.ToString() ?? "Friendly Only";
        [SupportedOSPlatform("windows")]
        public string GetSiegeEnginesInFieldBattles() => OptionSelection_SiegeEngines.SelectedItem?.ToString() ?? "Enabled";
        [SupportedOSPlatform("windows")]
        public string GetShowPostBattleReport() => OptionSelection_ShowPostBattleReport.SelectedItem?.ToString() ?? "Enabled";

        // Methods to set values of controls
        [SupportedOSPlatform("windows")]
        public void SetCloseCK3(string value) { OptionSelection_CloseCK3.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetCloseAttila(string value) { OptionSelection_CloseAttila.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetFullArmies(string value) { OptionSelection_FullArmies.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetTimeLimit(string value) { OptionSelection_TimeLimit.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetBattleMapsSize(string value) { OptionSelection_BattleMapsSize.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetDefensiveDeployables(string value) { OptionSelection_DefensiveDeployables.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetUnitCards(string value) { OptionSelection_UnitCards.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetSeparateArmies(string value) { OptionSelection_SeparateArmies.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetSiegeEnginesInFieldBattles(string value) { OptionSelection_SiegeEngines.SelectedItem = value; }
        [SupportedOSPlatform("windows")]
        public void SetShowPostBattleReport(string value) { OptionSelection_ShowPostBattleReport.SelectedItem = value; }
    }
}
