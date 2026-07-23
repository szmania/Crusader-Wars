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
            // Priority 1: User-forced override via CC_LINUX environment variable
            if (Environment.GetEnvironmentVariable("CC_LINUX") == "true")
            {
                return true;
            }
            
            // Priority 2: Native Linux detection via .NET runtime
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return true;
            }
            
            // Priority 3: Proton detection via Steam environment variables
            if (HasProtonEnvironmentVariables())
            {
                return true;
            }
            
            // Priority 4: Wine fallback — check if wine is available on a real Linux system (not WSL)
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "wine";
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit(3000);
                
                if (process.ExitCode == 0 && IsRealLinux())
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // wine not available — not Linux
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks for the presence of Steam Proton environment variables.
        /// STEAM_COMPAT_CLIENT_INSTALL_PATH and STEAM_COMPAT_DATA_PATH are set by Steam
        /// when running games through Proton/Steam Play.
        /// </summary>
        private bool HasProtonEnvironmentVariables()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STEAM_COMPAT_CLIENT_INSTALL_PATH")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STEAM_COMPAT_DATA_PATH"));
        }
        
        /// <summary>
        /// Returns true if the application is running under Steam Proton.
        /// This is true when Proton environment variables are detected, regardless of
        /// what RuntimeInformation.IsOSPlatform reports.
        /// </summary>
        public bool IsRunningUnderProton()
        {
            return HasProtonEnvironmentVariables();
        }
        
        /// <summary>
        /// Returns true if the application is running on native Linux (not under Proton or Wine emulation).
        /// This requires RuntimeInformation.IsOSPlatform(OSPlatform.Linux) to be true AND
        /// Proton environment variables to NOT be present.
        /// </summary>
        public bool IsRunningOnNativeLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !HasProtonEnvironmentVariables();
        }
        
        /// <summary>
        /// Returns the Proton wine prefix path if running under Proton, otherwise null.
        /// The prefix path is read from the STEAM_COMPAT_DATA_PATH environment variable.
        /// </summary>
        public string? GetProtonPrefix()
        {
            if (!IsRunningUnderProton()) return null;
            return Environment.GetEnvironmentVariable("STEAM_COMPAT_DATA_PATH");
        }
        
        /// <summary>
        /// Checks /proc/version to determine if this is a real Linux kernel (not WSL).
        /// WSL's /proc/version contains "Microsoft", which we use to distinguish.
        /// Returns true if /proc/version exists and does NOT contain "Microsoft".
        /// </summary>
        private bool IsRealLinux()
        {
            try
            {
                var procVersion = File.ReadAllText("/proc/version");
                return !procVersion.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // /proc/version not accessible — assume not real Linux
                return false;
            }
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
