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
                            bool isSlain = (searchData.isCommander && searchData.