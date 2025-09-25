using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization; // Added for CultureInfo
using CrusaderWars.twbattle; // Assuming this is needed for Sieges.GetSiegeProgress etc.
using CrusaderWars.armies; // Assuming this is needed for List<Army>

namespace CrusaderWars.data.save_file
{
    public static class BattleResult
    {
        // Placeholder for SiegeID and CombatID, which would normally be set by DataSearch
        public static string? SiegeID { get; set; }
        public static string? CombatID { get; set; }
        public static string? ProvinceID { get; set; }
        public static string? ProvinceName { get; set; }
        public static string? Player_Combat { get; set; }

        // Placeholder for battle outcome. In a real scenario, this would be set by parsing Attila logs.
        // For this task, we assume this is set by other BattleResult methods before EditSiegesFile is called.
        public static bool IsAttackerVictorious { get; set; } = false; 

        // Placeholder for other methods that would exist in BattleResult.cs
        public static void GetPlayerCombatResult() { }
        public static void ReadPlayerCombat(string commanderId) { }
        public static bool HasBattleEnded(string attilaLogPath) { return true; } // Placeholder
        public static void ReadAttilaResults(Army army, string path_log_attila) { }
        public static void CheckForDeathCommanders(Army army, string path_log_attila) { }
        public static void CheckKnightsKills(Army army) { }
        public static void CheckForDeathKnights(Army army) { }
        public static void EditLivingFile(List<Army> attacker_armies, List<Army> defender_armies) { }
        public static void EditCombatFile(List<Army> attacker_armies, List<Army> defender_armies, string attacker_side, string defender_side, string path_log_attila) { }
        public static void EditCombatResultsFile(List<Army> attacker_armies, List<Army> defender_armies) { }
        public static void EditRegimentsFile(List<Army> attacker_armies, List<Army> defender_armies) { }
        public static void EditArmyRegimentsFile(List<Army> attacker_armies, List<Army> defender_armies) { }
        public static void SendToSaveFile(string path_editedSave) { }
        public static void ClearAttilaLog() { }
        public static void LogPostBattleReport(List<Army> armies, Dictionary<string, int> originalSizes, string side) { }


        public static void EditSiegesFile(string path_log_attila, string attacker_side, string defender_side, List<Army> defender_armies)
        {
            Program.Logger.Debug("Starting EditSiegesFile for siege battle.");

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

            List<string> fileLines = File.ReadAllLines(siegesFilePath).ToList();
            StringBuilder updatedContent = new StringBuilder();
            bool inTargetSiegeBlock = false;
            bool targetSiegeBlockFound = false; // Track if we entered the target siege block at all

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
                    updatedContent.AppendLine(line);
                }
                else if (inTargetSiegeBlock && trimmedLine == "}")
                {
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
                    else
                    {
                        // Attacker won or other scenario.
                        // The plan doesn't specify changes for this, so we'll just write back the original line.
                        // In a full implementation, this would be where attacker victory logic updates progress.
                        Program.Logger.Debug("Attacker won or other scenario. Siege progress handled by existing logic (or remains unchanged).");
                        updatedContent.AppendLine(line); // Append original line
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
                File.WriteAllText(siegesFilePath, updatedContent.ToString());
                Program.Logger.Debug($"Sieges.txt updated for siege ID {SiegeID}.");
            }
            else
            {
                Program.Logger.Debug($"Sieges.txt for siege ID {SiegeID} was read, but the target siege block was not found.");
            }
        }
    }
}
