using CrusaderWars.armies;
using CrusaderWars.terrain;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CrusaderWars.data.save_file;


namespace CrusaderWars
{

    struct MartialSkill
    {
        public static int Terrible() { return 0; }
        public static int Poor() { return 0; }
        public static int Average() { return 1; }
        public static int Good() { return 2; }
        public static int Excellent() { return 3; }
    }


    struct Strength
    {
        public static double Terrible() { return 0.0; }
        public static double Poor() { return 0.1; }
        public static double Average() { return 0.2; }
        public static double Good() { return 0.3; }
        public static double Excellent() { return 0.4; }
    }

    internal struct WoundedTraits
    {
        public static int Wounded() { return ArmiesReader.GetTraitIndex("wounded_1"); }
        public static int Severely_Injured() { return ArmiesReader.GetTraitIndex("wounded_2"); }
        public static int Brutally_Mauled() { return ArmiesReader.GetTraitIndex("wounded_3"); }
        public static int Maimed() { return ArmiesReader.GetTraitIndex("maimed"); }
        public static int One_Legged() { return ArmiesReader.GetTraitIndex("one_legged"); }
        public static int One_Eyed() { return ArmiesReader.GetTraitIndex("one_eyed"); }
        public static int Disfigured() { return ArmiesReader.GetTraitIndex("disfigured"); }
    }


    internal static  class CharacterWounds
    {
        static string[] ids = { WoundedTraits.Wounded().ToString(), WoundedTraits.Severely_Injured().ToString(), WoundedTraits.Brutally_Mauled().ToString(),
                             WoundedTraits.Maimed().ToString(), WoundedTraits.One_Eyed().ToString(),
                             WoundedTraits.One_Legged().ToString(), WoundedTraits.Disfigured().ToString() };
        internal static string VerifyTraits(string str, string trait)
        {
            str = Regex.Replace(str, " }", "");

            //check if it is already wounded
            string already_wounded_trait="";
            if (str.Contains($" {ids[0]} ") || str.Contains($" {ids[0]}"))
            {
                already_wounded_trait = ids[0];
            }
            else if (str.Contains($" {ids[1]} ") || str.Contains($" {ids[1]}"))
            {
                already_wounded_trait = ids[1];
            }
            else if (str.Contains($" {ids[2]} ") || str.Contains($" {ids[2]}"))
            {
                already_wounded_trait = ids[2];
            }
            

            //if its not wounded, give wounded trait
            if (already_wounded_trait == string.Empty)
            {
                //Add trait if doesn't have one
                str += $" {trait}";

                return str + " }";
            }
            else //if its already wounded, increase wound
            {
                var match_1 = Regex.Match(str, $" {already_wounded_trait} ");
                if(match_1.Success)
                {
                    string edited_str = Regex.Replace(str, $" {already_wounded_trait} ", $" {WoundedTraits.Brutally_Mauled()} ");
                    return edited_str + " }";
                }


                var match_2 = Regex.Match(str, $" {already_wounded_trait}");
                if (match_2.Success)
                {
                    string edited_str = Regex.Replace(str, $" {already_wounded_trait}", $" {WoundedTraits.Brutally_Mauled()} ");
                    return edited_str + " }";
                }

                return str + " }";

            }
        }
    }





    //this are the base values from a character skills
    //modifiers do not count here
    public class BaseSkills
    {
        public int diplomacy { get; private set; }
        public int martial { get; private set; }
        public int stewardship { get; private set; }
        public int intrige { get; private set; }
        public int learning { get; private set; }
        public int prowess { get; private set; }

        public BaseSkills(List<string> skills_collection)
        {

            this.diplomacy = Int32.Parse(skills_collection[0]);
            this.martial = Int32.Parse(skills_collection[1]);
            this.stewardship = Int32.Parse(skills_collection[2]);
            this.intrige = Int32.Parse(skills_collection[3]);
            this.learning = Int32.Parse(skills_collection[4]);
            this.prowess = Int32.Parse(skills_collection[5]);
        }
    }


}
