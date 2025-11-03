using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.armies.commander_traits;
using CrusaderWars.data.save_file;

namespace CrusaderWars
{
    public class CommanderSystem
    {
        private class CourtPosition
        {
            string Profession { get; set; }
            string Employee_ID { get; set; }


            public CourtPosition(string profession, string employee_ID)
            {
                Profession = profession;
                Employee_ID = employee_ID;
            }
        }



        public string ID { get; private set; }
        public string Name { get; private set; }
        public int Rank { get; private set; }
        public int Martial { get; private set; }
        public int Prowess { get; private set; }
        private Culture? CultureObj { get; set; }
        public List<(int Index, string Key)> Traits_List { get; private set; } = new List<(int, string)>(); // INITIALIZED HERE

        public BaseSkills? BaseSkills { get; private set; }
        public CommanderTraits? CommanderTraits { get; private set; }
        private List<CourtPosition>? Employees { get; set; }

        public bool hasFallen { get; private set; }
        private bool MainCommander {  get; set; }
        private Accolade? Accolade { get; set; }
        private bool IsAccoladeCommander { get; set; }

        /// <summary>
        /// New object for MAIN COMMANDERS only.
        /// </summary>
        public CommanderSystem(string name, string id, int prowess, int martial, int rank, bool mainCommander)
        {
            Name = name;
            ID = id;
            Prowess = prowess;
            Martial = martial;
            Rank = rank;
            MainCommander = mainCommander;
        }

        /// <summary>
        /// New object for NON-MAIN COMMANDERS.
        /// </summary>
        public CommanderSystem(string name, string id, int prowess, int rank, BaseSkills baseSkills, Culture culture)
        {
            Name = name;
            ID = id;
            Prowess = prowess;
            Martial = baseSkills.martial;
            Rank = rank;
            BaseSkills = baseSkills;
            CultureObj = culture;
            MainCommander = false;
        }
        public void ChangeCulture(Culture obj) {CultureObj = obj;}
        public void SetBaseSkills(BaseSkills t) { BaseSkills = t; }
        public void SetAccolade(Accolade accolade) { Accolade = accolade; IsAccoladeCommander = true; }

        public string GetID() { return ID; }
        public string GetCultureName() { return CultureObj?.GetCultureName() ?? "unknown_culture"; } // NULL-SAFE
        public string GetHeritageName() { return CultureObj?.GetHeritageName() ?? "unknown_heritage"; } // NULL-SAFE
        public Culture? GetCultureObj () { return CultureObj; } // NULLABLE RETURN TYPE
        public bool IsMainCommander() { return MainCommander; }
        public Accolade? GetAccolade() { return Accolade; }
        public bool IsAccolade() { return IsAccoladeCommander; }






        public void AddCourtPosition(string profession, string id)
        {
            if (Employees is null) Employees = new List<CourtPosition>();
            Employees.Add(new CourtPosition(profession, id));
        }

        public int GetUnitSoldiers()
        {
            return UnitSoldiers();
        }


        public int GetUnitsExperience()
        {
            return MartialArmyExperience();
        }

        public int GetCommanderExperience()
        {
            return (int)Math.Round(ProwessExperience() + (ProwessExperience() * MartialExperience()));
        }

        public int GetCommanderStarRating()
        {
            return StarExperience();
        }

        public void SetTraits(List<(int, string)> traits)
        {
            Traits_List = traits;

            if(MainCommander)
            {
                CommanderTraits = new CommanderTraits(Traits_List);
            }
        }

        int UnitSoldiers()
        {

            //Title rank soldiers
            int soldiers = 0;
            switch (Rank)
            {
                case 1:
                    soldiers = 10;
                    break;
                case 2:
                    soldiers = 20;
                    break;
                case 3:
                    soldiers = 30;
                    break;
                case 4:
                    soldiers = 50;
                    break;
                case 5:
                    soldiers = 70;
                    break;
                case 6:
                    soldiers = 90;
                    break;
            }

            //Prowess soldiers
            int prowess = Prowess;
            if (prowess <= 4)
            {
                soldiers += 0;

            }
            else if (prowess >= 5 && prowess <= 8)
            {
                soldiers += 5;
            }
            else if (prowess >= 9 && prowess <= 12)
            {
                soldiers += 10;
            }
            else if (prowess >= 13 && prowess <= 16)
            {
                soldiers += 15;
            }
            else if (prowess >= 17)
            {
                soldiers += 20;
            }

            //Court positions soldiers
            if (Employees != null)
            {
                int courtiers = Employees.Count * 5;
                soldiers += courtiers;
            }

            //Health soldiers debuff
            foreach(var trait in Traits_List)
            {
                if (trait.Index == WoundedTraits.Wounded()) soldiers += -5;
                if (trait.Index == WoundedTraits.Severely_Injured()) soldiers += -10;
                if (trait.Index == WoundedTraits.Brutally_Mauled()) soldiers += -15;
                if (trait.Index == WoundedTraits.Maimed()) soldiers += -10;
                if (trait.Index == WoundedTraits.One_Eyed()) soldiers += -5;
                if (trait.Index == WoundedTraits.One_Legged()) soldiers += -10;
                if (trait.Index == WoundedTraits.Disfigured()) soldiers += -5;
            }

            

            //Minimum of 1 soldier
            if (soldiers < 1) soldiers = 1;

            return soldiers;
        }


