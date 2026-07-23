using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrusaderWars.client.LinuxSetup;
using CrusaderWars.client.LinuxSetup.Models;
using CrusaderWars.client.LinuxSetup.Services;
using CrusaderWars.client.LinuxSetup.Steps;
using Xunit;
using Moq;

namespace CrusaderWars.Tests.Integration
{
    public class LinuxSetupWizardTests : IDisposable
    {
        private readonly Mock<ILinuxEnvironmentDetector> _mockDetector;
        private readonly Mock<IWineManager> _mockWineManager;
        private readonly Mock<ISteamManager> _mockSteamManager;
        private readonly Mock<ISymlinkManager> _mockSymlinkManager;
        private readonly Mock<IShortcutManager> _mockShortcutManager;
        private readonly LinuxSetupWizard _wizard;
        private readonly LinuxSetupConfig _config;

        public LinuxSetupWizardTests()
        {
            _mockDetector = new Mock<ILinuxEnvironmentDetector>();
            _mockWineManager = new Mock<IWineManager>();
            _mockSteamManager = new Mock<ISteamManager>();
            _mockSymlinkManager = new Mock<ISymlinkManager>();
            _mockShortcutManager = new Mock<IShortcutManager>();
            _config = new LinuxSetupConfig();

            _wizard = new LinuxSetupWizard
            {
                _linuxEnvDetector = _mockDetector.Object,
                _wineManager = _mockWineManager.Object,
                _steamManager = _mockSteamManager.Object,
                _symlinkManager = _mockSymlinkManager.Object,
                _shortcutManager = _mockShortcutManager.Object,
                _config = _config
            };

            // Initialize wizard controls (mocked for testing)
            // In a real test, we'd need to initialize the form controls
            // For now, we'll test the logic methods directly
        }

        public void Dispose()
        {
            _mockDetector.Reset();
            _mockWineManager.Reset();
            _mockSteamManager.Reset();
            _mockSymlinkManager.Reset();
            _mockShortcutManager.Reset();
        }

        #region Detection Step Tests

        [Fact]
        public async Task RunDetectionStep_WithProtonEnvironment_ReturnsTrueAndSetsConfig()
        {
            // Arrange
            _mockDetector.Setup(d => d.IsRunningOnLinux()).Returns(true);
            _mockDetector.Setup(d => d.IsRunningUnderProton()).Returns(true);
            _mockDetector.Setup(d => d.IsRunningOnNativeLinux()).Returns(false);
            _mockDetector.Setup(d => d.GetProtonPrefix()).Returns("/home/user/.steam/steam/compatdata/123456/pfx");
            _mockDetector.Setup(d => d.GetWineVersion()).Returns("wine-8.0");
            _mockDetector.Setup(d => d.GetDesktopEnvironment()).Returns(DesktopEnvironment.GNOME);
            _mockSteamManager.Setup(s => s.GetSteamPath()).Returns("/home/user/.steam/steam");
            _mockSteamManager.Setup(s => s.GetAttilaPath()).Returns("/home/user/.steam/steam/steamapps/common/Total War Attila");

            // Act
            var result = await _wizard.RunDetectionStep();

            // Assert
            Assert.True(result);
            Assert.True(_config.IsProton);
            Assert.Equal("/home/user/.steam/steam/compatdata/123456/pfx", _config.ProtonPrefix);
        }

        [Fact]
        public async Task RunDetectionStep_WithNativeLinuxEnvironment_ReturnsTrueAndSetsConfig()
        {
            // Arrange
            _mockDetector.Setup(d => d.IsRunningOnLinux()).Returns(true);
            _mockDetector.Setup(d => d.IsRunningUnderProton()).Returns(false);
            _mockDetector.Setup(d => d.IsRunningOnNativeLinux()).Returns(true);
            _mockDetector.Setup(d => d.GetProtonPrefix()).Returns((string?)null);
            _mockDetector.Setup(d => d.GetWineVersion()).Returns("wine-8.0");
            _mockDetector.Setup(d => d.GetDesktopEnvironment()).Returns(DesktopEnvironment.KDE);
            _mockSteamManager.Setup(s => s.GetSteamPath()).Returns("/home/user/.steam/steam");
            _mockSteamManager.Setup(s => s.GetAttilaPath()).Returns("/home/user/.steam/steam/steamapps/common/Total War Attila");

            // Act
            var result = await _wizard.RunDetectionStep();

            // Assert
            Assert.True(result);
            Assert.False(_config.IsProton);
            Assert.Null(_config.ProtonPrefix);
        }

