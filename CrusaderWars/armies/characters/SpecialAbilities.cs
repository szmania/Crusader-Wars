using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CrusaderWars.armies
{
    public static class AccoladesAbilities
    {
        public static (string primaryKey, string secundaryKey) ReturnAbilitiesKeys(Accolade Accolade)
        {
            int Rank = GetRank(Accolade.GetGlory());

            string PrimaryAttribute_Name = GetAttribute(Accolade.GetPrimaryAttribute());
            string SecundaryAttribute_Name = GetAttribute(Accolade.GetSecundaryAttribute());

            int PrimaryAttribute_Level = DetermineAbilityLevel(Rank, true);
            int SecondaryAttribute_Level = DetermineAbilityLevel(Rank, false);

            string PrimaryAbilityKey = GetPrimaryAttributeSpecialAbilityKey(PrimaryAttribute_Name, PrimaryAttribute_Level);
            string SecundaryAbilityKey = GetSecundaryAttributeSpecialAbilityKey(SecundaryAttribute_Name, SecondaryAttribute_Level);
            
            var specialAbilitiesKeys = (PrimaryAbilityKey, SecundaryAbilityKey);

            return specialAbilitiesKeys;
        }

        private static string GetSecundaryAttributeSpecialAbilityKey(string SecundaryAttribute_Name, int SecondaryAttribute_Level)
        {
            switch (SecundaryAttribute_Name)
            {
                case "Contender":

                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Contender_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Contender_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Contender_Rank3();
                    }
                    break;
                case "Manipulator":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Manipulator_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Manipulator_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Manipulator_Rank3();
                    }
                    break;
                case "Mentor":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Mentor_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Mentor_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Mentor_Rank3();
                    }
                    break;
                case "Politicker":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Politicker_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Politicker_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Politicker_Rank3();
                    }
                    break;
                case "Reeve":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Reeve_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Reeve_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Reeve_Rank3();
                    }
                    break;
                case "Tactician":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Tactician_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Tactician_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Tactician_Rank3();
                    }
                    break;
                case "Charmer":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Charmer_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Charmer_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Charmer_Rank3();
                    }
                    break;
                case "Disciplinarian":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Disciplinarian_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Disciplinarian_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Disciplinarian_Rank3();
                    }
                    break;
                case "Idealist":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Idealist_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Idealist_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Idealist_Rank3();
                    }
                    break;
                case "Marauder":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Marauder_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Marauder_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Marauder_Rank3();
                    }
                    break;
                case "Scoundrel":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Scoundrel_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Scoundrel_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Scoundrel_Rank3();
                    }
                    break;
                case "Stalwart":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Stalwart_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Stalwart_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Stalwart_Rank3();
                    }
                    break;
                case "Thug":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Thug_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Thug_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Thug_Rank3();
                    }
                    break;
                case "Valiant":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Valiant_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Valiant_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Valiant_Rank3();
                    }
                    break;
                case "Fanatic":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Fanatic_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Fanatic_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Fanatic_Rank3();
                    }
                    break;
                case "Blademaster":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Blademaster_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Blademaster_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Blademaster_Rank3();
                    }
                    break;
                case "Huntsmaster":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Hunstsmaster_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Hunstsmaster_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Hunstsmaster_Rank3();
                    }
                    break;
                case "MasterOfRevels":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.MasterOfRevels_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.MasterOfRevels_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.MasterOfRevels_Rank3();
                    }
                    break;
                case "HouseParagon":
                    if (SecondaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.HouseParagon_Rank1();
                    }
                    else if (SecondaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.HouseParagon_Rank2();
                    }
                    else if (SecondaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.HouseParagon_Rank3();
                    }
                    break;
            }

            return "null";
        }

        private static string GetPrimaryAttributeSpecialAbilityKey(string PrimaryAttribute_Name, int PrimaryAttribute_Level)
        {
            switch (PrimaryAttribute_Name)
            {
                case "Contender":

                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Contender_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Contender_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Contender_Rank3();
                    }
                    break;
                case "Manipulator":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Manipulator_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Manipulator_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Manipulator_Rank3();
                    }
                    break;
                case "Mentor":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Mentor_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Mentor_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Mentor_Rank3();
                    }
                    break;
                case "Politicker":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Politicker_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Politicker_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Politicker_Rank3();
                    }
                    break;
                case "Reeve":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Reeve_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Reeve_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Reeve_Rank3();
                    }
                    break;
                case "Tactician":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Tactician_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Tactician_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Tactician_Rank3();
                    }
                    break;
                case "Charmer":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Charmer_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Charmer_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Charmer_Rank3();
                    }
                    break;
                case "Disciplinarian":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Disciplinarian_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Disciplinarian_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Disciplinarian_Rank3();
                    }
                    break;
                case "Idealist":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Idealist_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Idealist_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Idealist_Rank3();
                    }
                    break;
                case "Marauder":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Marauder_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Marauder_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Marauder_Rank3();
                    }
                    break;
                case "Scoundrel":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Scoundrel_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Scoundrel_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Scoundrel_Rank3();
                    }
                    break;
                case "Stalwart":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Stalwart_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Stalwart_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Stalwart_Rank3();
                    }
                    break;
                case "Thug":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Thug_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Thug_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Thug_Rank3();
                    }
                    break;
                case "Valiant":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Valiant_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Valiant_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Valiant_Rank3();
                    }
                    break;
                case "Fanatic":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Fanatic_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Fanatic_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Fanatic_Rank3();
                    }
                    break;
                case "Blademaster":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Blademaster_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Blademaster_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Blademaster_Rank3();
                    }
                    break;
                case "Huntsmaster":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.Hunstsmaster_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.Hunstsmaster_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.Hunstsmaster_Rank3();
                    }
                    break;
                case "MasterOfRevels":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.MasterOfRevels_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.MasterOfRevels_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.MasterOfRevels_Rank3();
                    }
                    break;
                case "HouseParagon":
                    if (PrimaryAttribute_Level == 1)
                    {
                        return SpecialAbilities_Keys.HouseParagon_Rank1();
                    }
                    else if (PrimaryAttribute_Level == 2)
                    {
                        return SpecialAbilities_Keys.HouseParagon_Rank2();
                    }
                    else if (PrimaryAttribute_Level == 3)
                    {
                        return SpecialAbilities_Keys.HouseParagon_Rank3();
                    }
                    break;
            }

            return "null";
        }

        private static int DetermineAbilityLevel(int Rank, bool isPrimary)
        {
            if(isPrimary)
            {
                if(Rank == 1 || Rank == 2) 
                {
                    return 1;
                }
                else if(Rank == 3 || Rank == 4)
                {
                    return 2;
                }
                else if(Rank == 5 || Rank == 6)
                {
                    return 3;
                }
            }
            else //Secundary Attribute
            {
                if (Rank == 1)
                {
                    return 0;
                }
                else if (Rank == 2 || Rank == 3)
                {
                    return 1;
                }
                else if (Rank == 4 || Rank == 5)
                {
                    return 2;
                }
                else if(Rank == 6)
                {
                    return 3;
                }
            }

            return 0;
        }

        private static int GetRank(int Honor)
        {
            if(Honor >= 100 && Honor < 300)
            {
                return 1;
            }
            else if(Honor >= 300 && Honor < 600)
            {
                return 2;
            }
            else if (Honor >= 600 && Honor < 1000)
            {
                return 3;
            }
            else if (Honor >= 1000 && Honor < 1500)
            {
                return 4;
            }
            else if (Honor >= 1500 && Honor < 2100)
            {
                return 5;
            }
            else if (Honor >= 2100)
            {
                return 6;
            }

            return 0;
        }

        private static string GetAttribute(string attribute)
        {
            switch(attribute)
            {
                case "contender_attribute":
                    return Attributes.Contender();
                case "manipulator_attribute":
                    return Attributes.Manipulator();
                case "mentor_attribute":
                    return Attributes.Mentor();
                case "politicker_attribute":
                    return Attributes.Politicker();
                case "tactician_attribute":
                    return Attributes.Tactician();
                case "charmer_attribute":
                    return Attributes.Charmer();
                case "disciplinarian_attribute":
                    return Attributes.Disciplinarian();
                case "idealist_attribute":
                    return Attributes.Idealist();
                case "marauder_attribute":
                    return Attributes.Marauder();
                case "scoundrel_attribute":
                    return Attributes.Scoundrel();
                case "stalwart_attribute":
                    return Attributes.Stalwart();
                case "thug_attribute":
                    return Attributes.Thug();
                case "valiant_attribute":
                    return Attributes.Valiant();
                case "fanatic_attribute":
                    return Attributes.Fanatic();
                case "blademaster_attribute":
                    return Attributes.Blademaster();
                case "huntsmaster_attribute":
                    return Attributes.Huntsmaster();
                case "master_of_revels_attribute":
                    return Attributes.MasterOfRevels();
                case "house_knight_attribute":
                    return Attributes.HouseParagon();

            }

            return null;
        }
    }

    struct Attributes
    {
        public static string Contender() { return "Contender"; }
        public static string Manipulator() { return "Manipulator"; }
        public static string Mentor() { return "Mentor"; }
        public static string Politicker() { return "Politicker"; }
        public static string Reeve() { return "Reeve"; }
        public static string Tactician() { return "Tactician"; }
        public static string Charmer() { return "Charmer"; }
        public static string Disciplinarian() { return "Disciplinarian"; }
        public static string Idealist() { return "Idealist"; }
        public static string Marauder() { return "Marauder"; }
        public static string Scoundrel() { return "Scoundrel"; }
        public static string Stalwart() { return "Stalwart"; }
        public static string Thug() { return "Thug"; }
        public static string Valiant() { return "Valiant"; }
        public static string Fanatic() { return "Fanatic"; }
        public static string Blademaster() { return "Blademaster"; }
        public static string Huntsmaster() { return "Huntsmaster"; }
        public static string MasterOfRevels() { return "MasterOfRevels"; }
        public static string HouseParagon() { return "HouseParagon"; }

    }

    struct SpecialAbilities_Keys
    {
        //Contender
        public static string Contender_Rank1() { return "att_gen_brace_01"; }
        public static string Contender_Rank2() { return "att_gen_brace_02"; }
        public static string Contender_Rank3() { return "att_gen_brace_03"; }

        //Manipulator
        public static string Manipulator_Rank1() { return "att_gen_brace_04"; }
        public static string Manipulator_Rank2() { return "att_gen_brace_05"; }
        public static string Manipulator_Rank3() { return "com_brace"; }

        //Mentor
        public static string Mentor_Rank1() { return "att_gen_fast_01"; }
        public static string Mentor_Rank2() { return "att_gen_fast_02"; }
        public static string Mentor_Rank3() { return "att_gen_fast_03"; }

        //Politicker
        public static string Politicker_Rank1() { return "att_gen_fast_04"; }
        public static string Politicker_Rank2() { return "att_gen_fast_05"; }
        public static string Politicker_Rank3() { return "com_fast_charge"; }  //<--- this one is broken

        //Reeve
        public static string Reeve_Rank1() { return "att_gen_fear_01"; }
        public static string Reeve_Rank2() { return "att_gen_fear_02"; }
        public static string Reeve_Rank3() { return "att_gen_fear_03"; }

        //Tactician
        public static string Tactician_Rank1() { return "att_gen_fear_04"; }
        public static string Tactician_Rank2() { return "att_gen_fear_05"; }
        public static string Tactician_Rank3() { return "com_the_fear"; }

        //Charmer
        public static string Charmer_Rank1() { return "att_gen_presence_01"; }
        public static string Charmer_Rank2() { return "att_gen_presence_02"; }
        public static string Charmer_Rank3() { return "att_gen_presence_03"; }

        //Disciplinarian
        public static string Disciplinarian_Rank1() { return "att_gen_presence_04"; }
        public static string Disciplinarian_Rank2() { return "att_gen_presence_05"; }
        public static string Disciplinarian_Rank3() { return "com_presence"; }

        //Idealist
        public static string Idealist_Rank1() { return "att_gen_raise_01"; }
        public static string Idealist_Rank2() { return "att_gen_raise_02"; }
        public static string Idealist_Rank3() { return "att_gen_raise_03"; }

        //Marauder
        public static string Marauder_Rank1() { return "att_gen_raise_04"; }
        public static string Marauder_Rank2() { return "att_gen_raise_05"; }
        public static string Marauder_Rank3() { return "com_raise_banner"; }

        //Scoundrel
        public static string Scoundrel_Rank1() { return "att_gen_rally_01"; }
        public static string Scoundrel_Rank2() { return "att_gen_rally_02"; }
        public static string Scoundrel_Rank3() { return "att_gen_rally_03"; }

        //Stalwart
        public static string Stalwart_Rank1() { return "att_gen_rally_04"; }
        public static string Stalwart_Rank2() { return "att_gen_rally_05"; }
        public static string Stalwart_Rank3() { return "com_rally"; }

        //Thug
        public static string Thug_Rank1() { return "att_gen_recon_01"; }
        public static string Thug_Rank2() { return "att_gen_recon_02"; }
        public static string Thug_Rank3() { return "att_gen_recon_03"; }

        //Valiant
        public static string Valiant_Rank1() { return "att_gen_recon_04"; }
        public static string Valiant_Rank2() { return "att_gen_recon_05"; }
        public static string Valiant_Rank3() { return "com_spirit"; }

        //Fanatic
        public static string Fanatic_Rank1() { return "att_gen_second_01"; }
        public static string Fanatic_Rank2() { return "att_gen_second_02"; }
        public static string Fanatic_Rank3() { return "att_gen_second_03"; }

        //Blademaster
        public static string Blademaster_Rank1() { return "att_gen_second_04"; }
        public static string Blademaster_Rank2() { return "att_gen_second_05"; }
        public static string Blademaster_Rank3() { return "com_second_wind"; }

        //Huntmaster
        public static string Hunstsmaster_Rank1() { return "att_gen_war_01"; }
        public static string Hunstsmaster_Rank2() { return "att_gen_war_02"; }
        public static string Hunstsmaster_Rank3() { return "att_gen_war_03"; }

        //Master Of Revels
        public static string MasterOfRevels_Rank1() { return "att_gen_war_04"; }
        public static string MasterOfRevels_Rank2() { return "att_gen_war_05"; }
        public static string MasterOfRevels_Rank3() { return "com_war_cry"; }

        //House Paragon
        public static string HouseParagon_Rank1() { return "a_mp_test_1"; }
        public static string HouseParagon_Rank2() { return "a_mp_test_2"; }
        public static string HouseParagon_Rank3() { return "a_mp_test_4"; }
    }
}
