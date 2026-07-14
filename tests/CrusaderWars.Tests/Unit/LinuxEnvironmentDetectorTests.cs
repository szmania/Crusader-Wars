using System;
using System.Collections.Generic;
using CrusaderWars.client.LinuxSetup.Services;
using Xunit;

namespace CrusaderWars.Tests.Unit
{
    public class LinuxEnvironmentDetectorTests : IDisposable
    {
        private readonly LinuxEnvironmentDetector _detector;
        private readonly HashSet<string> _originalEnvVars;

        public LinuxEnvironmentDetectorTests()
        {
            _detector = new LinuxEnvironmentDetector();
            _originalEnvVars = new HashSet<string>();
        }

        public void Dispose()
        {
            // Cleanup: restore original environment variables
            foreach (var key in new[] { "CC_LINUX", "STEAM_COMPAT_CLIENT_INSTALL_PATH", "STEAM_COMPAT_DATA_PATH", "XDG_CURRENT_DESKTOP", "DESKTOP_SESSION", "HOME" })
            {
                if (_originalEnvVars.Contains(key))
                {
                    Environment.SetEnvironmentVariable(key, Environment.GetEnvironmentVariable(key));
                }
                else
                {
                    Environment.SetEnvironmentVariable(key, null);
                }
            }
        }

        private void SetEnvVar(string key, string? value)
        {
            var original = Environment.GetEnvironmentVariable(key);
            if (!_originalEnvVars.Contains(key))
            {
                _originalEnvVars.Add(key);
            }
            Environment.SetEnvironmentVariable(key, value);
        }

        #region IsRunningOnLinux Tests

