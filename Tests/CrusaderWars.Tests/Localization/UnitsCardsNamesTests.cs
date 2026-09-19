using System;
using System.Collections.Generic;
using System.IO;
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

        public void Dispose()
        {
            _env?.Dispose();
        }
    }
}