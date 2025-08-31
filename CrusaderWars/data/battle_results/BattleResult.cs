using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Text;
using CrusaderWars.data.save_file;
using static CrusaderWars.data.save_file.Writter;


namespace CrusaderWars
{
    public static class BattleResult
    {

        public static string CombatID { get; set; }
        public static string ResultID { get; set; }
        public static string ProvinceID { get; set; }
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
                        string line = sr.ReadLine();
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
                        string line = sr.ReadLine();
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
                string battle_id="";
                StringBuilder f = new StringBuilder();
                using(StreamReader sr = new StreamReader(@".\data\save_file_data\BattleResults.txt"))
                {
                    while(!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line == null) break;
                        if (Regex.IsMatch(line, @"\t\t\d+={"))
                        {
                            battle_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;
                        }
                        else if (line == $"\t\t\tlocation={ProvinceID}")
                        {
                            break;
                        }
                    }

                    sr.BaseStream.Position = 0;
                    sr.DiscardBufferedData();

                    bool isSearchStarted = false;
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
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


        public static void SendToSaveFile(string filePath)
        {
            Program.Logger.Debug($"Sending battle results to save file: {filePath}");
            Writter.SendDataToFile(filePath);
            Program.Logger.Debug("Resetting data and collecting garbage.");
            Data.Reset();
            Player_Combat = "";
            GC.Collect();
        }