        [Fact]
        public void IsRunningOnLinux_WithCC_LinuxOverride_ReturnsTrue()
        {
            // Arrange
            SetEnvVar("CC_LINUX", "true");

            // Act
            var result = _detector.IsRunningOnLinux();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRunningOnLinux_WithCC_LinuxInvalidValue_ContinuesToNextCheck()
        {
            // Arrange
            SetEnvVar("CC_LINUX", "false");

            // Act
            var result = _detector.IsRunningOnLinux();

            // Assert
            // On Windows, should return false since no other Linux indicators present
            Assert.False(result);
        }

        [Theory]
        [InlineData("STEAM_COMPAT_CLIENT_INSTALL_PATH", "/home/user/.steam/steam")]
        [InlineData("STEAM_COMPAT_DATA_PATH", "/home/user/.steam/steam/steamapps/compatdata/1091500")]
        public void IsRunningOnLinux_WithProtonEnvVars_ReturnsTrue(string envVar, string value)
        {
            // Arrange
            SetEnvVar(envVar, value);

            // Act
            var result = _detector.IsRunningOnLinux();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRunningOnLinux_WithBothProtonEnvVars_ReturnsTrue()
        {
            // Arrange
            SetEnvVar("STEAM_COMPAT_CLIENT_INSTALL_PATH", "/home/user/.steam/steam");
            SetEnvVar("STEAM_COMPAT_DATA_PATH", "/home/user/.steam/steam/steamapps/compatdata/1091500");

            // Act
            var result = _detector.IsRunningOnLinux();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRunningOnLinux_WithNoLinuxIndicators_ReturnsFalse()
        {
            // Arrange - ensure no Linux env vars are set
            SetEnvVar("CC_LINUX", null);
            SetEnvVar("STEAM_COMPAT_CLIENT_INSTALL_PATH", null);
            SetEnvVar("STEAM_COMPAT_DATA_PATH", null);

            // Act
            var result = _detector.IsRunningOnLinux();

            // Assert
            // On Windows, RuntimeInformation.IsOSPlatform(OSPlatform.Linux) is false
            Assert.False(result);
        }

        #endregion

        #region IsRunningUnderProton Tests

        [Fact]
        public void IsRunningUnderProton_WithSteamCompatClientPath_ReturnsTrue()
        {
            // Arrange
            SetEnvVar("STEAM_COMPAT_CLIENT_INSTALL_PATH", "/home/user/.steam/steam");

            // Act
            var result = _detector.IsRunningUnderProton();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRunningUnderProton_WithSteamCompatDataPath_ReturnsTrue()
        {
            // Arrange
            SetEnvVar("STEAM_COMPAT_DATA_PATH", "/home/user/.steam/steam/steamapps/compatdata/1091500");

            // Act
            var result = _detector.IsRunningUnderProton();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRunningUnderProton_WithNoProtonEnvVars_ReturnsFalse()
        {
            // Arrange
            SetEnvVar("STEAM_COMPAT_CLIENT_INSTALL_PATH", null);
            SetEnvVar("STEAM_COMPAT_DATA_PATH", null);

            // Act
            var result = _detector.IsRunningUnderProton();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRunningUnderProton_WithEmptyProtonEnvVars_ReturnsFalse()
        {
            // Arrange
            SetEnvVar("STEAM_COMPAT_CLIENT_INSTALL_PATH", "");
            SetEnvVar("STEAM_COMPAT_DATA_PATH", "");

            // Act
            var result = _detector.IsRunningUnderProton();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsRunningOnNativeLinux Tests

        [Fact]
        public void IsRunningOnNativeLinux_OnWindows_ReturnsFalse()
        {
            // Act
            var result = _detector.IsRunningOnNativeLinux();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRunningOnNativeLinux_WithProtonEnvVars_ReturnsFalse()
        {
            // Arrange
            SetEnvVar("STEAM_COMPAT_CLIENT_INSTALL_PATH", "/home/user/.steam/steam");

            // Act
            var result = _detector.IsRunningOnNativeLinux();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetProtonPrefix Tests

        [Fact]
        public void GetProtonPrefix_WithProtonEnvVars_ReturnsPrefixPath()
        {
            // Arrange
            var expectedPrefix = "/home/user/.steam/steam/steamapps/compatdata/1091500";
            SetEnvVar("STEAM_COMPAT_DATA_PATH", expectedPrefix);

            // Act
            var result = _detector.GetProtonPrefix();

            // Assert
            Assert.Equal(expectedPrefix, result);
        }

        [Fact]
        public void GetProtonPrefix_WithoutProtonEnvVars_ReturnsNull()
        {
            // Arrange
            SetEnvVar("STEAM_COMPAT_DATA_PATH", null);

            // Act
            var result = _detector.GetProtonPrefix();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetProtonPrefix_WithEmptyProtonEnvVar_ReturnsNull()
        {
            // Arrange
            SetEnvVar("STEAM_COMPAT_DATA_PATH", "");

            // Act
            var result = _detector.GetProtonPrefix();

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetDesktopEnvironment Tests

        [Fact]
        public void GetDesktopEnvironment_WithGnome_ReturnsGnome()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", "GNOME");

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.GNOME, result);
        }

        [Fact]
        public void GetDesktopEnvironment_WithKDE_ReturnsKDE()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", "KDE");

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.KDE, result);
        }

        [Fact]
        public void GetDesktopEnvironment_WithPlasma_ReturnsKDE()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", "plasma");

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.KDE, result);
        }

        [Fact]
        public void GetDesktopEnvironment_WithXFCE_ReturnsXFCE()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", "XFCE");

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.XFCE, result);
        }

        [Fact]
        public void GetDesktopEnvironment_WithMATE_ReturnsMATE()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", "MATE");

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.MATE, result);
        }

        [Fact]
        public void GetDesktopEnvironment_WithCinnamon_ReturnsCinnamon()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", "Cinnamon");

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.Cinnamon, result);
        }

        [Fact]
        public void GetDesktopEnvironment_WithUnknownDesktop_ReturnsUnknown()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", "UnknownDesktop");

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.Unknown, result);
        }

        [Fact]
        public void GetDesktopEnvironment_WithNoEnvVar_ReturnsUnknown()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", null);

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.Unknown, result);
        }

        [Fact]
        public void GetDesktopEnvironment_FallsBackToDesktopSession()
        {
            // Arrange
            SetEnvVar("XDG_CURRENT_DESKTOP", null);
            SetEnvVar("DESKTOP_SESSION", "gnome");

            // Act
            var result = _detector.GetDesktopEnvironment();

            // Assert
            Assert.Equal(DesktopEnvironment.GNOME, result);
        }

        #endregion

        #region GetHomeDirectory Tests

        [Fact]
        public void GetHomeDirectory_OnWindows_ReturnsNull()
        {
            // Act
            var result = _detector.GetHomeDirectory();

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetSteamPath Tests

        [Fact]
        public void GetSteamPath_OnWindows_ReturnsNull()
        {
            // Act
            var result = _detector.GetSteamPath();

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetWineVersion Tests

        [Fact]
        public void GetWineVersion_OnWindows_ReturnsNull()
        {
            // Act
            var result = _detector.GetWineVersion();

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}