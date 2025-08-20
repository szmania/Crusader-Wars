using System.IO;
using IWshRuntimeLibrary;


namespace Crusader_Wars.twbattle
{
    public static class BattleState
    {
        private static string StateFolder => @".\data\attila_battle";
        private static string StateFile => Path.Combine(StateFolder, "battle_state.txt");
        
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
            return System.IO.File.Exists(StateFile);
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
        }
    }
}
