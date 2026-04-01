using System.Threading.Tasks;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public interface ISteamManager
    {
        string? GetSteamPath();
        string? GetAttilaPath();
        string? GetWorkshopModsPath();
        Task<bool> SetLaunchOptions(string gameId, string launchOptions);
    }
}
