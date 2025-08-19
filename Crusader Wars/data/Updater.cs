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

namespace Crusader_Wars
{
    public  class Updater
    {
        public  string AppVersion { get; set; }
        public string UMVersion { get; set; }


        private static readonly HttpClient client = new HttpClient();
        private const string LatestReleaseUrl = "https://api.github.com/repos/farayC/Crusader-Wars/releases/latest";
        private const string UnitMappersLatestReleaseUrl = "https://api.github.com/repos/farayC/CW-Mappers/releases/latest";
        private async Task<(bool IsUpdateAvailable, string DownloadUrl, string UpdateVersion)> CheckForUpdatesAsync(string currentVersion, string releaseUrl)
        {
            Program.Logger.Debug($"Checking for updates. Current version: {currentVersion}, URL: {releaseUrl}");
            if (currentVersion == string.Empty) {
                Program.Logger.Debug("Current version is empty, skipping update check.");
                return (false, null, null);
            }

            try
            {
                client.DefaultRequestHeaders.Add("User-Agent", "CW App Updater");
                string json = await client.GetStringAsync(releaseUrl);

                // Parse the JSON response
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;

                    // Get the latest version tag
                    string latestVersion = root.GetProperty("tag_name").GetString();
                    Program.Logger.Debug($"Latest version from GitHub: {latestVersion}");

                    // Get the download URL of the first asset
                    string downloadUrl = root.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    Program.Logger.Debug($"Download URL: {downloadUrl}");

                    if (IsMostRecentUpdate(currentVersion, latestVersion.TrimStart('v')))
                    {
                        Program.Logger.Debug("A more recent update is available.");
                        return (true, downloadUrl, latestVersion.TrimStart('v'));
                    }
                    else
                    {
                        Program.Logger.Debug("Application is up to date.");
                    }
                };
               
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error checking for updates: {ex.Message}");
            }

            return (false, null, null);
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

        bool IsMostRecentUpdate(string app_version, string github_version)
        {
            Program.Logger.Debug($"Comparing versions - App: {app_version}, GitHub: {github_version}");
            string[] AppComponents = app_version.Split('.');
            string[] ModComponents = github_version.Split('.');

            for (int i = 0; i < Math.Max(AppComponents.Length, ModComponents.Length); i++)
            {
                int v1 = i < AppComponents.Length ? int.Parse(AppComponents[i]) : 0;
                int v2 = i < ModComponents.Length ? int.Parse(ModComponents[i]) : 0;

                if (v2 > v1)
                {
                    Program.Logger.Debug("GitHub version is newer.");
                    return true;
                }
                else if (v1 > v2)
                {
                    Program.Logger.Debug("App version is newer or same.");
                    return false;
                }
            }
            Program.Logger.Debug("Versions are identical.");
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

        public async void CheckAppVersion()
        {
            Program.Logger.Debug("Checking app version...");
            if (HasInternetConnection())
            {
                Program.Logger.Debug("Internet connection detected.");
                var (isUpdateAvailable, downloadUrl, updateVersion) = await CheckForUpdatesAsync(GetAppVersion(), LatestReleaseUrl);
                if (isUpdateAvailable)
                {
                    Program.Logger.Debug($"Update available for app: {updateVersion}. Starting updater...");
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = @".\data\updater\CW-Updater.exe";
                        startInfo.Arguments = $"{downloadUrl} {updateVersion}";
                        Process.Start(startInfo);
                        Environment.Exit(0);
                    }
                    catch(Exception ex)
                    {
                        Program.Logger.Debug($"Failed to start updater: {ex.Message}");
                        return;
                    }
                }
            }
            else
            {
                Program.Logger.Debug("No internet connection detected.");
            }
        }

        public async void CheckUnitMappersVersion()
        {
            Program.Logger.Debug("Checking unit mappers version...");
            if (HasInternetConnection())
            {
                Program.Logger.Debug("Internet connection detected.");
                var (isUpdateAvailable, downloadUrl, updateVersion) = await CheckForUpdatesAsync(GetUnitMappersVersion(), UnitMappersLatestReleaseUrl);
                if (isUpdateAvailable)
                {
                    Program.Logger.Debug($"Update available for unit mappers: {updateVersion}. Starting updater...");
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = @".\data\updater\CW-Updater.exe";
                        startInfo.Arguments = $"{downloadUrl} {updateVersion} {"unit_mapper"}";
                        Process.Start(startInfo);
                        Environment.Exit(0);
                    }
                    catch(Exception ex)
                    {
                        Program.Logger.Debug($"Failed to start updater: {ex.Message}");
                        return;
                    }
                }
            }
            else
            {
                Program.Logger.Debug("No internet connection detected.");
            }
        }
    }

   
}
