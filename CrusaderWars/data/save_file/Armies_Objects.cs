using CrusaderWars.armies.commander_traits;
using CrusaderWars.armies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrusaderWars.unit_mapper;
using System.Xml;
using CrusaderWars.client;

namespace CrusaderWars.data.save_file
{
    public static class Print
    {
        public static void PrintArmiesData(List<Army> armies)
        {
            int armyRegimentsTotal = 0;
            int regimentsTotal = 0;
            foreach (var i in armies)
            {
                Program.Logger.Debug($"#Army - {i.ID} | {i.CombatSide} | Commander {i.CommanderID}");
                Program.Logger.Debug("------------------------------------------------------------");
                foreach (var x in i.ArmyRegiments)
                {
                    if (x.Type == RegimentType.Knight)
                    {
                        Program.Logger.Debug($"##Army Regiment - {x.ID} |{x.Type} | Character ID {x.MAA_Name}");
                    }
                    else
                    {
                        Program.Logger.Debug($"##Army Regiment - {x.ID} |{x.Type} | {x.MAA_Name}");
                        armyRegimentsTotal += x.CurrentNum;
                    }
                        
                    foreach (var t in x.Regiments)
                    {
                        if (!t.isMercenary())
                        {
                            if (t.Culture is null)
                            {
                                Program.Logger.Debug($"## ## Chunk Regiment: {t.ID} | Owner: {t.Owner} |Index: {t.Index} | Origin: {t.Origin} | Soldiers: {ModOptions.FullArmies(t)} | County Key: {t.GetCountyKey()} | Culture ID: null");
                                regimentsTotal += int.TryParse(t.CurrentNum, out int currentNum) ? currentNum : 0;
                            }
                            else
                            {
                                Program.Logger.Debug($"## ## Chunk Regiment: {t.ID} | Owner: {t.Owner} | Index: {t.Index} | Origin: {t.Origin} | Soldiers: {ModOptions.FullArmies(t)} | County Key: {t.GetCountyKey()} | Culture: {t.Culture.GetCultureName()} | Heritage: {t.Culture.GetHeritageName()}");
                                regimentsTotal += int.TryParse(t.CurrentNum, out int currentNum) ? currentNum : 0;
                            }
                        }
                        else
                        {
                            ///Program.Logger.Debug($"## ## Mercenary Chunk Regiment: {t.ID} | Owner: {t.Owner} | Index: {t.Index} | Origin: {t.Origin} | Soldiers: {t.CurrentNum} | County Key: {t.GetCountyKey()} | Culture ID: null");
                            Program.Logger.Debug($"## ## Mercenary Chunk Regiment: {t.ID} | Owner: {t.Owner} | Index: {t.Index} | Origin: {t.Origin} | Soldiers: {ModOptions.FullArmies(t)} | County Key: {t.GetCountyKey()} | Culture: {t.Culture.GetCultureName()} | Heritage: {t.Culture.GetHeritageName()}");
                            regimentsTotal += int.TryParse(t.CurrentNum, out int currentNum) ? currentNum : 0;
                        }
                            
                    }

                }
                Program.Logger.Debug("\n");
            }
            
            Program.Logger.Debug("\n");
            Program.Logger.Debug($"ARMY REGIMENTS TOTAL FOUND: {armyRegimentsTotal}\n" +
                              $"REGIMENTS TOTAL FOUND: {regimentsTotal}\n");
            Program.Logger.Debug("\n");
            Program.Logger.Debug("\n");

        }
    }


    public class Unit
    {

        string Name { get; set; }
        RegimentType Type { get; set; }
        Culture UnitCulture { get; set; }
        Owner Owner {  get; set; }
        bool IsMercenaryBool { get; set; }
        int Soldiers { get; set; }
        string AttilaKey { get; set; }
        string AttilaFaction { get; set; }
        int Max { get; set; }
        string LocName { get; set; }



        public Unit(string regiment_name, int soldiers, Culture culture_obj, RegimentType type)
        {
            Name = regiment_name;
            UnitCulture = culture_obj;
            Soldiers = soldiers;
            Type = type;
        }

        public Unit(string regiment_name, int soldiers, Culture culture_obj, RegimentType type, bool is_merc)
        {
            Name = regiment_name;
            UnitCulture = culture_obj;
            Soldiers = soldiers;
            Type = type;
            IsMercenaryBool = is_merc;
        }
        public Unit(string regiment_name, int soldiers, Culture culture_obj, RegimentType type, bool is_merc, Owner owner)
        {
            Name = regiment_name;
            UnitCulture = culture_obj;
            Soldiers = soldiers;
            Type = type;
            IsMercenaryBool = is_merc;
            Owner = owner;
        }




        public void SetAttilaFaction(string a) { AttilaFaction = a; }
        public void SetUnitKey(string unit_key) { AttilaKey = unit_key; }
        public void ChangeName(string y) { Name = y; }
        public void ChangeSoldiers(int y) { Soldiers = y; }
        public void SetMax(int i) { Max = i; }
        public void SetLocName(string t) { LocName = t; }
        public void ChangeCulture(Culture culture) { UnitCulture = culture; }


