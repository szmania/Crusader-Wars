using System;
using System.Threading.Tasks;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public interface IWineManager
    {
        Task<bool> CreatePrefix(string prefixPath);
        Task<bool> InstallDotNet472(string prefixPath, IProgress<string> progress);
        Task<bool> RemoveMono(string prefixPath);
        Task<string> ExecuteCommand(string prefixPath, string command);
    }
}
