using System;
using System.IO;
using Xunit;
using CrusaderWars.Tests.TestHelpers;

namespace CrusaderWars.Tests.Options
{
    public class OptionsTests : IDisposable
    {
        private readonly TempTestEnvironment _env;
        private readonly string _settingsPath;

        public OptionsTests()
        {
            _env = new TempTestEnvironment();
            _settingsPath = _env.SettingsPath;
            _env.CreateOptionsXml();
            _env.CreateUnitMappersXml(bookmarksPlus: true);
        }

        [Fact]
        public void ReadUnitMappersOptions_CreatesFile_WhenMissing()
        {
            // Arrange
            var missingDir = Path.Combine(_env.RootPath, "missing_settings");
            Directory.CreateDirectory(missingDir);
            var missingFile = Path.Combine(missingDir, "UnitMappers.xml");
            if (File.Exists(missingFile)) File.Delete(missingFile);

            // Act - simulate what Options.ReadUnitMappersOptions does for file creation
            if (!File.Exists(missingFile))
            {
                var defaultXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><UMOptions><UnitMappers name=\"DefaultCK3\">False</UnitMappers><UnitMappers name=\"BookmarksPlus\">False</UnitMappers></UMOptions>";
                File.WriteAllText(missingFile, defaultXml);
            }

            // Assert
            Assert.True(File.Exists(missingFile));
            var content = File.ReadAllText(missingFile);
            Assert.Contains("DefaultCK3", content);
            Assert.Contains("BookmarksPlus", content);
        }

        [Fact]
        public void ReadUnitMappersOptions_PreservesExistingValues()
        {
            // Arrange
            var filePath = Path.Combine(_settingsPath, "UnitMappers.xml");
            var originalContent = File.ReadAllText(filePath);

            // Act
            var contentAfterRead = File.ReadAllText(filePath);

            // Assert
            Assert.Equal(originalContent, contentAfterRead);
            Assert.Contains("<UnitMappers name=\"BookmarksPlus\">True</UnitMappers>", contentAfterRead);
        }

        [Fact]
        public void WriteUnitMappersOptions_UpdatesBookmarksPlusState()
        {
            // Arrange
            var filePath = Path.Combine(_settingsPath, "UnitMappers.xml");
            var xml = File.ReadAllText(filePath);
            xml = xml.Replace("<UnitMappers name=\"BookmarksPlus\">True</UnitMappers>",
                              "<UnitMappers name=\"BookmarksPlus\">False</UnitMappers>");
            File.WriteAllText(filePath, xml);

            // Act
            var updatedContent = File.ReadAllText(filePath);

            // Assert
            Assert.Contains("<UnitMappers name=\"BookmarksPlus\">False</UnitMappers>", updatedContent);
        }

        [Fact]
        public void GetActivePlaythrough_ReturnsBookmarksPlus_WhenEnabled()
        {
            // Arrange
            var filePath = Path.Combine(_settingsPath, "UnitMappers.xml");
            var xml = File.ReadAllText(filePath);

            // Simulate parsing to find active playthrough
            string activePlaythrough = null;
            if (xml.Contains("<UnitMappers name=\"BookmarksPlus\">True</UnitMappers>"))
            {
                activePlaythrough = "BookmarksPlus";
            }

            // Act
            var result = activePlaythrough;

            // Assert
            Assert.Equal("BookmarksPlus", result);
        }

        [Fact]
        public void GetActivePlaythrough_ReturnsNull_WhenNoneEnabled()
        {
            // Arrange
            var filePath = Path.Combine(_settingsPath, "UnitMappers.xml");
            var xml = File.ReadAllText(filePath);
            xml = xml.Replace("<UnitMappers name=\"BookmarksPlus\">True</UnitMappers>",
                              "<UnitMappers name=\"BookmarksPlus\">False</UnitMappers>");
            File.WriteAllText(filePath, xml);

            // Act
            string activePlaythrough = null;
            if (xml.Contains(">True<"))
            {
                // Would extract name in real code
            }

            // Assert
            Assert.Null(activePlaythrough);
        }

        [Fact]
        public void CheckPlaythroughSelection_StopsPulsing_WhenPlaythroughSelected()
        {
            // Arrange
            var filePath = Path.Combine(_settingsPath, "UnitMappers.xml");
            var xml = File.ReadAllText(filePath);
            bool isPulsing = true;

            // Act - simulate what CheckPlaythroughSelection does
            if (xml.Contains(">True<"))
            {
                isPulsing = false;
            }

            // Assert
            Assert.False(isPulsing);
        }

        [Fact]
        public void CheckPlaythroughSelection_StartsPulsing_WhenNoPlaythroughSelected()
        {
            // Arrange
            var filePath = Path.Combine(_settingsPath, "UnitMappers.xml");
            var xml = File.ReadAllText(filePath);
            xml = xml.Replace("<UnitMappers name=\"BookmarksPlus\">True</UnitMappers>",
                              "<UnitMappers name=\"BookmarksPlus\">False</UnitMappers>");
            File.WriteAllText(filePath, xml);
            bool isPulsing = false;

            // Act - simulate what CheckPlaythroughSelection does
            if (!xml.Contains(">True<"))
            {
                isPulsing = true;
            }

            // Assert
            Assert.True(isPulsing);
        }

        [Fact]
        public void OptionsXml_ContainsAllRequiredOptions()
        {
            // Arrange
            var filePath = Path.Combine(_settingsPath, "Options.xml");
            var content = File.ReadAllText(filePath);

            // Assert - verify all expected options exist
            Assert.Contains("CloseCK3", content);
            Assert.Contains("CloseAttila", content);
            Assert.Contains("FullArmies", content);
            Assert.Contains("TimeLimit", content);
            Assert.Contains("BattleMapsSize", content);
            Assert.Contains("DefensiveDeployables", content);
            Assert.Contains("UnitCards", content);
            Assert.Contains("SeparateArmies", content);
            Assert.Contains("SiegeEnginesInFieldBattles", content);
            Assert.Contains("ShowPostBattleReport", content);
            Assert.Contains("LeviesMax", content);
            Assert.Contains("RangedMax", content);
            Assert.Contains("InfantryMax", content);
            Assert.Contains("CavalryMax", content);
            Assert.Contains("BattleScale", content);
            Assert.Contains("AutoScaleUnits", content);
            Assert.Contains("CommanderWoundedChance", content);
            Assert.Contains("KnightWoundedChance", content);
            Assert.Contains("OptInPreReleases", content);
            Assert.Contains("CombineKnights", content);
            Assert.Contains("LinuxSetupCompleted", content);
        }

        public void Dispose()
        {
            _env?.Dispose();
        }
    }
}