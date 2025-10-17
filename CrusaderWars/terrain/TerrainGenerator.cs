using System;
using System.Drawing;
using System.Globalization;
using CrusaderWars.terrain;

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
                Program.Logger.Debug($"Getting strait battle map for terrain: {TerrainType}");
                var battlemap = Straits.GetBattleMap(Region ,TerrainType);
                return battlemap;   
            }

            //River Battle Maps
            if(isRiver) 
            {
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
