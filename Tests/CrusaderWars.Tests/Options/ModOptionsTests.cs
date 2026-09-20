using System;
using System.Collections.Generic;
using Xunit;
using CrusaderWars.client;

namespace CrusaderWars.Tests.Options
{
    public class ModOptionsTests
    {
        [Fact]
        public void GetSelectedCustomMapper_ReturnsEmptyString_WhenNotSet()
        {
            // Arrange
            ModOptions.SelectedCustomMapper = string.Empty;

            // Act
            var result = ModOptions.GetSelectedCustomMapper();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetSelectedCustomMapper_ReturnsValue_WhenSet()
        {
            // Arrange
            ModOptions.SelectedCustomMapper = "TestMapper";

            // Act
            var result = ModOptions.GetSelectedCustomMapper();

            // Assert
            Assert.Equal("TestMapper", result);
        }

        [Fact]
        public void GetLevyMax_ReturnsCorrectValue()
        {
            // Arrange
            ModOptions.optionsValuesCollection["LeviesMax"] = "15";

            // Act
            var result = ModOptions.GetLevyMax();

            // Assert
            Assert.Equal(15, result);
        }

        [Fact]
        public void GetInfantryMax_ReturnsCorrectValue()
        {
            // Arrange
            ModOptions.optionsValuesCollection["InfantryMax"] = "12";

            // Act
            var result = ModOptions.GetInfantryMax();

            // Assert
            Assert.Equal(12, result);
        }

        [Fact]
        public void GetRangedMax_ReturnsCorrectValue()
        {
            // Arrange
            ModOptions.optionsValuesCollection["RangedMax"] = "6";

            // Act
            var result = ModOptions.GetRangedMax();

            // Assert
            Assert.Equal(6, result);
        }

        [Fact]
        public void GetCavalryMax_ReturnsCorrectValue()
        {
            // Arrange
            ModOptions.optionsValuesCollection["CavalryMax"] = "8";

            // Act
            var result = ModOptions.GetCavalryMax();

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void GetBattleScale_ReturnsCorrectValue()
        {
            // Arrange
            ModOptions.optionsValuesCollection["BattleScale"] = "150%";

            // Act
            var result = ModOptions.GetBattleScale();

            // Assert
            Assert.Equal(150, result);
        }

        [Fact]
        public void GetAutoScale_ReturnsTrue_WhenEnabled()
        {
            // Arrange
            ModOptions.optionsValuesCollection["AutoScaleUnits"] = "Enabled";

            // Act
            var result = ModOptions.GetAutoScale();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetAutoScale_ReturnsFalse_WhenDisabled()
        {
            // Arrange
            ModOptions.optionsValuesCollection["AutoScaleUnits"] = "Disabled";

            // Act
            var result = ModOptions.GetAutoScale();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CloseCK3DuringBattle_ReturnsTrue_WhenEnabled()
        {
            // Arrange
            ModOptions.optionsValuesCollection["CloseCK3"] = "Enabled";

            // Act
            var result = ModOptions.CloseCK3DuringBattle();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CloseCK3DuringBattle_ReturnsFalse_WhenDisabled()
        {
            // Arrange
            ModOptions.optionsValuesCollection["CloseCK3"] = "Disabled";

            // Act
            var result = ModOptions.CloseCK3DuringBattle();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetOptInPreReleases_ReturnsFalse_WhenNotSet()
        {
            // Arrange
            ModOptions.optionsValuesCollection.Remove("OptInPreReleases");

            // Act
            var result = ModOptions.GetOptInPreReleases();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetOptInPreReleases_ReturnsTrue_WhenSetToTrue()
        {
            // Arrange
            ModOptions.optionsValuesCollection["OptInPreReleases"] = "True";

            // Act
            var result = ModOptions.GetOptInPreReleases();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetOptInPreReleases_ReturnsFalse_WhenSetToFalse()
        {
            // Arrange
            ModOptions.optionsValuesCollection["OptInPreReleases"] = "False";

            // Act
            var result = ModOptions.GetOptInPreReleases();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetSelectedPlaythrough_ReturnsEmptyString_WhenNotSet()
        {
            // Arrange
            ModOptions.optionsValuesCollection.Remove("Playthrough");

            // Act
            var result = ModOptions.GetSelectedPlaythrough();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetSelectedPlaythrough_ReturnsValue_WhenSet()
        {
            // Arrange
            ModOptions.optionsValuesCollection["Playthrough"] = "BookmarksPlus";

            // Act
            var result = ModOptions.GetSelectedPlaythrough();

            // Assert
            Assert.Equal("BookmarksPlus", result);
        }

        [Fact]
        public void GetShowPostBattleReport_ReturnsTrue_Default()
        {
            // Arrange
            ModOptions.optionsValuesCollection.Remove("ShowPostBattleReport");

            // Act
            var result = ModOptions.ShowPostBattleReport();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetShowPostBattleReport_ReturnsTrue_WhenEnabled()
        {
            // Arrange
            ModOptions.optionsValuesCollection["ShowPostBattleReport"] = "Enabled";

            // Act
            var result = ModOptions.ShowPostBattleReport();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetShowPostBattleReport_ReturnsFalse_WhenDisabled()
        {
            // Arrange
            ModOptions.optionsValuesCollection["ShowPostBattleReport"] = "Disabled";

            // Act
            var result = ModOptions.ShowPostBattleReport();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetLinuxSetupCompleted_ReturnsFalse_WhenNotSet()
        {
            // Arrange
            ModOptions.optionsValuesCollection.Remove("LinuxSetupCompleted");

            // Act
            var result = ModOptions.GetLinuxSetupCompleted();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetLinuxSetupCompleted_ReturnsTrue_WhenSetToTrue()
        {
            // Arrange
            ModOptions.optionsValuesCollection["LinuxSetupCompleted"] = "True";

            // Act
            var result = ModOptions.GetLinuxSetupCompleted();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetLinuxSetupCompleted_ReturnsFalse_WhenSetToFalse()
        {
            // Arrange
            ModOptions.optionsValuesCollection["LinuxSetupCompleted"] = "False";

            // Act
            var result = ModOptions.GetLinuxSetupCompleted();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetCombineKnightsEnabled_ReturnsFalse_Default()
        {
            // Arrange
            ModOptions.optionsValuesCollection.Remove("CombineKnights");

            // Act
            var result = ModOptions.CombineKnightsEnabled();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetCombineKnightsEnabled_ReturnsTrue_WhenEnabled()
        {
            // Arrange
            ModOptions.optionsValuesCollection["CombineKnights"] = "Enabled";

            // Act
            var result = ModOptions.CombineKnightsEnabled();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetCombineKnightsEnabled_ReturnsFalse_WhenDisabled()
        {
            // Arrange
            ModOptions.optionsValuesCollection["CombineKnights"] = "Disabled";

            // Act
            var result = ModOptions.CombineKnightsEnabled();

            // Assert
            Assert.False(result);
        }
    }
}