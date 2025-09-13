using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Linq;

namespace CrusaderWars
{
    public static class PackFile
    {


        public static void PackFileCreator()
        {
            Program.Logger.Debug("Starting .pack file creation process...");
            // Create and import .pack file

            string create_path =Directory.GetCurrentDirectory() + @"\CrusaderConflicts.pack";
            string add_path = Directory.GetCurrentDirectory() + @"\data\battle files";
            string thumbnail_path = Directory.GetCurrentDirectory() + @"\settings\CrusaderWars.png";
            string tsv_path = Directory.GetCurrentDirectory() + @"\data\schema_att.ron";

            string create_command = $@"--game attila pack create -p ""{create_path}""";
            //string add_command = $@"--game attila pack add -p ""{create_path}"" -F ""{add_path}""";
            string add_command = $@"--game attila pack add -p ""{create_path}"" -t ""{tsv_path}"" -F ""{add_path}""";

            CreatePackFile(create_command);
            CreatePackFile(add_command);

            string pack_dir_path = Path.GetDirectoryName(Properties.Settings.Default.VAR_attila_path) + @"\data";
            string pack_to_move_path = create_path;
            string pack_file_path = pack_dir_path + @"\CrusaderConflicts.pack";
            string thumb_file_path = pack_dir_path + @"\CrusaderWars.png";

            if (File.Exists(pack_file_path))
            {
                File.Delete(pack_file_path);
            }
            File.Move(pack_to_move_path, pack_file_path);

            if(!File.Exists(thumb_file_path))
            {
                File.Copy(thumbnail_path, thumb_file_path);
            }
            Program.Logger.Debug(".pack file created and moved to Attila data folder successfully.");
        }

        private static string? CreatePackFile(string command)
        {
            Program.Logger.Debug($"Executing RPFM command: {command}");
            string rpfm_client_path =  @".\data\rpfm\rpfm_cli.exe";

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
