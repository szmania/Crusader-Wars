using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.armies
{
    public class DefensiveSystem
    {
        struct DefensiveSkill
        {
            public static int Terrible() { return 1; }
            public static int Poor() { return 2; }
            public static int Average() { return 3; }
            public static int Good() { return 4; }
            public static int Excellent() { return 5; }
        }

        static int TotalDeployments { get; set; }
        string Text { get; set; }

        public void IncreaseDeployables(int plus_number)
        {
            TotalDeployments += plus_number;
        }

        public int GetTotalDeployments()
        {
            return TotalDeployments;
        }

        public void DecreaseDeployables(int subtract_number)
        {
            TotalDeployments -= subtract_number;
        }

        private static int GetDefensiveLevel(int martial_skill)
        {
            int value = 0;

            if (martial_skill <= 3)
            {
                value += DefensiveSkill.Terrible();

            }
            else if (martial_skill >= 4 && martial_skill <= 7)
            {
                value += DefensiveSkill.Poor();
            }
            else if (martial_skill >= 8 && martial_skill <= 11)
            {
                value += DefensiveSkill.Average();
            }
            else if (martial_skill >= 12 && martial_skill <= 15)
            {
                value += DefensiveSkill.Good();
            }
            else if (martial_skill >= 16)
            {
                value += DefensiveSkill.Excellent();
            }

            return value;

        }
        private  void GetTotalOfDeployments(int army_size)
        {
            double num = army_size / 750;
            double total = Math.Round(num);
            if(total < 1) { total = 1; }
            TotalDeployments = (int)total;
        }
        public DefensiveSystem(int army_size, int martial_skill, int deployables_boost)
        {
            int defensive_level = GetDefensiveLevel(martial_skill);
            GetTotalOfDeployments (army_size);

            int num_spikes = 0;
            int num_ditches = 0;
            int num_caltrops = 0;

            switch (defensive_level)
            {
                /*
                 * Level 1 (Terrible Martial) - low tier ones and reduzed 50% penalty.
                 */
                case 1:

                    //Add Spikes
                    //Half deployments removed
                    num_spikes = (int)Math.Round(TotalDeployments * 0.50);

                    break;
                /*
                 * Level 2 (Poor Martial) - low tier ones and reduzed 25% penalty.
                 */
                case 2:

                    //Add Spikes
                    //A quarter of deployments removed
                    num_spikes = (int)Math.Round(TotalDeployments * 0.75);

                    break;
                    
                /*
                 * Level 3 (Average Martial) - 75% low tier and 25% mid tier.
                 */
                case 3:

                    num_spikes = (int)Math.Round(TotalDeployments * 0.75);
                    num_ditches = (int)Math.Round(TotalDeployments * 0.25);

                    break;
                /*
                 * Level 4 (Good Martial) - 25% high tier & 25% mid tier ones & 50% low tier ones.
                 */
                case 4:
                    num_spikes = (int)Math.Round(TotalDeployments * 0.50);
                    num_ditches = (int)Math.Round(TotalDeployments * 0.25);
                    num_caltrops = (int)Math.Round(TotalDeployments * 0.25);

                    break;
                /*
                 * Level 5 (Excelent Martial) - 30% high tier & 30% mid tier ones & 40% low tier ones.
                 */
                case 5:
                    num_spikes = (int)Math.Round(TotalDeployments * 0.33);
                    num_ditches = (int)Math.Round(TotalDeployments * 0.33);
                    num_caltrops = (int)Math.Round(TotalDeployments * 0.33);

                    //Give extra one
                    num_spikes++;
                    num_ditches++;
                    num_caltrops++;

                    break;
            }

            num_spikes += deployables_boost;


            string text = "<deployables>\n" +
                          $"<deployable_item name=\"deployable_stakes\" num_deployables =\"{num_spikes}\">\n" +
                           "</deployable_item>\n" +
                          $"<deployable_item name=\"deployable_spiketrap\" num_deployables =\"{num_ditches}\">\n" +
                           "</deployable_item>\n" +
                          $"<deployable_item name=\"deployable_caltrop\" num_deployables =\"{num_caltrops}\">\n" +
                          "</deployable_item>\n" + 
                           "</deployables>\n\n";

            TotalDeployments = 0;

            Text = text;
        }

        public string GetText() { return Text; }


    }
}
