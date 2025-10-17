using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.terrain
{
    internal static class UniqueMaps
    {
        private static readonly Random _random = new Random();
        static (string X, string Y, string[] attPositions, string[] defPositions) FoundMap;
        static string[] all = { "All", "All" };

        public static void Clear()
        {
            FoundMap = default;
        }

        struct BattleMaps
        {
            public static (string X, string Y, string[] attPositions, string[] defPositions) PyramidsOfGizeh = ("0.525", "0.737", all, all);
            public static (string X, string Y, string[] attPositions, string[] defPositions) Stonehenge = ("0.139", "0.233", all, all);
            public static (string X, string Y, string[] attPositions, string[] defPositions)[] HadrianWalls = {

                ("0.125", "0.137", all, all), //[0] Hills
                ("0.122", "0.131", new string[]{"N", "E"},new string[]{"S", "W"}), //[1] Hills
                ("0.121", "0.137", new string[]{"S", "W"},new string[]{"N", "E"}), //[2] Hills
                ("0.132", "0.128", all, all), //[3] Forest
            };


        };

        

        public static void ReadSpecialBuilding(string building)
        {
            Program.Logger.Debug($"ReadSpecialBuilding called for building: '{building}'");
            string x, y;

            //Search for added historical maps by unit mappers
            
            if (UnitMappers_BETA.Terrains != null)
            {
                Program.Logger.Debug($"Checking for custom unique map in unit mapper for building: '{building}'");
                foreach (var map in UnitMappers_BETA.Terrains.GetHistoricalMaps())
                {
                    if (building == map.building)
                    {
                        x = map.x;
                        y = map.y;
                        FoundMap = (x, y, all, all);
                        TerrainGenerator.isUniqueBattle(true);
                        Program.Logger.Debug($"Found custom unique map for '{building}' in unit mapper: ({x}, {y})");
                        return;
                    }
                }
                Program.Logger.Debug($"No custom unique map found for '{building}' in unit mapper. Checking hardcoded maps.");
            }
            else
            {
                Program.Logger.Debug("No unit mapper terrains loaded. Checking hardcoded unique maps.");
            }
            
            switch (building) 
            {
                case "the_pyramids_01":
                    x = BattleMaps.PyramidsOfGizeh.X;
                    y = BattleMaps.PyramidsOfGizeh.Y;
                    FoundMap = (x, y, all, all);
                    TerrainGenerator.isUniqueBattle(true);
                    Program.Logger.Debug($"Found hardcoded unique map for '{building}': ({x}, {y})");
                    return;
                case "stonehenge_01":
                    x = BattleMaps.Stonehenge.X;
                    y = BattleMaps.Stonehenge.Y;
                    FoundMap = (x, y, all, all);
                    TerrainGenerator.isUniqueBattle(true);
                    Program.Logger.Debug($"Found hardcoded unique map for '{building}': ({x}, {y})");
                    return;
                case "hadrians_wall_01":
                    Program.Logger.Debug($"Found hardcoded unique map for '{building}'. Terrain type: '{TerrainGenerator.TerrainType}'");
                    switch(TerrainGenerator.TerrainType)
                    {
                        case "Forest":
                        case "Bosque":
                        case "Forêt":
                        case "Wald":
                        case "Лес":
                        case "삼림":
                        case "森林":
                            x = BattleMaps.HadrianWalls[3].X;
                            y = BattleMaps.HadrianWalls[3].Y;
                            FoundMap = (x, y, all, all);
                            TerrainGenerator.isUniqueBattle(true);
                            Program.Logger.Debug($"Selected Hadrian's Wall Forest variant: ({x}, {y})");
                            break;
                        default:
                            int index = _random.Next(0, 3);
                            x = BattleMaps.HadrianWalls[index].X;
                            y = BattleMaps.HadrianWalls[index].Y;
                            FoundMap = (x, y, BattleMaps.HadrianWalls[index].attPositions, BattleMaps.HadrianWalls[index].defPositions);
                            TerrainGenerator.isUniqueBattle(true);
                            Program.Logger.Debug($"Selected Hadrian's Wall Hills variant (index {index}): ({x}, {y}), Att: [{string.Join(",", BattleMaps.HadrianWalls[index].attPositions)}], Def: [{string.Join(",", BattleMaps.HadrianWalls[index].defPositions)}]");
                            break;

                    }
                    return;
                    
            }


            Program.Logger.Debug($"No unique map found for building: '{building}'.");
            TerrainGenerator.isUniqueBattle(false);


        }
        public static (string X, string Y, string[] attPositions, string[] defPositions) GetBattleMap()
        {
            Program.Logger.Debug($"GetBattleMap called. Returning FoundMap: ({FoundMap.X}, {FoundMap.Y}), Att: [{string.Join(",", FoundMap.attPositions)}], Def: [{string.Join(",", FoundMap.defPositions)}]");
            return FoundMap;
        }

    }
}