        //---------------------------------//
        //----------Functions--------------//
        //---------------------------------//
        public static (List<string> AliveList, List<string> KillsList) GetRemainingAndKills(string path_attila_log)
        {
            Program.Logger.Debug($"Entering GetRemainingAndKills for log file: {path_attila_log}");
            string aliveText = "";
            string killsText = "";

            bool aliveSearchStarted = false;
            bool killsSearchStarted = false;

            List<string> alive_list = new List<string>();
            List<string> kills_list = new List<string>();

            using (StreamReader reader = new StreamReader(path_attila_log))
            {
                string line;
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
                        alive_list.Add(aliveText);
                        kills_list.Add(killsText);
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
            Program.Logger.Debug($"Found {alive_list.Count} alive reports and {kills_list.Count} kill reports.");
            return (alive_list, kills_list);
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
                List<(string Script, string Type, string CultureID, string Kills)> Kills_PursuitPhase = new List<(string Name, string Type, string CultureID, string Kills)>();

                var (AliveList, KillsList) = GetRemainingAndKills(path_attila_log);
                if (AliveList.Count == 0)
                {
                    Program.Logger.Debug($"Warning: No battle reports found in Attila log for army {army.ID}. Assuming no survivors or battle did not generate logs.");
                }
                else if (AliveList.Count == 1)
                {
                    Program.Logger.Debug($"Single battle phase detected for army {army.ID}.");
                    Alive_MainPhase = ReturnList(army, AliveList[0], DataType.Alive);
                    units.SetAliveMainPhase(Alive_MainPhase);
                    Kills_MainPhase = ReturnList(army, KillsList[0], DataType.Kills);
                    units.SetKillsMainPhase(Kills_MainPhase);

                }
                else if (AliveList.Count > 1)
                {
                    Program.Logger.Debug($"Multiple battle phases detected for army {army.ID}. (Main and Pursuit)");
                    Alive_MainPhase = ReturnList(army, AliveList[0], DataType.Alive);
                    units.SetAliveMainPhase(Alive_MainPhase);

                    Alive_PursuitPhase = ReturnList(army, AliveList[1], DataType.Alive);
                    units.SetAlivePursuitPhase(Alive_PursuitPhase);

                    Kills_MainPhase = ReturnList(army, KillsList[0], DataType.Kills);
                    units.SetKillsMainPhase(Kills_MainPhase);

                    Kills_PursuitPhase = ReturnList(army, KillsList[1], DataType.Kills);
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
                MessageBox.Show($"Error reading Attila results: {e.ToString()}", "Crusader Wars: Battle Results Error",
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
            foreach(ArmyRegiment armyRegiment in army.ArmyRegiments)
            {
                if (armyRegiment.Type == data.save_file.RegimentType.Commander || armyRegiment.Type == data.save_file.RegimentType.Knight) continue;

                foreach (Regiment regiment in armyRegiment.Regiments)
                {
                    if(regiment.Culture is null ) continue; // skip siege maa

                    var unitReport = army.CasualitiesReports.FirstOrDefault(x => x.GetUnitType() == armyRegiment.Type && x.GetCulture().ID == regiment.Culture.ID && x.GetTypeName() == armyRegiment.MAA_Name);
                    if (unitReport == null)
                        continue;

                    int killed = unitReport.GetKilled();
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
                    Program.Logger.Debug($"Regiment {regiment.ID} (Type: {armyRegiment.Type}, Culture: {regiment.Culture.ID}): Soldiers changed from {originalSoldiers} to {regSoldiers}.");


                    int army_regiment_total = armyRegiment.Regiments.Where(reg => reg.CurrentNum != null).Sum(x => Int32.Parse(x.CurrentNum));
                    armyRegiment.SetCurrentNum(army_regiment_total.ToString());

                }
            }
        }

        static void CreateUnitsReports(Army army)
        {
            Program.Logger.Debug($"Entering CreateUnitsReports for army {army.ID}.");
            List<UnitCasualitiesReport> reportsList = new List<UnitCasualitiesReport>();

            // Group by Type and CultureID
            var grouped = army.UnitsResults.Alive_MainPhase.GroupBy(item => new { item.Type, item.CultureID });
            Program.Logger.Debug($"Found {grouped.Count()} unit groups for army {army.ID}.");
            var pursuit_grouped = army.UnitsResults.Alive_PursuitPhase?.GroupBy(item => new { item.Type, item.CultureID });

            Program.Logger.Debug("#############################");
            Program.Logger.Debug($"REPORT FROM {army.CombatSide.ToUpper()} ARMY {army.ID}");
            foreach(var group in grouped)
            {
                // Set the regiment type to the correct one
                RegimentType unitType;
                if (group.Key.Type.Contains("Levy")) { unitType = RegimentType.Levy; }
                else if (group.Key.Type.Contains("commander") || group.Key.Type == "knights") { continue; }
                else { unitType = RegimentType.MenAtArms; }

                // Search for type, culture, starting soldiers and remaining soldiers of a Unit
                if(army.Units == null) { continue; }
                string type = Regex.Match(group.Key.Type, @"\D+").Value;
                // Safely get the unit, then its culture. If unit is null, culture will be null.
                // The warning CS8600 is because GetObjCulture() is called on a potentially null result of FirstOrDefault().
                var matchingUnit = army.Units.FirstOrDefault(x => x.GetRegimentType() == unitType && x.GetObjCulture()?.ID == group.Key.CultureID && x.GetName() == type);
                Culture? culture = matchingUnit?.GetObjCulture(); // CHANGE THIS LINE: Add '?' to make Culture nullable
                
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
                if (pursuit_grouped != null)
                {
                    var pursuitGroup = pursuit_grouped.FirstOrDefault(x => x.Key.Type == group.Key.Type && x.Key.CultureID == group.Key.CultureID);
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
                int remaining = 0;
                var knightReport = army.UnitsResults.Alive_PursuitPhase.FirstOrDefault(x => x.Type == "knights");

                // If no report in pursuit phase, check main phase
                if (knightReport.Remaining == null)
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
                var knightKillsReport = army.UnitsResults.Kills_MainPhase.FirstOrDefault(x => x.Type == "knights");
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
                if (army.Commander != null && army.Commander.ID == char_id)
                {
                    Program.Logger.Debug($"Commander {char_id} found in army {army.ID}.");
                    return (true, true, army.Commander, false, null);
                }
                else if (army.Knights.GetKnightsList() != null)
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
                        if (mergedArmy.Commander != null && mergedArmy.Commander.ID == char_id)
                        {
                            Program.Logger.Debug($"Commander {char_id} found in merged army {mergedArmy.ID} (part of main army {army.ID}).");
                            return (true, true, army.Commander, false, null);
                        }
                        else if (mergedArmy.Knights.GetKnightsList() != null)
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

                CommanderSystem commander = null;
                Knight knight = null;


                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if(!searchStarted && Regex.IsMatch(line, @"\d+={"))
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
                        if (isCommander && commander.hasFallen)
                        {
                            edited_line = commander.Health(edited_line);
                            Program.Logger.Debug($"Applying wounds to fallen commander {commander.ID}.");
                        }
                        else if(isKnight)
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

                string regimentType = "";
                string knightID = "";

                string line;
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
                            Program.Logger.Debug($"Attacker: Detected regiment type: {regimentType}");
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
                                Program.Logger.Debug($"Attacker: Detected Men-at-Arms regiment (ID: {id}).");
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
                            foreach(Army army in attacker_armies)
                            {
                                main_kills += army.UnitsResults.GetKillsAmountOfMainPhase(regimentType);
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
                                if(army.Knights != null && army.Knights.HasKnights())
                                {
                                    if(army.Knights.GetKnightsList().Exists(knight => knight.GetID() == knightID))
                                    {
                                        main_kills = army.Knights.GetKnightsList().FirstOrDefault(knight => knight.GetID() == knightID).GetKills();
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
                                pursuit_kills += army.UnitsResults.GetKillsAmountOfPursuitPhase(regimentType);
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
                                main_losses += army.UnitsResults.GetDeathAmountOfMainPhase(army.CasualitiesReports,regimentType);
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
                                pursuit_losses += army.UnitsResults.GetDeathAmountOfPursuitPhase(army.CasualitiesReports, regimentType);
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
                                main_kills += army.UnitsResults.GetKillsAmountOfMainPhase(regimentType);
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
                                if (army.Knights != null && army.Knights.HasKnights())
                                {
                                    if (army.Knights.GetKnightsList().Exists(knight => knight.GetID() == knightID))
                                    {
                                        main_kills = army.Knights.GetKnightsList().FirstOrDefault(knight => knight.GetID() == knightID).GetKills();
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
                                pursuit_kills += army.UnitsResults.GetKillsAmountOfPursuitPhase(regimentType);
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
                                main_losses += army.UnitsResults.GetDeathAmountOfMainPhase(army.CasualitiesReports, regimentType);
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
                                pursuit_losses += army.UnitsResults.GetDeathAmountOfPursuitPhase(army.CasualitiesReports, regimentType);
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

                string line;
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
                            string currentNum = (SearchArmyRegiment(attacker_armies, army_regiment_id)?.CurrentNum)?.ToString() ?? "0";
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
                            string currentNum = (SearchArmyRegiment(defender_armies, army_regiment_id)?.CurrentNum)?.ToString() ?? "0";
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
            int total = 0;
            foreach(Army army in armies)
            {
                total += army.ArmyRegiments.Sum(x => x.CurrentNum);

            }
            Program.Logger.Debug($"Calculated total fighting men for armies: {total}");
            return total;
        }

        static int GetArmiesTotalLevyMen(List<Army> armies)
        {
            int total = 0;
            foreach (Army army in armies)
            {
                total += army.ArmyRegiments.Where(y => y.Type == RegimentType.Levy).Sum(x => x.CurrentNum);

            }
            Program.Logger.Debug($"Calculated total levy men for armies: {total}");
            return total;
        }

        static ArmyRegiment SearchArmyRegiment(List<Army> armies, string army_regiment_id)
        {
            Program.Logger.Debug($"Searching for ArmyRegiment ID: {army_regiment_id}");
            foreach (Army army in armies)
            {
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment.Type == RegimentType.Knight) continue;
                    if (armyRegiment.ID == army_regiment_id)
                    {
                        Program.Logger.Debug($"Found ArmyRegiment {army_regiment_id} in army {army.ID}.");
                        return armyRegiment;
                    }
                }
            }
            Program.Logger.Debug($"ArmyRegiment ID: {army_regiment_id} not found.");
            return null;
        }

        static void SetWinner(string winner)
        {
            Program.Logger.Debug($"Setting battle winner to: {winner}");
            try
            {
                //Set pursuit phase
                Player_Combat = Regex.Replace(Player_Combat, @"(phase=)\w+", "$1" + "pursuit");

                //Set last day of phase
                Player_Combat = Regex.Replace(Player_Combat, @"(days=\d+)", "days=3\n\t\t\twiped=no");

                //Set winner
                Player_Combat = Regex.Replace(Player_Combat, @"(base_combat_width=\d+)", "$1\n\t\t\twinning_side=" + winner);

                Player_Combat = Player_Combat.Replace("\r", "");

                File.WriteAllText(Writter.DataFilesPaths.Combats_Path(), Player_Combat);

                Program.Logger.Debug("Winner of battle set successfully");
            }
            catch(Exception ex)
            {
                Program.Logger.Debug($"Error setting winner of battle: {ex.Message}");
            }

        }

        //Get winner from Attila
        static string GetAttilaWinner(string path_attila_log, string player_armies_combat_side, string enemy_armies_combat_side)
        {
            Program.Logger.Debug($"Entering GetAttilaWinner for log file: {path_attila_log}");
            string winner = "";
            using (FileStream logFile = File.Open(path_attila_log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(logFile))
            {
                string line;
                //winning_side=attacker/defender
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("Victory")) { winner = player_armies_combat_side; break; }
                    else if (line.Contains("Defeat")) { winner = enemy_armies_combat_side; break; }
                    else winner = enemy_armies_combat_side;
                }

                reader.Close();
                logFile.Close();
                Program.Logger.Debug($"Determined Attila winner: {winner}");
                return winner;
            }
        }

        public static void EditArmyRegimentsFile(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Editing Army Regiments file...");
            bool editStarted = false;
            ArmyRegiment editArmyRegiment = null;

            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.ArmyRegiments_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.ArmyRegiments_Path()))
            {
                streamWriter.NewLine = "\n";

                string line;
                while ((line = streamReader.ReadLine()) != null || !streamReader.EndOfStream)
                {

                    //Regiment ID line
                    if (!editStarted && Regex.IsMatch(line, @"\t\t\d+={"))
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

                    else if (editStarted == true && line.Contains("\t\t\t\tcurrent="))
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
            Program.Logger.Debug($"Searching for ArmyRegiment ID: {army_regiment_id} in ArmyRegiments file.");
            bool editStarted = false;
            ArmyRegiment? editRegiment = null;

            foreach (Army army in armies)
            {
                foreach (ArmyRegiment army_regiment in army.ArmyRegiments)
                {
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
            Program.Logger.Debug($"ArmyRegiment ID: {army_regiment_id} not found in ArmyRegiments file.");
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
            Regiment editRegiment = null;

            int index = -1;
            bool isNewData = false;
            

            using (StreamReader streamReader = new StreamReader(Writter.DataFilesPaths.Regiments_Path()))
            using (StreamWriter streamWriter = new StreamWriter(Writter.DataTEMPFilesPaths.Regiments_Path()))
            { 
                streamWriter.NewLine = "\n";

                string line;
                while((line = streamReader.ReadLine()) != null || !streamReader.EndOfStream)
                {

                    //Regiment ID line
                    if(!editStarted && Regex.IsMatch(line, @"\t\t\d+={"))
                    {
                        string regiment_id = Regex.Match(line, @"\d+").Value;


                        var searchingData = SearchRegimentsFile(attacker_armies,regiment_id);
                        if(searchingData.editStarted)
                        {
                            editStarted = true;
                            editRegiment = searchingData.editRegiment;
                            Program.Logger.Debug($"Found Regiment {regiment_id} for editing (Attacker).");
                        }
                        else
                        {
                            searchingData = SearchRegimentsFile(defender_armies,regiment_id);
                            if(searchingData.editStarted)
                            {
                                editStarted = true;
                                editRegiment = searchingData.editRegiment;
                                Program.Logger.Debug($"Found Regiment {regiment_id} for editing (Defender).");
                            }
                        }

                    }

                    else if(editStarted && line.Contains("\t\t\tsize="))
                    {
                        isNewData = true;
                        string newLine = GetChunksText((editRegiment?.Max)?.ToString() ?? "0", editRegiment?.Owner ?? "", (editRegiment?.CurrentNum)?.ToString() ?? "0");
                        streamWriter.WriteLine(newLine);
                        Program.Logger.Debug($"Regiment {editRegiment?.ID}: Writing new data format with current soldiers {editRegiment?.CurrentNum ?? "0"}.");
                        continue;
                    }

                    //Index Counter
                    else if(!isNewData && editStarted && line == "\t\t\t\t{")
                    {
                        index++;
                        if (editRegiment.Index == "") 
                            editRegiment.ChangeIndex(0.ToString());
                        if(index.ToString() == editRegiment.Index)
                        {
                            editIndex = true;
                        }
                    }

                    else if(!isNewData && (editStarted==true && editIndex==true) && line.Contains("\t\t\t\t\tcurrent="))
                    {
                        string currentNum = (editRegiment?.CurrentNum)?.ToString() ?? "0";
                        string edited_line = "\t\t\t\t\tcurrent=" + currentNum;
                        streamWriter.WriteLine(edited_line);
                        Program.Logger.Debug($"Regiment {editRegiment?.ID}: Updating old data format with current soldiers {currentNum}.");
                        continue;
                    }

                    //End Line
                    else if(editStarted && line == "\t\t}")
                    {
                        editStarted = false; editRegiment = null; editIndex = false; index = -1; isNewData = false;
                    }

                    if(!isNewData)
                    {
                        streamWriter.WriteLine(line);
                    }
                    
                }
            }
            Program.Logger.Debug("Finished editing Regiments file.");
        }

        static (bool editStarted, Regiment? editRegiment) SearchRegimentsFile(List<Army> armies, string regiment_id)
        {
            Program.Logger.Debug($"Searching for Regiment ID: {regiment_id} in Regiments file.");
            bool editStarted = false;
            Regiment? editRegiment = null;

            foreach (Regiment regiment in armies?.SelectMany(army => army.ArmyRegiments)?.SelectMany(armyRegiment => armyRegiment.Regiments))
            {
                if (regiment.ID == regiment_id)
                {
                    editStarted = true;
                    editRegiment = regiment;
                    Program.Logger.Debug($"Found Regiment {regiment_id}.");
                    return (editStarted, editRegiment);
                }
            }
            // Program.Logger.Debug($"Regiment ID: {regiment_id} not found in Regiments file.");
            return (false, null);
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

    }
}
