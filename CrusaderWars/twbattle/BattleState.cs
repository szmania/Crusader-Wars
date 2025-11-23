using System.IO;
using System.Collections.Generic; // Added for List<string>

namespace CrusaderWars.twbattle
{
    public static class BattleState
    {
        private static string StateFolder => @".\data\attila_battle";
        private static string StateFile => Path.Combine(StateFolder, "battle_state.txt");
        private static string LogSnippetFile => Path.Combine(StateFolder, "battle_log_snippet.txt");
        public static bool IsSiegeBattle { get; set; }
        public static bool HasReliefArmy { get; set; }
        public static string? AutofixDeploymentSizeOverride { get; set; } = null;
        public static bool? AutofixDeploymentRotationOverride { get; set; } = null;
        public static string? AutofixAttackerDirectionOverride { get; set; } = null;
        public static string? OriginalSiegeAttackerDirection { get; set; } = null;
        public static List<string>? SiegeBesiegerOrientations { get; set; } = null;
        public static bool AutofixForceGenericMap { get; set; } = false;
        public static int AutofixMapVariantOffset { get; set; } = 0;
        public static Dictionary<string, (string replacementKey, bool isSiege)> ManualUnitReplacements { get; set; } = new Dictionary<string, (string, bool)>();

        static BattleState()
        {
            // Ensure directory exists
            if (!Directory.Exists(StateFolder))
            {
                Program.Logger.Debug($"BattleState: State folder not found at '{StateFolder}'. Creating it.");
                Directory.CreateDirectory(StateFolder);
            }
        }

        public static bool IsBattleInProgress()
        {
            bool battleInProgress = System.IO.File.Exists(StateFile);
            return battleInProgress;
        }

        public static void MarkBattleStarted()
        {
            if (!System.IO.File.Exists(StateFile))
            {
                Program.Logger.Debug($"Marking battle as started. Creating state file: '{StateFile}'");
                System.IO.File.WriteAllText(StateFile, "battle_in_progress");
            }
            else
            {
                Program.Logger.Debug($"Battle already marked as started. State file exists: '{StateFile}'");
            }
        }

        public static void ClearAutofixOverrides()
        {
            Program.Logger.Debug("Clearing autofix deployment overrides.");
            AutofixDeploymentSizeOverride = null;
            AutofixDeploymentRotationOverride = null;
            AutofixAttackerDirectionOverride = null;
            OriginalSiegeAttackerDirection = null;
            SiegeBesiegerOrientations = null;
            AutofixForceGenericMap = false;
            AutofixMapVariantOffset = 0;
            ManualUnitReplacements.Clear();
        }

        public static void ClearBattleState()
        {
            Program.Logger.Debug("Clearing battle state...");
            IsSiegeBattle = false;
            HasReliefArmy = false;
            ClearAutofixOverrides(); // Reset autofix overrides
            if (System.IO.File.Exists(StateFile))
            {
                Program.Logger.Debug($"Deleting battle state file: '{StateFile}'");
                System.IO.File.Delete(StateFile);
            }
            if (System.IO.File.Exists(LogSnippetFile))
            {
                string backupLogSnippetFile = LogSnippetFile + ".bak";
                Program.Logger.Debug($"Backing up battle log snippet to: '{backupLogSnippetFile}'");
                System.IO.File.Copy(LogSnippetFile, backupLogSnippetFile, true);
                Program.Logger.Debug($"Deleting battle log snippet file: '{LogSnippetFile}'");
                System.IO.File.Delete(LogSnippetFile);
            }
            Program.Logger.Debug("Battle state cleared.");
        }

        public static void SaveLogSnippet(string logContent)
        {
            Program.Logger.Debug($"Saving battle log snippet to: '{LogSnippetFile}'");
            System.IO.File.WriteAllText(LogSnippetFile, logContent);
            Program.Logger.Debug($"Saved battle log snippet.");
        }

        public static string? LoadLogSnippet()
        {
            if (System.IO.File.Exists(LogSnippetFile))
            {
                Program.Logger.Debug($"Loading battle log snippet from: '{LogSnippetFile}'");
                string content = System.IO.File.ReadAllText(LogSnippetFile);
                Program.Logger.Debug("Successfully loaded battle log snippet.");
                return content;
            }
            else
            {
                Program.Logger.Debug($"Battle log snippet not found at: '{LogSnippetFile}'");
                return null;
            }
        }
    }
}
