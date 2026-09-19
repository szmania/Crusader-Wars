using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using CrusaderWars.Tests.TestHelpers;

namespace CrusaderWars.Tests.Validation
{
    /// <summary>
    /// Tests for the compatibility patch validation logic found in MainFile.cs ExecuteButton_Click.
    /// These tests simulate the dlc_load.json parsing and patch validation without requiring
    /// the full application or CK3/Attila to be installed.
    /// </summary>
    public class CompatibilityPatchValidationTests : IDisposable
    {
        private readonly TempTestEnvironment _env;

        public CompatibilityPatchValidationTests()
        {
            _env = new TempTestEnvironment();
        }

        /// <summary>
        /// Helper that simulates the dlc_load.json parsing logic from MainFile.cs.
        /// Returns a HashSet of enabled mod filenames.
        /// </summary>
        private HashSet<string> ParseEnabledMods(string jsonContent)
        {
            var enabledMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("enabled_mods", out JsonElement enabledModsElement) &&
                    enabledModsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement modEntry in enabledModsElement.EnumerateArray())
                    {
                        string? modPath = modEntry.GetString();
                        if (modPath != null)
                        {
                            string? fileName = Path.GetFileName(modPath);
                            if (fileName != null)
                            {
                                enabledMods.Add(fileName);
                            }
                        }
                    }
                }
            }
            return enabledMods;
        }

        [Fact]
        public void ParseEnabledMods_ReturnsCorrectMods_FromValidJson()
        {
            // Arrange
            _env.CreateDlcLoadJson(new[] { "crusader_conflicts.mod", "ufc_2933252806.mod" });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);

            // Act
            var enabledMods = ParseEnabledMods(jsonContent);

            // Assert
            Assert.Contains("crusader_conflicts.mod", enabledMods);
            Assert.Contains("ufc_2933252806.mod", enabledMods);
        }

        [Fact]
        public void ParseEnabledMods_ReturnsEmpty_WhenNoModsEnabled()
        {
            // Arrange
            _env.CreateDlcLoadJson(new string[0]);
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);

            // Act
            var enabledMods = ParseEnabledMods(jsonContent);

            // Assert
            Assert.Empty(enabledMods);
        }

        [Fact]
        public void ValidateBaseMod_DetectsMissingCrusaderConflictsMod()
        {
            // Arrange - dlc_load.json without crusader_conflicts.mod
            _env.CreateDlcLoadJson(new[] { "ufc_2933252806.mod", "some_other_mod.mod" });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            // Act - simulate the base mod check from MainFile.cs
            bool isBaseModEnabled = enabledMods.Contains("crusader_conflicts.mod") ||
                                    enabledMods.Contains("ugc_3612451961.mod");

            // Assert
            Assert.False(isBaseModEnabled);
        }

        [Fact]
        public void ValidateBaseMod_DetectsPresentCrusaderConflictsMod()
        {
            // Arrange - dlc_load.json with crusader_conflicts.mod
            _env.CreateDlcLoadJson(new[] { "crusader_conflicts.mod", "ufc_2933252806.mod" });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            // Act
            bool isBaseModEnabled = enabledMods.Contains("crusader_conflicts.mod") ||
                                    enabledMods.Contains("ugc_3612451961.mod");

            // Assert
            Assert.True(isBaseModEnabled);
        }

        [Fact]
        public void ValidateBaseMod_DetectsSteamWorkshopVersion()
        {
            // Arrange - dlc_load.json with Steam Workshop version
            _env.CreateDlcLoadJson(new[] { "ugc_3612451961.mod" });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            // Act
            bool isBaseModEnabled = enabledMods.Contains("crusader_conflicts.mod") ||
                                    enabledMods.Contains("ugc_3612451961.mod");

            // Assert
            Assert.True(isBaseModEnabled);
        }

        [Fact]
        public void ValidateBookmarksPlusPatch_DetectsMissingPatch()
        {
            // Arrange - Bookmarks+ playthrough active, but patch not in dlc_load.json
            _env.CreateDlcLoadJson(new[] { "crusader_conflicts.mod", "ufc_2933252806.mod" });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            string requiredPatch = "crusader_conflicts_bookmarksplus_compat_patch.mod";
            string steamPatch = "ugc_3612526842.mod";

            // Act
            bool isPatchEnabled = enabledMods.Contains(requiredPatch) ||
                                  enabledMods.Contains(steamPatch);

            // Assert
            Assert.False(isPatchEnabled);
        }

        [Fact]
        public void ValidateBookmarksPlusPatch_DetectsPresentPatch()
        {
            // Arrange - Bookmarks+ playthrough active, patch present
            _env.CreateDlcLoadJson(new[] {
                "crusader_conflicts.mod",
                "ufc_2933252806.mod",
                "crusader_conflicts_bookmarksplus_compat_patch.mod"
            });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            string requiredPatch = "crusader_conflicts_bookmarksplus_compat_patch.mod";
            string steamPatch = "ugc_3612526842.mod";

            // Act
            bool isPatchEnabled = enabledMods.Contains(requiredPatch) ||
                                  enabledMods.Contains(steamPatch);

            // Assert
            Assert.True(isPatchEnabled);
        }

        [Fact]
        public void ValidateBookmarksPlusPatch_DetectsSteamWorkshopVersion()
        {
            // Arrange - Steam Workshop version of patch
            _env.CreateDlcLoadJson(new[] {
                "crusader_conflicts.mod",
                "ufc_2933252806.mod",
                "ugc_3612526842.mod"
            });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            string requiredPatch = "crusader_conflicts_bookmarksplus_compat_patch.mod";
            string steamPatch = "ugc_3612526842.mod";

            // Act
            bool isPatchEnabled = enabledMods.Contains(requiredPatch) ||
                                  enabledMods.Contains(steamPatch);

            // Assert
            Assert.True(isPatchEnabled);
        }

        [Fact]
        public void ValidateConflictingPatch_DetectsAGOTPatch_WhenBookmarksPlusActive()
        {
            // Arrange - Bookmarks+ active but AGOT patch also enabled
            _env.CreateDlcLoadJson(new[] {
                "crusader_conflicts.mod",
                "ufc_2933252806.mod",
                "crusader_conflicts_bookmarksplus_compat_patch.mod",
                "crusader_conflicts_agot_compat_patch.mod"
            });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            string activePlaythrough = "BookmarksPlus";
            string agotPatch = "crusader_conflicts_agot_compat_patch.mod";

            // Act - simulate conflicting patch check from MainFile.cs
            bool hasConflictingPatch = activePlaythrough == "BookmarksPlus" &&
                                       enabledMods.Contains(agotPatch);

            // Assert
            Assert.True(hasConflictingPatch);
        }

        [Fact]
        public void ValidateConflictingPatch_DetectsLOTRPatch_WhenBookmarksPlusActive()
        {
            // Arrange - Bookmarks+ active but LOTR patch also enabled
            _env.CreateDlcLoadJson(new[] {
                "crusader_conflicts.mod",
                "ufc_2933252806.mod",
                "crusader_conflicts_bookmarksplus_compat_patch.mod",
                "crusader_conflicts_realms_in_exile_compat_patch.mod"
            });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            string activePlaythrough = "BookmarksPlus";
            string lotrPatch = "crusader_conflicts_realms_in_exile_compat_patch.mod";

            // Act
            bool hasConflictingPatch = activePlaythrough == "BookmarksPlus" &&
                                       enabledMods.Contains(lotrPatch);

            // Assert
            Assert.True(hasConflictingPatch);
        }

        [Fact]
        public void ValidateConflictingPatch_NoConflict_WhenOnlyCorrectPatchEnabled()
        {
            // Arrange - Bookmarks+ active with only correct patch
            _env.CreateDlcLoadJson(new[] {
                "crusader_conflicts.mod",
                "ufc_2933252806.mod",
                "crusader_conflicts_bookmarksplus_compat_patch.mod"
            });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            string activePlaythrough = "BookmarksPlus";
            string agotPatch = "crusader_conflicts_agot_compat_patch.mod";
            string lotrPatch = "crusader_conflicts_realms_in_exile_compat_patch.mod";

            // Act
            bool hasAgotConflict = activePlaythrough == "BookmarksPlus" && enabledMods.Contains(agotPatch);
            bool hasLotrConflict = activePlaythrough == "BookmarksPlus" && enabledMods.Contains(lotrPatch);

            // Assert
            Assert.False(hasAgotConflict);
            Assert.False(hasLotrConflict);
        }

        [Fact]
        public void ValidateLoadOrder_DetectsIncorrectOrder_WhenPatchNotLast()
        {
            // Arrange - patch is not the last mod in the list
            _env.CreateDlcLoadJson(new[] {
                "crusader_conflicts_bookmarksplus_compat_patch.mod",
                "crusader_conflicts.mod",
                "ufc_2933252806.mod"
            });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);

            // Parse enabled mods in order
            var enabledModsList = new List<string>();
            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("enabled_mods", out JsonElement enabledModsElement) &&
                    enabledModsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement modEntry in enabledModsElement.EnumerateArray())
                    {
                        string? modPath = modEntry.GetString();
                        if (modPath != null)
                        {
                            enabledModsList.Add(Path.GetFileName(modPath));
                        }
                    }
                }
            }

            string bookmarksPlusPatchLocal = "crusader_conflicts_bookmarksplus_compat_patch.mod";
            string bookmarksPlusPatchSteam = "ugc_3612526842.mod";

            // Act - find last index of patch
            int patchIndex = enabledModsList.FindLastIndex(m =>
                m.Equals(bookmarksPlusPatchLocal, StringComparison.OrdinalIgnoreCase) ||
                m.Equals(bookmarksPlusPatchSteam, StringComparison.OrdinalIgnoreCase));

            bool loadOrderCorrect = patchIndex == enabledModsList.Count - 1;

            // Assert
            Assert.False(loadOrderCorrect);
        }

        [Fact]
        public void ValidateLoadOrder_DetectsCorrectOrder_WhenPatchIsLast()
        {
            // Arrange - patch is the last mod
            _env.CreateDlcLoadJson(new[] {
                "crusader_conflicts.mod",
                "ufc_2933252806.mod",
                "crusader_conflicts_bookmarksplus_compat_patch.mod"
            });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);

            var enabledModsList = new List<string>();
            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("enabled_mods", out JsonElement enabledModsElement) &&
                    enabledModsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement modEntry in enabledModsElement.EnumerateArray())
                    {
                        string? modPath = modEntry.GetString();
                        if (modPath != null)
                        {
                            enabledModsList.Add(Path.GetFileName(modPath));
                        }
                    }
                }
            }

            string bookmarksPlusPatchLocal = "crusader_conflicts_bookmarksplus_compat_patch.mod";
            string bookmarksPlusPatchSteam = "ugc_3612526842.mod";

            // Act
            int patchIndex = enabledModsList.FindLastIndex(m =>
                m.Equals(bookmarksPlusPatchLocal, StringComparison.OrdinalIgnoreCase) ||
                m.Equals(bookmarksPlusPatchSteam, StringComparison.OrdinalIgnoreCase));

            bool loadOrderCorrect = patchIndex == enabledModsList.Count - 1;

            // Assert
            Assert.True(loadOrderCorrect);
        }

        [Fact]
        public void ValidateIncompatibleParadoxPlazaVersion_Detected()
        {
            // Arrange - Paradox Plaza version present
            _env.CreateDlcLoadJson(new[] {
                "pdx_120158.mod",
                "crusader_conflicts.mod"
            });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            // Act - simulate the Paradox Plaza check
            bool hasIncompatibleVersion = enabledMods.Contains("pdx_120158.mod");

            // Assert
            Assert.True(hasIncompatibleVersion);
        }

        [Fact]
        public void ValidateIncompatibleParadoxPlazaVersion_NotDetected_WhenAbsent()
        {
            // Arrange - no Paradox Plaza version
            _env.CreateDlcLoadJson(new[] { "crusader_conflicts.mod" });
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            var jsonContent = File.ReadAllText(jsonPath);
            var enabledMods = ParseEnabledMods(jsonContent);

            // Act
            bool hasIncompatibleVersion = enabledMods.Contains("pdx_120158.mod");

            // Assert
            Assert.False(hasIncompatibleVersion);
        }

        public void Dispose()
        {
            _env?.Dispose();
        }
    }
}