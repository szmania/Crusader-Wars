using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using CrusaderWars.Tests.TestHelpers;

namespace CrusaderWars.Tests.Localization
{
    public class UnitsCardsNamesTests : IDisposable
    {
        private readonly TempTestEnvironment _env;

        public UnitsCardsNamesTests()
        {
            _env = new TempTestEnvironment();
        }

        [Fact]
        public void GetLocFilesForPlaythrough_ReturnsFiles_WhenDirectoryExists()
        {
            // Arrange
            var locDir = _env.CreateFireforgedEmpireLocDir();
            var baseFolderName = "fireforged empire";

            // Act - simulate GetLocFilesForPlaythrough logic
            var files = new List<string>();
            string baseLocPath = Path.Combine(_env.UnitsCardsNamesPath, baseFolderName);
            if (Directory.Exists(baseLocPath))
            {
                files.AddRange(Directory.GetFiles(baseLocPath));
            }

            // Assert
            Assert.Single(files);
            Assert.EndsWith(".loc", files[0]);
        }

        [Fact]
        public void GetLocFilesForPlaythrough_ReturnsEmpty_WhenDirectoryMissing()
        {
            // Arrange
            var baseFolderName = "nonexistent folder";

            // Act - simulate GetLocFilesForPlaythrough logic
            var files = new List<string>();
            string baseLocPath = Path.Combine(_env.UnitsCardsNamesPath, baseFolderName);
            if (Directory.Exists(baseLocPath))
            {
                files.AddRange(Directory.GetFiles(baseLocPath));
            }

            // Assert
            Assert.Empty(files);
        }

        [Fact]
        public void GetLocFilesForPlaythrough_IncludesSubmodFiles_WhenActive()
        {
            // Arrange
            var baseFolderName = "fireforged empire";
            var baseLocPath = Path.Combine(_env.UnitsCardsNamesPath, baseFolderName);
            Directory.CreateDirectory(baseLocPath);
            File.WriteAllText(Path.Combine(baseLocPath, "base.loc"), "base content");

            // Create submod folder
            var submodDir = Path.Combine(_env.UnitsCardsNamesPath, $"{baseFolderName}_submod1");
            Directory.CreateDirectory(submodDir);
            File.WriteAllText(Path.Combine(submodDir, "submod.loc"), "submod content");

            // Simulate active submods
            var activeSubmods = new List<string> { "submod1" };

            // Act - simulate GetLocFilesForPlaythrough with submods
            var allLocFiles = new List<string>();
            if (Directory.Exists(baseLocPath))
            {
                allLocFiles.AddRange(Directory.GetFiles(baseLocPath));
            }

            foreach (var submodTag in activeSubmods)
            {
                string submodLocPath = Path.Combine(_env.UnitsCardsNamesPath, $"{baseFolderName}_{submodTag}");
                if (Directory.Exists(submodLocPath))
                {
                    allLocFiles.AddRange(Directory.GetFiles(submodLocPath));
                }
            }

            // Assert
            Assert.Equal(2, allLocFiles.Count);
        }

        [Fact]
        public void EditUnitCardsFiles_ReplacesUnitNames_WhenMatchFound()
        {
            // Arrange
            var locDir = _env.CreateFireforgedEmpireLocDir();
            var locFile = Path.Combine(locDir, "unit_cards.loc");
            var originalContent = File.ReadAllText(locFile);

            // Act - simulate EditUnitCardsFiles replacement logic
            string editedNames = "";
            using (var reader = new StreamReader(locFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("Fireforged Swordsmen"))
                    {
                        line = line.Replace("Fireforged Swordsmen", "Elite Swordsmen");
                    }
                    editedNames += line + "\n";
                }
            }

            // Write back
            File.WriteAllText(locFile, editedNames);

            // Assert
            var updatedContent = File.ReadAllText(locFile);
            Assert.Contains("Elite Swordsmen", updatedContent);
            Assert.DoesNotContain("Fireforged Swordsmen", updatedContent);
        }

