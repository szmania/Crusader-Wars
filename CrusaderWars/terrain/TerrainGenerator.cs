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
   
        public static string TerrainType { get; set; } = string.Empty;
        public static string Region { get; private set; } = string.Empty;

        public static bool isRiver { get; private set; }
        public static bool isStrait { get; private set; }
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
            isUnique = false;
            UniqueMaps.Clear();
        }

        private static (string[] attPositions, string[] defPositions) GetRandomNorthSouthPositions()
        {
            Random random = new Random();
            if (random.Next(0, 2) == 0)
            {
                return (new string[] { "N", "N" }, new string[] { "S", "S" });
            }
            else
            {
                return (new string[] { "S", "S" }, new string[] { "N", "N" });
            }
        }

        public static void CheckForLandBridgeBattle(List<Army> attacker_armies, List<Army> defender_armies)
        {
            var landBridge = unit_mapper.UnitMappers_BETA.GetLandBridgeMap(data.battle_results.BattleResult.ProvinceID);
            if (landBridge == null)
            {
                return; // Not a land bridge province
            }

            var allArmyIDs = attacker_armies.Concat(defender_armies).Select(a => a.ArmyUnitID).Where(id => !string.IsNullOrEmpty(id)).ToHashSet();
            if (!allArmyIDs.Any())
            {
                return; // No armies to check
            }

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
                            return; // Found one, no need to check further
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error checking for land bridge battle in Units.txt: {ex.Message}");
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

            //Straits Battle Maps
            if(isStrait)
            {
                var landBridgeStrait = unit_mapper.UnitMappers_BETA.GetLandBridgeMap(data.battle_results.BattleResult.ProvinceID);
                if (landBridgeStrait != null && landBridgeStrait.Variants.Any())
                {
                    Program.Logger.Debug($"Getting land bridge (strait type) battle map for province: {data.battle_results.BattleResult.ProvinceID}");
                    int index = unit_mapper.UnitMappers_BETA.GetDeterministicIndex(data.battle_results.BattleResult.ProvinceID, landBridgeStrait.Variants.Count);
                    var variant = landBridgeStrait.Variants[index];
                    var (attPositions, defPositions) = GetRandomNorthSouthPositions();
                    return (variant.X, variant.Y, attPositions, defPositions);
                }

                Program.Logger.Debug($"Getting strait battle map for terrain: {TerrainType}");
                var battlemap = Straits.GetBattleMap(Region ,TerrainType);
                return battlemap;   
            }

            //River Battle Maps
            if(isRiver) 
            {
                var landBridgeRiver = unit_mapper.UnitMappers_BETA.GetLandBridgeMap(data.battle_results.BattleResult.ProvinceID);
                if (landBridgeRiver != null && landBridgeRiver.Variants.Any())
                {
                    Program.Logger.Debug($"Getting land bridge battle map for province: {data.battle_results.BattleResult.ProvinceID}");
                    int index = unit_mapper.UnitMappers_BETA.GetDeterministicIndex(data.battle_results.BattleResult.ProvinceID, landBridgeRiver.Variants.Count);
                    var variant = landBridgeRiver.Variants[index];
                    var (attPositions, defPositions) = GetRandomNorthSouthPositions();
                    return (variant.X, variant.Y, attPositions, defPositions);
                }

                Program.Logger.Debug($"Getting river battle map for terrain: {TerrainType}");
                var battlemap = Lands.GetBattleMap(TerrainType, data.battle_results.BattleResult.ProvinceName ?? "");
                return battlemap;
            }

            //Land Battle Maps
            bool isLand = (!isStrait && !isRiver && !isUnique);
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
