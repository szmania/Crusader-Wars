using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public class SymlinkManager : ISymlinkManager
    {
        public async Task<int> CreateModSymlinks(string sourceModPath, string targetDataPath)
        {
            Program.Logger.Debug($"Creating mod symlinks from {sourceModPath} to {targetDataPath}");
            if (!Directory.Exists(sourceModPath) || !Directory.Exists(targetDataPath))
            {
                Program.Logger.Debug("Source or target directory for symlinks does not exist.");
                return 0;
            }

            int count = 0;
            var packFiles = Directory.GetFiles(sourceModPath, "*.pack", SearchOption.AllDirectories);

            foreach (var packFile in packFiles)
            {
                string filename = Path.GetFileName(packFile);
                if (!filename.StartsWith("@"))
                {
                    filename = "@" + filename;
                }
                string symlinkTarget = Path.Combine(targetDataPath, filename);

                if (File.Exists(symlinkTarget) || Directory.Exists(symlinkTarget)) // Symlinks appear as files/dirs
                {
                    Program.Logger.Debug($"Symlink target '{symlinkTarget}' already exists, skipping.");
                    continue;
                }

                if (await CreateSymlink(packFile, symlinkTarget))
                {
                    count++;
                }
            }

            return count;
        }

        public async Task<bool> CreateSymlink(string source, string target)
        {
            Program.Logger.Debug($"Creating symlink: {target} -> {source}");
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ln",
                        Arguments = $"-s \"{source}\" \"{target}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    Program.Logger.Debug("Symlink created successfully.");
                    return true;
                }
                else
                {
                    Program.Logger.Debug($"Failed to create symlink. Exit code: {process.ExitCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Exception while creating symlink: {ex.Message}");
                return false;
            }
        }
    }
}
