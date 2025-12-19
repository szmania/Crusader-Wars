using CrusaderWars.armies.commander_traits;
using CrusaderWars.twbattle;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CrusaderWars.armies;
using CrusaderWars.data.save_file;
using CrusaderWars.unit_mapper;

namespace CrusaderWars
{
    public class Army
    {
        public string ID { get; set; }
        public Owner? Owner { get; private set; }
        public string? ArmyUnitID { get; set; }

        public List<Army> MergedArmies { get; private set; } = new List<Army>();
        // For field armies, this list is populated from the save file and converted into the Units list.
        public List<ArmyRegiment> ArmyRegiments { get; private set; } = new List<ArmyRegiment>();
        // Contains the final list of units for battle. For garrison armies, this list is populated directly, bypassing the ArmyRegiments structure.
        public List<Unit> Units { get; private set; } = new List<Unit>();
        public KnightSystem? Knights { get; private set; }
        public CommanderSystem? Commander { get; private set; }
        public DefensiveSystem? Defences { get; private set; }

        public string? CommanderID { get; set; }
        public bool isMainArmy { get; set; } // Changed to public set for easier modification in ArmiesReader
        bool IsPlayerArmy { get; set; }
        bool IsEnemyArmy { get; set; }
        public bool IsGarrisonArmy { get; private set; }
        public bool IsReinforcement { get; private set; } // New property for reinforcements

        public string? RealmName { get; set; }
        public string CombatSide { get; set; }
        public UnitsResults? UnitsResults { get; set; }
        public List<UnitCasualitiesReport> CasualitiesReports { get; private set; } = new List<UnitCasualitiesReport>();
        public Dictionary<string, int> SiegeEngines { get; set; } = new Dictionary<string, int>();


        public Army(string id, string combat_side, bool is_main)
        {
            ID = id;
            CombatSide = combat_side;
            isMainArmy = is_main;
            Owner = new Owner(string.Empty); // Initialize Owner to a default non-null value
            Knights = new KnightSystem(new List<Knight>(), 0); // Initialize Knights to a default non-null value
            IsGarrisonArmy = false; // Initialize new flag
            IsReinforcement = false; // Initialize new reinforcement flag
        }

        //Getters
        public bool IsEnemy() { return IsEnemyArmy; }
        public bool IsPlayer() { return IsPlayerArmy; }
        public bool IsGarrison() { return IsGarrisonArmy; }
        public bool IsReinforcementArmy() { return IsReinforcement; } // New getter for reinforcement


        //Setters
        public void AddMergedArmy(Army army) { 
            MergedArmies.Add(army); 
        }
        public void IsPlayer(bool u) { 
            IsPlayerArmy = u;
            if (u) IsEnemyArmy = false;
        }
        public void IsEnemy(bool u) { 
            IsEnemyArmy = u;
            if (u) IsPlayerArmy = false;
        }
        public void SetUnits(List<Unit> l) { Units = l; }
        public void SetCommander(CommanderSystem l) { Commander = l; }
        public void SetDefences(DefensiveSystem l) { Defences = l; }
        public void SetIsGarrison(bool isGarrison) { IsGarrisonArmy = isGarrison; }
        public void SetAsReinforcement(bool isReinforcement) { IsReinforcement = isReinforcement; } // New setter for reinforcement

        public void SetOwner(string id) {

            if (id == CK3LogData.LeftSide.GetMainParticipant().id)
                Owner = new Owner(id, new Culture(CK3LogData.LeftSide.GetMainParticipant().culture_id));
            else if (id == CK3LogData.RightSide.GetMainParticipant().id)
                Owner = new Owner(id, new Culture(CK3LogData.RightSide.GetMainParticipant().culture_id));
            else
                Owner = new Owner(id);
        }
        public void SetOwner(Owner owner) { this.Owner = owner; }
        public void SetArmyRegiments(List<ArmyRegiment> list) { ArmyRegiments = list; }
        public void SetKnights(KnightSystem knights){ Knights = knights; }
        public void SetCasualitiesReport(List<UnitCasualitiesReport> reports) { CasualitiesReports = reports; } 
        public void ClearNullArmyRegiments()
        {
            for (int i = 0; i < ArmyRegiments.Count; i++)
            {
                if (ArmyRegiments[i].Regiments is null)
                {
                    ArmyRegiments.Remove(ArmyRegiments[i]);
                }
            }
        }

