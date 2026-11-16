using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Linq;
using CrusaderWars.games;

namespace CrusaderWars
{
    public static class PackFile
    {


        public static void PackFileCreator()
        {
            PackFileCreator(CrusaderWars.unit_mapper.UnitMappers_BETA.ResolvedBattleGameType);
        }

        /// <summary>Creates the CrusaderConflicts.pack for the given game type.
        /// Uses the game-specific staging folder (data\attila_battle_files vs data\rome_2_battle_files),
        /// the RPFM --game flag (attila vs rome2), and moves the pack into that game's data folder.</summary>
        public static void PackFileCreator(string gameType)
        {
            Program.Logger.Debug($"Starting .pack file creation process for game '{gameType}'...");
            // Create and import .pack file

            bool isRome2 = gameType == "rome_2";
            string rpfmGame = isRome2 ? "rome_2" : "attila";
            string battleFilesDir = isRome2 ? @"\data\rome_2_battle_files" : @"\data\attila_battle_files";

            string create_path = Directory.GetCurrentDirectory() + @"\CrusaderConflicts.pack";
            string add_path = Directory.GetCurrentDirectory() + battleFilesDir;
            string? thumbnail_path = ResolveThumbnailPath(Directory.GetCurrentDirectory() + @"\settings");
            string tsv_path = Directory.GetCurrentDirectory() + (isRome2 ? @"\data\pack_file_schemas\schema_rom2.ron" : @"\data\pack_file_schemas\schema_att.ron");

            string create_command = $@"--game {rpfmGame} pack create -p ""{create_path}""";
            // ponytail: schema files are optional - omit -t when missing so RPFM does not warn/fail on it
            string schema_arg = File.Exists(tsv_path) ? " -t \"" + tsv_path + "\"" : "";
            if (schema_arg == "")
                Program.Logger.Debug($"RPFM schema file not found at {tsv_path}; adding files without -t schema.");
            string add_command = $@"--game {rpfmGame} pack add -p ""{create_path}""{schema_arg} -F ""{add_path}""";

            CreatePackFile(create_command);
            CreatePackFile(add_command);

            // Move the pack into the resolved game's data folder (game-agnostic via GameService).
            var gameService = GameServiceFactory.GetGameService(gameType);
            string pack_dir_path = gameService.GetModDataPath();
            string pack_to_move_path = create_path;
            string pack_file_path = Path.Combine(pack_dir_path, "CrusaderConflicts.pack");
            string thumb_file_path = Path.Combine(pack_dir_path, "CrusaderConflicts.png");

            if (File.Exists(pack_file_path))
            {
                File.Delete(pack_file_path);
            }
            File.Move(pack_to_move_path, pack_file_path);

            if (thumbnail_path != null && !File.Exists(thumb_file_path))
            {
                File.Copy(thumbnail_path, thumb_file_path);
            }
            Program.Logger.Debug($".pack file created and moved to {gameType} data folder successfully.");
        }

        /// <summary>Returns the first existing thumbnail in the settings folder
        /// (CrusaderConflicts.png, then CrusaderWars.png), or null if neither exists.</summary>
        private static string? ResolveThumbnailPath(string settings_dir)
        {
            foreach (var name in new[] { "CrusaderConflicts.png", "CrusaderWars.png" })
            {
                string candidate = Path.Combine(settings_dir, name);
                if (File.Exists(candidate)) return candidate;
            }
            Program.Logger.Debug($"No thumbnail found in {settings_dir} (tried CrusaderConflicts.png, CrusaderWars.png). Skipping thumbnail copy.");
            return null;
        }

        private static string? CreatePackFile(string command)
        {
            Program.Logger.Debug($"Executing RPFM command: {command}");
            string rpfm_client_path = @".\data\rpfm\rpfm_cli.exe";

            ProcessStartInfo procStartInfo = new ProcessStartInfo(rpfm_client_path, command)

            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true

            };

            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo = procStartInfo;
                    proc.Start();
                    string output = proc.StandardOutput.ReadToEnd();
                    Program.Logger.Debug($"RPFM output: {output}");
                    return output;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error creating pack file: {ex.Message}");
                return null;
            }
        }
    }
}
