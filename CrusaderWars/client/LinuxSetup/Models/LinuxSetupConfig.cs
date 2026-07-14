using CrusaderWars.client.LinuxSetup.Models;

namespace CrusaderWars.client.LinuxSetup.Models
{
    public class LinuxSetupConfig
    {
        public string WinePrefix { get; set; } = "~/.crusader-conflicts-net-pfx";
        public string? AttilaPath { get; set; }
        public string? SteamPath { get; set; }
        public string? WorkshopModsPath { get; set; }
        public string? CrusaderConflictsPath { get; set; }
        public DesktopEnvironment DesktopEnv { get; set; }
        public bool SetupCompleted { get; set; }
        
        /// <summary>
        /// True if the application is running under Steam Proton (as opposed to native Linux or plain Wine).
        /// Populated from ILinuxEnvironmentDetector.IsRunningUnderProton().
        /// </summary>
        public bool IsProton { get; set; }
        
        /// <summary>
        /// The Proton wine prefix path (STEAM_COMPAT_DATA_PATH) if running under Proton, otherwise null.
        /// Populated from ILinuxEnvironmentDetector.GetProtonPrefix().
        /// </summary>
        public string? ProtonPrefix { get; set; }
    }
}
