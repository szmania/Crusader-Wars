using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.CompilerServices;
using CrusaderWars.terrain;

namespace CrusaderWars.locs
{
    static class BattleDetails
    {
        // Line 15 - Initialize property
        public static string Name { get; set; } = string.Empty;
        
        public static void SetBattleName(string a)
        {
            Name = a;
        }

        public static void ChangeBattleDetails(int left_total, int right_total, string playerCombatSide, string enemyCombatSide)
        {

            EditButtonVersion();
            EditBattleTextDetails(left_total, right_total);
            EditCombatSidesDetails(playerCombatSide, enemyCombatSide);
            EditTerrainImage();

        }

        private static void EditButtonVersion()
        {
            string version_path = @".\app_version.txt";
            string version = "v1.0.0"; // Default version

            if (!File.Exists(version_path))
            {
                File.WriteAllText(version_path, $"version=\"{version}\"");
            }

            string fileContent = File.ReadAllText(version_path);
            Match match = Regex.Match(fileContent, @"""(.+)""");

            if (match.Success)
            {
                version = match.Groups[1].Value;
            }
            // If parsing fails, 'version' remains "v1.0.0"

            
            string original_buttonVersion_path = @".\data\battle files\text\db\tutorial_historical_battles_uied_component_texts.loc.tsv";
            string copy_path = @".\data\tutorial_historical_battles_uied_component_texts.loc.tsv";
            File.Copy(original_buttonVersion_path, copy_path);
            File.WriteAllText(copy_path, string.Empty);

            string new_data = "";
            using (FileStream btnVersion = File.Open(original_buttonVersion_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(btnVersion))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("uied_component_texts_localised_string_string_NewState_Text_3a000c"))
                    {
                        string new_version = $"uied_component_texts_localised_string_string_NewState_Text_3a000c\tCrusader Conflicts {version}\ttrue";
                        new_data += new_version + "\n";
                        continue;
                    }
                    if (line.Contains("uied_component_texts_localised_string_string_NewState_Text_3a000cm"))
                    {
                        string new_version = $"uied_component_texts_localised_string_string_NewState_Text_3a000cm\tCrusader Conflicts {version}\ttrue";
                        new_data += new_version + "\n";
                        continue;
                    }
                    if (line.Contains("uied_component_texts_localised_string_button_txt_NewState_Text_49000c"))
                    {
                        string new_version = $"uied_component_texts_localised_string_button_txt_NewState_Text_49000c\tCrusader Conflicts {version}\ttrue";
                        new_data += new_version + "\n";
                        continue;
                    }
                    new_data += line + "\n";
                }

                reader.Close();
                btnVersion.Close();
            }

