using CrusaderWars.client.LinuxSetup.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public class LinuxEnvironmentDetector : ILinuxEnvironmentDetector
    {
        public bool IsRunningOnLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        public string? GetWineVersion()
        {
            if (!IsRunningOnLinux()) return null;

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "wine";
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Could not get wine version: {ex.Message}");
                return null;
            }
        }

        public string? GetHomeDirectory()
        {
            if (!IsRunningOnLinux()) return null;
            return Environment.GetEnvironmentVariable("HOME");
        }

        public string? GetSteamPath()
        {
            if (!IsRunningOnLinux()) return null;

            string? home = GetHomeDirectory();
            if (string.IsNullOrEmpty(home)) return null;

            // Common Steam path on Linux
            string steamPath = Path.Combine(home, ".steam", "steam");
            if (Directory.Exists(steamPath))
            {
                return steamPath;
            }
            
            // Another common path (e.g., for Flatpak installs)
            steamPath = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".steam", "steam");
            if (Directory.Exists(steamPath))
            {
                return steamPath;
            }

            // Fallback for some systems
            steamPath = Path.Combine(home, ".local", "share", "Steam");
            if (Directory.Exists(steamPath))
            {
                return steamPath;
            }

            return null;
        }

        public DesktopEnvironment GetDesktopEnvironment()
        {
            if (!IsRunningOnLinux()) return DesktopEnvironment.Unknown;

            string? de = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
            if (string.IsNullOrEmpty(de))
            {
                de = Environment.GetEnvironmentVariable("DESKTOP_SESSION");
            }

            if (string.IsNullOrEmpty(de)) return DesktopEnvironment.Unknown;

            de = de.ToLower();

            if (de.Contains("gnome")) return DesktopEnvironment.GNOME;
            if (de.Contains("kde") || de.Contains("plasma")) return DesktopEnvironment.KDE;
            if (de.Contains("xfce")) return DesktopEnvironment.XFCE;
            if (de.Contains("mate")) return DesktopEnvironment.MATE;
            if (de.Contains("cinnamon")) return DesktopEnvironment.Cinnamon;

            return DesktopEnvironment.Unknown;
        }
    }
}
