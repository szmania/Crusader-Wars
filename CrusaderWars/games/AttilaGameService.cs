using System;
using System.IO;
using CrusaderWars.Properties;

namespace CrusaderWars.games
{
    public class AttilaGameService : GameService
    {
        public override string GetProcessName() => "Attila";
        public override string GetExecutableName() => "Attila.exe";
        public override string GetGameType() => "Attila";

        public override string GetLogPath()
        {
            string attilaPath = Settings.Default.VAR_attila_path;
            if (string.IsNullOrEmpty(attilaPath) || !attilaPath.Contains("Attila.exe"))
                return string.Empty;

            return attilaPath.Substring(0, attilaPath.IndexOf("Attila.exe")) + "data\\BattleResults_log.txt";
        }

        public override string GetModDataPath()
        {
            string attilaPath = Settings.Default.VAR_attila_path;
            if (string.IsNullOrEmpty(attilaPath))
                return string.Empty;

            return attilaPath.Replace(@"\Attila.exe", @"\data");
        }

        public override string GetWorkshopPath()
        {
            // Steam App ID for Attila is 325610
            string steamAppsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam", "steamapps");

            if (!Directory.Exists(steamAppsPath))
            {
                // Try alternate Steam library locations
                string attilaPath = Settings.Default.VAR_attila_path;
                if (!string.IsNullOrEmpty(attilaPath))
                {
                    // Walk up from the game directory to find steamapps
                    string? dir = Path.GetDirectoryName(attilaPath);
                    while (dir != null)
                    {
                        string potentialSteamApps = Path.Combine(dir, "steamapps");
                        if (Directory.Exists(potentialSteamApps))
                        {
                            steamAppsPath = potentialSteamApps;
                            break;
                        }
                        dir = Path.GetDirectoryName(dir);
                    }
                }
            }

            return Path.Combine(steamAppsPath, "workshop", "content", "325610");
        }

        public override string GetModActivationFileName() => "used_mods_cc.txt";

        protected override string GetGamePath() => client.ModOptions.GetGamePath("Attila");
    
    }
}