        int StarExperience()
        {
            int martial = Martial;
            int value = 0;

            if (martial <= 3)
            {
                //Terrible Martial
                value += 1;

            }
            else if (martial >= 4 && martial <= 7)
            {
                //Poor Martial
                value += 2;
            }
            else if (martial >= 8 && martial <= 11)
            {
                //Averege Martial
                value += 3;
            }
            else if (martial >= 12 && martial <= 15)
            {
                //Good Martial
                value += 4;
            }
            else if (martial >= 16)
            {
                //Excelent Martial
                value += 5;
            }

            if (value > 9) value = 9;
            return value;
        }

        double ProwessExperience()
        {
            int prowess = Prowess;
            int value = 0;
            if (prowess <= 4)
            {
                value += MartialSkill.Terrible();

            }
            else if (prowess >= 5 && prowess <= 8)
            {
                value += MartialSkill.Poor();
            }
            else if (prowess >= 9 && prowess <= 12)
            {
                value += MartialSkill.Average();
            }
            else if (prowess >= 13 && prowess <= 16)
            {
                value += MartialSkill.Good();
            }
            else if (prowess >= 17)
            {
                value += MartialSkill.Excellent();
            }

            return value;
        }

        double MartialExperience()
        {
            int martial = Martial;
            double value = 0;

            if (martial <= 3)
            {
                value += 0.0;

            }
            else if (martial >= 4 && martial <= 7)
            {
                value += 0.2;
            }
            else if (martial >= 8 && martial <= 11)
            {
                value += 0.4;
            }
            else if (martial >= 12 && martial <= 15)
            {
                value += 0.6;
            }
            else if (martial >= 16)
            {
                value += 0.8;
            }


            return value;
        }

        int MartialArmyExperience()
        {
            int martial = Martial;
            int value = 0;

            if (martial <= 3)
            {
                value += MartialSkill.Terrible();

            }
            else if (martial >= 4 && martial <= 7)
            {
                value += MartialSkill.Poor();
            }
            else if (martial >= 8 && martial <= 11)
            {
                value += MartialSkill.Average();
            }
            else if (martial >= 12 && martial <= 15)
            {
                value += MartialSkill.Good();
            }
            else if (martial >= 16)
            {
                value += MartialSkill.Excellent();
            }


            return value;

        }

        
        public void HasGeneralFallen(string path_attila_log)
        {
            using (FileStream logFile = File.Open(path_attila_log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(logFile))
            {
                string str = reader.ReadToEnd();

                if (str.Contains($"Commander{ID} from Army"))
                {
                    hasFallen = true;
                    Program.Logger.Debug($"Commander {ID} has fallen!");

                    reader.Close();
                    logFile.Close();
                    return;
                }

                hasFallen = false;
            }
        }
        

        public (bool isSlain, bool isCaptured, string newTraits) Health(string traits_line, bool wasOnLosingSide)
        {
            if (hasFallen)
            {
                // Get percentages from options
                int slainChance = client.ModOptions.GetCommanderSlainChance();
                int woundedChance = client.ModOptions.GetCommanderWoundedChance();
                int severelyInjuredChance = client.ModOptions.GetCommanderSeverelyInjuredChance();
                int brutallyMauledChance = client.ModOptions.GetCommanderBrutallyMauledChance();
                int maimedChance = client.ModOptions.GetCommanderMaimedChance();
                int oneLeggedChance = client.ModOptions.GetCommanderOneLeggedChance();
                int oneEyedChance = client.ModOptions.GetCommanderOneEyedChance();
                int disfiguredChance = client.ModOptions.GetCommanderDisfiguredChance();
                int prisonerChance = client.ModOptions.GetCommanderPrisonerChance();

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
                    Program.Logger.Debug($"Commander {ID} has been slain in battle (chance: {slainChance}%).");
                    return (true, false, traits_line);
                }

                string newTraits = traits_line;
                if (RandomNumber <= woundedThreshold)
                {
                    Program.Logger.Debug($"Commander {ID} got Wounded");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Wounded().ToString());
                }
                else if (RandomNumber <= severelyInjuredThreshold)
                {
                    Program.Logger.Debug($"Commander {ID} got Severely Injured");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Severely_Injured().ToString());
                }
                else if (RandomNumber <= brutallyMauledThreshold)
                {
                    Program.Logger.Debug($"Commander {ID} got Brutally Mauled");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Brutally_Mauled().ToString());
                }
                else if (RandomNumber <= maimedThreshold)
                {
                    Program.Logger.Debug($"Commander {ID} got Maimed");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Maimed().ToString());
                }
                else if (RandomNumber <= oneLeggedThreshold)
                {
                    Program.Logger.Debug($"Commander {ID} got One Legged");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.One_Legged().ToString());
                }
                else if (RandomNumber <= oneEyedThreshold)
                {
                    Program.Logger.Debug($"Commander {ID} got One Eyed");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.One_Eyed().ToString());
                }
                else if (RandomNumber <= disfiguredThreshold)
                {
                    Program.Logger.Debug($"Commander {ID} got Disfigured");
                    newTraits = CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Disfigured().ToString());
                }

                // If survived, roll for capture only if on the losing side
                bool isCaptured = false;
                if (wasOnLosingSide)
                {
                    var prisonerRng = CharacterSharedRandom.Rng.Next(1, 101);
                    isCaptured = prisonerRng <= prisonerChance;
                    if (isCaptured)
                    {
                        Program.Logger.Debug($"Commander {ID} has been captured (chance: {prisonerChance}%).");
                    }
                }

                return (false, isCaptured, newTraits);
            }

            return (false, false, traits_line);
        }
    }
}
