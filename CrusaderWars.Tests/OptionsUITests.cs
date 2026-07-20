using System;
using System.Windows.Forms;
using CrusaderWars.client;
using CrusaderWars.client.Options;
using CrusaderWars.mod_manager;
using Xunit;

namespace CrusaderWars.Tests;

[Collection("Sequential")]
public class OptionsUITests
{
    [Fact]
    public void OptionsForm_Rome2Tab_IsVisibleAndFunctional()
    {
        // Arrange
        var optionsForm = new Options();
        optionsForm.Show();

        // Act
        var rome2TabButton = optionsForm.Controls.Find("Btn_Rome2Tab", true).FirstOrDefault() as Button;
        var umPanel = optionsForm.Controls.Find("UMpanel", true).FirstOrDefault() as Panel;
        var bronzeAgeTab = typeof(Options).GetField("BronzeAge_Tab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(optionsForm) as UC_UnitMapper;

        // Assert - Tab Button Visibility
        Assert.NotNull(rome2TabButton);
        Assert.True(rome2TabButton.Visible);
        Assert.NotNull(umPanel);
        Assert.NotNull(bronzeAgeTab);

        // Act - Simulate Click
        rome2TabButton.PerformClick();

        // Assert - Tab Content Visibility
        Assert.Contains(bronzeAgeTab, umPanel.Controls);
        Assert.True(bronzeAgeTab.Visible);

        optionsForm.Close();
    }

    [Fact]
    public void OptionsForm_Rome2PathButton_UpdatesStatusIcon()
    {
        // Arrange
        var optionsForm = new Options();
        optionsForm.Show();

        var statusIcon = optionsForm.Controls.Find("Status_Rome2_Icon", true).FirstOrDefault() as PictureBox;
        var statusRefreshMethod = typeof(Options).GetMethod("Status_Refresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(statusIcon);
        Assert.NotNull(statusRefreshMethod);

        // Act - Set Invalid Path
        Properties.Settings.Default.VAR_rome2_path = @"C:\invalid\path\game.exe";
        Properties.Settings.Default.Save();
        statusRefreshMethod.Invoke(optionsForm, null);

        // Assert - Warning Icon
        Assert.Equal(CrusaderWars.Properties.Resources.warning__1_, statusIcon.BackgroundImage);

        // Act - Set Valid Path
        Properties.Settings.Default.VAR_rome2_path = @"C:\Games\Total War Rome II\Rome2.exe";
        Properties.Settings.Default.Save();
        statusRefreshMethod.Invoke(optionsForm, null);

        // Assert - Correct Icon
        Assert.Equal(CrusaderWars.Properties.Resources.correct, statusIcon.BackgroundImage);

        optionsForm.Close();
    }
}