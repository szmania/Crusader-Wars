using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace Crusader_Wars.data.attila_settings
{
    static class AttilaPreferences
    {

        static string preferences_file_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\The Creative Assembly\Attila\scripts\preferences.script.txt";
        static string user_script_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\The Creative Assembly\Attila\scripts\user.script.txt";

        public static bool ValidateUserScriptFile()
        {
            if (File.Exists(user_script_path))
            {
                string content = File.ReadAllText(user_script_path);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    string errorMessage = "Potential Mod Conflict Detected!\n\n" +
                                          "The file 'user.script.txt' contains custom scripts that may conflict with Crusader Wars.\n\n" +
                                          "To prevent issues, please clear the contents of this file.\n\n" +
                                          $"Path: {user_script_path}";
                    MessageBox.Show(errorMessage, "Crusader Wars: User Script Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Program.Logger.Debug($"User script file conflict detected at {user_script_path}.");
                    return false; // Indicates a problem
                }
            }
            Program.Logger.Debug("User script file is valid (empty or non-existent).");
            return true; // OK
        }
        public static void ChangeUnitSizes()
        {
            if(!isUnitsSetToUltra())
            {
                string new_data = "";
                using (FileStream attila_settings_file = File.Open(preferences_file_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(attila_settings_file))
                {
                    string line = "";
                    while (line != null && !reader.EndOfStream)
                    {
                        line = reader.ReadLine();

                        if (line.Contains("gfx_unit_size"))
                        {
                            line = Regex.Replace(line, @"gfx_unit_size (\d)", @"gfx_unit_size 3");
                        }

                        new_data += line + "\n";
                    }

                    reader.Close();
                    attila_settings_file.Close();
                }

                File.Create(preferences_file_path).Close();
                File.WriteAllText(preferences_file_path, new_data);
            }
            else
            {
                return;
            }

        }

        private static bool isUnitsSetToUltra()
        {

            if (!File.Exists(preferences_file_path)) return true;
            
            string unit_size_setting = "";
            using (FileStream attila_settings_file = File.Open(preferences_file_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(attila_settings_file))
            {
                string line = "";
                while (line != null && !reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    if (line.Contains("gfx_unit_size"))
                    {
                        unit_size_setting = line;
                        break;
                    }
                }

                reader.Close();
                attila_settings_file.Close();
            }

            Match isCorrect = Regex.Match(unit_size_setting, @"gfx_unit_size 3");
            if(isCorrect.Success) 
            {
                return true;
            }
            else 
            { 
                return false; 
            }

        }
    }
}
