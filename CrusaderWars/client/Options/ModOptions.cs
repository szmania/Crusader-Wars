using CrusaderWars.terrain;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CrusaderWars.data.save_file;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CrusaderWars.client
{
    public static class ModOptions
    {
        static Dictionary<string, string> optionsValuesCollection = new Dictionary<string, string>();
        public static void StoreOptionsValues(Dictionary<string, string> OptionsForm_ValuesCollection)
        {
            optionsValuesCollection = OptionsForm_ValuesCollection;
        }

        public static int GetLevyMax()
        {
            return Int32.Parse(optionsValuesCollection["LeviesMax"]);
        }
        public static int GetInfantryMax()
        {
            return Int32.Parse(optionsValuesCollection["InfantryMax"]);
        }

        public static int GetRangedMax()
        {
            return Int32.Parse(optionsValuesCollection["RangedMax"]);
        }
        public static int GetCavalryMax()
        {
            return Int32.Parse(optionsValuesCollection["CavalryMax"]);
        }

        public static void SetLevyMax(int value)
        {
            optionsValuesCollection["LeviesMax"] = value.ToString();
        }
        public static void SetInfantryMax(int value)
        {
            optionsValuesCollection["InfantryMax"] = value.ToString();
        }

        public static void SetRangedMax(int value)
        {
            optionsValuesCollection["RangedMax"] = value.ToString();
        }
        public static void SetCavalryMax(int value)
        {
            optionsValuesCollection["CavalryMax"] = value.ToString();
        }


        public static int GetBattleScale()
        {
            return Int32.Parse(optionsValuesCollection["BattleScale"].Trim('%'));
        }
        
        public static bool GetAutoScale()
        {
            switch (optionsValuesCollection["AutoScaleUnits"])
            {
                case "Disabled":
                    return false;
                case "Enabled":
                    return true;
                default:
                    return true;

            }
        }


        public static bool CloseCK3DuringBattle()
        {
            switch(optionsValuesCollection["CloseCK3"])
            {
                case "Disabled":
                    return false;
                case "Enabled":
                    return true;
                default:
                    return true;
            }
        }

        public static void CloseAttila()
        {
            switch(optionsValuesCollection["CloseAttila"])
            {
                case "Disabled":
                    return;
                case "Enabled":
                    ShutdownAttila();
                    break;
                default:
                    return;

            }
            
        }

        public enum ArmiesSetup
        {
            All_Controled,
            Friendly_Only,
            All_Separate
        }

        public static ArmiesSetup SeparateArmies()
        {
            switch (optionsValuesCollection["SeparateArmies"])
            {
                case "All Controled":
                    return ArmiesSetup.All_Controled;
                case "Friendly Only":
                    return ArmiesSetup.Friendly_Only;
                case "All Separate":
                    return ArmiesSetup.All_Separate;
                default:
                    return ArmiesSetup.Friendly_Only;

            }
        }
        
        public static string DeploymentsZones()
        {
            return optionsValuesCollection["BattleMapsSize"];
        }


        public static string SetMapSize(int total_soldiers)
        {
            switch (optionsValuesCollection["BattleMapsSize"])
            {
                case "Dynamic":
                    if (total_soldiers <= 5000)
                    {
                        return "1000";
                    }
                    else if (total_soldiers > 5000 && total_soldiers < 20000)
                    {
                        return "1500";
                    }
                    else if (total_soldiers >= 20000)
                    {
                        return "2000";
                    }
                    break;
                case "Medium":
                    return "1000";
                case "Big":
                    return "1500";
                case "Huge":
                    return "2000";
            }

            return "1500";
        }

        public static string FullArmies(Regiment reg)
        {
            if (reg?.CurrentNum == null) return "0";

            // The "FullArmies" option is not directly used here, but the value is returned.
            // var option = optionsValuesCollection["FullArmies"];

            return reg.CurrentNum;

            /*
            switch (option.value)
            {
                case "Disabled":
                    return sum.ToString();
                case "Enabled":
                    return sum.ToString();
                default:
                    return sum.ToString(); 
            }
            */

        }

        public static string TimeLimit()
        {
            switch (optionsValuesCollection["TimeLimit"]) 
            {
                case "Disabled":
                    return "";
                case "Enabled":
                    return "<duration>3600</duration>\n";
                default:
                    return "<duration>3600</duration>\n";
            }
        }

        public static bool DefensiveDeployables()
        {
            switch (optionsValuesCollection["DefensiveDeployables"])
            {
                case "Disabled":
                    return false;
                case "Enabled":
                    return true;
                default:
                    return true;
            }
        }

        public static bool UnitCards()
        {
            switch (optionsValuesCollection["UnitCards"])
            {
                case "Disabled":
                    return false;
                case "Enabled":
                    return true;
                default:
                    return true;
            }
        }

        public static int CulturalPreciseness()
        {
            int minumum = 5;
            return minumum;
        }

        private static void ShutdownAttila()
        {
            Process[] process_attila = Process.GetProcessesByName("Attila");
            foreach (Process worker in process_attila)
            {
                worker.Kill();
                worker.WaitForExit();
                worker.Dispose();
            }
        }

        public static int GetCommanderWoundedChance() => Int32.Parse(optionsValuesCollection["CommanderWoundedChance"]);
        public static int GetCommanderSeverelyInjuredChance() => Int32.Parse(optionsValuesCollection["CommanderSeverelyInjuredChance"]);
        public static int GetCommanderBrutallyMauledChance() => Int32.Parse(optionsValuesCollection["CommanderBrutallyMauledChance"]);
        public static int GetCommanderMaimedChance() => Int32.Parse(optionsValuesCollection["CommanderMaimedChance"]);
        public static int GetCommanderOneLeggedChance() => Int32.Parse(optionsValuesCollection["CommanderOneLeggedChance"]);
        public static int GetCommanderOneEyedChance() => Int32.Parse(optionsValuesCollection["CommanderOneEyedChance"]);
        public static int GetCommanderDisfiguredChance() => Int32.Parse(optionsValuesCollection["CommanderDisfiguredChance"]);

        public static int GetKnightWoundedChance() => Int32.Parse(optionsValuesCollection["KnightWoundedChance"]);
        public static int GetKnightSeverelyInjuredChance() => Int32.Parse(optionsValuesCollection["KnightSeverelyInjuredChance"]);
        public static int GetKnightBrutallyMauledChance() => Int32.Parse(optionsValuesCollection["KnightBrutallyMauledChance"]);
        public static int GetKnightMaimedChance() => Int32.Parse(optionsValuesCollection["KnightMaimedChance"]);
        public static int GetKnightOneLeggedChance() => Int32.Parse(optionsValuesCollection["KnightOneLeggedChance"]);
        public static int GetKnightOneEyedChance() => Int32.Parse(optionsValuesCollection["KnightOneEyedChance"]);
        public static int GetKnightDisfiguredChance() => Int32.Parse(optionsValuesCollection["KnightDisfiguredChance"]);

        public static bool GetOptInPreReleases()
        {
            return optionsValuesCollection.TryGetValue("OptInPreReleases", out var value) && bool.TryParse(value, out bool result) && result;
        }
    }
}
