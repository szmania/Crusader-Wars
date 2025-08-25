using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

namespace CrusaderWars.data.save_file
{


    public static class ArmiesReader
    {

        // V1.0 Beta
        static List<Army> attacker_armies;
        static List<Army> defender_armies;
        public static List<(string name, int index)> save_file_traits { get; set; }
        public static (List<Army> attacker, List<Army> defender) ReadBattleArmies()
        {
            Program.Logger.Debug("Reading battle armies from CK3 save data...");
            try
            {
                ReadSaveFileTraits();
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error reading traits: {ex.Message}");
                throw new Exception("Couldn't read traits data", ex);
            }

            ReadCombatArmies(BattleResult.Player_Combat);
            ReadArmiesData();
            ReadArmiesUnits();
            ReadArmyRegiments();
            ReadCombatSoldiersNum(BattleResult.Player_Combat);
            ReadRegiments();
            ReadOriginsKeys();

            // Log army counts before proceeding
            Program.Logger.Debug($"Armies after initialization: attacker={attacker_armies.Count}, defender={defender_armies.Count}");

            if (attacker_armies.Count == 0 || defender_armies.Count == 0)
            {
                Program.Logger.Debug("ERROR: No armies were initialized");
                throw new Exception("No armies found in save data. Possible corrupt save or unsupported game state.");
            }

            try
            {
                LandedTitles.ReadProvinces(attacker_armies, defender_armies);
                ReadCountiesManager();
                ReadMercenaries();
                BattleFile.SetArmiesSides(attacker_armies, defender_armies);

                CreateKnights();
                CreateMainCommanders();
                ReadCharacters();
                ReadCourtPositions();
                CheckForNullCultures();
                ReadCultureManager();

                // Organize Units
                CreateUnits();

                // Print Armies
                Print.PrintArmiesData(attacker_armies);
                Print.PrintArmiesData(defender_armies);

                Program.Logger.Debug($"Finished reading battle armies. Found {attacker_armies.Count} attacker armies and {defender_armies.Count} defender armies.");
                return (attacker_armies, defender_armies);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error processing army data: {ex.Message}");
                throw new Exception("Error processing army data", ex);
            }
        }


        static void ClearNullArmyRegiments()
        {
            Program.Logger.Debug("Clearing null army regiments...");
            // Clear Empty Regiments
            for (int i = 0; i < attacker_armies.Count; i++)
            {
                attacker_armies[i].ClearNullArmyRegiments();
            }
            for (int i = 0; i < defender_armies.Count; i++)
            {
                defender_armies[i].ClearNullArmyRegiments();
            }
            Program.Logger.Debug("Finished clearing null army regiments.");
        }

        static void CheckForNullCultures()
        {
            Program.Logger.Debug("ATTACKER  WITH NULL CULTURE REGIMENTS:\n");
            foreach(Regiment regiment in attacker_armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiment => armyRegiment.Regiments))
            {
                Program.Logger.Debug($"WARNING - REGIMENT {regiment.ID} HAS A NULL CULTURE");
            }

