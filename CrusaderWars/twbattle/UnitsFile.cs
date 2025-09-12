using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using CrusaderWars.armies;
using CrusaderWars.armies.commander_traits;
using CrusaderWars.client;
using CrusaderWars.data.save_file;
using CrusaderWars.terrain;
using CrusaderWars.unit_mapper;

namespace CrusaderWars
{
    public static class UnitsFile
    {
        public static int MAX_LEVIES_UNIT_NUMBER = ModOptions.GetLevyMax();
        public static int MAX_CAVALRY_UNIT_NUMBER = ModOptions.GetCavalryMax();
        public static int MAX_INFANTRY_UNIT_NUMBER = ModOptions.GetInfantryMax();
        public static int MAX_RANGED_UNIT_NUMBER = ModOptions.GetRangedMax();

        public static CommanderTraits? PlayerCommanderTraits;
        public static CommanderTraits? EnemyCommanderTraits;

        private static HashSet<string> processedArmyIDs = new HashSet<string>();

        public static void ResetProcessedArmies()
        {
            processedArmyIDs.Clear();
        }

        public static (int UnitSoldiers, int UnitNum, int SoldiersRest) RetriveCalculatedUnits(int soldiers, int unit_limit)
        {
            //if it's a special unit like siege equipement, monsters, etc...
            if (unit_limit == 1111)
            {
                int special_num = soldiers / 10; //10, because siege equipement is 10 persons one equipement
                return (special_num, 1, 0);
            }

            int unit_num;
            for (int i = 1; i <= soldiers; i++)
            {
                int result = soldiers / i;

                if (result <= unit_limit)
                {
                    unit_num = i;
                    int rest = soldiers % i;

                    return (result, unit_num, rest);
                }

            }

            return (0, 0, 0);
        }

        static int i;
        public static void BETA_ConvertandAddArmyUnits(Army army)
        {
            if (processedArmyIDs.Contains(army.ID))
            {
                Program.Logger.Debug($"BETA_ConvertandAddArmyUnits: Army {army.ID} has already been processed (likely as a merged army). Skipping.");
                return;
            }

            // Process the parent army
            BETA_AddArmyUnits(army);
            processedArmyIDs.Add(army.ID);

            // Process merged armies
            if (army.MergedArmies != null)
            {
                foreach (Army merged_army in army.MergedArmies)
                {
                    if (processedArmyIDs.Contains(merged_army.ID))
                    {
                        Program.Logger.Debug($"BETA_ConvertandAddArmyUnits: Merged army {merged_army.ID} was already processed. This is unexpected but handled. Skipping.");
                        continue;
                    }
                    BETA_AddArmyUnits(merged_army);
                    processedArmyIDs.Add(merged_army.ID);
                }
            }
        }

        public static CommanderTraits? GetCommanderTraitsObj(bool isPlayer)
        {
            if (isPlayer && PlayerCommanderTraits != null)
            {
                return PlayerCommanderTraits;
            }
            else if (!isPlayer && EnemyCommanderTraits != null)
            {
                return EnemyCommanderTraits;
            }
            return null;
        }

        static int GetTraitsXP(bool isPlayer,string combatSide, string terrainType, bool isRiverCrossing, bool isHostileFaith, bool isWinter)
        {
            var commander_traits =  GetCommanderTraitsObj(isPlayer);
            if (commander_traits != null)
                return commander_traits.GetBenefits(combatSide, terrainType, isRiverCrossing, isHostileFaith, isWinter);

            return 0;
        }


