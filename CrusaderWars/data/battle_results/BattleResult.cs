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
                            editRegiment = searchingData.editRegiment;
                            parentArmyRegiment = searchingData.parentArmyRegiment; // Store parent ArmyRegiment
                            Program.Logger.Debug($"Found Regiment {regiment_id} for editing (Attacker).");
                        }
                        else
                        {
                            searchingData = SearchRegimentsFile(defender_armies, regiment_id);
                            if (searchingData.editStarted)
                            {
                                editStarted = true;
                                editRegiment = searchingData.editRegiment;
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
    }
}
