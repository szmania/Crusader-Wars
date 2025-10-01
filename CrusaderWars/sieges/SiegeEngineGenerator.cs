using System;
using System.Collections.Generic;
using System.Linq;
using CrusaderWars.twbattle;
using CrusaderWars.unit_mapper;
using CrusaderWars.data.save_file; // Added for Army

namespace CrusaderWars.sieges
{
    public static class SiegeEngineGenerator
    {
        public static Dictionary<string, int> Generate(List<Army> attackerArmies)
        {
            Program.Logger.Debug("Starting SiegeEngineGenerator.Generate()");
            var siegeEnginesToBuild = new Dictionary<string, int>();

            int attackerArmySize = attackerArmies.SelectMany(a => a.Units).Sum(u => u.GetSoldiers());

            double siegeProgress = Sieges.GetSiegeProgress();
            int fortLevel = Sieges.GetFortLevel();
            int holdingLevel = Sieges.GetHoldingLevel();
            var allAvailableEngines = UnitMappers_BETA.SiegeEngines;

            Program.Logger.Debug($"Siege Progress: {siegeProgress}, Fort Level: {fortLevel}, Holding Level: {holdingLevel}");

            // Calculate total progress required for the siege
            // Base 100 progress + 75 progress per fort level
            double totalRequiredProgress = 100 + (fortLevel * 75);
            Program.Logger.Debug($"Total Required Siege Progress: {totalRequiredProgress}");

            // Return immediately if current progress is less than one-fourth of the total required progress
            if (siegeProgress < totalRequiredProgress / 4)
            {
                Program.Logger.Debug($"Siege progress ({siegeProgress}) is less than one-fourth of total required progress ({totalRequiredProgress / 4}). No siege engines generated based on progress.");
                // The default ram logic will now be handled after this if-else block.
            }
            else // Logic for when siege progress is sufficient
            {
                // Determine which wall height to exclude from siege equipment
                // Holding level 1 or 2 means we exclude 15m equipment
                // Holding level 3 or higher means we exclude 8m equipment
                string excludedWallHeight = (holdingLevel >= 3) ? "8m" : "15m";
                Program.Logger.Debug($"Excluding engines with wall height '{excludedWallHeight}' based on holding level ({holdingLevel}).");

                // Filter out engines designed for the wrong wall height.
                // This correctly includes engines for the required height AND engines with no height requirement (like rams).
                var filteredEngines = allAvailableEngines
                    .Where(e => !e.Key.Contains(excludedWallHeight, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Program.Logger.Debug($"Filtered to {filteredEngines.Count} engines after excluding '{excludedWallHeight}'.");

                // Calculate progress percentage to determine allowed effort cost and quantity
                double progressPercentage = siegeProgress / totalRequiredProgress;
                // Scale effort cost from 0 to 100 based on progress percentage
                int maxEffortCost = (int)(progressPercentage * 100);
                Program.Logger.Debug($"Progress Percentage: {progressPercentage:P2}, Max Effort Cost: {maxEffortCost}");

                // Select the best available engine for each type that meets the criteria
                var bestRam = filteredEngines
                    .Where(e => e.Type.Contains("ram", StringComparison.OrdinalIgnoreCase) && e.SiegeEffortCost <= maxEffortCost)
                    .OrderByDescending(e => e.SiegeEffortCost)
                    .FirstOrDefault();

                var bestTower = filteredEngines
                    .Where(e => e.Type.Contains("tower", StringComparison.OrdinalIgnoreCase) && e.SiegeEffortCost <= maxEffortCost)
                    .OrderByDescending(e => e.SiegeEffortCost)
                    .FirstOrDefault();

                var bestLadder = filteredEngines
                    .Where(e => e.Type.Contains("ladder", StringComparison.OrdinalIgnoreCase) && e.SiegeEffortCost <= maxEffortCost)
                    .OrderByDescending(e => e.SiegeEffortCost)
                    .FirstOrDefault();

                // Determine the quantity of each selected engine
                int baseQuantity = 1;
                int quantityMultiplier = 0;
                if (progressPercentage >= 0.5)
                {
                    quantityMultiplier = 1;
                }
                if (progressPercentage >= 0.75)
                {
                    quantityMultiplier = 2;
                }
                int finalQuantity = baseQuantity + quantityMultiplier;
                Program.Logger.Debug($"Final quantity for each engine type: {finalQuantity}");

                // Add selected engines to the dictionary
                if (bestRam != null)
                {
                    siegeEnginesToBuild.Add(bestRam.Key, finalQuantity);
                    Program.Logger.Debug($"Selected Ram: {bestRam.Key}, Quantity: {finalQuantity}");
                }
                if (bestTower != null)
                {
                    siegeEnginesToBuild.Add(bestTower.Key, finalQuantity);
                    Program.Logger.Debug($"Selected Tower: {bestTower.Key}, Quantity: {finalQuantity}");
                }
                if (bestLadder != null)
                {
                    siegeEnginesToBuild.Add(bestLadder.Key, finalQuantity);
                    Program.Logger.Debug($"Selected Ladder: {bestLadder.Key}, Quantity: {finalQuantity}");
                }
            }

            // --- START: NEW DEFAULT RAM LOGIC (Unified) ---
            // This logic now runs regardless of initial siege progress,
            // ensuring a ram is added if no other siege units are present and no deployables were generated.
            bool attackerHasSiegeUnits = attackerArmies.SelectMany(a => a.Units).Any(u => u.IsSiege());
            if (!attackerHasSiegeUnits && !siegeEnginesToBuild.Any())
            {
                Program.Logger.Debug("Attacker has no mapped siege units and no deployables were generated. Adding a default battering ram.");
                var cheapestRam = UnitMappers_BETA.SiegeEngines
                    .Where(se => se.Type.Contains("ram", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(se => se.SiegeEffortCost)
                    .FirstOrDefault();

                if (cheapestRam != null && !string.IsNullOrEmpty(cheapestRam.Key))
                {
                    siegeEnginesToBuild.Add(cheapestRam.Key, 1);
                    Program.Logger.Debug($"Added one deployable of '{cheapestRam.Key}'.");
                }
                else
                {
                    Program.Logger.Debug("WARNING: Could not find any battering ram in siege engine definitions. Cannot add default siege engine.");
                }
            }
            // --- END: NEW DEFAULT RAM LOGIC ---

            // --- START: NEW CAPPING LOGIC ---
            int maxAllowedEngines = Math.Min(10, Math.Max(1, attackerArmySize / 800));
            int currentTotalEngines = siegeEnginesToBuild.Values.Sum();
            Program.Logger.Debug($"Siege engine limit: {maxAllowedEngines} (based on {attackerArmySize} soldiers, capped at 10). Current count before limit: {currentTotalEngines}.");

            while (currentTotalEngines > maxAllowedEngines)
            {
                // Find the key of the cheapest engine currently in our results
                var keyToRemove = siegeEnginesToBuild.Keys
                    .Select(key => new { Key = key, Cost = UnitMappers_BETA.SiegeEngines.FirstOrDefault(se => se.Key == key)?.SiegeEffortCost ?? int.MaxValue })
                    .OrderBy(x => x.Cost)
                    .FirstOrDefault()?.Key;

                if (keyToRemove == null) { break; } // Safety break

                // Decrement the count and total
                siegeEnginesToBuild[keyToRemove]--;
                currentTotalEngines--;
                Program.Logger.Debug($"Removing one '{keyToRemove}' to meet limit. New total: {currentTotalEngines}");

                // If count is zero, remove the engine type from the dictionary
                if (siegeEnginesToBuild[keyToRemove] == 0)
                {
                    siegeEnginesToBuild.Remove(keyToRemove);
                }
            }
            Program.Logger.Debug($"Final siege engine count after applying limit: {currentTotalEngines}.");
            // --- END: NEW CAPPING LOGIC ---

            Program.Logger.Debug($"Finished SiegeEngineGenerator.Generate(). Generated {siegeEnginesToBuild.Count} unique siege engine types.");
            return siegeEnginesToBuild;
        }
    }
}
