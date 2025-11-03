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
                        $@"(?<Unit>.+_army{army.ID}_TYPE(?<Type>.+)_CULTURE(?<Culture>.+)_.+)-(?<Remaining>.+)");
                    foreach (Match match in pattern)
                    {
                        string unit_script = match.Groups["Unit"].Value;
                        string remaining = match.Groups["Remaining"].Value;
                        string culture_id = match.Groups["Culture"].Value;
                        string type = Regex.Match(match.Groups["Type"].Value, @"\D+").Value;

                        list.Add((unit_script, type, culture_id, remaining));
                    }

                    break;
                case DataType.Kills:
                    pattern = Regex.Matches(text,
                        $@"(?<Unit>kills_.+_army{army.ID}_TYPE(?<Type>.+)_CULTURE(?<Culture>.+)_.+)-(?<Kills>.+)");
                    foreach (Match match in pattern)
                    {

                        string unit_script = match.Groups["Unit"].Value;
                        string kills = match.Groups["Kills"].Value;
                        string culture_id = match.Groups["Culture"].Value;
                        string type = Regex.Match(match.Groups["Type"].Value, @"\D+").Value;

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
            if (army.IsGarrison())
            {
                Program.Logger.Debug($"Processing casualties for Garrison Army {army.ID}.");
                foreach (var unit in army.Units)
                {
                    if (unit == null) continue;

                    var unitReport = army.CasualitiesReports.FirstOrDefault(x =>
                            x.GetUnitType() == unit.GetRegimentType() &&
                            x.GetCulture()?.ID == unit.GetObjCulture()?.ID &&
                            x.GetTypeName() == unit.GetAttilaUnitKey() // Garrisons are matched by Attila key
                    );

                    if (unitReport != null)
                    {
                        int killed = unitReport.GetKilled();
                        if (killed <= 0) continue;

                        int originalSoldiers = unit.GetSoldiers();
                        int remainingSoldiers = Math.Max(0, originalSoldiers - killed);
                        int casualtiesApplied = originalSoldiers - remainingSoldiers;

                        unit.ChangeSoldiers(remainingSoldiers);
                        unitReport.SetKilled(killed - casualtiesApplied); // Update report with remaining casualties

                        Program.Logger.Debug(
                            $"Garrison Unit Report: Type '{unit.GetAttilaUnitKey()}', Culture: {unit.GetCulture()}: Soldiers changed from {originalSoldiers} to {remainingSoldiers}.");
                    }
                }
                return; // Exit after processing garrison
            }

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
                // For Garrison, the Unit.GetName() is "Garrison".
                string unitNameToMatch = armyRegiment.MAA_Name;
                if (armyRegiment.Type == RegimentType.Levy) unitNameToMatch = "Levy";
                if (armyRegiment.Type == RegimentType.Garrison) unitNameToMatch = "Garrison";

                // Find a corresponding Unit object. We use the culture of the first regiment for matching,
                // assuming all regiments within an ArmyRegiment share the same base unit type and culture for siege status.
                Unit? correspondingUnit = army.Units.FirstOrDefault(u =>
                    u.GetRegimentType() == armyRegiment.Type &&
                    u.GetName() == unitNameToMatch &&
                    u.GetObjCulture()?.ID == armyRegiment.Regiments.FirstOrDefault()?.Culture?.ID
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


                foreach (Regiment regiment in armyRegiment.Regiments)
                {
                    if (regiment.Culture is null) continue; // skip siege maa

                    string unitTypeNameToFind;
                    if (armyRegiment.Type == RegimentType.Levy)
                    {
                        unitTypeNameToFind = "Levy";
                    }
                    else
                    {
                        unitTypeNameToFind = armyRegiment.MAA_Name;
                    }

                    var unitReport = army.CasualitiesReports.FirstOrDefault(x =>
                        x.GetUnitType() == armyRegiment.Type && x.GetCulture() != null &&
                        x.GetCulture().ID == regiment.Culture.ID && x.GetTypeName() == unitTypeNameToFind);
                    if (unitReport == null)
                        continue;

                    int killed = unitReport.GetKilled();

                    // Check if CurrentNum is null or empty before parsing
                    if (string.IsNullOrEmpty(regiment.CurrentNum)) continue;

                    if (isSiegeType)
                    {
                        // NEW LOGIC: Proportional casualties for all siege weapon types
                        int finalMachineCount = 0;
                        int originalMachines = Int32.Parse(regiment.CurrentNum);

                        if (correspondingUnit != null && unitReport.GetStarting() > 0)
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
                        // Original logic for non-siege units (soldiers)
                        int originalSoldiers = Int32.Parse(regiment.CurrentNum); // Capture original value
                        int regSoldiers = originalSoldiers;
                        while (regSoldiers > 0 && killed > 0)
                        {
                            if (regSoldiers > killed)
                            {
                                regSoldiers -= killed;
                                killed = 0;
                            }
                            else
                            {
                                killed -= regSoldiers;
                                regSoldiers = 0;
                            }
                        }

                        regiment.SetSoldiers(regSoldiers.ToString());
                        unitReport.SetKilled(killed);
                        Program.Logger.Debug(
                            $"Non-Siege Regiment {regiment.ID} (Type: {armyRegiment.Type}, Culture: {regiment.Culture?.ID ?? "N/A"}): Soldiers changed from {originalSoldiers} to {regSoldiers}.");
                    }
                }

                // Moved these lines outside the inner loop
                int army_regiment_total = armyRegiment.Regiments.Where(reg => !string.IsNullOrEmpty(reg.CurrentNum))
                    .Sum(x => Int32.Parse(x.CurrentNum!));
                armyRegiment.SetCurrentNum(army_regiment_total.ToString());
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
            var grouped = army.UnitsResults!.Alive_MainPhase.GroupBy(item => new { item.Type, item.CultureID });
            Program.Logger.Debug($"Found {grouped.Count()} unit groups for army {army.ID}.");
            var pursuit_grouped =
                army.UnitsResults.Alive_PursuitPhase?.GroupBy(item => new { item.Type, item.CultureID });

            Program.Logger.Debug("#############################");
            Program.Logger.Debug($"REPORT FROM {army.CombatSide.ToUpper()} ARMY {army.ID}");
            foreach (var group in grouped)
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

                if (army.IsGarrison())
                {
                    unitType = RegimentType.Garrison;
                    type = group.Key.Type; // The log type is the Attila unit key
                }
                else
                {
                    // This logic is for field armies
                    if (group.Key.Type.Contains("Levy"))
                    {
                        unitType = RegimentType.Levy;
                        type = "Levy"; // Match against the generic unit name "Levy"
                    }
                    else if (group.Key.Type.Contains("commander") || group.Key.Type == "knights")
                    {
                        continue;
                    }
                    else
                    {
                        unitType = RegimentType.MenAtArms;
                        type = group.Key.Type; // The log type is the CK3 MAA name
                    }
                }

                // Search for type, culture, starting soldiers and remaining soldiers of a Unit
                if (army.Units == null)
                {
                    continue;
                }

                // Safely get the unit(s), then its culture.
                var matchingUnits = army.Units.Where(x => x != null &&
                    x.GetRegimentType() == unitType &&
                    x.GetObjCulture()?.ID == group.Key.CultureID &&
                    ((unitType == RegimentType.Garrison && x.GetAttilaUnitKey() == type) ||
                     (unitType != RegimentType.Garrison && x.GetName() == type))
                );

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
                    starting = UnitMappers_BETA.ConvertMachinesToMen(matchingUnits.Sum(u => u.GetOriginalSoldiers()));
                }
                else // Not a siege unit
                {
                    starting = matchingUnits.Sum(u => u.GetOriginalSoldiers());
                }

                int remaining = group.Sum(x => Int32.Parse(x.Remaining));

                // Create a Unit Report of the main casualities as default, if pursuit data is available, it creates one from the pursuit casualties
                UnitCasualitiesReport unitReport;
                var pursuitGroup = pursuit_grouped?.FirstOrDefault(x =>
                    x.Key.Type == group.Key.Type && x.Key.CultureID == group.Key.CultureID);

                if (pursuitGroup != null)
                {
                    int pursuitRemaining = pursuitGroup.Sum(x => Int32.Parse(x.Remaining));
                    unitReport =
                        new UnitCasualitiesReport(unitType, type, culture, starting, remaining, pursuitRemaining);
                }
                else
                {
                    unitReport = new UnitCasualitiesReport(unitType, type, culture, starting, remaining);
                }


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
            if (army.Knights != null && army.Knights.HasKnights())
            {
                if (army.UnitsResults == null) return;

                int remaining = 0;
                var knightReport = default((string Script, string Type, string CultureID, string Remaining));

                if (army.UnitsResults.Alive_PursuitPhase != null)
                {
                    knightReport = army.UnitsResults.Alive_PursuitPhase.FirstOrDefault(x => x.Type == "knights");
                }

                // If no report in pursuit phase, check main phase
                if (knightReport.Remaining == null && army.UnitsResults.Alive_MainPhase != null)
                {
                    knightReport = army.UnitsResults.Alive_MainPhase.FirstOrDefault(x => x.Type == "knights");
                }

                // If we found a report in either phase, parse it
                if (knightReport.Remaining != null)
                {
                    Int32.TryParse(knightReport.Remaining, out remaining);
                }

                army.Knights.GetKilled(remaining);
                foreach (var knight in army.Knights.GetKnightsList().Where(k => k.HasFallen()))
                {
                    Program.Logger.Debug($"Knight {knight.GetID()} ({knight.GetName()}) in army {army.ID} has fallen.");
                }
            }
        }

        public static void CheckKnightsKills(Army army)
        {
            Program.Logger.Debug($"Checking knight kills for army {army.ID}");
            if (army.Knights != null && army.Knights.HasKnights())
            {
                var knightKillsReport = army.UnitsResults!.Kills_MainPhase.FirstOrDefault(x => x.Type == "knights");
                int kills = 0;
                if (knightKillsReport.Kills != null)
                {
                    Int32.TryParse(knightKillsReport.Kills, out kills);
                }

                army.Knights.GetKills(kills);
                Program.Logger.Debug($"Total knight kills for army {army.ID}: {kills}");
            }

        }

        static (bool searchStarted, bool isCommander, CommanderSystem? commander, bool isKnight, Knight? knight, Army? army)
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
                            return (true, true, army.Commander, false, null, army);
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

        public static void EditLivingFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Living file (2-pass approach)...");
            var allArmies = attacker_armies.Concat(defender_armies).ToList();
            string playerCharId = DataSearch.Player_Character.GetID();
            string? playerHeirId = DataSearch.Player_Heir_ID;

            // --- PASS 1: Determine all health outcomes ---
            Program.Logger.Debug("Living file: Starting Pass 1 (Determine outcomes)");
            var healthOutcomes = new Dictionary<string, (bool isSlain, bool isCaptured, string newTraits)>();
            bool playerIsSlain = false;

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
                                    bool wasOnLosingSide = (searchData.army.CombatSide == "left" && !IsAttackerVictorious) ||
                                                           (searchData.army.CombatSide == "right" && IsAttackerVictorious);

                                    (bool isSlain, bool isCaptured, string newTraits) healthResult;
                                    if (searchData.isCommander)
                                    {
                                        healthResult = searchData.commander.Health(traitsLine, wasOnLosingSide);
                                    }
                                    else // isKnight
                                    {
                                        healthResult = searchData.knight.Health(traitsLine, wasOnLosingSide);
                                    }
                                    
                                    healthOutcomes[char_id] = healthResult;

                                    if (healthResult.isSlain && char_id == playerCharId)
                                    {
                                        playerIsSlain = true;
                                        Program.Logger.Debug($"Player character {playerCharId} was slain.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Program.Logger.Debug($"Living file: Pass 1 complete. Found {healthOutcomes.Count} characters with health outcomes. Player slain: {playerIsSlain}.");

            EditPlayerCharacterFiles(playerIsSlain, playerHeirId);

            // --- PASS 2: Apply changes and write to temp file ---
            Program.Logger.Debug("Living file: Starting Pass 2 (Apply changes)");
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

                        if (healthOutcomes.TryGetValue(char_id, out var outcome))
                        {
                            if (outcome.isSlain)
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
                            else // Wounded or Captured
                            {
                                // Apply wound traits first
                                int traitsLineIndex = charBlock.FindIndex(l => l.Trim().StartsWith("traits={"));
                                if (traitsLineIndex != -1)
                                {
                                    charBlock[traitsLineIndex] = outcome.newTraits;
                                }

                                // If captured, add prison_data block
                                if (outcome.isCaptured)
                                {
                                    var searchData = SearchCharacters(char_id, allArmies);
                                    Army characterArmy = searchData.army;

                                    if (characterArmy != null)
                                    {
                                        string imprisonerId = "";
                                        if (characterArmy.CombatSide == "left")
                                        {
                                            imprisonerId = CK3LogData.RightSide.GetMainParticipant().id;
                                        }
                                        else
                                        {
                                            imprisonerId = CK3LogData.LeftSide.GetMainParticipant().id;
                                        }

                                        // Fallback logic
                                        if (string.IsNullOrEmpty(imprisonerId))
                                        {
                                            if (characterArmy.CombatSide == "left") // Character was on left side, winner is right side
                                            {
                                                imprisonerId = CK3LogData.RightSide.GetCommander().id;
                                            }
                                            else // Character was on right side, winner is left side
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
                                    else
                                    {
                                        Program.Logger.Debug($"Warning: Could not find army for captured character {char_id}.");
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

        public static void EditCombatResultsFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Combat Results file...");

            bool inPlayerCombatResultBlock = false; // NEW: State flag to track if we are in the player's block

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
                        }
                        // 2b. If not the end of the block, run the original modification logic
                        else
                        {
                            if (line == "\t\t\tattacker={")
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
                                            if (mainArmy.MergedArmies != null && mainArmy.MergedArmies.Any(ma => ma.Commander?.ID == currentParticipantId))
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


        public static void EditCombatFile(List<Army> attacker_armies, List<Army> defender_armies, string player_armies_combat_side, string enemy_armies_combat_side, string path_attila_log)
        {
            Program.Logger.Debug("Editing Combat file using block replacement...");

            if (string.IsNullOrEmpty(Player_Combat) || string.IsNullOrEmpty(Original_Player_Combat))
            {
                Program.Logger.Debug("No player combat block found. Copying original Combats.txt to temp.");
                File.Copy(Writter.DataFilesPaths.Combats_Path(), Writter.DataTEMPFilesPaths.Combats_Path(), true);
                return;
            }

            // 1. Set winner and phase changes on the main Player_Combat string
            string winner = GetAttilaWinner(path_attila_log, player_armies_combat_side, enemy_armies_combat_side);
            SetWinner(winner); // This modifies Player_Combat in memory

            // 2. Isolate Attacker and Defender blocks to prevent regex cross-contamination
            Match attackerMatch = Regex.Match(Player_Combat, @"(attacker={[\s\S]*?^\t\t\t})", RegexOptions.Multiline);
            Match defenderMatch = Regex.Match(Player_Combat, @"(defender={[\s\S]*?^\t\t\t})", RegexOptions.Multiline);

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
            Player_Combat = Player_Combat.Replace(originalAttackerBlock, modifiedAttackerBlock);
            Player_Combat = Player_Combat.Replace(originalDefenderBlock, modifiedDefenderBlock);

            // 5. Perform final block replacement into the full Combats.txt content
            string fullFileContent = File.ReadAllText(Writter.DataFilesPaths.Combats_Path());
            string updatedFileContent = fullFileContent.Replace(Original_Player_Combat, Player_Combat);
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
                if (winner == "attacker")
                {
                    IsAttackerVictorious = true;
                    Program.Logger.Debug("Battle winner is attacker. IsAttackerVictorious = true.");
                }
                else
                {
                    IsAttackerVictorious = false;
                    Program.Logger.Debug("Battle winner is defender. IsAttackerVictorious = false.");
                }

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
                        if (army_regiment == null) continue; // Added null check
                        if (army_regiment.Type == RegimentType.Knight) continue;
                        if (army_regiment.ID == army_regiment_id)
                        {
                            editStarted = true;
                            editRegiment = army_regiment;
                            Program.Logger.Debug($"Found ArmyRegiment {army_regimentModel API Response Error. Please retry the previous request