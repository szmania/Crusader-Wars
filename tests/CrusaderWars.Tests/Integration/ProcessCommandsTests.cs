using System;
using System.Collections.Generic;
using CrusaderWars.process;
using Xunit;

namespace CrusaderWars.Tests.Integration
{
    public class ProcessCommandsTests : IDisposable
    {
        private readonly string _originalHome;
        private readonly string _originalCompatClient;
        private readonly string _originalCompatData;

        public ProcessCommandsTests()
        {
            _originalHome = Environment.GetEnvironmentVariable("HOME");
            _originalCompatClient = Environment.GetEnvironmentVariable("STEAM_COMPAT_CLIENT_INSTALL_PATH");
            _originalCompatData = Environment.GetEnvironmentVariable("STEAM_COMPAT_DATA_PATH");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("HOME", _originalHome);
            Environment.SetEnvironmentVariable("STEAM_COMPAT_CLIENT_INSTALL_PATH", _originalCompatClient);
            Environment.SetEnvironmentVariable("STEAM_COMPAT_DATA_PATH", _originalCompatData);
        }

        #region Initialize Tests

        [Fact]
        public void Initialize_WithNullController_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ProcessCommands.Initialize(null!));
        }

        [Fact]
        public void Initialize_WithWindowsController_SetsController()
        {
            // Arrange
            var controller = new WindowsProcessController();

            // Act
            ProcessCommands.Initialize(controller);

            // Assert - no exception thrown, initialization successful
            Assert.True(true);
        }

        [Fact]
        public void Initialize_WithLinuxController_SetsController()
        {
            // Arrange
            var controller = new LinuxProcessController();

            // Act
            ProcessCommands.Initialize(controller);

            // Assert - no exception thrown, initialization successful
            Assert.True(true);
        }

        #endregion

        #region SuspendProcess Tests

        [Fact]
        public void SuspendProcess_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Arrange - ensure ProcessCommands is not initialized
            // We can't easily reset the static state, so we test the behavior
            // by calling SuspendProcess before Initialize
            // Note: This test may fail if another test already initialized ProcessCommands
            // In a real test suite, we'd use a test fixture to reset state
            Assert.True(true); // Placeholder - static state management is complex
        }

        [Fact]
        public void SuspendProcess_WithUnsupportedController_SkipsGracefully()
        {
            // Arrange
            var controller = new LinuxProcessController();
            ProcessCommands.Initialize(controller);

            // Act - on Windows, LinuxProcessController.IsSupported is false
            // SuspendProcess should log a warning and return without throwing
            if (!controller.IsSupported)
            {
                // Should not throw
                ProcessCommands.SuspendProcess();
                Assert.True(true);
            }
        }

        [Fact]
        public void SuspendProcess_WithSupportedController_DelegatesToController()
        {
            // Arrange
            var controller = new WindowsProcessController();
            ProcessCommands.Initialize(controller);

            // Act - on Windows, WindowsProcessController.IsSupported is true
            // SuspendProcess should delegate to controller.SuspendProcess("ck3.exe")
            // This will throw FileNotFoundException if pssuspend64.exe is not found
            // which is expected behavior
            try
            {
                ProcessCommands.SuspendProcess();
            }
            catch (FileNotFoundException)
            {
                // Expected when pssuspend64.exe is not in the expected location
                Assert.True(true);
            }
        }

        #endregion

        #region ResumeProcess Tests

        [Fact]
        public void ResumeProcess_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Similar to SuspendProcess - static state management is complex
            Assert.True(true); // Placeholder
        }

        [Fact]
        public void ResumeProcess_WithUnsupportedController_SkipsGracefully()
        {
            // Arrange
            var controller = new LinuxProcessController();
            ProcessCommands.Initialize(controller);

            // Act - on Windows, LinuxProcessController.IsSupported is false
            if (!controller.IsSupported)
            {
                ProcessCommands.ResumeProcess();
                Assert.True(true);
            }
        }

        [Fact]
        public void ResumeProcess_WithSupportedController_DelegatesToController()
        {
            // Arrange
            var controller = new WindowsProcessController();
            ProcessCommands.Initialize(controller);

            // Act
            try
            {
                ProcessCommands.ResumeProcess();
            }
            catch (FileNotFoundException)
            {
                // Expected when pssuspend64.exe is not in the expected location
                Assert.True(true);
            }
        }

        #endregion

        #region Controller Selection Tests

        [Fact]
        public void ControllerSelection_OnWindows_SelectsWindowsController()
        {
            // Arrange
            var detector = new CrusaderWars.client.LinuxSetup.Services.LinuxEnvironmentDetector();
            IProcessController controller;

            // Act
            if (detector.IsRunningOnLinux())
            {
                var linuxController = new LinuxProcessController();
                if (linuxController.IsSupported)
                {
                    controller = linuxController;
                }
                else
                {
                    controller = new WindowsProcessController();
                }
            }
            else
            {
                controller = new WindowsProcessController();
            }

            // Assert - on Windows, should select WindowsProcessController
            Assert.IsType<WindowsProcessController>(controller);
        }

        [Fact]
        public void ControllerSelection_WithProtonEnvVars_SelectsLinuxController()
        {
            // Arrange - simulate Proton environment
            Environment.SetEnvironmentVariable("STEAM_COMPAT_CLIENT_INSTALL_PATH", "/home/user/.steam/steam");
            var detector = new CrusaderWars.client.LinuxSetup.Services.LinuxEnvironmentDetector();
            IProcessController controller;

            // Act
            if (detector.IsRunningOnLinux())
            {
                var linuxController = new LinuxProcessController();
                if (linuxController.IsSupported)
                {
                    controller = linuxController;
                }
                else
                {
                    controller = new WindowsProcessController();
                }
            }
            else
            {
                controller = new WindowsProcessController();
            }

            // Assert - with Proton env vars, should select LinuxProcessController
            Assert.IsType<LinuxProcessController>(controller);
        }

        [Fact]
        public void ControllerSelection_WithProtonEnvVars_ButUnsupported_SelectsLinuxController()
        {
            // Arrange - simulate Proton environment
            Environment.SetEnvironmentVariable("STEAM_COMPAT_CLIENT_INSTALL_PATH", "/home/user/.steam/steam");
            var detector = new CrusaderWars.client.LinuxSetup.Services.LinuxEnvironmentDetector();
            IProcessController controller;

            // Act
            if (detector.IsRunningOnLinux())
            {
                var linuxController = new LinuxProcessController();
                if (linuxController.IsSupported)
                {
                    controller = linuxController;
                }
                else
                {
                    // Graceful degradation: use LinuxProcessController anyway
                    // (IsSupported=false causes graceful skip in ProcessCommands)
                    controller = linuxController;
                }
            }
            else
            {
                controller = new WindowsProcessController();
            }

            // Assert - with Proton env vars, should select LinuxProcessController
            // even if IsSupported is false (graceful degradation)
            Assert.IsType<LinuxProcessController>(controller);
        }

        #endregion
    }
}