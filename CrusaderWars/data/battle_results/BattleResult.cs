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
    public static class BattleResult
    {

        public static string? CombatID { get; set; }
        public static string? ResultID { get; set; }
        public static string? ProvinceID { get; set; }
        public static string? SiegeID { get; set; }
        public static string? ProvinceName { get; set; }
        public static bool IsAttackerVictorious { get; set; } = false;
        public static double? WarScoreValue { get; set; }
        public static string? WarID { get; set; }
        //public static twbattle.Date FirstDay_Date { get; set; }

        public static string? Original_Player_Combat;
        public static string? Player_CombatResult;
        public static string? Original_Player_CombatResult;


        //Combats
        public static string? Player_Combat;

        public static void ReadPlayerCombat(string playerID)
        {
            Program.Logger.Debug($"Reading player combat for player ID: {playerID}");
            try
            {
                bool isSearchStarted = false;
                string battleID = "";
                StringBuilder sb = new StringBuilder();
                using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Combats_Path()))
                {
                    while (!sr.EndOfStream)
                    {
                        string? line = sr.ReadLine();
                        if (line == null) break;
                        if (Regex.IsMatch(line, @"\t\t\d+={"))
                        {
                            battleID = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;
                        }
                        else if (line == $"\t\t\t\tcommander={playerID}")
                        {
                            break;
                        }
                    }

                    sr.BaseStream.Position = 0;
                    sr.DiscardBufferedData();

                    while (!sr.EndOfStream)
                    {
                        string? line = sr.ReadLine();
                        if (line == null) break;

                        //Battle ID
                        if (!isSearchStarted && line == $"\t\t{battleID}={{")
                        {
                            sb.AppendLine(line);
                            isSearchStarted = true;
                        }
                        //Battle end line
                        else if (isSearchStarted && line == "\t\t}")
                        {
                            sb.AppendLine(line);
                            isSearchStarted = false;
                            break;
                        }
                        else if (isSearchStarted)
                        {
                            sb.AppendLine(line);
                        }
                    }
                }

                BattleResult.CombatID = battleID;
                Player_Combat = sb.ToString();
                Original_Player_Combat = sb.ToString();
                Program.Logger.Debug("Combat ID - " + battleID);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error reading player combat: {ex.Message}");
            }



        }

        public static void ReadCombatBlockByProvinceID()
        {
            Program.Logger.Debug($"Searching for combat block using ProvinceID: {ProvinceID}");
            try
            {
                string battle_id = "";
                StringBuilder sb = new StringBuilder();
                using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Combats_Path()))
                {
                    // First pass to find the battle_id
                    while (!sr.EndOfStream)
                    {
                        string? line = sr.ReadLine();
                        if (line == null) break;
                        if (Regex.IsMatch(line, @"^\t\t\d+={"))
                        {
                            battle_id = Regex.Match(line, @"^\t\t(\d+)={").Groups[1].Value;
                        }
                        else if (line.Trim() == $"province={ProvinceID}")
                        {
                            Program.Logger.Debug($"Found combat block ID: {battle_id} for ProvinceID: {ProvinceID}");
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(battle_id))
                    {
                        Program.Logger.Debug($"Could not find a combat block for ProvinceID: {ProvinceID}");
                        return;
                    }

                    // Second pass to read the block content
                    sr.BaseStream.Position = 0;
                    sr.DiscardBufferedData();

                    bool isSearchStarted = false;
                    while (!sr.EndOfStream)
                    {
                        string? line = sr.ReadLine();
                        if (line == null) break;

                        if (!isSearchStarted && line == $"\t\t{battle_id}={{")
                        {
                            sb.AppendLine(line);
                            isSearchStarted = true;
                        }
                        else if (isSearchStarted && line == "\t\t}")
                        {
                            sb.AppendLine(line);
                            break;
                        }
                        else if (isSearchStarted)
                        {
                            sb.AppendLine(line);
                        }
                    }
                }

                BattleResult.CombatID = battle_id;
                Player_Combat = sb.ToString();
                Original_Player_Combat = sb.ToString();
                Program.Logger.Debug($"Combat block for ID {battle_id} read successfully.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error reading combat block by province ID: {ex.Message}");
            }
        }

        public static void GetPlayerCombatResult()
        {
            Program.Logger.Debug("Getting player combat result...");
            try
            {
                string battle_id = "";
                StringBuilder f = new StringBuilder();
                using (StreamReader sr = new StreamReader(@".\data\save_file_data\CombatResults.txt"))
                {
                    if (twbattle.BattleState.IsSiegeBattle && !twbattle.BattleState.HasReliefArmy && SiegeID != null)
                    {
                        Program.Logger.Debug($"Searching for siege battle result using SiegeID: {SiegeID}");
                        string? current_result_block_id = null;
                        while (!sr.EndOfStream)
                        {
                            string? line = sr.ReadLine();
                            if (line == null) break;

                            Match idMatch = Regex.Match(line, @"\t\t(\d+)={");
                            if (idMatch.Success)
                            {
                                current_result_block_id = idMatch.Groups[1].Value;
                            }
                            else if (line.Trim() == $"siege={SiegeID}") // Trim for safety
                            {
                                battle_id = current_result_block_id ?? ""; // Use the last captured ID
                                Program.Logger.Debug(
                                    $"Found siege battle result block ID: {battle_id} for SiegeID: {SiegeID}");
                                break;
                            }
                        }
                    }
                    else
                    {
                        Program.Logger.Debug($"Searching for field battle result using ProvinceID: {ProvinceID}");
                        while (!sr.EndOfStream)
                        {
                            string? line = sr.ReadLine();
                            if (line == null) break;
                            if (Regex.IsMatch(line, @"\t\t\d+={"))
                            {
                                battle_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;
                            }
                            else if (line.Trim() == $"location={ProvinceID}")
                            {
                                Program.Logger.Debug(
                                    $"Found field battle result block ID: {battle_id} for ProvinceID: {ProvinceID}");
                                break;
                            }
                        }
                    }

                    sr.BaseStream.Position = 0;
                    sr.DiscardBufferedData();

                    bool isSearchStarted = false;
                    while (!sr.EndOfStream)
                    {
                        string? line = sr.ReadLine();
                        if (line == null) break;
                        if (line == $"\t\t{battle_id}={{")
                        {
                            f.AppendLine(line);
                            isSearchStarted = true;
                        }
                        else if (isSearchStarted && line.Contains("\t\t\tstart_date="))
                        {
                            f.AppendLine(line);
                            Match date = Regex.Match(line, @"(?<year>\d+).(?<month>\d+).(?<day>\d+)");
                            string year = date.Groups["year"].Value,
                                month = date.Groups["month"].Value,
                                day = date.Groups["day"].Value;
                            //FirstDay_Date = new twbattle.Date(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day));

                        }
                        else if (isSearchStarted && line == "\t\t}")
                        {
                            f.AppendLine(line);
                            isSearchStarted = false;
                            break;
                        }
                        else if (isSearchStarted)
                        {
                            f.AppendLine(line);
                        }
                    }
                }

                BattleResult.ResultID = battle_id;
                Player_CombatResult = f.ToString();
                Original_Player_CombatResult = f.ToString();
                Program.Logger.Debug("ResultID - " + battle_id);
                Program.Logger.Debug("All combat results were read successfully");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error reading all combat results: {ex.Message}");
            }



        }

        public static void GetPlayerSiege()
        {
            Program.Logger.Debug("Getting player siege data...");
            try
            {
                string siege_id = "";
                string siegesPath = CrusaderWars.data.save_file.Writter.DataFilesPaths.Sieges_Path();

                // First pass: Find the correct siege_id
                using (StreamReader sr = new StreamReader(siegesPath))
                {
                    string? current_siege_id = null;
                    while (!sr.EndOfStream)
                    {
                        string? line = sr.ReadLine();
                        if (line == null) break;

                        if (Regex.IsMatch(line, @"\t\t\d+={"))
                        {
                            current_siege_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;
                        }
                        else if (line.Trim() == $"province={ProvinceID}" && current_siege_id != null)
                        {
                            // We found the province, so the last siege_id we saw is the correct one.
                            siege_id = current_siege_id;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(siege_id))
                {
                    Program.Logger.Debug($"Could not find a siege for ProvinceID: {ProvinceID}");
                    return;
                }

                BattleResult.SiegeID = siege_id;
                Program.Logger.Debug("SiegeID - " + siege_id);
                Program.Logger.Debug("Siege ID identified. No further modification to Sieges.txt in GetPlayerSiege.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error getting player siege data: {ex.Message}");
            }
        }

        public static void SendToSaveFile(string filePath)
        {
            Program.Logger.Debug($"Sending battle results to save file: {filePath}");

            Writter.SendDataToFile(filePath);
            Program.Logger.Debug("Resetting data and collecting garbage.");
            Data.Reset();
            Player_Combat = "";
            ProvinceName = null;
            GC.Collect();
        }

        public static void CalculateAndSetWarScore(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Calculating and setting war score...");
            double newWarScore = CalculateWarScore(attacker_armies, defender_armies);
            WarScoreValue = newWarScore;
            Program.Logger.Debug($"War score calculated and set to: {newWarScore}");
        }





        //---------------------------------//
        //----------Functions--------------//
        //---------------------------------//
        public static (List<string> AliveList, List<string> KillsList) GetRemainingAndKills(string path_attila_log)
        {
            Program.Logger.Debug($"Entering GetRemainingAndKills for log file: {path_attila_log}");
            // Initialize with empty collections
            List<string> aliveList = new();
            List<string> killsList = new();

            if (!File.Exists(path_attila_log))
            {
                Program.Logger.Debug($"Attila log file not found at: {path_attila_log}. Returning empty lists.");
                return (aliveList, killsList);
            }

            string aliveText = "";
            string killsText = "";

            bool aliveSearchStarted = false;
            bool killsSearchStarted = false;

            using (StreamReader reader = new StreamReader(path_attila_log))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == "-----REMAINING SOLDIERS-----!!")
                    {
                        Program.Logger.Debug("Found '-----REMAINING SOLDIERS-----!!' marker.");
                        aliveSearchStarted = true;
                        killsSearchStarted = false;
                    }

                    else if (line == "-----NUMBERS OF KILLS-----!!")
                    {
                        Program.Logger.Debug("Found '-----NUMBERS OF KILLS-----!!' marker.");
                        aliveSearchStarted = false;
                        killsSearchStarted = true;
                    }

                    else if (line == "-----PRINT ENDED-----!!")
                    {
                        Program.Logger.Debug("Found '-----PRINT ENDED-----!!' marker. Adding current report.");
                        aliveList.Add(aliveText);
                        killsList.Add(killsText);
                        aliveText = "";
                        killsText = "";
                        aliveSearchStarted = false;
                        killsSearchStarted = false;
                    }

                    else if (aliveSearchStarted)
                    {
                        aliveText += line + "\n";
                    }

                    else if (killsSearchStarted && line.StartsWith("kills"))
                    {
                        killsText += line + "\n";
                    }
                }
            }

            Program.Logger.Debug(
                $"Found {aliveList.Count} entries for alive reports and {killsList.Count} entries for kill reports.");
            return (aliveList, killsList);
        }

        // Get attila remaining soldiers
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

                var (AliveList, KillsList) = GetRemainingAndKills(path_attila_log);
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

        private enum DataType
        {
            Alive,
            Kills
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
            
            // The logic for updating individual Levy/Garrison unit soldier counts 
            // is now handled in CreateUnitsReports by proportionally distributing casualties 
            // and updating the Unit.Soldiers property.
            // We now focus on updating the Regiment objects for the save file.

            foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
            {
                if (armyRegiment.Type == data.save_file.RegimentType.Commander ||
                    armyRegiment.Type == data.save_file.RegimentType.Knight) continue;

                // Determine if this ArmyRegiment represents a siege unit
                bool isSiegeType = false;
                // The `Unit` objects in army.Units are the *processed* units for Attila.
                // We need to find the Unit that corresponds to this ArmyRegiment's *type* and *name*.
                // For MAA, the Unit.GetName() is armyRegiment.MAA_Name.
                // For Levy, the Unit.GetName() is "Levy".
                // For Garrison, the Unit.GetName() is the specific Attila unit key.
                string unitIdentifier = (armyRegiment.Type == RegimentType.Levy) ? "Levy" : armyRegiment.MAA_Name;

                // Find a corresponding Unit object. We use the culture of the first regiment for matching,
                // assuming all regiments within an ArmyRegiment share the same base unit type and culture for siege status.
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
                    Program.Logger.Debug(
                        $"ArmyRegiment {armyRegiment.ID} (Type: {armyRegiment.Type}, Name: {armyRegiment.MAA_Name}) identified as a siege unit.");
                }
                else
                {
                    Program.Logger.Debug(
                        $"ArmyRegiment {armyRegiment.ID} (Type: {armyRegiment.Type}, Name: {armyRegiment.MAA_Name}) identified as a non-siege unit.");
                }

                // Get the total casualties for this ArmyRegiment type to distribute among its regiments
                var unitReport = army.CasualitiesReports.FirstOrDefault(x =>
                    x.GetUnitType() == armyRegiment.Type && x.GetCulture() != null &&
                    x.GetCulture().ID == cultureId && x.GetTypeName() == unitIdentifier);

                int totalCasualtiesToApply = unitReport?.GetCasualties() ?? 0;

                foreach (Regiment regiment in armyRegiment.Regiments)
                {
                    if (regiment.Culture is null) continue; // skip siege maa

                    // Check if CurrentNum is null or empty before parsing
                    if (string.IsNullOrEmpty(regiment.CurrentNum)) continue;

                    if (isSiegeType)
                    {
                        // NEW LOGIC: Proportional casualties for all siege weapon types
                        int finalMachineCount = 0;
                        int originalMachines = Int32.Parse(regiment.CurrentNum);

                        if (correspondingUnit != null && unitReport != null && unitReport.GetStarting() > 0)
                        {
                            int finalMenCount = unitReport.GetAliveAfterPursuit() != -1 ? unitReport.GetAliveAfterPursuit() : unitReport.GetAliveBeforePursuit();
                            int startingMen = unitReport.GetStarting();

                            double survivalRate = (double)finalMenCount / startingMen;
                            if (double.IsNaN(survivalRate) || double.IsInfinity(survivalRate))
                            {
                                survivalRate = 0;
                            }

                            finalMachineCount = (int)Math.Round(originalMachines * survivalRate);
                        }
                        else
                        {
                            // Fallback or if unit had 0 men to begin with
                            finalMachineCount = 0;
                        }

                        // Cap the final count at the original number to prevent negative casualties from scaling artifacts.
                        int cappedFinalMachineCount = Math.Min(originalMachines, finalMachineCount);

                        regiment.SetSoldiers(cappedFinalMachineCount.ToString());
                        Program.Logger.Debug(
                            $"Siege Regiment {regiment.ID} (Type: {armyRegiment.Type}, Culture: {regiment.Culture?.ID ?? "N/A"}): Machines changed from {originalMachines} to {cappedFinalMachineCount}.");
                    }
                    else
                    {
                        // Fixed logic for non-siege units (soldiers) - distribute casualties correctly
                        int originalSoldiers = Int32.Parse(regiment.CurrentNum); // Capture original value
                        int regSoldiers = originalSoldiers;
                        int casualtiesApplied = 0; // Track actual casualties applied to this regiment
                        
                        // Apply casualties from the total pool
                        while (regSoldiers > 0 && totalCasualtiesToApply > 0)
                        {
                            if (regSoldiers > totalCasualtiesToApply)
                            {
                                casualtiesApplied += totalCasualtiesToApply;
                                regSoldiers -= totalCasualtiesToApply;
                                totalCasualtiesToApply = 0;
                            }
                            else
                            {
                                casualtiesApplied += regSoldiers;
                                totalCasualtiesToApply -= regSoldiers;
                                regSoldiers = 0;
                            }
                        }

                        regiment.SetSoldiers(regSoldiers.ToString());
                        // REMOVED: unitReport.SetCasualties(casualties); - This was causing the bug by modifying shared state
                        Program.Logger.Debug(
                            $"Non-Siege Regiment {regiment.ID} (Type: {armyRegiment.Type}, Culture: {regiment.Culture?.ID ?? "N/A"}): Soldiers changed from {originalSoldiers} to {regSoldiers}. Casualties applied: {casualtiesApplied}.");
                    }
                }

                // Moved these lines outside the inner loop
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
                Program.Logger.Debug(
                    $"Warning: army.UnitsResults is null for army {army.ID}. Skipping unit reports creation.");
                return;
            }

            Program.Logger.Debug($"Entering CreateUnitsReports for army {army.ID}.");
            List<UnitCasualitiesReport> reportsList = new List<UnitCasualitiesReport>();

            // Group by Type and CultureID
            var grouped = army.UnitsResults.Alive_MainPhase.GroupBy(item => new { item.Type, item.CultureID });
            Program.Logger.Debug($"Found {grouped.Count()} unit groups for army {army.ID}.");
            var pursuit_grouped =
                army.UnitsResults.Alive_PursuitPhase?.GroupBy(item => new { item.Type, item.CultureID });
            
            // Group kills by Type and CultureID for proper aggregation
            var kills_grouped = army.UnitsResults.Kills_MainPhase.GroupBy(item => new { item.Type, item.CultureID });

            // Separate knight groups from other groups
            var knightGroups = grouped.Where(g => g.Key.Type.StartsWith("knight")).ToList();
            var otherGroups = grouped.Where(g => !g.Key.Type.StartsWith("knight")).ToList();

            // Process knight groups by aggregating all knights of the same culture
            var knightGroupsByCulture = knightGroups.GroupBy(g => g.Key.CultureID);
            foreach (var cultureGroup in knightGroupsByCulture)
            {
                string cultureId = cultureGroup.Key;
                Program.Logger.Debug(
                    $"Processing aggregated casualty report for knights with CultureID='{cultureId}'.");

                // Get the culture object from the first matching knight unit
                var matchingKnightUnits = army.Units?.Where(u => 
                    u.GetRegimentType() == RegimentType.Knight && 
                    u.GetObjCulture()?.ID == cultureId).ToList();

                if (matchingKnightUnits == null || !matchingKnightUnits.Any())
                {
                    Program.Logger.Debug(
                        $"Warning: Could not find matching knight units for culture ID '{cultureId}' in army {army.ID}. Skipping report for this knight group.");
                    continue;
                }

                Culture? culture = matchingKnightUnits.First().GetObjCulture();
                if (culture == null)
                {
                    Program.Logger.Debug(
                        $"Warning: Could not find valid culture for knight units with culture ID '{cultureId}' in army {army.ID}. Skipping report for this knight group.");
                    continue;
                }

                // Calculate total deployed knights for this culture
                int starting = (int)Math.Round(matchingKnightUnits.Sum(u => u.GetSoldiers()) * (ArmyProportions.BattleScale / 100.0));
                int startingMachines = 0;

                // Calculate total remaining knights for this culture from main phase
                int remaining = cultureGroup.Sum(g => g.Sum(x => Int32.Parse(x.Remaining)));

                // Calculate total kills for this culture
                int totalKills = 0;
                var killsCultureGroup = kills_grouped.Where(kg => kg.Key.Type.StartsWith("knight") && kg.Key.CultureID == cultureId);
                if (killsCultureGroup.Any())
                {
                    totalKills = killsCultureGroup.Sum(kg => kg.Sum(x => Int32.Parse(x.Kills)));
                }

                // Calculate pursuit remaining if available
                int? pursuitRemaining = null;
                if (pursuit_grouped != null)
                {
                    var pursuitCultureGroup = pursuit_grouped.Where(pg => pg.Key.Type.StartsWith("knight") && pg.Key.CultureID == cultureId);
                    if (pursuitCultureGroup.Any())
                    {
                        pursuitRemaining = pursuitCultureGroup.Sum(pg => pg.Sum(x => Int32.Parse(x.Remaining)));
                    }
                }

                // Create a single Unit Report for all knights of this culture
                UnitCasualitiesReport unitReport;
                if (pursuitRemaining.HasValue)
                {
                    unitReport = new UnitCasualitiesReport(RegimentType.Knight, "Knight", culture, starting, remaining, pursuitRemaining.Value, startingMachines);
                }
                else
                {
                    unitReport = new UnitCasualitiesReport(RegimentType.Knight, "Knight", culture, starting, remaining, startingMachines);
                }

                // Set the kills from the aggregated kills data
                unitReport.SetKills(totalKills);

                unitReport.PrintReport();

                reportsList.Add(unitReport);
            }

            Program.Logger.Debug("#############################");
            Program.Logger.Debug($"REPORT FROM {army.CombatSide.ToUpper()} ARMY {army.ID}");
            foreach (var group in otherGroups)
            {
                if (group.Key.Type == null)
                {
                    Program.Logger.Debug(
                        $"Warning: Skipping unit report due to null unit type in group key for army {army.ID}.");
                    continue;
                }

                Program.Logger.Debug(
                    $"Processing casualty report for group: Type='{group.Key.Type}', CultureID='{group.Key.CultureID}'.");

                RegimentType unitType;
                string type; // This is the identifier string (CK3 name, Attila key, or generic "Levy")

                if (group.Key.Type.StartsWith("Levy"))
                {
                    unitType = RegimentType.Levy;
                    type = "Levy"; // Match against the generic unit name "Levy"
                }
                else if (group.Key.Type.Contains("commander")) // Handle commander specifically
                {
                    unitType = RegimentType.Commander;
                    type = "General"; // Match against the generic unit name "General"
                }
                else
                {
                    // This is either a MAA unit (type is CK3 name) or a Garrison unit (type is Attila key).
                    // We need to check if this key corresponds to a Garrison unit in the army.
                    var isGarrisonUnit = army.Units != null && army.Units.Any(u => 
                        u.GetRegimentType() == RegimentType.Garrison && 
                        u.GetAttilaUnitKey() == group.Key.Type);

                    if (isGarrisonUnit)
                    {
                        unitType = RegimentType.Garrison;
                        type = group.Key.Type; // The Attila unit key
                    }
                    else
                    {
                        unitType = RegimentType.MenAtArms;
                        type = group.Key.Type; // The CK3 MAA name
                    }
                }

                // Search for type, culture, starting soldiers and remaining soldiers of a Unit
                if (army.Units == null)
                {
                    continue;
                }

                // Safely get the unit(s), then its culture.
                // FIX: Use GetAttilaUnitKey() for Garrison units for correct matching.
                var matchingUnits = army.Units.Where(x =>
                {
                    if (x == null || x.GetRegimentType() != unitType || x.GetObjCulture()?.ID != group.Key.CultureID)
                    {
                        return false;
                    }

                    if (unitType == RegimentType.Garrison)
                    {
                        // For garrisons, match by Attila unit key
                        return x.GetAttilaUnitKey() == type;
                    }
                    else
                    {
                        // For other unit types, match by name
                        return x.GetName() == type;
                    }
                });

                if (!matchingUnits.Any())
                {
                    Program.Logger.Debug(
                        $"Warning: Could not find matching unit for type '{type}' and culture ID '{group.Key.CultureID}' in army {army.ID}. Skipping report for this unit group.");
                    continue;
                }

                Culture? culture = matchingUnits.First().GetObjCulture();

                // If culture is null at this point, it means either no matching unit was found,
                // or the matching unit itself had a null culture object.
                // This scenario should be logged and skipped to prevent further errors.
                if (culture == null)
                {
                    Program.Logger.Debug(
                        $"Warning: Could not find valid culture for unit type '{type}' and culture ID '{group.Key.CultureID}' in army {army.ID}. Skipping report for this unit group.");
                    continue; // Skip this group if culture is unexpectedly null
                }

                int starting;
                int startingMachines = 0;
                var firstUnit = matchingUnits.First();
                
                int effectiveNumGuns = firstUnit.GetNumGuns();
                if (firstUnit.IsSiegeEnginePerUnit() && effectiveNumGuns <= 0)
                {
                    effectiveNumGuns = 1;
                }

                if (firstUnit.IsSiege() && effectiveNumGuns > 0)
                {
                    // New logic for multi-gun siege units
                    int totalCk3Machines = matchingUnits.Sum(u => u.GetOriginalSoldiers());
                    startingMachines = totalCk3Machines;
                    int numGunsPerUnit = effectiveNumGuns;
                    int numAttilaUnits = (int)Math.Ceiling((double)totalCk3Machines / numGunsPerUnit);
                    
                    int totalMen = 0;
                    for (int j = 0; j < numAttilaUnits; j++)
                    {
                        int machinesForThisUnit = (j == numAttilaUnits - 1)
                            ? totalCk3Machines - (numGunsPerUnit * (numAttilaUnits - 1))
                            : numGunsPerUnit;
                        totalMen += UnitMappers_BETA.ConvertMachinesToMen(machinesForThisUnit);
                    }
                    starting = totalMen;
                }
                else if (firstUnit.IsSiege()) // Old logic for single-entry siege units
                {
                    startingMachines = matchingUnits.Sum(u => u.GetOriginalSoldiers());
                    starting = UnitMappers_BETA.ConvertMachinesToMen(startingMachines);
                }
                else // Not a siege unit
                {
                    starting = (int)Math.Round(matchingUnits.Sum(u => u.GetSoldiers()) * (ArmyProportions.BattleScale / 100.0));
                    startingMachines = 0;
                }

                // Levy Inflation Fix: Remove reverse scaling from 'starting'
                // The 'starting' value should reflect the actual number of soldiers deployed in Attila.
                // Scaling is applied when units are created for Attila, so 'starting' should already be scaled.
                // The previous logic was incorrectly inflating the 'starting' value.

                int remaining = group.Sum(x => Int32.Parse(x.Remaining));

                // Get total kills for this group from the kills data
                int totalGroupKills = 0;
                var killsGroup = kills_grouped.FirstOrDefault(x => x.Key.Type == group.Key.Type && x.Key.CultureID == group.Key.CultureID);
                if (killsGroup != null)
                {
                    totalGroupKills = killsGroup.Sum(x => Int32.Parse(x.Kills));
                }

                // Create a Unit Report of the main casualities as default, if pursuit data is available, it creates one from the pursuit casualties
                UnitCasualitiesReport unitReport;
                var pursuitGroup = pursuit_grouped?.FirstOrDefault(x =>
                    x.Key.Type == group.Key.Type && x.Key.CultureID == group.Key.CultureID);

                if (pursuitGroup != null)
                {
                    int pursuitRemaining = pursuitGroup.Sum(x => Int32.Parse(x.Remaining));
                    unitReport =
                        new UnitCasualitiesReport(unitType, type, culture, starting, remaining, pursuitRemaining, startingMachines);
                }
                else
                {
                    unitReport = new UnitCasualitiesReport(unitType, type, culture, starting, remaining, startingMachines);
                }

                // Set the kills from the aggregated kills data
                unitReport.SetKills(totalGroupKills);

                unitReport.PrintReport();

                reportsList.Add(unitReport);
            }

            army.SetCasualitiesReport(reportsList);
            Program.Logger.Debug($"Created {reportsList.Count} casualty reports for army {army.ID}.");
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

        public static (bool searchStarted, bool isCommander, CommanderSystem? commander, bool isKnight, Knight? knight, Army? army)
            SearchCharacters(string char_id, List<Army> armies)
        {
            // Program.Logger.Debug($"Searching for character ID: {char_id}");
            foreach (Army army in armies)
            {
                if (army == null) continue; // Added null check for army
                if (army.Commander != null && army.Commander.ID == char_id)
                {
                    Program.Logger.Debug($"Commander {char_id} found in army {army.ID}.");
                    return (true, true, army.Commander, false, null, army);
                }
                else if (army.Knights?.GetKnightsList() != null)
                {
                    foreach (Knight knight_u in army.Knights.GetKnightsList())
                    {
                        if (knight_u.GetID() == char_id)
                        {
                            Program.Logger.Debug($"Knight {char_id} found in army {army.ID}.");
                            return (true, false, null, true, knight_u, army);
                        }
                    }
                }

                if (army.MergedArmies != null)
                {
                    foreach (Army mergedArmy in army.MergedArmies)
                    {
                        if (mergedArmy == null) continue; // Added null check for mergedArmy
                        if (mergedArmy.Commander != null && mergedArmy.Commander.ID == char_id)
                        {
                            Program.Logger.Debug(
                                $"Commander {char_id} found in merged army {mergedArmy.ID} (part of main army {army.ID}).");
                            return (true, true, mergedArmy.Commander, false, null, army);
                        }
                        else if (mergedArmy.Knights?.GetKnightsList() != null)
                        {
                            foreach (Knight knight_u in mergedArmy.Knights.GetKnightsList())
                            {
                                if (knight_u.GetID() == char_id)
                                {
                                    Program.Logger.Debug(
                                        $"Knight {char_id} found in merged army {mergedArmy.ID} (part of main army {army.ID}).");
                                    return (true, false, null, true, knight_u, army);
                                }
                            }
                        }
                    }
                }
            }

            // Program.Logger.Debug($"Character ID: {char_id} not found in any army.");
            return (false, false, null, false, null, null);
        }

        public static void DetermineCharacterFates(List<Army> allArmies)
        {
            Program.Logger.Debug("Determining character fates...");

            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.Living_Path()))
            {
                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (Regex.IsMatch(line, @"^\d+={"))
                    {
                        string char_id = Regex.Match(line, @"\d+").Value;
                        var searchData = SearchCharacters(char_id, allArmies);
                        if (searchData.searchStarted && searchData.army != null)
                        {
                            bool hasFallen = (searchData.isCommander && searchData.commander.hasFallen) || (searchData.isKnight && searchData.knight.HasFallen());
                            if (hasFallen)
                            {
                                string? traitsLine = null;
                                int braceCount = line.Count(c => c == '{') - line.Count(c => c == '}');
                                while (braceCount > 0 && (line = streamReader.ReadLine()) != null)
                                {
                                    if (line.Trim().StartsWith("traits={"))
                                    {
                                        traitsLine = line;
                                    }
                                    braceCount += line.Count(c => c == '{');
                                    braceCount -= line.Count(c => c == '}');
                                }

                                if (traitsLine != null)
                                {
                                    bool wasOnLosingSide = (searchData.army.CombatSide == "attacker" && !IsAttackerVictorious) ||
                                                           (searchData.army.CombatSide == "defender" && IsAttackerVictorious);

                                    (bool isSlain, bool isCaptured, string newTraits) healthResult;
                                    if (searchData.isCommander)
                                    {
                                        healthResult = searchData.commander.Health(traitsLine, wasOnLosingSide);
                                        searchData.commander.IsSlain = healthResult.isSlain;
                                        searchData.commander.IsPrisoner = healthResult.isCaptured;
                                    }
                                    else // isKnight
                                    {
                                        healthResult = searchData.knight.Health(traitsLine, wasOnLosingSide);
                                        searchData.knight.IsSlain = healthResult.isSlain;
                                        searchData.knight.IsPrisoner = healthResult.isCaptured;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Program.Logger.Debug("Finished determining character fates.");
        }

        public static void EditLivingFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Living file...");
            var allArmies = attacker_armies.Concat(defender_armies).ToList();
            string playerCharId = DataSearch.Player_Character.GetID();
            string? playerHeirId = DataSearch.Player_Heir_ID;

            // --- Determine which combat side corresponds to the "left" side from CK3 log data ---
            string leftSideCombatRole = "";
            string leftSideParticipantId = CK3LogData.LeftSide.GetMainParticipant().id;
            if (!string.IsNullOrEmpty(leftSideParticipantId))
            {
                if (attacker_armies.Any(a => a.Owner?.GetID() == leftSideParticipantId || a.Commander?.GetID() == leftSideParticipantId || (a.MergedArmies != null && a.MergedArmies.Any(ma => ma.Owner?.GetID() == leftSideParticipantId || ma.Commander?.GetID() == leftSideParticipantId))))
                {
                    leftSideCombatRole = "attacker";
                }
                else if (defender_armies.Any(a => a.Owner?.GetID() == leftSideParticipantId || a.Commander?.GetID() == leftSideParticipantId || (a.MergedArmies != null && a.MergedArmies.Any(ma => ma.Owner?.GetID() == leftSideParticipantId || ma.Commander?.GetID() == leftSideParticipantId))))
                {
                    leftSideCombatRole = "defender";
                }
            }
            Program.Logger.Debug($"Determined Left Side combat role: {leftSideCombatRole}");

            bool playerIsSlain = allArmies.Any(a => a.Commander?.ID == playerCharId && a.Commander.IsSlain);
            if (!playerIsSlain)
            {
                playerIsSlain = allArmies.Any(a => a.Knights?.GetKnightsList().Any(k => k.GetID() == playerCharId && k.IsSlain) ?? false);
            }
            if(playerIsSlain) Program.Logger.Debug($"Player character {playerCharId} was slain.");

            EditPlayerCharacterFiles(playerIsSlain, playerHeirId);

            // --- Apply changes and write to temp file ---
            Program.Logger.Debug("Living file: Applying pre-determined fates...");
            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.Living_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.Living_Path()))
            {
                streamWriter.NewLine = "\n";
                string? line;
                
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (Regex.IsMatch(line, @"^\d+={")) // Start of a character block
                    {
                        List<string> charBlock = new List<string> { line };
                        string char_id = Regex.Match(line, @"\d+").Value;

                        int braceCount = line.Count(c => c == '{') - line.Count(c => c == '}');
                        while (braceCount > 0 && (line = streamReader.ReadLine()) != null)
                        {
                            charBlock.Add(line);
                            braceCount += line.Count(c => c == '{');
                            braceCount -= line.Count(c => c == '}');
                        }

                        var searchData = SearchCharacters(char_id, allArmies);
                        if (searchData.searchStarted && searchData.army != null)
                        {
                            bool isSlain = (searchData.isCommander && searchData.commander.IsSlain) || (searchData.isKnight && searchData.knight.IsSlain);
                            bool isCaptured = (searchData.isCommander && searchData.commander.IsPrisoner) || (searchData.isKnight && searchData.knight.IsPrisoner);

                            if (isSlain)
                            {
                                Program.Logger.Debug($"Character {char_id} was slain. Adding dead_data block and removing alive_data block.");
                                int aliveDataStartIndex = charBlock.FindIndex(l => l.Trim() == "alive_data={");
                                if (aliveDataStartIndex != -1)
                                {
                                    int aliveDataEndIndex = -1;
                                    int aliveBraceCount = 0;
                                    for (int i = aliveDataStartIndex; i < charBlock.Count; i++)
                                    {
                                        aliveBraceCount += charBlock[i].Count(c => c == '{');
                                        aliveBraceCount -= charBlock[i].Count(c => c == '}');
                                        if (aliveBraceCount == 0)
                                        {
                                            aliveDataEndIndex = i;
                                            break;
                                        }
                                    }
                                    if (aliveDataEndIndex != -1)
                                    {
                                        charBlock.RemoveRange(aliveDataStartIndex, aliveDataEndIndex - aliveDataStartIndex + 1);
                                    }
                                }

                                int closingBraceIndex = charBlock.FindLastIndex(l => l.Trim() == "}");
                                if (closingBraceIndex != -1)
                                {
                                    charBlock.Insert(closingBraceIndex, "\tdead_data={");
                                    charBlock.Insert(closingBraceIndex + 1, $"\t\tdate={Date.Year}.{Date.Month}.{Date.Day}");
                                    charBlock.Insert(closingBraceIndex + 2, "\t\treason=death_battle");
                                    charBlock.Insert(closingBraceIndex + 3, "\t}");
                                }
                            }
                            else // Not slain, but could be wounded and/or captured
                            {
                                (bool _, bool __, string newTraits) healthResult;
                                bool wasOnLosingSide = (searchData.army.CombatSide == "attacker" && !IsAttackerVictorious) ||
                                                       (searchData.army.CombatSide == "defender" && IsAttackerVictorious);
                                
                                int traitsLineIndex = charBlock.FindIndex(l => l.Trim().StartsWith("traits={"));
                                if (traitsLineIndex != -1)
                                {
                                    if (searchData.isCommander)
                                    {
                                        healthResult = searchData.commander.Health(charBlock[traitsLineIndex], wasOnLosingSide);
                                    }
                                    else // isKnight
                                    {
                                        healthResult = searchData.knight.Health(charBlock[traitsLineIndex], wasOnLosingSide);
                                    }
                                    charBlock[traitsLineIndex] = healthResult.newTraits;
                                }

                                if (isCaptured)
                                {
                                    string imprisonerId = "";
                                    bool isCharacterOnLeftSide = !string.IsNullOrEmpty(leftSideCombatRole) && searchData.army.CombatSide == leftSideCombatRole;

                                    if (isCharacterOnLeftSide)
                                    {
                                        imprisonerId = CK3LogData.RightSide.GetMainParticipant().id;
                                    }
                                    else
                                    {
                                        imprisonerId = CK3LogData.LeftSide.GetMainParticipant().id;
                                    }

                                    if (string.IsNullOrEmpty(imprisonerId))
                                    {
                                        if (isCharacterOnLeftSide)
                                        {
                                            imprisonerId = CK3LogData.RightSide.GetCommander().id;
                                        }
                                        else
                                        {
                                            imprisonerId = CK3LogData.LeftSide.GetCommander().id;
                                        }
                                        Program.Logger.Debug($"Could not find main participant for imprisoner. Falling back to main commander of winning side. Imprisoner ID: {imprisonerId}");
                                    }

                                    if (!string.IsNullOrEmpty(imprisonerId))
                                    {
                                        string dateString = $"{Date.Year}.{Date.Month}.{Date.Day}";
                                        var prisonBlock = new List<string>
                                        {
                                            "\t\tprison_data={",
                                            $"\t\t\timprisoner={imprisonerId}",
                                            $"\t\t\tdate={dateString}",
                                            $"\t\t\timprison_type_date={dateString}",
                                            "\t\t\ttype=house_arrest",
                                            "\t\t}"
                                        };
                                        int aliveDataIndex = charBlock.FindIndex(l => l.Trim() == "alive_data={");
                                        if (aliveDataIndex != -1)
                                        {
                                            charBlock.InsertRange(aliveDataIndex + 1, prisonBlock);
                                            Program.Logger.Debug($"Character {char_id} captured by {imprisonerId}. Adding prison_data block.");
                                        }
                                        else
                                        {
                                            Program.Logger.Debug($"Warning: Could not find alive_data block for captured character {char_id}.");
                                        }
                                    }
                                    else
                                    {
                                        Program.Logger.Debug($"Warning: Could not determine an imprisoner for captured character {char_id}.");
                                    }
                                }
                            }
                        }

                        if (playerIsSlain && playerHeirId != null && char_id == playerHeirId)
                        {
                            Program.Logger.Debug($"Player was slain. Adding 'was_player=yes' to heir {playerHeirId}.");
                            int playableDataIndex = charBlock.FindIndex(l => l.Trim() == "playable_data={");
                            if (playableDataIndex != -1)
                            {
                                charBlock.Insert(playableDataIndex + 1, "\t\twas_player=yes");
                            }
                            else
                            {
                                Program.Logger.Debug($"Warning: Could not find playable_data block for heir {playerHeirId}.");
                            }
                        }

                        foreach (var blockLine in charBlock)
                        {
                            streamWriter.WriteLine(blockLine);
                        }
                    }
                    else
                    {
                        streamWriter.WriteLine(line);
                    }
                }
            }
            Program.Logger.Debug("Finished editing Living file.");
        }

        public static void EditPlayerCharacterFiles(bool playerIsSlain, string? playerHeirId)
        {
            Program.Logger.Debug("Editing Player Character files...");
            string playedCharPath = Writter.DataFilesPaths.PlayedCharacter_Path();
            string playedCharTempPath = Writter.DataTEMPFilesPaths.PlayedCharacter_Path();
            string currentlyPlayedPath = Writter.DataFilesPaths.CurrentlyPlayedCharacters_Path();
            string currentlyPlayedTempPath = Writter.DataTEMPFilesPaths.CurrentlyPlayedCharacters_Path();

            if (!playerIsSlain || string.IsNullOrEmpty(playerHeirId))
            {
                Program.Logger.Debug("Player not slain or no heir. Copying original player character files to temp.");
                if (File.Exists(playedCharPath)) File.Copy(playedCharPath, playedCharTempPath, true);
                if (File.Exists(currentlyPlayedPath)) File.Copy(currentlyPlayedPath, currentlyPlayedTempPath, true);
                return;
            }

            Program.Logger.Debug($"Player slain, heir is {playerHeirId}. Modifying player character files.");

            // --- Edit PlayedCharacter.txt ---
            if (File.Exists(playedCharPath))
            {
                string content = File.ReadAllText(playedCharPath);

                // NEW: Update the main character ID to the heir, replacing only the first occurrence.
                Regex mainCharRegex = new Regex(@"(^\s*character=)\d+", RegexOptions.Multiline);
                content = mainCharRegex.Replace(content, $"${{1}}{playerHeirId}", 1);
                Program.Logger.Debug($"Updated main character ID to heir ID {playerHeirId}.");

                string newLegacyEntry = $"\t\t{{\n\t\t\tcharacter={playerHeirId}\n\t\t\tdate={Date.Year}.{Date.Month}.{Date.Day}\n\t\t\twars={{ 0 0 0 0 }}\n\t\t}}";
                
                // Corrected Regex: Use a lookahead to find the closing brace of the legacy block,
                // which is followed by another property. This is more robust than assuming it's at the end of the file.
                Regex legacyRegex = new Regex(@"(legacy\s*=\s*{[\s\S]*?)(?=\s*\}\s*\w+\s*=)", RegexOptions.Multiline);
                Match legacyMatch = legacyRegex.Match(content);

                if (legacyMatch.Success)
                {
                    // Check if the last non-whitespace character in the matched group is a closing brace.
                    // If so, we need to add a space. Otherwise, we add a newline and tab.
                    string separator = legacyMatch.Groups[1].Value.TrimEnd().EndsWith("}") ? " " : "\n\t\t";
                    
                    // The lookahead in the regex ensures we don't consume the closing brace.
                    // We replace the matched part (everything up to the brace) with itself plus the new entry.
                    string replacement = $"{legacyMatch.Groups[1].Value}{separator}{newLegacyEntry}";
                    content = legacyRegex.Replace(content, replacement, 1);
                    Program.Logger.Debug("Added new legacy entry to played_character block.");
                }
                else
                {
                    Program.Logger.Debug("Warning: Could not find legacy block in PlayedCharacter.txt to add heir.");
                }
                File.WriteAllText(playedCharTempPath, content);
            }

            // --- Edit CurrentlyPlayedCharacters.txt ---
            if (File.Exists(currentlyPlayedPath))
            {
                string content = File.ReadAllText(currentlyPlayedPath);
                string updatedContent = Regex.Replace(content, @"(currently_played_characters={\s*)\d+", "${1}" + playerHeirId);
                File.WriteAllText(currentlyPlayedTempPath, updatedContent);
                Program.Logger.Debug($"Updated currently_played_characters to heir ID {playerHeirId}.");
            }
        }

        private static double CalculateWarScore(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Calculating dynamic war score with 'Battle Impact' algorithm...");

            var winningArmies = IsAttackerVictorious ? attacker_armies : defender_armies;
            var losingArmies = IsAttackerVictorious ? defender_armies : attacker_armies;

            // --- STAGE 1: Calculate Battle Impact Score (0-100) ---

            // 1.1: Calculate Casualty Rates
            double totalLosingStart = losingArmies.SelectMany(a => a.CasualitiesReports ?? Enumerable.Empty<UnitCasualitiesReport>()).Sum(r => r.GetStarting());
            double totalLosingEnd = losingArmies.SelectMany(a => a.CasualitiesReports ?? Enumerable.Empty<UnitCasualitiesReport>()).Sum(r => r.GetAliveAfterPursuit() != -1 ? r.GetAliveAfterPursuit() : r.GetAliveBeforePursuit());
            double loserCasualtyRate = (totalLosingStart > 0) ? (totalLosingStart - totalLosingEnd) / totalLosingStart : 0;

            double totalWinningStart = winningArmies.SelectMany(a => a.CasualitiesReports ?? Enumerable.Empty<UnitCasualitiesReport>()).Sum(r => r.GetStarting());
            double totalWinningEnd = winningArmies.SelectMany(a => a.CasualitiesReports ?? Enumerable.Empty<UnitCasualitiesReport>()).Sum(r => r.GetAliveAfterPursuit() != -1 ? r.GetAliveAfterPursuit() : r.GetAliveBeforePursuit());
            double winnerCasualtyRate = (totalWinningStart > 0) ? (totalWinningStart - totalWinningEnd) / totalWinningStart : 0;

            // 1.2: Calculate Impact Components
            double casualtyImpact = loserCasualtyRate * 40.0;
            double victoryMarginImpact = Math.Max(0, loserCasualtyRate - winnerCasualtyRate) * 30.0;
            double characterImpact = 0;

            foreach (var army in losingArmies)
            {
                if (army.Commander != null)
                {
                    if (army.Commander.IsMainCommander())
                    {
                        if (army.Commander.IsSlain) characterImpact += 15.0;
                        else if (army.Commander.IsPrisoner) characterImpact += 10.0;
                    }
                    else // Other commanders
                    {
                        if (army.Commander.IsSlain) characterImpact += 1.5;
                        else if (army.Commander.IsPrisoner) characterImpact += 0.75;
                    }
                }

                if (army.Knights != null)
                {
                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        if (knight.IsSlain) characterImpact += 1.5;
                        else if (knight.IsPrisoner) characterImpact += 0.75;
                    }
                }
            }

            double totalImpactScore = Math.Clamp(casualtyImpact + victoryMarginImpact + characterImpact, 0, 100);

            Program.Logger.Debug($"Battle Impact Score: Casualty({casualtyImpact:F2}/40) + Margin({victoryMarginImpact:F2}/30) + Character({characterImpact:F2}) = {totalImpactScore:F2}");

            // --- STAGE 2: Convert Impact Score to Final War Score ---
            double normalizedImpact = totalImpactScore / 100.0;
            double finalWarScore = Math.Pow(normalizedImpact, 1.5) * 75.0;

            // Ensure a minimum score for any victory, but don't give points for a loss.
            if (finalWarScore < 1.0 && totalImpactScore > 0) finalWarScore = 1.0;

            finalWarScore = Math.Clamp(finalWarScore, 0, 75.0); // Clamp to a max of 75

            Program.Logger.Debug($"Final Calculated War Score: {finalWarScore:F4}");

            return Math.Round(finalWarScore, 4);
        }

        private static double CalculatePrestige(double warScore, List<Army> losingArmies)
        {
            // Calculate total size of losing armies from their starting casualties
            double totalLosingArmySize = losingArmies.SelectMany(a => a.CasualitiesReports ?? Enumerable.Empty<UnitCasualitiesReport>())
                                                     .Sum(r => (double)r.GetStarting());

            // Calculate prestige based on war score and size of defeated force
            double prestige = (warScore * 5.0) + (totalLosingArmySize / 200.0);
            
            // Clamp prestige to reasonable values
            prestige = Math.Max(5.0, Math.Min(500.0, prestige));
            
            Program.Logger.Debug($"Calculated prestige award: {prestige:F2} (War Score: {warScore:F2}, Losing Army Size: {totalLosingArmySize:F0})");
            return prestige;
        }

        public static void EditCombatResultsFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Combat Results file...");
            
            // Use pre-calculated war score if available, otherwise calculate it
            double newWarScore;
            if (WarScoreValue.HasValue)
            {
                newWarScore = WarScoreValue.Value;
                Program.Logger.Debug($"Using pre-calculated war score: {newWarScore}");
            }
            else
            {
                // Fallback calculation if somehow WarScoreValue is null
                newWarScore = CalculateWarScore(attacker_armies, defender_armies);
                WarScoreValue = newWarScore;
                Program.Logger.Debug($"Calculated war score (fallback): {newWarScore}");
            }
            
            // Determine winning side value (0 for attacker, 1 for defender)
            string winningSideValue = IsAttackerVictorious ? "0" : "1";
            
            // Determine the winning participant ID
            string winningParticipantId = "";
            var left_side_armies = ArmiesReader.GetSideArmies("left", attacker_armies, defender_armies);
            if (IsAttackerVictorious)
            {
                // Attacker won - check if player was on attacker side
                if (left_side_armies == attacker_armies)
                {
                    winningParticipantId = CK3LogData.LeftSide.GetMainParticipant().id;
                }
                else
                {
                    winningParticipantId = CK3LogData.RightSide.GetMainParticipant().id;
                }
            }
            else
            {
                // Defender won - check if player was on defender side
                if (left_side_armies == defender_armies)
                {
                    winningParticipantId = CK3LogData.LeftSide.GetMainParticipant().id;
                }
                else
                {
                    winningParticipantId = CK3LogData.RightSide.GetMainParticipant().id;
                }
            }
            
            // Determine losing armies for prestige calculation
            var losingArmies = IsAttackerVictorious ? defender_armies : attacker_armies;
            
            // Calculate prestige to award
            double prestigeAward = CalculatePrestige(newWarScore, losingArmies);

            bool inPlayerCombatResultBlock = false; // NEW: State flag to track if we are in the player's block
            bool warScoreUpdated = false; // Track if war_score was updated

            // The original line-by-line logic is stateful and complex to convert to Regex.
            // We will keep it, but ensure it reads from the full, uncorrupted file and writes to the temp file.
            // The bug was the file being overwritten before this method was called. By removing the overwrites,
            // this method will now function correctly. No changes to its internal logic are needed.
            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.CombatResults_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.CombatResults_Path()))
            {
                streamWriter.NewLine = "\n";

                bool isAttacker = false;
                bool isDefender = false;

                bool isMAA = false;
                bool isKnight = false;

                string? regimentType = ""; // Changed to nullable
                string knightID = "";

                string? currentParticipantId = null;
                Army? currentArmy = null;

                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    // 1. Check if we should START processing the player's block
                    if (!inPlayerCombatResultBlock)
                    {
                        Match blockStartMatch = Regex.Match(line, @"^\t\t(\d+)={");
                        if (blockStartMatch.Success && blockStartMatch.Groups[1].Value == BattleResult.ResultID)
                        {
                            inPlayerCombatResultBlock = true;
                            Program.Logger.Debug($"Entering player combat result block ID: {BattleResult.ResultID}");
                        }
                    }

                    // 2. If we are inside the player's block, apply the modification logic
                    if (inPlayerCombatResultBlock)
                    {
                        // 2a. Check if we should STOP processing (end of block)
                        if (line == "\t\t}")
                        {
                            // Inject new summary fields before closing the block
                            if (!warScoreUpdated)
                            {
                                streamWriter.WriteLine($"\t\t\twar_score={newWarScore.ToString("F4", CultureInfo.InvariantCulture)}");
                                Program.Logger.Debug($"Added war_score: {newWarScore:F4}");
                            }
                            
                            streamWriter.WriteLine($"\t\t\twinning_side={winningSideValue}");
                            streamWriter.WriteLine($"\t\t\tend_date={Date.Year}.{Date.Month}.{Date.Day}");
                            
                            // Write the prestige result block
                            streamWriter.WriteLine("\t\t\tresult={");
                            streamWriter.WriteLine($"\t\t\t\tprestige={prestigeAward:F2}");
                            streamWriter.WriteLine("\t\t\t}");
                            
                            streamWriter.WriteLine($"\t\t\twin={winningParticipantId}");
                            streamWriter.WriteLine($"\t\t\tleader={winningParticipantId}");
                            
                            inPlayerCombatResultBlock = false;
                            Program.Logger.Debug($"Exiting player combat result block ID: {BattleResult.ResultID}");
                            
                            // Reset all state variables to prevent them from affecting other parts of the file
                            isAttacker = false;
                            isDefender = false;
                            isMAA = false;
                            isKnight = false;
                            regimentType = "";
                            knightID = "";
                            currentParticipantId = null;
                            currentArmy = null;
                            warScoreUpdated = false;
                        }
                        // 2b. If not the end of the block, run the original modification logic
                        else
                        {
                            if (line.Trim().StartsWith("war_score="))
                            {
                                string edited_line = $"\t\t\twar_score={newWarScore.ToString("F4", CultureInfo.InvariantCulture)}";
                                streamWriter.WriteLine(edited_line);
                                Program.Logger.Debug($"Updated war_score to: {newWarScore:F4}");
                                warScoreUpdated = true;
                                continue;
                            }
                            // Skip writing old summary fields to prevent duplicates
                            else if (line.Trim().StartsWith("end_date=") ||
                                     line.Trim().StartsWith("winning_side=") ||
                                     line.Trim().StartsWith("win=") ||
                                     line.Trim().StartsWith("leader="))
                            {
                                continue; // Skip writing the old line
                            }
                            else if (line.Trim().StartsWith("result={"))
                            {
                                // This block skips a multi-line result block
                                int braceCount = 1;
                                while (braceCount > 0 && (line = streamReader.ReadLine()) != null)
                                {
                                    braceCount += line.Count(c => c == '{');
                                    braceCount -= line.Count(c => c == '}');
                                }
                                continue; // Skip to the line after the result block
                            }
                            else if (line == "\t\t\tattacker={")
                            {
                                isAttacker = true;
                                isDefender = false;
                                currentParticipantId = null; // Reset participant for new block
                                currentArmy = null; // Reset army for new block
                                Program.Logger.Debug("Processing attacker results in CombatResults file.");
                            }
                            else if (line == "\t\t\tdefender={")
                            {
                                isDefender = true;
                                isAttacker = false;
                                currentParticipantId = null; // Reset participant for new block
                                currentArmy = null; // Reset army for new block
                                Program.Logger.Debug("Processing defender results in CombatResults file.");
                            }
                            else if ((isAttacker || isDefender) && (line.Contains("\t\t\tmain_participant=") || line.Contains("\t\t\tcommander=")))
                            {
                                currentParticipantId = Regex.Match(line, @"\d+").Groups[0].Value;
                                List<Army> targetArmies = isAttacker ? attacker_armies : defender_armies;

                                if (line.Contains("main_participant"))
                                {
                                    // The main_participant is the owner of the army/regiments in this block.
                                    currentArmy = targetArmies.FirstOrDefault(a => a.Owner?.GetID() == currentParticipantId);

                                    // If not found, check if it's an owner of a merged army within one of the main armies
                                    if (currentArmy == null)
                                    {
                                        foreach (var mainArmy in targetArmies)
                                        {
                                            if (mainArmy.MergedArmies != null && mainArmy.MergedArmies.Any(ma => ma.Owner?.GetID() == currentParticipantId))
                                            {
                                                currentArmy = mainArmy; // The main army is the one we're interested in for reporting
                                                break;
                                            }
                                        }
                                    }
                                    Program.Logger.Debug($"Detected main_participant: {currentParticipantId}. Current Army found: {currentArmy?.ID ?? "None"}");
                                }
                                else // It's a commander line
                                {
                                    // The commander is the commander of the army/regiments in this block.
                                    currentArmy = targetArmies.FirstOrDefault(a => a.Commander?.ID == currentParticipantId);

                                    // If not found, check if it's a commander of a merged army within one of the main armies
                                    if (currentArmy == null)
                                    {
                                        foreach (var mainArmy in targetArmies)
                                        {
                                            if (mainArmy.MergedArmies != null && mainArmy.MergedArmies.Any(ma => ma.Commander?.GetID() == currentParticipantId))
                                            {
                                                currentArmy = mainArmy; // The main army is the one we're interested in for reporting
                                                break;
                                            }
                                        }
                                    }
                                    Program.Logger.Debug($"Detected commander: {currentParticipantId}. Current Army found: {currentArmy?.ID ?? "None"}");
                                }
                            }
                            else if (isAttacker)
                            {
                                if (line.Contains("\t\t\t\tsurviving_soldiers="))
                                {
                                    int totalFightingMen;
                                    if (twbattle.BattleState.IsSiegeBattle)
                                    {
                                        // For sieges, sum only mobile armies (attackers are the besiegers).
                                        totalFightingMen = attacker_armies.Where(a => !a.IsGarrison()).Sum(a => a.ArmyRegiments.Sum(ar => ar.CurrentNum));
                                    }
                                    else
                                    {
                                        // For field battles, sum all armies.
                                        totalFightingMen = attacker_armies.Sum(a => a.ArmyRegiments.Sum(ar => ar.CurrentNum));
                                    }
                                    string edited_line = "\t\t\t\tsurviving_soldiers=" + totalFightingMen;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Attacker side: surviving_soldiers={totalFightingMen}");
                                    continue;
                                }
                                else if (line.Contains("\t\t\t\t\t\ttype="))
                                {
                                    isMAA = true;
                                    regimentType = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                                    Program.Logger.Debug($"Attacker: Detected Men-at-Arms regiment type: {regimentType}");
                                }
                                else if (line.Contains("\t\t\t\t\t\tknight="))
                                {
                                    string id = Regex.Match(line, @"\d+").Value;
                                    if (id == "4294967295" && !isMAA)
                                    {
                                        regimentType = "Levy";
                                        Program.Logger.Debug($"Attacker: Detected Levy regiment (ID: {id}).");
                                    }
                                    else if (id == "4294967295" && isMAA)
                                    {
                                        isMAA = true;
                                        string logMessage = string.Format("Attacker: Detected Men-at-Arms regiment (ID: {0}).", id);
                                        Program.Logger.Debug(logMessage);
                                    }
                                    else
                                    {
                                        isKnight = true;
                                        knightID = id;
                                        Program.Logger.Debug($"Attacker: Detected Knight (ID: {knightID}).");
                                    }
                                }
                                else if (!isKnight && line.Contains("\t\t\t\t\t\tmain_kills="))
                                {
                                    int main_kills = 0;
                                    foreach (Army army in attacker_armies)
                                    {
                                        if (army == null) continue;
                                        var results = army.UnitsResults;
                                        if (results != null)
                                        {
                                            main_kills += results.GetKillsAmountOfMainPhase(regimentType);
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tmain_kills=" + main_kills;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Attacker: {regimentType} main_kills={main_kills}");
                                    continue;
                                }
                                else if (isKnight && line.Contains("\t\t\t\t\t\tmain_kills="))
                                {
                                    int main_kills = 0;
                                    foreach (Army army in attacker_armies)
                                    {
                                        if (army == null) continue;
                                        var knightsList = army.Knights?.GetKnightsList();
                                        if (knightsList != null)
                                        {
                                            var knight = knightsList.FirstOrDefault(k => k != null && k.GetID() == knightID);
                                            if (knight != null)
                                            {
                                                main_kills = knight.GetKills();
                                                break;
                                            }
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tmain_kills=" + main_kills;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Attacker: Knight {knightID} main_kills={main_kills}");
                                    continue;
                                }
                                else if (!isKnight && line.Contains("\t\t\t\t\t\tpursuit_kills="))
                                {
                                    int pursuit_kills = 0;
                                    foreach (Army army in attacker_armies)
                                    {
                                        if (army == null) continue;
                                        var results = army.UnitsResults;
                                        if (results != null)
                                        {
                                            pursuit_kills += results.GetKillsAmountOfPursuitPhase(regimentType);
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tpursuit_kills=" + pursuit_kills;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Attacker: {regimentType} pursuit_kills={pursuit_kills}");
                                    continue;
                                }
                                else if (!isKnight && line.Contains("\t\t\t\t\t\tmain_losses="))
                                {
                                    int main_losses = 0;
                                    foreach (Army army in attacker_armies)
                                    {
                                        if (army == null) continue;
                                        var results = army.UnitsResults;
                                        if (results != null)
                                        {
                                            main_losses += results.GetDeathAmountOfMainPhase(army.CasualitiesReports, regimentType);
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tmain_losses=" + main_losses;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Attacker: {regimentType} main_losses={main_losses}");
                                    continue;
                                }
                                else if (!isKnight && line.Contains("\t\t\t\t\t\tpursuit_losses_maa="))
                                {
                                    int pursuit_losses = 0;
                                    foreach (Army army in attacker_armies)
                                    {
                                        if (army == null) continue;
                                        var results = army.UnitsResults;
                                        if (results != null)
                                        {
                                            pursuit_losses += results.GetDeathAmountOfPursuitPhase(army.CasualitiesReports, regimentType);
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tpursuit_losses_maa=" + pursuit_losses;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Attacker: {regimentType} pursuit_losses_maa={pursuit_losses}");
                                    continue;
                                }
                                else if (line == "\t\t\t\t\t}")
                                {
                                    isKnight = false;
                                    isMAA = false;
                                    knightID = "";
                                    regimentType = "";
                                    Program.Logger.Debug("Attacker: End of regiment block.");
                                }
                            }
                            else if (isDefender)
                            {
                                if (line.Contains("\t\t\t\tsurviving_soldiers="))
                                {
                                    int totalFightingMen;
                                    if (twbattle.BattleState.IsSiegeBattle)
                                    {
                                        // For sieges, sum only mobile armies (defenders can be garrison + relief).
                                        totalFightingMen = defender_armies.Where(a => !a.IsGarrison()).Sum(a => a.ArmyRegiments.Sum(ar => ar.CurrentNum));
                                    }
                                    else
                                    {
                                        // For field battles, sum all armies.
                                        totalFightingMen = defender_armies.Sum(a => a.ArmyRegiments.Sum(ar => ar.CurrentNum));
                                    }
                                    string edited_line = "\t\t\t\tsurviving_soldiers=" + totalFightingMen;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Defender side: surviving_soldiers={totalFightingMen}");
                                    continue;
                                }
                                else if (line.Contains("\t\t\t\t\t\ttype="))
                                {
                                    isMAA = true;
                                    regimentType = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                                    Program.Logger.Debug($"Defender: Detected regiment type: {regimentType}");
                                }
                                else if (line.Contains("\t\t\t\t\t\tknight="))
                                {
                                    string id = Regex.Match(line, @"\d+").Value;
                                    if (id == "4294967295" && !isMAA)
                                    {
                                        regimentType = "Levy";
                                        Program.Logger.Debug($"Defender: Detected Levy regiment (ID: {id}).");
                                    }
                                    else
                                    {
                                        isKnight = true;
                                        knightID = id;
                                        Program.Logger.Debug($"Defender: Detected Knight (ID: {knightID}).");
                                    }
                                }
                                else if (!isKnight && line.Contains("\t\t\t\t\t\tmain_kills="))
                                {
                                    int main_kills = 0;
                                    foreach (Army army in defender_armies)
                                    {
                                        if (army == null) continue;
                                        var results = army.UnitsResults;
                                        if (results != null)
                                        {
                                            main_kills += results.GetKillsAmountOfMainPhase(regimentType);
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tmain_kills=" + main_kills;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Defender: {regimentType} main_kills={main_kills}");
                                    continue;
                                }
                                else if (isKnight && line.Contains("\t\t\t\t\t\tmain_kills="))
                                {
                                    int main_kills = 0;
                                    foreach (Army army in defender_armies)
                                    {
                                        if (army == null) continue;
                                        var knightsList = army.Knights?.GetKnightsList();
                                        if (knightsList != null)
                                        {
                                            var knight = knightsList.FirstOrDefault(k => k != null && k.GetID() == knightID);
                                            if (knight != null)
                                            {
                                                main_kills = knight.GetKills();
                                                break;
                                            }
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tmain_kills=" + main_kills;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Defender: Knight {knightID} main_kills={main_kills}");
                                    continue;
                                }
                                else if (!isKnight && line.Contains("\t\t\t\t\t\tpursuit_kills="))
                                {
                                    int pursuit_kills = 0;
                                    foreach (Army army in defender_armies)
                                    {
                                        if (army == null) continue;
                                        var results = army.UnitsResults;
                                        if (results != null)
                                        {
                                            pursuit_kills += results.GetKillsAmountOfPursuitPhase(regimentType);
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tpursuit_kills=" + pursuit_kills;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Defender: {regimentType} pursuit_kills={pursuit_kills}");
                                    continue;
                                }
                                else if (!isKnight && line.Contains("\t\t\t\t\t\tmain_losses="))
                                {
                                    int main_losses = 0;
                                    foreach (Army army in defender_armies)
                                    {
                                        if (army == null) continue;
                                        var results = army.UnitsResults;
                                        if (results != null)
                                        {
                                            main_losses += results.GetDeathAmountOfMainPhase(army.CasualitiesReports, regimentType);
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tmain_losses=" + main_losses;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Defender: {regimentType} main_losses={main_losses}");
                                    continue;
                                }
                                else if (!isKnight && line.Contains("\t\t\t\t\t\tpursuit_losses_maa="))
                                {
                                    int pursuit_losses = 0;
                                    foreach (Army army in defender_armies)
                                    {
                                        if (army == null) continue;
                                        var results = army.UnitsResults;
                                        if (results != null)
                                        {
                                            pursuit_losses += results.GetDeathAmountOfPursuitPhase(army.CasualitiesReports, regimentType);
                                        }
                                    }
                                    string edited_line = "\t\t\t\t\t\tpursuit_losses_maa=" + pursuit_losses;
                                    streamWriter.WriteLine(edited_line);
                                    Program.Logger.Debug($"Defender: {regimentType} pursuit_losses_maa={pursuit_losses}");
                                    continue;
                                }
                                else if (line == "\t\t\t\t\t}")
                                {
                                    isKnight = false;
                                    isMAA = false;
                                    knightID = "";
                                    regimentType = "";
                                    Program.Logger.Debug("Defender: End of regiment block.");
                                }
                            }
                            else if (line == "\t\t\t}") // End of an attacker or defender block
                            {
                                isAttacker = false;
                                isDefender = false;
                                currentParticipantId = null;
                                currentArmy = null;
                                Program.Logger.Debug("Resetting participant state at end of alliance block.");
                                streamWriter.WriteLine(line); // Write the closing brace
                                continue; // Continue to next line, preventing default write
                            }
                            streamWriter.WriteLine(line);
                            continue; // Ensure the line is not written again by the outer block
                        }
                    }

                    // 3. Write the line (either original or from a non-player block)
                    streamWriter.WriteLine(line);
                }
            }
            Program.Logger.Debug("Finished editing Combat Results file.");
        }


        public static void EditCombatFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Combat file using block replacement...");

            if (string.IsNullOrEmpty(Player_Combat) || string.IsNullOrEmpty(Original_Player_Combat))
            {
                Program.Logger.Debug("No player combat block found. Copying original Combats.txt to temp.");
                File.Copy(Writter.DataFilesPaths.Combats_Path(), Writter.DataTEMPFilesPaths.Combats_Path(), true);
                return;
            }

            // 1. Set winner and phase changes on the main Player_Combat string
            string winner = IsAttackerVictorious ? "attacker" : "defender";
            SetWinner(winner); // This modifies Player_Combat in memory

            // 2. Isolate Attacker and Defender blocks to prevent regex cross-contamination
            Match attackerMatch = Regex.Match(Player_Combat!, @"(attacker={[\s\S]*?^\t\t\t})", RegexOptions.Multiline);
            Match defenderMatch = Regex.Match(Player_Combat!, @"(defender={[\s\S]*?^\t\t\t})", RegexOptions.Multiline);

            if (!attackerMatch.Success || !defenderMatch.Success)
            {
                Program.Logger.Debug("CRITICAL: Could not isolate attacker or defender block from Player_Combat. Aborting modification.");
                // Fallback: write the partially modified (winner only) combat block to temp
                string fullContent = File.ReadAllText(Writter.DataFilesPaths.Combats_Path());
                string updatedContent = fullContent.Replace(Original_Player_Combat, Player_Combat);
                File.WriteAllText(Writter.DataTEMPFilesPaths.Combats_Path(), updatedContent);
                return;
            }

            string originalAttackerBlock = attackerMatch.Value;
            string originalDefenderBlock = defenderMatch.Value;
            string modifiedAttackerBlock = originalAttackerBlock;
            string modifiedDefenderBlock = originalDefenderBlock;

            // 3. Apply casualty updates to the ISOLATED blocks
            // --- Attacker modifications ---
            int attackerTotalFightingMen = GetArmiesTotalFightingMen(attacker_armies);
            modifiedAttackerBlock = Regex.Replace(modifiedAttackerBlock, @"(total_fighting_men=)[\d\.]+", $"${{1}}{attackerTotalFightingMen}");
            int attackerTotalLevyMen = GetArmiesTotalLevyMen(attacker_armies);
            modifiedAttackerBlock = Regex.Replace(modifiedAttackerBlock, @"(total_levy_men=)[\d\.]+", $"${{1}}{attackerTotalLevyMen}");

            foreach (var army in attacker_armies)
            {
                if (army.ArmyRegiments == null) continue;
                foreach (var armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment == null || armyRegiment.Type == RegimentType.Knight) continue;
                    string currentNum = armyRegiment.CurrentNum.ToString();
                    // BUG FIX: Changed `\d+` to `[\d\.]+` to handle decimals
                    modifiedAttackerBlock = Regex.Replace(modifiedAttackerBlock, $@"(regiment={armyRegiment.ID}(?:(?!regiment=)[\s\S])*?current=)[\d\.]+", $"${{1}}{currentNum}");
                }
            }

            // --- Defender modifications ---
            int defenderTotalFightingMen = GetArmiesTotalFightingMen(defender_armies);
            modifiedDefenderBlock = Regex.Replace(modifiedDefenderBlock, @"(total_fighting_men=)[\d\.]+", $"${{1}}{defenderTotalFightingMen}");
            int defenderTotalLevyMen = GetArmiesTotalLevyMen(defender_armies);
            modifiedDefenderBlock = Regex.Replace(modifiedDefenderBlock, @"(total_levy_men=)[\d\.]+", $"${{1}}{defenderTotalLevyMen}");

            foreach (var army in defender_armies)
            {
                if (army.ArmyRegiments == null) continue;
                foreach (var armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment == null || armyRegiment.Type == RegimentType.Knight) continue;
                    string currentNum = armyRegiment.CurrentNum.ToString();
                    // BUG FIX: Changed `\d+` to `[\d\.]+` to handle decimals
                    modifiedDefenderBlock = Regex.Replace(modifiedDefenderBlock, $@"(regiment={armyRegiment.ID}(?:(?!regiment=)[\s\S])*?current=)[\d\.]+", $"${{1}}{currentNum}");
                }
            }

            // 4. Replace the original blocks in Player_Combat with the modified ones
            Player_Combat = Player_Combat!.Replace(originalAttackerBlock, modifiedAttackerBlock);
            Player_Combat = Player_Combat.Replace(originalDefenderBlock, modifiedDefenderBlock);

            // 5. Perform final block replacement into the full Combats.txt content
            string fullFileContent = File.ReadAllText(Writter.DataFilesPaths.Combats_Path());
            string updatedFileContent = fullFileContent.Replace(Original_Player_Combat!, Player_Combat);
            File.WriteAllText(Writter.DataTEMPFilesPaths.Combats_Path(), updatedFileContent);
            Program.Logger.Debug("Finished editing Combat file.");
        }

        static int GetArmiesTotalFightingMen(List<Army> armies)
        {
            int total = armies.Where(army => army != null)
                .Sum(army => army.GetTotalSoldiers()); // Use the updated GetTotalSoldiers

            string logMessage = string.Format("Calculated total fighting men for armies: {0}", total);
            Program.Logger.Debug(logMessage);
            return total;
        }

        static int GetArmiesTotalLevyMen(List<Army> armies)
        {
            int total = 0;
            foreach (Army army in armies)
            {
                if (army.IsGarrison())
                {
                    // For garrisons, levies are part of the Units list
                    total += army.Units.Where(u => u != null && u.GetRegimentType() == RegimentType.Levy).Sum(u => u.GetSoldiers());
                }
                else
                {
                    // For field armies, levies are part of ArmyRegiments
                    if (army.ArmyRegiments == null) continue;
                    total += army.ArmyRegiments.Where(y => y != null && y.Type == RegimentType.Levy).Sum(x => x.CurrentNum);
                }
            }

            Program.Logger.Debug($"Calculated total levy men for armies: {total}");
            return total;
        }

        static ArmyRegiment? SearchArmyRegiment(List<Army> armies, string army_regiment_id)
        {
            Program.Logger.Debug($"Searching for ArmyRegiment ID: {army_regiment_id}");
            foreach (Army army in armies)
            {
                if (army.ArmyRegiments != null)
                {
                    foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                    {
                        if (armyRegiment == null) continue;

                        if (armyRegiment.Type == RegimentType.Knight) continue;
                        if (armyRegiment.ID == army_regiment_id)
                        {
                            Program.Logger.Debug($"Found ArmyRegiment {army.ID}.{armyRegiment.ID}.");
                            return armyRegiment;
                        }
                    }
                }
            }

            Program.Logger.Debug($"ArmyRegiment ID: {army_regiment_id} not found.");
            return null;
        }

        public static string GetAttilaWinner(string path_attila_log, string player_armies_combat_side,
            string enemy_armies_combat_side)
        {
            Program.Logger.Debug($"Entering GetAttilaWinner for log file: {path_attila_log}");
            string winner = enemy_armies_combat_side; // Default to a loss

            try
            {
                if (!File.Exists(path_attila_log))
                {
                    Program.Logger.Debug($"Attila log file not found at: {path_attila_log}. Returning default winner.");
                    return winner;
                }

                string logContent;
                using (FileStream logFile =
                       File.Open(path_attila_log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(logFile))
                {
                    logContent = reader.ReadToEnd();
                }

                // Normalize line endings for consistent searching
                logContent = logContent.Replace("\r\n", "\n");

                // Define the multi-line header for a battle report
                const string battleReportHeader =
                    "--------------------------------------------------------\n" +
                    "--------------------------------------------------------\n" +
                    "--\n" +
                    "--\t                    CRUSADER CONFLICTS               \n" +
                    "--\n" +
                    "--------------------------------------------------------\n" +
                    "--------------------------------------------------------";

                string normalizedHeader = battleReportHeader.Replace("\r\n", "\n");

                string relevantLogSection;
                int lastHeaderIndex = logContent.LastIndexOf(normalizedHeader);

                if (lastHeaderIndex != -1)
                {
                    relevantLogSection = logContent.Substring(lastHeaderIndex);
                    Program.Logger.Debug("Isolated final battle report section from Attila log.");
                }
                else
                {
                    relevantLogSection = logContent; // Fallback to entire log if header not found
                    Program.Logger.Debug("Battle report header not found. Analyzing entire Attila log content.");
                }

                // Search for "Victory" only within the isolated log section
                if (relevantLogSection.Contains("Victory"))
                {
                    winner = player_armies_combat_side;
                    Program.Logger.Debug($"'Victory' keyword found in relevant log section. Winner set to: {winner}");
                }
                else
                {
                    Program.Logger.Debug(
                        $"'Victory' keyword not found in relevant log section. Winner remains default: {winner}");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug(
                    $"Error determining Attila winner: {ex.Message}. Returning default winner ({winner}).");
                // The 'winner' variable is already initialized to enemy_armies_combat_side, so no need to re-assign.
            }

            Program.Logger.Debug($"Determined Attila winner: {winner}");
            return winner;
        }

        static void SetWinner(string winner)
        {
            Program.Logger.Debug($"Setting battle winner to: {winner}");
            try
            {
                // IsAttackerVictorious is now set authoritatively in BattleProcessor before any file edits.
                // This method now only modifies the Player_Combat string.

                //Set pursuit phase
                Player_Combat = Regex.Replace(Player_Combat ?? string.Empty, @"(phase=)\w+", "$1" + "pursuit");

                //Set last day of phase
                Player_Combat = Regex.Replace(Player_Combat ?? string.Empty, @"(days=\d+)", "days=3\n\t\t\twiped=no");

                //Set winner
                Player_Combat = Regex.Replace(Player_Combat ?? string.Empty, @"(base_combat_width=\d+)",
                    "$1\n\t\t\twinning_side=" + winner);

                Player_Combat = Player_Combat?.Replace("\r", "");

                Program.Logger.Debug("Winner of battle set successfully");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error setting winner of battle: {ex.Message}");
            }

        }

        static int ConvertMenToMachines(int men)
        {
            if (men < 3) return 0;
            return (int)Math.Round(men / 4.0, MidpointRounding.AwayFromZero);
        }

        public static void EditArmyRegimentsFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Army Regiments file...");
            bool editStarted = false;
            ArmyRegiment? editArmyRegiment = null;

            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.ArmyRegiments_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.ArmyRegiments_Path()))
            {
                streamWriter.NewLine = "\n";

                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {

                    //Regiment ID line
                    if (!editStarted && line != null && Regex.IsMatch(line, @"\t\t\d+={"))
                    {
                        string army_regiment_id = Regex.Match(line, @"\d+").Value;


                        var searchingData = SearchArmyRegimentsFile(attacker_armies, army_regiment_id);
                        if (searchingData.editStarted)
                        {
                            editStarted = true;
                            editArmyRegiment = searchingData.editArmyRegiment;
                            Program.Logger.Debug($"Found ArmyRegiment {army_regiment_id} for editing (Attacker).");
                        }
                        else
                        {
                            searchingData = SearchArmyRegimentsFile(defender_armies, army_regiment_id);
                            if (searchingData.editStarted)
                            {
                                editStarted = true;
                                editArmyRegiment = searchingData.editArmyRegiment;
                                Program.Logger.Debug($"Found ArmyRegiment {army_regiment_id} for editing (Defender).");
                            }
                        }

                    }

                    else if (editStarted == true && line.Contains("\t\t\t\tcurrent=") && editArmyRegiment != null)
                    {
                        string edited_line = "\t\t\t\tcurrent=" + editArmyRegiment.CurrentNum;
                        streamWriter.WriteLine(edited_line);
                        Program.Logger.Debug(
                            $"Updated ArmyRegiment {editArmyRegiment.ID} current soldiers to {editArmyRegiment.CurrentNum}.");
                        continue;
                    }

                    //End Line
                    else if (editStarted && line == "\t\t}")
                    {
                        editStarted = false;
                        editArmyRegiment = null;
                    }

                    streamWriter.WriteLine(line);
                }
            }

            Program.Logger.Debug("Finished editing Army Regiments file.");
        }

        static (bool editStarted, ArmyRegiment? editArmyRegiment) SearchArmyRegimentsFile(List<Army> armies,
            string army_regiment_id)
        {
            // Program.Logger.Debug($"Searching for ArmyRegiment ID: {army_regiment_id} in ArmyRegiments file.");
            bool editStarted = false;
            ArmyRegiment? editRegiment = null;

            foreach (Army army in armies)
            {
                if (army == null) continue;
                if (army.ArmyRegiments != null)
                {
                    foreach (ArmyRegiment army_regiment in army.ArmyRegiments)
                    {
                        if (army_regiment == null) continue;

                        if (army_regiment.Type == RegimentType.Knight) continue;
                        if (army_regiment.ID == army_regiment_id)
                        {
                            editStarted = true;
                            editRegiment = army_regiment;
                            Program.Logger.Debug($"Found ArmyRegiment {army_regiment_id}.");
                            return (editStarted, editRegiment);
                        }
                    }
                }
            }

            // Program.Logger.Debug($"ArmyRegiment ID: {army_regiment_id} not found in ArmyRegiments file.");
            return (false, null);
        }


        static string GetChunksText(string size, string owner, string current)
        {
            string str;
            if (string.IsNullOrEmpty(owner))
            {
                str = $"\t\t\tmax={size}\n" +
                      $"\t\t\tchunks={{\n" +
                      $"\t\t\t\t{{\n" +
                      $"\t\t\t\t\tmax={size}\n" +
                      $"\t\t\t\t\tcurrent={current}\n" +
                      $"\t\t\t\t}}\n" +
                      $"\t\t\t}}\n";
            }
            else
            {
                str = $"\t\t\tmax={size}\n" +
                      $"\t\t\towner={owner}\n" +
                      $"\t\t\tchunks={{\n" +
                      $"\t\t\t\t{{\n" +
                      $"\t\t\t\t\tmax={size}\n" +
                      $"\t\t\t\t\tcurrent={current}\n" +
                      $"\t\t\t\t}}\n" +
                      $"\t\t\t}}\n";
            }


            return str;
        }

        public static void EditRegimentsFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Regiments file...");
            bool editStarted = false;
            bool editIndex = false;
            Regiment? editRegiment = null;
            ArmyRegiment? parentArmyRegiment = null; // Declare new variable

            int index = -1;
            bool isNewData = false;


            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.Regiments_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.Regiments_Path()))
            {
                streamWriter.NewLine = "\n";

                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {

                    //Regiment ID line
                    if (!editStarted && line != null && Regex.IsMatch(line, @"\t\t\d+={"))
                    {
                        string regiment_id = Regex.Match(line, @"\d+").Value;


                        var searchingData = SearchRegimentsFile(attacker_armies, regiment_id);
                        if (searchingData.editStarted)
                        {
                            editStarted = true;
                            editRegiment = searchingData.foundRegiment;
                            parentArmyRegiment = searchingData.parentArmyRegiment; // Store parent ArmyRegiment
                            Program.Logger.Debug($"Found Regiment {regiment_id} for editing (Attacker).");
                        }
                        else
                        {
                            searchingData = SearchRegimentsFile(defender_armies, regiment_id);
                            if (searchingData.editStarted)
                            {
                                editStarted = true;
                                editRegiment = searchingData.foundRegiment;
                                parentArmyRegiment = searchingData.parentArmyRegiment; // Store parent ArmyRegiment
                                Program.Logger.Debug($"Found Regiment {regiment_id} for editing (Defender).");
                            }
                        }

                    }

                    else if (editStarted && line.Contains("\t\t\tsize="))
                    {
                        if (parentArmyRegiment != null && parentArmyRegiment.Type == RegimentType.MenAtArms)
                        {
                            if (editRegiment != null)
                            {
                                // For Men-at-Arms, only update the 'size' line and keep other properties.
                                string currentNum = editRegiment.CurrentNum ?? "0";
                                string edited_line = "\t\t\tsize=" + currentNum;
                                streamWriter.WriteLine(edited_line);
                                string regId = editRegiment.ID ?? "N/A"; // Extract ID for logging
                                string logMessage = string.Format("Regiment {0}: Updating Men-at-Arms size to {1}.",
                                    regId, currentNum);
                                Program.Logger.Debug(logMessage);
                            }

                            continue; // Continue to next line without setting isNewData
                        }
                        else if (editRegiment != null)
                        {
                            var reg = editRegiment; // New local variable
                            // For other types (Levy, Garrison), use the existing logic to rewrite the block.
                            isNewData = true;
                            string max = reg.Max ?? "0";
                            string owner = reg.Owner ?? "";
                            string current = reg.CurrentNum ?? "0";
                            string newLine = GetChunksText(max, owner, current);
                            streamWriter.WriteLine(newLine);
                            string regId = reg.ID ?? "N/A"; // Extract ID for logging
                            Program.Logger.Debug($"Regiment {regId}: Writing new data format with current soldiers {reg.CurrentNum ?? "0"}.");
                            continue;
                        }
                    }

                    //Index Counter
                    else if(!isNewData && editStarted && line == "\t\t\t\t{")
                    {
                        index++;
                        if (editRegiment != null && editRegiment.Index == "") 
                            editRegiment.ChangeIndex(0.ToString());
                        if(editRegiment != null && index.ToString() == editRegiment.Index)
                        {
                            editIndex = true;
                        }
                    }

                    else if(!isNewData && (editStarted==true && editIndex==true) && line.Contains("\t\t\t\t\tcurrent="))
                    {
                        if (editRegiment != null) // Added null check
                        {
                            string currentNum = editRegiment.CurrentNum ?? "0";
                            string edited_line = "\t\t\t\t\tcurrent=" + currentNum;
                            streamWriter.WriteLine(edited_line);
                            string regId = editRegiment.ID ?? "N/A"; // Extract ID for logging
                            string logMessage = string.Format("Regiment {0}: Updating old data format with current soldiers {1}.", regId, currentNum);
                            Program.Logger.Debug(logMessage);
                            continue;
                        }
                    }

                    //End Line
                    else if(editStarted && line == "\t\t}")
                    {
                        editStarted = false; editRegiment = null; editIndex = false; index = -1; isNewData = false;
                        parentArmyRegiment = null; // Reset parent ArmyRegiment
                    }

                    if(!isNewData)
                    {
                        streamWriter.WriteLine(line);
                    }
                    
                }
            }
            Program.Logger.Debug("Finished editing Regiments file.");
        }

        static (bool editStarted, Regiment? foundRegiment, ArmyRegiment? parentArmyRegiment) SearchRegimentsFile(List<Army> armies, string regiment_id)
        {
            // Program.Logger.Debug($"Searching for Regiment ID: {regiment_id} in Regiments file.");
            bool editStarted = false;
            Regiment? foundRegiment = null;
            ArmyRegiment? parentArmyRegiment = null;

            foreach (Army army in armies)
            {
                if (army == null) continue;
                if (army.ArmyRegiments == null) continue;
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment == null) continue;
                    if (armyRegiment.Regiments == null) continue;
                    foreach (Regiment regiment in armyRegiment.Regiments)
                    {
                        if (regiment == null) continue; // Added null check
                        if (regiment.ID == regiment_id)
                        {
                            editStarted = true;
                            foundRegiment = regiment;
                            parentArmyRegiment = armyRegiment;
                            Program.Logger.Debug($"Found Regiment {regiment_id} with parent ArmyRegiment {armyRegiment.ID}.");
                            return (editStarted, foundRegiment, parentArmyRegiment);
                        }
                    }
                }
            }
            // Program.Logger.Debug($"Regiment ID: {regiment_id} not found in Regiments file.");
            return (false, null, null);
        }

        public static void EditSiegesFile(string path_log_attila, string attacker_side, string defender_side,
            List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Entering EditSiegesFile method.");

            if (!BattleState.IsSiegeBattle)
            {
                Program.Logger.Debug("Not a siege battle. Skipping EditSiegesFile.");
                return;
            }

            Program.Logger.Debug($"Using pre-determined battle winner. IsAttackerVictorious: {IsAttackerVictorious}");


            if (string.IsNullOrEmpty(SiegeID))
            {
                Program.Logger.Debug("SiegeID is null or empty. Cannot edit Sieges.txt.");
                return;
            }

            string siegesFilePath = Writter.DataFilesPaths.Sieges_Path();
            if (!File.Exists(siegesFilePath))
            {
                Program.Logger.Debug($"Sieges file not found at {siegesFilePath}. Cannot edit siege progress.");
                return;
            }

            // 1. Calculate Breach Increment and check for settlement capture from the LATEST battle log
            int breachIncrement = 0;
            bool settlementCaptured = false;
            if (File.Exists(path_log_attila))
            {
                string attilaLogContent = File.ReadAllText(path_log_attila);
                
                // Normalize line endings for consistent searching
                attilaLogContent = attilaLogContent.Replace("\r\n", "\n");

                // Isolate the log for the last battle to avoid reading old data
                string relevantLogSection;
                const string battleReportHeader =
                    "--------------------------------------------------------\n" +
                    "--------------------------------------------------------\n" +
                    "--\n" +
                    "--\t                    CRUSADER CONFLICTS               \n" +
                    "--\n" +
                    "--------------------------------------------------------\n" +
                    "--------------------------------------------------------";
                
                int lastHeaderIndex = attilaLogContent.LastIndexOf(battleReportHeader);

                if (lastHeaderIndex != -1)
                {
                    relevantLogSection = attilaLogContent.Substring(lastHeaderIndex);
                    Program.Logger.Debug("Isolated final battle report section from Attila log for siege analysis.");
                }
                else
                {
                    relevantLogSection = attilaLogContent; // Fallback to entire log if header not found
                    Program.Logger.Debug("Battle report header not found. Analyzing entire Attila log content for siege analysis.");
                }

                settlementCaptured = relevantLogSection.Contains("SETTLEMENT_CAPTURED");
                if (settlementCaptured)
                {
                    Program.Logger.Debug("Attila log: SETTLEMENT_CAPTURED event found in last battle report.");
                }

                int wallsAttackedCount = Regex.Matches(relevantLogSection, "WALLS_ATTACKED").Count;
                int wallsDestroyedCount = Regex.Matches(relevantLogSection, "WALLS_DESTROYED").Count;

                if (wallsDestroyedCount > 1)
                {
                    breachIncrement = 3;
                    Program.Logger.Debug(
                        $"Attila log: Multiple WALLS_DESTROYED events ({wallsDestroyedCount}). Setting breachIncrement to 3.");
                }
                else if (wallsDestroyedCount == 1)
                {
                    breachIncrement = 2;
                    Program.Logger.Debug($"Attila log: One WALLS_DESTROYED event. Setting breachIncrement to 2.");
                }
                else if (wallsAttackedCount > 0)
                {
                    breachIncrement = 1;
                    Program.Logger.Debug(
                        $"Attila log: WALLS_ATTACKED event(s) found ({wallsAttackedCount}). Setting breachIncrement to 1.");
                }
                else
                {
                    Program.Logger.Debug("Attila log: No wall attack/destruction events found. breachIncrement remains 0.");
                }
            }
            else
            {
                Program.Logger.Debug($"Attila log file not found at {path_log_attila}. Cannot calculate breach increment or check for capture.");
            }


            List<string> fileLines = File.ReadAllLines(siegesFilePath).ToList();
            StringBuilder updatedContent = new StringBuilder();
            bool inTargetSiegeBlock = false;
            bool targetSiegeBlockFound = false; // Track if we entered the target siege block at all
            bool breachLineHandled = false; // NEW: Track if breach= line was found/handled

            for (int i = 0; i < fileLines.Count; i++)
            {
                string line = fileLines[i];
                string trimmedLine = line.Trim();

                if (trimmedLine == $"{SiegeID}={{")
                {
                    inTargetSiegeBlock = true;
                    targetSiegeBlockFound = true;
                    breachLineHandled = false; // NEW: Reset for each siege block
                    updatedContent.AppendLine(line);
                }
                else if (inTargetSiegeBlock && trimmedLine == "}")
                {
                    // NEW: Add missing breach line before closing brace if applicable
                    if (!breachLineHandled && breachIncrement > 0)
                    {
                        int newBreach = Math.Min(3, breachIncrement);
                        updatedContent.AppendLine($"\t\t\tbreach={newBreach}");
                        Program.Logger.Debug($"Added missing breach line with value: {newBreach}");
                    }

                    inTargetSiegeBlock = false;
                    updatedContent.AppendLine(line);
                }
                else if (inTargetSiegeBlock && trimmedLine.StartsWith("progress="))
                {
                    if (settlementCaptured)
                    {
                        // Settlement was captured, siege is won by besieger.
                        int fortLevel = twbattle.Sieges.GetFortLevel();
                        double newProgress = 100 + (fortLevel * 75);
                        Program.Logger.Debug($"Settlement captured. Updating siege progress for SiegeID {SiegeID} to {newProgress} (based on fort level {fortLevel}).");
                        updatedContent.AppendLine($"{line.Substring(0, line.IndexOf("progress="))}progress={newProgress.ToString("F2", CultureInfo.InvariantCulture)}");
                    }
                    else
                    {
                        // Settlement not captured, use existing logic based on routing or casualties.
                        bool isPlayerBesieged = attacker_armies.Concat(defender_armies).Any(a => a.IsGarrison() && a.IsPlayer());
                        bool siegeWonByBesieger = !isPlayerBesieged && IsAttackerVictorious;

                        if (siegeWonByBesieger)
                        {
                            // Attacker won a standard assault by routing defender: set progress to 100%
                            int fortLevel = twbattle.Sieges.GetFortLevel();
                            double newProgress = 100 + (fortLevel * 75);
                            Program.Logger.Debug(
                                $"Attacker won standard assault by routing defender. Updating siege progress for SiegeID {SiegeID} to {newProgress} (based on fort level {fortLevel}).");
                            updatedContent.AppendLine(
                                $"{line.Substring(0, line.IndexOf("progress="))}progress={newProgress.ToString("F2", CultureInfo.InvariantCulture)}");
                        }
                        else
                        {
                            // All other outcomes: calculate progress based on garrison casualties.
                            // This includes:
                            // - Besieger loses a standard assault.
                            // - Besieger wins a sally-out (garrison loses).
                            // - Besieger loses a sally-out (garrison wins).

                            int initialGarrisonSize = twbattle.Sieges.GetGarrisonSize();
                            var garrisonArmies = attacker_armies.Where(a => a.IsGarrison()).Concat(defender_armies.Where(a => a.IsGarrison()));
                            int finalGarrisonSize = garrisonArmies.Sum(a => a.GetTotalSoldiers());

                            double casualtyPercentage = 0;
                            if (initialGarrisonSize > 0)
                            {
                                int casualties = initialGarrisonSize - finalGarrisonSize;
                                if (casualties > 0)
                                {
                                    casualtyPercentage = (double)casualties / initialGarrisonSize;
                                }
                            }

                            string outcomeLog = "";
                            if (isPlayerBesieged) {
                                // In a sally-out, the garrison is the attacker. We know the player is the garrison.
                                outcomeLog = IsAttackerVictorious ? "Player (garrison) won sally-out." : "Besieger won against player's sally-out.";
                            } else {
                                outcomeLog = "Besieger lost assault.";
                            }
                            Program.Logger.Debug($"{outcomeLog} Garrison casualties: {initialGarrisonSize - finalGarrisonSize} ({casualtyPercentage:P2}). Calculating siege progress gain.");

                            if (casualtyPercentage > 0)
                            {
                                int fortLevel = twbattle.Sieges.GetFortLevel();
                                double totalRequiredProgress = 100 + (fortLevel * 75);
                                double currentProgress = twbattle.Sieges.GetSiegeProgress();
                                double remainingProgress = totalRequiredProgress - currentProgress;

                                if (remainingProgress < 0) remainingProgress = 0;

                                double progressToAdd = casualtyPercentage * remainingProgress;
                                double newProgress = currentProgress + progressToAdd;

                                Program.Logger.Debug(
                                    $"Adding {progressToAdd:F2} to siege progress. New progress: {newProgress:F2}");

                                // Append the new progress line, preserving indentation
                                updatedContent.AppendLine(
                                    $"{line.Substring(0, line.IndexOf("progress="))}progress={newProgress.ToString("F2", CultureInfo.InvariantCulture)}");
                            }
                            else
                            {
                                Program.Logger.Debug("No garrison casualties. Siege progress remains unchanged.");
                                updatedContent.AppendLine(line); // Append original line
                            }
                        }
                    }
                }
                else if (inTargetSiegeBlock && trimmedLine.StartsWith("breach=")) // NEW BLOCK: Update breach value
                {
                    breachLineHandled = true; // NEW: Mark that breach line was handled
                    if (breachIncrement > 0)
                    {
                        int currentBreach = 0;
                        Match breachMatch = Regex.Match(trimmedLine, @"breach=(\d+)");
                        if (breachMatch.Success && int.TryParse(breachMatch.Groups[1].Value, out currentBreach))
                        {
                            int newBreach = Math.Min(3, currentBreach + breachIncrement); // Cap at max 3
                            Program.Logger.Debug(
                                $"Updating breach from {currentBreach} to {newBreach} (increment: {breachIncrement}).");
                            updatedContent.AppendLine($"{line.Substring(0, line.IndexOf("breach="))}breach={newBreach}");
                        }
                        else
                        {
                            Program.Logger.Debug(
                                $"Warning: Could not parse current breach value from line: '{line}'. Appending original line.");
                            updatedContent.AppendLine(line);
                        }
                    }
                    else
                    {
                        updatedContent.AppendLine(line); // No increment, append original line
                    }
                }
                else if (inTargetSiegeBlock &&
                         trimmedLine.StartsWith("action_history={")) // MODIFIED BLOCK: Update action_history
                {
                    if (breachIncrement > 0 &&
                        i + 2 < fileLines.Count) // Ensure there are enough lines to read (opening, tokens, closing)
                    {
                        string tokensLineContent = fileLines[i + 1].Trim(); // e.g., "none none none starvation"
                        string tokensLineIndentation =
                            fileLines[i + 1]
                                .Substring(0,
                                    fileLines[i + 1].IndexOf(tokensLineContent)); // Get indentation of tokens line
                        string closingBraceIndentation =
                            fileLines[i + 2]
                                .Substring(0, fileLines[i + 2].IndexOf('}')); // Get indentation of closing brace line

                        List<string> tokens = tokensLineContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .ToList();
                        string originalTokensString = string.Join(" ", tokens); // For logging

                        for (int k = 0; k < breachIncrement; k++)
                        {
                            int noneIndex = tokens.IndexOf("none");
                            if (noneIndex != -1)
                            {
                                tokens[noneIndex] = "breach";
                            }
                            else if (tokens.Any())
                            {
                                // If no "none" tokens, remove the oldest and add "breach"
                                tokens.RemoveAt(0);
                                tokens.Add("breach");
                            }
                            else
                            {
                                // If history is empty, just add "breach"
                                tokens.Add("breach");
                            }
                        }

                        string newTokensString = string.Join(" ", tokens);
                        Program.Logger.Debug(
                            $"Updating action_history from '{{ {originalTokensString} }}' to '{{ {newTokensString} }}' (increment: {breachIncrement}).");

                        updatedContent.AppendLine(line); // Append "action_history={"
                        updatedContent.AppendLine(
                            $"{tokensLineIndentation}{newTokensString}"); // Append modified tokens line
                        updatedContent.AppendLine($"{closingBraceIndentation}}}"); // Append "}"

                        i += 2; // Skip the next two lines as they have been processed
                    }
                    else
                    {
                        updatedContent.AppendLine(line); // No increment or not enough lines, append original line
                    }
                }
                else
                {
                    // For all other lines, just append them
                    updatedContent.AppendLine(line);
                }
            }

            if (targetSiegeBlockFound) // Only write if we actually found the target siege block
            {
                File.WriteAllText(Writter.DataTEMPFilesPaths.Sieges_Path(), updatedContent.ToString());
                Program.Logger.Debug($"Sieges.txt updated for siege ID {SiegeID}.");
            }
            else
            {
                Program.Logger.Debug(
                    $"Sieges.txt for siege ID {SiegeID} was read, but the target siege block was not found.");
            }
        }
        
        public static (string outcome, string wall_damage) GetSiegeOutcome(string path_attila_log, string left_side_combat_side, string right_side_combat_side)
        {
            Program.Logger.Debug($"Entering GetSiegeOutcome for log file: {path_attila_log}");
            string outcome = "Successfully Defended"; // Default for defender
            string wall_damage = "No Damage";

            try
            {
                if (!File.Exists(path_attila_log))
                {
                    Program.Logger.Debug($"Attila log file not found at: {path_attila_log}. Returning default siege outcome.");
                    return (outcome, wall_damage);
                }

                string logContent;
                using (FileStream logFile = File.Open(path_attila_log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(logFile))
                {
                    logContent = reader.ReadToEnd();
                }

                logContent = logContent.Replace("\r\n", "\n");

                const string battleReportHeader =
                    "--------------------------------------------------------\n" +
                    "--------------------------------------------------------\n" +
                    "--\n" +
                    "--\t                    CRUSADER CONFLICTS               \n" +
                    "--\n" +
                    "--------------------------------------------------------\n" +
                    "--------------------------------------------------------";
                string normalizedHeader = battleReportHeader.Replace("\r\n", "\n");

                string relevantLogSection;
                int lastHeaderIndex = logContent.LastIndexOf(normalizedHeader);

                if (lastHeaderIndex != -1)
                {
                    relevantLogSection = logContent.Substring(lastHeaderIndex);
                }
                else
                {
                    relevantLogSection = logContent;
                }

                // Determine outcome
                bool settlementCaptured = relevantLogSection.Contains("SETTLEMENT_CAPTURED");
                if (settlementCaptured)
                {
                    outcome = "Settlement Captured";
                }
                else if (relevantLogSection.Contains("Victory")) // Fallback to "Victory" if SETTLEMENT_CAPTURED not found
                {
                    outcome = "Settlement Captured"; // Treat as captured if "Victory" is present in a siege context
                }

                // Determine wall damage using event counting logic from EditSiegesFile
                int wallsAttackedCount = Regex.Matches(relevantLogSection, "WALLS_ATTACKED").Count;
                int wallsDestroyedCount = Regex.Matches(relevantLogSection, "WALLS_DESTROYED").Count;

                if (wallsDestroyedCount > 1)
                {
                    wall_damage = "Breached";
                }
                else if (wallsDestroyedCount == 1)
                {
                    wall_damage = "Breached"; // Changed from "Damaged" to "Breached" for consistency with EditSiegesFile
                }
                else if (wallsAttackedCount > 0)
                {
                    wall_damage = "Damaged"; // Changed from "No Damage" to "Damaged" for consistency with EditSiegesFile
                }
                else
                {
                    wall_damage = "No Damage";
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error determining siege outcome: {ex.Message}. Returning default values.");
            }

            Program.Logger.Debug($"Determined Siege Outcome: {outcome}, Wall Damage: {wall_damage}");
            return (outcome, wall_damage);
        }


        public static bool HasBattleEnded(string path_attila_log)
        {
            Program.Logger.Debug($"Checking if battle has ended in log file: {path_attila_log}");
            using (FileStream logFile = File.Open(path_attila_log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(logFile))
            {
                string str = reader.ReadToEnd();

                if (str.Contains("Battle has finished"))
                {
                    reader.Close();
                    logFile.Close();
                    Program.Logger.Debug("Battle has finished marker found. Battle ended.");
                    return true;
                }
                else
                {
                    Program.Logger.Debug("Battle has finished marker not found. Battle still in progress.");
                    return false;
                }

            }
        }


        public static void ClearAttilaLog()
        {
            Program.Logger.Debug("Entering ClearAttilaLog method.");
            string Attila_Path = Properties.Settings.Default.VAR_attila_path;
            Properties.Settings.Default.VAR_log_attila = Attila_Path.Substring(0, Attila_Path.IndexOf("Attila.exe")) + "data\\BattleResults_log.txt";
            Properties.Settings.Default.Save();
            string path_attila_log = Properties.Settings.Default.VAR_log_attila;
            Program.Logger.Debug($"Attila log file path to clear: {path_attila_log}");

            bool isCreated = false;
            if (isCreated == false)
            {
                using (FileStream logFile = File.Open(path_attila_log, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    isCreated = true;
                    logFile.Close();
                }
            }
            Program.Logger.Debug("Attila log file cleared successfully.");
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
                        if (armyRegiment.Regiments != null)
                        {
                            foreach (var regiment in armyRegiment.Regiments)
                            {
                                if (regiment == null || string.IsNullOrEmpty(regiment.CurrentNum)) continue;

                                string key = $"{army.ID}_{regiment.ID}";
                                totalOriginalSize += originalSizes.ContainsKey(key) ? originalSizes[key] : 0;
                                totalFinalSize += Int32.Parse(regiment.CurrentNum);
                            }
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

        public static void EditWarsFile(List<Army> attacker_armies, List<Army> defender_armies, double warScoreChange)
        {
            Program.Logger.Debug("Editing Wars file...");
            if (string.IsNullOrEmpty(WarID))
            {
                Program.Logger.Debug("WarID is not set. Skipping Wars file edit and copying original to temp.");
                if (File.Exists(Writter.DataFilesPaths.Wars_Path()))
                {
                    File.Copy(Writter.DataFilesPaths.Wars_Path(), Writter.DataTEMPFilesPaths.Wars_Path(), true);
                }
                return;
            }

            try
            {
                string warsContent = File.ReadAllText(Writter.DataFilesPaths.Wars_Path());
                
                string warBlockPattern = $@"^\t\t{WarID}={{([\s\S]*?)^\t\t}}";
                Match warBlockMatch = Regex.Match(warsContent, warBlockPattern, RegexOptions.Multiline);

                if (!warBlockMatch.Success)
                {
                    Program.Logger.Debug($"Could not find war block for WarID: {WarID}. Skipping edit.");
                    File.Copy(Writter.DataFilesPaths.Wars_Path(), Writter.DataTEMPFilesPaths.Wars_Path(), true);
                    return;
                }

                string originalWarBlock = warBlockMatch.Value;
                string warBlockContent = originalWarBlock;

                // Determine if the battle winner is on the war attacker's side
                var warAttackersMatch = Regex.Match(warBlockContent, @"attackers={\s*([\d\s]+)\s*}");
                if (!warAttackersMatch.Success)
                {
                    Program.Logger.Debug($"Could not find attackers list in war block for WarID: {WarID}. Skipping edit.");
                    File.Copy(Writter.DataFilesPaths.Wars_Path(), Writter.DataTEMPFilesPaths.Wars_Path(), true);
                    return;
                }

                var warAttackerIds = new HashSet<string>(warAttackersMatch.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                
                string leftSideParticipantId = CK3LogData.LeftSide.GetMainParticipant().id;
                bool isLeftSideWarAttacker = warAttackerIds.Contains(leftSideParticipantId);

                var left_side_armies = ArmiesReader.GetSideArmies("left", attacker_armies, defender_armies);
                bool isBattleAttackerLeftSide = (left_side_armies == attacker_armies);

                bool isWinnerLeftSide = (IsAttackerVictorious && isBattleAttackerLeftSide) || (!IsAttackerVictorious && !isBattleAttackerLeftSide);

                bool winnerIsWarAttacker = (isWinnerLeftSide && isLeftSideWarAttacker) || (!isWinnerLeftSide && !isLeftSideWarAttacker);

                var warScoreMatch = Regex.Match(warBlockContent, @"(war_score={\s*)([\d\.-]+)\s+([\d\.-]+)(\s*})");
                if (!warScoreMatch.Success)
                {
                    Program.Logger.Debug($"Could not find war_score in war block for WarID: {WarID}. Skipping edit.");
                    File.Copy(Writter.DataFilesPaths.Wars_Path(), Writter.DataTEMPFilesPaths.Wars_Path(), true);
                    return;
                }

                double currentAttackerScore = double.Parse(warScoreMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                double currentDefenderScore = double.Parse(warScoreMatch.Groups[3].Value, CultureInfo.InvariantCulture);

                double newAttackerScore, newDefenderScore;

                if (winnerIsWarAttacker)
                {
                    newAttackerScore = currentAttackerScore + warScoreChange;
                    newDefenderScore = currentDefenderScore - warScoreChange;
                    Program.Logger.Debug($"Winner is war attacker. Adding {warScoreChange} to war score.");
                }
                else
                {
                    newAttackerScore = currentAttackerScore - warScoreChange;
                    newDefenderScore = currentDefenderScore + warScoreChange;
                    Program.Logger.Debug($"Winner is war defender. Subtracting {warScoreChange} from war score.");
                }

                newAttackerScore = Math.Clamp(newAttackerScore, -100.0, 100.0);
                newDefenderScore = Math.Clamp(newDefenderScore, -100.0, 100.0);

                string newWarScoreLine = $"{warScoreMatch.Groups[1].Value}{newAttackerScore.ToString("F4", CultureInfo.InvariantCulture)} {newDefenderScore.ToString("F4", CultureInfo.InvariantCulture)}{warScoreMatch.Groups[4].Value}";
                
                string updatedWarBlock = warBlockContent.Replace(warScoreMatch.Value, newWarScoreLine);
                string updatedWarsContent = warsContent.Replace(originalWarBlock, updatedWarBlock);

                File.WriteAllText(Writter.DataTEMPFilesPaths.Wars_Path(), updatedWarsContent);
                Program.Logger.Debug($"Successfully updated war score for WarID {WarID}. New score: Attacker={newAttackerScore:F4}, Defender={newDefenderScore:F4}");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error editing Wars file: {ex.Message}. Copying original to temp.");
                File.Copy(Writter.DataFilesPaths.Wars_Path(), Writter.DataTEMPFilesPaths.Wars_Path(), true);
            }
        }
    }
}