        public void RemoveGarrisonRegiments()
        {
            this.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments).ToList().RemoveAll(x => x.IsGarrison());
            this.ArmyRegiments.RemoveAll(x => (x.Regiments == null || x.Regiments.Count == 0) && x.Type == RegimentType.Levy);
        }
        

        
        public int GetTotalSoldiers()
        {
            // If Units list is populated, we're in the final battle composition stage
            if (Units != null && Units.Any())
            {
                return Units.Sum(u => u.GetSoldiers());
            }
            
            // If we're dealing with a garrison army
            if (IsGarrison())
            {
                if (Units == null) return 0;
                return Units.Sum(u => u.GetOriginalSoldiers());
            }
            
            // For non-garrison armies in initial strength calculation
            if (ArmyRegiments == null) return 0;
            
            // For non-garrison armies, calculate initial strength.
            // StartingNum is from Combats.txt (field battles/relief sieges) and is the most accurate.
            // CurrentNum is from ArmyRegiments.txt and is the fallback for simple sieges.
            return ArmyRegiments.Sum(ar => ar.StartingNum > 0 ? ar.StartingNum : ar.CurrentNum);
        }
        
        public int GetTotalDeployed()
        {
            return GetTotalSoldiers();
        }
        
        public int GetTotalRemaining()
        {
            if (IsGarrison())
            {
                if (Units == null) return 0;
                return Units.Sum(u => u.GetSoldiers());
            }
            else
            {
                if (ArmyRegiments == null) return 0;
                return ArmyRegiments.Sum(ar => ar.CurrentNum);
            }
        }

        public void ScaleUnits(int ratio)
        {
            if (ratio > 0)
            {
                foreach (var unit in Units)
                {
                    if (unit.GetRegimentType() == RegimentType.Knight || unit.GetRegimentType() == RegimentType.Commander) continue;

                    double porcentage = (double)ratio / 100;
                    double num_ratio = unit.GetSoldiers() * porcentage;
                    num_ratio = Math.Round(num_ratio);
                    unit.ChangeSoldiers((int)num_ratio);
                }

                Program.Logger.Debug("Army scaled by " + (double)ratio + '%');
            }

        }

        public void RemoveNullUnits()
        {
            var ascending_list = Units.OrderBy(x => x.GetSoldiers()).ToList();
            if (ascending_list == null || ascending_list.Count < 1)
                return;

            var major_levy_culture = ascending_list[0];
            int total_soldiers = 0;
            for(int i = 0; i< Units.Count;i++)
            {
                var unit = Units[i];
                if(unit.GetObjCulture() == null)
                {
                    int soldiers = unit.GetSoldiers();
                    total_soldiers += soldiers;
                    Units.Remove(unit);
                }
            }

            foreach(var unit in Units)
            {
                if(major_levy_culture == unit)
                {
                    unit.ChangeSoldiers(unit.GetSoldiers()+total_soldiers);
                }
            }
        }

        public void PrintUnits()
        {
           
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"ARMY - {ID} | {CombatSide}");
            Program.Logger.Debug($"ARMY - {ID} | {CombatSide}");