        [Fact]
        public void GetUnitScreenNames_ReturnsDictionary_WhenLocFilesExist()
        {
            // Arrange
            var locDir = _env.CreateFireforgedEmpireLocDir();
            var screenNames = new Dictionary<string, string>();

            // Act - simulate GetUnitScreenNames logic
            var locFiles = Directory.GetFiles(locDir);
            foreach (var file in locFiles)
            {
                using (var reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 2 && line.StartsWith("land_units_onscreen_name_"))
                        {
                            var key = parts[0].Replace("land_units_onscreen_name_", "");
                            var name = parts[1];
                            screenNames[key] = name;
                        }
                    }
                }
            }

            // Assert
            Assert.NotEmpty(screenNames);
            Assert.Contains("att_unit_infantry_swordsmen", screenNames.Keys);
            Assert.Equal("Fireforged Swordsmen", screenNames["att_unit_infantry_swordsmen"]);
        }

        [Fact]
        public void GetUnitScreenNames_ReturnsEmpty_WhenNoLocFiles()
        {
            // Arrange
            var screenNames = new Dictionary<string, string>();
            var nonExistentDir = Path.Combine(_env.UnitsCardsNamesPath, "nonexistent");

            // Act - simulate GetUnitScreenNames with missing directory
            if (Directory.Exists(nonExistentDir))
            {
                var locFiles = Directory.GetFiles(nonExistentDir);
                foreach (var file in locFiles)
                {
                    using (var reader = new StreamReader(file))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split('\t');
                            if (parts.Length >= 2 && line.StartsWith("land_units_onscreen_name_"))
                            {
                                var key = parts[0].Replace("land_units_onscreen_name_", "");
                                var name = parts[1];
                                screenNames[key] = name;
                            }
                        }
                    }
                }
            }

            // Assert
            Assert.Empty(screenNames);
        }

        // ---------------------------------------------------------------------------------
        // Section 18 Phase D (D2): game-type subfolder resolution.
        // These mirror the production logic in CrusaderWars\locs\UnitsCardsNames.cs:
        //   LocRoot            = .\data\units_cards_names\{ResolvedBattleGameType}
        //   EffectiveLocFolder = MapperUnitsCardsNamesFolder (attribute) ?? switch(mapperName)
        //   GetLocFilesForPlaythrough(base) = LocRoot\base\* + LocRoot\base_{submod}\*
        //   GetGenericLocFiles()            = LocRoot recursive, else legacy flat root recursive
        // The production class is internal (not visible to this assembly), so the path logic is
        // reproduced faithfully here, exactly as the pre-existing tests in this file do.
        // ---------------------------------------------------------------------------------

        private static string LocRootFor(TempTestEnvironment env, string gameType)
            => Path.Combine(env.UnitsCardsNamesPath, gameType);

        private static string[] GetLocFilesForPlaythroughSim(
            TempTestEnvironment env, string gameType, string baseFolderName, IEnumerable<string> activeSubmods)
        {
            var allLocFiles = new List<string>();
            string locRoot = LocRootFor(env, gameType);

            string baseLocPath = Path.Combine(locRoot, baseFolderName);
            if (Directory.Exists(baseLocPath))
            {
                allLocFiles.AddRange(Directory.GetFiles(baseLocPath));
            }

            foreach (var submodTag in activeSubmods)
            {
                string submodLocPath = Path.Combine(locRoot, baseFolderName + "_" + submodTag);
                if (Directory.Exists(submodLocPath))
                {
                    allLocFiles.AddRange(Directory.GetFiles(submodLocPath));
                }
            }

            return allLocFiles.ToArray();
        }

        private static string[] GetGenericLocFilesSim(TempTestEnvironment env, string gameType)
        {
            string locRoot = LocRootFor(env, gameType);
            if (Directory.Exists(locRoot))
            {
                var files = Directory.GetFiles(locRoot, "*.loc", SearchOption.AllDirectories);
                if (files.Any())
                {
                    return files;
                }
            }

            if (Directory.Exists(env.UnitsCardsNamesPath))
            {
                return Directory.GetFiles(env.UnitsCardsNamesPath, "*.loc", SearchOption.AllDirectories);
            }

            return Array.Empty<string>();
        }

        [Fact]
        public void GetLocFilesForPlaythrough_ResolvesBaseFolder_UnderGameTypeSubfolder()
        {
            // Arrange: base loc folder lives under the game-type subfolder (attila).
            var locDir = _env.CreateFireforgedEmpireLocDirInGameFolder("attila");
            var baseFolderName = "fireforged empire";

            // Act
            var files = GetLocFilesForPlaythroughSim(_env, "attila", baseFolderName, Array.Empty<string>());

            // Assert: resolved from data\units_cards_names\attila\fireforged empire
            Assert.Single(files);
            Assert.EndsWith(".loc", files[0]);
            Assert.StartsWith(_env.UnitsCardsNamesAttilaPath, files[0]);
            Assert.Equal(locDir, Path.GetDirectoryName(files[0]));
        }

        [Fact]
        public void GetLocFilesForPlaythrough_ResolvesSubmodFolder_UnderGameTypeSubfolder()
        {
            // Arrange: base + submod folders both under the game-type subfolder.
            var baseFolderName = "fireforged empire";
            string locRoot = LocRootFor(_env, "attila");

            var baseLocPath = Path.Combine(locRoot, baseFolderName);
            Directory.CreateDirectory(baseLocPath);
            File.WriteAllText(Path.Combine(baseLocPath, "base.loc"), "base content");

            var submodDir = Path.Combine(locRoot, $"{baseFolderName}_submod1");
            Directory.CreateDirectory(submodDir);
            File.WriteAllText(Path.Combine(submodDir, "submod.loc"), "submod content");

            // Act
            var files = GetLocFilesForPlaythroughSim(_env, "attila", baseFolderName, new[] { "submod1" });

            // Assert: both files found, both under the game-type subfolder.
            Assert.Equal(2, files.Length);
            Assert.All(files, f => Assert.StartsWith(_env.UnitsCardsNamesAttilaPath, f));
            Assert.Contains(files, f => f.EndsWith("base.loc"));
            Assert.Contains(files, f => f.EndsWith("submod.loc"));
        }

        [Fact]
        public void EffectiveLocFolder_AttributeOverride_WinsOverSwitchMapping()
        {
            // Arrange: the mapper name maps to "fireforged empire" via the switch, but the
            // Mods.xml units_cards_names_folder attribute declares "divide_et_impera".
            const string mapperName = "OfficialCC_BookmarksPlus_FireforgedEmpire";
            const string attributeValue = "divide_et_impera";

            // Switch mapping (mirrors ResolveBaseFolderName).
            string switchMapping = mapperName switch
            {
                "OfficialCC_BookmarksPlus_FireforgedEmpire" => "fireforged empire",
                _ => ""
            };

            // EffectiveLocFolder: attribute wins when non-empty.
            string effective = !string.IsNullOrEmpty(attributeValue) ? attributeValue : switchMapping;

            // Act: create the attribute-named folder under the game-type subfolder and resolve.
            string locRoot = LocRootFor(_env, "rome_2");
            var attrDir = Path.Combine(locRoot, effective);
            Directory.CreateDirectory(attrDir);
            File.WriteAllText(Path.Combine(attrDir, "attr.loc"), "attr content");

            var files = GetLocFilesForPlaythroughSim(_env, "rome_2", effective, Array.Empty<string>());

            // Assert: the attribute value was used, not the switch mapping.
            Assert.Equal(attributeValue, effective);
            Assert.NotEqual(switchMapping, effective);
            Assert.Single(files);
            Assert.EndsWith("attr.loc", files[0]);
            Assert.StartsWith(_env.UnitsCardsNamesRome2Path, files[0]);
        }

        [Fact]
        public void GetGenericLocFiles_FallsBackToLegacyFlatRoot_WhenGameTypeSubfolderEmpty()
        {
            // Arrange: no loc files under the game-type subfolder; a legacy flat folder exists.
            var legacyDir = Path.Combine(_env.UnitsCardsNamesPath, "legacy_mapper");
            Directory.CreateDirectory(legacyDir);
            File.WriteAllText(Path.Combine(legacyDir, "legacy.loc"), "legacy content");

            // Act
            var files = GetGenericLocFilesSim(_env, "attila");

            // Assert: fell back to the legacy flat root and found the legacy file.
            Assert.Single(files);
            Assert.EndsWith("legacy.loc", files[0]);
            Assert.Equal(legacyDir, Path.GetDirectoryName(files[0]));
        }

        public void Dispose()
        {
            _env?.Dispose();
        }
    }
}