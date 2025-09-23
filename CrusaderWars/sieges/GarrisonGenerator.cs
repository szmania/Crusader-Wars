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
            int soldiersRemaining = garrisonSize;

            levyComposition = levyComposition.OrderByDescending(l => l.porcentage).ToList();

            for (int i = 0; i < levyComposition.Count; i++)
            {
                var levy = levyComposition[i];
                int soldiersForThisType;

                if (i == levyComposition.Count - 1)
                {
                    soldiersForThisType = soldiersRemaining;
                }
                else
                {
                    soldiersForThisType = (int)Math.Round(garrisonSize * (levy.porcentage / 100.0));
                }

                if (soldiersForThisType > 0)
                {
                    soldiersForThisType = Math.Min(soldiersForThisType, soldiersRemaining);
                    Program.Logger.Debug($"Allocating {soldiersForThisType} soldiers to garrison unit '{levy.unit_key}' ({levy.porcentage}%)");
                    armyRegiments.AddRange(CreateArmyRegimentsForType(soldiersForThisType, levy.unit_key, cultureID, levy.name, garrisonArmy));
                    soldiersRemaining -= soldiersForThisType;
                }
                
                if (soldiersRemaining <= 0) break;
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
