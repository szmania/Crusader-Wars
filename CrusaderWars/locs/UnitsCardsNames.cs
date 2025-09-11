using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using CrusaderWars.data.save_file;

namespace CrusaderWars.locs
{
    static class UnitsCardsNames
    {
        static List<string> default_text_files = new List<string>() 
        {
            "CW_special_ability_phases.loc",
            "random_localisation_strings.loc",
            "tutorial_historical_battles.loc.tsv",
            "tutorial_historical_battles_factions.loc.tsv",
            "tutorial_historical_battles_scripted_subtitles.loc",
            "tutorial_historical_battles_uied_component_texts.loc",
            "tutorial_historical_battles_uied_component_texts.loc.tsv",
            "unit_abilities.loc"
        };
        public static void RemoveFiles()
        {

            string path = @".\data\battle files\text\db";
            foreach(var file in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(file);
                if (default_text_files.Exists(x => x == fileName))
                    continue;
                else
                    File.Delete(file);
            }
        }

        public static void ChangeUnitsCardsNames(string Mapper_Name, List<Army> attacker_armies, List<Army> defender_armies)
        {
            SearchMAANamesInLocalizationFiles(attacker_armies);
            SearchMAANamesInLocalizationFiles(defender_armies);

            var unitsCollection = new List<Unit>();
            foreach(Army army in attacker_armies) { unitsCollection.AddRange(army.Units); }
            foreach (Army army in defender_armies) { unitsCollection.AddRange(army.Units); }

            switch (Mapper_Name)
            {
                case "OfficialCW_EarlyMedieval_919Mod":
                    string[] loc_files = Directory.GetFiles(@".\data\units_cards_names\anno domini\");
                    EditUnitCardsFiles(loc_files, unitsCollection);
                    break;
                case "OfficialCW_HighMedieval_MK1212Mod":
                case "OfficialCW_LateMedieval_MK1212Mod":
                case "OfficialCW_Renaissance_MK1212Mod":
                    string[] mk1212_loc_files = Directory.GetFiles(@".\data\units_cards_names\mk1212\");
                    EditUnitCardsFiles(mk1212_loc_files, unitsCollection);
                    break;
                case "OfficialCW_FallenEagle_AgeOfJustinian":
                    string[] aoj_loc_files = Directory.GetFiles(@".\data\units_cards_names\age of justinian\");
                    EditUnitCardsFiles(aoj_loc_files, unitsCollection);
                    break;
                case "OfficialCW_FallenEagle_FallofTheEagle":
                    string[] fte_loc_files = Directory.GetFiles(@".\data\units_cards_names\fall of the eagles\");
                    EditUnitCardsFiles(fte_loc_files, unitsCollection);
                    break;
                case "OfficialCW_FallenEagle_Fireforged-Empire":
                    string[] fe_loc_files = Directory.GetFiles(@".\data\units_cards_names\fireforged empires\");
                    EditUnitCardsFiles(fe_loc_files, unitsCollection);
                    break;
                case "OfficialCW_RealmsInExile_TheDawnlessDays":
                    string[] lotr_loc_files = Directory.GetFiles(@".\data\units_cards_names\dawnless days\");
                    EditUnitCardsFiles(lotr_loc_files, unitsCollection);
                    break;
                case "OfficialCW_AGOT_SevenKingdoms":
                    string[] agot_loc_files = Directory.GetFiles(@".\data\units_cards_names\seven_kingdoms\");
                    EditUnitCardsFiles(agot_loc_files, unitsCollection);
                    break;
            }

        }

        private static void EditUnitCardsFiles(string[] unit_cards_files, List<Unit> allUnits)
        {
            for (int i = 0; i < unit_cards_files.Length; i++)
            {
                string loc_file_path = unit_cards_files[i];
                string loc_file_name = Path.GetFileName(loc_file_path);
                string file_to_edit_path = $@".\data\{loc_file_name}";

                
                if(File.Exists(file_to_edit_path)) 
                    File.Delete(file_to_edit_path);
                
                //Copy original loc file
                File.Copy(loc_file_path, file_to_edit_path);

                //Clears the new one
                File.WriteAllText(file_to_edit_path, string.Empty);

                string edited_names = "";
                using (StreamReader reader = new StreamReader(loc_file_path))
                {
                    string line = "";
                    while (line != null && !reader.EndOfStream)
                    {
                        line = reader.ReadLine();

                        
                        foreach(Unit unit in allUnits)
                        {
                            if (line.Contains($"land_units_onscreen_name_{unit.GetAttilaUnitKey()}\t"))
                            {
                                //Commander
                                if (unit.GetRegimentType() == RegimentType.Commander)
                                {
                                    string commander_name = "Commander";
                                    line = Regex.Replace(line, @"\t(?<UnitName>.+)\t", $"\t{commander_name}\t");
                                }

                                //Knights
                                else if (unit.GetRegimentType() == RegimentType.Knight && unit.GetSoldiers() > 0)
                                {
                                    string knights_name = "Knights";
                                    line = Regex.Replace(line, @"\t(?<UnitName>.+)\t", $"\t{knights_name}\t");
                                }


                                //Levies
                                else if (unit.GetRegimentType() == RegimentType.Levy)
                                {
                                    /*
                                    string levies_name = ReturnLeviesName(regiment.Type);
                                    line = Regex.Replace(line, @"\t(?<UnitName>.+)\t", $"\t{levies_name}\t");
                                    */
                                    continue;
                                }

                                //Men-At-Arms
                                else if (unit.GetRegimentType() == RegimentType.MenAtArms)
                                {
                                    string maaName = unit.GetLocName();
                                    if (string.IsNullOrEmpty(maaName)) maaName = "MenAtArms";
                                    line = Regex.Replace(line, @"\t(?<UnitName>.+)\t", $"\tMAA {maaName}\t");
                                }

                            }

                        }

                        edited_names += line + "\n"; // write to string every line
                    }

                    reader.Dispose();
                }

                string battle_files_path = $@".\data\battle files\text\db\{loc_file_name}";

                File.WriteAllText(file_to_edit_path, edited_names);
                if(File.Exists(battle_files_path))File.Delete(battle_files_path);
                File.Move(file_to_edit_path, battle_files_path);

            }
        }


        static void SearchMAANamesInLocalizationFiles(List<Army> armies)
        {
            List<string> enabledCK3ModsPaths = LandedTitles.GetEnabledModsPaths();
            string defaultCK3LocFilePath = Properties.Settings.Default.VAR_ck3_path.Replace(@"binaries\ck3.exe", @"game\localization\english\regiment_l_english.yml");
            string defaultCK3DLCLocFilePath = Properties.Settings.Default.VAR_ck3_path.Replace(@"binaries\ck3.exe", @"game\localization\english\dlc\fp1\dlc_fp1_regiment_l_english.yml");

            var maaList = new List<Unit>();
            foreach(Army army in armies)
            {
                maaList.AddRange(army.Units.Where(x => x.GetRegimentType() == RegimentType.MenAtArms));
            }

            List<string> allRegimentLocFilesPaths = new List<string>
            {
                defaultCK3LocFilePath,
                defaultCK3DLCLocFilePath

            };
            foreach(string modFolder in enabledCK3ModsPaths)
            {
                if (Directory.Exists($@"{modFolder}\localization\english\"))
                {
                    var files = Directory.GetFiles($@"{modFolder}\localization\english\").ToList();
                    bool doesRegimentLocFileExists = files.Exists(x => x.Contains("regiment_l_"));
                    if (doesRegimentLocFileExists) { 
                        string regimentLocFilePath = files.FirstOrDefault(x => x.Contains("regiment_l_"));
                        allRegimentLocFilesPaths.Add(regimentLocFilePath);
                    }
                }
            }

            foreach(string path in allRegimentLocFilesPaths)
            {
                if (!File.Exists(path)) continue;
                using (StreamReader SR = new StreamReader(path))
                {
                    string line;
                    while ((line = SR.ReadLine()) != null && !SR.EndOfStream)
                    {
                        if (line == " " || line == string.Empty || char.IsUpper(line[1]))
                            continue;

                        foreach (Unit maa in maaList)
                        {
                            string maaScriptName = maa.GetName();
                            if (line.StartsWith($" {maaScriptName}:") && string.IsNullOrEmpty(maa.GetLocName()))
                            {
                                string maaName = Regex.Match(line, @"""(.+)""").Groups[1].Value;
                                var sameMaaGroup = maaList.Where(x => x.GetName() == maaScriptName);
                                foreach (var equalMAA in sameMaaGroup) { equalMAA.SetLocName(RemoveDiacritics(maaName)); }
                            }
                        }
                    }
                }
            }
        }


        static string RemoveDiacritics(string input)
        {
            string normalizedString = input.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

    }
}
