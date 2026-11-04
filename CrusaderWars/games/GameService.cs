using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CrusaderWars.client;

namespace CrusaderWars.games
{
    public abstract class GameService
    {
        // Abstract methods that must be implemented by concrete game services
        public abstract string GetProcessName();
        public abstract string GetExecutableName();
        public abstract string GetGameType();
        public abstract string GetLogPath();
        public abstract string GetModDataPath();
        public abstract string GetWorkshopPath();
        public abstract string GetModActivationFileName();
public abstract string GetShortcutArguments();
        public abstract string GetBattleFilesPath();
        public abstract string GetScriptPath();
        public abstract string GetBattleTemplatePath();

        // Virtual methods that can be overridden if needed
        public virtual void CreateShortcut(string shortcutPath)
        {
            // Use Windows Script Host to create shortcut
            try
            {
                Type? t = Type.GetTypeFromProgID("WScript.Shell");
                if (t == null)
                {
                    throw new InvalidOperationException("Unable to create WScript.Shell object");
                }

                dynamic shell = Activator.CreateInstance(t!)!;
                var shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = GetGamePath();
                shortcut.WorkingDirectory = Path.GetDirectoryName(GetGamePath())!;
                shortcut.Arguments = GetShortcutArguments();
                shortcut.Description = $"Crusader Conflicts shortcut for {GetGameType()}";
                shortcut.Save();
            }
            catch (Exception ex)
            {
                // Fallback: create a simple batch file
                try
                {
                    string batchPath = shortcutPath.Replace(".lnk", ".bat");
                    string batchContent = $"@echo off\n" +
                                          $"cd /d \"{Path.GetDirectoryName(GetGamePath())}\"\n" +
                                          $"start \"\" \"{GetGamePath()}\" {GetShortcutArguments()}\n";
                    File.WriteAllText(batchPath, batchContent);
                }
                catch (Exception batchEx)
                {
                    throw new InvalidOperationException($"Failed to create shortcut: {ex.Message}. Fallback also failed: {batchEx.Message}");
                }
            }
        }

        public virtual void LaunchGame(string shortcutPath)
        {
            if (!File.Exists(shortcutPath))
            {
                throw new FileNotFoundException($"Shortcut not found: {shortcutPath}");
            }

            Process.Start(new ProcessStartInfo(shortcutPath) { UseShellExecute = true });
        }

        public virtual void CloseGame()
        {
            string processName = GetProcessName();
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (Process process in processes)
            {
                process.Kill();
                process.WaitForExit();
                process.Dispose();
            }
        }

        public virtual bool IsGameRunning()
        {
            string processName = GetProcessName();
            return Process.GetProcessesByName(processName).Length > 0;
        }

        public virtual bool ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string fileName = Path.GetFileName(path).ToLower();
            string expectedExe = GetExecutableName().ToLower();
            
            return fileName == expectedExe || fileName == $"{expectedExe}.exe";
        }

        // Protected helper method to get the game path from ModOptions
        protected abstract string GetGamePath();
    }
}