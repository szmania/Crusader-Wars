using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.client
{
    public class BattleReport
    {
        public SideReport AttackerSide { get; set; }
        public SideReport DefenderSide { get; set; }
        public string BattleResult { get; set; } // "Victory", "Defeat"
        public string SiegeResult { get; set; } // "Settlement Captured", "Successfully Defended", "N/A"
        public string WallDamage { get; set; } // "No Damage", "Damaged", "Breached", "N/A"

        // New Battle Details
        public string BattleName { get; set; }
        public string BattleDate { get; set; }
        public string LocationDetails { get; set; }
        public string ProvinceName { get; set; }
        public string TimeOfDay { get; set; }
        public string Season { get; set; }
        public string Weather { get; set; }
    }

    public class SideReport
    {
        public string SideName { get; set; } // "Attackers", "Defenders"
        public List<ArmyReport> Armies { get; set; } = new List<ArmyReport>();

        // New Summary Stats for the Side
        public int TotalDeployed { get; set; }
        public int TotalLosses { get; set; }
        public int TotalRemaining { get; set; }
        public int TotalKills { get; set; }
    }

    public class ArmyReport
    {
        public string ArmyName { get; set; } // e.g., "Army of Duke Antso III"
        public string CommanderName { get; set; }
        public List<UnitReport> Units { get; set; } = new List<UnitReport>();

        // New Summary Stats for the Army
        public int TotalDeployed { get; set; }
        public int TotalLosses { get; set; }
        public int TotalRemaining { get; set; }
        public int TotalKills { get; set; }
    }

    public class UnitReport
    {
        public string AttilaUnitName { get; set; }
        public int Deployed { get; set; }
        public int Remaining { get; set; }
        public int Kills { get; set; }

        // Calculated Losses property
        public int Losses => Math.Max(0, Deployed - Remaining);

        // Detailed Info
        public string Ck3UnitType { get; set; }
        public string AttilaUnitKey { get; set; }
        public string Ck3Heritage { get; set; }
        public string Ck3Culture { get; set; }
        public string AttilaFaction { get; set; }
        public List<CharacterReport> Characters { get; set; } = new List<CharacterReport>();
    }

    public class CharacterReport
    {
        public string Name { get; set; }
        public string Status { get; set; } // "Unharmed", "Wounded", "Slain", "Captured"
        public string Details { get; set; } // e.g., "Wounded (Severely Injured)", "Gained trait: Scarred"
    }
}
