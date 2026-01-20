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
using CrusaderWars.twbattle;

namespace CrusaderWars
{
    public static class UnitsFile
    {
        private static readonly Random _random = new Random();
        public static int MAX_LEVIES_UNIT_NUMBER = ModOptions.GetLevyMax();
        public static int MAX_CAVALRY_UNIT_NUMBER = ModOptions.GetCavalryMax();
        public static int MAX_INFANTRY_UNIT_NUMBER = ModOptions.GetInfantryMax();
        public static int MAX_RANGED_UNIT_NUMBER = ModOptions.GetRangedMax();

        public static CommanderTraits? PlayerCommanderTraits;
        public static CommanderTraits? EnemyCommanderTraits;

        private static int attackerSiegeUnitsCount = 0;
        private static int defenderSiegeUnitsCount = 0;
        private const int MAX_SIEGE_UNITS_PER_SIDE = 20;

        private static HashSet<string> processedArmyIDs = new HashSet<string>();

        public static void ResetProcessedArmies()
        {
            processedArmyIDs.Clear();
            attackerSiegeUnitsCount = 0;
            defenderSiegeUnitsCount = 0;
            Program.Logger.Debug($"Cleared processed armies list and siege unit counters.");
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
                
                Unit commander_unit = new Unit("General", commander_soldiers, commander.GetCultureObj(), RegimentType.Commander, false, army.Owner, commander.Rank);
                commander_unit.SetAttilaFaction(UnitMappers_BETA.GetAttilaFaction(commander.GetCultureName(), commander.GetHeritageName()));
                var (commanderKey, isSiege) = UnitMappers_BETA.GetUnitKey(commander_unit);

                // Check for autofix replacement
                if (twbattle.BattleProcessor.AutofixReplacements.TryGetValue(commanderKey, out var replacement))
                {
                    Program.Logger.Debug($"Autofix: Applying commander unit replacement for '{commanderKey}' with '{replacement.replacementKey}'.");
                    commanderKey = replacement.replacementKey;
                    isSiege = replacement.isSiege;
                }

                commander_unit.SetUnitKey(commanderKey);
                commander_unit.SetIsSiege(isSiege);

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
            if (army.Knights != null && army.Knights.GetKnightsList()?.Count > 0)
            {
                // The KnightSystem now contains the correct mix of standard knights and unassigned prominent knights.
                // We process them all together into a single unit.
                var knightSystem = army.Knights;
                knightSystem.SetMajorCulture();

                Unit knights_unit;
                if (knightSystem.GetMajorCulture() != null)
                    knights_unit = new Unit("Knight", knightSystem.GetKnightsSoldiers(), knightSystem.GetMajorCulture(), RegimentType.Knight, false, army.Owner, 0);
                else
                    knights_unit = new Unit("Knight", knightSystem.GetKnightsSoldiers(), army.Owner.GetCulture(), RegimentType.Knight, false, army.Owner, 0);

                knights_unit.SetAttilaFaction(UnitMappers_BETA.GetAttilaFaction(knights_unit.GetCulture(), knights_unit.GetHeritage()));
                var (knightKey, isSiegeKnight) = UnitMappers_BETA.GetUnitKey(knights_unit);

                if (twbattle.BattleProcessor.AutofixReplacements.TryGetValue(knightKey, out var replacement))
                {
                    knightKey = replacement.replacementKey;
                    isSiegeKnight = replacement.isSiege;
                }

                knights_unit.SetUnitKey(knightKey);
                knights_unit.SetIsSiege(isSiegeKnight);

                string knightAttilaKey = knights_unit.GetAttilaUnitKey();
                if (string.IsNullOrEmpty(knightAttilaKey) || knightAttilaKey == UnitMappers_BETA.NOT_FOUND_KEY)
                {
                    BattleLog.AddUnmappedUnit(knights_unit, army.ID);
                }
                else
                {
                    army.Units.Insert(1, knights_unit); // Insert near commander

                    string knights_script_name;
                    if (knightSystem.GetMajorCulture() != null)
                        knights_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPEknights_CULTURE{knightSystem.GetMajorCulture()?.ID ?? "unknown"}_";
                    else
                        knights_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPEknights_CULTURE{army.Owner.GetCulture()?.ID ?? "unknown"}_";

                    string? knightNameToDisplay = null;
                    if (army.Commander == null && !army.Units.Any(u => u.KnightCommander != null))
                    {
                        var bestKnight = knightSystem.GetKnightsList()
                                                            .OrderByDescending(k => k.GetProwess())
                                                            .FirstOrDefault();
                        if (bestKnight != null)
                        {
                            knightNameToDisplay = bestKnight.GetName();
                        }
                    }

                    BattleFile.AddKnightUnit(knightSystem, knightAttilaKey, knights_script_name, knightSystem.SetExperience(), Deployments.beta_GeDirection(army.CombatSide), knightNameToDisplay);
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
            //    GARRISON     #
            //                 #
            //##################
            var newGarrisonUnits = new List<Unit>();
            var garrison_units = army.Units.Where(item => item.GetRegimentType() == RegimentType.Garrison).ToList();
            if (garrison_units.Any())
            {
                // Group garrisons by their target Attila faction to prevent duplicate generation
                var garrison_by_faction = garrison_units.GroupBy(u => u.GetAttilaFaction());

                foreach (var faction_group in garrison_by_faction)
                {
                    string factionName = faction_group.Key;
                    if (string.IsNullOrEmpty(factionName) || factionName == UnitMappers_BETA.NOT_FOUND_KEY)
                    {
                        int soldiers = faction_group.Sum(u => u.GetSoldiers());
                        Program.Logger.Debug($"WARNING - {soldiers} GARRISON SOLDIERS WITHOUT A FACTION FOUND. SKIPPING.");
                        continue;
                    }

                    // Sum up all soldiers for this faction
                    int total_faction_garrison_soldiers = faction_group.Sum(u => u.GetSoldiers());

                    // Create a representative garrison unit for this faction group.
                    var representative_unit = faction_group.OrderByDescending(u => u.GetSoldiers()).First();

                    // Determine the culture for the merged garrison unit, falling back to the army owner's culture if needed.
                    var garrisonCulture = representative_unit.GetObjCulture();
                    if (garrisonCulture == null)
                    {
                        Program.Logger.Debug($"  - Garrison representative unit for faction '{factionName}' has no culture. Falling back to owner's culture.");
                        garrisonCulture = army.Owner.GetCulture();
                    }

                    Unit merged_garrison_unit = new Unit("Garrison", total_faction_garrison_soldiers, garrisonCulture, RegimentType.Garrison, faction_group.Any(u => u.IsMerc()), army.Owner, representative_unit.GarrisonLevel);
                    merged_garrison_unit.SetAttilaFaction(factionName);
                    merged_garrison_unit.SetMax(representative_unit.GetMax()); // Inherit max from representative

                    Program.Logger.Debug($"Processing garrisons for faction '{factionName}' with a total of {total_faction_garrison_soldiers} soldiers.");

                    int holdingLevel = twbattle.Sieges.GetHoldingLevel();
                    var garrison_porcentages = UnitMappers_BETA.GetFactionGarrison(factionName, holdingLevel);
                    newGarrisonUnits.AddRange(BETA_GarrisonComposition(merged_garrison_unit, army, garrison_porcentages, army_xp));
                }
            }

                //##################
                //                 #
                //      LEVIES     #
                //                 #
                //##################
    
                var newLevyUnits = new List<Unit>();
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

                        Unit merged_levy_unit = new Unit("Levy", total_faction_levy_soldiers, levyCulture, RegimentType.Levy, faction_group.Any(u => u.IsMerc()), army.Owner, 0);
                        merged_levy_unit.SetAttilaFaction(factionName);
                        merged_levy_unit.SetMax(representative_unit.GetMax()); // Inherit max from representative
    
                        Program.Logger.Debug($"Processing levies for faction '{factionName}' with a total of {total_faction_levy_soldiers} soldiers.");
    
                        var (levy_porcentages, factionUsed) = UnitMappers_BETA.GetFactionLevies(factionName);
                        newLevyUnits.AddRange(BETA_LevyComposition(merged_levy_unit, army, levy_porcentages, army_xp, factionUsed));
                    }
                }

            // Remove old placeholder units and add new composed units for logging
            army.Units.RemoveAll(u => u.GetRegimentType() == RegimentType.Garrison || u.GetRegimentType() == RegimentType.Levy);
            army.Units.AddRange(newGarrisonUnits);
            army.Units.AddRange(newLevyUnits);

            //##################
            //                 #
            //   MEN-AT-ARMS   #
            //                 #
            //##################
            foreach (var unit in army.Units)
            {
                string unitName = unit.GetName();
                //Skip if its not a Men at Arms Unit
                if (unitName == "General" || unit.GetRegimentType() == RegimentType.Knight || unit.GetRegimentType() == RegimentType.Levy || unit.GetRegimentType() == RegimentType.Garrison) continue;

                string attilaUnitKey = unit.GetAttilaUnitKey();
                if (string.IsNullOrEmpty(attilaUnitKey) || attilaUnitKey == UnitMappers_BETA.NOT_FOUND_KEY)
                {
                    Program.Logger.Debug($"  - WARNING: Could not map CK3 Unit '{unit.GetName()}' (Type: {unit.GetRegimentType()}) for army {army.ID}. It will be dropped from the battle.");
                    BattleLog.AddUnmappedUnit(unit, army.ID);
                    continue; // Skip this unit
                }

                Program.Logger.Debug($"  Processing MAA unit: Name={unit.GetName()}, Soldiers={unit.GetSoldiers()}, Culture={unit.GetCulture()}");

                // NEW LOGIC FOR SIEGE UNITS
                int effectiveNumGuns = unit.GetNumGuns();
                if (unit.IsSiegeEnginePerUnit() && effectiveNumGuns <= 0)
                {
                    effectiveNumGuns = 1;
                }

                if (unit.IsSiege() && effectiveNumGuns > 0)
                {
                    int totalCk3Machines = unit.GetSoldiers();
                    int numGunsPerUnit = effectiveNumGuns;
                    int numAttilaUnits = (int)Math.Ceiling((double)totalCk3Machines / numGunsPerUnit);

                    // NEW: Check against the cap
                    ref int currentSiegeCount = ref (army.CombatSide == "attacker" ? ref attackerSiegeUnitsCount : ref defenderSiegeUnitsCount);
                    int remainingSlots = MAX_SIEGE_UNITS_PER_SIDE - currentSiegeCount;

                    if (remainingSlots <= 0)
                    {
                        Program.Logger.Debug($"  - Unit '{unit.GetName()}' skipped. Siege unit limit of {MAX_SIEGE_UNITS_PER_SIDE} reached for {army.CombatSide} side.");
                        continue; // Skip this whole CK3 unit
                    }

                    int unitsToCreate = Math.Min(numAttilaUnits, remainingSlots);
                    if (unitsToCreate < numAttilaUnits)
                    {
                        Program.Logger.Debug($"  - Unit '{unit.GetName()}' creation capped from {numAttilaUnits} to {unitsToCreate} due to siege unit limit.");
                    }

                    Program.Logger.Debug($"  - Unit '{unit.GetName()}' is a multi-gun siege unit. CK3 Machines: {totalCk3Machines}, NumGuns/Unit: {numGunsPerUnit}. Creating {unitsToCreate} Attila units.");

                    for (int j = 0; j < unitsToCreate; j++)
                    {
                        // The last unit gets the remainder of machines
                        int machinesForThisUnit = (j == unitsToCreate - 1)
                            ? totalCk3Machines - (numGunsPerUnit * (unitsToCreate - 1))
                            : numGunsPerUnit;
                        
                        int soldiersForThisUnit = UnitMappers_BETA.ConvertMachinesToMen(machinesForThisUnit);

                        // Use a sub-counter 'j' to ensure unique script names for each created unit
                        string unit_script_name = $"{i}_{j}_{army.CombatSide}_army{army.ID}_TYPE{unit.GetName()}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                        BattleFile.AddUnit(attilaUnitKey, soldiersForThisUnit, 1, 0, unit_script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide), unit.KnightCommander);
                        currentSiegeCount++;
                    }
                }
                else // OLD LOGIC for non-siege units and siege units without num_guns
                {
                    int soldiersForAttila = unit.GetSoldiers();
                    var MAA_Data = (UnitSoldiers: 0, UnitNum: 0, SoldiersRest: 0);

                    if (unit.IsSiege())
                    {
                        soldiersForAttila = UnitMappers_BETA.ConvertMachinesToMen(unit.GetSoldiers());
                        Program.Logger.Debug($"  - Unit '{unit.GetName()}' is a single-entry siege unit. Converting {unit.GetSoldiers()} machines to {soldiersForAttila} soldiers for Attila.");
                    }

                    MAA_Data = RetriveCalculatedUnits(soldiersForAttila, unit.GetMax());

                    if (unit.IsSiege())
                    {
                        // NEW: Check against the cap
                        ref int currentSiegeCount = ref (army.CombatSide == "attacker" ? ref attackerSiegeUnitsCount : ref defenderSiegeUnitsCount);
                        int remainingSlots = MAX_SIEGE_UNITS_PER_SIDE - currentSiegeCount;

                        if (remainingSlots <= 0)
                        {
                            Program.Logger.Debug($"  - Unit '{unit.GetName()}' skipped. Siege unit limit of {MAX_SIEGE_UNITS_PER_SIDE} reached for {army.CombatSide} side.");
                            continue; // Skip this whole CK3 unit
                        }

                        int unitsToCreate = Math.Min(MAA_Data.UnitNum, remainingSlots);
                        if (unitsToCreate < MAA_Data.UnitNum)
                        {
                            Program.Logger.Debug($"  - Unit '{unit.GetName()}' creation capped from {MAA_Data.UnitNum} to {unitsToCreate} due to siege unit limit.");
                        }
                        MAA_Data.UnitNum = unitsToCreate;
                        currentSiegeCount += unitsToCreate;
                    }

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

                    int final_xp = army_xp;
                    if (unit.KnightCommander != null)
                    {
                        int prowess = unit.KnightCommander.GetProwess();
                        if (prowess <= 8) final_xp += 1;
                        else if (prowess <= 16) final_xp += 2;
                        else final_xp += 3;
                    }

                    //If is retinue maa, increase 2xp.
                    if (unitName.Contains("accolade"))
                    {
                        string unit_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPE{unit.GetName()}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                        int accolade_xp = final_xp + 2;
                        if (accolade_xp < 0) accolade_xp = 0;
                        if (accolade_xp > 9) accolade_xp = 9;
                        BattleFile.AddUnit(attilaUnitKey, MAA_Data.UnitSoldiers, MAA_Data.UnitNum, MAA_Data.SoldiersRest, unit_script_name, accolade_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide), unit.KnightCommander);
                    }
                    //If is normal maa
                    else
                    {
                        if (final_xp < 0) final_xp = 0;
                        if (final_xp > 9) final_xp = 9;
                        string unit_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPE{unit.GetName()}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                        BattleFile.AddUnit(attilaUnitKey, MAA_Data.UnitSoldiers, MAA_Data.UnitNum, MAA_Data.SoldiersRest, unit_script_name, final_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide), unit.KnightCommander);
                    }
                }
                i++;


            }