        static void BETA_AddArmyUnits(Army army)
        {
            Program.Logger.Debug($"BETA_AddArmyUnits: Processing army {army.ID} ({army.CombatSide}) with {army.Units.Count} units.");
            
            if (army.Owner is null)
            {
                Program.Logger.Debug($"  - WARNING: Army {army.ID} has no owner. Skipping unit processing.");
                return;
            }

            army.RemoveNullUnits();

            i = 0;
            int modifiers_xp = 0;
            int traits_xp = 0;
            int army_xp = 0;

            if (TerrainGenerator.isStrait || TerrainGenerator.isRiver && army.CombatSide == "attacker") { modifiers_xp -= 2; }

            //##################
            //                 #
            //    COMMANDER    #
            //                 #
            //##################
            int commander_army_xp = 0;
            if (army.Commander != null)
            {
                var commander = army.Commander;
                commander_army_xp = commander.GetUnitsExperience();
                int commander_xp = commander.GetCommanderExperience();
                int commander_soldiers = commander.GetUnitSoldiers();
                
                Unit commander_unit = new Unit("General", commander_soldiers, commander.GetCultureObj(), RegimentType.Commander, false, army.Owner);
                commander_unit.SetAttilaFaction(UnitMappers_BETA.GetAttilaFaction(commander.GetCultureName(), commander.GetHeritageName()));
                commander_unit.SetUnitKey(UnitMappers_BETA.GetUnitKey(commander_unit));

                string commanderAttilaKey = commander_unit.GetAttilaUnitKey();
                if (string.IsNullOrEmpty(commanderAttilaKey) || commanderAttilaKey == UnitMappers_BETA.NOT_FOUND_KEY)
                {
                    Program.Logger.Debug($"  - WARNING: Could not map Commander unit for army {army.ID}. It will be dropped from the battle.");
                    BattleLog.AddUnmappedUnit(commander_unit, army.ID);
                }
                else
                {
                    army.Units.Insert(0, commander_unit);
                    string general_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPEcommander{commander.ID}_CULTURE{commander.GetCultureObj()?.ID ?? "unknown"}_";
                    BattleFile.AddGeneralUnit(commander, commanderAttilaKey, general_script_name, commander_xp, Deployments.beta_GeDirection(army.CombatSide));
                    i++;
                }
            }


            //##################
            //                 #
            //     KNIGHTS     #
            //                 #
            //##################
            if (army.Knights != null && army.Knights?.GetKnightsList()?.Count > 0)
            {
                Unit knights_unit;
                if (army.Knights.GetMajorCulture() != null)
                    // Set major culture on the knights unit
                    knights_unit = new Unit("Knight", army.Knights.GetKnightsSoldiers(), army.Knights.GetMajorCulture(), RegimentType.Knight,false, army.Owner);
                else
                    // Set owner culture if it doesn't have a major culture
                    knights_unit = new Unit("Knight", army.Knights.GetKnightsSoldiers(), army.Owner.GetCulture(), RegimentType.Knight, false, army.Owner);


                knights_unit.SetAttilaFaction(UnitMappers_BETA.GetAttilaFaction(knights_unit.GetCulture(), knights_unit.GetHeritage()));
                knights_unit.SetUnitKey(UnitMappers_BETA.GetUnitKey(knights_unit));
                
                string knightAttilaKey = knights_unit.GetAttilaUnitKey();
                if (string.IsNullOrEmpty(knightAttilaKey) || knightAttilaKey == UnitMappers_BETA.NOT_FOUND_KEY)
                {
                    Program.Logger.Debug($"  - WARNING: Could not map Knights unit for army {army.ID}. It will be dropped from the battle.");
                    BattleLog.AddUnmappedUnit(knights_unit, army.ID);
                }
                else
                {
                    army.Units.Insert(1, knights_unit);

                    string knights_script_name;
                    if (army.Knights.GetMajorCulture() != null)
                        knights_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPEknights_CULTURE{army.Knights.GetMajorCulture()?.ID ?? "unknown"}_";
                    else
                        knights_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPEknights_CULTURE{army.Owner.GetCulture()?.ID ?? "unknown"}_";


                    BattleFile.AddKnightUnit(army.Knights, knightAttilaKey, knights_script_name, army.Knights.SetExperience(), Deployments.beta_GeDirection(army.CombatSide));
                    i++;
                }
            }


            //##################
            //     ARMY XP     #
            //##################
            if (army.IsPlayer()) { 
                traits_xp = GetTraitsXP(true, army.CombatSide, TerrainGenerator.TerrainType, TerrainGenerator.isRiver, false, Weather.HasWinter);
                modifiers_xp = CK3LogData.LeftSide.GetModifiers()?.GetXP() ?? 0;
            }
            else { 
                traits_xp = GetTraitsXP(false, army.CombatSide, TerrainGenerator.TerrainType, TerrainGenerator.isRiver, false, Weather.HasWinter);
                modifiers_xp = CK3LogData.RightSide.GetModifiers()?.GetXP() ?? 0;
            }

            army_xp += commander_army_xp;
            army_xp += modifiers_xp;
            army_xp += traits_xp;
            if (army_xp < 0) { army_xp = 0; }
            if (army_xp > 9) { army_xp = 9; }

                //##################
                //                 #
                //      LEVIES     #
                //                 #
                //##################
    
                var levies_units = army.Units.Where(item => item.GetRegimentType() == data.save_file.RegimentType.Levy).ToList();
                if (levies_units.Any())
                {
                    // Group levies by their target Attila faction to prevent duplicate generation
                    // if multiple cultures map to the same faction.
                    var levies_by_faction = levies_units.GroupBy(u => u.GetAttilaFaction());
    
                    foreach (var faction_group in levies_by_faction)
                    {
                        string factionName = faction_group.Key;
                        if (string.IsNullOrEmpty(factionName) || factionName == UnitMappers_BETA.NOT_FOUND_KEY)
                        {
                            int soldiers = faction_group.Sum(u => u.GetSoldiers());
                            Program.Logger.Debug($"WARNING - {soldiers} LEVY SOLDIERS WITHOUT A FACTION FOUND. SKIPPING.");
                            continue;
                        }
    
                        // Sum up all soldiers for this faction
                        int total_faction_levy_soldiers = faction_group.Sum(u => u.GetSoldiers());
    
                        // Create a representative levy unit for this faction group.
                        // We use the culture of the largest contributing unit for logging/script naming.
                        var representative_unit = faction_group.OrderByDescending(u => u.GetSoldiers()).First();
                        
                        // Determine the culture for the merged levy unit, falling back to the army owner's culture if needed.
                        var levyCulture = representative_unit.GetObjCulture();
                        if (levyCulture == null)
                        {
                            Program.Logger.Debug($"  - Levy representative unit for faction '{factionName}' has no culture. Falling back to owner's culture.");
                            levyCulture = army.Owner.GetCulture();
                        }

                        Unit merged_levy_unit = new Unit("Levy", total_faction_levy_soldiers, levyCulture, RegimentType.Levy, faction_group.Any(u => u.IsMerc()));
                        merged_levy_unit.SetAttilaFaction(factionName);
                        merged_levy_unit.SetMax(representative_unit.GetMax()); // Inherit max from representative
    
                        Program.Logger.Debug($"Processing levies for faction '{factionName}' with a total of {total_faction_levy_soldiers} soldiers.");
    
                        var levy_porcentages = UnitMappers_BETA.GetFactionLevies(factionName);
                        BETA_LevyComposition(merged_levy_unit, army, levy_porcentages, army_xp);
                    }
                }

            //##################
            //                 #
            //   MEN-AT-ARMS   #
            //                 #
            //##################
            foreach (var unit in army.Units)
            {
                string unitName = unit.GetName();
                //Skip if its not a Men at Arms Unit
                if (unitName == "General" || unit.GetRegimentType() == RegimentType.Knight || unit.GetRegimentType() == RegimentType.Levy) continue;

                string attilaUnitKey = unit.GetAttilaUnitKey();
                if (string.IsNullOrEmpty(attilaUnitKey) || attilaUnitKey == UnitMappers_BETA.NOT_FOUND_KEY)
                {
                    Program.Logger.Debug($"  - WARNING: Could not map CK3 Unit '{unit.GetName()}' (Type: {unit.GetRegimentType()}) for army {army.ID}. It will be dropped from the battle.");
                    BattleLog.AddUnmappedUnit(unit, army.ID);
                    continue; // Skip this unit
                }

                Program.Logger.Debug($"  Processing MAA unit: Name={unit.GetName()}, Soldiers={unit.GetSoldiers()}, Culture={unit.GetCulture()}");

                var MAA_Data = RetriveCalculatedUnits(unit.GetSoldiers(), unit.GetMax());

                if (unit.GetObjCulture() == null)
                {
                    var unitOwner = unit.GetOwner();
                    if (unitOwner != null)
                    {
                        unit.ChangeCulture(unitOwner.GetCulture());
                    }
                    else
                    {
                        unit.ChangeCulture(army.Owner.GetCulture());
                    }
                }

                //If is retinue maa, increase 2xp.
                if (unitName.Contains("accolade"))
                {
                    string unit_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPE{unit.GetName()}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                    int accolade_xp = army_xp + 2;
                    if (accolade_xp < 0) accolade_xp = 0;
                    if (accolade_xp > 9) accolade_xp = 9;
                    BattleFile.AddUnit(attilaUnitKey, MAA_Data.UnitSoldiers, MAA_Data.UnitNum, MAA_Data.SoldiersRest, unit_script_name, accolade_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                }
                //If is normal maa
                else
                {
                    string unit_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPE{unit.GetName()}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                    BattleFile.AddUnit(attilaUnitKey, MAA_Data.UnitSoldiers, MAA_Data.UnitNum, MAA_Data.SoldiersRest, unit_script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                }
                i++;


            }

            army.PrintUnits();
        }


        static void BETA_LevyComposition(Unit unit, Army army, List<(int porcentage, string unit_key, string name, string max)> faction_levy_porcentages, int army_xp)
        {
            if (faction_levy_porcentages == null || faction_levy_porcentages.Count < 1)
            {
                Program.Logger.Debug("ERROR - LEVIES WITHOUT FACTION IN UNIT" + $"\nNUMBER OF SOLDIERS:{unit.GetSoldiers()}" + $"\nATTILA FACTION:{unit.GetAttilaFaction()}");
                return;
            }

            // NEW: Filter out Men-At-Arms units from the levy pool
            var filtered_levy_porcentages = faction_levy_porcentages
                                                .Where(data => !data.unit_key.Contains("_MAA_"))
                                                .ToList();

            if (filtered_levy_porcentages.Any())
            {
                Program.Logger.Debug($"  BETA_LevyComposition ({army.CombatSide}): Filtered out MAA units from levy pool for faction '{unit.GetAttilaFaction()}'. Using filtered list.");
                faction_levy_porcentages = filtered_levy_porcentages;
            }
            else
            {
                Program.Logger.Debug($"  BETA_LevyComposition ({army.CombatSide}): WARNING: No non-MAA levy units found for faction '{unit.GetAttilaFaction()}'. Using original (unfiltered) list as fallback.");
                // Keep original faction_levy_porcentages as is (no change needed here as it's already the current value)
            }

            var Levies_Data = RetriveCalculatedUnits(unit.GetSoldiers(), unit.GetMax());

            int total_soldiers = unit.GetSoldiers();

            //  SINGULAR UNIT
            //  select random levy type
            if (unit.GetSoldiers() <= unit.GetMax())
            {
                Random r = new Random();
                var random = faction_levy_porcentages[r.Next(faction_levy_porcentages.Count)]; // Fixed: r.Next(count - 1) to r.Next(count)
                string script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPELevy{random.porcentage}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                BattleFile.AddUnit(random.unit_key, Levies_Data.UnitSoldiers, 1, Levies_Data.SoldiersRest, script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                
                string logLine = $"    - Levy Attila Unit: {random.unit_key}, Soldiers: {Levies_Data.UnitSoldiers} (1x unit of {Levies_Data.UnitSoldiers}), Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {unit.GetAttilaFaction()}";
                BattleLog.AddLevyLog(army.ID, logLine);

                i++;
                return;
            }


            //  MULTIPLE UNITS
            //  fulfill every levy type
            int levySoldiers = unit.GetSoldiers();

            int totalPercentageSum = faction_levy_porcentages.Sum(p => p.porcentage);
            if (totalPercentageSum <= 0)
            {
                Program.Logger.Debug($"  BETA_LevyComposition ({army.CombatSide}): WARNING: Total percentage sum for levies is 0 or less for faction '{unit.GetAttilaFaction()}'. No levy units will be generated.");
                return;
            }

            int assignedSoldiers = 0;
            for (int k = 0; k < faction_levy_porcentages.Count; k++)
            {
                var porcentageData = faction_levy_porcentages[k];
                int result;

                if (k < faction_levy_porcentages.Count - 1)
                {
                    double t = (double)porcentageData.porcentage / totalPercentageSum;
                    result = (int)Math.Round(levySoldiers * t);
                }
                else
                {
                    // Last unit gets the remainder to ensure total is correct
                    result = levySoldiers - assignedSoldiers;
                }

                if (result <= 0) continue;

                assignedSoldiers += result;

                var levy_type_data = RetriveCalculatedUnits(result, unit.GetMax());
                string logLine = $"    - Levy Attila Unit: {porcentageData.unit_key}, Soldiers: {result} ({levy_type_data.UnitNum}x units of {levy_type_data.UnitSoldiers}), Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {unit.GetAttilaFaction()}";
                BattleLog.AddLevyLog(army.ID, logLine);

                string script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPELevy{porcentageData.porcentage}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                BattleFile.AddUnit(porcentageData.unit_key, levy_type_data.UnitSoldiers, levy_type_data.UnitNum, levy_type_data.SoldiersRest, script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                i++;
            }
        }

    }
}
