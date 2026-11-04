using System;
using System.IO;
using CrusaderWars.Properties;

namespace CrusaderWars.games
{
    public class Rome2GameService : GameService
    {
        public override string GetProcessName() => "Rome2";
        public override string GetExecutableName() => "Rome2.exe";
public override string GetGameType() => "Rome2";

        public override string GetBattleFilesPath() => Path.Combine(Path.GetDirectoryName(GetGamePath())!, "data\\rome_2_battle_files");
        public override string GetScriptPath() => Path.Combine(Path.GetDirectoryName(GetGamePath())!, "data\\rome_2_battle_files\\script\\tut_tutorial_battle");
        public override string GetBattleTemplatePath() => Path.Combine(Path.GetDirectoryName(GetGamePath())!, "data\\rome_2_battle_files\\script");

        public override string GetLogPath()
        {
            string rome2Path = Settings.Default.VAR_rome2_path;
            if (string.IsNullOrEmpty(rome2Path) || !rome2Path.Contains("Rome2.exe"))
                return string.Empty;

            return rome2Path.Substring(0, rome2Path.IndexOf("Rome2.exe")) + "data\\BattleResults_log.txt";
        }

        public override string GetModDataPath()
        {
            string rome2Path = Settings.Default.VAR_rome2_path;
            if (string.IsNullOrEmpty(rome2Path))
                return string.Empty;

            return rome2Path.Replace(@"\Rome2.exe", @"\data");
        }

        public override string GetWorkshopPath()
        {
            // Steam App ID for Rome 2 is 214950
            string steamAppsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam", "steamapps");

            if (!Directory.Exists(steamAppsPath))
            {
                // Try alternate Steam library locations
                string rome2Path = Settings.Default.VAR_rome2_path;
                if (!string.IsNullOrEmpty(rome2Path))
                {
                    // Walk up from the game directory to find steamapps
                    string? dir = Path.GetDirectoryName(rome2Path);
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

            return Path.Combine(steamAppsPath, "workshop", "content", "214950");
        }

        public override string GetModActivationFileName() => "used_mods_cc.txt";

        public override string GetShortcutArguments() => "used_mods_cc.txt";

        protected override string GetGamePath() => client.ModOptions.GetGamePath("Rome2");
    
    }
}