        [Fact]
        public async Task RunDetectionStep_WithWineEnvironment_ReturnsTrueAndSetsConfig()
        {
            // Arrange
            _mockDetector.Setup(d => d.IsRunningOnLinux()).Returns(true);
            _mockDetector.Setup(d => d.IsRunningUnderProton()).Returns(false);
            _mockDetector.Setup(d => d.IsRunningOnNativeLinux()).Returns(false);
            _mockDetector.Setup(d => d.GetProtonPrefix()).Returns((string?)null);
            _mockDetector.Setup(d => d.GetWineVersion()).Returns("wine-8.0");
            _mockDetector.Setup(d => d.GetDesktopEnvironment()).Returns(DesktopEnvironment.XFCE);
            _mockSteamManager.Setup(s => s.GetSteamPath()).Returns("/home/user/.steam/steam");
            _mockSteamManager.Setup(s => s.GetAttilaPath()).Returns("/home/user/.steam/steam/steamapps/common/Total War Attila");

            // Act
            var result = await _wizard.RunDetectionStep();

            // Assert
            Assert.True(result);
            Assert.False(_config.IsProton);
            Assert.Null(_config.ProtonPrefix);
        }

        [Fact]
        public async Task RunDetectionStep_WithNonLinuxEnvironment_ReturnsFalse()
        {
            // Arrange
            _mockDetector.Setup(d => d.IsRunningOnLinux()).Returns(false);

            // Act
            var result = await _wizard.RunDetectionStep();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RunDetectionStep_WithWineNotFoundAndNotProton_ReturnsFalse()
        {
            // Arrange
            _mockDetector.Setup(d => d.IsRunningOnLinux()).Returns(true);
            _mockDetector.Setup(d => d.IsRunningUnderProton()).Returns(false);
            _mockDetector.Setup(d => d.GetWineVersion()).Returns((string?)null); // wine not found

            // Act
            var result = await _wizard.RunDetectionStep();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Wine Prefix Step Tests

        [Fact]
        public async Task RunWinePrefixStep_WithProtonAndExistingPrefix_ReusesPrefix()
        {
            // Arrange
            _config.IsProton = true;
            _config.ProtonPrefix = "/home/user/.steam/steam/compatdata/123456/pfx";
            _mockWineManager.Setup(w => w.CreatePrefix(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            await _wizard.RunWinePrefixStep();

            // Assert
            // Should NOT call CreatePrefix since we're reusing existing Proton prefix
            _mockWineManager.Verify(w => w.CreatePrefix(It.IsAny<string>()), Times.Never);
            Assert.Equal("/home/user/.steam/steam/compatdata/123456/pfx", _config.WinePrefix);
        }

        [Fact]
        public async Task RunWinePrefixStep_WithProtonButMissingPrefix_CreatesNewPrefix()
        {
            // Arrange
            _config.IsProton = true;
            _config.ProtonPrefix = "/home/user/.steam/steam/compatdata/123456/pfx"; // but directory doesn't exist
            _mockWineManager.Setup(w => w.CreatePrefix(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            await _wizard.RunWinePrefixStep();

            // Assert
            // Should call CreatePrefix since Proton prefix doesn't exist on disk
            _mockWineManager.Verify(w => w.CreatePrefix(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RunWinePrefixStep_WithNativeLinux_CreatesDefaultPrefix()
        {
            // Arrange
            _config.IsProton = false;
            _config.WinePrefix = "~/.crusader-conflicts-net-pfx";
            _mockWineManager.Setup(w => w.CreatePrefix(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            await _wizard.RunWinePrefixStep();

            // Assert
            _mockWineManager.Verify(w => w.CreatePrefix("~/.crusader-conflicts-net-pfx"), Times.Once);
        }

        [Fact]
        public async Task RunWinePrefixStep_WithCreationFailure_ReturnsFalse()
        {
            // Arrange
            _config.IsProton = false;
            _config.WinePrefix = "~/.test-prefix";
            _mockWineManager.Setup(w => w.CreatePrefix(It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var result = await _wizard.RunWinePrefixStep();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region .NET Install Step Tests

        [Fact]
        public async Task RunDotNetInstallStep_WithDotNetAlreadyInstalled_SkipsInstallation()
        {
            // Arrange
            _config.WinePrefix = "~/.test-prefix";
            _mockWineManager.Setup(w => w.CheckDotNetInstalled(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            await _wizard.RunDotNetInstallStep();

            // Assert
            // Should NOT call InstallDotNet472 since .NET is already installed
            _mockWineManager.Verify(w => w.InstallDotNet472(It.IsAny<string>(), It.IsAny<System.IProgress<string>>()), Times.Never);
        }

        [Fact]
        public async Task RunDotNetInstallStep_WithDotNetNotInstalled_AttemptsInstallation()
        {
            // Arrange
            _config.WinePrefix = "~/.test-prefix";
            _mockWineManager.Setup(w => w.CheckDotNetInstalled(It.IsAny<string>())).ReturnsAsync(false);
            _mockWineManager.Setup(w => w.InstallDotNet472(It.IsAny<string>(), It.IsAny<System.IProgress<string>>())).ReturnsAsync(true);

            // Act
            await _wizard.RunDotNetInstallStep();

            // Assert
            // Should call InstallDotNet472 since .NET is not installed
            _mockWineManager.Verify(w => w.InstallDotNet472(It.IsAny<string>(), It.IsAny<System.IProgress<string>>()), Times.Once);
        }

        #endregion
    }
}