using System.Collections.Generic;
using System.Linq;
using CrusaderWars.data.save_file;

namespace CrusaderWars
{
    public static class BattleLog
    {
        public struct UnmappedUnitInfo : System.IEquatable<UnmappedUnitInfo>
        {
            public string ArmyID { get; set; }
            public string UnitName { get; set; }
            public string RegimentType { get; set; }
            public string Culture { get; set; }
            public string AttilaFaction { get; set; }

            public bool Equals(UnmappedUnitInfo other)
            {
                return UnitName == other.UnitName &&
                       RegimentType == other.RegimentType &&
                       Culture == other.Culture &&
                       AttilaFaction == other.AttilaFaction;
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + (UnitName?.GetHashCode() ?? 0);
                hash = hash * 23 + (RegimentType?.GetHashCode() ?? 0);
                hash = hash * 23 + (Culture?.GetHashCode() ?? 0);
                hash = hash * 23 + (AttilaFaction?.GetHashCode() ?? 0);
                return hash;
            }
        }

        private static Dictionary<string, List<string>> levyBreakdown = new Dictionary<string, List<string>>();
        private static List<UnmappedUnitInfo> unmappedUnits = new List<UnmappedUnitInfo>();

        public static void Reset()
        {
            levyBreakdown.Clear();
            unmappedUnits.Clear();
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

        public static void AddUnmappedUnit(Unit unit, string armyId)
        {
            unmappedUnits.Add(new UnmappedUnitInfo
            {
                ArmyID = armyId,
                UnitName = unit.GetName(),
                RegimentType = unit.GetRegimentType().ToString(),
                Culture = unit.GetCulture(),
                AttilaFaction = unit.GetAttilaFaction()
            });
        }

        public static List<UnmappedUnitInfo> GetUnmappedUnits()
        {
            return unmappedUnits;
        }

        public static bool HasUnmappedUnits()
        {
            return unmappedUnits.Any();
        }
    }
}
