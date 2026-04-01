using System.Threading.Tasks;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public interface ISymlinkManager
    {
        Task<int> CreateModSymlinks(string sourceModPath, string targetDataPath);
        Task<bool> CreateSymlink(string source, string target);
    }
}
