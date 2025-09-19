using CrusaderWars.data.save_file;
using CrusaderWars.unit_mapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrusaderWars.sieges
{
    /// <summary>
    /// Generates a garrison army for siege battles.
    /// </summary>
    public static class GarrisonGenerator
    {
        /// <summary>
        /// Creates a complete Army object representing the garrison defenders.
        /// </summary>
        /// <param name="garrisonSize">Total number of soldiers in the garrison.</param>
        /// <param name="culture">The culture name of the garrison (e.g., "norse").</param>
        /// <param name="heritage">The heritage name of the garrison (e.g., "north_germanic_heritage").</param>
        /// <returns>An Army object populated with garrison regiments.</returns>
        public static Army GenerateGarrisonArmy(int garrisonSize, string culture, string heritage)
        {
            if (garrisonSize <= 0)
            {
                return new Army("garrison_army_empty", "defender", false); // Return an empty army
            }

            // Create a single army to hold the garrison regiments.
            // This army will represent the defender side in the battle.
            Army garrisonArmy = new Army("garrison_army", "defender", true);
            garrisonArmy.IsPlayer(false); // Garrison is always AI controlled.

            // Define garrison composition (e.g., 70% infantry, 30% ranged).
            // This can be adjusted later for more variety.
            int infantrySoldiers = (int)(garrisonSize * 0.7);
            int rangedSoldiers = garrisonSize - infantrySoldiers;

            // Get appropriate unit keys from the unit mapper.
            // NOTE: Assumes a method 'GetGarrisonUnit' exists in UnitMappers_BETA to find
            // a suitable default unit (e.g., levy spearman, levy archer) for a given culture.
            string infantryUnitKey = UnitMappers_BETA.GetGarrisonUnit("levy_infantry", culture, heritage);
            string rangedUnitKey = UnitMappers_BETA.GetGarrisonUnit("levy_archer", culture, heritage);

            // Create and add regiments to the army.
            var armyRegiments = new List<ArmyRegiment>();
            string infantryUnitName = "Garrison Levy Infantry";
            string rangedUnitName = "Garrison Levy Ranged";
            armyRegiments.AddRange(CreateArmyRegimentsForType(infantrySoldiers, infantryUnitKey, culture, heritage, infantryUnitName, garrisonArmy));
            armyRegiments.AddRange(CreateArmyRegimentsForType(rangedSoldiers, rangedUnitKey, culture, heritage, rangedUnitName, garrisonArmy));
            
            // NOTE: Assumes the 'Army' class has a public property 'ArmyRegiments'.
            garrisonArmy.ArmyRegiments.AddRange(armyRegiments);

            return garrisonArmy;
        }

        /// <summary>
        /// Creates a list of ArmyRegiment objects for a specific unit type (e.g., infantry, ranged).
        /// </summary>
        private static List<ArmyRegiment> CreateArmyRegimentsForType(int totalSoldiers, string unitKey, string cultureName, string heritageName, string unitName, Army army)
        {
            var armyRegiments = new List<ArmyRegiment>();
            if (totalSoldiers <= 0 || string.IsNullOrEmpty(unitKey) || unitKey == UnitMappers_BETA.NOT_FOUND_KEY)
            {
                return armyRegiments;
            }

            var armyRegiment = new ArmyRegiment($"garrison_army_reg_{unitName.Replace(" ", "_")}");
            armyRegiment.SetType(RegimentType.Levy, unitName);

            int soldiersRemaining = totalSoldiers;
            int regimentCounter = 0;

            // CK3 levy regiments are typically 100 soldiers at full strength.
            const int maxRegimentSize = 100;

            while (soldiersRemaining > 0)
            {
                int currentRegimentSize = Math.Min(soldiersRemaining, maxRegimentSize);

                // Create the Unit, which represents the soldiers in Attila.
                var culture = new Culture(cultureName, cultureName, heritageName);
                var unit = new Unit(unitName, currentRegimentSize, culture, RegimentType.Levy);
                unit.SetUnitKey(unitKey);
                unit.SetMax(maxRegimentSize);
                army.Units.Add(unit);

                // Create the Regiment, which is a container for units in CK3.
                var regiment = new Regiment($"garrison_reg_{unitName.Replace(" ", "_")}_{regimentCounter}", unitName); // Correct Regiment constructor
                regiment.IsGarrison(true);
                regiment.isMercenary(false);
                regiment.SetSoldiers(currentRegimentSize.ToString());
                regiment.SetMax(maxRegimentSize.ToString());
                regiment.SetCulture(cultureName);

                // Add the new regiment to the ArmyRegiment container's list of regiments.
                armyRegiment.Regiments.Add(regiment);

                soldiersRemaining -= currentRegimentSize;
                regimentCounter++;
            }

            armyRegiments.Add(armyRegiment);
            return armyRegiments;
        }
    }
}
