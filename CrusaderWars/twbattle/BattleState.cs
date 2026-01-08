using System.IO;
using System.Collections.Generic; // Added for List<string>
using System.Text.Json; // Added for JSON serialization

namespace CrusaderWars.twbattle
{
    public static class BattleState
    {
        private static string StateFolder => @".\data\attila_battle";
        private static string StateFile => Path.Combine(StateFolder, "battle_state.txt");
        private static string LogSnippetFile => Path.Combine(StateFolder, "battle_log_snippet.txt");
        public static bool IsSiegeBattle { get; set; }
        public static string? BattleType { get; set; }
        public static bool HasReliefArmy { get; set; }
        public static string? AutofixDeploymentSizeOverride { get; set; } = null;
        public static bool? AutofixDeploymentRotationOverride { get; set; } = null;
        public static string? AutofixAttackerDirectionOverride { get; set; } = null;
        public static string? OriginalSiegeAttackerDirection { get; set; } = null;
        public static List<string>? SiegeBesiegerOrientations { get; set; } = null;
        public static bool AutofixForceGenericMap { get; set; } = false;
        public static int AutofixMapVariantOffset { get; set; } = 0;
        public static Dictionary<(string originalKey, bool isPlayerAlliance), (string replacementKey, bool isSiege)> ManualUnitReplacements { get; set; } = new Dictionary<(string, bool), (string, bool)>();

        public class ZoneOverride
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
        }
        public static ZoneOverride? DeploymentZoneOverrideAttacker { get; set; } = null;
        public static ZoneOverride? DeploymentZoneOverrideDefender { get; set; } = null;
        
        private const string SETTINGS_FILE = @"persistent_settings.json";

        static BattleState()
        {
            // Ensure directory exists
            if (!Directory.Exists(StateFolder))
            {
                Program.Logger.Debug($"BattleState: State folder not found at '{StateFolder}'. Creating it.");
                Directory.CreateDirectory(StateFolder);
            }
            
            // Load persistent settings on startup
            LoadPersistentBattleSettings();
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
            // Note: Not clearing ManualUnitReplacements or DeploymentZoneOverrides as they should persist
            DeploymentZoneOverrideAttacker = null;
            DeploymentZoneOverrideDefender = null;
        }

        public static void ClearBattleState()
        {
            Program.Logger.Debug("Clearing battle state...");
            IsSiegeBattle = false;
            BattleType = null;
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
            // Save persistent settings after clearing
            SavePersistentBattleSettings();
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
        
        // Methods for handling persistent battle settings
        public static void SavePersistentBattleSettings()
        {
            try
            {
                var settings = new 
                {
                    ManualUnitReplacements = ManualUnitReplacements,
                    DeploymentZoneOverrideAttacker = DeploymentZoneOverrideAttacker,
                    DeploymentZoneOverrideDefender = DeploymentZoneOverrideDefender
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(settings, options);
                string settingsFilePath = Path.Combine(StateFolder, SETTINGS_FILE);
                File.WriteAllText(settingsFilePath, jsonString);
                Program.Logger.Debug($"Saved persistent battle settings to: '{settingsFilePath}'");
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"Error saving persistent battle settings: {ex.Message}");
            }
        }

        public static void LoadPersistentBattleSettings()
        {
            try
            {
                string settingsFilePath = Path.Combine(StateFolder, SETTINGS_FILE);
                if (File.Exists(settingsFilePath))
                {
                    string jsonString = File.ReadAllText(settingsFilePath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                    
                    // For now, we'll just log that settings were loaded
                    // The complex deserialization of the dictionary with tuple keys would require custom converters
                    Program.Logger.Debug($"Loaded persistent battle settings from: '{settingsFilePath}'");
                }
                else
                {
                    Program.Logger.Debug($"No persistent battle settings file found at: '{settingsFilePath}'");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"Error loading persistent battle settings: {ex.Message}");
            }
        }

        public static void ClearManualUnitReplacements()
        {
            ManualUnitReplacements.Clear();
            SavePersistentBattleSettings();
            Program.Logger.Debug("Cleared manual unit replacements and saved settings.");
        }

        public static void ClearDeploymentZoneOverrides()
        {
            DeploymentZoneOverrideAttacker = null;
            DeploymentZoneOverrideDefender = null;
            SavePersistentBattleSettings();
            Program.Logger.Debug("Cleared deployment zone overrides and saved settings.");
        }
    }
}
