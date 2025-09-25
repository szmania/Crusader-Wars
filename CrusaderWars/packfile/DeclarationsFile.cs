using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;


namespace CrusaderWars
{
    public static class DeclarationsFile
    {
     
        static string filePath = GetFilePath();

        private static string GetFilePath()
        {
            string scriptDirectory = ".\\data\\battle files\\script\\tut_tutorial_battle";
            string fileName = "tut_declarations.lua";
            string fullPath = Path.Combine(scriptDirectory, fileName);

            // Ensure the directory exists
            if (!Directory.Exists(scriptDirectory))
            {
                Directory.CreateDirectory(scriptDirectory);
            }

            // Search for the file
            string[] files = Directory.GetFiles(scriptDirectory, fileName, SearchOption.AllDirectories);

            if (files.Length > 0)
            {
                return files[0];
            }
            else
            {
                // If not found, create an empty file
                File.WriteAllText(fullPath, string.Empty);
                return fullPath;
            }
        }

        static List<Army> stark_armies { get; set; } = new List<Army>();
        static List<Army> bolton_armies { get; set; } = new List<Army>();

        public static List<Army> GetLeftSideArmies() { return stark_armies; }
        public static List<Army> GetRightSideArmies() { return bolton_armies; }

        static void SetArmies(List<Army> attacker, List<Army> defender)
        {
            stark_armies.Clear();
            bolton_armies.Clear();

            foreach (var army in attacker)
            {
                if (army.IsPlayer())
                {
                    stark_armies.Add(army);
                }
                else
                {
                    bolton_armies.Add(army);
                }
            }
            foreach (var army in defender)
            {
                if (army.IsPlayer())
                {
                    stark_armies.Add(army);
                }
                else
                {
                    bolton_armies.Add(army);
                }
            }
        }

        public static void CreateAlliances(List<Army>attacker, List<Army>defender)
        {
            string alliances_ = "--\r\n-- Alliance declarations\r\n--\r\n\r\nAlliances = bm:alliances();\r\n\r\nAlliance_Stark = Alliances:item(1);\r\nAlliance_Bolton = Alliances:item(2);\r\n\r\n";
            
            /*
             *  SET STARK AND BOLTON ARMIES
             */

            SetArmies(attacker, defender);


            for (int i = 1; i <= stark_armies.Count; i++)
            {
                string t = $"Stark_Army{i} = Alliance_Stark:armies():item({i});\r\n";
                alliances_ += t ;
            }

            for (int i = 1; i <= bolton_armies.Count; i++)
            {
                string t = $"Bolton_Army{i} = Alliance_Bolton:armies():item({i});\r\n";
                alliances_ += t;
            }

            File.AppendAllText(filePath, alliances_ + "\n\n");
        }

        public static void CreateAlliances(List<Army> attacker, List<Army> defender, Army player_main_army, Army enemy_main_army)
        {
            string alliances_ = "--\r\n-- Alliance declarations\r\n--\r\n\r\nAlliances = bm:alliances();\r\n\r\nAlliance_Stark = Alliances:item(1);\r\nAlliance_Bolton = Alliances:item(2);\r\n\r\n";

            /*
             *  SET STARK AND BOLTON ARMIES
             */

            SetArmies(attacker, defender);

            stark_armies.Insert(0, player_main_army);
            alliances_ += $"Stark_Army{1} = Alliance_Stark:armies():item({1});\r\n";
            for (int i = 2; i <= stark_armies.Count; i++)
            {
                string t = $"Stark_Army{i} = Alliance_Stark:armies():item({i});\r\n";
                alliances_ += t;
            }

            bolton_armies.Insert(0, enemy_main_army);
            alliances_ += $"Bolton_Army{1} = Alliance_Bolton:armies():item({1});\r\n";
            for (int i = 2; i <= bolton_armies.Count; i++)
            {
                string t = $"Bolton_Army{i} = Alliance_Bolton:armies():item({i});\r\n";
                alliances_ += t;
            }

            File.AppendAllText(filePath, alliances_ + "\n\n");
        }

        public static void AddUnitDeclaration(string unit_name, string unit_script_name)
        {

            string side;

            //Find army declaration side
            string army_id = Regex.Match(unit_name, @"_army(\d+|garrison_army)_").Groups[1].Value;
            int index = -1;

            if (stark_armies[0].MergedArmies != null)
            {
                foreach (var merged_army in stark_armies[0].MergedArmies)
                {
                    if (army_id == merged_army.ID)
                    {
                        army_id = stark_armies[0].ID;
                        break;
                    }
                }
            }

            if(stark_armies.Count > 1)
            {
                if (stark_armies[1].MergedArmies != null)
                {
                    foreach (var merged_army in stark_armies[1].MergedArmies)
                    {
                        if (army_id == merged_army.ID)
                        {
                            army_id = stark_armies[1].ID;
                            break;
                        }
                    }
                }
            }




            index = stark_armies.FindIndex(x => x.ID == army_id);
            if(index == -1)
            {
                if (bolton_armies[0].MergedArmies != null)
                {
                    foreach (var merged_army in bolton_armies[0].MergedArmies)
                    {
                        if (army_id == merged_army.ID)
                        {
                            army_id = bolton_armies[0].ID;
                            break;
                        }
                    }
                }

                
                if(bolton_armies.Count > 1)
                {
                    foreach (var merged_army in bolton_armies[1].MergedArmies)
                    {
                        if (army_id == merged_army.ID)
                        {
                            army_id = bolton_armies[1].ID;
                            break;
                        }
                    }
                }

                index = bolton_armies.FindIndex(x => x.ID == army_id);
                side = $"Bolton_Army{index+1}";
            }
            else
            {
                side = $"Stark_Army{index+1}";
            }
            


            string unit_declaration = $"\n{unit_name} = script_unit:new({side}, \"{unit_script_name}\");";
            File.AppendAllText(filePath, unit_declaration);
            
        }

        public static void DeclareSiegeVariables()
        {
            string supplyLevel = twbattle.Sieges.GetSuppliesLevel();
            string declarations = "\n-- Siege Specific Declarations --\n";
            declarations += $"DEFENDER_SUPPLY_LEVEL = \"{supplyLevel}\"\n";
            File.AppendAllText(filePath, declarations);
        }

        public static void Erase()
        {
            File.WriteAllText(filePath, string.Empty);
        }

    }
}