            if(Commander != null)
            {
                string commanderFaction = UnitMappers_BETA.GetAttilaFaction(Commander.GetCultureName(), Commander.GetHeritageName());
                var commanderUnit = Units.FirstOrDefault(u => u.GetRegimentType() == RegimentType.Commander);
                string commanderKey = commanderUnit?.GetAttilaUnitKey() ?? "not_found";
                sb.AppendLine($"## GENERAL | Name: {Commander.Name} | Soldiers: {Commander.GetUnitSoldiers()} | NobleRank: {Commander.Rank} | ArmyXP: +{Commander.GetUnitsExperience()} | Culture: {Commander.GetCultureName()} | Heritage: {Commander.GetHeritageName()} | Attila Faction: {commanderFaction} | Attila unit Key: {commanderKey}");
                Program.Logger.Debug($"GENERAL | Name: {Commander.Name} | Soldiers: {Commander.GetUnitSoldiers()} | NobleRank: {Commander.Rank} | ArmyXP: +{Commander.GetUnitsExperience()} | Culture: {Commander.GetCultureName()} | Heritage: {Commander.GetHeritageName()} | Attila Faction: {commanderFaction} | Attila unit Key: {commanderKey}");
            }
            if (Knights?.GetKnightsList() != null)
            {
                foreach (var knight in Knights.GetKnightsList())
                {
                    string knightFaction = UnitMappers_BETA.GetAttilaFaction(knight.GetCultureName(), knight.GetHeritageName());
                    var knightUnit = Units.FirstOrDefault(u => u.GetRegimentType() == RegimentType.Knight && u.GetName() == knight.GetName());
                    string knightKey = knightUnit?.GetAttilaUnitKey() ?? "not_found";
                    if(knight.IsAccolade())
                    {
                        sb.AppendLine($"## ACCOLADE | Name: {knight.GetName()} | Soldiers: {knight.GetSoldiers()} | Culture: {knight.GetCultureName()} | Heritage: {knight.GetHeritageName()} | Attila Faction: {knightFaction} | Attila unit Key: {knightKey}");
                        Program.Logger.Debug($"ACCOLADE | Name: {knight.GetName()} | Soldiers: {knight.GetSoldiers()} | Culture: {knight.GetCultureName()} | Heritage: {knight.GetHeritageName()} | Attila Faction: {knightFaction} | Attila unit Key: {knightKey}");
                    }
                    else
                    {
                        sb.AppendLine($"## KNIGHT | Name: {knight.GetName()} | Soldiers: {knight.GetSoldiers()} | Culture: {knight.GetCultureName()} | Heritage: {knight.GetHeritageName()} | Attila Faction: {knightFaction} | Attila unit Key: {knightKey}");
                        Program.Logger.Debug($"KNIGHT | Name: {knight.GetName()} | Soldiers: {knight.GetSoldiers()} | Culture: {knight.GetCultureName()} | Heritage: {knight.GetHeritageName()} | Attila Faction: {knightFaction} | Attila unit Key: {knightKey}");
                    }
                }
            }
            
            // Filter out character units to prevent double logging
            foreach (var unit in Units.Where(u => u.GetRegimentType() != RegimentType.Commander && u.GetRegimentType() != RegimentType.Knight))
            {
                string unitName = unit.GetName();
                if(unit.GetRegimentType() == RegimentType.MenAtArms)
                {
                    unitName = $"MenAtArm - {unitName}";
                }

                string culture = unit?.GetObjCulture()?.GetCultureName() ?? "NULL_CULTURE";
                string heritage = unit?.GetObjCulture()?.GetHeritageName() ?? "NULL_HERITAGE";
                
                sb.AppendLine($"## {unitName} | Soldiers: {unit.GetSoldiers()} | " +
                    $"Culture: {culture} | Heritage: {heritage} | Attila Faction: {unit.GetAttilaFaction()} | " +
                    $"Attila unit Key: {unit.GetAttilaUnitKey()}");
                
                Program.Logger.Debug($"{unitName} | Soldiers: {unit.GetSoldiers()} | " +
                    $"Culture: {culture} | Heritage: {heritage} | Attila Faction: {unit.GetAttilaFaction()} | " +
                    $"Attila unit Key: {unit.GetAttilaUnitKey()}");
            }
            Program.Logger.Debug("");
            sb.AppendLine();

            File.AppendAllText(@".\data\battle.log", sb.ToString());
        }


    }

    enum CharacterType
    {
        MainCommander,
        Commander,
        Knight,
    }
    public class Character
    {
        // MAIN DATA
        string? ID { get; set; }
        string? Name { get; set; }
        int Prowess { get; set; }
        int Martial { get; set; }
        int FeudalRank {  get; set; }

        // SECUNDARY DATA
        Culture? CultureObj { get; set; }
        List<(int Index, string Key)>? Traits { get; set; }
        BaseSkills? BaseSkills { get; set; }

        // IDENTIFIER BOOLS
        CharacterType CharacterType { get; set; }
        bool isAccolade { get; set; }
        Accolade? Accolade { get; set; }

        //AFTER BATTLE DATA
        bool hasFallen { get; set; }
        int Kills { get; set; }
    }
}
