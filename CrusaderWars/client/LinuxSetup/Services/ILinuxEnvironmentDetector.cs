using CrusaderWars.client.LinuxSetup.Models;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public interface ILinuxEnvironmentDetector
    {
        bool IsRunningOnLinux();
        
        /// <summary>
        /// Returns true if the application is running under Steam Proton (as opposed to native Linux or plain Wine).
        /// Checks for the presence of STEAM_COMPAT_CLIENT_INSTALL_PATH and STEAM_COMPAT_DATA_PATH environment variables.
        /// </summary>
        bool IsRunningUnderProton();
        
        /// <summary>
        /// Returns true if the application is running on native Linux (not under Proton or Wine).
        /// This is true when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) returns true and Proton env vars are not set.
        /// </summary>
        bool IsRunningOnNativeLinux();
        
        /// <summary>
        /// Returns the Proton wine prefix path (STEAM_COMPAT_DATA_PATH) if running under Proton, otherwise null.
        /// </summary>
        string? GetProtonPrefix();
        
        string? GetWineVersion();
        string? GetHomeDirectory();
        string? GetSteamPath();
        DesktopEnvironment GetDesktopEnvironment();
    }
}
