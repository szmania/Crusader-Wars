using CrusaderWars.data.save_file;
using CrusaderWars.unit_mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions; // Added for Regex

namespace CrusaderWars.sieges
{
    /// <summary>
    /// Generates a garrison army for siege battles.
    /// </summary>
    public static class GarrisonGenerator
    {
        private static string GetCultureNameFromID(string cultureID)
        {
            using (var sr = new System.IO.StreamReader(Writter.DataFilesPaths.Cultures_Path()))
            {
                bool in_block = false;
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Trim() == $"{cultureID}={{")
                    {
                        in_block = true;
                    }
                    else if (in_block)
                    {
                        if (line.Contains("name="))
                        {
                            var nameMatch = System.Text.RegularExpressions.Regex.Match(line, @"name=""([^""]+)""");
                            if (nameMatch.Success)
                            {
                                string cultureName = nameMatch.Groups[1].Value;
                                cultureName = cultureName.Replace("-", "");
                                cultureName = Armies_Functions.RemoveDiacritics(cultureName);
                                cultureName = cultureName.Replace(" ", "");
                                return cultureName;
                            }
                        }
                        if (line.Trim() == "}")
                        {
                            break; 
                        }
                    }
                }
            }
            Program.Logger.Debug($"Warning: Could not find culture name for ID {cultureID}. Falling back to ID.");
            return cultureID; 
        }

        /// <summary>
        /// Creates a complete Army object representing the garrison defenders.
        /// </summary>
        /// <param name="garrisonSize">Total number of soldiers in the garrison.</param>
        /// <param name="cultureID">The culture ID of the garrison.</param>
        /// <param name="heritage">The heritage name of the garrison (e.g., "north_germanic_heritage").</param>
        /// <param name="owner">The owner object for the garrison army.</param>
        /// <param name="isMainArmy">Indicates if this is the main army for its side.</param>
        /// <returns>An Army object populated with garrison regiments.</returns>
        public static Army GenerateGarrisonArmy(int garrisonSize, string cultureID, string heritage, Owner owner, bool isMainArmy)
        {
            if (garrisonSize <= 0)
            {
                return new Army("garrison_army_empty", "defender", false); // Return an empty army
            }

            Army garrisonArmy = new Army("garrison_army", "defender", isMainArmy);
            garrisonArmy.IsPlayer(false); // Garrison is always AI controlled.
            garrisonArmy.SetOwner(owner);
            garrisonArmy.SetIsGarrison(true);

            string cultureName = GetCultureNameFromID(cultureID);
            string attilaFaction = UnitMappers_BETA.GetAttilaFaction(cultureName, heritage);
            Program.Logger.Debug($"Generating garrison for faction: {attilaFaction}");

            List<(int porcentage, string unit_key, string name, string max)> levyComposition;
            try
            {
                levyComposition = UnitMappers_BETA.GetFactionLevies(attilaFaction);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Could not get levy composition for faction '{attilaFaction}'. Error: {ex.Message}. Cannot generate garrison.");
                return garrisonArmy; // Return empty army
            }

            if (!levyComposition.Any())
            {
                Program.Logger.Debug($"Warning: No levy composition found for faction '{attilaFaction}'. Cannot generate garrison.");
                return garrisonArmy; // Return empty army
            }

            var armyRegiments = new List<ArmyRegiment>();

            // 1. Calculate total percentage to normalize distribution
            double totalPercentage = levyComposition.Sum(l => l.porcentage);
            if (totalPercentage == 0)
            {
                Program.Logger.Debug($"Warning: Total percentage for levy composition is 0 for faction '{attilaFaction}'. Cannot generate garrison.");
                return garrisonArmy; // Return empty army
            }

            // 2. Calculate soldiers for each unit type and handle rounding
            var soldiersPerType = new Dictionary<(string unit_key, string name), int>();
            int totalAllocated = 0;

            foreach (var levy in levyComposition)
            {
                int soldiersForThisType = (int)Math.Round(garrisonSize * (levy.porcentage / totalPercentage));
                soldiersPerType[(levy.unit_key, levy.name)] = soldiersForThisType;
                totalAllocated += soldiersForThisType;
            }

            // 3. Adjust for any rounding errors by adding/subtracting the difference to the largest group
            int roundingDifference = garrisonSize - totalAllocated;
            if (roundingDifference != 0 && soldiersPerType.Any())
            {
                var largestGroup = soldiersPerType.OrderByDescending(kvp => kvp.Value).First();
                soldiersPerType[largestGroup.Key] += roundingDifference;
            }

            // 4. Create the actual regiments for each unit type
            foreach (var kvp in soldiersPerType)
            {
                var unit_key = kvp.Key.unit_key;
                var name = kvp.Key.name;
                var soldiers = kvp.Value;

                if (soldiers > 0)
                {
                    Program.Logger.Debug($"Allocating {soldiers} soldiers to garrison unit '{unit_key}'");
                    armyRegiments.AddRange(CreateArmyRegimentsForType(soldiers, unit_key, cultureID, name, garrisonArmy));
                }
            }
            
            garrisonArmy.ArmyRegiments.AddRange(armyRegiments);

            return garrisonArmy;
        }

        /// <summary>
        /// Creates a list of ArmyRegiment objects for a specific unit type (e.g., infantry, ranged).
        /// </summary>
        private static List<ArmyRegiment> CreateArmyRegimentsForType(int totalSoldiers, string unitKey, string cultureID, string unitName, Army army)
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
                var culture = new Culture(cultureID);
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
                regiment.SetCulture(cultureID);

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
