using System.Collections.Generic;

namespace CrusaderWars
{
    public static class BattleLog
    {
        private static Dictionary<string, List<string>> levyBreakdown = new Dictionary<string, List<string>>();

        public static void Reset()
        {
            levyBreakdown.Clear();
        }

        public static void AddLevyLog(string armyId, string logLine)
        {
            if (!levyBreakdown.ContainsKey(armyId))
            {
                levyBreakdown.Add(armyId, new List<string>());
            }
            levyBreakdown[armyId].Add(logLine);
        }

        public static List<string> GetLevyBreakdown(string armyId)
        {
            if (levyBreakdown.ContainsKey(armyId))
            {
                return levyBreakdown[armyId];
            }
            return new List<string>();
        }
    }
}
