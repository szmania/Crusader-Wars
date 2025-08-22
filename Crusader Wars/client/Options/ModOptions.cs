using Crusader_Wars.data.save_file;
using Crusader_Wars.terrain;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Crusader_Wars.client
{
    internal static class ModOptions
    {
        static List<(string option, string value)> optionsValuesCollection;
        public static void StoreOptionsValues(List<(string, string)> OptionsForm_ValuesCollection)
        {
            optionsValuesCollection = new List<(string option, string value)>();
            optionsValuesCollection = OptionsForm_ValuesCollection;
        }

        public static int GetLevyMax()
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "LeviesMax");
            return Int32.Parse(option.value);
        }
        public static int GetInfantryMax()
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "InfantryMax");
            return Int32.Parse(option.value);
        }

        public static int GetRangedMax()
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "RangedMax");
            return Int32.Parse(option.value);
        }
        public static int GetCavalryMax()
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "CavalryMax");
            return Int32.Parse(option.value);
        }

        public static void SetLevyMax(int value)
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "LeviesMax");
            int index = optionsValuesCollection.IndexOf(option);
            optionsValuesCollection[index] = (optionsValuesCollection[index].option,value.ToString());
        }
        public static void SetInfantryMax(int value)
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "InfantryMax");
            int index = optionsValuesCollection.IndexOf(option);
            optionsValuesCollection[index] = (optionsValuesCollection[index].option, value.ToString());
        }

        public static void SetRangedMax(int value)
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "RangedMax");
            int index = optionsValuesCollection.IndexOf(option);
            optionsValuesCollection[index] = (optionsValuesCollection[index].option, value.ToString());
        }
        public static void SetCavalryMax(int value)
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "CavalryMax");
            int index = optionsValuesCollection.IndexOf(option);
            optionsValuesCollection[index] = (optionsValuesCollection[index].option, value.ToString());
        }


        public static int GetBattleScale()
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "BattleScale");
            return Int32.Parse(option.value.Trim('%'));
        }
        
        public static bool GetAutoScale()
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "AutoScaleUnits");
            switch (option.value)
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
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "CloseCK3");
            switch(option.value)
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
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "CloseAttila");
            switch(option.value)
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

        internal enum ArmiesSetup
        {
            All_Controled,
            Friendly_Only,
            All_Separate
        }

        public static ArmiesSetup SeparateArmies()
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "SeparateArmies");
            switch (option.value)
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
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "BattleMapsSize");
            return option.value;
        }


        public static string SetMapSize(int total_soldiers)
        {
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "BattleMapsSize");


            switch (option.value)
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
            if (reg.CurrentNum == null) return null;

            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "FullArmies");

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
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "TimeLimit");
            switch (option.value) 
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
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "DefensiveDeployables");
            switch (option.value)
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
            var option = optionsValuesCollection.FirstOrDefault(x => x.option == "UnitCards");
            switch (option.value)
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


    }
}
