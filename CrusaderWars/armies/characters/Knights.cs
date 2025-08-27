using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.data.save_file;

namespace CrusaderWars
{
    public class Accolade
    {
        string ID {  get; set; }
        string PrimaryAttribute { get; set;}
        string SecundaryAttribute { get; set; }
        int Glory { get; set; }
        public Accolade(string id, string primaryAtt, string secundaryAtt, string glory) { 
            ID = id;
            PrimaryAttribute = primaryAtt;
            SecundaryAttribute = secundaryAtt;
            Glory = Int32.Parse(glory);
        }

        public string GetPrimaryAttribute() { return PrimaryAttribute;}
        public string GetSecundaryAttribute() { return SecundaryAttribute; }
        public int GetGlory() { return Glory; }
    }
    public class Knight
    {
        string Name { get; set; }
        string ID { get; set; }
        Culture CultureObj { get; set; }
        int Prowess { get; set; }
        int Soldiers { get; set; }
        List<(int Index, string Key)> Traits { get; set; }
        BaseSkills BaseSkills { get; set; }
        bool hasFallen { get; set; }
        int Kills { get; set; }

        bool isAccoladeKnight { get; set; }
        Accolade Accolade { get; set; }

        public string GetName() {  return Name; }
        public string GetID() { return ID; }
        public string GetCultureName() { return CultureObj.GetCultureName(); }
        public string GetHeritageName() { return CultureObj.GetHeritageName(); }
        public Culture GetCultureObj() { return CultureObj; }
        public int GetSoldiers() { return Soldiers; }
        public int GetProwess() { return Prowess; }
        public bool IsAccolade() { return isAccoladeKnight; }
        public Accolade GetAccolade() { return Accolade; } 
        public bool HasFallen() { return hasFallen; }
        public int GetKills() { return Kills; }

        internal void HasFallen(bool yn) { hasFallen = yn; }
        public void SetKills(int kills) { Kills = kills; }
        public void ChangeCulture(Culture cul) { CultureObj = cul; }
        public void SetTraits(List<(int, string)> list_trait) { Traits = list_trait; }
        public void IsAccolade(bool yn, Accolade accolade) { isAccoladeKnight = yn; Accolade = accolade; Soldiers += 4; }
        public void SetBaseSkills(BaseSkills t) { BaseSkills =  t; }


        internal Knight(string name, string id, Culture culture, int prowess, int soldiers) { 
            Name = name;
            ID = id;
            CultureObj = culture;
            Prowess = prowess;
            Soldiers = SetStrengh(soldiers);
        }
        

        public void SetWoundedDebuffs()
        {
            int debuff = 0;

            //Health soldiers debuff
            foreach(var trait in Traits)
            {
                if (trait.Index == WoundedTraits.Wounded()) debuff += -1;
                if (trait.Index == WoundedTraits.Severely_Injured()) debuff += -2;
                if (trait.Index == WoundedTraits.Brutally_Mauled()) debuff += -3;
                if (trait.Index == WoundedTraits.Maimed()) debuff += -2;
                if (trait.Index == WoundedTraits.One_Eyed()) debuff += -1;
                if (trait.Index == WoundedTraits.One_Legged()) debuff += -2;
                if (trait.Index == WoundedTraits.Disfigured()) debuff += -1;
            }


            Soldiers += debuff;
        }


        int SetStrengh(int soldiers)
        {
            int value = 0;
            if (Prowess <= 4)
            {
                value += 0;
            }
            else if (Prowess >= 5 && Prowess <= 8)
            {
                value += 1;
            }
            else if (Prowess >= 9 && Prowess <= 12)
            {
                value += 2;
            }
            else if (Prowess >= 13 && Prowess <= 16)
            {
                value += 3;
            }
            else if (Prowess >= 17)
            {
                value += 4;
            }

            return soldiers + value;
        }

