using System;
using System.Collections.Generic;
using System.Linq;
using Crusader_Wars.client;
using Crusader_Wars.unit_mapper;
using Crusader_Wars.data.save_file;
using Crusader_Wars.terrain;
using System.Windows;
using Crusader_Wars.armies.commander_traits;
using System.Runtime.CompilerServices;
using Crusader_Wars.armies;

namespace Crusader_Wars
{
    public static class UnitsFile
    {
        public static int MAX_LEVIES_UNIT_NUMBER = ModOptions.GetLevyMax();
        public static int MAX_CAVALRY_UNIT_NUMBER = ModOptions.GetCavalryMax();
        public static int MAX_INFANTRY_UNIT_NUMBER = ModOptions.GetInfantryMax();
        public static int MAX_RANGED_UNIT_NUMBER = ModOptions.GetRangedMax();

        public static CommanderTraits PlayerCommanderTraits;
        public static CommanderTraits EnemyCommanderTraits;

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

            BETA_AddArmyUnits(army);
            if(army.MergedArmies != null)
            {
                foreach(Army merged_army in army.MergedArmies)
                {
                    BETA_AddArmyUnits(merged_army);
                }
            }
        }

        public static CommanderTraits GetCommanderTraitsObj(bool isPlayer)
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
                commander_army_xp = army.Commander.GetUnitsExperience();
                int commander_xp = army.Commander.GetCommanderExperience();
                int commander_soldiers = army.Commander.GetUnitSoldiers();
                
                Unit commander_unit = new Unit("General", commander_soldiers, army.Commander.GetCultureObj(), RegimentType.Commander, false, army.Owner);
                commander_unit.SetAttilaFaction(UnitMappers_BETA.GetAttilaFaction(army.Commander.GetCultureName(), army.Commander.GetHeritageName()));
                commander_unit.SetUnitKey(UnitMappers_BETA.GetUnitKey(commander_unit));
                army.Units.Insert(0, commander_unit);

                
                string general_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPEcommander{army.Commander.ID}_CULTURE{army.Commander.GetCultureObj().ID}_";
                BattleFile.AddGeneralUnit(army.Commander, commander_unit.GetAttilaUnitKey(), general_script_name, commander_xp, Deployments.beta_GeDirection(army.CombatSide));
                i++;
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
                army.Units.Insert(1, knights_unit);

                string knights_script_name;
                if (army.Knights.GetMajorCulture() != null)
                    knights_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPEknights_CULTURE{army.Knights.GetMajorCulture().ID}_";
                else
                    knights_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPEknights_CULTURE{army.Owner.GetCulture().ID}_";


                BattleFile.AddKnightUnit(army.Knights, knights_unit.GetAttilaUnitKey(), knights_script_name, army.Knights.SetExperience(), Deployments.beta_GeDirection(army.CombatSide));
                i++;
            }


            //##################
            //     ARMY XP     #
            //##################
            if (army.IsPlayer()) { 
                traits_xp = GetTraitsXP(true, army.CombatSide, TerrainGenerator.TerrainType, TerrainGenerator.isRiver, false, Weather.HasWinter);
                modifiers_xp = CK3LogData.LeftSide.GetModifiers().GetXP();
            }
            else { 
                traits_xp = GetTraitsXP(false, army.CombatSide, TerrainGenerator.TerrainType, TerrainGenerator.isRiver, false, Weather.HasWinter);
                modifiers_xp = CK3LogData.RightSide.GetModifiers().GetXP();
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

            var levies_units = army.Units.Where(item => item.GetRegimentType() == data.save_file.RegimentType.Levy);
            if (levies_units.Count() > 0)
            {
                foreach (var levy_culture in levies_units)
                {
                    if(string.IsNullOrEmpty(levy_culture.GetAttilaFaction()))
                    {
                        Program.Logger.Debug($"WARNING - LEVY UNIT WITHOUT A FACTION FOUND. Amount = {levy_culture.GetSoldiers()} soldiers");
                        continue;
                    }

                    var levy_porcentages = UnitMappers_BETA.GetFactionLevies(levy_culture.GetAttilaFaction());
                    BETA_LevyComposition(levy_culture, army, levy_porcentages, army_xp);
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

                var MAA_Data = RetriveCalculatedUnits(unit.GetSoldiers(), unit.GetMax());

                if (unit.GetObjCulture() == null)
                    unit.ChangeCulture(unit.GetOwner().GetCulture());

                //If is retinue maa, increase 2xp.
                if (unitName.Contains("accolade"))
                {
                    string unit_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPE{unit.GetName()}_CULTURE{unit.GetObjCulture().ID}_";
                    int accolade_xp = army_xp + 2;
                    if (accolade_xp < 0) accolade_xp = 0;
                    if (accolade_xp > 9) accolade_xp = 9;
                    BattleFile.AddUnit(unit.GetAttilaUnitKey(), MAA_Data.UnitSoldiers, MAA_Data.UnitNum, MAA_Data.SoldiersRest, unit_script_name, accolade_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                }
                //If is normal maa
                else
                {
                    string unit_script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPE{unit.GetName()}_CULTURE{unit.GetObjCulture().ID}_";
                    BattleFile.AddUnit(unit.GetAttilaUnitKey(), MAA_Data.UnitSoldiers, MAA_Data.UnitNum, MAA_Data.SoldiersRest, unit_script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
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

            var Levies_Data = RetriveCalculatedUnits(unit.GetSoldiers(), unit.GetMax());

            int total_soldiers = unit.GetSoldiers();

            //  SINGULAR UNIT
            //  select random levy type
            if (unit.GetSoldiers() <= unit.GetMax())
            {
                Random r = new Random();
                var random = faction_levy_porcentages[r.Next(faction_levy_porcentages.Count - 1)];
                string script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPELevy{random.porcentage}_CULTURE{unit.GetObjCulture().ID}_";
                BattleFile.AddUnit(random.unit_key, Levies_Data.UnitSoldiers, 1, Levies_Data.SoldiersRest, script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                i++;
                return;
            }


            //  MULTIPLE UNITS
            //  fulfill every levy type
            int levySoldiers = unit.GetSoldiers();

            int compareNum = 0;

            foreach (var porcentageData in faction_levy_porcentages)
            {


                double t = (double)porcentageData.porcentage / 100;
                //int result = (int)Math.Round(Levies_Data.UnitNum * t);
                int result = (int)Math.Round(levySoldiers * t);
                var levy_type_data = RetriveCalculatedUnits(result, unit.GetMax());
                compareNum += (levy_type_data.UnitSoldiers * levy_type_data.UnitNum);
                //if (Levies_Data.UnitNum * t >= 0.5 && Levies_Data.UnitNum * t < 1) result = 1;
                string script_name = $"{i}_{army.CombatSide}_army{army.ID}_TYPELevy{porcentageData.porcentage}_CULTURE{unit.GetObjCulture().ID}_";
                BattleFile.AddUnit(porcentageData.unit_key, levy_type_data.UnitSoldiers, levy_type_data.UnitNum, levy_type_data.SoldiersRest, script_name, army_xp.ToString(), Deployments.beta_GeDirection(army.CombatSide));
                i++;
            }

        }

    }
}
