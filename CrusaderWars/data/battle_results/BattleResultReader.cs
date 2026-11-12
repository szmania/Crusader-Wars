using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using CrusaderWars.twbattle; // Added for BattleFile access
using System.Globalization; // Added for CultureInfo

namespace CrusaderWars.data.battle_results
{
    public static class BattleResultReader
    {
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
            // Resolve the log path for whichever Total War game this battle uses
            // (Attila or Rome 2) via the game service instead of hardcoding Attila.
            string gameType = CrusaderWars.unit_mapper.UnitMappers_BETA.ResolvedBattleGameType;
            var gameService = CrusaderWars.games.GameServiceFactory.GetGameService(gameType);
            string logPath = gameService.GetLogPath();

            // Persist the resolved path into the matching setting so ModOptions.GetLogPath
            // and other readers observe the same value for the active game.
            if (gameType == "Rome2")
                Properties.Settings.Default.VAR_log_rome2 = logPath;
            else
                Properties.Settings.Default.VAR_log_attila = logPath;
            Properties.Settings.Default.Save();

            Program.Logger.Debug($"{gameType} log file path to clear: {logPath}");

            if (string.IsNullOrEmpty(logPath))
            {
                Program.Logger.Debug($"{gameType} log path is not configured. Skipping log clear.");
                return;
            }

            using (FileStream logFile = File.Open(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                logFile.Close();
            }
            Program.Logger.Debug($"{gameType} log file cleared successfully.");
        }
    }
}