        public string Health(string traits_line)
        {
            if (hasFallen)
            {
                //50% Chance Light Wounds
                const int WoundedChance = 50; // 50%

                //40% Chance Harsh Wounds
                const int Severely_InjuredChance = 70; // 20%
                const int Brutally_MauledChance = 90; // 20%

                //10% Chance Extreme Wounds
                const int MaimedChance = 93; // 3%
                const int One_LeggedChance = 96;// 3%
                const int One_EyedChance = 99;// 3%
                const int Disfigured = 100; //1%

                var Chance = new Random();
                int RandomNumber;

                RandomNumber = Chance.Next(101);

                // Determine which option to set based on its percentage chance
                if (RandomNumber >= 0 && RandomNumber <= WoundedChance)
                {
                    Program.Logger.Debug("A Knight was Wounded ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Wounded().ToString());
                }
                else if (RandomNumber > WoundedChance && RandomNumber <= Severely_InjuredChance)
                {
                    Program.Logger.Debug("A Knight was Severely_Injured ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Severely_Injured().ToString());
                }
                else if (RandomNumber > Severely_InjuredChance && RandomNumber <= Brutally_MauledChance)
                {
                    Program.Logger.Debug("A Knight was Brutally Mauled ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Brutally_Mauled().ToString());
                }
                else if (RandomNumber > Brutally_MauledChance && RandomNumber <= MaimedChance)
                {
                    Program.Logger.Debug("A Knight was Maimed ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Maimed().ToString());
                }
                else if (RandomNumber > MaimedChance && RandomNumber <= One_LeggedChance)
                {
                    Program.Logger.Debug("A Knight got One Legged ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.One_Legged().ToString());
                }
                else if (RandomNumber > One_LeggedChance && RandomNumber <= One_EyedChance)
                {
                    Program.Logger.Debug("A Knight got One Eyed ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.One_Eyed().ToString());
                }
                else if (RandomNumber > One_EyedChance && RandomNumber <= Disfigured)
                {
                    Program.Logger.Debug("A Knight got Disfigured ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Disfigured().ToString());
                }
            }

            return traits_line;

        }

    }
    public class KnightSystem
    {
        private List<Knight> Knights { get; set; }
        private Culture MajorCulture { get; set; }
        private List<Accolade> Accolades { get; set; }
        private int UnitSoldiers { get; set; }

        private int Effectiveness { get; set; }

        //private List<Knight> KilledKnights { get; set; }
        private bool hasKnights { get; set; }


        public KnightSystem(List<Knight> data, int effectiveness)
        {
            if (data.Count > 0)
            {
                Knights = data;
                Effectiveness = effectiveness;
                hasKnights = true;
                SetKnightsCount();
            }
            else
            {
                hasKnights = false;
            }

        }

        public bool HasKnights() { return hasKnights; }
        public Culture GetMajorCulture() { return MajorCulture; }
        public List<Knight> GetKnightsList()
        {
            return Knights;
        }

        public List<Accolade> GetAccolades()
        {
            return Accolades;
        }

        public void SetMajorCulture()
        {
              MajorCulture = Knights.GroupBy(knight => knight.GetCultureObj())
                                    .OrderByDescending(group => group.Count())
                                    .Select(group => group.Key)
                                    .FirstOrDefault();

              if(MajorCulture == null)
                MajorCulture = Knights.FirstOrDefault(x => x.GetCultureObj() != null).GetCultureObj();
        }


        public void SetAccolades()
        {
            if(Knights.Exists(x=> x.IsAccolade()))
            {
                Accolades = new List<Accolade>();
                foreach (var knight in Knights)
                {
                    if(knight.IsAccolade())
                    {
                        Accolades.Add(knight.GetAccolade());
                    }
                }
            }
            else
            {
                Accolades = null;
            }
        }


        private double KnightEffectiveness(int level)
        {
            if (level > 0)
            {
                double mulltiplier = Effectiveness / 100;

                double value_to_increase = level * mulltiplier;
                return value_to_increase;
            }
            else
            {
                return 0;
            }

        }

        public void GetKills(int kills)
        {
            if(hasKnights)
            {
                Random random = new Random();

                // Calculate the total strength of all knights
                int totalStrength = Knights.Sum(knight => knight.GetSoldiers());

                // Calculate proportional kills based on strength
                int remainingKills = kills;
                for (int i = 0; i < Knights.Count; i++)
                {
                    // For all except the last knight
                    if (i < Knights.Count - 1)
                    {
                        // Calculate the proportion of kills based on strength
                        double proportion = (double)Knights[i].GetSoldiers() / totalStrength;
                        int knightKills = (int)Math.Round(proportion * kills);

                        // Randomize the number of kills slightly to add some variability
                        knightKills = random.Next(knightKills - 2, knightKills + 3);

                        // Ensure the randomized number of kills is within bounds
                        knightKills = Math.Max(0, Math.Min(knightKills, remainingKills));

                        Knights[i].SetKills(knightKills);
                        remainingKills -= knightKills;
                    }
                    else
                    {
                        // Assign the remaining kills to the last knight
                        Knights[i].SetKills(remainingKills);
                    }
                }
            }
        }

        public void GetKilled(int remaining)
        {
            if (hasKnights)
            {
                //KilledKnights = new List<Knight>();

                int totalSoldiers = UnitSoldiers;
                int remainingSoldiers = remaining;


                //random knight
                int soldiers_lost = totalSoldiers - remainingSoldiers;
                int weakest_knight_num = Knights.Select(x => x.GetSoldiers()).Min();
                List<Knight> tempKnightsList = new List<Knight>();
                tempKnightsList.AddRange(Knights);
                while (soldiers_lost >= weakest_knight_num)
                {
                    if (tempKnightsList.Count == 0) break;

                    Random random = new Random();
                    int random_index = random.Next(tempKnightsList.Count);
                    var knight = tempKnightsList[random_index];

                    soldiers_lost -= knight.GetSoldiers();

                    //if (soldiers_lost <= 0) break;
                    
                    knight.HasFallen(true);
                    tempKnightsList.Remove(knight);

                }


            }


        }


        public int SetExperience()
        {
            int prowess_level = (int)ProwessExperience();
            double effectiveness = KnightEffectiveness(prowess_level);
            int level = (int)Math.Round(prowess_level + effectiveness);

            return level;
        }

        public int GetKnightsSoldiers()
        {
            int num = 0;
            foreach(Knight knight in Knights)
            {
                num += knight.GetSoldiers();
            }
            UnitSoldiers = num;
            return UnitSoldiers;
        }


        private double ProwessExperience()
        {
            if (hasKnights)
            {
                double value = 0;

                foreach (var knight_value in Knights)
                {
                    if (knight_value.GetProwess() <= 4)
                    {
                        value += Strength.Terrible();
                    }
                    else if (knight_value.GetProwess() >= 5 && knight_value.GetProwess() <= 8)
                    {
                        value += Strength.Poor();
                    }
                    else if (knight_value.GetProwess() >= 9 && knight_value.GetProwess() <= 12)
                    {
                        value += Strength.Average();
                    }
                    else if (knight_value.GetProwess() >= 13 && knight_value.GetProwess() <= 16)
                    {
                        value += Strength.Good();
                    }
                    else if (knight_value.GetProwess() >= 17)
                    {
                        value += Strength.Excellent();
                    }


                    //Max level of experience
                    if (value >= 9)
                    {
                        value = 9;
                        break;
                    }
                }

                double rounded = Math.Round(value);
                return rounded;
            }
            else
            {
                return 0;
            }


        }


        private void SetKnightsCount()
        {
            UnitSoldiers = 0;

            if (hasKnights)
            {
                for (int i = 0; i < Knights.Count; i++)
                {
                    UnitSoldiers += Knights[i].GetSoldiers();
                }

            }
        }


    }
}
