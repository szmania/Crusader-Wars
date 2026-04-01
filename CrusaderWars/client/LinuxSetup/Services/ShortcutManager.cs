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
            Program.Logger.Debug("Creating Attila launcher shortcut...");
            string? homeDir = _linuxEnv.GetHomeDirectory();
            if (string.IsNullOrEmpty(homeDir) || string.IsNullOrEmpty(attilaPath)) return false;

            string? attilaRootDir = Path.GetDirectoryName(attilaPath);
            if (string.IsNullOrEmpty(attilaRootDir)) return false;

            // Create Attila-Launcher.bat
            string launcherBatPath = Path.Combine(attilaRootDir, "Attila-Launcher.bat");
            string launcherBatContent = "cmd /c start /unix Z:\\usr\\bin\\bash launch-attila.sh";
            await File.WriteAllTextAsync(launcherBatPath, launcherBatContent);
            Program.Logger.Debug($"Created {launcherBatPath}");

            // Create launch-attila.sh
            string launchShPath = Path.Combine(attilaRootDir, "launch-attila.sh");
            string launchShContent = "#!/bin/bash\ngtk-launch \"Total War ATTILA.desktop\"";
            await File.WriteAllTextAsync(launchShPath, launchShContent);
            Program.Logger.Debug($"Created {launchShPath}");
            
            // Make launch-attila.sh executable
            await ExecuteBashCommand($"chmod +x \"{launchShPath}\"");


            // Create the .lnk file
            string wineFormattedLauncherPath = "Z:" + launcherBatPath.Replace(homeDir, "").Replace("/", "\\");
            string wineFormattedOutputPath = "Z:" + outputPath.Replace(homeDir, "").Replace("/", "\\");
            
            return await CreateShortcut(wineFormattedOutputPath, wineFormattedLauncherPath, Path.GetDirectoryName(wineFormattedLauncherPath) ?? "", "", "Attila CW Launcher");
        }

        public async Task<bool> CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string arguments, string description)
        {
            Program.Logger.Debug($"Creating shortcut: {shortcutPath}");
            string? homeDir = _linuxEnv.GetHomeDirectory();
            if (string.IsNullOrEmpty(homeDir)) return false;

            string vbsScriptContent = $@"
Set FSO = CreateObject(""Scripting.FileSystemObject"")
TargetPath = FSO.GetAbsolutePathName(""{targetPath}"")
WorkingDirectory = ""{workingDirectory}""
Set lnk = CreateObject(""WScript.Shell"").CreateShortcut(""{shortcutPath}"")
    lnk.TargetPath = TargetPath
    lnk.WorkingDirectory = WorkingDirectory
    lnk.Arguments = ""{arguments}""
    lnk.Description = ""{description}""
    lnk.Save
";
            string vbsPath = Path.Combine(homeDir, "shortcut.vbs");
            string wineVbsPath = "Z:" + vbsPath.Replace(homeDir, "").Replace("/", "\\");
            await File.WriteAllTextAsync(vbsPath, vbsScriptContent);
            Program.Logger.Debug("Created temporary VBS script for shortcut creation.");

            try
            {
                await _wineManager.ExecuteCommand("~/.crusader-conflicts-net-pfx", $"wscript.exe //B \"{wineVbsPath}\"");
                Program.Logger.Debug("Shortcut creation script executed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to execute shortcut creation script: {ex.Message}");
                return false;
            }
            finally
            {
                if (File.Exists(vbsPath))
                {
                    File.Delete(vbsPath);
                    Program.Logger.Debug("Deleted temporary VBS script.");
                }
            }
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
