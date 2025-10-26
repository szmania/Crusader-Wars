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
using CrusaderWars.unit_mapper;

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
                case "OfficialCC_DefaultCK3_EarlyMedieval_919Mod":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("anno domini"), unitsCollection);
                    break;
                case "OfficialCC_DefaultCK3_HighMedieval_MK1212Mod":
                case "OfficialCC_DefaultCK3_LateMedieval_MK1212Mod":
                case "OfficialCC_DefaultCK3_Renaissance_MK1212Mod":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("mk1212"), unitsCollection);
                    break;
                case "OfficialCC_TheFallenEagle_AgeOfJustinian":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("age of justinian"), unitsCollection);
                    break;
                case "OfficialCC_TheFallenEagle_FallofTheEagle":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("fall of the eagles"), unitsCollection);
                    break;
                case "OfficialCC_TheFallenEagle_FireforgedEmpires":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("fireforged empires"), unitsCollection);
                    break;
                case "OfficialCC_RealmsInExile_TheDawnlessDays":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("dawnless days"), unitsCollection);
                    break;
                case "OfficialCC_AGOT_SevenKingdoms":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("seven_kingdoms"), unitsCollection);
                    break;
            }

        }

        private static string[] GetLocFilesForPlaythrough(string baseFolderName)
        {
            List<string> allLocFiles = new List<string>();

            // 1. Get base files
            string baseLocPath = @$".\data\units_cards_names\{baseFolderName}\";
            if (Directory.Exists(baseLocPath))
            {
                allLocFiles.AddRange(Directory.GetFiles(baseLocPath));
            }
            else
            {
                Program.Logger.Debug($"Warning: Base localization folder not found at '{baseLocPath}'");
            }

            // 2. Get active submods
            if (!string.IsNullOrEmpty(UnitMappers_BETA.ActivePlaythroughTag))
            {
                var activeSubmods = CrusaderWars.mod_manager.SubmodManager.GetActiveSubmodsForPlaythrough(UnitMappers_BETA.ActivePlaythroughTag);

                // 3. Get submod files
                foreach (var submodTag in activeSubmods)
                {
                    string submodLocPath = @$".\data\units_cards_names\{baseFolderName}_{submodTag}\";
                    if (Directory.Exists(submodLocPath))
                    {
                        Program.Logger.Debug($"Found and adding localization files from submod folder: '{submodLocPath}'");
                        allLocFiles.AddRange(Directory.GetFiles(submodLocPath));
                    }
                }
            }

            return allLocFiles.ToArray();
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
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            edited_names += "\n";
                            continue;
                        }

                        
                        foreach(Unit unit in allUnits)
                        {
                            // Line 219 - Add null check before accessing GetAttilaUnitKey()
                            if (unit?.GetAttilaUnitKey() != null && line.Contains($"land_units_onscreen_name_{unit.GetAttilaUnitKey()}\t"))
                            {
                                string ownerNameSuffix = "";
                                if (unit.GetOwner() != null && !string.IsNullOrEmpty(unit.GetOwner().GetID()))
                                {
                                    string ownerId = unit.GetOwner().GetID();
                                    string? displayName = null;

                                    if (ownerId == DataSearch.Player_Character.GetID())
                                    {
                                        displayName = Reader.GetMetaPlayerName();
                                    }
                                    else
                                    {
                                        var (firstName, nickname) = CharacterDataManager.GetCharacterFirstNameAndNickname(ownerId);
                                        if (!string.IsNullOrEmpty(firstName))
                                        {
                                            if (!string.IsNullOrEmpty(nickname))
                                            {
                                                displayName = $"{firstName} \"{nickname}\"";
                                            }
                                            else
                                            {
                                                displayName = firstName;
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(displayName))
                                        ownerNameSuffix = $" ({displayName})";
                                }

                                string newName = "";
                                bool shouldReplace = false;

                                //Commander
                                if (unit.GetRegimentType() == RegimentType.Commander)
                                {
                                    newName = $"Commander{ownerNameSuffix}";
                                    shouldReplace = true;
                                }
                                //Knights
                                else if (unit.GetRegimentType() == RegimentType.Knight && unit.GetSoldiers() > 0)
                                {
                                    newName = $"Knights{ownerNameSuffix}";
                                    shouldReplace = true;
                                }
                                //Men-At-Arms
                                else if (unit.GetRegimentType() == RegimentType.MenAtArms)
                                {
                                    string maaName = unit.GetLocName();
                                    if (string.IsNullOrEmpty(maaName)) maaName = "Men at Arms";
                                    newName = $"MAA {maaName}{ownerNameSuffix}";
                                    shouldReplace = true;
                                }
                                //Levies
                                else if (unit.GetRegimentType() == RegimentType.Levy)
                                {
                                    var match = Regex.Match(line, @"\t(?<UnitName>.+)\t");
                                    if (match.Success)
                                    {
                                        string originalName = match.Groups["UnitName"].Value;
                                        newName = $"Levy {originalName}{ownerNameSuffix}";
                                        shouldReplace = true;
                                    }
                                }
                                //Garrisons
                                else if (unit.GetRegimentType() == RegimentType.Garrison)
                                {
                                    var match = Regex.Match(line, @"\t(?<UnitName>.+)\t");
                                    if (match.Success)
                                    {
                                        string originalName = match.Groups["UnitName"].Value;
                                        newName = $"Garrison {originalName}{ownerNameSuffix}";
                                        shouldReplace = true;
                                    }
                                }

                                if (shouldReplace)
                                {
                                    line = Regex.Replace(line, @"\t(?<UnitName>.+)\t", $"\t{newName}\t");
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
                        string? regimentLocFilePath = files.FirstOrDefault(x => x.Contains("regiment_l_"));
                        if (regimentLocFilePath != null)
                        {
                            allRegimentLocFilesPaths.Add(regimentLocFilePath);
                        }
                    }
                }
            }

            foreach(string path in allRegimentLocFilesPaths)
            {
                if (!File.Exists(path)) continue;
                using (StreamReader SR = new StreamReader(path))
                {
                    string? line;
                    while ((line = SR.ReadLine()) != null && !SR.EndOfStream)
                    {
                        if (line == " " || line == string.Empty || char.IsUpper(line[1]))
                            continue;

                        foreach (Unit maa in maaList)
                        {
                            if (maa == null) continue; // Added null check for maa
                            string? maaScriptName = maa.GetName();
                            if (!string.IsNullOrEmpty(maaScriptName) && line.StartsWith($" {maaScriptName}:") && string.IsNullOrEmpty(maa.GetLocName()))
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
