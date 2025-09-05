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
        public  string AppVersion { get; set; } = string.Empty;
        public string UMVersion { get; set; } = string.Empty;


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
            string app_version = "1.0.0"; // Default version

            if (!File.Exists(version_path))
            {
                Program.Logger.Debug($"app_version.txt not found. Creating with default version \"{app_version}\".");
                File.WriteAllText(version_path, $"version=\"{app_version}\"");
            }

            string fileContent = File.ReadAllText(version_path);
            Match match = Regex.Match(fileContent, "\"(.+)\"");

            if (match.Success)
            {
                app_version = match.Groups[1].Value;
                Program.Logger.Debug($"App version parsed: {app_version}");
            }
            else
            {
                Program.Logger.Debug($"Failed to parse app version from '{fileContent}'. Using default version \"{app_version}\".");
            }

            AppVersion = app_version;
            return app_version;
        }

        string GetUnitMappersVersion()
        {
            string version_path = Directory.GetCurrentDirectory() + "\\um_version.txt";
            Program.Logger.Debug($"Reading unit mappers version from: {version_path}");
            string um_version = "1.0.0"; // Default version

            if (!File.Exists(version_path))
            {
                Program.Logger.Debug($"um_version.txt not found. Creating with default version \"{um_version}\".");
                File.WriteAllText(version_path, $"version=\"{um_version}\"");
            }

            string fileContent = File.ReadAllText(version_path);
            Match match = Regex.Match(fileContent, "\"(.+)\"");

            if (match.Success)
            {
                um_version = match.Groups[1].Value;
                Program.Logger.Debug($"Unit mappers version parsed: {um_version}");
            }
            else
            {
                Program.Logger.Debug($"Failed to parse unit mappers version from '{fileContent}'. Using default version \"{um_version}\".");
            }

            UMVersion = um_version;
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

        private string GetPreReleaseTag(string versionStr)
        {
            // Remove leading 'v' if present for consistent regex matching
            string processedVersion = versionStr.StartsWith("v") ? versionStr.Substring(1) : versionStr;
            Match match = Regex.Match(processedVersion, @"-(.+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        bool IsNewerVersion(string versionA, string versionB)
        {
            Program.Logger.Debug($"Comparing versions - A: '{versionA}', B: '{versionB}'");

            Version verA = ParseVersion(versionA);
            Version verB = ParseVersion(versionB);

            // 1. Compare main version numbers
            if (verB > verA)
            {
                Program.Logger.Debug($"  Main version B ({verB}) is newer than A ({verA}). B is newer.");
                return true;
            }
            if (verB < verA)
            {
                Program.Logger.Debug($"  Main version A ({verA}) is newer than B ({verB}). B is not newer.");
                return false;
            }

            // 2. If main versions are equal, compare pre-release tags
            Program.Logger.Debug($"  Main versions are equal ({verA}). Comparing pre-release tags.");
            string tagA = GetPreReleaseTag(versionA);
            string tagB = GetPreReleaseTag(versionB);

            // A stable version is always newer than a pre-release version
            if (tagA == null && tagB != null)
            {
                Program.Logger.Debug($"  A is stable, B is pre-release ('{tagB}'). A is newer. B is not newer.");
                return false; // A (stable) is newer than B (pre-release)
            }
            if (tagA != null && tagB == null)
            {
                Program.Logger.Debug($"  A is pre-release ('{tagA}'), B is stable. B is newer.");
                return true; // B (stable) is newer than A (pre-release)
            }

            // If both are stable or both are pre-release
            if (tagA == null && tagB == null)
            {
                Program.Logger.Debug($"  Both A and B are stable and equal. B is not newer.");
                return false; // Both are stable and equal
            }

            // Both have pre-release tags, compare them
            Program.Logger.Debug($"  Both A ('{tagA}') and B ('{tagB}') have pre-release tags. Comparing tags.");
            string[] partsA = tagA.Split('.');
            string[] partsB = tagB.Split('.');

            int length = Math.Min(partsA.Length, partsB.Length);
            for (int i = 0; i < length; i++)
            {
                string partA = partsA[i];
                string partB = partsB[i];

                bool isNumericA = int.TryParse(partA, out int numA);
                bool isNumericB = int.TryParse(partB, out int numB);

                if (isNumericA && isNumericB)
                {
                    // Both are numeric, compare numerically
                    if (numB > numA)
                    {
                        Program.Logger.Debug($"    Numeric part B ({numB}) > A ({numA}). B is newer.");
                        return true;
                    }
                    if (numB < numA)
                    {
                        Program.Logger.Debug($"    Numeric part A ({numA}) > B ({numB}). B is not newer.");
                        return false;
                    }
                }
                else if (isNumericA && !isNumericB)
                {
                    // Numeric has lower precedence than string
                    Program.Logger.Debug($"    Part A ('{partA}') is numeric, B ('{partB}') is string. B is newer.");
                    return true;
                }
                else if (!isNumericA && isNumericB)
                {
                    // String has higher precedence than numeric
                    Program.Logger.Debug($"    Part A ('{partA}') is string, B ('{partB}') is numeric. B is not newer.");
                    return false;
                }
                else
                {
                    // Both are strings, compare lexically
                    int comparison = string.Compare(partA, partB, StringComparison.OrdinalIgnoreCase);
                    if (comparison > 0)
                    {
                        Program.Logger.Debug($"    String part A ('{partA}') > B ('{partB}'). B is not newer.");
                        return false;
                    }
                    if (comparison < 0)
                    {
                        Program.Logger.Debug($"    String part B ('{partB}') > A ('{partA}'). B is newer.");
                        return true;
                    }
                }
            }

            // If all common parts are equal, the one with more parts is newer
            if (partsB.Length > partsA.Length)
            {
                Program.Logger.Debug($"  All common pre-release parts are equal. B has more parts. B is newer.");
                return true;
            }

            Program.Logger.Debug($"  Versions are considered equivalent or B is not newer.");
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

        private string GetUpdaterPath()
        {
            string primaryPath = @".\data\updater\CWUpdater.exe";
            string fallbackPath = @".\data\updater\CW-Updater.exe";

            if (File.Exists(primaryPath))
            {
                Program.Logger.Debug($"Found updater at: {primaryPath}");
                return primaryPath;
            }

            if (File.Exists(fallbackPath))
            {
                Program.Logger.Debug($"Updater not found at primary path. Using fallback: {fallbackPath}");
                return fallbackPath;
            }

            Program.Logger.Debug($"Updater executable not found at '{primaryPath}' or '{fallbackPath}'.");
            return null;
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
                    string updaterPath = GetUpdaterPath();
                    if (updaterPath == null)
                    {
                        throw new FileNotFoundException("Updater executable not found.");
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Arguments = $"\"{latestRelease.downloadUrl}\" \"{latestRelease.version}\"",
                        UseShellExecute = true
                    });
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
                    string updaterPath = GetUpdaterPath();
                    if (updaterPath == null)
                    {
                        throw new FileNotFoundException("Updater executable not found.");
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Arguments = $"\"{latestRelease.downloadUrl}\" \"{latestRelease.version}\" unit_mapper",
                        UseShellExecute = true
                    });
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
                            releaseUrl = $"https://github.com/{user}/{repoName}/releases/tag/{version}";
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
