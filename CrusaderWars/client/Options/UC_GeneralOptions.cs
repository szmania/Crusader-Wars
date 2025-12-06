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
    public partial class UC_GeneralOptions : UserControl
    {
        public UC_GeneralOptions()
        {
            InitializeComponent();
        }

        // Methods to get values from controls
        public string GetCloseCK3() => OptionSelection_CloseCK3.SelectedItem.ToString();
        public string GetCloseAttila() => OptionSelection_CloseAttila.SelectedItem.ToString();
        public string GetFullArmies() => OptionSelection_FullArmies.SelectedItem.ToString();
        public string GetTimeLimit() => OptionSelection_TimeLimit.SelectedItem.ToString();
        public string GetBattleMapsSize() => OptionSelection_BattleMapsSize.SelectedItem.ToString();
        public string GetDefensiveDeployables() => OptionSelection_DefensiveDeployables.SelectedItem.ToString();
        public string GetUnitCards() => OptionSelection_UnitCards.SelectedItem.ToString();
        public string GetSeparateArmies() => OptionSelection_SeparateArmies.SelectedItem.ToString();
        public string GetSiegeEnginesInFieldBattles() => OptionSelection_SiegeEngines.SelectedItem.ToString();
        public string GetShowPostBattleReport() => OptionSelection_ShowPostBattleReport.SelectedItem.ToString();

        // Methods to set values of controls
        public void SetCloseCK3(string value) { OptionSelection_CloseCK3.SelectedItem = value; }
        public void SetCloseAttila(string value) { OptionSelection_CloseAttila.SelectedItem = value; }
        public void SetFullArmies(string value) { OptionSelection_FullArmies.SelectedItem = value; }
        public void SetTimeLimit(string value) { OptionSelection_TimeLimit.SelectedItem = value; }
        public void SetBattleMapsSize(string value) { OptionSelection_BattleMapsSize.SelectedItem = value; }
        public void SetDefensiveDeployables(string value) { OptionSelection_DefensiveDeployables.SelectedItem = value; }
        public void SetUnitCards(string value) { OptionSelection_UnitCards.SelectedItem = value; }
        public void SetSeparateArmies(string value) { OptionSelection_SeparateArmies.SelectedItem = value; }
        public void SetSiegeEnginesInFieldBattles(string value) { OptionSelection_SiegeEngines.SelectedItem = value; }
        public void SetShowPostBattleReport(string value) { OptionSelection_ShowPostBattleReport.SelectedItem = value; }
    }
}
