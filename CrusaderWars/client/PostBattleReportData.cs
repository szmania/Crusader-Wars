using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.client
{
    public class BattleReport
    {
        public SideReport AttackerSide { get; set; } = new SideReport();
        public SideReport DefenderSide { get; set; } = new SideReport();
        public string BattleResult { get; set; } = string.Empty; // "Victory", "Defeat"
        public string SiegeResult { get; set; } = string.Empty; // "Settlement Captured", "Successfully Defended", "N/A"
        public string WallDamage { get; set; } = string.Empty; // "No Damage", "Damaged", "Breached", "N/A"
        public double? WarScoreValue { get; set; }

        // New Battle Details
        public string BattleName { get; set; } = string.Empty;
        public string BattleDate { get; set; } = string.Empty;
        public string LocationDetails { get; set; } = string.Empty;
        public string ProvinceName { get; set; } = string.Empty;
        public string TimeOfDay { get; set; } = string.Empty;
        public string Season { get; set; } = string.Empty;
        public string Weather { get; set; } = string.Empty;
    }

    public class SideReport
    {
        public string SideName { get; set; } = string.Empty; // "Attackers", "Defenders"
        public List<ArmyReport> Armies { get; set; } = new List<ArmyReport>();

        // Changed from computed properties to regular properties with backing fields
        public int TotalDeployed { get; set; }
        public int TotalLosses { get; set; }
        public int TotalRemaining { get; set; }
        public int TotalKills { get; set; }

        // Add consistency check
        public bool IsConsistent => TotalLosses == TotalKills;
    }

    public class SiegeEngineReport
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ArmyReport
    {
        public string ArmyName { get; set; } = string.Empty; // e.g., "Army of Duke Antso III"
        public string CommanderName { get; set; } = string.Empty;
        public List<UnitReport> Units { get; set; } = new List<UnitReport>();
        public List<SiegeEngineReport> SiegeEngines { get; set; } = new List<SiegeEngineReport>();

        // New Summary Stats for the Army
        public int TotalDeployed { get; set; }
        public int TotalLosses { get; set; }
        public int TotalRemaining { get; set; }
        public int TotalKills { get; set; }
    }

    public class UnitReport
    {
        public string AttilaUnitName { get; set; } = string.Empty;
        public int Deployed { get; set; }
        public int Remaining { get; set; }
        public int Kills { get; set; }
        public int Losses { get; set; } // Changed from computed property to regular property

        // Ensure losses are always non-negative
        public int GetLosses() => Math.Max(0, Deployed - Remaining);

        // Detailed Info
        public string Ck3UnitType { get; set; } = string.Empty;
        public string AttilaUnitKey { get; set; } = string.Empty;
        public string Ck3Heritage { get; set; } = string.Empty;
        public string Ck3Culture { get; set; } = string.Empty;
        public string AttilaFaction { get; set; } = string.Empty;
        public string Script { get; set; } = string.Empty;
        public List<CharacterReport> Characters { get; set; } = new List<CharacterReport>();

        // New property for combined knight unit details
        public List<KnightDetailReport> KnightDetails { get; set; } = new List<KnightDetailReport>();

        // Additional fields for better unit information
        public int Rank { get; set; } = 0; // For Commander and Knight units
        public int GarrisonLevel { get; set; } = 0; // For Garrison units

        // Siege engine properties
        public bool IsSiegeUnit { get; set; }
        public int DeployedMachines { get; set; }
        public int RemainingMachines { get; set; }
        public int MachineLosses { get; set; }
    }

    public class CharacterReport
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Unharmed", "Wounded", "Slain", "Captured"
        public string Details { get; set; } = string.Empty; // e.g., "Wounded (Severely Injured)", "Gained trait: Scarred"
    }

    // New class to hold individual knight details for the combined unit
    public class KnightDetailReport
    {
        public string Name { get; set; } = string.Empty;
        public int BodyguardSize { get; set; }
        public int Kills { get; set; }
        public bool Fallen { get; set; }
        public string Status { get; set; } = string.Empty; // "Unharmed", "Wounded", "Slain", "Captured"
    }
}
