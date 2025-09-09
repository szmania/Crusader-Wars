using System.Collections.Generic;
using CrusaderWars.client; // For Program.Logger

namespace CrusaderWars.client
{
    public static class ModOptions
    {
        public enum ArmiesSetup
        {
            All_Controled,
            Friendly_Only,
            All_Separate
        }

        public static int GetLevyMax() { return 160; } // Placeholder
        public static int GetCavalryMax() { return 80; } // Placeholder
        public static int GetInfantryMax() { return 160; } // Placeholder
        public static int GetRangedMax() { return 160; } // Placeholder

        public static bool CloseCK3DuringBattle() { return true; } // Placeholder
        public static ArmiesSetup SeparateArmies() { return ArmiesSetup.All_Separate; } // Placeholder
        public static bool DefensiveDeployables() { return true; } // Placeholder
        public static string TimeLimit() { return "<time_limit>1200</time_limit>"; } // Placeholder
        public static string SetMapSize(int totalSoldiers) { return "1000"; } // Placeholder
        public static bool UnitCards() { return true; } // Placeholder
        public static string FullArmies(data.save_file.Regiment regiment) { return regiment.CurrentNum; } // Placeholder
        public static int CulturalPreciseness() { return 500; } // Placeholder
        public static string DeploymentsZones() { return "normal"; } // Placeholder

        public static void StoreOptionsValues(Dictionary<string, string> optionsValuesCollection)
        {
            Program.Logger.Debug("ModOptions.StoreOptionsValues() called (placeholder).");
            // This method would typically read options from a dictionary and set internal values.
        }
    }
}