            Program.Logger.Debug("DEFENDER  WITH NULL CULTURE REGIMENTS:\n");
            foreach (Regiment regiment in defender_armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiment => armyRegiment.Regiments))
            {
                Program.Logger.Debug($"WARNING - REGIMENT {regiment.ID} HAS A NULL CULTURE");
            }
        }

        static void ReadSaveFileTraits()
        {
            Program.Logger.Debug("Reading save file traits...");
            MatchCollection allTraits = Regex.Matches(File.ReadAllText(Writter.DataFilesPaths.Traits_Path()), @" (\w+)");
            save_file_traits = new List<(string name, int index)>();

            for (int i = 0; i < allTraits.Count; i++)
            {
                //save_file_traits[i] = (allTraits[i].Groups[1].Value, i);
                save_file_traits.Add((allTraits[i].Groups[1].Value, i));
            }
            Program.Logger.Debug($"Finished reading save file traits. Found {save_file_traits.Count} traits.");
        }
         
        public static int GetTraitIndex(string trait_name)
        {
            int index;
            index = save_file_traits.FirstOrDefault(x => x.name == trait_name).index;
            Program.Logger.Debug($"GetTraitIndex for '{trait_name}': found index {index}");
            return index;

        }

        public static string GetTraitKey(int trait_index)
        {
            string key;
            key = save_file_traits.FirstOrDefault(x => x.index == trait_index).name;
            Program.Logger.Debug($"GetTraitKey for index '{trait_index}': found key '{key}'");
            return key;

        }

        static void ReadCourtPositions()
        {
            Program.Logger.Debug("Reading court positions...");
            string profession="";
            string employeeID="";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.CourtPositions_Path()))
            {
                string line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (line == "\t\t\tcourt_position=\"bodyguard_court_position\"")
                    {
                        profession = "bodyguard";
                    }
                    else if (line == "\t\t\tcourt_position=\"champion_court_position\"")
                    {
                        profession = "personal_champion";
                    }
                    else if (line == "\t\t\tcourt_position=\"garuva_warrior_court_position\"")
                    {
                        profession = "garuva_warrior";
                    }
                    else if (line.Contains("\t\t\temployee="))
                    {
                        employeeID = Regex.Match(line, @"\d+").Value;
                    }
                    else if (line.StartsWith("\t\t\temployer="))
                    {
                        string employerID = Regex.Match(line, @"\d+").Value;

                        var army = attacker_armies.Find(x => x.CommanderID == employerID)
                                   ?? defender_armies.Find(x => x.CommanderID == employerID) ?? null;

                        if (army != null)
                        {
                            Program.Logger.Debug($"Assigning court position '{profession}' to employee '{employeeID}' for employer '{employerID}' in army '{army.ID}'.");
                            army.Commander?.AddCourtPosition(profession, employeeID);
                        }

                    }
                }
            }
            Program.Logger.Debug("Finished reading court positions.");
        }


        static void ReadCharacters()
        {
            Program.Logger.Debug("Reading characters data...");
            bool searchStarted = false;
            bool isKnight = false, isCommander = false, isMainCommander = false, isOwner = false;
            Army searchingArmy = null;
            Knight searchingKnight = null;

            //non-main army commander variables
            int nonMainCommander_Rank = 1;
            string nonMainCommander_Name="";
            BaseSkills nonMainCommander_BaseSkills = null;
            Culture nonMainCommander_Culture = null;
            Accolade nonMainCommander_Accolade = null;
            int nonMainCommander_Prowess = 0;
            List<(int index, string key)> nonMainCommander_Traits = null;



            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Living_Path()))
            {
                string line;
                while((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (Regex.IsMatch(line, @"\d+={") && !searchStarted)
                    {
                        string line_id = Regex.Match(line, @"(\d+)={").Groups[1].Value;

                        var searchingData = Armies_Functions.SearchCharacters(line_id, attacker_armies);
                        if (searchingData.searchStarted)
                        {
                            Program.Logger.Debug($"Found character '{line_id}' in battle armies. Starting data extraction.");
                            searchStarted = true;
                            isKnight = searchingData.isKnight;
                            isMainCommander = searchingData.isMainCommander;
                            isCommander = searchingData.isCommander;
                            isOwner = searchingData.isOwner;
                            searchingArmy = searchingData.searchingArmy;
                            searchingKnight = searchingData.knight;

                        }
                        else
                        {
                            searchingData = Armies_Functions.SearchCharacters(line_id, defender_armies);
                            if (searchingData.searchStarted)
                            {
                                Program.Logger.Debug($"Found character '{line_id}' in battle armies. Starting data extraction.");
                                searchStarted = true;
                                isKnight = searchingData.isKnight;
                                isMainCommander = searchingData.isMainCommander;
                                isCommander = searchingData.isCommander;
                                isOwner = searchingData.isOwner;
                                searchingArmy = searchingData.searchingArmy;
                                searchingKnight = searchingData.knight;
                            }
                        }
                    }
                    else if (searchStarted && line.StartsWith("\tfirst_name=")) //# FIRST NAME
                    {
                        if(isCommander)
                        {
                            nonMainCommander_Name = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                        }
                    }
                    else if (searchStarted && line.StartsWith("\tskill={")) //# BASE SKILLS
                    {
                        MatchCollection found_skills = Regex.Matches(line, @"\d+");
                        var baseSkills_list = new List<string>();
                        baseSkills_list = found_skills.Cast<Match>().Select(m => m.Value).ToList();

                        if (isMainCommander)
                        {
                            searchingArmy.Commander.SetBaseSkills(new BaseSkills(baseSkills_list));
                        }
                        else if (isCommander)
                        {
                            nonMainCommander_BaseSkills = new BaseSkills(baseSkills_list);
                        }
                        else if(isKnight)
                        {
                            searchingKnight.SetBaseSkills(new BaseSkills(baseSkills_list));
                        }
                    }
                    else if(searchStarted && line.StartsWith("\t\taccolade=")) // # ACCOLADE
                    {
                        string accoladeID = Regex.Match(line, @"\d+").Value;
                        if(isKnight)
                        {
                            searchingKnight.IsAccolade(true, GetAccolade(accoladeID));
                        }
                        else if(isMainCommander)
                        {
                            searchingArmy.Commander.SetAccolade(GetAccolade(accoladeID));
                        }
                        else if(isCommander)
                        {
                            nonMainCommander_Accolade = GetAccolade(accoladeID);
                        }
                    }
                    else if (searchStarted && line.StartsWith("\ttraits={")) //# TRAITS
                    {
                        MatchCollection found_traits = Regex.Matches(line, @"\d+");
                        var traits_list = new List<(int index, string key)>();
                        foreach (Match found_trait in found_traits)
                        {
                            int index = Int32.Parse(found_trait.Value);
                            string key = GetTraitKey(index);
                            traits_list.Add((index, key));
                        }

                        if (isMainCommander)
                        {
                            searchingArmy.Commander.SetTraits(traits_list);
                        }
                        else if (isCommander)
                        {
                            nonMainCommander_Traits = traits_list;
                        }
                        else if (isKnight)
                        {
                            searchingArmy.Knights.GetKnightsList().FirstOrDefault(x => x == searchingKnight).SetTraits(traits_list);
                            searchingArmy.Knights.GetKnightsList().FirstOrDefault(x => x == searchingKnight).SetWoundedDebuffs();
                        }
                    }
                    else if (searchStarted && line.Contains("\tculture=")) //# CULTURE
                    {
                        string culture_id = Regex.Match(line, @"\d+").Value;
                        if (isKnight)
                        {
                            searchingArmy.Knights.GetKnightsList().Find(x => x == searchingKnight).ChangeCulture(new Culture(culture_id));
                            searchingArmy.Knights.SetMajorCulture();
                            if(isOwner) 
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }

                        else if (isMainCommander)
                        {
                            if(searchingArmy.IsPlayer())
                                searchingArmy.Commander.ChangeCulture(new Culture(CK3LogData.LeftSide.GetCommander().culture_id));
                            else
                                searchingArmy.Commander.ChangeCulture(new Culture(CK3LogData.RightSide.GetCommander().culture_id));
                            if (isOwner)
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                            /*
                            searchingArmy.Commander.ChangeCulture(new Culture(culture_id));
                            if (isOwner) 
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                            */
                        }
                        else if(isCommander)
                        {
                            nonMainCommander_Culture = new Culture(culture_id);
                            if (isOwner) 
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }
                        else
                        {
                            searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }


                    }
                    else if (searchStarted && line.Contains("\t\tdomain={")) //# TITLES
                    {
                        string firstTitleID = Regex.Match(line, @"\d+").Value;
                        if (isCommander)
                        {
                            if (isOwner) searchingArmy.Owner.SetPrimaryTitle(GetTitleKey(firstTitleID));

                            var landedTitlesData = GetCommanderNobleRankAndTitleName(firstTitleID);
                            nonMainCommander_Rank = landedTitlesData.rank;
                            if (searchingArmy.IsPlayer())
                            {
                                if (CK3LogData.LeftSide.GetKnights().Exists(x => x.id == searchingArmy.CommanderID))
                                {
                                    var commanderKnight = CK3LogData.LeftSide.GetKnights().FirstOrDefault(x => x.id == searchingArmy.CommanderID);
                                    nonMainCommander_Prowess = Int32.Parse(commanderKnight.prowess);
                                    if (nonMainCommander_Rank == 1)
                                        nonMainCommander_Name = commanderKnight.name;
                                    else
                                        nonMainCommander_Name = $"{commanderKnight.name} of {landedTitlesData.titleName}";
                                }
                                else
                                {
                                    nonMainCommander_Prowess = nonMainCommander_BaseSkills.prowess;
                                    if (nonMainCommander_Rank > 1)
                                        nonMainCommander_Name += $" of {landedTitlesData.titleName}";
                                }

                            }
                            else
                            {
                                if (CK3LogData.RightSide.GetKnights().Exists(x => x.id == searchingArmy.CommanderID))
                                {
                                    var commanderKnight = CK3LogData.RightSide.GetKnights().FirstOrDefault(x => x.id == searchingArmy.CommanderID);
                                    nonMainCommander_Prowess = Int32.Parse(commanderKnight.prowess);
                                    if (nonMainCommander_Rank == 1)
                                        nonMainCommander_Name = commanderKnight.name;
                                    else
                                        nonMainCommander_Name = $"{commanderKnight.name} of {landedTitlesData.titleName}";
                                }
                                else
                                {
                                    nonMainCommander_Prowess = nonMainCommander_BaseSkills.prowess;
                                    if (nonMainCommander_Rank > 1)
                                        nonMainCommander_Name += $" of {landedTitlesData.titleName}";
                                }

                            }
                        }
                        else if(isOwner) // <-- Owner
                        {
                            searchingArmy.Owner.SetPrimaryTitle(GetTitleKey(firstTitleID));
                        }
                    }
                    else if (searchStarted && line == "}")
                    {
                        if (isCommander)
                        {
                            Program.Logger.Debug($"Creating non-main commander '{nonMainCommander_Name}' ({searchingArmy.CommanderID}) for army '{searchingArmy.ID}'.");
                            searchingArmy.SetCommander(new CommanderSystem(nonMainCommander_Name, searchingArmy.CommanderID, nonMainCommander_Prowess, nonMainCommander_Rank, nonMainCommander_BaseSkills, nonMainCommander_Culture));
                            searchingArmy.Commander.SetTraits(nonMainCommander_Traits);
                            if (nonMainCommander_Accolade != null) searchingArmy.Commander.SetAccolade(nonMainCommander_Accolade);
                        }

                        searchStarted = false;
                        isCommander = false;
                        isMainCommander = false;
                        isOwner = false;
                        isKnight = false;
                        searchingKnight = null;
                        searchingArmy = null;

                        nonMainCommander_Rank = 1;
                        nonMainCommander_Name = "";
                        nonMainCommander_BaseSkills = null;
                        nonMainCommander_Culture = null;
                        nonMainCommander_Traits = null;
                        nonMainCommander_Prowess = 0;
                    }
                }
            }
            Program.Logger.Debug("Finished reading characters data.");
        }

        static Accolade GetAccolade(string accoladeID)
        {
            Program.Logger.Debug($"Searching for accolade with ID: {accoladeID}");
            bool searchStarted = false;
            string primaryAttribute = "";
            string secundaryAttribute = "";
            string glory = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Accolades()))
            {
                string line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (!searchStarted && line == $"\t\t{accoladeID}={{")
                    {
                        searchStarted = true;
                    }
                    else if (searchStarted && line.StartsWith("\t\t\tprimary="))
                    {
                        primaryAttribute = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                    }
                    else if (searchStarted && line.StartsWith("\t\t\tsecundary="))
                    {
                        secundaryAttribute = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                    }
                    else if (searchStarted && line.StartsWith("\t\t\tglory="))
                    {
                        glory = Regex.Match(line, @"\d+").Value;
                    }
                    else if (searchStarted && line == "\t\t}")
                    {
                        searchStarted = false;
                        Program.Logger.Debug($"Found accolade {accoladeID}: Primary={primaryAttribute}, Secondary={secundaryAttribute}, Glory={glory}");
                        return new Accolade(accoladeID, primaryAttribute, secundaryAttribute, glory);
                    }
                }
            }
            Program.Logger.Debug($"Accolade with ID {accoladeID} not found.");
            return null;
        }

        static string GetTitleKey(string title_id)
        {
            Program.Logger.Debug($"Getting title key for title ID: {title_id}");
            bool searchStarted = false;
            string titleKey = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.LandedTitles()))
            {
                string line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (line == $"{title_id}={{")
                    {
                        searchStarted = true;
                    }
                    else if (searchStarted && line.StartsWith("\tkey=")) //# KEY
                    {
                        titleKey = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                        Program.Logger.Debug($"Found title key '{titleKey}' for title ID '{title_id}'.");
                        return titleKey;
                    }

                }
                Program.Logger.Debug($"Title key for title ID '{title_id}' not found.");
                return titleKey;
            }
        }

        static void ReadCombatArmies(string g)
        {
            bool isAttacker = false, isDefender = false;

            using (StringReader SR = new StringReader(g))//Player_Combat
            {
                while (true)
                {
                    string line = SR.ReadLine();
                    if (line == null) break;

                    if (line == "\t\t\tattacker={")
                    {
                        isAttacker = true;
                        isDefender = false;
                    }
                    else if (line == "\t\t\tdefender={")
                    {
                        isAttacker = false;
                        isDefender = true;
                    }
                    else if (line == "\t\t\t}")
                    {
                        isDefender = false;
                        isAttacker = false;
                    }

                    if (isAttacker && line.Contains("\t\t\t\tarmies={"))
                    {
                        MatchCollection found_armies = Regex.Matches(line, @"(\d+) ");
                        attacker_armies = new List<Army>();

                        for(int i = 0; i < found_armies.Count; i++)
                        {
                            //Create new Army with combat sides on the constructor
                            //Army army
                            string id = found_armies[i].Groups[1].Value;
                            string combat_side = "attacker";

                            // main army
                            if(i == 0) //<-------------------------------------------------------------------[FIX THIS] !!!
                            {
                                Army army = new Army(id, combat_side, true);
                                attacker_armies.Add(army);
                            }
                            // ally army
                            else
                            {
                               Army army = new Army(id, combat_side, false);
                               attacker_armies.Add(army);
                            }
                        }
  
                    }
                    else if (isDefender && line.Contains("\t\t\t\tarmies={"))
                    {
                        MatchCollection found_armies = Regex.Matches(line, @"(\d+) ");
                        defender_armies = new List<Army>();

                        for (int i = 0; i < found_armies.Count; i++)
                        {
                            //Create new Army with combat sides on the constructor
                            //Army army
                            string id = found_armies[i].Groups[1].Value;
                            string combat_side = "defender";

                            // main army
                            if (i == 0)//<-------------------------------------------------------------------[FIX THIS] !!!
                            {
                                Army army = new Army(id, combat_side, true);
                                defender_armies.Add(army);
                            }
                            // ally army
                            else
                            {
                                Army army = new Army(id, combat_side, false);
                                defender_armies.Add(army);
                            }
                        }
                    }

                }
            }
        }

        static void ReadCombatSoldiersNum(string combat_string)
        {
            bool isAttacker = false, isDefender = false;
            string searchingArmyRegiment = null;
            using (StringReader SR = new StringReader(combat_string))//Player_Combat
            {
                while (true)
                {
                    string line = SR.ReadLine();
                    if (line == null) break;

                    if (line == "\t\t\tattacker={")
                    {
                        isAttacker = true;
                        isDefender = false;
                    }
                    else if (line == "\t\t\tdefender={")
                    {
                        isAttacker = false;
                        isDefender = true;
                    }
                    else if (line == "\t\t\t}")
                    {
                        isDefender = false;
                        isAttacker = false;
                    }

                    else if (isAttacker && line.Contains("\t\t\t\t\t\tregiment="))
                    {
                        searchingArmyRegiment = Regex.Match(line, @"\d+").Value;
                    }
                    else if (isDefender && line.Contains("\t\t\t\t\t\tregiment="))
                    {
                        searchingArmyRegiment = Regex.Match(line, @"\d+").Value;
                    }

                    else if(isAttacker && line.Contains("\t\t\t\t\t\tstarting="))
                    {
                        string startingNum = Regex.Match(line,@"\d+").Value;

                        foreach(var army in attacker_armies)
                        {
                            army.ArmyRegiments.FirstOrDefault(x => x.ID == searchingArmyRegiment)?.SetStartingNum(startingNum);
                        }

                    }
                    else if(isDefender && line.Contains("\t\t\t\t\t\tstarting="))
                    {
                        string startingNum = Regex.Match(line, @"\d+").Value;
                        foreach (var army in defender_armies)
                        {
                            army.ArmyRegiments.FirstOrDefault(x => x.ID == searchingArmyRegiment)?.SetStartingNum(startingNum);
                        }
                    }

                    else if((isAttacker || isDefender) && line == "\t\t\t}")
                    {
                        isAttacker = false;
                        isDefender = false;
                        searchingArmyRegiment = null;
                    }

                    //end line
                    else if(line == "\t\t}")
                    {
                        break;
                    }
 

                }
            }
        }


    }
}
