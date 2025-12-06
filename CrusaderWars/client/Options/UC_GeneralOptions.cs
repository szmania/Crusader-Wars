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
        public ComboBox OptionSelection_ShowPostBattleReport { get; private set; }

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
            OptionSelection_ShowPostBattleReport = this.Controls.Find("OptionSelection_ShowPostBattleReport", true).FirstOrDefault() as ComboBox;
        }

        // NOTE: The following Dispose method is added to fix the CS0115 error.
        // It seems the Dispose method in the associated .Designer.cs file is incorrect.
        // This method provides a valid override for the base UserControl.Dispose method.
        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // The designer file should contain the 'components' field and dispose it.
            // This is a minimal implementation to resolve the compile error.
            // If you have components to dispose, ensure they are handled correctly in the designer file.
            base.Dispose(disposing);
        }
    }
}
