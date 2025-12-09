using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using CrusaderWars.client;
using CrusaderWars.client;
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

            var armiesCollection = attacker_armies.Concat(defender_armies).ToList();

            switch (Mapper_Name)
            {
                case "OfficialCC_DefaultCK3_EarlyMedieval_919Mod":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("anno domini"), armiesCollection);
                    break;
                case "OfficialCC_DefaultCK3_HighMedieval_MK1212Mod":
                case "OfficialCC_DefaultCK3_LateMedieval_MK1212Mod":
                case "OfficialCC_DefaultCK3_Renaissance_MK1212Mod":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("mk1212"), armiesCollection);
                    break;
                case "OfficialCC_TheFallenEagle_AgeOfJustinian":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("age of justinian"), armiesCollection);
                    break;
                case "OfficialCC_TheFallenEagle_FallofTheEagle":
                case "OfficialCC_TheFallenEagle_FireforgedEmpire":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("fall of the eagles"), armiesCollection);
                    break;
                case "OfficialCC_RealmsInExile_TheDawnlessDays":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("dawnless days"), armiesCollection);
                    break;
                case "OfficialCC_AGOT_SevenKingdoms":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough("seven_kingdoms"), armiesCollection);
                    break;
                case "Custom":
                    EditUnitCardsFiles(GetLocFilesForPlaythrough(ModOptions.GetSelectedCustomMapper()), armiesCollection);
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

        private static void EditUnitCardsFiles(string[] unit_cards_files, List<Army> allArmies)
        {
            // Group armies by side and analyze commanders
            var attackerCommanders = new Dictionary<string, string>();
            var defenderCommanders = new Dictionary<string, string>();

            foreach (var army in allArmies)
            {
                var sideDict = army.CombatSide == "attacker" ? attackerCommanders : defenderCommanders;
                if (army.Commander != null && !string.IsNullOrEmpty(army.Commander.ID))
                {
                    string commanderName = GetCommanderDisplayName(army.Commander);
                    sideDict[army.Commander.ID] = commanderName;
                }
            }

            // Determine side commander status
            string? attackerCommanderName = attackerCommanders.Count == 1 ? attackerCommanders.Values.First() : null;
            string? defenderCommanderName = defenderCommanders.Count == 1 ? defenderCommanders.Values.First() : null;


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

                        Match keyMatch = Regex.Match(line, @"land_units_onscreen_name_([^\t]+)");
                        if (keyMatch.Success)
                        {
                            string currentAttilaKey = keyMatch.Groups[1].Value;
                            var matchingUnitsFromAllArmies = allArmies
                                .SelectMany(a => a.Units.Select(u => new { Unit = u, Army = a }))
                                .Where(x => x.Unit.GetAttilaUnitKey() == currentAttilaKey)
                                .ToList();

                            if (matchingUnitsFromAllArmies.Any())
                            {
                                bool isPresentInPlayerArmy = matchingUnitsFromAllArmies.Any(x => x.Army.IsPlayer());
                                bool isPresentInEnemyArmy = matchingUnitsFromAllArmies.Any(x => !x.Army.IsPlayer());

                                if (!(isPresentInPlayerArmy && isPresentInEnemyArmy))
                                {
                                    var representative = matchingUnitsFromAllArmies.First();
                                    var army = representative.Army;
                                    var unitToApply = representative.Unit;

                                    string commanderNameSuffix = "";
                                    string? sideCommander = army.CombatSide == "attacker" ? attackerCommanderName : defenderCommanderName;

                                    if (!string.IsNullOrEmpty(sideCommander))
                                    {
                                        // Side has exactly one commander - use their name
                                        commanderNameSuffix = $" [Cmdr. {sideCommander}]";
                                    }
                                    else if (army.Commander != null)
                                    {
                                        // Individual army has a commander (but side has multiple commanders)
                                        string individualCommanderName = GetCommanderDisplayName(army.Commander);
                                        commanderNameSuffix = $" [Cmdr. {individualCommanderName}]";
                                    }
                                    // If sideCommander is null and army.Commander is null, commanderNameSuffix remains empty


                                    string newName = "";
                                    bool shouldReplace = false;

                                    //Commander
                                    if (unitToApply.GetRegimentType() == RegimentType.Commander)
                                    {
                                        newName = $"Commander{commanderNameSuffix}";
                                        shouldReplace = true;
                                    }
                                    //Knights
                                    else if (unitToApply.GetRegimentType() == RegimentType.Knight && unitToApply.GetSoldiers() > 0)
                                    {
                                        // Combined Unit
                                        if (unitToApply.GetName() == "Knight")
                                        {
                                            var knightsInUnit = army.Knights?.GetKnightsList()?.OrderByDescending(k => k.GetProwess()).ToList() ?? new List<Knight>();
                                            var knightNames = knightsInUnit.Select(k => k.GetName()).Take(5).ToList();
                                            if (knightsInUnit.Count > 5)
                                            {
                                                knightNames.Add("etc...");
                                            }
                                            string knightList = string.Join(" | ", knightNames);
                                            newName = $"Knights ({knightList}){commanderNameSuffix}";
                                        }
                                        // Bodyguard Unit
                                        else
                                        {
                                            newName = $"Knights ({unitToApply.GetName()}){commanderNameSuffix}";
                                        }
                                        shouldReplace = true;
                                    }
                                    //Men-At-Arms
                                    else if (unitToApply.GetRegimentType() == RegimentType.MenAtArms)
                                    {
                                        string maaName = unitToApply.GetLocName();
                                        if (string.IsNullOrEmpty(maaName)) maaName = "Men at Arms";

                                        if (unitToApply.KnightCommander != null)
                                        {
                                            string knightName = unitToApply.KnightCommander.GetName();
                                            newName = $"MAA {maaName} ({knightName}){commanderNameSuffix}";
                                        }
                                        else
                                        {
                                            newName = $"MAA {maaName}{commanderNameSuffix}";
                                        }
                                        shouldReplace = true;
                                    }
                                    //Levies
                                    else if (unitToApply.GetRegimentType() == RegimentType.Levy)
                                    {
                                        var match = Regex.Match(line, @"\t(?<UnitName>.+)\t");
                                        if (match.Success)
                                        {
                                            string originalName = match.Groups["UnitName"].Value;
                                            if (originalName.StartsWith("Levy "))
                                            {
                                                originalName = originalName.Substring("Levy ".Length);
                                            }
                                            newName = $"Levy {originalName}{commanderNameSuffix}";
                                            shouldReplace = true;
                                        }
                                    }
                                    //Garrisons
                                    else if (unitToApply.GetRegimentType() == RegimentType.Garrison)
                                    {
                                        var match = Regex.Match(line, @"\t(?<UnitName>.+)\t");
                                        if (match.Success)
                                        {
                                            string originalName = match.Groups["UnitName"].Value;
                                            if (originalName.StartsWith("Garrison "))
                                            {
                                                originalName = originalName.Substring("Garrison ".Length);
                                            }
                                            newName = $"Garrison {originalName}{commanderNameSuffix}";
                                        shouldReplace = true;
                                    }
                                }

                                    if (shouldReplace)
                                    {
                                        line = Regex.Replace(line, @"\t(?<UnitName>.+)\t", $"\t{newName}\t");
                                    }
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

        public static Dictionary<string, string> GetUnitScreenNames(string Mapper_Name)
        {
            var screenNames = new Dictionary<string, string>();
            string[] locFiles;

            switch (Mapper_Name)
            {
                case "OfficialCC_DefaultCK3_EarlyMedieval_919Mod":
                    locFiles = GetLocFilesForPlaythrough("anno domini");
                    break;
                case "OfficialCC_DefaultCK3_HighMedieval_MK1212Mod":
                case "OfficialCC_DefaultCK3_LateMedieval_MK1212Mod":
                case "OfficialCC_DefaultCK3_Renaissance_MK1212Mod":
                    locFiles = GetLocFilesForPlaythrough("mk1212");
                    break;
                case "OfficialCC_TheFallenEagle_AgeOfJustinian":
                    locFiles = GetLocFilesForPlaythrough("age of justinian");
                    break;
                case "OfficialCC_TheFallenEagle_FallofTheEagle":
                case "OfficialCC_TheFallenEagle_FireforgedEmpire":
                    locFiles = GetLocFilesForPlaythrough("fall of the eagles");
                    break;
                case "OfficialCC_RealmsInExile_TheDawnlessDays":
                    locFiles = GetLocFilesForPlaythrough("dawnless days");
                    break;
                case "OfficialCC_AGOT_SevenKingdoms":
                    locFiles = GetLocFilesForPlaythrough("seven_kingdoms");
                    break;
                case "Custom":
                    locFiles = GetLocFilesForPlaythrough(ModOptions.GetSelectedCustomMapper());
                    break;
                default:
                    locFiles = new string[0];
                    break;
            }

            foreach (var file in locFiles)
            {
                if (!File.Exists(file)) continue;
                using (StreamReader reader = new StreamReader(file))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Match keyMatch = Regex.Match(line, @"land_units_onscreen_name_([^\t]+)");
                        if (keyMatch.Success)
                        {
                            string key = keyMatch.Groups[1].Value;
                            Match nameMatch = Regex.Match(line, @"\t(?<UnitName>[^\t]+)\t");
                            if (nameMatch.Success)
                            {
                                string name = nameMatch.Groups["UnitName"].Value;
                                screenNames[key] = name; // Add or overwrite
                            }
                        }
                    }
                }
            }

            return screenNames;
        }

        private static string GetCommanderDisplayName(CommanderSystem commander)
        {
            if (commander.ID == DataSearch.Player_Character.GetID())
            {
                return Reader.GetMetaPlayerName();
            }
            else
            {
                var (firstName, nickname) = CharacterDataManager.GetCharacterFirstNameAndNickname(commander.ID);
                if (!string.IsNullOrEmpty(firstName))
                {
                    return !string.IsNullOrEmpty(nickname) ? $"{firstName} \"{nickname}\"" : firstName;
                }
            }
            return commander?.Name ?? "Unknown Commander";
        }

        // NEW: Helper method to get formatted unit name for display in reports
        public static string GetFormattedUnitName(Unit unit, Army army)
        {
            string commanderNameSuffix = "";
            
            // Use actual commander name instead of "attacker"/"defender"
            if (army.Commander != null)
            {
                string actualCommanderName = GetCommanderDisplayName(army.Commander);
                commanderNameSuffix = $" [Cmdr. {actualCommanderName}]";
            }
            else
            {
                // Fallback to side name if no commander
                string sideName = army.CombatSide == "attacker" ? "Attacker" : "Defender";
                commanderNameSuffix = $" [Cmdr. {sideName}]";
            }

            switch (unit.GetRegimentType())
            {
                case RegimentType.Commander:
                    return $"Commander ({army.Commander?.Name ?? "Unknown Commander"}){commanderNameSuffix}";
                case RegimentType.Knight:
                    if (ModOptions.CombineKnightsEnabled())
                    {
                        var knightsInUnit = army.Knights?.GetKnightsList()?.OrderByDescending(k => k.GetProwess()).ToList() ?? new List<Knight>();
                        var knightNames = knightsInUnit.Select(k => k.GetName()).Take(5).ToList();
                        if (knightsInUnit.Count > 5)
                        {
                            knightNames.Add("etc...");
                        }
                        string knightList = string.Join(" | ", knightNames);
                        return $"Knights ({knightList}){commanderNameSuffix}";
                    }
                    else
                    {
                        // If not combined, it's a prominent knight's bodyguard unit
                        return $"Knights ({unit.GetName()}){commanderNameSuffix}";
                    }
                case RegimentType.MenAtArms:
                    string maaName = unit.GetLocName();
                    if (string.IsNullOrEmpty(maaName)) maaName = "Men at Arms";
                    if (unit.KnightCommander != null)
                    {
                        return $"MAA {maaName} ({unit.KnightCommander.GetName()}){commanderNameSuffix}";
                    }
                    return $"MAA {maaName}{commanderNameSuffix}";
                case RegimentType.Levy:
                    string levyOriginalName = unit.GetName(); // This is "Levy"
                    // We need to get the specific levy type from the Attila unit key if possible
                    string specificLevyName = unit.GetAttilaUnitKey(); // This is the Attila unit key, e.g., "att_unit_levy_spearmen"
                    // Attempt to get a more user-friendly name from the unit screen names
                    var screenNames = GetUnitScreenNames(UnitMappers_BETA.GetLoadedUnitMapperName() ?? "");
                    if (screenNames.TryGetValue(specificLevyName, out string? friendlyName) && !string.IsNullOrEmpty(friendlyName))
                    {
                        // Remove "Levy " prefix if it exists in the friendly name
                        if (friendlyName.StartsWith("Levy ", StringComparison.OrdinalIgnoreCase))
                        {
                            friendlyName = friendlyName.Substring("Levy ".Length);
                        }
                        return $"Levy {friendlyName}{commanderNameSuffix}";
                    }
                    return $"Levy {levyOriginalName}{commanderNameSuffix}"; // Fallback
                case RegimentType.Garrison:
                    string garrisonOriginalName = unit.GetName(); // This is "Garrison"
                    string specificGarrisonName = unit.GetAttilaUnitKey();
                    var garrisonScreenNames = GetUnitScreenNames(UnitMappers_BETA.GetLoadedUnitMapperName() ?? "");
                    if (garrisonScreenNames.TryGetValue(specificGarrisonName, out string? friendlyGarrisonName) && !string.IsNullOrEmpty(friendlyGarrisonName))
                    {
                        if (friendlyGarrisonName.StartsWith("Garrison ", StringComparison.OrdinalIgnoreCase))
                        {
                            friendlyGarrisonName = friendlyGarrisonName.Substring("Garrison ".Length);
                        }
                        return $"Garrison {friendlyGarrisonName}{commanderNameSuffix}";
                    }
                    return $"Garrison {garrisonOriginalName}{commanderNameSuffix}"; // Fallback
                default:
                    return unit.GetName();
            }
        }
    }
}
