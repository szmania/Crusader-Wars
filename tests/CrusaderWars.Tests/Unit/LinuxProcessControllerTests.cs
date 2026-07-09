using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CrusaderWars.process;
using Xunit;

namespace CrusaderWars.Tests.Unit
{
    public class LinuxProcessControllerTests : IDisposable
    {
        private readonly LinuxProcessController _controller;
        private readonly string _originalHome;
        private readonly string _originalCompatClient;
        private readonly string _originalCompatData;

        public LinuxProcessControllerTests()
        {
            _controller = new LinuxProcessController();
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

        #region IsSupported Tests

        [Fact]
        public void IsSupported_OnWindows_ReturnsFalse()
        {
            // Act
            var result = _controller.IsSupported;

            // Assert
            // On Windows, kill/pgrep are not available, so IsSupported should be false
            Assert.False(result);
        }

        #endregion

        #region FindPid Tests

        [Fact]
        public void FindPid_WithNullProcessName_ThrowsPlatformNotSupportedException()
        {
            // Act & Assert
            Assert.Throws<PlatformNotSupportedException>(() => _controller.FindPid(null!));
        }

        [Fact]
        public void FindPid_WithEmptyProcessName_ThrowsPlatformNotSupportedException()
        {
            // Act & Assert
            Assert.Throws<PlatformNotSupportedException>(() => _controller.FindPid(""));
        }

        [Fact]
        public void FindPid_WhenNotSupported_ThrowsPlatformNotSupportedException()
        {
            // This test verifies that FindPid throws when IsSupported is false
            // On Windows, IsSupported is false, so any call should throw
            if (!_controller.IsSupported)
            {
                Assert.Throws<PlatformNotSupportedException>(() => _controller.FindPid("ck3.exe"));
            }
        }

        #endregion

        #region SuspendProcess Tests

        [Fact]
        public void SuspendProcess_WithNullProcessName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _controller.SuspendProcess(null!));
        }

        [Fact]
        public void SuspendProcess_WithEmptyProcessName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _controller.SuspendProcess(""));
        }

        [Fact]
        public void SuspendProcess_WhenNotSupported_ThrowsPlatformNotSupportedException()
        {
            // On Windows, IsSupported is false
            if (!_controller.IsSupported)
            {
                Assert.Throws<PlatformNotSupportedException>(() => _controller.SuspendProcess("ck3.exe"));
            }
        }

        #endregion

        #region ResumeProcess Tests

        [Fact]
        public void ResumeProcess_WithNullProcessName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _controller.ResumeProcess(null!));
        }

        [Fact]
        public void ResumeProcess_WithEmptyProcessName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _controller.ResumeProcess(""));
        }

        [Fact]
        public void ResumeProcess_WhenNotSupported_ThrowsPlatformNotSupportedException()
        {
            // On Windows, IsSupported is false
            if (!_controller.IsSupported)
            {
                Assert.Throws<PlatformNotSupportedException>(() => _controller.ResumeProcess("ck3.exe"));
            }
        }

        #endregion

        #region TryPgrep Tests (via reflection since it's private)

        [Fact]
        public void TryPgrep_WithInvalidArguments_ReturnsNull()
        {
            // This test verifies TryPgrep behavior with invalid arguments
            // On Windows, pgrep doesn't exist, so it should return null or throw
            // The method is private, so we test it indirectly through FindPid
            if (_controller.IsSupported)
            {
                // If supported, TryPgrep should handle invalid args gracefully
                var result = InvokeTryPgrep("invalid_arg_that_will_fail");
                Assert.Null(result);
            }
        }

        #endregion

        #region SendSignal Tests (via reflection since it's private)

        [Fact]
        public void SendSignal_WithInvalidPid_ThrowsInvalidOperationException()
        {
            // This test verifies SendSignal behavior with invalid PID
            // On Windows, kill doesn't exist, so it should throw
            if (_controller.IsSupported)
            {
                // If supported, SendSignal should throw for invalid PID
                Assert.Throws<InvalidOperationException>(() => InvokeSendSignal(999999, "STOP"));
            }
        }

        #endregion

        #region Helper Methods for Reflection

        private int? InvokeTryPgrep(string arguments)
        {
            var method = typeof(LinuxProcessController).GetMethod("TryPgrep",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (int?)method.Invoke(null, new object[] { arguments });
        }

        private void InvokeSendSignal(int pid, string signal)
        {
            var method = typeof(LinuxProcessController).GetMethod("SendSignal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { pid, signal });
        }

        #endregion
    }
}