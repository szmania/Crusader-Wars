using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace CrusaderWars.data.attila_settings
{
    static class AttilaPreferences
    {

        static string preferences_file_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\The Creative Assembly\Attila\scripts\preferences.script.txt";
        static string user_script_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\The Creative Assembly\Attila\scripts\user.script.txt";

        public static void ValidateOnStartup()
        {
            if (File.Exists(user_script_path))
            {
                string content = File.ReadAllText(user_script_path);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    string errorMessage = "Potential Mod Conflict Detected!\n\n" +
                                          "The file 'user.script.txt' contains custom scripts that may conflict with Crusader Conflicts.\n\n" +
                                          "This can cause issues when launching a battle. It is recommended to clear this file before starting a battle.\n\n" +
                                          "Note: Crusader Conflicts has its own built-in mod manager for handling Attila mods.\n\n" +
                                          $"Path: {user_script_path}";
                    MessageBox.Show(errorMessage, "Crusader Conflicts: User Script Mod Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Program.Logger.Debug($"User script file mod conflict detected at {user_script_path} on startup.");
                }
            }
        }
        public static bool ValidateBeforeLaunch()
        {
            while (true) // Loop until the file is cleared or the user cancels
            {
                if (File.Exists(user_script_path))
                {
                    string content = File.ReadAllText(user_script_path);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        string errorMessage = "Potential Mod Conflict Detected!\n\n" +
                                              "The file 'user.script.txt' must be empty to launch Attila with Crusader Conflicts.\n\n" +
                                              "Please clear the contents of this file, then click 'Retry' to continue.\n\n" +
                                              "Click 'Cancel' to abort the launch.\n\n" +
                                              "Note: Crusader Conflicts has its own built-in mod manager. If you use an external one, save your profile first.\n\n" +
                                              $"Path: {user_script_path}";

                        DialogResult result = MessageBox.Show(errorMessage, "Crusader Conflicts: Action Required", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);

                        if (result == DialogResult.Cancel)
                        {
                            Program.Logger.Debug("User cancelled Attila launch due to user script conflict.");
                            return false; // User chose to cancel
                        }
                        // If they click Retry, the loop will run again, re-checking the file.
                    }
                    else
                    {
                        Program.Logger.Debug("User script file is valid (empty).");
                        return true; // File is empty, OK to proceed
                    }
                }
                else
                {
                    Program.Logger.Debug("User script file is valid (non-existent).");
                    return true; // File doesn't exist, OK to proceed
                }
            }
        }
        public static void ChangeUnitSizes()
        {
            if(!isUnitsSetToUltra())
            {
                string new_data = "";
                using (FileStream attila_settings_file = File.Open(preferences_file_path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(attila_settings_file))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
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
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
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
