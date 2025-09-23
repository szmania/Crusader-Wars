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
        public static Army CreateGarrisonPlaceholderArmy(int garrisonSize, string cultureID, string heritage, Owner owner, bool isMainArmy)
        {
            if (garrisonSize <= 0)
            {
                return new Army("garrison_army_empty", "defender", false); // Return an empty army
            }

            Army garrisonArmy = new Army("garrison_army", "defender", isMainArmy);
            garrisonArmy.IsPlayer(false); // Garrison is always AI controlled.
            garrisonArmy.SetOwner(owner);
            garrisonArmy.SetIsGarrison(true);

            // Create a single placeholder unit for the garrison
            var unit = new Unit("GarrisonLevies", garrisonSize, new Culture(cultureID), RegimentType.Levy);
            garrisonArmy.Units.Add(unit);

            Program.Logger.Debug($"Created garrison placeholder army with {garrisonSize} soldiers for culture ID {cultureID}.");
            return garrisonArmy;
        }

        /// <summary>
        /// Generates a list of distributed garrison units for a garrison based on its size, culture, and heritage.
        /// </summary>
        /// <param name="garrisonSize">Total number of soldiers in the garrison.</param>
        /// <param name="garrisonCulture">The culture object of the garrison.</param>
        /// <param name="heritage">The heritage name of the garrison.</param>
        /// <returns>A list of Unit objects representing the distributed garrisons.</returns>
        public static List<Unit> GenerateDistributedGarrisonUnits(int garrisonSize, Culture garrisonCulture, string heritage)
        {
            Program.Logger.Debug($"Generating distributed garrison units for garrison of size {garrisonSize}, culture {garrisonCulture.GetCultureName()}, heritage {heritage}.");

            List<Unit> newUnits = new List<Unit>();
            if (garrisonSize <= 0)
            {
                Program.Logger.Debug("Garrison size is 0 or less, returning empty unit list.");
                return newUnits;
            }

            string cultureName = garrisonCulture.GetCultureName();
            string attilaFaction = UnitMappers_BETA.GetAttilaFaction(cultureName, heritage);
            Program.Logger.Debug($"Determined Attila Faction for garrison: {attilaFaction}");

            List<(int porcentage, string unit_key, string name, string max)> garrisonComposition;
            try
            {
                int holdingLevel = twbattle.Sieges.GetHoldingLevel();
                garrisonComposition = UnitMappers_BETA.GetFactionGarrison(attilaFaction, holdingLevel);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Could not get garrison composition for faction '{attilaFaction}' at holding level {twbattle.Sieges.GetHoldingLevel()}. Error: {ex.Message}. Cannot generate distributed garrison units.");
                return newUnits; // Return empty list
            }

            if (!garrisonComposition.Any())
            {
                Program.Logger.Debug($"Warning: No garrison composition found for faction '{attilaFaction}'. Cannot generate distributed garrison units.");
                return newUnits; // Return empty list
            }

            // 1. Calculate total percentage to normalize distribution
            double totalPercentage = garrisonComposition.Sum(l => l.porcentage);
            if (totalPercentage == 0)
            {
                Program.Logger.Debug($"Warning: Total percentage for garrison composition is 0 for faction '{attilaFaction}'. Cannot generate distributed garrison units.");
                return newUnits; // Return empty list
            }

            // 2. Calculate soldiers for each unit type and handle rounding
            var soldiersPerType = new Dictionary<(string unit_key, string name), int>();
            int totalAllocated = 0;

            foreach (var garrisonUnit in garrisonComposition)
            {
                int soldiersForThisType = (int)Math.Round(garrisonSize * (garrisonUnit.porcentage / totalPercentage));
                soldiersPerType[(garrisonUnit.unit_key, garrisonUnit.name)] = soldiersForThisType;
                totalAllocated += soldiersForThisType;
            }

            // 3. Adjust for any rounding errors by adding/subtracting the difference to the largest group
            int roundingDifference = garrisonSize - totalAllocated;
            if (roundingDifference != 0 && soldiersPerType.Any())
            {
                var largestGroup = soldiersPerType.OrderByDescending(kvp => kvp.Value).First();
                soldiersPerType[largestGroup.Key] += roundingDifference;
            }

            // 4. Create the actual Unit objects for each unit type
            foreach (var kvp in soldiersPerType)
            {
                var unit_key = kvp.Key.unit_key;
                var name = kvp.Key.name; // This name is like "Garrison_25"
                var soldiers = kvp.Value;

                if (soldiers > 0)
                {
                    Program.Logger.Debug($"Allocating {soldiers} soldiers to distributed garrison unit '{unit_key}' (original name: {name})");
                    // Create the Unit, which represents the soldiers in Attila.
                    // Use "Garrison" as the generic name for these distributed units.
                    var unit = new Unit("Garrison", soldiers, garrisonCulture, RegimentType.Levy);
                    unit.SetUnitKey(unit_key); // Set the specific Attila unit key
                    unit.SetMax(UnitMappers_BETA.GetMax(unit)); // Get max based on the unit key/type
                    newUnits.Add(unit);
                }
            }

            // Populate Attila-specific data for the newly created units
            newUnits = Armies_Functions.GetAllUnits_AttilaFaction(newUnits);
            newUnits = Armies_Functions.GetAllUnits_Max(newUnits);
            // Removed: newUnits = Armies_Functions.GetAllUnits_UnitKeys(newUnits); // Unit keys are now set directly from XML

            Program.Logger.Debug($"Finished generating {newUnits.Count} distributed garrison units for garrison.");
            return newUnits;
        }
    }
}
