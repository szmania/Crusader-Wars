using Crusader_Wars.client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Crusader_Wars
{
    public static class ArmyProportions
    {
        static int BattleScale { get; set; }
        public static void SetRatio(int a)
        {
            BattleScale = a;
        }

        public static void AutoSizeUnits(int total_soldiers)
        {
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

        }

        public static void ResetUnitSizes()
        {
            BattleScale = 100;
        }
    }
}
