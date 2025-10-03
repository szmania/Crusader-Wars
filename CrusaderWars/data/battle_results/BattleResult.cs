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
                using(StreamReader sr = new StreamReader(Writter.DataFilesPaths.Combats_Path()))
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
                        if(line == null) break;

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
                Program.Logger.Debug("Combat ID - " + battleID);
                File.WriteAllText(Writter.DataFilesPaths.Combats_Path(), Player_Combat);
            }
            catch(Exception ex)
            {
                Program.Logger.Debug($"Error reading player combat: {ex.Message}");
            }
       


        }

        public static void GetPlayerCombatResult()
        {
            Program.Logger.Debug("Getting player combat result...");
            try
            {
                string battle_id = "";
                StringBuilder f = new StringBuilder();
                using (StreamReader sr = new StreamReader(@".\data\save_file_data\BattleResults.txt"))
                {
                    if (twbattle.BattleState.IsSiegeBattle && SiegeID != null)
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
                                Program.Logger.Debug($"Found siege battle result block ID: {battle_id} for SiegeID: {SiegeID}");
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
                            else if (line.Trim() == $"\t\t\tlocation={ProvinceID}")
                            {
                                Program.Logger.Debug($"Found field battle result block ID: {battle_id} for ProvinceID: {ProvinceID}");
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
                            string year = date.Groups["year"].Value, month = date.Groups["month"].Value, day = date.Groups["day"].Value;
                            //FirstDay_Date = new twbattle.Date(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day));

                        }
                        else if(isSearchStarted && line == "\t\t}")
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
                Program.Logger.Debug("ResultID - " + battle_id);
                File.WriteAllText(@".\data\save_file_data\BattleResults.txt", f.ToString());
                Program.Logger.Debug("All combat results were read successfully");
            }
            catch(Exception ex)
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
                while((line = reader.ReadLine()) != null && !reader.EndOfStream)
                {
                    if(line == "-----REMAINING SOLDIERS-----!!")
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

                    else if(aliveSearchStarted)
                    {
                        aliveText += line + "\n";
                    }

                    else if(killsSearchStarted && line.StartsWith("kills"))
                    {
                        killsText += line + "\n";
                    }
                }
            }
            Program.Logger.Debug($"Found {aliveList.Count} entries for alive reports and {killsList.Count} entries for kill reports.");
            return (aliveList, killsList);
        }

        // Get attila remaining soldiers
        public static void ReadAttilaResults(Army army, string path_attila_log)
        {
            Program.Logger.Debug($"Reading Attila results for army {army.ID} from log: {path_attila_log}");
            try
            {
                UnitsResults units = new UnitsResults();
                List<(string Script, string Type, string CultureID, string Remaining)> Alive_MainPhase = new List<(string Script, string Type, string CultureID, string Remaining)>();
                List<(string Script, string Type, string CultureID, string Remaining)> Alive_PursuitPhase = new List<(string Script, string Type, string CultureID, string Remaining)>();
                List<(string Script, string Type, string CultureID, string Kills)> Kills_MainPhase = new List<(string Name, string Type, string CultureID, string Kills)>();
                List<(string Script, string Type, string CultureID, string Kills)> Kills_PursuitPhase = new List<(string Script, string Type, string CultureID, string Kills)>();

                var (AliveList, KillsList) = GetRemainingAndKills(path_attila_log);
                if (AliveList.Count == 0)
                {
                    Program.Logger.Debug($"Warning: No battle reports found in Attila log for army {army.ID}. Assuming no survivors or battle did not generate logs.");
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
                    Program.Logger.Debug($"Multiple battle reports found for army {army.ID}. Using the last two reports for Main and Pursuit phases.");
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
            catch(Exception e)
            {
                Program.Logger.Debug($"Error reading Attila results for army {army.ID}: {e.ToString()}");
                MessageBox.Show($"Error reading Attila results: {e.ToString()}", "Crusader Conflicts: Battle Results Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
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
            var list = new List<(string, string, string, string)> ();

            MatchCollection pattern;
            switch (list_type)
            {
                case DataType.Alive:
                    pattern = Regex.Matches(text, $@"(?<Unit>.+_army{army.ID}_TYPE(?<Type>.+)_CULTURE(?<Culture>.+)_.+)-(?<Remaining>.+)");
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
                    pattern = Regex.Matches(text, $@"(?<Unit>kills_.+_army{army.ID}_TYPE(?<Type>.+)_CULTURE(?<Culture>.+)_.+)-(?<Kills>.+)");
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

                        Program.Logger.Debug($"Garrison Unit Report: Type '{unit.GetAttilaUnitKey()}', Culture: {unit.GetCulture()}: Soldiers changed from {originalSoldiers} to {remainingSoldiers}.");
                    }
                }
                return; // Exit after processing garrison
            }

            foreach(ArmyRegiment armyRegiment in army.ArmyRegiments)
            {
                if (armyRegiment.Type == data.save_file.RegimentType.Commander || armyRegiment.Type == data.save_file.RegimentType.Knight) continue;

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
                    Program.Logger.Debug($"ArmyRegiment {armyRegiment.ID} (Type: {armyRegiment.Type}, Name: {armyRegiment.MAA_Name}) identified as a siege unit.");
                }
                else
                {
                    Program.Logger.Debug($"ArmyRegiment {armyRegiment.ID} (Type: {armyRegiment.Type}, Name: {armyRegiment.MAA_Name}) identified as a non-siege unit.");
                }


                foreach (Regiment regiment in armyRegiment.Regiments)
                {
                    if (regiment.Culture is null) continue; // skip siege maa

                    var unitReport = army.CasualitiesReports.FirstOrDefault(x => x.GetUnitType() == armyRegiment.Type && x.GetCulture() != null && x.GetCulture().ID == regiment.Culture.ID && x.GetTypeName() == armyRegiment.MAA_Name);
                    if (unitReport == null)
                        continue;

                    int killed = unitReport.GetKilled();
                    
                    // Check if CurrentNum is null or empty before parsing
                    if (string.IsNullOrEmpty(regiment.CurrentNum)) continue;

                    if (isSiegeType)
                    {
                        // Logic for siege units (machines)
                        int originalMachines = Int32.Parse(regiment.CurrentNum);
                        int startingMen;
                        if (originalMachines <= 0) startingMen = 0;
                        else if (originalMachines == 1) startingMen = 3;
                        else startingMen = (originalMachines * 4) - 2;

                        int menKilledInThisRegiment = Math.Min(killed, startingMen);
                        int remainingMen = startingMen - menKilledInThisRegiment;
                        int remainingMachines = ConvertMenToMachines(remainingMen);

                        regiment.SetSoldiers(remainingMachines.ToString());
                        unitReport.SetKilled(killed - menKilledInThisRegiment); // Update report with remaining casualties
                        Program.Logger.Debug($"Siege Regiment {regiment.ID} (Type: {armyRegiment.Type}, Culture: {regiment.Culture?.ID ?? "N/A"}): Machines changed from {originalMachines} to {remainingMachines} (Men: {startingMen} -> {remainingMen}). Killed: {menKilledInThisRegiment}.");
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
                        Program.Logger.Debug($"Non-Siege Regiment {regiment.ID} (Type: {armyRegiment.Type}, Culture: {regiment.Culture?.ID ?? "N/A"}): Soldiers changed from {originalSoldiers} to {regSoldiers}.");
                    }
                }
                // Moved these lines outside the inner loop
                int army_regiment_total = armyRegiment.Regiments.Where(reg => !string.IsNullOrEmpty(reg.CurrentNum)).Sum(x => Int32.Parse(x.CurrentNum!));
                armyRegiment.SetCurrentNum(army_regiment_total.ToString());
                Program.Logger.Debug($"Updated ArmyRegiment {armyRegiment.ID} total soldiers to: {army_regiment_total}");
            }
        }

        static void CreateUnitsReports(Army army)
        {
            if (army.UnitsResults == null)
            {
                Program.Logger.Debug($"Warning: army.UnitsResults is null for army {army.ID}. Skipping unit reports creation.");
                return;
            }

            Program.Logger.Debug($"Entering CreateUnitsReports for army {army.ID}.");
            List<UnitCasualitiesReport> reportsList = new List<UnitCasualitiesReport>();

            // Group by Type and CultureID
            var grouped = army.UnitsResults!.Alive_MainPhase.GroupBy(item => new { item.Type, item.CultureID });
            Program.Logger.Debug($"Found {grouped.Count()} unit groups for army {army.ID}.");
            var pursuit_grouped = army.UnitsResults.Alive_PursuitPhase?.GroupBy(item => new { item.Type, item.CultureID });

            Program.Logger.Debug("#############################");
            Program.Logger.Debug($"REPORT FROM {army.CombatSide.ToUpper()} ARMY {army.ID}");
            foreach(var group in grouped)
            {
                if (group.Key.Type == null)
                {
                    Program.Logger.Debug($"Warning: Skipping unit report due to null unit type in group key for army {army.ID}.");
                    continue;
                }
                Program.Logger.Debug($"Processing casualty report for group: Type='{group.Key.Type}', CultureID='{group.Key.CultureID}'.");
                
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
                if (army.Units == null) { continue; }
                
                // Safely get the unit, then its culture. If unit is null, culture will be null.
                // The warning CS8600 is because GetObjCulture() is called on a potentially null result of FirstOrDefault().
                var matchingUnit = army.Units.Where(x => x != null).FirstOrDefault(x =>
                    x.GetRegimentType() == unitType &&
                    x.GetObjCulture()?.ID == group.Key.CultureID &&
                    ((unitType == RegimentType.Garrison && x.GetAttilaUnitKey() == type) || (unitType != RegimentType.Garrison && x.GetName() == type))
                );
                
                if (matchingUnit == null)
                {
                    Program.Logger.Debug($"Warning: Could not find matching unit for type '{type}' and culture ID '{group.Key.CultureID}' in army {army.ID}. Skipping report for this unit group.");
                    continue;
                }
                Culture? culture = matchingUnit.GetObjCulture(); // CHANGE THIS LINE: Add '?' to make Culture nullable
                
                // If culture is null at this point, it means either no matching unit was found,
                // or the matching unit itself had a null culture object.
                // This scenario should be logged and skipped to prevent further errors.
                if (culture == null)
                {
                    Program.Logger.Debug($"Warning: Could not find valid culture for unit type '{type}' and culture ID '{group.Key.CultureID}' in army {army.ID}. Skipping report for this unit group.");
                    continue; // Skip this group if culture is unexpectedly null
                }

                int starting = matchingUnit.GetSoldiers();
                int remaining = group.Sum(x => Int32.Parse(x.Remaining));

                // Create a Unit Report of the main casualities as default, if pursuit data is available, it creates one from the pursuit casualties
                UnitCasualitiesReport unitReport;
                var pursuitGroup = pursuit_grouped?.FirstOrDefault(x => x.Key.Type == group.Key.Type && x.Key.CultureID == group.Key.CultureID);

                if (pursuitGroup != null)
                {
                    int pursuitRemaining = pursuitGroup.Sum(x => Int32.Parse(x.Remaining));
                    unitReport = new UnitCasualitiesReport(unitType, type, culture, starting, remaining, pursuitRemaining);
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

        public static void CheckForDeathCommanders(Army army, string path_attila_log)
        {
            Program.Logger.Debug($"Checking for commander death in army {army.ID}");
            if (army.Commander != null)
            {
                army.Commander.HasGeneralFallen(path_attila_log);
                if (army.Commander.hasFallen)
                {
                    Program.Logger.Debug($"Commander {army.Commander.ID} in army {army.ID} has fallen.");
                }
            }
        }

        public static void CheckForDeathKnights(Army army)
        {
            Program.Logger.Debug($"Checking for knight deaths in army {army.ID}");
            if(army.Knights != null && army.Knights.HasKnights())
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

        static (bool searchStarted, bool isCommander, CommanderSystem? commander, bool isKnight, Knight? knight) SearchCharacters(string char_id, List<Army> armies)
        {
            // Program.Logger.Debug($"Searching for character ID: {char_id}");
            foreach (Army army in armies)
            {
                if (army == null) continue; // Added null check for army
                if (army.Commander != null && army.Commander.ID == char_id)
                {
                    Program.Logger.Debug($"Commander {char_id} found in army {army.ID}.");
                    return (true, true, army.Commander, false, null);
                }
                else if (army.Knights?.GetKnightsList() != null)
                {
                    foreach(Knight knight_u in army.Knights.GetKnightsList())
                    {
                        if(knight_u.GetID() == char_id)
                        {
                            Program.Logger.Debug($"Knight {char_id} found in army {army.ID}.");
                            return (true, false, null, true, knight_u);
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
                            Program.Logger.Debug($"Commander {char_id} found in merged army {mergedArmy.ID} (part of main army {army.ID}).");
                            return (true, true, army.Commander, false, null);
                        }
                        else if (mergedArmy.Knights?.GetKnightsList() != null)
                        {
                            foreach (Knight knight_u in mergedArmy.Knights.GetKnightsList())
                            {
                                if (knight_u.GetID() == char_id)
                                {
                                    Program.Logger.Debug($"Knight {char_id} found in merged army {mergedArmy.ID} (part of main army {army.ID}).");
                                    return (true, false, null, true, knight_u);
                                }
                            }
                        }
                    }
                }
            }
            // Program.Logger.Debug($"Character ID: {char_id} not found in any army.");
            return (false, false, null, false, null);
        }

        public static void EditLivingFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Living file...");
            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.Living_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.Living_Path()))
            {
                streamWriter.NewLine = "\n";

                bool searchStarted = false;
                bool isCommander = false;
                bool isKnight = false;

                CommanderSystem? commander = null;
                Knight? knight = null;


                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if(!searchStarted && line != null && Regex.IsMatch(line, @"\d+={"))
                    {
                        string char_id = Regex.Match(line, @"\d+").Value;

                        var searchData = SearchCharacters(char_id, attacker_armies);
                        if(searchData.searchStarted)
                        {
                            searchStarted = true;
                            if(searchData.isCommander)
                            {
                                isCommander = true;
                                commander = searchData.commander;
                                Program.Logger.Debug($"Found character {char_id} as Commander (Attacker).");
                            }
                            else if(searchData.isKnight)
                            {
                                isKnight = true;
                                knight = searchData.knight;
                                Program.Logger.Debug($"Found character {char_id} as Knight (Attacker).");
                            }
                        }
                        else
                        {
                            searchData = SearchCharacters(char_id, defender_armies);
                            if(searchData.searchStarted)
                            {
                                searchStarted = true;
                                if (searchData.isCommander)
                                {
                                    isCommander = true;
                                    commander = searchData.commander;
                                    Program.Logger.Debug($"Found character {char_id} as Commander (Defender).");
                                }
                                else if (searchData.isKnight)
                                {
                                    isKnight = true;
                                    knight = searchData.knight;
                                    Program.Logger.Debug($"Found character {char_id} as Knight (Defender).");
                                }
                            }
                        }
                    }

                    else if(searchStarted && line.StartsWith("\ttraits={"))
                    {
                        string edited_line = line;
                        if (isCommander && commander != null && commander.hasFallen)
                        {
                            edited_line = commander.Health(edited_line);
                            Program.Logger.Debug($"Applying wounds to fallen commander {commander.ID}.");
                        }
                        else if(isKnight && knight != null)
                        {
                            edited_line = knight.Health(edited_line);
                            Program.Logger.Debug($"Applying wounds to knight {knight.GetID()}.");
                        }

                        streamWriter.WriteLine(edited_line);
                        continue;
                    }

                    else if(searchStarted && line == "}")
                    {
                        searchStarted = false;
                        isCommander = false;
                        isKnight = false;
                        commander = null;
                        knight = null;
                    }

                    streamWriter.WriteLine(line);
                }
            }
            Program.Logger.Debug("Finished editing Living file.");
        }

        public static void EditCombatResultsFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Combat Results file...");
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

                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line == "\t\t\tattacker={")
                    {
                        isAttacker = true; isDefender = false;
                        Program.Logger.Debug("Processing attacker results in CombatResults file.");
                    }

                    else if (line == "\t\t\tdefender={")
                    {
                        isDefender = true; isAttacker = false;
                        Program.Logger.Debug("Processing defender results in CombatResults file.");

                    }

                    if (isAttacker)
                    {
                        if (line.Contains("\t\t\t\tsurviving_soldiers="))
                        {
                            int totalFightingMen = GetArmiesTotalFightingMen(attacker_armies);
                            string edited_line = "\t\t\t\tsurviving_soldiers=" + totalFightingMen;
                            streamWriter.WriteLine(edited_line);
                            Program.Logger.Debug($"Attacker: surviving_soldiers={totalFightingMen}");
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
                            //4294967295
                            string id = Regex.Match(line, @"\d+").Value;
                            if (id == "4294967295" && !isMAA)
                            {
                                regimentType = "Levy";
                                Program.Logger.Debug($"Attacker: Detected Levy regiment (ID: {id}).");
                            }    
                            else if(id == "4294967295" && isMAA)
                            {
                                isMAA = true; // Already set, but good for clarity
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
                        else if (isKnight && line.Contains("\t\t\t\t\t\tmain_kills=")) // <-- KNIGHTS MAIN KILLS
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
                                        break; // Found the knight, no need to check other armies
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
                                    main_losses += results.GetDeathAmountOfMainPhase(army.CasualitiesReports,regimentType);
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
                        else if(line == "\t\t\t\t\t}")
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
                            int totalFightingMen = GetArmiesTotalFightingMen(defender_armies);
                            string edited_line = "\t\t\t\tsurviving_soldiers=" + totalFightingMen;
                            streamWriter.WriteLine(edited_line);
                            Program.Logger.Debug($"Defender: surviving_soldiers={totalFightingMen}");
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
                            //4294967295
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
                        else if (isKnight && line.Contains("\t\t\t\t\t\tmain_kills=")) // <-- KNIGHTS MAIN KILLS
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
                                        break; // Found the knight, no need to check other armies
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

                    streamWriter.WriteLine(line);
                }
            }
            Program.Logger.Debug("Finished editing Combat Results file.");
        }

   
        public static void EditCombatFile(List<Army> attacker_armies,List<Army> defender_armies,string player_armies_combat_side, string enemy_armies_combat_side, string path_attila_log)
        {
            Program.Logger.Debug("Editing Combat file...");
            string winner = GetAttilaWinner(path_attila_log, player_armies_combat_side, enemy_armies_combat_side);
            SetWinner(winner);

            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.Combats_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.Combats_Path()))
            {
                streamWriter.NewLine = "\n";

                bool isAttacker = false;
                bool isDefender = false;
                string army_regiment_id = "";

                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if(line == "\t\t\tattacker={")
                    {
                        isAttacker = true; isDefender = false;
                        Program.Logger.Debug("Processing attacker data in Combat file.");
                    }

                    else if (line == "\t\t\tdefender={")
                    {
                        isDefender = true; isAttacker = false;
                        Program.Logger.Debug("Processing defender data in Combat file.");

                    }
                    
                    if(isAttacker)
                    {
                        if (line.Contains("\t\t\t\t\t\tregiment="))
                        {
                            army_regiment_id = Regex.Match(line, @"\d+").Value;
                        }
                        else if (line.Contains("\t\t\t\t\t\tcurrent="))
                        {
                            var armyRegAttacker = SearchArmyRegiment(attacker_armies, army_regiment_id);
                            string currentNum = armyRegAttacker != null ? armyRegAttacker.CurrentNum.ToString() : "0";
                            string edited_line = "\t\t\t\t\t\tcurrent=" + currentNum;
                            streamWriter.WriteLine(edited_line);
                            Program.Logger.Debug($"Attacker: Regiment {army_regiment_id} current={currentNum}");
                            continue;
                        }
                        else if (line.Contains("\t\t\t\t\t\tsoft_casualties="))
                        {
                            streamWriter.WriteLine(line);
                            continue;
                        }
                        else if (line.Contains("\t\t\t\ttotal_fighting_men="))
                        {
                            int totalFightingMen = GetArmiesTotalFightingMen(attacker_armies);
                            string edited_line = "\t\t\t\ttotal_fighting_men=" + totalFightingMen;
                            streamWriter.WriteLine(edited_line);
                            Program.Logger.Debug($"Attacker: total_fighting_men={totalFightingMen}");
                            continue;
                        }
                        else if (line.Contains("\t\t\t\total_levy_men="))
                        {
                            int totalLevyMen = GetArmiesTotalLevyMen(attacker_armies);
                            string edited_line = "\t\t\t\ttotal_levy_men=" + totalLevyMen;
                            streamWriter.WriteLine(edited_line);
                            Program.Logger.Debug($"Attacker: total_levy_men={totalLevyMen}");
                            continue;
                        }

                    }
                    else if (isDefender)
                    {
                        if (line.Contains("\t\t\t\t\t\tregiment="))
                        {
                            army_regiment_id = Regex.Match(line, @"\d+").Value;
                        }
                        else if (line.Contains("\t\t\t\t\t\tcurrent="))
                        {
                            var armyRegDefender = SearchArmyRegiment(defender_armies, army_regiment_id);
                            string currentNum = armyRegDefender != null ? armyRegDefender.CurrentNum.ToString() : "0";
                            string edited_line = "\t\t\t\t\t\tcurrent=" + currentNum;
                            streamWriter.WriteLine(edited_line);
                            Program.Logger.Debug($"Defender: Regiment {army_regiment_id} current={currentNum}");
                            continue;
                        }
                        else if (line.Contains("\t\t\t\t\t\tsoft_casualties="))
                        {
                            streamWriter.WriteLine(line);
                            continue;
                        }
                        else if (line.Contains("\t\t\t\ttotal_fighting_men="))
                        {
                            int totalFightingMen = GetArmiesTotalFightingMen(defender_armies);
                            string edited_line = "\t\t\t\ttotal_fighting_men=" + totalFightingMen;
                            streamWriter.WriteLine(edited_line);
                            Program.Logger.Debug($"Defender: total_fighting_men={totalFightingMen}");
                            continue;
                        }
                        else if (line.Contains("\t\t\t\total_levy_men="))
                        {
                            int totalLevyMen = GetArmiesTotalLevyMen(defender_armies);
                            string edited_line = "\t\t\t\ttotal_levy_men=" + totalLevyMen;
                            streamWriter.WriteLine(edited_line);
                            Program.Logger.Debug($"Defender: total_levy_men={totalLevyMen}");
                            continue;
                        }
                    }
                    
                    streamWriter.WriteLine(line);
                }
            }
            Program.Logger.Debug("Finished editing Combat file.");
        }
        static int GetArmiesTotalFightingMen(List<Army> armies)
        {
            int total = armies.Where(army => army != null && army.ArmyRegiments != null)
                              .SelectMany(army => army.ArmyRegiments)
                              .Where(armyRegiment => armyRegiment != null)
                              .Sum(armyRegiment => armyRegiment.CurrentNum);

            string logMessage = string.Format("Calculated total fighting men for armies: {0}", total);
            Program.Logger.Debug(logMessage);
            return total;
        }

        static int GetArmiesTotalLevyMen(List<Army> armies)
        {
            int total = 0;
            foreach (Army army in armies)
            {
                if (army.ArmyRegiments == null) continue;
                total += army.ArmyRegiments.Where(y => y != null && y.Type == RegimentType.Levy).Sum(x => x.CurrentNum);

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

        public static string GetAttilaWinner(string path_attila_log, string player_armies_combat_side, string enemy_armies_combat_side)
        {
            Program.Logger.Debug($"Entering GetAttilaWinner for log file: {path_attila_log}");
            string winner = enemy_armies_combat_side; // Initialize to enemy as default fallback
            using (FileStream logFile = File.Open(path_attila_log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(logFile))
            {
                string? line;
                //winning_side=attacker/defender
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("Victory"))
                    {
                        winner = player_armies_combat_side;
                    }
                    else if (line.Contains("Defeat"))
                    {
                        winner = enemy_armies_combat_side;
                    }
                    // Removed the 'else winner = enemy_armies_combat_side;'
                }

                Program.Logger.Debug($"Determined Attila winner: {winner}");
                return winner;
            }
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
                Player_Combat = Regex.Replace(Player_Combat ?? string.Empty, @"(base_combat_width=\d+)", "$1\n\t\t\twinning_side=" + winner);

                Player_Combat = Player_Combat?.Replace("\r", "");

                File.WriteAllText(Writter.DataFilesPaths.Combats_Path(), Player_Combat);

                Program.Logger.Debug("Winner of battle set successfully");
            }
            catch(Exception ex)
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
                        Program.Logger.Debug($"Updated ArmyRegiment {editArmyRegiment.ID} current soldiers to {editArmyRegiment.CurrentNum}.");
                        continue;
                    }

                    //End Line
                    else if (editStarted && line == "\t\t}")
                    {
                        editStarted = false; editArmyRegiment = null;
                    }

                    streamWriter.WriteLine(line);
                }
            }
            Program.Logger.Debug("Finished editing Army Regiments file.");
        }

        static (bool editStarted, ArmyRegiment? editArmyRegiment) SearchArmyRegimentsFile(List<Army> armies, string army_regiment_id)
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
                str =      $"\t\t\tmax={size}\n" +
                           $"\t\t\tchunks={{\n" +
                           $"\t\t\t\t{{\n" +
                           $"\t\t\t\t\tmax={size}\n" +
                           $"\t\t\t\t\tcurrent={current}\n" +
                           $"\t\t\t\t}}\n" +
                           $"\t\t\t}}\n";
            }
            else
            {
                str =      $"\t\t\tmax={size}\n" +
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
                    if(!editStarted && line != null && Regex.IsMatch(line, @"\t\t\d+={"))
                    {
                        string regiment_id = Regex.Match(line, @"\d+").Value;


                        var searchingData = SearchRegimentsFile(attacker_armies,regiment_id);
                        if(searchingData.editStarted)
                        {
                            editStarted = true;
                            editRegiment = searchingData.editRegiment;
                            parentArmyRegiment = searchingData.parentArmyRegiment; // Store parent ArmyRegiment
                            Program.Logger.Debug($"Found Regiment {regiment_id} for editing (Attacker).");
                        }
                        else
                        {
                            searchingData = SearchRegimentsFile(defender_armies,regiment_id);
                            if(searchingData.editStarted)
                            {
                                editStarted = true;
                                editRegiment = searchingData.editRegiment;
                                parentArmyRegiment = searchingData.parentArmyRegiment; // Store parent ArmyRegiment
                                Program.Logger.Debug($"Found Regiment {regiment_id} for editing (Defender).");
                            }
                        }

                    }

                    else if(editStarted && line.Contains("\t\t\tsize="))
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
                                string logMessage = string.Format("Regiment {0}: Updating Men-at-Arms size to {1}.", regId, currentNum);
                                Program.Logger.Debug(logMessage);
                            }
                            continue; // Continue to next line without setting isNewData
                        }
                        else if (editRegiment != null)
                        {
                            var reg = editRegiment; // New local variable
                            // For other types (Levy), use the existing logic to rewrite the block.
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

        static (bool editStarted, Regiment? editRegiment, ArmyRegiment? parentArmyRegiment) SearchRegimentsFile(List<Army> armies, string regiment_id)
        {
            // Program.Logger.Debug($"Searching for Regiment ID: {regiment_id} in Regiments file.");
            bool editStarted = false;
            Regiment? editRegiment = null;
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
                            editRegiment = regiment;
                            parentArmyRegiment = armyRegiment;
                            Program.Logger.Debug($"Found Regiment {regiment_id} with parent ArmyRegiment {armyRegiment.ID}.");
                            return (editStarted, editRegiment, parentArmyRegiment);
                        }
                    }
                }
            }
            // Program.Logger.Debug($"Regiment ID: {regiment_id} not found in Regiments file.");
            return (false, null, null);
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

                // Regular Regiments (Levy, MenAtArms)
                Program.Logger.Debug("  REGIMENTS:");
                if (army.ArmyRegiments != null)
                {
                    foreach (var armyRegiment in army.ArmyRegiments)
                    {
                        if (armyRegiment == null || armyRegiment.Type == RegimentType.Commander || armyRegiment.Type == RegimentType.Knight) continue;

                        string regimentTypeName = armyRegiment.Type == RegimentType.Levy ? "Levy" : armyRegiment.MAA_Name;

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

        public static void EditSiegesFile(string path_log_attila, string attacker_side, string defender_side, List<Army> defender_armies)
        {
            Program.Logger.Debug("Entering EditSiegesFile method.");

            if (!BattleState.IsSiegeBattle)
            {
                Program.Logger.Debug("Not a siege battle. Skipping EditSiegesFile.");
                return;
            }

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

            // 1. Calculate Breach Increment
            int breachIncrement = 0;
            if (File.Exists(path_log_attila))
            {
                string attilaLogContent = File.ReadAllText(path_log_attila);
                int wallsAttackedCount = Regex.Matches(attilaLogContent, "WALLS_ATTACKED").Count;
                int wallsDestroyedCount = Regex.Matches(attilaLogContent, "WALLS_DESTROYED").Count;

                if (wallsDestroyedCount > 1)
                {
                    breachIncrement = 3;
                    Program.Logger.Debug($"Attila log: Multiple WALLS_DESTROYED events ({wallsDestroyedCount}). Setting breachIncrement to 3.");
                }
                else if (wallsDestroyedCount == 1)
                {
                    breachIncrement = 2;
                    Program.Logger.Debug($"Attila log: One WALLS_DESTROYED event. Setting breachIncrement to 2.");
                }
                else if (wallsAttackedCount > 0)
                {
                    breachIncrement = 1;
                    Program.Logger.Debug($"Attila log: WALLS_ATTACKED event(s) found ({wallsAttackedCount}). Setting breachIncrement to 1.");
                }
                else
                {
                    Program.Logger.Debug("Attila log: No wall attack/destruction events found. breachIncrement remains 0.");
                }
            }
            else
            {
                Program.Logger.Debug($"Attila log file not found at {path_log_attila}. Cannot calculate breach increment.");
            }


            List<string> fileLines = File.ReadAllLines(siegesFilePath).ToList();
            StringBuilder updatedContent = new StringBuilder();
            bool inTargetSiegeBlock = false;
            bool targetSiegeBlockFound = false; // Track if we entered the target siege block at all
            bool breachLineHandled = false; // NEW: Track if breach= line was found/handled

            // Determine if attacker lost. This would be set by other BattleResult methods.
            bool attackerLost = !IsAttackerVictorious; // Assuming IsAttackerVictorious is set elsewhere

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
                    // NEW: Add missing breach= line before closing brace if applicable
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
                    if (attackerLost)
                    {
                        // --- NEW LOGIC FOR ATTACKER DEFEAT ---
                        int initialGarrisonSize = twbattle.Sieges.GetGarrisonSize();
                        int finalGarrisonSize = defender_armies.Where(a => a.IsGarrison()).Sum(a => a.GetTotalSoldiers());

                        double casualtyPercentage = 0;
                        if (initialGarrisonSize > 0)
                        {
                            int casualties = initialGarrisonSize - finalGarrisonSize;
                            if (casualties > 0)
                            {
                                casualtyPercentage = (double)casualties / initialGarrisonSize;
                            }
                        }
                        Program.Logger.Debug($"Garrison casualties: {initialGarrisonSize - finalGarrisonSize} ({casualtyPercentage:P2}). Calculating siege progress gain.");

                        if (casualtyPercentage > 0)
                        {
                            int fortLevel = twbattle.Sieges.GetFortLevel();
                            double totalRequiredProgress = 100 + (fortLevel * 75);
                            double currentProgress = twbattle.Sieges.GetSiegeProgress();
                            double remainingProgress = totalRequiredProgress - currentProgress;

                            if (remainingProgress < 0) remainingProgress = 0;

                            double progressToAdd = casualtyPercentage * remainingProgress;
                            double newProgress = currentProgress + progressToAdd;

                            Program.Logger.Debug($"Adding {progressToAdd:F2} to siege progress. New progress: {newProgress:F2}");

                            // Append the new progress line, preserving indentation
                            updatedContent.AppendLine($"{line.Substring(0, line.IndexOf("progress="))}progress={newProgress.ToString("F2", CultureInfo.InvariantCulture)}");
                        }
                        else
                        {
                            Program.Logger.Debug("No garrison casualties. Siege progress remains unchanged.");
                            updatedContent.AppendLine(line); // Append original line
                        }
                        // --- END NEW LOGIC ---
                    }
                    else // Attacker won
                    {
                        // Attacker won: set progress to 100%
                        int fortLevel = twbattle.Sieges.GetFortLevel();
                        double newProgress = 100 + (fortLevel * 75);
                        Program.Logger.Debug($"Attacker won. Updating siege progress for SiegeID {SiegeID} to {newProgress} (based on fort level {fortLevel}).");
                        updatedContent.AppendLine($"{line.Substring(0, line.IndexOf("progress="))}progress={newProgress.ToString("F2", CultureInfo.InvariantCulture)}");
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
                            Program.Logger.Debug($"Updating breach from {currentBreach} to {newBreach} (increment: {breachIncrement}).");
                            updatedContent.AppendLine($"{line.Substring(0, line.IndexOf("breach="))}breach={newBreach}");
                        }
                        else
                        {
                            Program.Logger.Debug($"Warning: Could not parse current breach value from line: '{line}'. Appending original line.");
                            updatedContent.AppendLine(line);
                        }
                    }
                    else
                    {
                        updatedContent.AppendLine(line); // No increment, append original line
                    }
                }
                else if (inTargetSiegeBlock && trimmedLine.StartsWith("action_history={")) // MODIFIED BLOCK: Update action_history
                {
                    if (breachIncrement > 0 && i + 2 < fileLines.Count) // Ensure there are enough lines to read (opening, tokens, closing)
                    {
                        string tokensLineContent = fileLines[i + 1].Trim(); // e.g., "none none none starvation"
                        string tokensLineIndentation = fileLines[i + 1].Substring(0, fileLines[i + 1].IndexOf(tokensLineContent)); // Get indentation of tokens line
                        string closingBraceIndentation = fileLines[i + 2].Substring(0, fileLines[i + 2].IndexOf('}')); // Get indentation of closing brace line

                        List<string> tokens = tokensLineContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
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
                        Program.Logger.Debug($"Updating action_history from '{{ {originalTokensString} }}' to '{{ {newTokensString} }}' (increment: {breachIncrement}).");

                        updatedContent.AppendLine(line); // Append "action_history={"
                        updatedContent.AppendLine($"{tokensLineIndentation}{newTokensString}"); // Append modified tokens line
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
                Program.Logger.Debug($"Sieges.txt for siege ID {SiegeID} was read, but the target siege block was not found.");
            }
        }
    }
}
