using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CrusaderWars.client.LinuxSetup.Services
{
    public class SteamManager : ISteamManager
    {
        private readonly ILinuxEnvironmentDetector _linuxEnv;
        private const string ATTILA_APP_ID = "325610";

        public SteamManager(ILinuxEnvironmentDetector linuxEnv)
        {
            _linuxEnv = linuxEnv;
        }

        public string? GetSteamPath()
        {
            return _linuxEnv.GetSteamPath();
        }

        public string? GetAttilaPath()
        {
            string? steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath)) return null;

            var libraryFolders = GetSteamLibraryFolders();
            foreach (var folder in libraryFolders)
            {
                string attilaPath = Path.Combine(folder, "steamapps", "common", "Total War Attila");
                if (Directory.Exists(attilaPath))
                {
                    return attilaPath;
                }
            }

            return null;
        }

        public string? GetWorkshopModsPath()
        {
            string? attilaPath = GetAttilaPath();
            if(string.IsNullOrEmpty(attilaPath)) return null;

            // Workshop mods are usually in a steamapps/workshop/content/{appid} folder in one of the library folders.
            var libraryFolders = GetSteamLibraryFolders();
            foreach (var folder in libraryFolders)
            {
                string workshopPath = Path.Combine(folder, "steamapps", "workshop", "content", ATTILA_APP_ID);
                if (Directory.Exists(workshopPath))
                {
                    return workshopPath;
                }
            }
            
            return null;
        }

        public Task<bool> SetLaunchOptions(string gameId, string launchOptions)
        {
            // Programmatically setting launch options is difficult and risky.
            // The wizard will provide clear instructions for the user to do it manually.
            Program.Logger.Debug($"Instruction for user: Set launch options for game {gameId} to '{launchOptions}' in Steam.");
            return Task.FromResult(true);
        }

        private List<string> GetSteamLibraryFolders()
        {
            var folders = new List<string>();
            string? steamPath = GetSteamPath();
            if (string.IsNullOrEmpty(steamPath)) return folders;

            folders.Add(steamPath); // The main steam installation folder is a library folder

            string libraryFoldersVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFoldersVdf)) return folders;

            try
            {
                string content = File.ReadAllText(libraryFoldersVdf);
                // Use regex to find all paths. This is a simplified parser for Valve's VDF format.
                var matches = Regex.Matches(content, @"\""path\""\s+\""(.+)\""");
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string path = match.Groups[1].Value.Replace(@"\\", @"\");
                        if (Directory.Exists(path))
                        {
                            folders.Add(path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Could not parse libraryfolders.vdf: {ex.Message}");
            }

            return folders;
        }
    }
}
