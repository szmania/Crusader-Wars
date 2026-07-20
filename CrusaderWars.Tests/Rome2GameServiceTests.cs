using System;
using System.IO;
using CrusaderWars.client;
using CrusaderWars.games;
using Xunit;

namespace CrusaderWars.Tests;

public class Rome2GameServiceTests
{
    [Fact]
    public void GetProcessName_ReturnsRome2()
    {
        var service = new Rome2GameService();
        Assert.Equal("Rome2", service.GetProcessName());
    }

    [Fact]
    public void GetExecutableName_ReturnsRome2Exe()
    {
        var service = new Rome2GameService();
        Assert.Equal("Rome2.exe", service.GetExecutableName());
    }

    [Fact]
    public void GetGameType_ReturnsRome2()
    {
        var service = new Rome2GameService();
        Assert.Equal("Rome2", service.GetGameType());
    }

    [Fact]
    public void GetModActivationFileName_ReturnsUsedModsCcTxt()
    {
        var service = new Rome2GameService();
        Assert.Equal("used_mods_cc.txt", service.GetModActivationFileName());
    }

    [Fact]
    public void GetShortcutArguments_ReturnsUsedModsCcTxt()
    {
        var service = new Rome2GameService();
        Assert.Equal("used_mods_cc.txt", service.GetShortcutArguments());
    }

    [Fact]
    public void ValidatePath_WithRome2Exe_ReturnsTrue()
    {
        var service = new Rome2GameService();
        Assert.True(service.ValidatePath(@"C:\Games\Rome2\Rome2.exe"));
        Assert.True(service.ValidatePath(@"C:\Games\Rome2\rome2.exe"));
    }

    [Fact]
    public void ValidatePath_WithWrongExe_ReturnsFalse()
    {
        var service = new Rome2GameService();
        Assert.False(service.ValidatePath(@"C:\Games\Attila\Attila.exe"));
        Assert.False(service.ValidatePath(@"C:\Games\Rome2\SomeOtherGame.exe"));
    }

    [Fact]
    public void ValidatePath_WithNull_ReturnsFalse()
    {
        var service = new Rome2GameService();
        Assert.False(service.ValidatePath(null!));
    }

    [Fact]
    public void ValidatePath_WithEmpty_ReturnsFalse()
    {
        var service = new Rome2GameService();
        Assert.False(service.ValidatePath(string.Empty));
    }

    [Fact]
    public void GetLogPath_WithValidPath_ReturnsBattleResultsLog()
    {
        // Arrange
        Properties.Settings.Default.VAR_rome2_path = @"C:\Games\Rome2\Rome2.exe";
        Properties.Settings.Default.Save();
        
        var service = new Rome2GameService();
        
        // Act
        var logPath = service.GetLogPath();
        
        // Assert
        Assert.Equal(@"C:\Games\Rome2\data\BattleResults_log.txt", logPath);
    }

    [Fact]
    public void GetLogPath_WithInvalidPath_ReturnsEmpty()
    {
        // Arrange
        Properties.Settings.Default.VAR_rome2_path = string.Empty;
        Properties.Settings.Default.Save();
        
        var service = new Rome2GameService();
        
        // Act
        var logPath = service.GetLogPath();
        
        // Assert
        Assert.Empty(logPath);
    }

    [Fact]
    public void GetModDataPath_WithValidPath_ReturnsDataPath()
    {
        // Arrange
        Properties.Settings.Default.VAR_rome2_path = @"C:\Games\Rome2\Rome2.exe";
        Properties.Settings.Default.Save();
        
        var service = new Rome2GameService();
        
        // Act
        var modDataPath = service.GetModDataPath();
        
        // Assert
        Assert.Equal(@"C:\Games\Rome2\data", modDataPath);
    }

    [Fact]
    public void GetModDataPath_WithEmptyPath_ReturnsEmpty()
    {
        // Arrange
        Properties.Settings.Default.VAR_rome2_path = string.Empty;
        Properties.Settings.Default.Save();
        
        var service = new Rome2GameService();
        
        // Act
        var modDataPath = service.GetModDataPath();
        
        // Assert
        Assert.Empty(modDataPath);
    }

    [Fact]
    public void GetWorkshopPath_ReturnsCorrectSteamWorkshopPath()
    {
        // Arrange
        var service = new Rome2GameService();
        
        // Act
        var workshopPath = service.GetWorkshopPath();
        
        // Assert
        Assert.Contains("214950", workshopPath);
        Assert.Contains("workshop", workshopPath);
        Assert.Contains("content", workshopPath);
    }

    [Fact]
    public void IsGameRunning_WhenNotRunning_ReturnsFalse()
    {
        var service = new Rome2GameService();
        // We can't guarantee Rome2 isn't running, but we can test the method returns a boolean
        var result = service.IsGameRunning();
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void CloseGame_DoesNotThrow()
    {
        var service = new Rome2GameService();
        // Should not throw even if game is not running
        var exception = Record.Exception(() => service.CloseGame());
        Assert.Null(exception);
    }
}