using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using CrusaderWars.data.save_file;
using static CrusaderWars.data.save_file.Writter;
using CrusaderWars.twbattle; // Added for BattleFile access
using System.Globalization; // Added for CultureInfo
using CrusaderWars.armies; // Added for List<Army>
using CrusaderWars.unit_mapper;


namespace CrusaderWars.data.battle_results
{
    public static class CasualtyProcessor
    {
        private enum DataType
        {
            Alive,
            Kills
        }

        public static void ReadAttilaResults(Army army, string path_attila_log)
        {
            Program.Logger.Debug($"Reading Attila results for army {army.ID} from log: {path_attila_log}");
            try
            {
                UnitsResults units = new UnitsResults();
                List<(string Script, string Type, string CultureID, string Remaining)> Alive_MainPhase =
                    new List<(string Script, string Type, string CultureID, string Remaining)>();
                List<(string Script, string Type, string CultureID, string Remaining)> Alive_PursuitPhase =
                    new List<(string Script, string Type, string CultureID, string Remaining)>();
                List<(string Script, string Type, string CultureID, string Kills)> Kills_MainPhase =
                    new List<(string Name, string Type, string CultureID, string Kills)>();
                List<(string Script, string Type, string CultureID, string Kills)> Kills_PursuitPhase =
                    new List<(string Script, string Type, string CultureID, string Kills)>();

                var (AliveList, KillsList) = BattleResultReader.GetRemainingAndKills(path_attila_log);
                if (AliveList.Count == 0)
                {
                    Program.Logger.Debug(
                        $"Warning: No battle reports found in Attila log for army {army.ID}. Assuming no survivors or battle did not generate logs.");
                }
                else if (AliveList.Count == 1)
                {
                    Program.Logger.Debug($"Single battle phase detected for army {army.ID}.");
                    Alive_MainPhase = ReturnList(army, AliveList.Last(), DataType.Alive);
                    units.SetAliveMainPhase(Alive_MainPhase);
                    Kills_MainPhase = ReturnList(army, KillsList.Last(), DataType.Kills);
                    units.SetKillsMainPhase(Kills_MainPhase);

                }
                else if (AliveList.Count > 1)
                {
                    Program.Logger.Debug(
                        $"Multiple battle reports found for army {army.ID}. Using the last two reports for Main and Pursuit phases.");
                    // Use the second-to-last report for the Main Phase
                    Alive_MainPhase = ReturnList(army, AliveList[AliveList.Count - 2], DataType.Alive);
                    units.SetAliveMainPhase(Alive_MainPhase);

                    // Use the very last report for the Pursuit Phase
                    Alive_PursuitPhase = ReturnList(army, AliveList.Last(), DataType.Alive);
                    units.SetAlivePursuitPhase(Alive_PursuitPhase);

                    Kills_MainPhase = ReturnList(army, KillsList[KillsList.Count - 2], DataType.Kills);
                    units.SetKillsMainPhase(Kills_MainPhase);

                    Kills_PursuitPhase = ReturnList(army, KillsList.Last(), DataType.Kills);
                    units.SetKillsPursuitPhase(Kills_PursuitPhase);
                }

                army.UnitsResults = units;
                army.UnitsResults.ScaleTo100Porcent();

                CreateUnitsReports(army);
                ChangeRegimentsSoldiers(army);
            }
            catch (Exception e)
            {
                Program.Logger.Debug($"Error reading Attila results for army {army.ID}: {e.ToString()}");
                MessageBox.Show($"Error reading Attila results: {e.ToString()}",
                    "Crusader Conflicts: Battle Results Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
                throw new Exception();

            }

        }

        private static List<(string, string, string, string)> ReturnList(Army army, string text, DataType list_type)
        {
            Program.Logger.Debug($"Entering ReturnList for army {army.ID}, data type: {list_type}");
            var list = new List<(string, string, string, string)>();

            MatchCollection pattern;
            switch (list_type)
            {
                case DataType.Alive:
                    pattern = Regex.Matches(text,
                        $@"(?<Unit>.+_army{army.ID}_TYPE(?<Type>.+?)_CULTURE(?<Culture>.+?)_.+)-(?<Remaining>.+)");
                    foreach (Match match in pattern)
                    {
                        string unit_script = match.Groups["Unit"].Value;
                        string remaining = match.Groups["Remaining"].Value;
                        string culture_id = match.Groups["Culture"].Value;
                        string type = match.Groups["Type"].Value;

                        list.Add((unit_script, type, culture_id, remaining));
                    }

                    break;
                case DataType.Kills:
                    pattern = Regex.Matches(text,
                        $@"(?<Unit>kills_.+_army{army.ID}_TYPE(?<Type>.+?)_CULTURE(?<Culture>.+?)_.+)-(?<Kills>.+)");
                    foreach (Match match in pattern)
                    {

                        string unit_script = match.Groups["Unit"].Value;
                        string kills = match.Groups["Kills"].Value;
                        string culture_id = match.Groups["Culture"].Value;
                        string type = match.Groups["Type"].Value;

                        list.Add((unit_script, type, culture_id, kills));
                    }

                    break;
            }

            Program.Logger.Debug($"Found {list.Count} entries for army {army.ID}, data type: {list_type}.");
            return list;
        }


        static void ChangeRegimentsSoldiers(Army army)
        {
            Program.Logger.Debug($"Entering ChangeRegimentsSoldiers for army {army.ID}.");

            // Separate ArmyRegiments into levy and non-levy regiments
            var levyArmyRegiments = new List<ArmyRegiment>();
            var nonLevyArmyRegiments = new List<ArmyRegiment>();

            foreach (var armyRegiment in army.ArmyRegiments)
            {
                if (armyRegiment.Type == data.save_file.RegimentType.Commander ||
                    armyRegiment.Type == data.save_file.RegimentType.Knight) continue;

                if (armyRegiment.Type == RegimentType.Levy)
                {
                    levyArmyRegiments.Add(armyRegiment);
                }
                else
                {
                    nonLevyArmyRegiments.Add(armyRegiment);
                }
            }

            // Process non-levy regiments (Men-at-Arms and Garrison)
            foreach (ArmyRegiment armyRegiment in nonLevyArmyRegiments)
            {
                bool isSiegeType = false;
                string unitIdentifier = armyRegiment.MAA_Name; 

                var firstRegiment = armyRegiment.Regiments.FirstOrDefault();
                string? cultureId = firstRegiment?.Culture?.ID;

                Unit? correspondingUnit = army.Units.FirstOrDefault(u =>
                    u.GetRegimentType() == armyRegiment.Type &&
                    u.GetName() == unitIdentifier &&
                    u.GetObjCulture()?.ID == cultureId
                );

                if (correspondingUnit != null && correspondingUnit.IsSiege())
                {
                    isSiegeType = true;
                }

                var unitReport = army.CasualitiesReports.FirstOrDefault(x =>
                    x.GetUnitType() == armyRegiment.Type && x.GetCulture() != null &&
                    x.GetCulture().ID == cultureId && x.GetTypeName() == unitIdentifier);

                foreach (Regiment regiment in armyRegiment.Regiments)
                {
                    if (regiment.Culture is null || string.IsNullOrEmpty(regiment.CurrentNum)) continue;

                    if (isSiegeType)
                    {
                        int finalMachineCount = 0;
                        int originalMachines = Int32.Parse(regiment.Max);

                        if (unitReport != null && unitReport.GetStarting() > 0)
                        {
                            int finalMenCount = unitReport.GetAliveAfterPursuit() != -1 ? unitReport.GetAliveAfterPursuit() : unitReport.GetAliveBeforePursuit();
                            double survivalRate = (double)finalMenCount / unitReport.GetStarting();
                            if (double.IsNaN(survivalRate) || double.IsInfinity(survivalRate)) survivalRate = 0;
                            finalMachineCount = (int)Math.Round(originalMachines * survivalRate);
                        }
                        
                        regiment.SetSoldiers(Math.Min(originalMachines, finalMachineCount).ToString());
                    }
                    else
                    {
                        int originalSoldiers = Int32.Parse(regiment.CurrentNum);
                        int finalSoldierCount = 0;

                        if (unitReport != null && unitReport.GetStarting() > 0)
                        {
                            int finalMenCount = unitReport.GetAliveAfterPursuit() != -1 ? unitReport.GetAliveAfterPursuit() : unitReport.GetAliveBeforePursuit();
                            double survivalRate = (double)finalMenCount / unitReport.GetStarting();
                            if (double.IsNaN(survivalRate) || double.IsInfinity(survivalRate)) survivalRate = 0;
                            finalSoldierCount = (int)Math.Round(originalSoldiers * survivalRate);
                        }
                        
                        regiment.SetSoldiers(Math.Min(originalSoldiers, finalSoldierCount).ToString());
                    }
                }
            }

            // Process levy regiments correctly by culture group
            var levyRegimentsByCulture = levyArmyRegiments
                .SelectMany(ar => ar.Regiments.Where(r => r.Culture?.ID != null))
                .GroupBy(r => r.Culture.ID);
                
            foreach (var cultureGroup in levyRegimentsByCulture)
            {
                string cultureId = cultureGroup.Key;
                
                var levyReports = army.CasualitiesReports.Where(x =>
                    x.GetUnitType() == RegimentType.Levy && 
                    x.GetCulture() != null && 
                    x.GetCulture().ID == cultureId);
                    
                int totalCasualtiesToApply = levyReports.Sum(r => r.GetCasualties());
                
                var regimentsInGroup = cultureGroup.ToList();
                int totalOriginalSoldiers = regimentsInGroup.Sum(r => int.TryParse(r.CurrentNum, out int num) ? num : 0);

                if (totalOriginalSoldiers == 0) continue;

                Program.Logger.Debug($"Processing levy casualties for culture {cultureId}: {totalCasualtiesToApply} total casualties to distribute among {totalOriginalSoldiers} soldiers.");

                int casualtiesAppliedSoFar = 0;
                for (int i = 0; i < regimentsInGroup.Count; i++)
                {
                    var regiment = regimentsInGroup[i];
                    if (string.IsNullOrEmpty(regiment.CurrentNum)) continue;

                    int originalSoldiers = int.Parse(regiment.CurrentNum);
                    int finalSoldiers;
                    int casualtiesForThisRegiment;

                    if (i == regimentsInGroup.Count - 1)
                    {
                        // For the last regiment, assign all remaining casualties to prevent rounding errors.
                        casualtiesForThisRegiment = totalCasualtiesToApply - casualtiesAppliedSoFar;
                    }
                    else
                    {
                        double proportion = (double)originalSoldiers / totalOriginalSoldiers;
                        casualtiesForThisRegiment = (int)Math.Round(totalCasualtiesToApply * proportion);
                    }

                    finalSoldiers = Math.Max(0, originalSoldiers - casualtiesForThisRegiment);
                    casualtiesAppliedSoFar += (originalSoldiers - finalSoldiers);

                    regiment.SetSoldiers(finalSoldiers.ToString());
                    Program.Logger.Debug(
                        $"Levy Regiment {regiment.ID} (Culture: {cultureId}): Soldiers changed from {originalSoldiers} to {finalSoldiers}. Casualties applied: {originalSoldiers - finalSoldiers}.");
                }
            }

            // Update ArmyRegiment totals at the end
            foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
            {
                if (armyRegiment.Type == data.save_file.RegimentType.Commander ||
                    armyRegiment.Type == data.save_file.RegimentType.Knight) continue;
                    
                int army_regiment_total = armyRegiment.Regiments.Where(reg => !string.IsNullOrEmpty(reg.CurrentNum))
                    .Sum(x => Int32.Parse(x.CurrentNum!));
                armyRegiment.CurrentNum = army_regiment_total;
                Program.Logger.Debug(
                    $"Updated ArmyRegiment {armyRegiment.ID} total soldiers to: {army_regiment_total}");
            }
        }

        static void CreateUnitsReports(Army army)
        {
            if (army.UnitsResults == null)
            {
                Program.Logger.Debug($"Warning: army.UnitsResults is null for army {army.ID}. Skipping unit reports creation.");
                return;
            }

            Program.Logger.Debug($"Entering CreateUnitsReports for army {army.ID}. Processing each Attila unit record individually.");
            List<UnitCasualitiesReport> reportsList = new List<UnitCasualitiesReport>();

            // Process every individual unit record from the Main Phase
            foreach (var mainUnit in army.UnitsResults.Alive_MainPhase)
            {
                string script = mainUnit.Script;
                string typeIdentifier = mainUnit.Type;
                string cultureId = mainUnit.CultureID;
                int remaining = Int32.Parse(mainUnit.Remaining);

                Program.Logger.Debug($"Processing individual unit report: Script='{script}', Type='{typeIdentifier}', CultureID='{cultureId}'.");

                RegimentType unitType;
                string reportTypeName;
                int? uniqueId = null;
                string? commanderIdToMatch = null;

                // Parse the type identifier to find the corresponding CK3 unit
                if (typeIdentifier.StartsWith("Levy"))
                {
                    unitType = RegimentType.Levy;
                    Match idMatch = Regex.Match(typeIdentifier, @"(\d+)$");
                    if (idMatch.Success) uniqueId = int.Parse(idMatch.Groups[1].Value);
                }
                else if (typeIdentifier.StartsWith("Garrison"))
                {
                    unitType = RegimentType.Garrison;
                    Match idMatch = Regex.Match(typeIdentifier, @"(\d+)$");
                    if (idMatch.Success) uniqueId = int.Parse(idMatch.Groups[1].Value);
                }
                else if (typeIdentifier.Contains("commander"))
                {
                    unitType = RegimentType.Commander;
                    Match idMatch = Regex.Match(typeIdentifier, @"commander(\d+)");
                    if (idMatch.Success) commanderIdToMatch = idMatch.Groups[1].Value;
                }
                else if (typeIdentifier == "knights")
                {
                    unitType = RegimentType.Knight;
                }
                else
                {
                    unitType = RegimentType.MenAtArms;
                }

                // Find the matching Unit object in the army
                Unit? matchingUnit = null;
                if (uniqueId.HasValue)
                {
                    matchingUnit = army.Units.FirstOrDefault(x => x.UniqueID == uniqueId.Value);
                }
                else if (commanderIdToMatch != null)
                {
                    matchingUnit = army.Units.FirstOrDefault(x => x != null && x.GetCharacterID() == commanderIdToMatch);
                }
                else if (unitType == RegimentType.Knight)
                {
                    matchingUnit = army.Units.FirstOrDefault(u => u.GetRegimentType() == RegimentType.Knight && u.GetObjCulture()?.ID == cultureId);
                }
                else
                {
                    matchingUnit = army.Units.FirstOrDefault(x => x != null && x.GetRegimentType() == unitType && x.GetObjCulture()?.ID == cultureId && x.GetName() == typeIdentifier);
                }

                if (matchingUnit == null)
                {
                    Program.Logger.Debug($"Warning: Could not find matching Unit object for script '{script}'. Skipping.");
                    continue;
                }

                // Determine the display name for the report
                if (unitType == RegimentType.Levy || unitType == RegimentType.Garrison || unitType == RegimentType.Knight)
                {
                    reportTypeName = matchingUnit.GetAttilaUnitKey();
                }
                else if (unitType == RegimentType.Commander)
                {
                    reportTypeName = "General";
                }
                else
                {
                    reportTypeName = matchingUnit.GetName();
                }

                Culture? culture = matchingUnit.GetObjCulture();
                string attilaFaction = matchingUnit.GetAttilaFaction();
                if (culture == null) continue;

                // Calculate starting numbers
                int starting;
                int startingMachines = 0;
                int effectiveNumGuns = matchingUnit.GetNumGuns();
                if (matchingUnit.IsSiegeEnginePerUnit() && effectiveNumGuns <= 0) effectiveNumGuns = 1;

                if (matchingUnit.IsSiege() && effectiveNumGuns > 0)
                {
                    // For multi-gun siege units, we need to know how many machines this specific Attila unit represents.
                    // Since we are now reporting per Attila unit, we use the num_guns value.
                    startingMachines = effectiveNumGuns;
                    starting = UnitMappers_BETA.ConvertMachinesToMen(startingMachines);
                }
                else if (matchingUnit.IsSiege())
                {
                    startingMachines = matchingUnit.GetOriginalSoldiers();
                    starting = UnitMappers_BETA.ConvertMachinesToMen(startingMachines);
                }
                else
                {
                    // For non-siege units, we use the soldiers count from the Attila unit record.
                    // We assume the 'starting' is the same as 'remaining' if no casualties, 
                    // but since we don't have the individual starting count per Attila unit record in the log,
                    // we use the Unit object's soldiers count as a base, distributed if necessary.
                    // However, for MAA/Knights/Commanders, they are usually 1:1 with Attila units.
                    starting = (int)Math.Round(matchingUnit.GetOriginalSoldiers() * (ArmyProportions.BattleScale / 100.0));
                }

                // Find corresponding kills for this specific script
                int kills = 0;
                var killRecord = army.UnitsResults.Kills_MainPhase.FirstOrDefault(k => k.Script == "kills_" + script);
                if (killRecord != default) Int32.TryParse(killRecord.Kills, out kills);

                // Find corresponding pursuit remaining for this specific script
                int? pursuitRemaining = null;
                var pursuitRecord = army.UnitsResults.Alive_PursuitPhase?.FirstOrDefault(p => p.Script == script);
                if (pursuitRecord != null) pursuitRemaining = Int32.Parse(pursuitRecord.Remaining);

                UnitCasualitiesReport unitReport;
                if (pursuitRemaining.HasValue)
                {
                    unitReport = new UnitCasualitiesReport(unitType, reportTypeName, culture, starting, remaining, pursuitRemaining.Value, startingMachines, attilaFaction);
                }
                else
                {
                    unitReport = new UnitCasualitiesReport(unitType, reportTypeName, culture, starting, remaining, startingMachines, attilaFaction);
                }

                unitReport.SetKills(kills);
                unitReport.PrintReport();
                reportsList.Add(unitReport);
            }

            army.SetCasualitiesReport(reportsList);
            Program.Logger.Debug($"Created {reportsList.Count} individual unit casualty reports for army {army.ID}.");
        }

        public static void CheckForSlainCommanders(Army army, string path_attila_log)
        {
            Program.Logger.Debug($"Checking for slain commander in army {army.ID}");
            if (army.Commander != null)
            {
                army.Commander.HasGeneralFallen(path_attila_log);
                if (army.Commander.hasFallen)
                {
                    Program.Logger.Debug($"Commander {army.Commander.ID} in army {army.ID} has fallen.");
                }
                else
                {
                    Program.Logger.Debug($"Commander {army.Commander.ID} in army {army.ID} has NOT fallen.");
                }
            }
        }

        public static void CheckForSlainKnights(Army army)
        {
            Program.Logger.Debug($"Checking for slain knights in army {army.ID}");
            if (army.Knights == null || !army.Knights.HasKnights()) return;

            // --- Part 1: Handle prominent knights leading MAA units ---
            if (army.Units != null && army.CasualitiesReports != null)
            {
                foreach (var unit in army.Units.Where(u => u.KnightCommander != null))
                {
                    var report = army.CasualitiesReports.FirstOrDefault(r =>
                        r.GetUnitType() == unit.GetRegimentType() &&
                        r.GetTypeName() == unit.GetName() &&
                        r.GetCulture()?.ID == unit.GetObjCulture()?.ID);

                    if (report != null && report.GetStarting() > 0)
                    {
                        int finalSoldiers = report.GetAliveAfterPursuit() != -1 ? report.GetAliveAfterPursuit() : report.GetAliveBeforePursuit();
                        int casualties = report.GetStarting() - finalSoldiers;
                        double casualtyPercentage = (double)casualties / report.GetStarting();

                        // New probabilistic logic
                        unit.KnightCommander.CalculateMAACommanderFate(casualtyPercentage);
                    }
                }
            }

            // --- Part 2: Handle the combined knights unit AND prominent bodyguard units ---
            if (army.UnitsResults != null)
            {
                List<(string Script, string Type, string CultureID, string Remaining)> allKnightUnitReports = new();

                // First, try to get reports from the pursuit phase
                if (army.UnitsResults.Alive_PursuitPhase != null && army.UnitsResults.Alive_PursuitPhase.Any())
                {
                    allKnightUnitReports = army.UnitsResults.Alive_PursuitPhase.Where(x => x.Type.StartsWith("knight")).ToList();
                    Program.Logger.Debug($"Found {allKnightUnitReports.Count} knight reports in pursuit phase for army {army.ID}.");
                }

                // If no reports in pursuit phase, try the main phase
                if (!allKnightUnitReports.Any() && army.UnitsResults.Alive_MainPhase != null && army.UnitsResults.Alive_MainPhase.Any())
                {
                    allKnightUnitReports = army.UnitsResults.Alive_MainPhase.Where(x => x.Type.StartsWith("knight")).ToList();
                    Program.Logger.Debug($"Found {allKnightUnitReports.Count} knight reports in main phase for army {army.ID}.");
                }

                // Handle combined unit first
                var combinedUnitReport = allKnightUnitReports.FirstOrDefault(r => r.Type == "knights");
                if (combinedUnitReport != default)
                {
                    int remaining = 0;
                    Int32.TryParse(combinedUnitReport.Remaining, out remaining);
                    army.Knights.GetKilled(remaining);
                }
            }


            // --- Final Log ---
            foreach (var knight in army.Knights.GetKnightsList().Where(k => k.HasFallen()))
            {
                Program.Logger.Debug($"Knight {knight.GetID()} ({knight.GetName()}) in army {army.ID} has fallen.");
            }
        }

        public static void CheckKnightsKills(Army army)
        {
            Program.Logger.Debug($"Checking knight kills for army {army.ID}");
            if (army.Knights != null && army.Knights.HasKnights())
            {
                var knightKillsReport = army.UnitsResults!.Kills_MainPhase.FirstOrDefault(x => x.Type == "knights");
                int kills = 0;
                if (knightKillsReport.Item4 != null)
                {
                    Int32.TryParse(knightKillsReport.Item4, out kills);
                }

                army.Knights.GetKills(kills);
                Program.Logger.Debug($"Total knight kills for army {army.ID}: {kills}");
            }

        }

        public static void LogPostBattleReport(List<Army> armies, Dictionary<string, int> originalSizes, string side)
        {
            Program.Logger.Debug($"******************** POST-BATTLE CASUALTY REPORT: {side}S ********************");

            foreach (var army in armies)
            {
                Program.Logger.Debug($"--- Army ID: {army.ID} ({army.CombatSide.ToUpper()}) ---");

                // Regular Regiments (Levy, MenAtArms, Garrison)
                Program.Logger.Debug("  REGIMENTS:");
                if (army.ArmyRegiments != null)
                {
                    foreach (var armyRegiment in army.ArmyRegiments)
                    {
                        if (armyRegiment == null || armyRegiment.Type == RegimentType.Commander || armyRegiment.Type == RegimentType.Knight) continue;

                        string regimentTypeName = armyRegiment.Type == RegimentType.Levy ? "Levy" : armyRegiment.MAA_Name;
                        if (armyRegiment.Type == RegimentType.Garrison) regimentTypeName = "Garrison"; // Explicitly set for garrison

                        // Calculate aggregate casualties for this regiment group
                        int totalOriginalSize = 0;
                        int totalFinalSize = 0;
                        if (armyRegiment.Regiments != null && armyRegiment.Regiments.Any())
                        {
                            foreach (var regiment in armyRegiment.Regiments)
                            {
                                if (regiment == null || string.IsNullOrEmpty(regiment.CurrentNum)) continue;

                                string key = $"{army.ID}_{regiment.ID}";
                                totalOriginalSize += originalSizes.ContainsKey(key) ? originalSizes[key] : 0;
                                totalFinalSize += Int32.Parse(regiment.CurrentNum);
                            }
                        }
                        else if (armyRegiment.Type == RegimentType.Levy)
                        {
                            // For Levies that don't have sub-regiments in the list
                            totalFinalSize = armyRegiment.CurrentNum;
                            // Note: totalOriginalSize should have been captured during initialization
                        }
                        int totalCasualties = totalOriginalSize - totalFinalSize;

                        // Log the high-level summary for the regiment group
                        Program.Logger.Debug($"    Type: {regimentTypeName}, Original: {totalOriginalSize}, Casualties: {totalCasualties}, Remaining: {totalFinalSize}");

                        if (armyRegiment.Regiments != null)
                        {
                            foreach (var regiment in armyRegiment.Regiments)
                            {
                                if (regiment == null || string.IsNullOrEmpty(regiment.CurrentNum)) continue;

                                string key = $"{army.ID}_{regiment.ID}";
                                int originalSize = originalSizes.ContainsKey(key) ? originalSizes[key] : 0;
                                int finalSize = Int32.Parse(regiment.CurrentNum);
                                int casualties = originalSize - finalSize;

                                Program.Logger.Debug($"      - ID: {regiment.ID}, Culture: {regiment.Culture?.ID ?? "N/A"}, Original: {originalSize}, Casualties: {casualties}, Remaining: {finalSize}");
                            }
                        }
                    }
                }

                // Knights
                if (army.Knights != null && army.Knights.HasKnights())
                {
                    Program.Logger.Debug("  KNIGHTS:");
                    int originalKnightCount = army.Knights.GetKnightsList().Count;
                    int fallenKnightCount = army.Knights.GetKnightsList().Count(k => k.HasFallen());
                    int remainingKnightCount = originalKnightCount - fallenKnightCount;
                    Program.Logger.Debug($"    Total Knights: {originalKnightCount}, Fallen: {fallenKnightCount}, Remaining: {remainingKnightCount}");

                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        if (knight.HasFallen())
                        {
                            Program.Logger.Debug($"      - Fallen Knight: {knight.GetName()} (ID: {knight.GetID()})");
                        }
                    }
                }

                // Commander
                if (army.Commander != null)
                {
                    Program.Logger.Debug("  COMMANDER:");
                    if (army.Commander.hasFallen)
                    {
                        Program.Logger.Debug($"    Commander {army.Commander.Name} (ID: {army.Commander.ID}) has fallen.");
                    }
                    else
                    {
                        Program.Logger.Debug($"    Commander {army.Commander.Name} (ID: {army.Commander.ID}) survived.");
                    }
                }
            }
            Program.Logger.Debug("************************************************************************************");
        }
    }
}
