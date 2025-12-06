using System.Linq;
using System.Windows.Forms;

namespace CrusaderWars.client.Options
{
    public partial class UC_GeneralOptions : UserControl
    {
        // These properties will be populated from the controls created in the designer.
        public ComboBox OptionSelection_CloseCK3 { get; private set; }
        public ComboBox OptionSelection_CloseAttila { get; private set; }
        public ComboBox OptionSelection_FullArmies { get; private set; }
        public ComboBox OptionSelection_TimeLimit { get; private set; }
        public ComboBox OptionSelection_BattleMapsSize { get; private set; }
        public ComboBox OptionSelection_DefensiveDeployables { get; private set; }
        public ComboBox OptionSelection_UnitCards { get; private set; }
        public ComboBox OptionSelection_SeparateArmies { get; private set; }
        public ComboBox OptionSelection_SiegeEngines { get; private set; }

        public UC_GeneralOptions()
        {
            // This method is defined in the designer file and creates the controls.
            InitializeComponent();

            // Find the controls by name and assign them to the public properties.
            OptionSelection_CloseCK3 = this.Controls.Find("OptionSelection_CloseCK3", true).FirstOrDefault() as ComboBox;
            OptionSelection_CloseAttila = this.Controls.Find("OptionSelection_CloseAttila", true).FirstOrDefault() as ComboBox;
            OptionSelection_FullArmies = this.Controls.Find("OptionSelection_FullArmies", true).FirstOrDefault() as ComboBox;
            OptionSelection_TimeLimit = this.Controls.Find("OptionSelection_TimeLimit", true).FirstOrDefault() as ComboBox;
            OptionSelection_BattleMapsSize = this.Controls.Find("OptionSelection_BattleMapsSize", true).FirstOrDefault() as ComboBox;
            OptionSelection_DefensiveDeployables = this.Controls.Find("OptionSelection_DefensiveDeployables", true).FirstOrDefault() as ComboBox;
            OptionSelection_UnitCards = this.Controls.Find("OptionSelection_UnitCards", true).FirstOrDefault() as ComboBox;
            OptionSelection_SeparateArmies = this.Controls.Find("OptionSelection_SeparateArmies", true).FirstOrDefault() as ComboBox;
            OptionSelection_SiegeEngines = this.Controls.Find("OptionSelection_SiegeEngines", true).FirstOrDefault() as ComboBox;
        }
    }
}