            File.WriteAllText(copy_path, new_data);
            if (File.Exists(original_buttonVersion_path)) File.Delete(original_buttonVersion_path);
            File.Move(copy_path, original_buttonVersion_path);
        }

        private static void EditBattleTextDetails(int left_side_total, int right_side_total)
        {
            string patreon_text = "Special thanks to our patreons for supporting the development of the mod: Grant Swift, Galahad, Kieran Britt, Chris Kelly, Kyle T David, Oron Gabay, I Regret This Already, TheRagingMagnus, Gav, Kyra, Michael Nathan Chananja Klaassen, Lightmare Studios, PTOLEMY, wanderinghobo49, Nico Mangold, Thierry La Fraude, Cameron Alexander Booth and Braden!";

            string original_battle_details_path = @".\data\battle files\text\db\tutorial_historical_battles.loc.tsv";
            string copy_path = @".\data\tutorial_historical_battles.loc.tsv";
            File.Copy(original_battle_details_path, copy_path);
            File.WriteAllText(copy_path, string.Empty);


            string new_data = "";
            using (FileStream battle_details_file = File.Open(original_battle_details_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(battle_details_file))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    //BATTLE NAME
                    if (line.Contains("battles_localised_name_tut_tutorial_battle"))
                    {
                        line = $"battles_localised_name_tut_tutorial_battle\t{Name}\ttrue";
                    }

                    //BATTLE DETAILS
                    if (line.Contains("battles_description_tut_tutorial_battle"))
                    {
                        string player_side_realm_name = CK3LogData.LeftSide.GetRealmName();
                        string enemy_side_realm_name = CK3LogData.RightSide.GetRealmName();

                        double player_total_soldiers = left_side_total;
                        double enemy_total_soldiers = right_side_total;

                        string new_text = $"{player_side_realm_name}" + "  VS  " + $"{enemy_side_realm_name}" +
                                           "\\\\n" +
                                          $"Total Soldiers: {player_total_soldiers}" + "\\\\t||\\\\t" + $"Total Soldiers: {enemy_total_soldiers}" +
                                          "\\\\n\\\\n" +
                                          $"{patreon_text}";

                        line = Regex.Replace(line, @"\t(?<BattleName>.+)\t", $"\t{new_text}\t");
                    }
                    new_data += line + "\n";
                }

                reader.Close();
                battle_details_file.Close();
            }

            File.WriteAllText(copy_path, new_data);
            if(File.Exists(original_battle_details_path))File.Delete(original_battle_details_path);
            File.Move(copy_path, original_battle_details_path);
        }

        private static void EditCombatSidesDetails(string playerCombatSide, string enemyCombatSide)
        {
            string original_attila_file_path = @".\data\battle files\text\db\tutorial_historical_battles_factions.loc.tsv";
            string copy_path = @".\data\tutorial_historical_battles_factions.loc.tsv";
            File.Copy(original_attila_file_path, copy_path);
            File.WriteAllText(copy_path, string.Empty);

            string new_data = "";
            using (FileStream battle_side_file = File.Open(original_attila_file_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(battle_side_file))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    //Enemy Side
                    if (line.Contains("factions_screen_name_historical_house_bolton"))
                    {
                        line = Regex.Replace(line, @"\t()\t", $"\t{enemyCombatSide}\t");
                    }

                    //Player Side
                    if(line.Contains("factions_screen_name_historical_house_stark"))
                    {
                        line = Regex.Replace(line, @"\t()\t", $"\t{playerCombatSide}\t");
                    }

                    new_data += line + "\n";
                }

                reader.Close();
                battle_side_file.Close();
            }

            File.WriteAllText(copy_path, new_data);
            if(File.Exists(original_attila_file_path))File.Delete(original_attila_file_path);
            File.Move(copy_path, original_attila_file_path);
        }

        private static void EditTerrainImage()
        {
            /*  SNOW VALUES
             * MildWinter
             * NormalWinter
             * HarshWinter
             * 
             * nullOrEmpty
             */

            /*  SEASONS VALUES
             * winter
             * fall
             * spring
             * summer
             * 
             * null value = random
             */

            
            string images_folder_path = @".\data\terrains_images\";

            string image_to_copy_path = "";

            
            string terrain = TerrainGenerator.TerrainType ?? string.Empty;
            string weather = Weather.Season ?? string.Empty;
            Weather.WinterSeverity? snow = Weather.Winter_Severity; // Changed to nullable enum
            bool hasSnow = Weather.HasWinter;
            


            //For each terrain folder
            foreach (var folder_path in Directory.GetDirectories(images_folder_path))
            {
                string folder_name = Path.GetFileName(folder_path);

                if (terrain == folder_name)
                {
                    //For each image on folder
                    foreach(var image_path in Directory.GetFiles(folder_path))
                    {
                        string image_name = Path.GetFileNameWithoutExtension(image_path);
                        terrain = FirstCharSubstring(terrain);
                        

                        //Terrain Image
                        if(weather == "random" && image_name == terrain)
                        {
                            image_to_copy_path = image_path;
                            break;
                        }

                        //Terrain Image + Weather
                        if(weather != "random" && !hasSnow && image_name == $"{terrain}_{weather}")
                        {
                            image_to_copy_path = image_path;
                            break;
                        }
                        //Terrain Image + Weather + Snow
                        // Added null guard for 'snow' parameter
                        if (weather != "random" && hasSnow && snow.HasValue && image_name == $"{terrain}_{weather}_{GetSnow(snow.Value)}")
                        {
                            image_to_copy_path = image_path;
                            break;
                        }

                    }

                    break;
                }
            }

            string default_image_path = @".\data\terrains_images\screenshot_small.png";
            
            //Default Version Image
            if(string.IsNullOrEmpty(image_to_copy_path))
            {
                image_to_copy_path = default_image_path;
            }

            string battle_files_image_path = @".\data\battle files\script\tut_tutorial_battle\screenshot_small.png";

            if(File.Exists(battle_files_image_path)) File.Delete(battle_files_image_path);
            File.Copy(image_to_copy_path, battle_files_image_path);


        }



        private static string FirstCharSubstring(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            if (input == "Desert Mountains") return "desert mountains";

            return $"{input[0].ToString().ToLower()}{input.Substring(1)}";
        }

        private static string GetSnow(Weather.WinterSeverity snow_severity)
        {
            switch(snow_severity)
            {
                case Weather.WinterSeverity.Mild:
                    return "mildsnow";
                case Weather.WinterSeverity.Normal:
                    return "normalsnow";
                case Weather.WinterSeverity.Harsh:
                    return "harshsnow";
                default:
                    return "";
            }
        }


    }
}
