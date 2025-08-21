using System.IO;
using IWshRuntimeLibrary;


namespace Crusader_Wars.twbattle
{
    public static class BattleState
    {
        private static string StateFolder => @".\data\attila_battle";
        private static string StateFile => Path.Combine(StateFolder, "battle_state.txt");
        private static string LogSnippetFile => Path.Combine(StateFolder, "battle_log_snippet.txt");

        static BattleState()
        {
            // Ensure directory exists
            if (!Directory.Exists(StateFolder))
            {
                Directory.CreateDirectory(StateFolder);
            }
        }

        public static bool IsBattleInProgress()
        {
            bool battleInProgress = System.IO.File.Exists(StateFile);
            return battleInProgress;
        }

        public static void MarkBattleStarted()
        {
            if (!System.IO.File.Exists(StateFile))
            {
                System.IO.File.WriteAllText(StateFile, "battle_in_progress");
            }
        }

        public static void ClearBattleState()
        {
            if (System.IO.File.Exists(StateFile))
            {
                System.IO.File.Delete(StateFile);
            }
            if (System.IO.File.Exists(LogSnippetFile))
            {
                System.IO.File.Delete(LogSnippetFile);
            }
        }

        public static void SaveLogSnippet(string logContent)
        {
            System.IO.File.WriteAllText(LogSnippetFile, logContent);
            Program.Logger.Debug($"Saved battle log snippet to: '{LogSnippetFile}'");
        }

        public static string LoadLogSnippet()
        {
            if (System.IO.File.Exists(LogSnippetFile))
            {
                Program.Logger.Debug($"Loading battle log snippet from: '{LogSnippetFile}'");
                return System.IO.File.ReadAllText(LogSnippetFile);
            }
            else
            {
                Program.Logger.Debug($"Battle log snippet not found at: '{LogSnippetFile}'");
                return null;
            }
        }
    }
}
