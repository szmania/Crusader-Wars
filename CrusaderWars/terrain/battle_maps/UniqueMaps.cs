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
        static (string X, string Y, string[] attPositions, string[] defPositions) FoundMap;
        static string[] all = { "All", "All" };

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
            string x, y;

            //Search for added historical maps by unit mappers
            
            if (UnitMappers_BETA.Terrains != null)
            {
                foreach (var map in UnitMappers_BETA.Terrains.GetHistoricalMaps())
                {
                    if (building == map.building)
                    {
                        Random random = new Random();
                        int index = random.Next(0, 3);
                        x = map.x;
                        y = map.y;
                        FoundMap = (x, y, all, all);
                        TerrainGenerator.isUniqueBattle(true);
                        return;
                    }
                }
            }
            
            switch (building) 
            {
                case "the_pyramids_01":
                    x = BattleMaps.PyramidsOfGizeh.X;
                    y = BattleMaps.PyramidsOfGizeh.Y;
                    FoundMap = (x, y, all, all);
                    TerrainGenerator.isUniqueBattle(true);
                    return;
                case "stonehenge_01":
                    x = BattleMaps.Stonehenge.X;
                    y = BattleMaps.Stonehenge.Y;
                    FoundMap = (x, y, all, all);
                    TerrainGenerator.isUniqueBattle(true);
                    return;
                case "hadrians_wall_01":
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
                            break;
                        default:
                            Random random = new Random();
                            int index = random.Next(0, 3);
                            x = BattleMaps.HadrianWalls[index].X;
                            y = BattleMaps.HadrianWalls[index].Y;
                            FoundMap = (x, y, BattleMaps.HadrianWalls[index].attPositions, BattleMaps.HadrianWalls[index].defPositions);
                            TerrainGenerator.isUniqueBattle(true);
                            break;

                    }
                    return;
                    
            }



            TerrainGenerator.isUniqueBattle(false);


        }
        public static (string X, string Y, string[] attPositions, string[] defPositions) GetBattleMap()
        {
            return FoundMap;
        }

    }
}
