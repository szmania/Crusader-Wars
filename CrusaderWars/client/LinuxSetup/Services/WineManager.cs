using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public class WineManager : IWineManager
    {
        private readonly ILinuxEnvironmentDetector _linuxEnv;

        public WineManager(ILinuxEnvironmentDetector linuxEnv)
        {
            _linuxEnv = linuxEnv;
        }

        public async Task<bool> CreatePrefix(string prefixPath)
        {
            Program.Logger.Debug($"Creating Wine prefix at {prefixPath}...");
            if (!_linuxEnv.IsRunningOnLinux()) return false;

            try
            {
                string? home = _linuxEnv.GetHomeDirectory();
                if (string.IsNullOrEmpty(home)) return false;

                string fullPrefixPath = prefixPath.Replace("~", home);

                if (Directory.Exists(fullPrefixPath))
                {
                    Program.Logger.Debug("Wine prefix already exists.");
                    return true;
                }

                return await ExecuteWineCommandAsync($"WINEPREFIX=\"{fullPrefixPath}\" winecfg");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to create wine prefix: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> InstallDotNet472(string prefixPath, IProgress<string> progress)
        {
            Program.Logger.Debug($"Installing .NET 4.7.2 in {prefixPath}...");
            if (!_linuxEnv.IsRunningOnLinux()) return false;

            try
            {
                string? home = _linuxEnv.GetHomeDirectory();
                if (string.IsNullOrEmpty(home)) return false;
                string fullPrefixPath = prefixPath.Replace("~", home);

                progress.Report("Removing Mono...");
                await RemoveMono(prefixPath);

                progress.Report("Installing .NET 4.7.2 via winetricks (this may take several minutes)...");
                return await ExecuteWineCommandAsync($"WINEPREFIX=\"{fullPrefixPath}\" winetricks dotnet472");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to install .NET 4.7.2: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveMono(string prefixPath)
        {
            Program.Logger.Debug($"Removing mono from {prefixPath}...");
            if (!_linuxEnv.IsRunningOnLinux()) return false;
            
            string? home = _linuxEnv.GetHomeDirectory();
            if (string.IsNullOrEmpty(home)) return false;
            string fullPrefixPath = prefixPath.Replace("~", home);

            return await ExecuteWineCommandAsync($"WINEPREFIX=\"{fullPrefixPath}\" wine uninstaller --remove '{{e45d8ac6-2d54-4626-9247-6e4616551863}}'");

        }

        public async Task<string> ExecuteCommand(string prefixPath, string command)
        {
            Program.Logger.Debug($"Executing command in {prefixPath}: {command}");
            if (!_linuxEnv.IsRunningOnLinux()) return "Not on Linux";

            string? home = _linuxEnv.GetHomeDirectory();
            if (string.IsNullOrEmpty(home)) return "HOME directory not found.";
            string fullPrefixPath = prefixPath.Replace("~", home);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"WINEPREFIX='{fullPrefixPath}' {command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Program.Logger.Debug($"Error executing command: {error}");
                throw new Exception($"Command failed with exit code {process.ExitCode}: {error}");
            }

            return output;
        }

        private async Task<bool> ExecuteWineCommandAsync(string command)
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
