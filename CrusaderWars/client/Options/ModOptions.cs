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
using System.Xml;
using System.IO;

namespace CrusaderWars.client
{
    public static class ModOptions
    {
        internal static Dictionary<string, string> optionsValuesCollection = new Dictionary<string, string>();
        internal static string SelectedCustomMapper { get; set; } = string.Empty;

        public static string GetSelectedCustomMapper() { return SelectedCustomMapper; }
        public static int GetLevyMax()
        {
            if (optionsValuesCollection.TryGetValue("LeviesMax", out var value) && int.TryParse(value, out int result)) { return result >= 100 ? result : 300; }
            return 300;
        }

        public static int GetInfantryMax()
        {
            if (optionsValuesCollection.TryGetValue("InfantryMax", out var value) && int.TryParse(value, out int result)) { return result >= 100 ? result : 200; }
            return 200;
        }

        public static int GetRangedMax()
        {
            if (optionsValuesCollection.TryGetValue("RangedMax", out var value) && int.TryParse(value, out int result)) { return result >= 100 ? result : 200; }
            return 200;
        }

        public static int GetCavalryMax()
        {
            if (optionsValuesCollection.TryGetValue("CavalryMax", out var value) && int.TryParse(value, out int result)) { return result >= 50 ? result : 100; }
            return 100;
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
if (optionsValuesCollection.TryGetValue("BattleScale", out var value) && int.TryParse(value.Trim('%'), out int result))
            {
                return result;
            }
            return 100;
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
            switch (optionsValuesCollection["CloseCK3"])
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
            switch (optionsValuesCollection["CloseAttila"])
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


        public static string SetMapSize(int total_soldiers, bool isSiege)
        {
            if (isSiege) return "2000";

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

        public static bool CombineKnightsEnabled()
        {
            if (optionsValuesCollection.TryGetValue("CombineKnights", out var value))
            {
                return value == "Enabled";
            }
            return false; // Default is Disabled
        }

        public static void SetCombineKnights(string value)
        {
            optionsValuesCollection["CombineKnights"] = value;
        }

        public static int CulturalPreciseness()
        {
            return GetLevyMinSize();
        }

        public static int GetLevyMinSize()
        {
            if (optionsValuesCollection.TryGetValue("LevyMinSize", out var value) && int.TryParse(value, out int result))
            {
                return Math.Clamp(result, 1, 10000);
            }
            return 10; // Default
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

public static int GetCommanderWoundedChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderWoundedChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 65;
        }
public static int GetCommanderSeverelyInjuredChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderSeverelyInjuredChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 10;
        }
public static int GetCommanderBrutallyMauledChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderBrutallyMauledChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 5;
        }
public static int GetCommanderMaimedChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderMaimedChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 5;
        }
public static int GetCommanderOneLeggedChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderOneLeggedChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 2;
        }
public static int GetCommanderOneEyedChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderOneEyedChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 3;
        }
public static int GetCommanderDisfiguredChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderDisfiguredChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 2;
        }
        public static int GetCommanderSlainChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderSlainChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            // Fallback for backward compatibility with old settings files
            if (optionsValuesCollection.TryGetValue("CommanderKilledChance", out var oldValue) && int.TryParse(oldValue, out int oldResult))
            {
                return oldResult;
            }
            return 15; // Default value
        }
        public static int GetKnightSlainChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightSlainChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            // Fallback for backward compatibility with old settings files
            if (optionsValuesCollection.TryGetValue("KnightKilledChance", out var oldValue) && int.TryParse(oldValue, out int oldResult))
            {
                return oldResult;
            }
            return 15; // Default value
        }

public static int GetKnightWoundedChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightWoundedChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 65;
        }
public static int GetKnightSeverelyInjuredChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightSeverelyInjuredChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 10;
        }
public static int GetKnightBrutallyMauledChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightBrutallyMauledChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 5;
        }
public static int GetKnightMaimedChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightMaimedChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 5;
        }
public static int GetKnightOneLeggedChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightOneLeggedChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 2;
        }
public static int GetKnightOneEyedChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightOneEyedChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 3;
        }
public static int GetKnightDisfiguredChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightDisfiguredChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 2;
        }

public static int GetCommanderPrisonerChance()
        {
            if (optionsValuesCollection.TryGetValue("CommanderPrisonerChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 60;
        }
public static int GetKnightPrisonerChance()
        {
            if (optionsValuesCollection.TryGetValue("KnightPrisonerChance", out var value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return 60;
        }

        public static string GetSelectedPlaythrough()
        {
            if (optionsValuesCollection.TryGetValue("Playthrough", out var playthrough))
            {
                return playthrough;
            }
            return string.Empty;
        }

        public static bool GetOptInPreReleases()
        {
            return optionsValuesCollection.TryGetValue("OptInPreReleases", out var value) && bool.TryParse(value, out bool result) && result;
        }

        public static bool ShowPostBattleReport()
        {
            if (optionsValuesCollection.TryGetValue("ShowPostBattleReport", out var value))
            {
                return value == "Enabled";
            }
            return true; // Default to enabled
        }

        public static bool GetLinuxSetupCompleted()
        {
            return optionsValuesCollection.TryGetValue("LinuxSetupCompleted", out var value) && bool.TryParse(value, out bool result) && result;
        }
    }
}