            army.PrintUnits();
        }


        static List<Unit> BETA_LevyComposition(Unit unit, Army army, List<(int percentage, string unit_key, string name, string max)> faction_levy_percentages, int army_xp, string factionUsed)
        {
            var composedUnits = new List<Unit>();
            if (faction_levy_percentages == null || faction_levy_percentages.Count < 1)
            {
                Program.Logger.Debug("ERROR - LEVIES WITHOUT FACTION IN UNIT" + $"\nNUMBER OF SOLDIERS:{unit.GetSoldiers()}" + $"\nATTILA FACTION:{unit.GetAttilaFaction()}");
                return composedUnits;
            }

            // Apply manual and autofix replacements to the levy template list
            var corrected_levy_percentages = new List<(int percentage, string unit_key, string name, string max, bool isSiege)>();
            bool isPlayerAlliance = army.IsPlayer();
            foreach (var levyData in faction_levy_percentages)
            {
                string currentKey = levyData.unit_key;
                bool isSiege = false; // Default to false for levies

                // Priority 1: Manual Replacements
                if (BattleState.ManualUnitReplacements.TryGetValue((currentKey, isPlayerAlliance), out var manualReplacement))
                {
                    Program.Logger.Debug($"Manual Replace: Applying levy template replacement for '{currentKey}' with '{manualReplacement.replacementKey}' for {(isPlayerAlliance ? "player" : "enemy")} alliance.");
                    currentKey = manualReplacement.replacementKey;
                    isSiege = manualReplacement.isSiege;
                }
                // Priority 2: Autofix Replacements
                else if (twbattle.BattleProcessor.AutofixReplacements.TryGetValue(currentKey, out var autofixReplacement))
                {
                    Program.Logger.Debug($"Autofix: Applying levy template replacement for '{currentKey}' with '{autofixReplacement.replacementKey}'.");
                    currentKey = autofixReplacement.replacementKey;
                    isSiege = autofixReplacement.isSiege;
                }

                corrected_levy_percentages.Add((levyData.percentage, currentKey, levyData.name, levyData.max, isSiege));
            }


            // NEW: Filter out Men-At-Arms units from the levy pool
            var filtered_levy_porcentages = corrected_levy_percentages
                                                .Where(data => !data.unit_key.Contains("_MAA_"))
                                                .ToList();

            if (filtered_levy_porcentages.Any())
            {
                Program.Logger.Debug($"  BETA_LevyComposition ({army.CombatSide}): Filtered out MAA units from levy pool for faction '{unit.GetAttilaFaction()}'. Using filtered list.");
                corrected_levy_percentages = filtered_levy_porcentages;
            }
            else
            {
                Program.Logger.Debug($"  BETA_LevyComposition ({army.CombatSide}): WARNING: No non-MAA levy units found for faction '{unit.GetAttilaFaction()}'. Using original (unfiltered) list as fallback.");
            }


            //  SINGULAR UNIT
            //  select random levy type
            if (unit.GetSoldiers() <= unit.GetMax())
            {
                var random = corrected_levy_percentages[_random.Next(corrected_levy_percentages.Count)];
                
                int soldiersInCk3 = unit.GetSoldiers();
                int soldiersForAttila = soldiersInCk3;
                if (random.isSiege)
                {
                    soldiersForAttila = UnitMappers_BETA.ConvertMachinesToMen(soldiersInCk3);
                }

                string script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPELevy{random.percentage}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                BattleFile.AddUnit(random.unit_key, soldiersForAttila, 1, 0, script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));

                var newUnit = new Unit("Levy", unit.GetSoldiers(), unit.GetObjCulture(), RegimentType.Levy, unit.IsMerc(), army.Owner);
                newUnit.SetUnitKey(random.unit_key);
                newUnit.SetIsSiege(random.isSiege);
                newUnit.SetAttilaFaction(unit.GetAttilaFaction());
                composedUnits.Add(newUnit);

                string logLine = $"    - Levy Attila Unit: {random.unit_key}, Soldiers: {soldiersForAttila} (1x unit of {soldiersForAttila}), Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {factionUsed}";
                BattleLog.AddLevyLog(army.ID, logLine);

                i++;
                return composedUnits;
            }


            //  MULTIPLE UNITS
            //  fulfill every levy type
            int levySoldiers = unit.GetSoldiers();

            int totalPercentageSum = corrected_levy_percentages.Sum(p => p.percentage);
            if (totalPercentageSum <= 0)
            {
                Program.Logger.Debug($"  BETA_LevyComposition ({army.CombatSide}): WARNING: Total percentage sum for levies is 0 or less for faction '{unit.GetAttilaFaction()}'. No levy units will be generated.");
                return composedUnits;
            }

            int assignedSoldiers = 0;
            for (int k = 0; k < corrected_levy_percentages.Count; k++)
            {
                var percentageData = corrected_levy_percentages[k];
                int result;

                if (k < corrected_levy_percentages.Count - 1)
                {
                    double t = (double)percentageData.percentage / totalPercentageSum;
                    result = (int)Math.Round(levySoldiers * t);
                }
                else
                {
                    // Last unit gets the remainder to ensure total is correct
                    result = levySoldiers - assignedSoldiers;
                }

                if (result <= 0) continue;

                assignedSoldiers += result;

                int soldiersForAttila = result;
                if (percentageData.isSiege)
                {
                    soldiersForAttila = UnitMappers_BETA.ConvertMachinesToMen(result);
                }

                var levy_type_data = RetriveCalculatedUnits(soldiersForAttila, unit.GetMax());

                for (int j = 0; j < levy_type_data.UnitNum; j++)
                {
                    int soldiers = levy_type_data.UnitSoldiers;
                    if (j == 0) soldiers += levy_type_data.SoldiersRest;
                    if (soldiers <= 0) continue;

                    var newUnit = new Unit("Levy", soldiers, unit.GetObjCulture(), RegimentType.Levy, unit.IsMerc(), army.Owner);
                    newUnit.SetUnitKey(percentageData.unit_key);
                    newUnit.SetIsSiege(percentageData.isSiege);
                    newUnit.SetAttilaFaction(unit.GetAttilaFaction());
                    composedUnits.Add(newUnit);
                }

                string logLine = $"    - Levy Attila Unit: {percentageData.unit_key}, Soldiers: {soldiersForAttila} ({levy_type_data.UnitNum}x units of {levy_type_data.UnitSoldiers}), Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {factionUsed}";
                BattleLog.AddLevyLog(army.ID, logLine);

                string script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPELevy{percentageData.percentage}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                BattleFile.AddUnit(percentageData.unit_key, levy_type_data.UnitSoldiers, levy_type_data.UnitNum, levy_type_data.SoldiersRest, script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                i++;
            }
            return composedUnits;
        }

        static List<Unit> BETA_GarrisonComposition(Unit unit, Army army, List<(int porcentage, string unit_key, string name, string max)> faction_garrison_porcentages, int army_xp)
        {
            var composedUnits = new List<Unit>();
            if (faction_garrison_porcentages == null || faction_garrison_porcentages.Count < 1)
            {
                Program.Logger.Debug("ERROR - GARRISON WITHOUT FACTION IN UNIT" + $"\nNUMBER OF SOLDIERS:{unit.GetSoldiers()}" + $"\nATTILA FACTION:{unit.GetAttilaFaction()}");
                return composedUnits;
            }

            // Apply manual and autofix replacements to the garrison template list
            var corrected_garrison_porcentages = new List<(int porcentage, string unit_key, string name, string max, bool isSiege)>();
            bool isPlayerAlliance = army.IsPlayer();
            foreach (var garrisonData in faction_garrison_porcentages)
            {
                string currentKey = garrisonData.unit_key;
                bool isSiege = false; // Default to false

                // Priority 1: Manual Replacements
                if (BattleState.ManualUnitReplacements.TryGetValue((currentKey, isPlayerAlliance), out var manualReplacement))
                {
                    Program.Logger.Debug($"Manual Replace: Applying garrison template replacement for '{currentKey}' with '{manualReplacement.replacementKey}' for {(isPlayerAlliance ? "player" : "enemy")} alliance.");
                    currentKey = manualReplacement.replacementKey;
                    isSiege = manualReplacement.isSiege;
                }
                // Priority 2: Autofix Replacements
                else if (twbattle.BattleProcessor.AutofixReplacements.TryGetValue(currentKey, out var autofixReplacement))
                {
                    Program.Logger.Debug($"Autofix: Applying garrison template replacement for '{currentKey}' with '{autofixReplacement.replacementKey}'.");
                    currentKey = autofixReplacement.replacementKey;
                    isSiege = autofixReplacement.isSiege;
                }

                corrected_garrison_porcentages.Add((garrisonData.porcentage, currentKey, garrisonData.name, garrisonData.max, isSiege));
            }

            int garrisonSoldiers = unit.GetSoldiers();

            int totalPercentageSum = corrected_garrison_porcentages.Sum(p => p.porcentage);
            if (totalPercentageSum <= 0)
            {
                Program.Logger.Debug($"  BETA_GarrisonComposition ({army.CombatSide}): WARNING: Total percentage sum for garrisons is 0 or less for faction '{unit.GetAttilaFaction()}'. No garrison units will be generated.");
                return composedUnits;
            }

            int assignedSoldiers = 0;
            for (int k = 0; k < corrected_garrison_porcentages.Count; k++)
            {
                var porcentageData = corrected_garrison_porcentages[k];
                int result;

                if (k < corrected_garrison_porcentages.Count - 1)
                {
                    double t = (double)porcentageData.porcentage / totalPercentageSum;
                    result = (int)Math.Round(garrisonSoldiers * t);
                }
                else
                {
                    // Last unit gets the remainder to ensure total is correct
                    result = garrisonSoldiers - assignedSoldiers;
                }

                if (result <= 0) continue;

                assignedSoldiers += result;

                int soldiersForAttila = result;
                if (porcentageData.isSiege)
                {
                    soldiersForAttila = UnitMappers_BETA.ConvertMachinesToMen(result);
                }

                var garrison_type_data = RetriveCalculatedUnits(soldiersForAttila, unit.GetMax());

                for (int j = 0; j < garrison_type_data.UnitNum; j++)
                {
                    int soldiers = garrison_type_data.UnitSoldiers;
                    if (j == 0) soldiers += garrison_type_data.SoldiersRest;
                    if (soldiers <= 0) continue;

                    var newUnit = new Unit("Garrison", soldiers, unit.GetObjCulture(), RegimentType.Garrison, unit.IsMerc(), army.Owner);
                    newUnit.SetUnitKey(porcentageData.unit_key);
                    newUnit.SetIsSiege(porcentageData.isSiege);
                    newUnit.SetAttilaFaction(unit.GetAttilaFaction());
                    composedUnits.Add(newUnit);
                }

                string logLine = $"    - Garrison Attila Unit: {porcentageData.unit_key}, Soldiers: {soldiersForAttila} ({garrison_type_data.UnitNum}x units of {garrison_type_data.UnitSoldiers}), Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {unit.GetAttilaFaction()}";
                BattleLog.AddLevyLog(army.ID, logLine); // Keep AddLevyLog as per instruction, only text changed

                string script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPE{porcentageData.unit_key}_CULTURE{unit.GetObjCulture()?.ID ?? "unknown"}_";
                BattleFile.AddUnit(porcentageData.unit_key, garrison_type_data.UnitSoldiers, garrison_type_data.UnitNum, garrison_type_data.SoldiersRest, script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                i++;
            }
            return composedUnits;
        }

    }
}
