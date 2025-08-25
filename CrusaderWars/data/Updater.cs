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

namespace CrusaderWars
{
    public  class Updater
    {
        public  string AppVersion { get; set; }
        public string UMVersion { get; set; }


        private static readonly HttpClient client = new HttpClient();
        private const string LatestReleaseUrl = "https://api.github.com/repos/farayC/Crusader-Wars/releases/latest";
        private const string SzmaniaLatestReleaseUrl = "https://api.github.com/repos/szmania/Crusader-Wars/releases/latest";
        private const string UnitMappersLatestReleaseUrl = "https://api.github.com/repos/farayC/CW-Mappers/releases/latest";
        private const string SzmaniaUnitMappersLatestReleaseUrl = "https://api.github.com/repos/szmania/CW-Mappers/releases/latest";
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
                    string latestVersion = root.GetProperty("tag_name").GetString();

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

            // Remove leading 'v' if present
            string processedVersion = versionStr.StartsWith("v") ? versionStr.Substring(1) : versionStr;

            // Take only the numeric and dot part of the version, stopping at the first letter (pre-release tag)
            string mainVersionPart = new string(processedVersion.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray()).TrimEnd('.');

            if (string.IsNullOrWhiteSpace(mainVersionPart)) return new Version("0.0");

            try
            {
                return new Version(mainVersionPart);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to parse version string '{versionStr}' (processed to '{mainVersionPart}'). Error: {ex.Message}");
                return new Version("0.0"); // Fallback to a default version on failure.
            }
        }

        bool IsNewerVersion(string localVersion, string remoteVersion)
        {
            Program.Logger.Debug($"Comparing versions - Local: {localVersion}, Remote: {remoteVersion}");

            Version localVer = ParseVersion(localVersion);
            Version remoteVer = ParseVersion(remoteVersion);

            if (remoteVer > localVer)
            {
                Program.Logger.Debug("Remote version is newer based on version number.");
                return true;
            }

            if (remoteVer < localVer)
            {
                Program.Logger.Debug("Local version is newer based on version number.");
                return false;
            }

            // If base versions are equal (e.g., 1.0.15), check for pre-release vs stable.
            // A stable release is considered an update to a pre-release.
            bool isLocalPreRelease = Regex.IsMatch(localVersion, "[a-zA-Z]");
            bool isRemoteStable = !Regex.IsMatch(remoteVersion, "[a-zA-Z]");

            if (isLocalPreRelease && isRemoteStable)
            {
                Program.Logger.Debug("Remote version is a stable release of the local pre-release version.");
                return true;
            }

            Program.Logger.Debug("Versions are considered equivalent or local is newer/same.");
            return false;
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

        private async Task<(string version, string downloadUrl)> GetLatestReleaseFromReposAsync(string[] releaseUrls)
        {
            var releaseTasks = releaseUrls.Select(url => GetLatestReleaseInfoAsync(url)).ToArray();
            var releases = await Task.WhenAll(releaseTasks);

            var validReleases = releases.Where(r => r.version != null).ToList();

            if (!validReleases.Any())
            {
                Program.Logger.Debug("No releases found in any repository.");
                return (null, null);
            }

            var latestRelease = validReleases.Aggregate((r1, r2) => IsNewerVersion(r1.version, r2.version) ? r2 : r1);
            return latestRelease;
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

            string[] appReleaseUrls = { LatestReleaseUrl, SzmaniaLatestReleaseUrl };
            var latestRelease = await GetLatestReleaseFromReposAsync(appReleaseUrls);

            if (latestRelease.version == null) return;


            if (IsNewerVersion(currentVersion, latestRelease.version))
            {
                Program.Logger.Debug($"Update available for app: {latestRelease.version}. Starting updater...");
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = @".\data\updater\CWUpdater.exe";
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

            string[] umReleaseUrls = { UnitMappersLatestReleaseUrl, SzmaniaUnitMappersLatestReleaseUrl };
            var latestRelease = await GetLatestReleaseFromReposAsync(umReleaseUrls);

            if (latestRelease.version == null) return;


            if (IsNewerVersion(currentVersion, latestRelease.version))
            {
                Program.Logger.Debug($"Update available for unit mappers: {latestRelease.version}. Starting updater...");
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = @".\data\updater\CWUpdater.exe";
                    startInfo.Arguments = $"{latestRelease.downloadUrl} {latestRelease.version} {"unit_mapper"}";
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

        public async Task<string> GetReleaseUrlForVersion(string version, bool isUnitMapper)
        {
            Program.Logger.Debug($"Searching release URL for version {version} (isUnitMapper: {isUnitMapper})");
            string repoName = isUnitMapper ? "CW-Mappers" : "Crusader-Wars";
            string[] users = { "farayC", "szmania" };

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "CW App Version Checker");

            foreach (var user in users)
            {
                string apiUrl = $"https://api.github.com/repos/{user}/{repoName}/releases/tags/{version}";
                if (!version.StartsWith("v"))
                {
                    apiUrl = $"https://api.github.com/repos/{user}/{repoName}/releases/tags/v{version}";
                }

                try
                {
                    var response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string releaseUrl = $"https://github.com/{user}/{repoName}/releases/tag/{version}";
                        if (!version.StartsWith("v"))
                        {
                            releaseUrl = $"https://github.com/{user}/{repoName}/releases/tag/v{version}";
                        }
                        Program.Logger.Debug($"Found release at: {releaseUrl}");
                        return releaseUrl;
                    }
                    else
                    {
                        // Try without 'v' if it was added
                        if (!version.StartsWith("v"))
                        {
                            apiUrl = $"https://api.github.com/repos/{user}/{repoName}/releases/tags/{version}";
                            response = await client.GetAsync(apiUrl);
                            if (response.IsSuccessStatusCode)
                            {
                                string releaseUrl = $"https://github.com/{user}/{repoName}/releases/tag/{version}";
                                Program.Logger.Debug($"Found release at: {releaseUrl}");
                                return releaseUrl;
                            }
                        }
                        Program.Logger.Debug($"Release not found for user {user} with tag {version} or v{version}. Status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error checking release for user {user}: {ex.Message}");
                }
            }

            // Fallback if not found in any repo
            string fallbackUrl = $"https://github.com/farayC/{repoName}/releases";
            Program.Logger.Debug($"Version tag not found in any user repo. Falling back to main releases page: {fallbackUrl}");
            return fallbackUrl;
        }
    }

   
}
