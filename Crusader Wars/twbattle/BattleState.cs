using System.IO;
using IWshRuntimeLibrary;


namespace Crusader_Wars.twbattle
{
    public static class BattleState
    {
        public static string StateFile => @".\CW.lnk";

        public static bool IsBattleInProgress()
        {
            return File.Exists(StateFile);
        }

        public static void MarkBattleStarted()
        {
            // We use the existing shortcut as battle marker
            if (!File.Exists(StateFile))
            {
                CreateAttilaShortcut();
            }
        }

        public static void ClearBattleState()
        {
            if (File.Exists(StateFile))
            {
                File.Delete(StateFile);
            }
        }

        private static void CreateAttilaShortcut()
        {
            if(File.Exists(StateFile)) return;
            
            // Using existing shortcut creation logic...
            object shDesktop = (object)"Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = StateFile;
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Shortcut for continuing Crusader Wars battles";
            shortcut.WorkingDirectory = Properties.Settings.Default.VAR_attila_path.Replace(@"\Attila.exe", "");
            shortcut.Arguments = "used_mods_cw.txt";
            shortcut.TargetPath = Properties.Settings.Default.VAR_attila_path;
            shortcut.Save();
        }
    }
}
