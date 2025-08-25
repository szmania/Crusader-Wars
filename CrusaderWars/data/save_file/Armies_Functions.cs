using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using CrusaderWars.client;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.data.save_file
{
    internal static class Armies_Functions
    {
        /*##############################################
         *####               COUNTIES               #### 
         *####--------------------------------------####
         *####   Reader for the counties manager    ####
         *##############################################
         */

        public static bool SearchCounty(string county_key, List<Army> armies)
        {
            foreach(Army army in armies)
            {
                foreach(ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment.Regiments == null)
                        continue;

                    foreach(Regiment regiment in armyRegiment.Regiments)
                    {
                        //if county key is empty, skip
                        if (string.IsNullOrEmpty(regiment.GetCountyKey()))
                        {
                            continue;
                        }
                        else
                        {
                            string regiment_county_key = regiment.GetCountyKey();
                            if (regiment_county_key == county_key)
                            {
                                Program.Logger.Debug($"Found county key '{county_key}' in regiment '{regiment.ID}'.");
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static void PopulateRegimentsWithCultures(List<(string county_key, string culture_id)> foundCultures, List<Army> armies)
        {
            Program.Logger.Debug("Populating regiments with cultures...");
            var temp_characters_cultures = new List<(string char_id, string culture_id)>();

            foreach (Army army in armies)
            {
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment.Regiments == null)
                        continue;

                    foreach (Regiment regiment in armyRegiment.Regiments)
                    {
                        string owner_id = regiment.Owner;
                        if (!string.IsNullOrEmpty(owner_id))
                        {
                            if (temp_characters_cultures.Exists(t => t.char_id == owner_id))
                            {
                                string culture_id = temp_characters_cultures.FirstOrDefault(p => p.char_id == owner_id).culture_id;
                                regiment.SetCulture(culture_id);
                            }
                            else
                            {
                                string culture_id = GetCharacterCultureID(owner_id);
                                temp_characters_cultures.Add((owner_id, culture_id));
                                regiment.SetCulture(culture_id);
                            }

                        }
                        else if (foundCultures.Exists(culture => culture.county_key == regiment.GetCountyKey()))
                        {
                            var foundCulture = foundCultures.Find(culture => culture.county_key == regiment.GetCountyKey());
                            regiment.SetCulture(foundCulture.culture_id);
                        }
                    }
                }
            }
            Program.Logger.Debug("Finished populating regiments with cultures.");
        }

        static string GetCharacterCultureID(string character_id)
        {
            Program.Logger.Debug($"Getting culture ID for character '{character_id}'...");
            bool isSearchStarted = false;
            string culture_id = "";
            using (StreamReader stringReader = new StreamReader(Writter.DataFilesPaths.Living_Path()))
            {
                while (true)
                {
                    string line = stringReader.ReadLine();
                    if (line == null) break;

                    if (line == $"{character_id}={{" && !isSearchStarted)
                    {
                        isSearchStarted = true;
                    }

                    if (isSearchStarted && Regex.IsMatch(line, @"\tculture=\d+"))
                    {
                        culture_id = Regex.Match(line, @"\tculture=(\d+)").Groups[1].Value;
                        Program.Logger.Debug($"Found culture ID '{culture_id}' for character '{character_id}'.");
                        return culture_id;
                    }

                    if (isSearchStarted && line == "}")
                    {
                        Program.Logger.Debug($"Could not find culture ID for character '{character_id}'.");
                        return "";
                    }
                }
                Program.Logger.Debug($"Could not find culture ID for character '{character_id}' (end of file).");
                return culture_id;
            }

        }

        /*##############################################
         *####               CULTURES               #### 
         *####--------------------------------------####
         *####   Reader for the culture manager     ####
         *##############################################
         */

        public static void ReadArmiesCultures(List<Army> armies)
        {
            Program.Logger.Debug("Reading cultures for all armies...");
            bool isSearchStared = false;
            string culture_id = "";
            string culture_name = "";
            string heritage_name = "";
            var found_cultures = new List<(string culture_id, string culture_name, string heritage_name)>();

            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Cultures_Path()))
            {
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null) break;

                    //Culture Line
                    if (Regex.IsMatch(line, @"^\t\t\d+={$") && !isSearchStared)
                    {
                        culture_id = Regex.Match(line, @"(\d+)").Groups[1].Value;

                        //Armies
                        foreach (Army army in armies)
                        {
                            //Owner
                            if (army.Owner.GetCulture() != null && army.Owner.GetCulture() != null && army.Owner.GetCulture().ID == culture_id)
                            {
                                isSearchStared = true;
                                break;
                            }
                            //Commanders
                            else if (army.Commander != null && army.Commander.GetCultureObj() != null && army.Commander.GetCultureObj().ID == culture_id)
                            {
                                isSearchStared = true;
                                break;
                            }
                            //Knights
                            else if (army.Knights != null && army.Knights.GetKnightsList() != null)
                            {
                                foreach (var knight in army.Knights.GetKnightsList())
                                {
                                    if (knight.GetCultureObj() != null && knight.GetCultureObj().ID == culture_id)
                                    {
                                        isSearchStared = true;
                                        break;
                                    }
                                }

                                if (isSearchStared)
                                    break;
                            }
                            //Army Regiments
                            foreach(ArmyRegiment armyRegiments in army.ArmyRegiments)
                            {
                                if (armyRegiments.Regiments == null) 
                                    continue;
                                //Regiments
                                foreach(Regiment regiment in armyRegiments.Regiments)
                                {
                                    if(regiment.Culture == null)
                                    {
                                        continue;
                                    }
                                    else if(string.IsNullOrEmpty(regiment.Culture.ID))
                                    {
                                        continue;
                                    }
                                    else if(regiment.Culture.ID == culture_id)
                                    {
                                        isSearchStared = true;
                                        break;
                                    }
                                }
                                if (isSearchStared)
                                    break;
                            }
                            if (isSearchStared)
                                break;
                        }
                    }


                    //Culture Name
                    if (isSearchStared && line.Contains("\t\t\tname="))
                    {
                        culture_name = Regex.Match(line, @"""(.+)""").Groups[1].Value;
                        culture_name = culture_name.Replace("-", "");
                        culture_name = RemoveDiacritics(culture_name);
                        culture_name = culture_name.Replace(" ", "");

                    }
                    //Heritage Name
                    else if (isSearchStared && line.Contains("\t\t\theritage="))
                    {
                        heritage_name = Regex.Match(line, @"heritage=(.+)\t\t\tlanguage=").Groups[1].Value;
                        heritage_name = heritage_name.Trim('-');

                        //End Line
                        Program.Logger.Debug($"Found culture data: ID={culture_id}, Name={culture_name}, Heritage={heritage_name}");
                        found_cultures.Add((culture_id, culture_name, heritage_name));
                        isSearchStared = false;
                        culture_id = ""; culture_name = ""; heritage_name = "";
                    }

                    //End Line
                    if (isSearchStared && line == "\t\t}")
                    {

                    }
                }
            }

            // This is only if there are still null cultures
            Program.Logger.Debug("Checking for any remaining null cultures after initial pass...");
            foreach (Army army in armies)
            {
                //  COMMANDERS
                if (army.Commander != null && army.Commander.GetCultureObj() == null)
                {
                    Program.Logger.Debug($"Commander for army {army.ID} has null culture. Assigning from log data or owner.");
                    if (army.IsPlayer())
                    {
                        army.Commander.ChangeCulture(new Culture(CK3LogData.LeftSide.GetCommander().culture_id));
                    }
                    else if (army.IsEnemy())
                    {
                        army.Commander.ChangeCulture(new Culture(CK3LogData.RightSide.GetCommander().culture_id));
                    }
                    else
                    {
                        army.Commander.ChangeCulture(army.Owner.GetCulture());
                    }
                }

                // KNIGHTS
                if (army.Knights != null && army.Knights.GetKnightsList() != null)
                {
                    foreach (Knight knight in army.Knights.GetKnightsList())
                    {
                        if (knight.GetCultureObj() == null)
                        {
                            Culture mainParticipantCulture = null;
                            if (army.IsPlayer())
                            {
                                string id = CK3LogData.LeftSide.GetMainParticipant().culture_id;
                                Program.Logger.Debug($"Knight {knight.GetID()} in army {army.ID} has null culture. Assigning fallback culture. " +
                                                     $"Assigning fallback culture ID: {id}");
                                mainParticipantCulture = new Culture(id);
                            }
                            else if (army.IsEnemy())
                            {
                                string id = CK3LogData.RightSide.GetMainParticipant().culture_id;
                                Program.Logger.Debug($"Knight {knight.GetID()} in army {army.ID} has null culture. Assigning fallback culture. " +
                                                     $"Assigning fallback culture ID: {id}");
                                mainParticipantCulture = new Culture(id);
                            }

                            Culture new_culture = army.Knights.GetKnightsList()?.Find(x => x.GetCultureObj() != null)?.GetCultureObj() ?? mainParticipantCulture;
                            
                            string newCultureID = new_culture?.ID ?? "no_culture_found";
                            Program.Logger.Debug($"Knight {knight.GetID()} in army {army.ID} has null culture. Assigning fallback culture. " +
                                $"Assigning fallback culture ID: {newCultureID}");
                            
                            knight.ChangeCulture(new_culture);
                            army.Knights.SetMajorCulture();
                        }
                    }
                }

            }
            Program.Logger.Debug("Finished reading cultures for all armies.");
            SetCulturesToAll(armies, found_cultures);
        }

        static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }

        internal static void SetCulturesToAll(List<Army> armies, List<(string culture_id, string culture_name, string heritage_name)> foundCultures)
        {
            Program.Logger.Debug("Setting culture names and heritages for all relevant objects...");
            foreach (Army army in armies)
            {

                //Owner
                if (army.Owner.GetCulture() != null && foundCultures.Exists(culture => culture.culture_id == army.Owner.GetCulture().ID))
                {
                    var foundCulture = foundCultures.Find(culture => culture.culture_id == army.Owner.GetCulture().ID);
                    army.Owner.GetCulture().SetName(foundCulture.culture_name);
                    army.Owner.GetCulture().SetHeritage(foundCulture.heritage_name);
                }

                //Commanders
                if (army.Commander != null)
                {
                    if (foundCultures.Exists(culture => culture.culture_id == army.Commander.GetCultureObj().ID))
                    {
                        var foundCulture = foundCultures.Find(culture => culture.culture_id == army.Commander.GetCultureObj().ID);
                        army.Commander.GetCultureObj().SetName(foundCulture.culture_name);
                        army.Commander.GetCultureObj().SetHeritage(foundCulture.heritage_name);
                    }
                }

                //Knights
                if (army.Knights.GetKnightsList() != null)
                {
                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        string knight_culture_id = knight.GetCultureObj().ID;
                        if (foundCultures.Exists(culture => culture.culture_id == knight_culture_id))
                        {
                            var foundCulture = foundCultures.Find(culture => culture.culture_id == knight_culture_id);
                            knight.GetCultureObj().SetName(foundCulture.culture_name);
                            knight.GetCultureObj().SetHeritage(foundCulture.heritage_name);
                        }
                    }
                }


                //Army Regiments
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment.Regiments == null) continue;
                    foreach (Regiment regiment in armyRegiment.Regiments)
                    {
                        if (regiment.Culture == null)
                        {
                            Program.Logger.Debug($"WARNING - NULL CULTURE IN REGIMENT {regiment.ID}");
                            continue;
                        }
                        string regimentCultureID = regiment.Culture.ID;
                        if (foundCultures.Exists(culture => culture.culture_id == regimentCultureID))
                        {
                            var foundCulture = foundCultures.Find(culture => culture.culture_id == regimentCultureID);
                            regiment.Culture.SetName(foundCulture.culture_name);
                            regiment.Culture.SetHeritage(foundCulture.heritage_name);
                        }

                    }
                }
            }
            Program.Logger.Debug("Finished setting culture names and heritages.");
        }

        /*##############################################
         *####                  Unit                #### 
         *####--------------------------------------####
         *####         Searcher for unit file       ####
         *##############################################
         */

        public static (bool searchHasStarted, Army army) SearchUnit(string unitID, List<Army> armies)
        {
            foreach (Army army in armies)
            {
                if(unitID == army.ArmyUnitID)
                {
                    Program.Logger.Debug($"Found army unit ID '{unitID}' in army '{army.ID}'.");
                    return (true, army);
                }
            }
            return (false, null);
        }

        /*##############################################
         *####             Army Regiments           #### 
         *####--------------------------------------####
         *####    Searcher for army regiments file  ####
         *##############################################
         */

        public static (bool searchHasStarted, ArmyRegiment regiment) SearchArmyRegiments(string armyRegimentId, List<Army> armies)
        {
            foreach (Army army in armies)
            {
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if(armyRegimentId == armyRegiment.ID)
                    {
                        Program.Logger.Debug($"Found army regiment ID '{armyRegimentId}' in army '{army.ID}'.");
                        return (true, armyRegiment);
                    }
                }
            }
            return (false, null);
        }

        /*##############################################
         *####               Regiments              #### 
         *####--------------------------------------####
         *####      Searcher for regiments file     ####
         *##############################################
         */

        public static (bool searchHasStarted, Regiment regiment) SearchRegiments(string regiment_id, List<Army> armies)
        {
            foreach(Army army in armies)
            {
                foreach(ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if(armyRegiment.Regiments != null)
                    {
                        foreach(Regiment regiment in armyRegiment.Regiments)
                        {
                            if(regiment.ID == regiment_id)
                            {
                                Program.Logger.Debug($"Found regiment ID '{regiment_id}' in army '{army.ID}'.");
                                return (true, regiment);
                            }
                        }
                    }
                }
            }
            return (false, null);
        }

        /*##############################################
         *####              CHARACTERS              #### 
         *####--------------------------------------####
         *####      Reader for the living file      ####
         *##############################################
         */

        public static (bool searchStarted, Army searchingArmy, bool isCommander, bool isMainCommander, bool isKnight, Knight knight, bool isOwner) SearchCharacters(string id, List<Army> armies)
        {
            foreach (Army army in armies)
            {
                //Main Commanders
                if(army.Commander != null && id == army.Commander.ID && id == army.Owner.GetID())
                {
                    Program.Logger.Debug($"Found character '{id}': Main Commander and Owner of army '{army.ID}'.");
                    return (true, army, false, true, false, null, true);
                }
                else if (army.Commander != null && id == army.Commander.ID)
                {
                    Program.Logger.Debug($"Found character '{id}': Main Commander of army '{army.ID}'.");
                    return (true, army, false, true,false, null, false);
                }

                //Commanders
                if(id == army.CommanderID && id == army.Owner.GetID())
                {
                    Program.Logger.Debug($"Found character '{id}': Commander and Owner of army '{army.ID}'.");
                    return (true, army, true, false, false, null, true);
                }
                else if (id == army.CommanderID)
                {
                    Program.Logger.Debug($"Found character '{id}': Commander of army '{army.ID}'.");
                    return (true, army, true, false, false, null, false);
                }

                // KNIGHTS
                else if (army.Knights.GetKnightsList() != null)
                {
                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        if (id == knight.GetID() && id == army.Owner.GetID())
                        {
                            Program.Logger.Debug($"Found character '{id}': Knight and Owner of army '{army.ID}'.");
                            return (true, army, false, false,true, knight, true);
                        }
                        else if(id == knight.GetID())
                        {
                            Program.Logger.Debug($"Found character '{id}': Knight in army '{army.ID}'.");
                            return (true, army, false, false, true, knight, false);
                        }
                    }
                }
                //ARMY OWNER
                else if (id == army.Owner.GetID())
                {
                    Program.Logger.Debug($"Found character '{id}': Owner of army '{army.ID}'.");
                    return (true, army, false, false,false, null, true);
                }
            }

            return (false, null, false, false,false, null, false);
        }

        /*##############################################
         *####                UNITS                 #### 
         *####--------------------------------------####
         *####   Conversion of regiments to units   ####
         *##############################################
         */

        internal static List<Unit> GetAllUnits_UnitKeys(List<Unit> units)
        {
            Program.Logger.Debug("Getting Attila unit keys for all units...");
            //Set Unit Keys
            foreach (var unit in units)
            {
                if (unit.GetRegimentType() == RegimentType.Levy) continue;

                string key = UnitMappers_BETA.GetUnitKey(unit);
                if (key == "not_found")
                {
                    Program.Logger.Debug($"Unit key not found for '{unit.GetName()}' ({unit.GetCulture()}). Using default 'cha_spa_royal_cav'.");
                    unit.SetUnitKey("cha_spa_royal_cav");
                }
                else
                    unit.SetUnitKey(key);
            }
            Program.Logger.Debug("Finished getting unit keys.");
            return units;
        }

        internal static List<Unit> GetAllUnits_AttilaFaction(List<Unit> units)
        {
            Program.Logger.Debug("Getting Attila factions for all units...");
            //Get Unit Mapper Faction
            foreach (var unit in units)
            {
                unit.SetAttilaFaction(UnitMappers_BETA.GetAttilaFaction(unit.GetCulture(), unit.GetHeritage()));
            }
            Program.Logger.Debug("Finished getting Attila factions.");
            return units;
        }
        internal static List<Unit> GetAllUnits_Max(List<Unit> units)
        {
            Program.Logger.Debug("Getting max unit sizes for all units...");
            //Read Unit Limit
            foreach (var unit in units)
            {
                unit.SetMax(UnitMappers_BETA.GetMax(unit));
            }
            Program.Logger.Debug("Finished getting max unit sizes.");
            return units;
        }

        // Function to get the top N units by soldiers count
        internal static List<Unit> GetTopUnits(List<Unit> units, int topN)
        {
            Program.Logger.Debug($"Getting top {topN} units by soldier count.");
            if (topN <= 0)
            {
                throw new ArgumentException("topN must be greater than 0");
            }

            // Sort units by soldiers count in descending order
            var sortedUnits = units.OrderByDescending(u => u.GetSoldiers()).ToList();

            // Take the top N units
            return sortedUnits.Take(topN).ToList();
        }

        internal static void CreateUnits(List<Army> armies)
        {
            Program.Logger.Debug("Creating units from regiments for all armies...");
            foreach (var army in armies)
            {
                Program.Logger.Debug($"Creating units from {armies.Count} total armies, for army {army.ID}");
                List<(Regiment regiment, RegimentType type, string maa_name)> list = new List<(Regiment regiment, RegimentType type, string maa_name)>();
                foreach (var army_regiment in army.ArmyRegiments)
                {
                    foreach (var regiment in army_regiment.Regiments)
                    {
                        list.Add((regiment, army_regiment.Type, army_regiment.MAA_Name));
                    }
                }

                List<Unit> units = new List<Unit>();
                foreach (var regiment in list)
                {
                    // if no soldiers, skip
                    if (ModOptions.FullArmies(regiment.regiment) is null) continue;

                    if (Int32.Parse(ModOptions.FullArmies(regiment.regiment)) == 0) continue;

                    Unit unit;
                    if (regiment.type == RegimentType.Levy)
                    {
                        Culture levyCulture = regiment.regiment.Culture ?? army.Owner.GetCulture();
                        if (regiment.regiment.isMercenary())
                            unit = new Unit("Levy", Int32.Parse(ModOptions.FullArmies(regiment.regiment)), levyCulture, regiment.type, true);
                        else
                            unit = new Unit("Levy", Int32.Parse(ModOptions.FullArmies(regiment.regiment)), levyCulture, regiment.type);
                    }
                    else if (regiment.type == RegimentType.MenAtArms)
                        if (regiment.regiment.isMercenary())
                            unit = new Unit(regiment.maa_name, Int32.Parse(ModOptions.FullArmies(regiment.regiment)), regiment.regiment.Culture, regiment.type, true);
                        else
                            unit = new Unit(regiment.maa_name, Int32.Parse(ModOptions.FullArmies(regiment.regiment)), regiment.regiment.Culture, regiment.type);
                    else
                        continue;

                    if (unit != null)
                        units.Add(unit);


                }

                units = OrganizeUnitsIntoCultures(units, army.Owner);
                units = OrganizeLeviesUnits(units);
                units = GetAllUnits_AttilaFaction(units);
                units = GetAllUnits_Max(units);
                units = GetAllUnits_UnitKeys(units);
                army.SetUnits(units);
                Program.Logger.Debug($"Finished creating {units.Count} units for army {army.ID}.");
                //army.PrintUnits();
            }
            Program.Logger.Debug("Finished creating units for all armies.");
        }

        static List<Unit> OrganizeUnitsIntoCultures(List<Unit> units, Owner owner)
        {
            Program.Logger.Debug($"Organizing {units.Count} units by culture for owner {owner.GetID()}");
            foreach (var unit in units)
            {
                Program.Logger.Debug($"- Unit: {unit.GetName()}, Soldiers: {unit.GetSoldiers()}, " +
                    $"Culture: {unit.GetObjCulture()?.GetCultureName() ?? "null"}, " +
                    $"Heritage: {unit.GetObjCulture()?.GetHeritageName() ?? "null"}");
            }

            var organizedUnits = new List<Unit>();

            // Group units by Name and Culture
            var groupedUnits = units.GroupBy(u => new { Name = u.GetName(), Culture = u.GetCulture(), Type = u.GetRegimentType(), IsMerc = u.IsMerc() });

            // Merge units with the same Name and Culture
            foreach (var group in groupedUnits)
            {
                int totalSoldiers = group.Sum(u => u.GetSoldiers());

                // Create a new Unit with the merged NumberOfSoldiers
                Unit mergedUnit = new Unit(group.Key.Name, totalSoldiers, group.ElementAt(0).GetObjCulture(), group.ElementAt(0).GetRegimentType(), group.ElementAt(0).IsMerc(), owner);
                
                organizedUnits.Add(mergedUnit);
            }
            Program.Logger.Debug($"Organized {units.Count} units into {organizedUnits.Count} merged units.");
            return organizedUnits;
        }

        static List<Unit> OrganizeLeviesUnits(List<Unit> units)
        {
            var unitsBelowThreshold = units.Where(u => u.GetSoldiers() <= ModOptions.CulturalPreciseness() && u.GetName() == "Levy").ToList();
            Program.Logger.Debug($"Organizing {unitsBelowThreshold.Count} levy units below threshold {ModOptions.CulturalPreciseness()}");
            if (unitsBelowThreshold.Count == 0) return units;

            int total = 0;
            Unit biggest = null;
            int lastRegistered = 0;
            foreach (var u in unitsBelowThreshold)
            {
                total += u.GetSoldiers();

                if (u.GetSoldiers() > lastRegistered)
                {
                    lastRegistered = u.GetSoldiers();
                    biggest = u;
                }

            }

            var unit = new Unit("Levy", total, biggest.GetObjCulture(), RegimentType.Levy);
            var unit_data = UnitsFile.RetriveCalculatedUnits(unit.GetSoldiers(), ModOptions.GetLevyMax());
            var levies_top_cultures = GetTopUnits(unitsBelowThreshold, unit_data.UnitNum);

            int null_soldiers = 0;

            var null_cultures_levies = units.Where(x => x.GetObjCulture() == null).ToList();
            Console.WriteLine($"Found {null_cultures_levies.Count} null-culture levies");
            if (null_cultures_levies.Count > 0)
            {
                foreach (var null_levie in null_cultures_levies)
                {
                    null_soldiers += null_levie.GetSoldiers();
                    units.Remove(null_levie);
                }
            }


            int limit = ModOptions.CulturalPreciseness();
            units.RemoveAll(x => x.GetName() == "Levy" && x.GetSoldiers() <= limit);
            for (int i = 0; i < unit_data.UnitNum; i++)
            {
                if (i == 0)
                    units.Add(new Unit("Levy", unit_data.UnitSoldiers + null_soldiers, levies_top_cultures[i].GetObjCulture(), RegimentType.Levy));
                else
                    units.Add(new Unit("Levy", unit_data.UnitSoldiers, levies_top_cultures[i].GetObjCulture(), RegimentType.Levy));
            }

            return units;
        }
    }
}
