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
                    string? line = stringReader.ReadLine();
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
            // Add detailed start log
            Program.Logger.Debug("START ReadArmiesCultures: Matching cultures in armies");
            bool isSearchStared = false;
            string culture_id = "";
            string culture_name = "";
            string heritage_name = "";
            var found_cultures = new List<(string culture_id, string culture_name, string heritage_name)>();

            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Cultures_Path()))
            {
                while (true)
                {
                    string? line = sr.ReadLine();
                    if (line == null) break;

                    //Culture Line
                    if (Regex.IsMatch(line, @"^\t\t\d+={$") && !isSearchStared)
                    {
                        culture_id = Regex.Match(line, @"(\d+)").Groups[1].Value;
                        Program.Logger.Debug($"  Found CultureID: {culture_id}");

                        //Armies
                        foreach (Army army in armies)
                        {
                            //Owner
                            if (army.Owner?.GetCulture()?.ID == culture_id)
                            {
                                isSearchStared = true;
                                break;
                            }
                            //Commanders
                            else if (army.Commander != null)
                            {
                                var commanderCulture = army.Commander?.GetCultureObj();
                                if (commanderCulture != null && commanderCulture.ID == culture_id)
                                {
                                    isSearchStared = true;
                                    break;
                                }
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

                        // Program.Logger.Debug("    Starting line-by-line culture parsing...");
                    }

                    // Add log for found culture name
                    if (isSearchStared && line.Contains("\t\t\tname="))
                    {
                        culture_name = Regex.Match(line, @"""(.+)""").Groups[1].Value;
                        culture_name = culture_name.Replace("-", "");
                        culture_name = RemoveDiacritics(culture_name);
                        culture_name = culture_name.Replace(" ", "");
                        Program.Logger.Debug($"  Found CultureName: {culture_name}");
                    }

                    // Add log for found heritage
                    else if (isSearchStared && line.Contains("\t\t\theritage="))
                    {
                        Match heritageMatch = Regex.Match(line, @"heritage=([^\s\t}]+)");
                        if (heritageMatch.Success)
                        {
                            heritage_name = heritageMatch.Groups[1].Value.Trim('"');
                            Program.Logger.Debug($"  Found Heritage: {heritage_name}");
                        }
                    }
                    else if (isSearchStared && line.Contains("\t\t}"))
                    {
                        if (!string.IsNullOrEmpty(culture_id) && !string.IsNullOrEmpty(culture_name) && !string.IsNullOrEmpty(heritage_name))
                        {
                            found_cultures.Add((culture_id, culture_name, heritage_name));
                            Program.Logger.Debug($"Processed Culture: ID={culture_id} | Name={culture_name} | Heritage={heritage_name}");
                        }
                        isSearchStared = false;
                        culture_id = ""; culture_name = ""; heritage_name = "";
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
                        var ownerCulture = army.Owner?.GetCulture();
                        if (ownerCulture != null)
                        {
                            army.Commander.ChangeCulture(ownerCulture);
                        }
                    }
                }

                // KNIGHTS
                if (army.Knights != null && army.Knights.GetKnightsList() != null)
                {
                    foreach (Knight knight in army.Knights.GetKnightsList())
                    {
                        if (knight.GetCultureObj() == null)
                        {
                            Culture? mainParticipantCulture = null;
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

                            Culture? new_culture = army.Knights.GetKnightsList()?.Find(x => x.GetCultureObj() != null)?.GetCultureObj() ?? mainParticipantCulture;
                            
                            string newCultureID = new_culture?.ID ?? "no_culture_found";
                            Program.Logger.Debug($"Knight {knight.GetID()} in army {army.ID} has null culture. Assigning fallback culture. " +
                                $"Assigning fallback culture ID: {newCultureID}");
                            
                            if (new_culture != null)
                            {
                                knight.ChangeCulture(new_culture);
                            }
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
            // Add detailed start log
            Program.Logger.Debug("START SetCulturesToAll: Applying culture names");
            Program.Logger.Debug($"Found {foundCultures.Count} cultures to apply");
            
            foreach (Army army in armies)
            {
                Program.Logger.Debug($"Applying to ARMY: {army.ID}");
                
                // Owner culture log
                if (army.Owner?.GetCulture() != null && foundCultures.Exists(c => c.culture_id == army.Owner.GetCulture().ID))
                {
                    var found = foundCultures.First(c => c.culture_id == army.Owner.GetCulture().ID);
                    Program.Logger.Debug($"  APPLYING OWNER CULTURE | Old: {(army.Owner.GetCulture().GetCultureName() ?? "null")}/{(army.Owner.GetCulture().GetHeritageName() ?? "null")}");
                    Program.Logger.Debug($"    New: {found.culture_name}/{found.heritage_name}");
                    army.Owner.GetCulture().SetName(found.culture_name);
                    army.Owner.GetCulture().SetHeritage(found.heritage_name);
                }

                // Commander culture log
                if (army.Commander != null)
                {
                    var commanderCulture = army.Commander.GetCultureObj();
                    if (commanderCulture != null && foundCultures.Exists(c => c.culture_id == commanderCulture.ID))
                    {
                        var found = foundCultures.First(c => c.culture_id == commanderCulture.ID);
                        Program.Logger.Debug($"  APPLYING COMMANDER CULTURE | Old: {(army.Commander.GetCultureName() ?? "null")}/{(army.Commander.GetHeritageName() ?? "null")}");
                        Program.Logger.Debug($"    New: {found.culture_name}/{found.heritage_name}");
                        commanderCulture.SetName(found.culture_name);
                        commanderCulture.SetHeritage(found.heritage_name);
                    }
                }

                // Knights culture log
                if (army.Knights?.GetKnightsList() != null)
                {
                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        string? knightCultureID = knight.GetCultureObj()?.ID;
                        if (knightCultureID != null && foundCultures.Exists(c => c.culture_id == knightCultureID))
                        {
                            var found = foundCultures.Find(c => c.culture_id == knightCultureID);
                            Program.Logger.Debug($"  APPLYING KNIGHT CULTURE | Old: {(knight.GetCultureName() ?? "null")}/{(knight.GetHeritageName() ?? "null")}");
                            Program.Logger.Debug($"    New: {found.culture_name}/{found.heritage_name}");
                            knight.GetCultureObj().SetName(found.culture_name);
                            knight.GetCultureObj().SetHeritage(found.heritage_name);
                        }
                    }
                }

                // Regiments culture log
                Program.Logger.Debug($"  Processing {army.ArmyRegiments.Sum(r => r.Regiments?.Count ?? 0)} regiments");
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment.Regiments == null) continue;
                    foreach (Regiment regiment in armyRegiment.Regiments)
                    {
                        if (regiment.Culture == null)
                        {
                            Program.Logger.Debug($"    SKIPPING NULL CULTURE REGIMENT: {regiment.ID}");
                            continue;
                        }
                        
                        string regimentCultureID = regiment.Culture.ID;
                        if (foundCultures.Exists(c => c.culture_id == regimentCultureID))
                        {
                            var found = foundCultures.Find(c => c.culture_id == regimentCultureID);
                            Program.Logger.Debug($"    APPLYING REGIMENT CULTURE: {regiment.ID}");
                            Program.Logger.Debug($"      Old: {(regiment.Culture.GetCultureName() ?? "null")}/{(regiment.Culture.GetHeritageName() ?? "null")}");
                            Program.Logger.Debug($"      New: {found.culture_name}/{found.heritage_name}");
                            regiment.Culture.SetName(found.culture_name);
                            regiment.Culture.SetHeritage(found.heritage_name);
                        }
                    }
                }
            }
            Program.Logger.Debug("END SetCulturesToAll");
        }

        
        /*##############################################
         *####                  Unit                #### 
         *####--------------------------------------####
         *####         Searcher for unit file       ####
         *##############################################
         */

        public static (bool searchHasStarted, Army? army) SearchUnit(string unitID, List<Army> armies)
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

        public static (bool searchHasStarted, ArmyRegiment? regiment) SearchArmyRegiments(string armyRegimentId, List<Army> armies)
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

        public static (bool searchHasStarted, Regiment? regiment) SearchRegiments(string regiment_id, List<Army> armies)
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

        public static (bool searchStarted, Army? searchingArmy, bool isCommander, bool isMainCommander, bool isKnight, Knight? knight, bool isOwner) SearchCharacters(string id, List<Army> armies)
        {
            foreach (Army army in armies)
            {
                //Main Commanders
                if(army.Commander != null && id == army.Commander.ID && id == army.Owner?.GetID())
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
                if(id == army.CommanderID && id == army.Owner?.GetID())
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
                else if (army.Knights?.GetKnightsList() != null)
                {
                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        if (id == knight.GetID() && id == army.Owner?.GetID())
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
                else if (id == army.Owner?.GetID())
                {
                    Program.Logger.Debug($"Found character '{id}': Owner of army '{army.ID}'.");
                    return (true, army, false, false,false, null, true);
                }
            }

            return (false, null, false, false,false, null, false);
        }

        
        /*##############################################
         *####                  Unit                #### 
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

                Program.Logger.Debug($"Attempting to get AttilaKey for Unit: Name='{unit.GetName()}', CK3 Type='{unit.GetRegimentType()}', Culture='{unit.GetCulture()}', Heritage='{unit.GetHeritage()}', IsMercenary='{unit.IsMerc()}'");
                string key = UnitMappers_BETA.GetUnitKey(unit);
                if (key == UnitMappers_BETA.NOT_FOUND_KEY)
                {
                    Program.Logger.Debug($"Unit key not found for '{unit.GetName()}' ({unit.GetCulture()}). Attempting to find a default fallback.");
                    string fallbackKey = UnitMappers_BETA.GetDefaultUnitKey(unit.GetRegimentType());
                    if (fallbackKey != UnitMappers_BETA.NOT_FOUND_KEY)
                    {
                        Program.Logger.Debug($"Using default fallback unit key '{fallbackKey}' for unit '{unit.GetName()}'.");
                        unit.SetUnitKey(fallbackKey);
                    }
                    else
                    {
                        Program.Logger.Debug($"WARNING: No default fallback unit key found for type '{unit.GetRegimentType()}'. Unit '{unit.GetName()}' will be dropped.");
                        unit.SetUnitKey(string.Empty); 
                    }
                }
                else
                {
                    unit.SetUnitKey(key);
                }
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
                    if (!Int32.TryParse(ModOptions.FullArmies(regiment.regiment), out int soldiersNum) || soldiersNum == 0)
                    {
                        continue;
                    }

                    Unit unit;
                    if (regiment.type == RegimentType.Levy)
                    {
                        Culture? levyCulture = regiment.regiment.Culture ?? army.Owner?.GetCulture();
                        if (regiment.regiment.isMercenary())
                            unit = new Unit("Levy", soldiersNum, levyCulture, regiment.type, true);
                        else
                            unit = new Unit("Levy", soldiersNum, levyCulture, regiment.type);
                    }
                    else if (regiment.type == RegimentType.MenAtArms)
                        if (regiment.regiment.isMercenary())
                            unit = new Unit(regiment.maa_name, soldiersNum, regiment.regiment.Culture, regiment.type, true);
                        else
                            unit = new Unit(regiment.maa_name, soldiersNum, regiment.regiment.Culture, regiment.type);
                    else
                        continue;

                    if (unit != null)
                        units.Add(unit);


                }

                // ADD COMMANDER UNIT
                if (army.Commander != null)
                {
                    var cmdr = army.Commander;
                    Unit commanderUnit = new Unit(cmdr.Name, cmdr.GetUnitSoldiers(), cmdr.GetCultureObj(), RegimentType.Commander);
                    commanderUnit.SetCharacterRank(cmdr.Rank);
                    units.Add(commanderUnit);
                }

                // ADD KNIGHT UNITS
                if (army.Knights != null && army.Knights.GetKnightsList() != null)
                {
                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        Unit knightUnit = new Unit(knight.GetName(), knight.GetSoldiers(), knight.GetCultureObj(), RegimentType.Knight);
                        knightUnit.SetCharacterRank(knight.Rank);
                        units.Add(knightUnit);
                    }
                }


                // Separate character units from regular units to prevent merging
                var characterUnits = units.Where(u => u.GetRegimentType() == RegimentType.Commander || u.GetRegimentType() == RegimentType.Knight).ToList();
                var regularUnits = units.Where(u => u.GetRegimentType() != RegimentType.Commander && u.GetRegimentType() != RegimentType.Knight).ToList();

                // Organize only regular units
                regularUnits = OrganizeUnitsIntoCultures(regularUnits, army.Owner);
                regularUnits = OrganizeLeviesUnits(regularUnits);

                // Combine them back
                var allUnits = new List<Unit>();
                allUnits.AddRange(characterUnits);
                allUnits.AddRange(regularUnits);

                allUnits = GetAllUnits_AttilaFaction(allUnits);
                allUnits = GetAllUnits_Max(allUnits);
                allUnits = GetAllUnits_UnitKeys(allUnits);
                army.SetUnits(allUnits);
                Program.Logger.Debug($"Finished creating {units.Count} units for army {army.ID}.");
                //army.PrintUnits();
            }
            Program.Logger.Debug("Finished creating units for all armies.");
        }

        static List<Unit> OrganizeUnitsIntoCultures(List<Unit> units, Owner? owner)
        {
            Program.Logger.Debug($"Organizing {units.Count} units by culture for owner {owner?.GetID() ?? "Unknown"}");
            foreach (var unit in units)
            {
                Program.Logger.Debug($"- Unit: {unit.GetName()}, Soldiers: {unit.GetSoldiers()}, " +
                    $"Culture: {unit.GetObjCulture()?.GetCultureName() ?? "null"}, " +
                    $"Heritage: {unit.GetObjCulture()?.GetHeritageName() ?? "null"}");
            }

            var organizedUnits = new List<Unit>();

            // Group units by Name and Culture
            var groupedUnits = units.GroupBy(u => new {
                Name = u.GetName(),
                Culture = u.GetCulture(),
                Type = u.GetRegimentType(),
                IsMerc = (u.GetRegimentType() == RegimentType.Levy) ? false : u.IsMerc()
            });

            // Merge units with the same Name and Culture
            foreach (var group in groupedUnits)
            {
                int totalSoldiers = group.Sum(u => u.GetSoldiers());

                // Determine the correct mercenary status for the merged unit
                bool isMerc = group.Key.Type == RegimentType.Levy ? group.Any(u => u.IsMerc()) : group.Key.IsMerc;

                // Create a new Unit with the merged NumberOfSoldiers
                Unit mergedUnit = new Unit(group.Key.Name, totalSoldiers, group.First().GetObjCulture(), group.Key.Type, isMerc, owner);
                
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
            Unit? biggest = null;
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

            if (biggest == null) return units;

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
