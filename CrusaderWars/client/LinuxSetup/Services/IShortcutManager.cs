using System.Threading.Tasks;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public interface IShortcutManager
    {
        Task<bool> CreateAttilaLauncherShortcut(string attilaPath, string outputPath);
        Task<bool> CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string arguments, string description);
    }
}
