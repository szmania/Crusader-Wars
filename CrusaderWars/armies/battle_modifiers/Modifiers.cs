using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CrusaderWars.armies
{
    public class Modifiers
    {
        bool IsDefendingStrait { get; set; }
        bool IsDefendingRiver { get; set; }
        bool isLiegeLeadingArmy { get; set; }
        bool isRealmInDebt { get; set; }
        bool isArmyGathering { get; set; }
        bool isArmyLowOnSupplies { get;set; }
        bool isArmyOutOfSupplies { get; set; }
        bool isArmyRecentlyDisembarked { get; set; }
        bool isFightingHostileFaith {  get; set; }

        public Modifiers(string modifiers_text_side)
        {
            ReadModifiers(modifiers_text_side);
        }

        public int GetXP()
        {
            int xp = 0;
            if (isLiegeLeadingArmy) xp += 1;
            if (isFightingHostileFaith) xp += 1;

            if (isRealmInDebt) xp -= 1;
            if(isArmyGathering) xp -= 1;
            if (isArmyLowOnSupplies) xp -= 1;
            if (isArmyOutOfSupplies) xp -= 2;
            if (isArmyRecentlyDisembarked) xp -= 3;

            return xp;
        }
        
        void ReadModifiers(string modifiers_text_side)
        {

            if (modifiers_text_side.Contains("cw_advantage_strait")) { IsDefendingStrait = true; TerrainGenerator.isStraitBattle(true); }
            if (modifiers_text_side.Contains("cw_advantage_river")) { IsDefendingRiver = true; TerrainGenerator.isRiverBattle(true); }
            if (modifiers_text_side.Contains("cw_advantage_big_river")) { IsDefendingRiver = true; TerrainGenerator.isRiverBattle(true); }
            if (modifiers_text_side.Contains("cw_advantage_leading")) { isLiegeLeadingArmy = true; }
            if (modifiers_text_side.Contains("debt")) { isRealmInDebt = true; }
            if (modifiers_text_side.Contains("cw_advantage_gathering")) { isArmyGathering = true; }
            if (modifiers_text_side.Contains("cw_advantage_lowsupplies")) { isArmyLowOnSupplies = true; }
            if (modifiers_text_side.Contains("cw_advantage_nosupplies")) { isArmyOutOfSupplies = true; }
            if (modifiers_text_side.Contains("faith_hostility")) { isFightingHostileFaith = true; }
            if (modifiers_text_side.Contains("recently_disembarked")) { isArmyRecentlyDisembarked = true; }
        }
        
    }
}
