using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public class ShortcutManager : IShortcutManager
    {
        private readonly IWineManager _wineManager;
        private readonly ILinuxEnvironmentDetector _linuxEnv;

        public ShortcutManager(IWineManager wineManager, ILinuxEnvironmentDetector linuxEnv)
        {
            _wineManager = wineManager;
            _linuxEnv = linuxEnv;
        }

        public async Task<bool> CreateAttilaLauncherShortcut(string attilaPath, string outputPath)
        {
            Program.Logger.Debug("Skipping legacy shortcut creation. The new method uses start-linux.sh with Steam.");
            // This function is now obsolete as we are instructing the user to add
            // start-linux.sh to Steam directly.
            // We will return true to not break the wizard flow.
            return await Task.FromResult(true);
        }

        public async Task<bool> CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string arguments, string description)
        {
            Program.Logger.Debug("Skipping legacy shortcut creation. The new method uses start-linux.sh with Steam.");
            return await Task.FromResult(true);
        }
        
        private async Task<bool> ExecuteBashCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
    }
}
