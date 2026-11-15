using System.IO;
using System.Collections.Generic; // Added for List<string>
using System.Text.Json; // Added for JSON serialization
using System.Linq; // Added for Enumerable.Any in legacy state migration

namespace CrusaderWars.twbattle
{
    public static class BattleState
    {
        private static string StateFolder => @".\data\tw_battle";
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
        /// <summary>True once the Total War game has crashed at least once during the current battle.
        /// Used to gate post-battle popups (e.g. "manual tool settings" report) behind an actual crash.</summary>
        public static bool BattleCrashedAtLeastOnce { get; set; } = false;

        // Serializable classes for JSON persistence
        public class ReplacementEntry
        {
            public string OriginalKey { get; set; } = string.Empty;
            public bool IsPlayerAlliance { get; set; }
            public string ReplacementKey { get; set; } = string.Empty;
            public bool IsSiege { get; set; }
        }

        public class PersistentSettings
        {
            public List<ReplacementEntry> ManualUnitReplacements { get; set; } = new List<ReplacementEntry>();
            public ZoneOverride? DeploymentZoneOverrideAttacker { get; set; }
            public ZoneOverride? DeploymentZoneOverrideDefender { get; set; }
        }

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
            // One-time migration: the state folder was previously named 'attila_battle'.
            // Move any existing state files into the new 'tw_battle' folder so an in-progress
            // battle (or saved manual replacements) survives the rename.
            MigrateLegacyStateFolder();

            // Ensure directory exists
            if (!Directory.Exists(StateFolder))
            {
                Program.Logger.Debug($"BattleState: State folder not found at '{StateFolder}'. Creating it.");
                Directory.CreateDirectory(StateFolder);
            }

            // Load persistent settings on startup
            LoadPersistentBattleSettings();
        }

        /// <summary>Moves the legacy 'data\attila_battle' folder contents into 'data\tw_battle'.
        /// No-op when the legacy folder is absent. Existing files in the new folder are kept
        /// (the legacy file is only moved when the destination does not already exist).</summary>
        private static void MigrateLegacyStateFolder()
        {
            const string legacyFolder = @".\data\attila_battle";
            if (!Directory.Exists(legacyFolder)) return;

            try
            {
                Directory.CreateDirectory(StateFolder);
                foreach (var file in Directory.GetFiles(legacyFolder))
                {
                    string dest = Path.Combine(StateFolder, Path.GetFileName(file));
                    if (File.Exists(dest)) continue; // never clobber newer state
                    File.Move(file, dest);
                }
                // Remove the legacy folder if it is now empty.
                if (!Directory.EnumerateFileSystemEntries(legacyFolder).Any())
                    Directory.Delete(legacyFolder);
                Program.Logger.Debug("BattleState: migrated legacy 'attila_battle' state folder to 'tw_battle'.");
            }
            catch (Exception ex)
            {
                // Migration failure must never block startup; the new folder is created below.
                Program.Logger.Debug($"BattleState: legacy state folder migration failed: {ex.Message}");
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
            // Note: Not clearing ManualUnitReplacements or DeploymentZoneOverrides as they are persistent.
        }

        public static void ClearBattleState()
        {
            Program.Logger.Debug("Clearing battle state...");
            IsSiegeBattle = false;
            BattleType = null;
            HasReliefArmy = false;
            BattleCrashedAtLeastOnce = false; // Reset crash flag for the next battle
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

        // Methods for handling persistent battle settings
        public static void SavePersistentBattleSettings()
        {
            try
            {
                var serializableReplacements = ManualUnitReplacements.Select(kvp => new ReplacementEntry
                {
                    OriginalKey = kvp.Key.originalKey,
                    IsPlayerAlliance = kvp.Key.isPlayerAlliance,
                    ReplacementKey = kvp.Value.replacementKey,
                    IsSiege = kvp.Value.isSiege
                }).ToList();

                var settings = new PersistentSettings
                {
                    ManualUnitReplacements = serializableReplacements,
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
                Program.Logger.Debug($"Error saving persistent battle settings: {ex.Message}");
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
                    var settings = JsonSerializer.Deserialize<PersistentSettings>(jsonString);

                    if (settings != null)
                    {
                        ManualUnitReplacements.Clear();
                        if (settings.ManualUnitReplacements != null)
                        {
                            foreach (var entry in settings.ManualUnitReplacements)
                            {
                                var key = (entry.OriginalKey, entry.IsPlayerAlliance);
                                var value = (entry.ReplacementKey, entry.IsSiege);
                                ManualUnitReplacements[key] = value;
                            }
                        }

                        DeploymentZoneOverrideAttacker = settings.DeploymentZoneOverrideAttacker;
                        DeploymentZoneOverrideDefender = settings.DeploymentZoneOverrideDefender;

                        Program.Logger.Debug($"Loaded persistent battle settings from: '{settingsFilePath}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error loading persistent battle settings: {ex.Message}");
                ManualUnitReplacements.Clear();
                DeploymentZoneOverrideAttacker = null;
                DeploymentZoneOverrideDefender = null;
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
