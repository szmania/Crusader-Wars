using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CrusaderWars.data.save_file
{
    
    //Change to internal later
    public static class LandedTitles
    {
        static List<string> EnabledMods;

        public static List<string> GetEnabledModsPaths() { return EnabledMods_Folders_Paths; }

        public static void ReadProvinces(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Reading landed titles...");
            ReadEnabledMods();
            ReadEachModPath();
            ReadModsLandedTitles(attacker_armies, defender_armies);
            ReadDefaultCK3LandedTitles(attacker_armies, defender_armies);
        }
        static void ReadEnabledMods()
        {
            string ck3_mods_folder_path = Properties.Settings.Default.VAR_log_ck3;
            ck3_mods_folder_path = Regex.Replace(ck3_mods_folder_path, @"console_history.txt", "mod");

            string ck3_mods_enabled_file_path = Regex.Replace(ck3_mods_folder_path, "mod", "dlc_load.json");
            Program.Logger.Debug($"Reading enabled mods from: {ck3_mods_enabled_file_path}");
            string enabled_mods_json_text = File.ReadAllText(ck3_mods_enabled_file_path);
            var json = JsonSerializer.Deserialize<Dictionary<string, string[]>>(enabled_mods_json_text);
            EnabledMods = json.ElementAt(0).Value.ToList();
            for(int i = 0; i < EnabledMods.Count; i++)
            {
                EnabledMods[i] = EnabledMods[i].Replace("mod/", "");
            }
            Program.Logger.Debug($"Found {EnabledMods.Count} enabled mods.");
        }

        static List<string> EnabledMods_Folders_Paths = new List<string>();
        static void ReadEachModPath()
        {
            string ck3_mods_folder_path = Properties.Settings.Default.VAR_log_ck3;
            ck3_mods_folder_path = Regex.Replace(ck3_mods_folder_path, @"console_history.txt", "mod");
            var all_mods_paths = Directory.GetFiles(ck3_mods_folder_path);

            
            foreach(var mod_path in all_mods_paths)
            {
                foreach(string enabled_mod in EnabledMods)
                {
                    if(mod_path.Contains(enabled_mod))
                    {
                        using (StreamReader sr = new StreamReader(mod_path))
                        {
                            while(!sr.EndOfStream)
                            {
                                string line = sr.ReadLine();
                                if (line == null) break;

                                if(line.StartsWith("path="))
                                {
                                    string path = Regex.Match(line, @"path=""([^""]*)""").Groups[1].Value;

                                    //NON-STEAM VERSION
                                    if (path.StartsWith("mod/"))
                                    {
                                        string non_steam_path;
                                        non_steam_path = ck3_mods_folder_path.Replace("mod", "") + path.Replace('/', '\\');
                                        EnabledMods_Folders_Paths.Add(non_steam_path);
                                        Program.Logger.Debug($"Found non-Steam mod path: {non_steam_path}");
                                    }
                                    //STEAM VERSION
                                    else
                                    {
                                        string steam_path;
                                        steam_path = path.Replace('/', '\\');
                                        EnabledMods_Folders_Paths.Add(steam_path);
                                        Program.Logger.Debug($"Found Steam mod path: {steam_path}");
                                    }


                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }


        static void ReadModsLandedTitles(List<Army> attacker_armies, List<Army> defender_armies)
        {
            foreach(var mod_folder_path in EnabledMods_Folders_Paths)
            {
                string landed_titles_dir = mod_folder_path.Trim('"') + @"\common\landed_titles";
                if (Directory.Exists(landed_titles_dir))
                {
                    Program.Logger.Debug($"Scanning for landed titles files in mod directory: {landed_titles_dir}");
                    string[] titleFiles = Directory.GetFiles(landed_titles_dir, "*.txt");
                    foreach (string file in titleFiles)
                    {
                        ReadLandedTitles(file, attacker_armies, defender_armies);
                    }
                }
                else
                {
                    Program.Logger.Debug($"Landed titles directory not found for mod: {landed_titles_dir}");
                }
            }
        }

        static void ReadDefaultCK3LandedTitles(List<Army> attacker_armies, List<Army> defender_armies)
        {
            string ck3_exe_path = Properties.Settings.Default.VAR_ck3_path;
            string default_landed_titles_dir = Regex.Replace(ck3_exe_path, @"binaries\\ck3.exe", @"game\\common\\landed_titles");

            if (Directory.Exists(default_landed_titles_dir))
            {
                Program.Logger.Debug($"Scanning for landed titles files in default CK3 directory: {default_landed_titles_dir}");
                string[] titleFiles = Directory.GetFiles(default_landed_titles_dir, "*.txt");
                foreach (string file in titleFiles)
                {
                    ReadLandedTitles(file, attacker_armies, defender_armies);
                }
            }
            else
            {
                Program.Logger.Debug($"Default CK3 landed titles directory not found: {default_landed_titles_dir}");
            }
        }

        static void ReadLandedTitles(string file_path, List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug($"Reading landed titles from file: {file_path}");
            using (StreamReader reader = new StreamReader(file_path))
            {
                string line = reader.ReadLine();
                string empire = "";
                string empire_capital = "";
                string kingdom = "";
                string kingdom_capital = "";
                string duchy = "";
                string duchy_capital = "";
                string county = "";
                string barony = "";

                bool empire_started = false;
                bool kingdom_started = false;
                bool duchy_started = false;
                bool county_started = false;
                bool barony_started = false;

                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (line == null) break;

                    //Empire Key
                    if (Regex.IsMatch(line, @"e_.+ = {"))
                    {
                        empire = Regex.Match(line, @"(e_.+) = {").Groups[1].Value;
                        empire_started = true;
                    }
                    else if(empire_started && line.StartsWith("\tcapital"))
                    {
                        empire_capital = Regex.Match(line, @"capital = (\w+)").Groups[1].Value;
                        SetTitleRegimentsCountiesKeys(attacker_armies, empire_capital, empire);
                        SetTitleRegimentsCountiesKeys(defender_armies, empire_capital, empire);
                    }
                    //Kingdom Key
                    if (Regex.IsMatch(line, @"k_.+ = {"))
                    {
                        kingdom = Regex.Match(line, @"(k_.+) = {").Groups[1].Value;
                        kingdom_started = true;
                    }
                    else if (kingdom_started && line.StartsWith("\t\tcapital"))
                    {
                        kingdom_capital = Regex.Match(line, @"capital = (\w+)").Groups[1].Value;
                        SetTitleRegimentsCountiesKeys(attacker_armies, kingdom_capital, kingdom);
                        SetTitleRegimentsCountiesKeys(defender_armies, kingdom_capital, kingdom);
                    }
                    //Duchy Key
                    if (Regex.IsMatch(line, @"d_.+ = {"))
                    {
                        duchy = Regex.Match(line, @"(d_.+) = {").Groups[1].Value;
                        duchy_started = true;
                    }
                    else if (duchy_started && line.StartsWith("\t\t\tcapital"))
                    {
                        duchy_capital = Regex.Match(line, @"capital = (\w+)").Groups[1].Value;
                        SetTitleRegimentsCountiesKeys(attacker_armies, duchy_capital, duchy);
                        SetTitleRegimentsCountiesKeys(defender_armies, duchy_capital, duchy);
                    }
                    //County Key
                    if (Regex.IsMatch(line, @"c_.+ = {"))
                    {
                        county = Regex.Match(line, @"(c_.+) = {").Groups[1].Value;
                        county_started = true;
                        SetTitleRegimentsCountiesKeys(attacker_armies, county, county);
                        SetTitleRegimentsCountiesKeys(defender_armies, county, county);
                    }
                    //Barony Key
                    else if (county_started && Regex.IsMatch(line, @"b_.+ = {"))
                    {
                        barony_started = true;
                        barony = Regex.Match(line, @"(b_.+) = {").Groups[1].Value;
                    }
                    else if (county_started && barony_started && line.Contains("province ="))
                    {
                        string province_id = Regex.Match(line, @"\d+").Value;
                        SetRegimentsCountiesKeys(attacker_armies, county, province_id);
                        SetRegimentsCountiesKeys(defender_armies, county, province_id);

                        barony_started = false;
                    }

                    //Finishers
                    else if (line.StartsWith("}"))
                    {
                        empire = "";
                        empire_capital = "";
                        empire_started = false;
                    }
                    else if (line.StartsWith("\t}"))
                    {
                        kingdom = "";
                        kingdom_capital = "";
                        kingdom_started = false;
                    }
                    else if (line.StartsWith("\t\t}"))
                    {
                        duchy = "";
                        duchy_capital = "";
                        duchy_started = false;
                    }
                    else if (line.StartsWith("\t\t\t}"))
                    {
                        county = "";
                        county_started = false;
                    }
                    else if (line.StartsWith("\t\t\t\t}"))
                    {
                        barony = "";
                        barony_started = false;
                    }
                }
                reader.Close();
            }
        }
        static void SetTitleRegimentsCountiesKeys(List<Army> armies, string county_key, string title_key)
        {

            foreach (Regiment regiment in armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiments => armyRegiments.Regiments))
            {
                if (!string.IsNullOrEmpty(regiment.OwningTitle) && regiment.OriginKey == title_key && string.IsNullOrEmpty(regiment.GetCountyKey()) && !regiment.isMercenary())
                {
                    regiment.StoreCountyKey(county_key);
                }

            }
        }

        static void SetRegimentsCountiesKeys(List<Army> armies, string county_key, string province_id)
        {

            foreach (Regiment regiment in armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiments => armyRegiments.Regiments))
            {
                if (regiment.Origin == province_id && string.IsNullOrEmpty(regiment.GetCountyKey()) && !regiment.isMercenary())
                {
                    regiment.StoreCountyKey(county_key);
                }
            }

        }


        /*
        static void SetRegimentsCountiesKeys(List<Army> armies, string county_key, string title_key)
        {

            foreach (Regiment regiment in armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiments => armyRegiments.Regiments))
            {
                if (regiment.OriginKey == title_key && string.IsNullOrEmpty(regiment.GetCountyKey()) && !regiment.isMercenary())
                {
                    regiment.StoreCountyKey(county_key);
                }
            }

        }
        */

        /*
        static void ReadLandedTitles(string file_path, List<Army> attacker_armies, List<Army> defender_armies)
        {
            using (StreamReader reader = new StreamReader(file_path))
            {
                string line = reader.ReadLine();
                string county = "";
                bool county_started = false;
                bool barony_started = false;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (line == null) break;

                    //County Key
                    if (Regex.IsMatch(line, @"c_.+ = {"))
                    {
                        county = Regex.Match(line, @"(c_.+) = {").Groups[1].Value;
                        county_started = true;
                    }
                    //Barony Key
                    else if(county_started && Regex.IsMatch(line, @"b_.+ = {"))
                    {
                        barony_started = true;
                    }
                    else if (county_started && barony_started && line.Contains("province ="))
                    {
                        string province_id = Regex.Match(line, @"\d+").Value;
                        SetRegimentsCountiesKeys(attacker_armies, county, province_id);
                        SetRegimentsCountiesKeys(defender_armies, county, province_id);

                        barony_started = false;
                    }
                }
                reader.Close();
            }
        }
        */

        /*
        static void SetRegimentsCountiesKeys(List<Army> armies, string county_key, string province_id)
        {
            //Armies
            for (int i = 0; i < armies.Count; i++)
            {
                //Army Regiments
                for (int x = 0; x < armies[i].ArmyRegiments.Count; x++)
                {
                    //Regiments
                    if (armies[i].ArmyRegiments[x].Regiments != null)
                    {
                        for (int y = 0; y < armies[i].ArmyRegiments[x].Regiments.Count; y++)
                        {
                            //if county key is set, skip
                            if (!string.IsNullOrEmpty(armies[i].ArmyRegiments[x].Regiments[y].GetCountyKey()))
                            {
                                continue;
                            }
                            string origin = armies[i].ArmyRegiments[x].Regiments[y].Origin;

                            //Regiment Province Origin
                            if (province_id == origin && !armies[i].ArmyRegiments[x].Regiments[y].isMercenary())
                            {
                                armies[i].ArmyRegiments[x].Regiments[y].StoreCountyKey(county_key.Trim());
                            }
                        }
                    }

                }
            }
        }
        */

    }
}
