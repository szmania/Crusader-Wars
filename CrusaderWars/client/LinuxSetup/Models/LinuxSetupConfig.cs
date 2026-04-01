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
    }
}
