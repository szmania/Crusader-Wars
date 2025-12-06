using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.data.save_file;
using CrusaderWars.client; // Added for ModOptions

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
        int BaseSoldiers { get; set; }
        public int Rank { get; private set; }
        List<(int, string)>? Traits { get; set; }
        BaseSkills? BaseSkills { get; set; }
        bool hasFallen { get; set; }
        public bool IsSlain { get; private set; }
        public bool IsPrisoner { get; private set; }
        int Kills { get; set; }

        bool isAccoladeKnight { get; set; }
        Accolade? Accolade { get; set; }
        private bool _isProminent;
        public bool IsProminent
        {
            get { return _isProminent; }
            set
            {
                _isProminent = value;
                Soldiers = SetStrengh(BaseSoldiers);
            }
        }

        private KnightSystem? _knightSystem; // Added: Reference to parent KnightSystem

        public string GetName() {  return Name; }
        public string GetID() { return ID; }
        public string GetCultureName() { return CultureObj?.GetCultureName() ?? "unknown_culture"; }
        public string GetHeritageName() { return CultureObj?.GetHeritageName() ?? "unknown_heritage"; }
        public Culture GetCultureObj() { return CultureObj; }
        public int GetSoldiers() { return Soldiers; }
        public int GetProwess() { return Prowess; }
        public bool IsAccolade() { return isAccoladeKnight; }
        public Accolade? GetAccolade() { return Accolade; } 
        public bool HasFallen() { return hasFallen; }
        public int GetKills() { return Kills; }
        public List<(int, string)> GetTraits() { return Traits ?? new List<(int, string)>(); }

        internal void SetHasFallen(bool yn) { hasFallen = yn; }
        public void SetKills(int kills) { Kills = kills; }
        public void ChangeCulture(Culture cul) { CultureObj = cul; }
        public void SetTraits(List<(int, string)> list_trait) { Traits = list_trait; }
        public void IsAccolade(bool yn, Accolade accolade) { isAccoladeKnight = yn; Accolade = accolade; Soldiers += 4; }
        public void SetBaseSkills(BaseSkills t) { BaseSkills =  t; }
        public void SetKnightSystem(KnightSystem ks) { _knightSystem = ks; } // Added: Setter for KnightSystem


        internal Knight(string name, string id, Culture culture, int prowess, int soldiers) { 
            Name = name;
            ID = id;
            CultureObj = culture;
            Prowess = prowess;
            BaseSoldiers = soldiers;
            _isProminent = false;
            Soldiers = SetStrengh(soldiers);
            SetRank();
        }
        

        private void SetRank()
        {
            if (Prowess >= 20)
            {
                Rank = 3; // Elite
            }
            else if (Prowess >= 10)
            {
                Rank = 2; // Skilled
            }
            else
            {
                Rank = 1; // Average
            }
        }
        public void SetWoundedDebuffs()
        {
            int debuff = 0;

            //Health soldiers debuff
            if (Traits != null)
            {
                foreach(var trait in Traits)
                {
                    if (trait.Item1 == WoundedTraits.Wounded()) debuff += -1;
                    if (trait.Item1 == WoundedTraits.Severely_Injured()) debuff += -2;
                    if (trait.Item1 == WoundedTraits.Brutally_Mauled()) debuff += -3;
                    if (trait.Item1 == WoundedTraits.Maimed()) debuff += -2;
                    if (trait.Item1 == WoundedTraits.One_Eyed()) debuff += -1;
                    if (trait.Item1 == WoundedTraits.One_Legged()) debuff += -2;
                    if (trait.Item1 == WoundedTraits.Disfigured()) debuff += -1;
                }
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

            int finalSoldiers = soldiers + value;
            if (_isProminent && !ModOptions.CombineKnightsEnabled())
            {
                finalSoldiers *= 2;
            }
            return finalSoldiers;
        }

        public void CalculateMAACommanderFate(double casualty_percentage)
        {
            if (casualty_percentage <= 0) return;

            // NEW: Guarantee fall if unit is 100% wiped out
            if (casualty_percentage >= 1.0)
            {
                hasFallen = true;
                Program.Logger.Debug($"Knight {Name} ({ID}) commanding an MAA unit has fallen. Unit Casualties: 100%.");
                return;
            }

            // Prowess provides a survival bonus (reduces chance of falling)
            double prowess_survival_bonus = 0.0;
            if (Prowess >= 17) prowess_survival_bonus = 0.15;       // 15% bonus for Excellent
            else if (Prowess >= 13) prowess_survival_bonus = 0.10;  // 10% bonus for Good
            else if (Prowess >= 9) prowess_survival_bonus = 0.05;   // 5% bonus for Average
            
            double final_chance_to_fall = casualty_percentage - prowess_survival_bonus;

            // Ensure chance is not negative
            if (final_chance_to_fall < 0) final_chance_to_fall = 0;

            // Roll the dice
            var random_roll = CharacterSharedRandom.Rng.NextDouble(); // Returns a value between 0.0 and 1.0

            if (random_roll <= final_chance_to_fall)
            {
                hasFallen = true;
                Program.Logger.Debug($"Knight {Name} ({ID}) commanding an MAA unit has fallen. Unit Casualties: {casualty_percentage:P2}, Prowess: {Prowess}, Survival Bonus: {prowess_survival_bonus:P2}, Final Chance: {final_chance_to_fall:P2}, Roll: {random_roll:P2}.");
            }
            else
            {
                Program.Logger.Debug($"Knight {Name} ({ID}) commanding an MAA unit survived. Unit Casualties: {casualty_percentage:P2}, Prowess: {Prowess}, Survival Bonus: {prowess_survival_bonus:P2}, Final Chance: {final_chance_to_fall:P2}, Roll: {random_roll:P2}.");
            }
        }

        public (bool isSlain, bool isCaptured, string newTraits) Health(string traits_line, bool wasOnLosingSide)
        {
            if (hasFallen)
            {
                // Get percentages from options
                int slainChance = client.ModOptions.GetKnightSlainChance();
                int woundedChance = client.ModOptions.GetKnightWoundedChance();
                int severelyInjuredChance = client.ModOptions.GetKnightSeverelyInjuredChance();
                int brutallyMauledChance = client.ModOptions.GetKnightBrutallyMauledChance();
                int maimedChance = client.ModOptions.GetKnightMaimedChance();
                int oneLeggedChance = client.ModOptions.GetKnightOneLeggedChance();
                int oneEyedChance = client.ModOptions.GetKnightOneEyedChance();
                int disfiguredChance = client.ModOptions.GetKnightDisfiguredChance();
                int prisonerChance = client.ModOptions.GetKnightPrisonerChance();

                // Calculate cumulative thresholds
                int slainThreshold = slainChance;
                int woundedThreshold = slainThreshold + woundedChance;
                int severelyInjuredThreshold = woundedThreshold + severelyInjuredChance;
                int brutallyMauledThreshold = severelyInjuredThreshold + brutallyMauledChance;
                int maimedThreshold = brutallyMauledThreshold + maimedChance;
                int oneLeggedThreshold = maimedThreshold + oneLeggedChance;
                int oneEyedThreshold = oneLeggedThreshold + oneEyedChance;
                int disfiguredThreshold = oneEyedThreshold + disfiguredChance;

                var RandomNumber = CharacterSharedRandom.Rng.Next(1, 101); // 1 to 100

                // Determine which option to set based on its percentage chance
                if (RandomNumber <= slainThreshold)
                {
                    Program.Logger.Debug($"Knight {ID} has been slain in battle (chance: {slainChance}%).");
                    IsSlain = true;
                    return (true, false, traits_line);
                }

                string newTraits = traits_line;
                if (RandomNumber <= woundedThreshold)
                {
                    Program.Logger.Debug("A Knight was Wounded");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Wounded().ToString());
                }
                else if (RandomNumber <= severelyInjuredThreshold)
                {
                    Program.Logger.Debug("A Knight was Severely Injured");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Severely_Injured().ToString());
                }
                else if (RandomNumber <= brutallyMauledThreshold)
                {
                    Program.Logger.Debug("A Knight was Brutally Mauled");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Brutally_Mauled().ToString());
                }
                else if (RandomNumber <= maimedThreshold)
                {
                    Program.Logger.Debug("A Knight was Maimed");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Maimed().ToString());
                }
                else if (RandomNumber <= oneLeggedThreshold)
                {
                    Program.Logger.Debug("A Knight got One Legged");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.One_Legged().ToString());
                }
                else if (RandomNumber <= oneEyedThreshold)
                {
                    Program.Logger.Debug("A Knight got One Eyed");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.One_Eyed().ToString());
                }
                else if (RandomNumber <= disfiguredThreshold)
                {
                    Program.Logger.Debug("A Knight got Disfigured");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Disfigured().ToString());
                }

                // If survived, roll for capture
                bool isCaptured = false;
                int effectivePrisonerChance = wasOnLosingSide ? prisonerChance : (int)Math.Round(prisonerChance * 0.25);
                var prisonerRng = CharacterSharedRandom.Rng.Next(1, 101);
                isCaptured = prisonerRng <= effectivePrisonerChance;
                if (isCaptured)
                {
                    IsPrisoner = true;
                    string side = wasOnLosingSide ? "losing" : "winning";
                    Program.Logger.Debug($"Knight {ID} has been captured from the {side} side (chance: {effectivePrisonerChance}%).");
                }

                return (false, isCaptured, newTraits);
            }

            return (false, false, traits_line);
        }

        public int GetProminentKnightExperienceBoost()
        {
            double prowess_contribution = 0.0;
            if (Prowess <= 4) prowess_contribution = 0.0;
            else if (Prowess >= 5 && Prowess <= 8) prowess_contribution = 0.1;
            else if (Prowess >= 9 && Prowess <= 12) prowess_contribution = 0.2;
            else if (Prowess >= 13 && Prowess <= 16) prowess_contribution = 0.3;
            else if (Prowess >= 17) prowess_contribution = 0.4;

            // A prominent knight's prowess has a greater impact on a single unit's experience.
            // We scale the contribution to make it significant after rounding.
            // A scaling factor of 5 makes an excellent knight contribute 2 points before effectiveness.
            double scaled_prowess_value = prowess_contribution * 5;

            int prowess_level = (int)Math.Round(scaled_prowess_value);

            // Safely get effectiveness from the KnightSystem
            if (_knightSystem == null)
            {
                Program.Logger.Debug("WARNING: Knight's KnightSystem is not set. Cannot calculate effectiveness boost.");
                return prowess_level;
            }
            int effectiveness = _knightSystem.Effectiveness; // Modified: Get from _knightSystem
            double effectiveness_bonus = 0;
            if (prowess_level > 0)
            {
                double multiplier = (double)effectiveness / 100.0; // Fixed: Integer division
                effectiveness_bonus = prowess_level * multiplier;
            }

            int final_level = (int)Math.Round(prowess_level + effectiveness_bonus);
            return final_level;
        }

    }
    public class KnightSystem
    {
        private List<Knight> Knights { get; set; }
        private Culture? MajorCulture { get; set; }
        private List<Accolade>? Accolades { get; set; }
        private int UnitSoldiers { get; set; }

        public int Effectiveness { get; private set; }

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
                foreach(var knight in Knights) // Added: Set back-reference to KnightSystem
                {
                    knight.SetKnightSystem(this);
                }
            }
            else
            {
                Knights = new List<Knight>(); // ADDED THIS LINE
                hasKnights = false;
            }

        }

        public bool HasKnights() { return hasKnights; }
        public Culture? GetMajorCulture() { return MajorCulture; }
        public List<Knight> GetKnightsList()
        {
            return Knights;
        }

        public List<Accolade>? GetAccolades()
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
                MajorCulture = Knights.FirstOrDefault(x => x.GetCultureObj() != null)?.GetCultureObj(); // ADDED NULL-CONDITIONAL OPERATOR
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
                        if (knight.GetAccolade() != null)
                        {
                            Accolades.Add(knight.GetAccolade()!);
                        }
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
                double mulltiplier = Effectiveness / 100.0; // Fixed: Integer division

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
                // This method is now only for the combined, standard knights unit.
                // All knights in the KnightSystem (who are not already fallen) are part of the combined bodyguard unit
                // unless they are explicitly leading an MAA unit (which is handled in BattleProcessor.ProcessProminentKnights).
                // Therefore, all remaining knights are subject to casualties.
                var eligibleKnights = Knights.Where(k => !k.HasFallen()).ToList();
                if (!eligibleKnights.Any()) return;

                int totalSoldiers = eligibleKnights.Sum(k => k.GetSoldiers());
                int remainingSoldiers = remaining;

                int soldiers_lost = totalSoldiers - remainingSoldiers;
                if (soldiers_lost <= 0) return;

                // Find the weakest knight to ensure we can start removing knights
                int weakest_knight_num = eligibleKnights.Min(x => x.GetSoldiers());
                if (weakest_knight_num == 0) weakest_knight_num = 1; // Avoid infinite loop if a knight has 0 soldiers

                List<Knight> tempKnightsList = new List<Knight>(eligibleKnights);
                while (soldiers_lost >= weakest_knight_num && tempKnightsList.Any())
                {
                    Random random = new Random();
                    int random_index = random.Next(tempKnightsList.Count);
                    var knight = tempKnightsList[random_index];

                    soldiers_lost -= knight.GetSoldiers();
                    
                    knight.SetHasFallen(true);
                    tempKnightsList.Remove(knight);

                    // Update weakest_knight_num in case the weakest was removed
                    if (tempKnightsList.Any())
                    {
                        weakest_knight_num = tempKnightsList.Min(x => x.GetSoldiers());
                        if (weakest_knight_num == 0) weakest_knight_num = 1;
                    }
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
