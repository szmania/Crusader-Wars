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
using System.Windows.Forms; // Added for MessageBox
using CrusaderWars.client; // Added for ModOptions

namespace CrusaderWars
{
    public  class Updater
    {
        public  string AppVersion { get; set; } = string.Empty;
        public string UMVersion { get; set; } = string.Empty;
        private bool _updaterChecked = false;

        private static readonly HttpClient client = new HttpClient();
        // Changed to /releases endpoint and renamed
        private const string SzmaniaReleasesUrl = "https://api.github.com/repos/szmania/Crusader-Wars/releases";
        private const string SzmaniaUnitMappersReleasesUrl = "https://api.github.com/repos/szmania/CC-Mappers/releases";

        // Modified signature to return releaseApiUrl
        private async Task<(string? version, string? downloadUrl, string? releaseApiUrl)> GetLatestReleaseInfoAsync(string releasesUrl)
        {
            Program.Logger.Debug($"Getting latest release info from: {releasesUrl}");
            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "CW App Updater");
                string json = await client.GetStringAsync(releasesUrl);

                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Array)
                    {
                        Program.Logger.Debug($"Expected JSON array from {releasesUrl}, but got {root.ValueKind}.");
                        return (null, null, null);
                    }

                    bool optInPreReleases = ModOptions.GetOptInPreReleases();
                    JsonElement? targetRelease = null;

                    foreach (JsonElement release in root.EnumerateArray())
                    {
                        bool isPreRelease = release.GetProperty("prerelease").GetBoolean();
                        if (optInPreReleases || !isPreRelease)
                        {
                            targetRelease = release;
                            break; // Found the most recent suitable release (either pre-release if opted in, or first stable)
                        }
                    }

                    if (targetRelease.HasValue)
                    {
                        string? latestVersion = targetRelease.Value.GetProperty("tag_name").GetString();
                        string? releaseApiUrl = targetRelease.Value.GetProperty("url").GetString(); // API URL for this specific release

                        string? downloadUrl = null;
                        if (targetRelease.Value.TryGetProperty("assets", out JsonElement assets) && assets.EnumerateArray().Any())
                        {
                            downloadUrl = assets.EnumerateArray().First().GetProperty("browser_download_url").GetString();
                        }
                        
                        Program.Logger.Debug($"Found version: {latestVersion}, URL: {downloadUrl}, Release API URL: {releaseApiUrl} (Pre-release opt-in: {optInPreReleases})");
                        return (latestVersion, downloadUrl, releaseApiUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error getting release info from {releasesUrl}: {ex.Message}");
            }

            return (null, null, null);
        }

        public string GetAppVersion()
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

        public string GetUnitMappersVersion()
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

        private string? GetPreReleaseTag(string versionStr)
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
            string? tagA = GetPreReleaseTag(versionA);
            string? tagB = GetPreReleaseTag(versionB);

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
            string[] partsA = tagA!.Split('.');
            string[] partsB = tagB!.Split('.');

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

        // Modified signature to return releaseApiUrl
        private async Task<(string? version, string? downloadUrl, string? releaseApiUrl)> GetLatestReleaseFromReposAsync(string[] releaseUrls)
        {
            var releaseTasks = releaseUrls.Select(url => GetLatestReleaseInfoAsync(url)).ToArray();
            var releases = await Task.WhenAll(releaseTasks);

            var validReleases = releases.Where(r => r.version != null).ToList();

            if (!validReleases.Any())
            {
                Program.Logger.Debug("No releases found in any repository.");
                return (null, null, null);
            }

            // Aggregate to find the single latest release based on IsNewerVersion logic
            var latestRelease = validReleases.Aggregate((r1, r2) => IsNewerVersion(r1.version!, r2.version!) ? r2 : r1);
            return latestRelease;
        }

        private string? GetUpdaterPath()
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

        // Modified signature to accept releaseApiUrl
        private async Task<string?> GetAssetDownloadUrlAsync(string releaseApiUrl, string assetName)
        {
            Program.Logger.Debug($"Searching for asset '{assetName}' in release: {releaseApiUrl}");
            try
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "CW App Updater");
                string json = await client.GetStringAsync(releaseApiUrl); // Fetch single release object

                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;
                    JsonElement assets = root.GetProperty("assets");

                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string? name = asset.GetProperty("name").GetString();
                        if (name != null && name.Equals(assetName, StringComparison.OrdinalIgnoreCase))
                        {
                            string? downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            Program.Logger.Debug($"Found asset '{assetName}' with download URL: {downloadUrl}");
                            return downloadUrl;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error getting asset info from {releaseApiUrl}: {ex.Message}");
            }

