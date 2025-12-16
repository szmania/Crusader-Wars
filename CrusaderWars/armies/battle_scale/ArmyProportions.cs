using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CrusaderWars.client;

namespace CrusaderWars
{
    public static class ArmyProportions
    {
        public static int BattleScale { get; private set; }
        public static void SetRatio(int a)
        {
            BattleScale = a;
        }

        public static void AutoSizeUnits(int total_soldiers)
        {
            Program.Logger.Debug($"AutoSizeUnits called with total_soldiers: {total_soldiers}");
            if(ModOptions.GetBattleScale() == 0)
            {
                if (total_soldiers <= 10000)
                {
                    BattleScale = 100;
                }
                else if (total_soldiers > 10000 && total_soldiers <= 20000)
                {
                    BattleScale = 75;
                }
                else if (total_soldiers > 20000 && total_soldiers <= 30000)
                {
                    BattleScale = 50;
                }
                else if (total_soldiers > 30000)
                {
                    BattleScale = 25;
                }
            }
            else
            {
                BattleScale = ModOptions.GetBattleScale();
            }
            Program.Logger.Debug($"Determined BattleScale: {BattleScale}");

        }

        public static void ResetUnitSizes()
        {
            Program.Logger.Debug("Resetting unit sizes, BattleScale set to 100.");
            BattleScale = 100;
        }
    }
}
