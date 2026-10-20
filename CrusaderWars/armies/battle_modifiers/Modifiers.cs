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
        bool isArmyLowOnSupplies { get; set; }
        bool isArmyOutOfSupplies { get; set; }
        bool isArmyRecentlyDisembarked { get; set; }
        bool isFightingHostileFaith { get; set; }
        bool isVictoriousMomentum { get; set; }
        bool isHumblingDefeat { get; set; }
        bool isDecisiveVictory { get; set; }

        public Modifiers(string modifiers_text_side)
        {
            ReadModifiers(modifiers_text_side);
        }

        public int GetXP()
        {
            int xp = 0;
            if (isLiegeLeadingArmy) xp += 1;
            if (isFightingHostileFaith) xp += 1;
            if (isVictoriousMomentum) xp += 1;
            if (isDecisiveVictory) xp += 1;

            if (isRealmInDebt) xp -= 1;
            if (isArmyGathering) xp -= 1;
            if (isArmyLowOnSupplies) xp -= 1;
            if (isArmyOutOfSupplies) xp -= 2;
            if (isArmyRecentlyDisembarked) xp -= 3;
            if (isHumblingDefeat) xp -= 1;

            return xp;
        }

        public List<string> GetModifierDescriptions()
        {
            var descriptions = new List<string>();

            if (isVictoriousMomentum) descriptions.Add("Victorious Momentum (+1 XP)");
            if (isDecisiveVictory) descriptions.Add("Decisive Victory (+1 XP)");
            if (isLiegeLeadingArmy) descriptions.Add("Leader Commanding (+1 XP)");
            if (isFightingHostileFaith) descriptions.Add("Fighting a Hostile Faith (+1 XP)");
            if (IsDefendingStrait) descriptions.Add("Defending a Strait");
            if (IsDefendingRiver) descriptions.Add("Defending a River");
            if (isHumblingDefeat) descriptions.Add("Humbling Defeat (-1 XP)");
            if (isRealmInDebt) descriptions.Add("Realm in Debt (-1 XP)");
            if (isArmyGathering) descriptions.Add("Army Gathering (-1 XP)");
            if (isArmyLowOnSupplies) descriptions.Add("Low on Supplies (-1 XP)");
            if (isArmyOutOfSupplies) descriptions.Add("No Supplies (-2 XP)");
            if (isArmyRecentlyDisembarked) descriptions.Add("Recently Disembarked (-3 XP)");

            return descriptions;
        }

        void ReadModifiers(string modifiers_text_side)
        {

            if (modifiers_text_side.Contains("cc_advantage_strait")) { IsDefendingStrait = true; TerrainGenerator.isStraitBattle(true); }
            if (modifiers_text_side.Contains("cc_advantage_river")) { IsDefendingRiver = true; TerrainGenerator.isRiverBattle(true); }
            if (modifiers_text_side.Contains("cc_advantage_big_river")) { IsDefendingRiver = true; TerrainGenerator.isRiverBattle(true); }
            if (modifiers_text_side.Contains("cc_advantage_leading")) { isLiegeLeadingArmy = true; }
            if (modifiers_text_side.Contains("debt")) { isRealmInDebt = true; }
            if (modifiers_text_side.Contains("cc_advantage_gathering")) { isArmyGathering = true; }
            if (modifiers_text_side.Contains("cc_advantage_lowsupplies")) { isArmyLowOnSupplies = true; }
            if (modifiers_text_side.Contains("cc_advantage_nosupplies")) { isArmyOutOfSupplies = true; }
            if (modifiers_text_side.Contains("faith_hostility")) { isFightingHostileFaith = true; }
            if (modifiers_text_side.Contains("recently_disembarked")) { isArmyRecentlyDisembarked = true; }
            if (modifiers_text_side.Contains("cc_victorious_momentum")) { isVictoriousMomentum = true; }
            if (modifiers_text_side.Contains("cc_decisive_victory")) { isDecisiveVictory = true; }
            if (modifiers_text_side.Contains("cc_humbling_defeat")) { isHumblingDefeat = true; }
        }

    }
}
