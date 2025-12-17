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





        static void SetWinner(string winner)
        {
            if (string.IsNullOrEmpty(Player_Combat)) return;

            // Set winner
            Player_Combat = Regex.Replace(Player_Combat, @"(winner=)\w+", $"${{1}}{winner}");

            // Set phase to aftermath
            Player_Combat = Regex.Replace(Player_Combat, @"(phase=)\w+", "${1}aftermath");
            
            Program.Logger.Debug($"Set combat winner to '{winner}' and phase to 'aftermath'.");
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

    }
}