            Program.Logger.Debug($"Asset '{assetName}' not found in release.");
            return null;
        }

        private async Task CheckForUpdaterUpdateAsync()
        {
            if (_updaterChecked) return;
            _updaterChecked = true;

            Program.Logger.Debug("Checking for updater self-update...");
            if (!HasInternetConnection())
            {
                Program.Logger.Debug("No internet connection, skipping updater self-update check.");
                return;
            }

            string? localUpdaterPath = GetUpdaterPath();
            if (string.IsNullOrEmpty(localUpdaterPath))
            {
                Program.Logger.Debug("Local updater not found, cannot perform self-update check.");
                return;
            }

            try
            {
                // Per user request, use the main app version for comparison, not the updater's file version.
                // The updater's version is tied to the main application release.
                string currentVersion = GetAppVersion();
                if (string.IsNullOrEmpty(currentVersion))
                {
                    Program.Logger.Debug("Could not determine current app version from app_version.txt. Aborting updater self-update.");
                    return;
                }
                Program.Logger.Debug($"Using current app version for updater comparison: {currentVersion}");

                // Use SzmaniaReleasesUrl (plural) and GetLatestReleaseInfoAsync to get the appropriate release based on opt-in
                var latestRelease = await GetLatestReleaseInfoAsync(SzmaniaReleasesUrl);
                if (string.IsNullOrEmpty(latestRelease.version) || string.IsNullOrEmpty(latestRelease.releaseApiUrl))
                {
                    Program.Logger.Debug("Could not fetch latest release version tag or API URL. Aborting self-update.");
                    return;
                }

                if (IsNewerVersion(currentVersion, latestRelease.version))
                {
                    Program.Logger.Debug($"A newer release ({latestRelease.version}) is available. Checking for updated updater asset.");

                    // Pass the specific release API URL to GetAssetDownloadUrlAsync
                    string? updaterDownloadUrl = await GetAssetDownloadUrlAsync(latestRelease.releaseApiUrl, "CWUpdater.exe");
                    if (string.IsNullOrEmpty(updaterDownloadUrl))
                    {
                        Program.Logger.Debug("Newer release found, but it does not contain a 'CWUpdater.exe' asset. Skipping self-update.");
                        return;
                    }

                    Program.Logger.Debug($"Downloading new updater from: {updaterDownloadUrl}");
                    string tempUpdaterPath = Path.Combine(Path.GetTempPath(), "CWUpdater_new.exe");

                    using (var httpClient = new HttpClient())
                    {
                        byte[] fileBytes = await httpClient.GetByteArrayAsync(updaterDownloadUrl);
                        File.WriteAllBytes(tempUpdaterPath, fileBytes);
                    }

                    // The release tag is newer, so we assume the asset is newer and replace it without checking its internal version.
                    Program.Logger.Debug($"Downloaded updater from release {latestRelease.version}. Replacing local updater.");
                    File.Copy(tempUpdaterPath, localUpdaterPath, true);
                    Program.Logger.Debug("Updater has been successfully updated.");

                    File.Delete(tempUpdaterPath);
                }
                else
                {
                    Program.Logger.Debug($"Current version ({currentVersion}) is up-to-date with latest release ({latestRelease.version}). No updater self-update needed.");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"An error occurred during updater self-update check: {ex.Message}");
            }
        }

        public async Task CheckAppVersion()
        {
            await CheckForUpdaterUpdateAsync();
            Program.Logger.Debug("Checking app version...");
            if (!HasInternetConnection())
            {
                Program.Logger.Debug("No internet connection detected.");
                return;
            }

            Program.Logger.Debug("Internet connection detected.");
            string currentVersion = GetAppVersion() ?? "1.0.0";
            if (string.IsNullOrEmpty(currentVersion))
            {
                Program.Logger.Debug("Current version is empty, skipping update check.");
                return;
            }

            // Use SzmaniaReleasesUrl (plural)
            string[] appReleaseUrls = { SzmaniaReleasesUrl };
            var latestRelease = await GetLatestReleaseFromReposAsync(appReleaseUrls);

            if (latestRelease.version == null) return;

            string? updaterPath = null; // Declare updaterPath here

            if (IsNewerVersion(currentVersion, latestRelease.version))
            {
                // Check if this version has been skipped by the user
                string skippedVersionPath = @".\app_skipped_version.txt";
                if (File.Exists(skippedVersionPath))
                {
                    string skippedVersion = File.ReadAllText(skippedVersionPath).Trim();
                    if (skippedVersion.Equals(latestRelease.version, StringComparison.OrdinalIgnoreCase))
                    {
                        Program.Logger.Debug($"Update to version {latestRelease.version} was previously skipped. Bypassing update check.");
                        return; // Skip the update
                    }
                }

                Program.Logger.Debug($"Update available for app: {latestRelease.version}. Starting updater...");
                try
                {
                    updaterPath = GetUpdaterPath(); // Assign inside try block
                    if (updaterPath == null)
                    {
                        throw new FileNotFoundException("Updater executable not found.");
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Arguments = $"\"{latestRelease.downloadUrl}\" \"{currentVersion}\" \"{latestRelease.version}\"",
                        UseShellExecute = true
                    });
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Failed to start updater: {ex.Message}");
                    string fullUpdaterPath = updaterPath != null ? Path.GetFullPath(updaterPath) : "N/A";
                    MessageBox.Show(
                        $"The Crusader Conflicts updater failed to launch.\n\n" +
                        $"This is often caused by antivirus software blocking the executable.\n\n" +
                        $"Please try to manually run the updater from:\n" +
                        $"{fullUpdaterPath}\n\n" +
                        $"Error details: {ex.Message}",
                        "Crusader Conflicts: Updater Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    Environment.Exit(0); // Exit the application after notifying the user
                }
            }
            else
            {
                Program.Logger.Debug("Application is up to date.");
            }
        }

        public async Task CheckUnitMappersVersion()
        {
            await CheckForUpdaterUpdateAsync();
            Program.Logger.Debug("Checking unit mappers version...");
            if (!HasInternetConnection())
            {
                Program.Logger.Debug("No internet connection detected.");
                return;
            }

            Program.Logger.Debug("Internet connection detected.");
            string currentVersion = GetUnitMappersVersion() ?? "1.0.0";
            if (string.IsNullOrEmpty(currentVersion))
            {
                Program.Logger.Debug("Current unit mappers version is empty, skipping update check.");
                return;
            }

            // Use SzmaniaUnitMappersReleasesUrl (plural)
            string[] umReleaseUrls = { SzmaniaUnitMappersReleasesUrl };
            var latestRelease = await GetLatestReleaseFromReposAsync(umReleaseUrls);

            if (latestRelease.version == null) return;

            string? updaterPath = null; // Declare updaterPath here

            if (IsNewerVersion(currentVersion, latestRelease.version))
            {
                // Check if this version has been skipped by the user
                string skippedVersionPath = @".\um_skipped_version.txt";
                if (File.Exists(skippedVersionPath))
                {
                    string skippedVersion = File.ReadAllText(skippedVersionPath).Trim();
                    if (skippedVersion.Equals(latestRelease.version, StringComparison.OrdinalIgnoreCase))
                    {
                        Program.Logger.Debug($"Update to unit mappers version {latestRelease.version} was previously skipped. Bypassing update check.");
                        return; // Skip the update
                    }
                }

                Program.Logger.Debug($"Update available for unit mappers: {latestRelease.version}. Starting updater...");
                try
                {
                    updaterPath = GetUpdaterPath(); // Assign inside try block
                    if (updaterPath == null)
                    {
                        throw new FileNotFoundException("Updater executable not found.");
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Arguments = $"\"{latestRelease.downloadUrl}\" \"{latestRelease.version}\" \"{currentVersion}\" unit_mapper",
                        UseShellExecute = true
                    });
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Failed to start updater: {ex.Message}");
                    string fullUpdaterPath = updaterPath != null ? Path.GetFullPath(updaterPath) : "N/A";
                    MessageBox.Show(
                        $"The Crusader Conflicts unit mappers updater failed to launch.\n\n" +
                        $"This is often caused by antivirus software blocking the executable.\n\n" +
                        $"Please try to manually run the updater from:\n" +
                        $"{fullUpdaterPath}\n\n" +
                        $"Error details: {ex.Message}",
                        "Crusader Conflicts: Unit Mappers Updater Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    Environment.Exit(0); // Exit the application after notifying the user
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
            string repoName = isUnitMapper ? "CC-Mappers" : "Crusader-Wars";
            string[] users = { "szmania" };

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
            string fallbackUrl = $"https://github.com/szmania/{repoName}/releases";
            Program.Logger.Debug($"Version tag not found in any user repo. Falling back to main releases page: {fallbackUrl}");
            return fallbackUrl;
        }
    }

   
}
