using CrusaderWars.client.LinuxSetup.Models;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public interface ILinuxEnvironmentDetector
    {
        bool IsRunningOnLinux();
        string? GetWineVersion();
        string? GetHomeDirectory();
        string? GetSteamPath();
        DesktopEnvironment GetDesktopEnvironment();
    }
}
