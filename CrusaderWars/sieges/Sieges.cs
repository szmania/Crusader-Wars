using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.sieges;

namespace CrusaderWars.twbattle
{
    public static class Sieges
    {

        /* level 1/2 - default attila
         * 
         * level 1 -  no walls
         *      holding walls = palisade walls
         * 
         * level 2 - high walls
         */
        // level 3/4 - mk1212 custom cities
        static int HoldingLevel { get; set; }
        static string HoldingCulture { get; set; } = string.Empty;
        static string GarrisonHeritage { get; set; } = string.Empty;
        static string GarrisonCulture { get; set; } = string.Empty;
        static int GarrisonSize { get; set; }
        static string SuppliesLevel { get; set; } = string.Empty;
        static string SicknessLevel { get; set; } = string.Empty;
        static string EscalationLevel { get; set; } = string.Empty;
        static int NumberSiegeEquipement { get; set; }
        static int FortLevel { get; set; }

        public static void SetFortLevel(int a)
        {
            FortLevel = a;
        }

        public static void SetHoldingSupplies(string key)
        {
            SuppliesLevel = key;
        }
        public static void SetHoldingSickness(string key)
        {
            SicknessLevel = key;
        }
        public static void SetHoldingEscalation(string key)
        {
            EscalationLevel = key;
        }

        public static void SetHoldingLevel(string key)
        {
            HoldingLevel = Sieges_DataTypes.Holding.GetLevel(key, Data.Province_Buildings.ToArray());
        }



    }

    
}
