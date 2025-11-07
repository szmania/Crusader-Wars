using System;
using System.Drawing;
using System.Globalization;
using CrusaderWars.terrain;
using CrusaderWars.data.save_file;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CrusaderWars
{
    //Desert
    //Desert Mountain
    //Dryland
    //Farmlands
    //Floodplains -- X
    //Forest
    //Hills
    //Jungle -- X
    //Mountains
    //Oasis -- X
    //---------------------
    //Plains
    //Steppe
    //Taiga
    //Wetlands


    public static class TerrainGenerator
    {
        private static readonly Random _random = new Random();
        public static string TerrainType { get; set; } = string.Empty;
        public static string Region { get; private set; } = string.Empty;

        public static bool isRiver { get; private set; }
        public static bool isStrait { get; private set; }
        public static bool isCoastal { get; private set; }
        public static bool isUnique { get; private set; }

        public static void SetRegion(string a)
        {
            Region = a;
        }
        public static void isUniqueBattle(bool yn)
        {
            switch (yn)
            {
                case true:
                    isUnique = true;
                    return;
                case false:
                    isUnique = false;
                    return;
            }
        }

        public static void isCoastalBattle(bool yn)
        {
            switch (yn)
            {
                case true:
                    isCoastal = true;
                    return;
                case false:
                    isCoastal = false;
                    return;
            }
        }

        public static void isRiverBattle(bool yn)
        {
            switch(yn) 
            {
                case true:
                    isRiver = true;
                    return;
                case false:
                    isRiver = false;
                    return;
            }
        }

        public static void isStraitBattle(bool yn)
        {
            switch (yn)
            {
                case true:
                    isStrait = true;
                    return;
                case false:
                    isStrait = false;
                    return;
            }
        }
        public static void Clear()
        {
            TerrainType = String.Empty;
            isRiver = false;
            isStrait = false;
            isCoastal = false;
            isUnique = false;
            UniqueMaps.Clear();
        }

        public static void CheckForSpecialCrossingBattle(List<Army> attacker_armies, List<Army> defender_armies)
        {
            // Check for army movement across a land bridge first (strait/river crossing)
            var landBridge = unit_mapper.UnitMappers_BETA.GetLandBridgeMap(data.battle_results.BattleResult.ProvinceID);
            
            if (landBridge != null)
            {
                var allArmyIDs = attacker_armies.Concat(defender_armies).Select(a => a.ArmyUnitID).Where(id => !string.IsNullOrEmpty(id)).ToHashSet();
                if (allArmyIDs.Any())
                {
                    try
                    {
                        string unitsContent = File.ReadAllText(Writter.DataFilesPaths.Units_Path());
                        string[] unitBlocks = Regex.Split(unitsContent, @"(?=\s*\t\d+={)");

                        foreach (string block in unitBlocks)
                        {
                            if (string.IsNullOrWhiteSpace(block)) continue;

                            var unitIdMatch = Regex.Match(block, @"^\s*\t(\d+)={");
                            if (!unitIdMatch.Success || !allArmyIDs.Contains(unitIdMatch.Groups[1].Value))
                            {
                                continue; // Not a relevant army unit
                            }

                            var locationMatch = Regex.Match(block, @"location=(\d+)");
                            var prevMatch = Regex.Match(block, @"prev=(\d+)");

                            if (locationMatch.Success && prevMatch.Success)
                            {
                                string location = locationMatch.Groups[1].Value;
                                string prev = prevMatch.Groups[1].Value;

                                if ((location == landBridge.ProvinceFrom && prev == landBridge.ProvinceTo) ||
                                    (location == landBridge.ProvinceTo && prev == landBridge.ProvinceFrom))
                                {
                                    Program.Logger.Debug($"Land bridge crossing detected for army unit {unitIdMatch.Groups[1].Value}. CK3 Type: {landBridge.CK3Type}");
                                    if (landBridge.CK3Type == "strait")
                                    {
                                        isStraitBattle(true);
                                    }
                                    else // Default to river for "river_large" and any other types
                                    {
                                        isRiverBattle(true);
                                    }
                                    return; // Strait/River crossing found, this is the highest priority.
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Debug($"Error checking for land bridge crossing in Units.txt: {ex.Message}");
                    }
                }
            }

            // If no strait/river crossing was detected, check for a coastal battle.
            var coastalMapByLocation = unit_mapper.UnitMappers_BETA.GetCoastalMap(data.battle_results.BattleResult.ProvinceID);
            if (coastalMapByLocation != null)
            {
                if (_random.Next(100) < 30)
                {
                    Program.Logger.Debug($"Coastal province battle detected for province {data.battle_results.BattleResult.ProvinceID}. 40% chance succeeded. Setting as coastal battle.");
                    isCoastalBattle(true);
                    return;
                }
                else
                {
                    Program.Logger.Debug($"Coastal province battle detected for province {data.battle_results.BattleResult.ProvinceID}. 40% chance failed. Proceeding with normal land battle terrain.");
                }
            }
        }



        public static (string X, string Y, string[] attPositions, string[] defPositions) GetBattleMap()
       {

            //Special Battle Maps           
            if(isUnique)
            {
                Program.Logger.Debug("Getting unique battle map.");
                var battlemap = UniqueMaps.GetBattleMap();
                return battlemap;
            }

            if (isCoastal)
            {
                var coastalMap = unit_mapper.UnitMappers_BETA.GetCoastalMap(data.battle_results.BattleResult.ProvinceID);
                if (coastalMap != null && coastalMap.Variants.Any())
                {
                    Program.Logger.Debug($"Getting coastal battle map for province: {data.battle_results.BattleResult.ProvinceID}");
                    int index = unit_mapper.UnitMappers_BETA.GetDeterministicIndex(data.battle_results.BattleResult.ProvinceID, coastalMap.Variants.Count);
                    var variant = coastalMap.Variants[index];
                    string[] orientations = (variant.Orientations != null && variant.Orientations.Any()) ? variant.Orientations.ToArray() : new string[] { "All" };
                    return (variant.X, variant.Y, orientations, orientations);
                }
            }

            //Straits Battle Maps
            if(isStrait)
            {
                var landBridgeStrait = unit_mapper.UnitMappers_BETA.GetLandBridgeMap(data.battle_results.BattleResult.ProvinceID);
                if (landBridgeStrait != null && landBridgeStrait.CK3Type == "strait" && landBridgeStrait.Variants.Any())
                {
                    Program.Logger.Debug($"Getting land bridge (strait type) battle map for province: {data.battle_results.BattleResult.ProvinceID}");
                    int index = unit_mapper.UnitMappers_BETA.GetDeterministicIndex(data.battle_results.BattleResult.ProvinceID, landBridgeStrait.Variants.Count);
                    var variant = landBridgeStrait.Variants[index];
                    string[] orientations = (variant.Orientations != null && variant.Orientations.Any()) ? variant.Orientations.ToArray() : new string[] { "All" };
                    return (variant.X, variant.Y, orientations, orientations);
                }

                Program.Logger.Debug($"Getting strait battle map for terrain: {TerrainType}");
                var battlemap = Straits.GetBattleMap(Region ,TerrainType);
                return battlemap;   
            }

            //River Battle Maps
            if(isRiver) 
            {
                var landBridgeRiver = unit_mapper.UnitMappers_BETA.GetLandBridgeMap(data.battle_results.BattleResult.ProvinceID);
                if (landBridgeRiver != null && landBridgeRiver.CK3Type != "strait" && landBridgeRiver.Variants.Any())
                {
                    Program.Logger.Debug($"Getting land bridge battle map for province: {data.battle_results.BattleResult.ProvinceID}");
                    int index = unit_mapper.UnitMappers_BETA.GetDeterministicIndex(data.battle_results.BattleResult.ProvinceID, landBridgeRiver.Variants.Count);
                    var variant = landBridgeRiver.Variants[index];
                    string[] orientations = (variant.Orientations != null && variant.Orientations.Any()) ? variant.Orientations.ToArray() : new string[] { "All" };
                    return (variant.X, variant.Y, orientations, orientations);
                }

                Program.Logger.Debug($"Getting river battle map for terrain: {TerrainType}");
                var battlemap = Lands.GetBattleMap(TerrainType, data.battle_results.BattleResult.ProvinceName ?? "");
                return battlemap;
            }

            //Land Battle Maps
            bool isLand = (!isStrait && !isRiver && !isUnique && !isCoastal);
            if (isLand) 
            {
                Program.Logger.Debug($"Getting land battle map for terrain: {TerrainType}");
                var battlemap = Lands.GetBattleMap(TerrainType, data.battle_results.BattleResult.ProvinceName ?? "");
                return battlemap;
            }


            return ("0.146", "0.177", new string[] { "All", "All" }, new string[] { "All", "All" });
       }
    }
}
