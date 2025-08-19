using Crusader_Wars.terrain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Crusader_Wars
{

    public static class TerrainGenerator
    {
        public static string TerrainType { get; set; }
        public static string Region { get; set; }
        public static bool isUnique { get; set; }
        public static bool isRiver { get; set; }
        public static bool isStrait { get; set; }

        public static void SetRegion(string a)
        {
            Region = a;
        }
        public static void isUniqueBattle(bool yn)
        {
            isUnique = yn;
        }
        public static void isRiverBattle(bool yn)
        {
            isRiver = yn;
        }
        public static void isStraitBattle(bool yn)
        {
            isStrait = yn;
        }

        public static (string X, string Y, string[] attPositions, string[] defPositions) GetBattleMap()
        {
            if (isUnique)
            {
                return UniqueMaps.GetBattleMap();
            }
            else if (isRiver)
            {
                return Rivers.GetBattleMap(TerrainType);
            }
            else if (isStrait)
            {
                return Straits.GetBattleMap(TerrainType);
            }
            else
            {
                return Lands.GetBattleMap(TerrainType);
            }
        }
    }
}
