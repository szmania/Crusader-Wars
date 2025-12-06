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
    }

    public class SideReport
    {
        public string SideName { get; set; } // "Attackers", "Defenders"
        public List<ArmyReport> Armies { get; set; } = new List<ArmyReport>();
    }

    public class ArmyReport
    {
        public string ArmyName { get; set; } // e.g., "Army of Duke Antso III"
        public string CommanderName { get; set; }
        public List<UnitReport> Units { get; set; } = new List<UnitReport>();
    }

    public class UnitReport
    {
        public string AttilaUnitName { get; set; }
        public int Deployed { get; set; }
        public int Losses { get; set; }
        public int Remaining { get; set; }
        public int Kills { get; set; }

        // Detailed Info
        public string Ck3UnitType { get; set; }
        public string AttilaUnitKey { get; set; }
        public List<CharacterReport> Characters { get; set; } = new List<CharacterReport>();
    }

    public class CharacterReport
    {
        public string Name { get; set; }
        public string Status { get; set; } // "Unharmed", "Wounded", "Slain", "Captured"
        public string Details { get; set; } // e.g., "Wounded (Severely Injured)", "Gained trait: Scarred"
    }
}
