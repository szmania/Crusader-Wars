using CrusaderWars.terrain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.twbattle
{
    internal static class ArmiesControl
    {


        /*##############################################
         *####           MERGING FUNCTIONS          #### 
         *####--------------------------------------####
         *#### Functions to merge armies to comply  ####
         *####      the Attila army size limit      ####
         *##############################################
         */

        // Merge armies until there are only three
        internal static void MergeArmiesUntilThree(List<Army> armies)
        {
            while (armies.Count > 3)
            {
                for (int i = 3; i < armies.Count; i++)
                {
                    armies[0].AddMergedArmy(armies[i]);
                    //armies[0].Units.AddRange(armies[i].Units);
                    armies.RemoveAt(i);
                }
            }
        }

        // Merge armies until there are only four
        internal static void MergeArmiesUntilFour(List<Army> armies)
        {
            while (armies.Count > 4)
            {
                for (int i = 4; i < armies.Count; i++)
                {
                    armies[1].AddMergedArmy(armies[i]);
                    //armies[0].Units.AddRange(armies[i].Units);
                    armies.RemoveAt(i);
                }
            }
        }
        internal static void MergeIntoOneArmy(List<Army> armies)
        {
            if (armies.Count > 1)
            {
                for (int i = 1; i < armies.Count; i++)
                {
                    armies[0].AddMergedArmy(armies[i]);
                    //armies[0].Units.AddRange(armies[i].Units);
                }
                armies.RemoveRange(1, armies.Count - 1); // Remove all armies except the first one
            }
        }

        internal static Army MergeFriendlies(List<Army> armies, Army main_army)
        {
            string? main_owner = main_army.Owner?.GetID();
            if (main_owner == null)
            {
                Program.Logger.Debug($"Warning: Main army {main_army.ID} has no owner. Cannot merge friendlies.");
                return main_army;
            }
            armies.Remove(main_army);

            for (int i = 0; i < armies.Count; i++)
            {
                if (armies[i].Owner?.GetID() == main_owner)
                {
                    main_army.AddMergedArmy(armies[i]);
                    //main_army.Units.AddRange(armies[i].Units);
                    //armies.RemoveAt(i);
                }
            }

            if (main_army.MergedArmies != null)
            {
                foreach (Army merged_army in main_army.MergedArmies)
                {
                    armies.Remove(merged_army);
                }
            }

            return main_army;
        }

        internal static void MergeSiegeDefendersForCombinedArmyTag(List<Army> defender_armies)
        {
            if (defender_armies.Count <= 1) return;

            var baseArmy = defender_armies.FirstOrDefault(a => a.IsGarrison()) ?? defender_armies[0];

            defender_armies.Remove(baseArmy);
            defender_armies.Insert(0, baseArmy);

            var otherArmies = defender_armies.Skip(1).ToList();
            foreach (var army in otherArmies)
            {
                baseArmy.AddMergedArmy(army);
                army.isMainArmy = false; 
            }

            defender_armies.RemoveRange(1, defender_armies.Count - 1);
        }
    }
}