        public int GetMax() { return Max; }
        public string GetAttilaFaction() { return AttilaFaction; }
        public string GetAttilaUnitKey() { return AttilaKey; }
        public Owner GetOwner() { return Owner; }
        public string GetName() { return Name; }
        public Culture GetObjCulture() { 

            return UnitCulture; 
        }
        public string GetCulture() {
            if (UnitCulture == null)
            { 
                return "not_found";
            }
                
            return UnitCulture.GetCultureName(); 
        }
        public string GetHeritage() { 
            if (UnitCulture == null) 
                return "not_found"; 
            return UnitCulture.GetHeritageName(); 
        }
        public int GetSoldiers() { return Soldiers; }
        public bool IsMerc() { return IsMercenaryBool; }
        public RegimentType GetRegimentType() { return Type; }
        public string GetLocName() { return LocName; }

    }

    public enum RegimentType
    {
        Commander,
        MenAtArms,
        Levy,
        Knight
    };

    public class ArmyRegiment
    {
        public string ID { get; private set; }
        public RegimentType Type { get; private set; }
        public string MAA_Name { get; private set; }
        public List<Regiment> Regiments { get; private set; }
        public int CurrentNum { get; private set; }
        public int StartingNum { get; private set; }
        public int Max { get; private set; }


        public ArmyRegiment(string id)
        {
            ID = id;
        }

        public void SetType(RegimentType type)
        {
            Type = type;
            if (type == RegimentType.Levy) MAA_Name = "Levy";
        }
        public void SetType(RegimentType type, string maa_name)
        {
            MAA_Name = maa_name;
            Type = type;
        }

        public void SetRegiments(List<Regiment> regiments)
        {
            Regiments = regiments;

        }
        public void SetCurrentNum (string x)
        {
            CurrentNum = Int32.Parse(x);
        }
        public void SetMax(string x)
        {
            Max = Int32.Parse(x);
        }
        public void SetStartingNum(string x)
        {
            StartingNum = Int32.Parse(x);
        }
    }

    public class Regiment
    {
        public string ID { get; private set; }
        public string Index { get; private set; }
        public string Origin { get; private set; }
        public string OriginKey { get; private set; }
        public string OwningTitle { get; private set; }
        public string Owner { get; private set; }
        public string Max {  get; private set; }
        public string CurrentNum { get; private set; }
        public Culture Culture { get; private set; }
        string county_key { get; set; }
        bool IsMercenary { get; set; }
        bool isGarrison {  get; set; }

        public Regiment(string id, string index)
        {
            ID = id;
            Index = index;
        }

        //Getters
        public string GetCountyKey() { return county_key; }
        public bool isMercenary() { return IsMercenary; }
        public bool IsGarrison() { return isGarrison; }

        //Setters
        public void SetOwningTitle(string o) { this.OwningTitle = o; }
        public void SetOwner(string o) { this.Owner = o; }
        public void SetCulture(string id) { this.Culture = new Culture(id); }
        public void isMercenary(bool t) { this.IsMercenary = t; }
        public void IsGarrison(bool t) { this.isGarrison = t; }
        public void SetOrigin(string origin) { this.Origin = origin; }
        public void SetOriginKey(string key) { this.OriginKey = key; }
        public void SetMax(string max) { this.Max = max; }
        public void SetSoldiers(string soldiers) {this.CurrentNum = soldiers; }
        public void StoreCountyKey(string key) { this.county_key = key; }
        public void ChangeIndex(string index) { this.Index = index; }

        
    }

    public class Culture
    {
        public string ID { get; private set; }
        string CultureName { get; set; }
        string HeritageName { get; set; }

        public Culture(string id)
        {
            ID = id;
        }
        public Culture(string id, string name, string heritage)
        {
            ID = id;
            CultureName = name;
            HeritageName = heritage;
        }

        public string GetCultureName()
        {
            if (CultureName is null)
                return "";
            else
                return CultureName;
        }
        public string GetHeritageName() { return HeritageName; }


        public void SetName(string t) { CultureName = t; }
        public void SetHeritage(string t) { HeritageName = t; }

    }

    public class Owner
    {
        string ID { get; set; }
        Culture Culture { get; set; }
        string PrimaryTitleKey { get; set; }
        public Owner(string iD)
        {
            ID = iD;
        }

        public Owner(string iD, Culture culture)
        {
            ID = iD;
            Culture = culture;
        }

        public void SetCulture(Culture culture) { Culture = culture; }
        public void SetPrimaryTitle(string t) { PrimaryTitleKey = t; }

        public string GetID() { return ID; }
        public Culture GetCulture() { return Culture; }
        public string GetPrimaryTitleKey() {  return PrimaryTitleKey; }
    }
}
