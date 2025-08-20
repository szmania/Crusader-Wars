using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Xml.Linq;
using System.Drawing;
using System.Linq;

namespace Crusader_Wars
{
    public  class Updater
    {
        public  string AppVersion { get; set; }
        public string UMVersion { get; set; }


        private static readonly HttpClient client = new HttpClient();
        private const string LatestReleaseUrl = "https://api.github.com/repos/farayC/Crusader-Wars/releases/latest";
        private const string SzmaniaLatestReleaseUrl = "https://api.github.com/repos/szmania/Crusader-Wars/releases/latest";
        private const string UnitMappersLatestReleaseUrl = "https://api.github.com/repos/farayC/CW-Mappers/releases/latest";
        private async Task<(string version, string downloadUrl)> GetLatestReleaseInfoAsync(string releaseUrl)
        {
            Program.Logger.Debug($"Getting latest release info from: {releaseUrl}");
            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "CW App Updater");
                string json = await client.GetStringAsync(releaseUrl);

                // Parse the JSON response
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;

                    // Get the latest version tag
                    string latestVersion = root.GetProperty("tag_name").GetString()?.TrimStart('v');

                    // Get the download URL of the first asset
                    string downloadUrl = root.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    Program.Logger.Debug($"Found version: {latestVersion}, URL: {downloadUrl}");

                    return (latestVersion, downloadUrl);
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error getting release info from {releaseUrl}: {ex.Message}");
            }

            return (null, null);
        }

        string GetAppVersion()
        {
            string version_path = Directory.GetCurrentDirectory() + "\\app_version.txt";
            Program.Logger.Debug($"Reading app version from: {version_path}");
            string app_version = "";
            if (File.Exists(version_path))
            {
                app_version = File.ReadAllText(version_path);
                app_version = Regex.Match(app_version, "\"(.+)\"").Groups[1].Value;
                AppVersion = app_version;
                Program.Logger.Debug($"App version found: {app_version}");
                return app_version;
            }
            Program.Logger.Debug("app_version.txt not found.");
            return app_version;
        }

        string GetUnitMappersVersion()
        {
            string version_path = Directory.GetCurrentDirectory() + "\\um_version.txt";
            Program.Logger.Debug($"Reading unit mappers version from: {version_path}");
            string um_version = "";
            if (File.Exists(version_path))
            {
                um_version = File.ReadAllText(version_path);
                um_version = Regex.Match(um_version, "\"(.+)\"").Groups[1].Value;
                UMVersion = um_version;
                Program.Logger.Debug($"Unit mappers version found: {um_version}");
                return um_version;
            }
            Program.Logger.Debug("um_version.txt not found.");
            return um_version;
        }

        private static Version ParseVersion(string versionStr)
        {
            if (string.IsNullOrWhiteSpace(versionStr)) return new Version("0.0");

            // Remove any non-digit characters except for the dot.
            string cleanedVersion = Regex.Replace(versionStr, @"[^\d.]", "");

            // Clean up potential multiple dots or leading/trailing dots that may result from the regex replace.
            cleanedVersion = Regex.Replace(cleanedVersion, @"\.{2,}", "."); // Replace two or more dots with a single dot.
            cleanedVersion = cleanedVersion.Trim('.'); // Remove leading or trailing dots.

            if (string.IsNullOrWhiteSpace(cleanedVersion)) return new Version("0.0");

            try
            {
                return new Version(cleanedVersion);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to parse version string '{versionStr}' (cleaned to '{cleanedVersion}'). Error: {ex.Message}");
                return new Version("0.0"); // Fallback to a default version on failure.
            }
        }

        bool IsMostRecentUpdate(string app_version, string github_version)
        {
            Program.Logger.Debug($"Comparing versions - App: {app_version}, GitHub: {github_version}");
            Version appVer = ParseVersion(app_version);
            Version gitVer = ParseVersion(github_version);

            if (gitVer > appVer)
            {
                Program.Logger.Debug("GitHub version is newer.");
                return true;
            }
            else
            {
                if (appVer > gitVer)
                {
                    Program.Logger.Debug("App version is newer.");
                }
                else
                {
                    Program.Logger.Debug("Versions are identical.");
                }
                return false;
            }
        }
        

        bool HasInternetConnection()
        {
            Ping myPing = new Ping();
            String host = "google.com";
            byte[] buffer = new byte[32];
            int timeout = 2;
            PingOptions pingOptions = new PingOptions();
            try
            {
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public async void CheckAppVersion()
        {
            Program.Logger.Debug("Checking app version...");
            if (!HasInternetConnection())
            {
                Program.Logger.Debug("No internet connection detected.");
                return;
            }

            Program.Logger.Debug("Internet connection detected.");
            string currentVersion = GetAppVersion();
            if (string.IsNullOrEmpty(currentVersion))
            {
                Program.Logger.Debug("Current version is empty, skipping update check.");
                return;
            }

            var farayC_release = await GetLatestReleaseInfoAsync(LatestReleaseUrl);
            var szmania_release = await GetLatestReleaseInfoAsync(SzmaniaLatestReleaseUrl);

            var releases = new[] { farayC_release, szmania_release }
                .Where(r => r.version != null)
                .ToList();

            if (!releases.Any())
            {
                Program.Logger.Debug("No releases found in any repository.");
                return;
            }

            var latestRelease = releases.Aggregate((r1, r2) => IsMostRecentUpdate(r1.version, r2.version) ? r2 : r1);

            if (IsMostRecentUpdate(currentVersion, latestRelease.version))
            {
                Program.Logger.Debug($"Update available for app: {latestRelease.version}. Starting updater...");
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = @".\data\updater\CW-Updater.exe";
                    startInfo.Arguments = $"{latestRelease.downloadUrl} {latestRelease.version}";
                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Failed to start updater: {ex.Message}");
                }
            }
            else
            {
                Program.Logger.Debug("Application is up to date.");
            }
        }

        public async void CheckUnitMappersVersion()
        {
            Program.Logger.Debug("Checking unit mappers version...");
            if (!HasInternetConnection())
            {
                Program.Logger.Debug("No internet connection detected.");
                return;
            }

            Program.Logger.Debug("Internet connection detected.");
            string currentVersion = GetUnitMappersVersion();
            if (string.IsNullOrEmpty(currentVersion))
            {
                Program.Logger.Debug("Current unit mappers version is empty, skipping update check.");
                return;
            }

            var releaseInfo = await GetLatestReleaseInfoAsync(UnitMappersLatestReleaseUrl);
            if (releaseInfo.version != null && IsMostRecentUpdate(currentVersion, releaseInfo.version))
            {
                Program.Logger.Debug($"Update available for unit mappers: {releaseInfo.version}. Starting updater...");
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = @".\data\updater\CW-Updater.exe";
                    startInfo.Arguments = $"{releaseInfo.downloadUrl} {releaseInfo.version} {"unit_mapper"}";
                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Failed to start updater: {ex.Message}");
                }
            }
            else
            {
                Program.Logger.Debug("Unit mappers are up to date.");
            }
        }
    }

   
}
