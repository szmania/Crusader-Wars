using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CrusaderWars.twbattle;
using CrusaderWars.data.battle_results;

namespace CrusaderWars.data.save_file
{

    public static class ArmiesReader
    {

        // V1.0 Beta
        static List<Army> attacker_armies = new List<Army>();
        static List<Army> defender_armies = new List<Army>();
        public static List<(string name, int index)> save_file_traits { get; set; } = new List<(string name, int index)>();
        public static (List<Army> attacker, List<Army> defender) ReadBattleArmies()
        {
            attacker_armies.Clear();
            defender_armies.Clear();
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

            if (twbattle.BattleState.IsSiegeBattle)
            {
                // --- NEW LOGIC START ---
                var (besiegerIds, reliefIds, combatBlock) = FindSiegeCombatBlockAndExtractArmies();

                if (combatBlock != null)
                {
                    Program.Logger.Debug("Found combat block. Populating mobile armies from it.");
                    BattleResult.Player_Combat = combatBlock;
                    BattleState.HasReliefArmy = reliefIds.Any();

                    foreach (var armyId in besiegerIds)
                    {
                        attacker_armies.Add(new Army(armyId, "attacker", false));
                    }

                    foreach (var armyId in reliefIds)
                    {
                        var reliefArmy = new Army(armyId, "defender", false);
                        reliefArmy.SetAsReinforcement(true);
                        defender_armies.Add(reliefArmy);
                    }
                    Program.Logger.Debug($"Populated from combat block: {attacker_armies.Count} besiegers, {defender_armies.Count} relief forces.");
                }
                else
                {
                    Program.Logger.Debug("No combat block found. Using Units.txt to find besieging armies (simple siege).");
                    // Determine the player's actual side, accounting for the log swap when the player is besieged.
                    DataSearchSides playerSide;
                    // If the player is the main participant or commander of the LeftSide (the besiegers in the log),
                    // they are on the attacking side. Otherwise, they must be involved with the RightSide (besieged).
                    if (CK3LogData.LeftSide.GetMainParticipant().id == DataSearch.Player_Character.GetID() ||
                        CK3LogData.LeftSide.GetCommander().id == DataSearch.Player_Character.GetID())
                    {
                        playerSide = DataSearchSides.LeftSide;
                        Program.Logger.Debug("Player is on LeftSide in the log.");
                    }
                    else
                    {
                        playerSide = DataSearchSides.RightSide;
                        Program.Logger.Debug("Player is not on LeftSide in the log, so assigning to RightSide.");
                    }

                    // Create sets of character IDs for quick lookup
                    var attackerCharIDs = new HashSet<string>(CK3LogData.LeftSide.GetKnights().Select(k => k.id).Append(CK3LogData.LeftSide.GetMainParticipant().id));
                    var defenderCharIDs = new HashSet<string>(CK3LogData.RightSide.GetKnights().Select(k => k.id).Append(CK3LogData.RightSide.GetMainParticipant().id).Append(CK3LogData.RightSide.GetCommander().id));
                    // Also include the player's own character ID in the appropriate set to correctly identify armies they own but don't command.
                    if (playerSide == DataSearchSides.LeftSide)
                    {
                        attackerCharIDs.Add(CK3LogData.LeftSide.GetCommander().id);
                        attackerCharIDs.Add(DataSearch.Player_Character.GetID());
                        Program.Logger.Debug($"Player is on LeftSide, adding Player ID {DataSearch.Player_Character.GetID()} to attacker character set.");
                    }
                    else
                    {
                        defenderCharIDs.Add(CK3LogData.LeftSide.GetCommander().id);
                        defenderCharIDs.Add(DataSearch.Player_Character.GetID());
                        Program.Logger.Debug($"Player is on RightSide, adding Player ID {DataSearch.Player_Character.GetID()} to defender character set.");
                    }

                    // Pre-parse Armies.txt to find all merged sub-armies
                    var mergedSubArmyIDs = new HashSet<string>();
                    try
                    {
                        string armiesContent = File.ReadAllText(Writter.DataFilesPaths.Armies_Path());
                        string[] armyBlocks = Regex.Split(armiesContent, @"(?=\s*\t\t\d+={)");
                        foreach (var block in armyBlocks)
                        {
                            if (string.IsNullOrWhiteSpace(block)) continue;
                            var mergedArmiesMatch = Regex.Match(block, @"merged_armies={\s*([\d\s]+)\s*}");
                            if (mergedArmiesMatch.Success)
                            {
                                var ids = mergedArmiesMatch.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var id in ids)
                                {
                                    mergedSubArmyIDs.Add(id);
                                }
                            }
                        }
                        Program.Logger.Debug($"Identified {mergedSubArmyIDs.Count} merged sub-armies.");
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Debug($"Error pre-parsing Armies.txt for merged armies: {ex.Message}");
                    }


                    DataSearchSides besiegerSide = DataSearchSides.LeftSide; // Besiegers are always LeftSide in the log
                    var potentialBesiegerArmyIDs = new List<string>();
                    var potentialReliefArmyIDs = new List<string>();

                    // Pre-parse Armies.txt to map army IDs to commander IDs
                    var armyToCommanderMap = new Dictionary<string, string>();
                    try
                    {
                        string armiesContent = File.ReadAllText(Writter.DataFilesPaths.Armies_Path());
                        string[] armyBlocks = Regex.Split(armiesContent, @"(?=\s*\t\t\d+={)");
                        foreach (var block in armyBlocks)
                        {
                            if (string.IsNullOrWhiteSpace(block)) continue;
                            var armyIdMatch = Regex.Match(block, @"\t\t(\d+)={");
                            var commanderIdMatch = Regex.Match(block, @"commander=(\d+)");
                            if (armyIdMatch.Success && commanderIdMatch.Success)armyToCommanderMap[armyIdMatch.Groups[1].Value] = commanderIdMatch.Groups[1].Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Debug($"Error pre-parsing Armies.txt to map commanders: {ex.Message}");
                        throw new Exception("Could not map armies to commanders, cannot identify siege participants.", ex);
                    }

                    // 1. Find all mobile forces at the location and categorize their IDs
                    try
                    {
                        string unitsContent = File.ReadAllText(Writter.DataFilesPaths.Units_Path());
                        string[] unitBlocks = Regex.Split(unitsContent, @"(?=\s*\t\d+={)");

                        foreach (string block in unitBlocks)
                        {
                            if (string.IsNullOrWhiteSpace(block) || !block.Contains($"location={BattleResult.ProvinceID}")) continue;

                            // Modified regex to robustly handle whitespace
                            Match armyIdMatch = Regex.Match(block, @"army=(\d+)");
                            if (!armyIdMatch.Success)
                            {
                                continue; // This block in Units.txt is not a standard army, so skip it.
                            }
                            string armyID = armyIdMatch.Groups[1].Value;
                            string ownerID = Regex.Match(block, @"owner=(\d+)").Groups[1].Value; // Original line

                            armyToCommanderMap.TryGetValue(armyID, out var commanderID);

                            DataSearchSides? currentArmySide = null;
                            if (attackerCharIDs.Contains(ownerID) || (commanderID != null && attackerCharIDs.Contains(commanderID))) currentArmySide = DataSearchSides.LeftSide;
                            else if (defenderCharIDs.Contains(ownerID) || (commanderID != null && defenderCharIDs.Contains(commanderID))) currentArmySide = DataSearchSides.RightSide;
                            else continue;

                            // Categorize army IDs into potential besiegers or relief forces
                            if (currentArmySide == besiegerSide)
                            {
                                if (!potentialBesiegerArmyIDs.Contains(armyID)) potentialBesiegerArmyIDs.Add(armyID);
                            }
                            else
                            {
                                if (!potentialReliefArmyIDs.Contains(armyID)) potentialReliefArmyIDs.Add(armyID);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Debug($"Error reading besieger armies from Units.txt: {ex.Message}");
                    }

                    // 2. Filter out merged sub-armies to get only top-level commanders
                    var topLevelBesiegerIDs = potentialBesiegerArmyIDs.Where(id => !mergedSubArmyIDs.Contains(id)).ToList();
                    var topLevelReliefIDs = potentialReliefArmyIDs.Where(id => !mergedSubArmyIDs.Contains(id)).ToList();

                    Program.Logger.Debug($"Found {potentialBesiegerArmyIDs.Count} potential besieger armies, filtered to {topLevelBesiegerIDs.Count} top-level armies.");
                    Program.Logger.Debug($"Found {potentialReliefArmyIDs.Count} potential relief armies, filtered to {topLevelReliefIDs.Count} top-level armies.");

                    // 3. Create Army objects for the top-level armies
                    var besiegerForce = new List<Army>();
                    foreach (var armyID in topLevelBesiegerIDs)
                    {
                        string combatSide = "attacker"; // Besiegers are always attackers in Attila
                        Army army = new Army(armyID, combatSide, false); // isMainArmy will be set later
                        besiegerForce.Add(army);
                        Program.Logger.Debug($"Created top-level besieger army object for ID {armyID}.");
                    }

                    var reliefForce = new List<Army>();
                    foreach (var armyID in topLevelReliefIDs)
                    {
                        string combatSide = "defender"; // Relief forces are defenders in Attila
                        Army army = new Army(armyID, combatSide, false); // isMainArmy will be set later
                        reliefForce.Add(army);
                        Program.Logger.Debug($"Created top-level relief army object for ID {armyID}.");
                    }


                    // Flag relief forces as reinforcements
                    foreach (var army in reliefForce)
                    {
                        army.SetAsReinforcement(true);
                        Program.Logger.Debug($"Army {army.ID} flagged as reinforcement.");
                    }

                    // 3. Assign forces to Attila attacker/defender roles
                    if (!besiegerForce.Any() && !reliefForce.Any())
                    {
                        throw new Exception("Could not find any besieger or relief forces for the siege battle.");
                    }

                    Program.Logger.Debug("Assigning besieger to Attila attacker role and relief to defender role.");
                    attacker_armies.AddRange(besiegerForce);
                    defender_armies.AddRange(reliefForce); // Add relief forces to defender_armies
                }
                // --- NEW LOGIC END ---

                // --- This garrison logic should now be here, outside the if/else ---
                Army? garrisonArmy = null;
                try
                {
                    int garrisonSize = twbattle.Sieges.GetGarrisonSize();
                    if (garrisonSize > 0)
                    {
                        Program.Logger.Debug($"Found garrison of size {garrisonSize}. Creating garrison placeholder army.");
                        string garrisonCultureID = twbattle.Sieges.GetGarrisonCulture();
                        string garrisonHeritage = twbattle.Sieges.GetGarrisonHeritage();

                        // For sieges, the besieged side is always RightSide in the log data
                        var garrisonOwnerInfo = CK3LogData.RightSide.GetMainParticipant();
                        var garrisonOwner = new Owner(garrisonOwnerInfo.id, new Culture(garrisonOwnerInfo.culture_id));

                        garrisonArmy = sieges.GarrisonGenerator.CreateGarrisonPlaceholderArmy(garrisonSize, garrisonCultureID, garrisonHeritage, garrisonOwner, true);
                        if (garrisonArmy != null)
                        {
                            defender_armies.Add(garrisonArmy);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Failed to create garrison placeholder army: {ex.Message}");
                }
            }
            else if (BattleResult.Player_Combat is not null)
            {
                Program.Logger.Debug("Field battle detected. Reading armies from combat data.");
                ReadCombatArmies(BattleResult.Player_Combat);
            }


            ReadArmiesData();
            ReadArmiesUnits();
            ReadArmyRegiments();
            
            ReadCombatSoldiersNum(BattleResult.Player_Combat);
            ReadRegiments();
            ReadOriginsKeys();

            // Correct regiment soldiers to use starting values from combat data
            CorrectRegimentSoldiers();

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
                SetMAARegimentCultures(attacker_armies);
                SetMAARegimentCultures(defender_armies);
                ReadCourtPositions();
                CheckForNullCultures();
                ReadCultureManager();
                CreateUnits();

                // Expand garrison placeholder units into distributed levies
                var allArmies = attacker_armies.Concat(defender_armies).ToList();
                Armies_Functions.ExpandGarrisonArmies(allArmies);

                if (twbattle.BattleState.IsSiegeBattle)
                {
                    // --- Attacker (Besieger) Side ---
                    // Identify the main besieging army without consolidating. This allows BattleFile.cs
                    // to apply the user's "Armies Control" setting correctly.
                    // The besieger is always LeftSide in the log data for sieges.
                    var besiegers = attacker_armies.Where(a => !a.IsGarrison() && !a.IsReinforcementArmy()).ToList();
                    if (besiegers.Any())
                    {
                        var mainBesiegerCommanderId = CK3LogData.LeftSide.GetCommander().id;
                        var mainBesieger = besiegers.FirstOrDefault(a => a.CommanderID == mainBesiegerCommanderId);

                        if (mainBesieger == null)
                        {
                            mainBesieger = besiegers.OrderByDescending(a => a.GetTotalSoldiers()).FirstOrDefault();
                            if (mainBesieger != null) Program.Logger.Debug($"Main besieger commander's army not found. Using largest army as main: {mainBesieger.ID}");
                        }

                        if (mainBesieger != null)
                        {
                            mainBesieger.isMainArmy = true;
                            Program.Logger.Debug($"Main besieger army flagged: {mainBesieger.ID}");
                        }
                        else // Fallback if still null
                        {
                            besiegers[0].isMainArmy = true;
                            Program.Logger.Debug($"Could not determine main besieger. Using first army in list as main: {besiegers[0].ID}");
                        }
                    }

                    // --- Defender (Besieged) Side ---
                    // Identify the main defending army (garrison or main relief force).
                    // The besieged side is always RightSide in the log data for sieges.
                    var reliefForces = defender_armies.Where(a => a.IsReinforcementArmy()).ToList();
                    var garrison = defender_armies.FirstOrDefault(a => a.IsGarrison());

                    if (reliefForces.Any())
                    {
                        var mainDefenderCommanderId = CK3LogData.RightSide.GetCommander().id;
                        var mainReliefForce = reliefForces.FirstOrDefault(a => a.CommanderID == mainDefenderCommanderId);

                        if (mainReliefForce == null)
                        {
                            mainReliefForce = reliefForces.OrderByDescending(a => a.GetTotalSoldiers()).FirstOrDefault();
                            if (mainReliefForce != null) Program.Logger.Debug($"Main relief force commander's army not found. Using largest relief army as main: {mainReliefForce.ID}");
                        }

                        if (mainReliefForce != null)
                        {
                            mainReliefForce.isMainArmy = true;
                            Program.Logger.Debug($"Main relief force army flagged: {mainReliefForce.ID}");
                        }
                        else // Fallback if still null
                        {
                            reliefForces[0].isMainArmy = true;
                            Program.Logger.Debug($"Could not determine main relief force. Using first in list as main: {reliefForces[0].ID}");
                        }
                    }
                    else if (garrison != null)
                    {
                        // If there are no relief forces, the garrison is the main defending "army".
                        garrison.isMainArmy = true;
                        Program.Logger.Debug($"Garrison flagged as main defending army: {garrison.ID}");
                    }
                }

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

        public static string? FindWarID()
        {
            Program.Logger.Debug("Finding WarID by matching battle participants with war participants...");
            
            try
            {
                // Get the main participants from the current battle log data
                string leftParticipantId = CK3LogData.LeftSide.GetMainParticipant().id;
                string rightParticipantId = CK3LogData.RightSide.GetMainParticipant().id;
                
                if (string.IsNullOrEmpty(leftParticipantId) || string.IsNullOrEmpty(rightParticipantId))
                {
                    Program.Logger.Debug("Could not find main participants in CK3LogData. Cannot determine WarID.");
                    return null;
                }
                
                Program.Logger.Debug($"Battle participants - Left: {leftParticipantId}, Right: {rightParticipantId}");
                
                // Read the Wars.txt file
                string warsFilePath = Writter.DataFilesPaths.Wars_Path();
                if (!File.Exists(warsFilePath))
                {
                    Program.Logger.Debug($"Wars file not found at {warsFilePath}. Cannot determine WarID.");
                    return null;
                }
                
                string warsContent = File.ReadAllText(warsFilePath);
                
                // Split content into individual war blocks
                // This regex looks for patterns like "123={ ... }" at the top level
                string[] warBlocks = Regex.Split(warsContent, @"(?=^\s*\d+={)", RegexOptions.Multiline);
                
                foreach (string warBlock in warBlocks)
                {
                    if (string.IsNullOrWhiteSpace(warBlock)) continue;
                    
                    // Extract the WarID from the block header
                    Match warIdMatch = Regex.Match(warBlock, @"^\s*(\d+)={");
                    if (!warIdMatch.Success) continue;
                    
                    string warId = warIdMatch.Groups[1].Value;
                    Program.Logger.Debug($"Checking war ID: {warId}");
                    
                    // Extract attacker and defender participant lists using robust block parsing
                    var attackerParticipants = new HashSet<string>();
                    var defenderParticipants = new HashSet<string>();
                    
                    // Find attacker block content and extract participants
                    // Using the 'defender' block as a delimiter to capture the full attacker block
                    Match attackerBlockMatch = Regex.Match(warBlock, @"attacker\s*=\s*({[\s\S]*?})\s*defender\s*=\s*{", RegexOptions.Multiline);
                    if (attackerBlockMatch.Success)
                    {
                        string attackerBlockContent = attackerBlockMatch.Groups[1].Value;
                        foreach (Match charMatch in Regex.Matches(attackerBlockContent, @"character=(\d+)"))
                        {
                            attackerParticipants.Add(charMatch.Groups[1].Value);
                        }
                    }
                    
                    // Find defender block content and extract participants
                    // Using the 'start_date' as a delimiter to capture the full defender block
                    Match defenderBlockMatch = Regex.Match(warBlock, @"defender\s*=\s*({[\s\S]*?})\s*start_date\s*=", RegexOptions.Multiline);
                    if (defenderBlockMatch.Success)
                    {
                        string defenderBlockContent = defenderBlockMatch.Groups[1].Value;
                        foreach (Match charMatch in Regex.Matches(defenderBlockContent, @"character=(\d+)"))
                        {
                            defenderParticipants.Add(charMatch.Groups[1].Value);
                        }
                    }
                    
                    // Check if battle participants are on opposite sides in this war
                    bool isMatch = (attackerParticipants.Contains(leftParticipantId) && defenderParticipants.Contains(rightParticipantId)) ||
                                  (defenderParticipants.Contains(leftParticipantId) && attackerParticipants.Contains(rightParticipantId));
                    
                    if (isMatch)
                    {
                        Program.Logger.Debug($"Found matching war. WarID: {warId}");
                        return warId;
                    }
                }
                
                Program.Logger.Debug("No matching war found for the current battle participants.");
                return null;
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error finding WarID: {ex.Message}");
                return null;
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
                if (regiment?.Culture == null)
                {
                    Program.Logger.Debug($"WARNING - REGIMENT {regiment?.ID ?? "unknown"} HAS A NULL CULTURE");
                }
            }

            Program.Logger.Debug("DEFENDER  WITH NULL CULTURE REGIMENTS:\n");
            foreach (Regiment regiment in defender_armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiment => armyRegiment.Regiments))
            {
                if (regiment?.Culture == null)
                {
                    Program.Logger.Debug($"WARNING - REGIMENT {regiment?.ID ?? "unknown"} HAS A NULL CULTURE");
                }
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
                string? line;
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
            Army? searchingArmy = null;
            Knight? searchingKnight = null;

            //non-main army commander variables
            int nonMainCommander_Rank = 1;
            string nonMainCommander_Name="";
            BaseSkills? nonMainCommander_BaseSkills = null;
            Culture? nonMainCommander_Culture = null;
            Accolade? nonMainCommander_Accolade = null;
            int nonMainCommander_Prowess = 0;
            List<(int index, string key)>? nonMainCommander_Traits = null;



            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Living_Path()))
            {
                string? line;
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

                        if (isMainCommander && searchingArmy?.Commander != null)
                        {
                            searchingArmy.Commander.SetBaseSkills(new BaseSkills(baseSkills_list));
                        }
                        else if (isCommander)
                        {
                            nonMainCommander_BaseSkills = new BaseSkills(baseSkills_list);
                        }
                        else if(isKnight && searchingKnight != null)
                        {
                            searchingKnight.SetBaseSkills(new BaseSkills(baseSkills_list));
                        }
                    }
                    else if(searchStarted && line.StartsWith("\t\taccolade=")) // # ACCOLADE
                    {
                        string accoladeID = Regex.Match(line, @"\d+").Value;
                        if(isKnight && searchingKnight != null)
                        {
                            var accolade = GetAccolade(accoladeID);
                            if (accolade != null)
                            {
                                searchingKnight.IsAccolade(true, accolade);
                            }
                        }
                        else if(isMainCommander && searchingArmy?.Commander != null)
                        {
                            var accolade = GetAccolade(accoladeID);
                            if (accolade != null)
                            {
                                searchingArmy.Commander.SetAccolade(accolade);
                            }
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

                        if (isMainCommander && searchingArmy?.Commander != null)
                        {
                            searchingArmy.Commander.SetTraits(traits_list);
                        }
                        else if (isCommander)
                        {
                            nonMainCommander_Traits = traits_list;
                        }
                        else if (isKnight && searchingArmy?.Knights?.GetKnightsList() != null && searchingKnight != null)
                        {
                            searchingArmy.Knights.GetKnightsList().FirstOrDefault(x => x == searchingKnight)?.SetTraits(traits_list);
                            searchingArmy.Knights.GetKnightsList().FirstOrDefault(x => x == searchingKnight)?.SetWoundedDebuffs();
                        }
                    }
                    else if (searchStarted && line.Contains("\tculture=")) //# CULTURE
                    {
                        string culture_id = Regex.Match(line, @"\d+").Value;
                        if (isKnight && searchingKnight != null && searchingArmy?.Knights?.GetKnightsList() != null)
                        {
                            searchingArmy.Knights.GetKnightsList().Find(x => x == searchingKnight)?.ChangeCulture(new Culture(culture_id));
                            searchingArmy.Knights.SetMajorCulture();
                            if(isOwner && searchingArmy != null && searchingArmy.Owner != null) 
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }

                        else if (isMainCommander && searchingArmy?.Commander != null && searchingArmy != null && searchingArmy.Owner != null)
                        {
                            if(searchingArmy != null && searchingArmy.IsPlayer())
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
                            if (isOwner && searchingArmy != null && searchingArmy.Owner != null) 
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }
                        else if (searchingArmy != null && searchingArmy.Owner != null)
                        {
                            searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }


                    }
                    else if (searchStarted && line.Contains("\t\tdomain={")) //# TITLES
                    {
                        string firstTitleID = Regex.Match(line, @"\d+").Value;
                        if (isCommander && searchingArmy != null && nonMainCommander_BaseSkills != null && nonMainCommander_Culture != null && searchingArmy.CommanderID != null)
                        {
                            if (isOwner)
                            {
                                var owner = searchingArmy.Owner;
                                if (owner != null)
                                {
                                    owner.SetPrimaryTitle(GetTitleKey(firstTitleID));
                                }
                            }

                            var landedTitlesData = GetCommanderNobleRankAndTitleName(firstTitleID);
                            nonMainCommander_Rank = landedTitlesData.rank;
                            if (searchingArmy != null && searchingArmy.IsPlayer())
                            {
                                var commanderKnight = CK3LogData.LeftSide.GetKnights().FirstOrDefault(x => x.id == searchingArmy.CommanderID);
                                if (commanderKnight.id != null) // Reverted from commanderKnight != null
                                {
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
                            else if (searchingArmy != null && CK3LogData.RightSide.GetKnights().Exists(x => x.id == searchingArmy.CommanderID))
                            {
                                var commanderKnight = CK3LogData.RightSide.GetKnights().FirstOrDefault(x => x.id == searchingArmy.CommanderID);
                                if (commanderKnight.id != null) // Reverted from commanderKnight != null
                                {
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
                        else if (isOwner && searchingArmy != null)
                        {
                            if (searchingArmy.Owner != null)
                            {
                                searchingArmy.Owner.SetPrimaryTitle(GetTitleKey(firstTitleID));
                            }
                        }
                    }
                    else if (searchStarted && line == "}")
                    {
                        if (isCommander && searchingArmy != null && nonMainCommander_BaseSkills != null && nonMainCommander_Culture != null && searchingArmy.CommanderID != null)
                        {
                            Program.Logger.Debug($"Creating non-main commander '{nonMainCommander_Name}' ({searchingArmy.CommanderID}) for army '{searchingArmy.ID}'.");
                            searchingArmy.SetCommander(new CommanderSystem(nonMainCommander_Name, searchingArmy.CommanderID, nonMainCommander_Prowess, nonMainCommander_Rank, nonMainCommander_BaseSkills, nonMainCommander_Culture));
                            if (nonMainCommander_Traits != null)
                            {
                                searchingArmy.Commander?.SetTraits(nonMainCommander_Traits);
                            }
                            if (nonMainCommander_Accolade != null)
                            {
                                searchingArmy.Commander?.SetAccolade(nonMainCommander_Accolade);
                            }
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

        private static void SetMAARegimentCultures(List<Army> armies)
        {
            Program.Logger.Debug("Setting MAA regiment cultures...");
            foreach (var army in armies)
            {
                if (army == null || army.Owner == null || army.Owner.GetCulture() == null) continue;
                
                string ownerCultureId = army.Owner.GetCulture().ID;
                if (string.IsNullOrEmpty(ownerCultureId)) continue;

                foreach (var armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment == null || armyRegiment.Type != RegimentType.MenAtArms) continue;

                    foreach (var regiment in armyRegiment.Regiments)
                    {
                        if (regiment == null || (!string.IsNullOrEmpty(regiment.Culture?.ID))) continue;
                        
                        regiment.SetCulture(ownerCultureId);
                        Program.Logger.Debug($"Set culture '{ownerCultureId}' for MAA regiment '{regiment.ID}' in army '{army.ID}'");
                    }
                }
            }
            Program.Logger.Debug("Finished setting MAA regiment cultures.");
        }

        static Accolade? GetAccolade(string accoladeID)
        {
            Program.Logger.Debug($"Searching for accolade with ID: {accoladeID}");
            bool searchStarted = false;
            string primaryAttribute = "";
            string secundaryAttribute = "";
            string glory = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Accolades()))
            {
                string? line;
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
                string? line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (line == $"{title_id}={{")
                    {
                        searchStarted = true;
                    }
                    else if (searchStarted && line.StartsWith("\tkey=")) //# KEY
                    {
                        titleKey = Regex.Match(line, "=(.+)").Groups[1].Value;
                        Program.Logger.Debug($"Found title key '{titleKey}' for title ID '{title_id}'.");
                        return titleKey;
                    }

                }
                Program.Logger.Debug($"Title key for title ID '{title_id}' not found.");
                return titleKey;
            }
        }

        private static (List<string> besiegerIds, List<string> reliefIds, string? combatBlock) FindSiegeCombatBlockAndExtractArmies()
        {
            Program.Logger.Debug("Searching for siege combat block in Combats.txt...");
            try
            {
                string combatsContent = File.ReadAllText(Writter.DataFilesPaths.Combats_Path());
                string[] combatBlocks = Regex.Split(combatsContent, @"(?=^\s*\d+={)", RegexOptions.Multiline);

                string leftParticipantId = CK3LogData.LeftSide.GetMainParticipant().id;
                string rightParticipantId = CK3LogData.RightSide.GetMainParticipant().id;

                foreach (string block in combatBlocks)
                {
                    if (string.IsNullOrWhiteSpace(block)) continue;
                    if (!block.Contains($"province={BattleResult.ProvinceID}")) continue;

                    var blockAttackerChars = new HashSet<string>();
                    var blockDefenderChars = new HashSet<string>();

                    Match attackerBlockMatch = Regex.Match(block, @"attacker\s*=\s*({[\s\S]*?})\s*defender\s*=\s*{", RegexOptions.Multiline);
                    if (attackerBlockMatch.Success)
                    {
                        foreach (Match charMatch in Regex.Matches(attackerBlockMatch.Groups[1].Value, @"character=(\d+)"))
                        {
                            blockAttackerChars.Add(charMatch.Groups[1].Value);
                        }
                    }

                    Match defenderBlockMatch = Regex.Match(block, @"defender\s*=\s*({[\s\S]*?})\s*start_date\s*=", RegexOptions.Multiline);
                    if (defenderBlockMatch.Success)
                    {
                        foreach (Match charMatch in Regex.Matches(defenderBlockMatch.Groups[1].Value, @"character=(\d+)"))
                        {
                            blockDefenderChars.Add(charMatch.Groups[1].Value);
                        }
                    }

                    bool isMatch = (blockAttackerChars.Contains(leftParticipantId) && blockDefenderChars.Contains(rightParticipantId)) ||
                                   (blockDefenderChars.Contains(leftParticipantId) && blockAttackerChars.Contains(rightParticipantId));

                    if (isMatch)
                    {
                        Program.Logger.Debug("Found matching combat block based on participants and province.");
                        var attackerIds = new List<string>();
                        var defenderIds = new List<string>();

                        if (attackerBlockMatch.Success)
                        {
                            Match armyMatch = Regex.Match(attackerBlockMatch.Groups[1].Value, @"armies={\s*([\d\s]+)\s*}");
                            if (armyMatch.Success)
                            {
                                attackerIds.AddRange(armyMatch.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                        }
                        if (defenderBlockMatch.Success)
                        {
                            Match armyMatch = Regex.Match(defenderBlockMatch.Groups[1].Value, @"armies={\s*([\d\s]+)\s*}");
                            if (armyMatch.Success)
                            {
                                defenderIds.AddRange(armyMatch.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                        }

                        List<string> besiegerIds;
                        List<string> reliefIds;

                        if (blockAttackerChars.Contains(leftParticipantId))
                        {
                            besiegerIds = attackerIds;
                            reliefIds = defenderIds;
                        }
                        else
                        {
                            besiegerIds = defenderIds;
                            reliefIds = attackerIds;
                        }

                        Program.Logger.Debug($"Extracted Besieger IDs: [{string.Join(", ", besiegerIds)}], Relief IDs: [{string.Join(", ", reliefIds)}]");
                        return (besiegerIds, reliefIds, block);
                    }
                }

                Program.Logger.Debug("No matching siege combat block found.");
                return (new List<string>(), new List<string>(), null);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error reading or parsing Combats.txt for siege battle: {ex.Message}");
                return (new List<string>(), new List<string>(), null);
            }
        }

        static void ReadOriginsKeys()
        {
            Program.Logger.Debug("Reading origins keys from landed titles...");
            bool searchStarted = false;
            string originKey = "";
            string id = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.LandedTitles()))
            {
                string? line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (!line.StartsWith("\t") && line != "}")
                    {
                        id = Regex.Match(line, @"\d+").Value;
                        searchStarted = true;
                    }
                    else if (searchStarted && line.StartsWith("\tkey=")) //# KEY
                    {
                        originKey = Regex.Match(line, "=(.+)").Groups[1].Value;
                        SetRegimentsOriginsKeys(id,originKey);
                    }
                    else if (searchStarted && line == "}")
                    {
                        searchStarted= false;
                        originKey = "";
                        id = "";
                    }

                }
            }
            Program.Logger.Debug("Finished reading origins keys.");
        }

        private static void CorrectRegimentSoldiers()
        {
            Program.Logger.Debug("Correcting regiment soldiers to use starting values from combat data...");
            
            // Process attacker armies
            foreach (var army in attacker_armies)
            {
                if (army.ArmyRegiments == null) continue;
                
                foreach (var armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment == null || armyRegiment.Regiments == null || !armyRegiment.Regiments.Any()) continue;
                    
                    // Get the total soldiers from the army regiment's starting number
                    string? totalSoldiersStr = armyRegiment.StartingNum.ToString();
                    if (string.IsNullOrEmpty(totalSoldiersStr) || totalSoldiersStr == "0")
                    {
                        totalSoldiersStr = armyRegiment.CurrentNum.ToString();
                    }
                    
                    if (string.IsNullOrEmpty(totalSoldiersStr) || totalSoldiersStr == "0")
                    {
                        Program.Logger.Debug($"Skipping soldier correction for ArmyRegiment {armyRegiment.ID} in army {army.ID}: no valid combat or current count found. Using value from Regiments.txt.");
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(totalSoldiersStr) || totalSoldiersStr == "0")
                    {
                        Program.Logger.Debug($"Skipping soldier correction for ArmyRegiment {armyRegiment.ID} in army {army.ID}: no valid combat or current count found. Using value from Regiments.txt.");
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(totalSoldiersStr))
                    {
                        Program.Logger.Debug($"No soldier count found for ArmyRegiment {armyRegiment.ID} in army {army.ID}");
                        continue;
                    }
                    
                    int totalSoldiers;
                    if (!int.TryParse(totalSoldiersStr, out totalSoldiers))
                    {
                        Program.Logger.Debug($"Failed to parse soldier count '{totalSoldiersStr}' for ArmyRegiment {armyRegiment.ID}");
                        continue;
                    }
                    
                    // Distribute the total soldiers to the first regiment and zero out the rest
                    for (int i = 0; i < armyRegiment.Regiments.Count; i++)
                    {
                        var regiment = armyRegiment.Regiments[i];
                        if (regiment == null) continue;
                        
                        if (i == 0)
                        {
                            // Assign all soldiers to the first regiment
                            regiment.SetSoldiers(totalSoldiers.ToString());
                            Program.Logger.Debug($"Set {totalSoldiers} soldiers for first regiment {regiment.ID} in ArmyRegiment {armyRegiment.ID}");
                        }
                        else
                        {
                            // Zero out the rest
                            regiment.SetSoldiers("0");
                            Program.Logger.Debug($"Zeroed soldiers for additional regiment {regiment.ID} in ArmyRegiment {armyRegiment.ID}");
                        }
                    }
                }
            }
            
            // Process defender armies
            foreach (var army in defender_armies)
            {
                if (army.ArmyRegiments == null) continue;
                
                foreach (var armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment == null || armyRegiment.Regiments == null || !armyRegiment.Regiments.Any()) continue;
                    
                    // Get the total soldiers from the army regiment's starting number
                    string? totalSoldiersStr = armyRegiment.StartingNum.ToString();
                    if (string.IsNullOrEmpty(totalSoldiersStr) || totalSoldiersStr == "0")
                    {
                        totalSoldiersStr = armyRegiment.CurrentNum.ToString();
                    }
                    
                    if (string.IsNullOrEmpty(totalSoldiersStr))
                    {
                        Program.Logger.Debug($"No soldier count found for ArmyRegiment {armyRegiment.ID} in army {army.ID}");
                        continue;
                    }
                    
                    int totalSoldiers;
                    if (!int.TryParse(totalSoldiersStr, out totalSoldiers))
                    {
                        Program.Logger.Debug($"Failed to parse soldier count '{totalSoldiersStr}' for ArmyRegiment {armyRegiment.ID}");
                        continue;
                    }
                    
                    // Distribute the total soldiers to the first regiment and zero out the rest
                    for (int i = 0; i < armyRegiment.Regiments.Count; i++)
                    {
                        var regiment = armyRegiment.Regiments[i];
                        if (regiment == null) continue;
                        
                        if (i == 0)
                        {
                            // Assign all soldiers to the first regiment
                            regiment.SetSoldiers(totalSoldiers.ToString());
                            Program.Logger.Debug($"Set {totalSoldiers} soldiers for first regiment {regiment.ID} in ArmyRegiment {armyRegiment.ID}");
                        }
                        else
                        {
                            // Zero out the rest
                            regiment.SetSoldiers("0");
                            Program.Logger.Debug($"Zeroed soldiers for additional regiment {regiment.ID} in ArmyRegiment {armyRegiment.ID}");
                        }
                    }
                }
            }
            
            Program.Logger.Debug("Finished correcting regiment soldiers.");
        }

        static void SetRegimentsOriginsKeys(string title_id, string originKey)
        {
            // Refactored to use explicit null checks and nested loops for clarity and compiler verification
            foreach (Army? army in attacker_armies)
            {
                if (army == null) continue;
                foreach (ArmyRegiment? armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment == null) continue;
                    foreach (Regiment? regiment in armyRegiment.Regiments)
                    {
                        if (regiment == null) continue;
                        if (string.IsNullOrEmpty(regiment.OwningTitle) || !string.IsNullOrEmpty(regiment.OriginKey)) continue;
                        if (regiment.OwningTitle == title_id)
                        {
                            regiment.SetOriginKey(originKey);
                        }
                    }
                }
            }

            foreach (Army? army in defender_armies)
            {
                if (army == null) continue;
                foreach (ArmyRegiment? armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment == null) continue;
                    foreach (Regiment? regiment in armyRegiment.Regiments)
                    {
                        if (regiment == null) continue;
                        if (string.IsNullOrEmpty(regiment.OwningTitle) || !string.IsNullOrEmpty(regiment.OriginKey)) continue;
                        if (regiment.OwningTitle == title_id)
                        {
                            regiment.SetOriginKey(originKey);
                        }
                    }
                }
            }
        }

        static (int rank, string titleName) GetCommanderNobleRankAndTitleName(string commanderTitleID)
        {
            Program.Logger.Debug($"Getting commander rank and title name for title ID: {commanderTitleID}");
            bool searchStarted = false;
            int rankInt = 1; string titleName = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.LandedTitles()))
            {
                string? line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if(line == $"{commanderTitleID}={{")
                    {
                        searchStarted = true;
                    }
                    else if(searchStarted && line.StartsWith("\tkey=")) //# KEY
                    {
                        string title_key = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                        
                        if(title_key.StartsWith("b_"))
                        {
                            rankInt = 2;
                        }
                        else if (title_key.StartsWith("c_"))
                        {
                            rankInt = 3;
                        }
                        else if (title_key.StartsWith("d_"))
                        {
                            rankInt = 4;
                        }
                        else if (title_key.StartsWith("k_"))
                        {
                            rankInt = 5;
                        }
                        else if (title_key.StartsWith("e_"))
                        {
                            rankInt = 6;
                        }
                    }
                    else if(searchStarted && line.StartsWith("\tname="))
                    {
                        string name = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                        titleName = name;

                        Program.Logger.Debug($"Found rank '{rankInt}' and title name '{titleName}' for title ID '{commanderTitleID}'.");
                        return (rankInt, titleName);
                    }
                }
                Program.Logger.Debug($"Could not find rank/title for title ID '{commanderTitleID}'. Returning default.");
                return (1, string.Empty);
            }
        }

        static void RemoveCommandersAsKnights()
        {
            Program.Logger.Debug("Removing commanders from knight regiments...");
            foreach(Army army in attacker_armies)
            {
                ArmyRegiment? commanderRegiment = army.ArmyRegiments.FirstOrDefault(x => x.MAA_Name == army.CommanderID);
                if(commanderRegiment != null)
                {
                    Program.Logger.Debug($"Removing commander '{army.CommanderID}' from knight regiments in attacker army '{army.ID}'.");
                    army.ArmyRegiments.Remove(commanderRegiment);
                }
            }
            foreach (Army army in defender_armies)
            {
                ArmyRegiment? commanderRegiment = army.ArmyRegiments.FirstOrDefault(x => x.MAA_Name == army.CommanderID);
                if(commanderRegiment != null)
                {
                    Program.Logger.Debug($"Removing commander '{army.CommanderID}' from knight regiments in defender army '{army.ID}'.");
                    army.ArmyRegiments.Remove(commanderRegiment);
                }
            }
            Program.Logger.Debug("Finished removing commanders from knight regiments.");
        }

        
        public static List<Army>? GetSideArmies(string side, List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug($"Getting armies for side: {side}");
            List<Army>? left_side = null, right_side = null;
            foreach (var army in attacker_armies)
            {
                if (army.IsPlayer())
                {
                    left_side = attacker_armies;
                    break;
                }
                else if (army.IsEnemy())
                {
                    right_side = attacker_armies;
                    break;
                }
            }
            foreach (var army in defender_armies)
            {
                if (army.IsPlayer())
                {
                    left_side = defender_armies;
                    break;
                }
                else if (army.IsEnemy())
                {
                    right_side = defender_armies;
                    break;
                }
            }

            // Infer missing side if one is known
            if (left_side != null && right_side == null)
            {
                right_side = (left_side == attacker_armies) ? defender_armies : attacker_armies;
            }
            else if (right_side != null && left_side == null)
            {
                left_side = (right_side == attacker_armies) ? defender_armies : attacker_armies; // Corrected line
            }

            if (side == "left")
            {
                Program.Logger.Debug($"Returning {left_side?.Count ?? 0} armies for left side.");
                return left_side;
            }
            else
            {
                Program.Logger.Debug($"Returning {right_side?.Count ?? 0} armies for right side.");
                return right_side;
            }
        }

        static void CreateMainCommanders()
        {
            Program.Logger.Debug("Creating main commanders...");
            var left_side_armies = GetSideArmies("left", attacker_armies, defender_armies);
            var right_side_armies = GetSideArmies("right", attacker_armies, defender_armies);

            if(left_side_armies != null)
            {
                var left_main_commander_data = CK3LogData.LeftSide.GetCommander();
                Program.Logger.Debug($"Setting left side main commander: {left_main_commander_data.name} ({left_main_commander_data.id})");
                var mainArmy = left_side_armies.FirstOrDefault(x => x.isMainArmy && !x.IsGarrison());
                if (mainArmy != null)
                {
                    mainArmy.SetCommander(new CommanderSystem(left_main_commander_data.name, left_main_commander_data.id, left_main_commander_data.prowess, left_main_commander_data.martial, left_main_commander_data.rank, true));
                }
            }

            if(right_side_armies != null)
            {
                var right_main_commander_data = CK3LogData.RightSide.GetCommander();
                Program.Logger.Debug($"Setting right side main commander: {right_main_commander_data.name} ({right_main_commander_data.id})");
                var mainArmy = right_side_armies.FirstOrDefault(x => x.isMainArmy && !x.IsGarrison());
                if (mainArmy != null)
                {
                    mainArmy.SetCommander(new CommanderSystem(right_main_commander_data.name, right_main_commander_data.id, right_main_commander_data.prowess, right_main_commander_data.martial, right_main_commander_data.rank, true));
                }
            }
            Program.Logger.Debug("Finished creating main commanders.");
        }
        static void CreateKnights()
        {
            Program.Logger.Debug("Creating knights...");
            RemoveCommandersAsKnights();

            var left_side_armies = GetSideArmies("left", attacker_armies, defender_armies);
            var right_side_armies = GetSideArmies("right", attacker_armies, defender_armies);

            var KnightsList = new List<Knight>();
            Program.Logger.Debug("Creating knights for left side armies...");
            if(left_side_armies != null)
            {
                for (int x = 0; x < left_side_armies.Count; x++)
                {
                    var army = left_side_armies[x];
                    for (int y = 0; y < army.ArmyRegiments.Count; y++)
                    {
                        var regiment = army.ArmyRegiments[y];
                        if (regiment.Type == RegimentType.Knight)
                        {
                            var leftKnights = CK3LogData.LeftSide.GetKnights();
                            if (leftKnights != null && leftKnights.Count > 0)
                            {
                                for (int i = 0; i < leftKnights.Count; i++)
                                {
                                    string id = leftKnights[i].id;
                                    if (id == army.CommanderID) continue;
                                    if (id == regiment.MAA_Name)
                                    {
                                        int prowess = Int32.Parse(leftKnights[i].prowess);
                                        string name = leftKnights[i].name;

                                        KnightsList.Add(new Knight(name, regiment.MAA_Name, null!, prowess, 4));
                                    }
                                }
                            }
                        }

                    }

                    int leftEffectivenss = 0;
                    var leftSideKnights = CK3LogData.LeftSide.GetKnights();
                    if (leftSideKnights != null && leftSideKnights.Count > 0)
                    {
                        leftEffectivenss = leftSideKnights[0].effectiveness;
                    }

                    Program.Logger.Debug($"Creating KnightSystem for left side army {left_side_armies[x].ID} with {KnightsList.Count} knights.");
                    KnightSystem leftSide = new KnightSystem(KnightsList, leftEffectivenss);
                    if (left_side_armies == attacker_armies)
                    {
                        attacker_armies[x].SetKnights(leftSide);
                    }
                    else if (left_side_armies == defender_armies)
                    {
                        defender_armies[x].SetKnights(leftSide);
                    }
                    KnightsList = new List<Knight>();

                }
            }


            KnightsList = new List<Knight>();
            Program.Logger.Debug("Creating knights for right side armies...");
            if(right_side_armies != null)
            {
                for (int x = 0; x < right_side_armies.Count; x++)
                {
                    var army = right_side_armies[x];
                    for (int y = 0; y < army.ArmyRegiments.Count; y++)
                    {
                        var regiment = army.ArmyRegiments[y];
                        if (regiment.Type == RegimentType.Knight)
                        {
                            var rightKnights = CK3LogData.RightSide.GetKnights();
                            if (rightKnights != null && rightKnights.Count > 0)
                            {
                                for (int i = 0; i < rightKnights.Count; i++)
                                {
                                    string id = rightKnights[i].id;
                                    if (id == army.CommanderID) continue;
                                    if (id == regiment.MAA_Name)
                                    {
                                        int prowess = Int32.Parse(rightKnights[i].prowess);
                                        string name = rightKnights[i].name;

                                        KnightsList.Add(new Knight(name, regiment.MAA_Name, null!, prowess, 4));
                                    }
                                }
                            }
                        }

                    }

                    int rightEffectivenss = 0;
                    var rightSideKnights = CK3LogData.RightSide.GetKnights();
                    if (rightSideKnights != null && rightSideKnights.Count > 0)
                    {
                        rightEffectivenss = rightSideKnights[0].effectiveness;
                    }

                    Program.Logger.Debug($"Creating KnightSystem for right side army {right_side_armies[x].ID} with {KnightsList.Count} knights.");
                    KnightSystem rightSide = new KnightSystem(KnightsList, rightEffectivenss);

                    if (right_side_armies == attacker_armies)
                    {
                        attacker_armies[x].SetKnights(rightSide);
                    }
                    else if (right_side_armies == defender_armies)
                    {
                        defender_armies[x].SetKnights(rightSide);
                    }
                    KnightsList = new List<Knight>();
                }
            }
            Program.Logger.Debug("Finished creating knights.");
        }

        private static void CreateUnits()
        {
            Program.Logger.Debug("Creating and organizing units from regiments...");
            Armies_Functions.CreateUnits(attacker_armies);
            Armies_Functions.CreateUnits(defender_armies);
            Program.Logger.Debug("Finished creating units.");
        }

        private static void ReadMercenaries()
        {
            Program.Logger.Debug("Reading mercenaries data...");
            bool isSearchStarted = false;
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Mercenaries_Path()))
            {
                string culture_id = "";
                List<string> regiments_ids = new List<string>();

                string? line;
                while((line = sr.ReadLine()) != null)
                {

                    //Mercenary Company ID
                    if(Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStarted)
                    {
                        isSearchStarted = true;
                        continue;
                    }
                    else if(line == "\t\t}")
                    {
                        var attacker_mercenaries_regiments = attacker_armies.SelectMany(army => army.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments))
                                                            .Where(regiment => regiment.isMercenary())
                                                            .ToList();

                        
                        var defender_mercenaries_regiments = defender_armies.SelectMany(army => army.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments))
                                                            .Where(regiment => regiment.isMercenary())
                                                            .ToList();

                        //break loop if all cultures are set
                        int attackerNumOfNotSetCultures = attacker_mercenaries_regiments.Count(x => x.Culture == null);
                        int defenderNumOfNotSetCultures = defender_mercenaries_regiments.Count(x => x.Culture == null);
                        if (attackerNumOfNotSetCultures == 0 && defenderNumOfNotSetCultures == 0)
                            break;


                        for (int i = 0; i < attacker_armies.Count; i++)
                        {
                            //Army Regiments
                            for (int x = 0; x < attacker_armies[i].ArmyRegiments.Count; x++)
                            {
                                //Regiments
                                if(attacker_armies[i].ArmyRegiments[x].Regiments != null)
                                {
                                    for (int y = 0; y < attacker_armies[i].ArmyRegiments[x].Regiments.Count; y++)
                                    {
                                        var regiment = attacker_armies[i].ArmyRegiments[x].Regiments[y];

                                        foreach (var t in regiments_ids)
                                        {

                                            if (t == regiment.ID && (regiment.isMercenary() || regiment.Culture is null))
                                            {
                                                attacker_armies[i].ArmyRegiments[x].Regiments[y].SetCulture(culture_id);
                                                break;
                                            }
                                        }

                                    }
                                }

                            }
                        }

                        for (int i = 0; i < defender_armies.Count; i++)
                        {
                            //Army Regiments
                            for (int x = 0; x < defender_armies[i].ArmyRegiments.Count; x++)
                            {
                                //Regiments
                                if(defender_armies[i].ArmyRegiments[x].Regiments != null)
                                {
                                    for (int y = 0; y < defender_armies[i].ArmyRegiments[x].Regiments.Count; y++)
                                    {
                                        var regiment = defender_armies[i].ArmyRegiments[x].Regiments[y];
                                        foreach (var t in regiments_ids)
                                        {
                                            if (t == regiment.ID && (regiment.isMercenary() || regiment.Culture is null))
                                            {
                                                defender_armies[i].ArmyRegiments[x].Regiments[y].SetCulture(culture_id);
                                                break;
                                            }
                                        }
                                    }
                                }

                            }
                        }

                    }
                    else if (isSearchStarted)
                    {
                        if (line.Contains("\t\tculture="))
                        {
                            culture_id = Regex.Match(line, @"\d+").Value;
                        }
                        else if (line.Contains("\t\tregiments={ "))
                        {
                            regiments_ids = Regex.Matches(line, @"\d+").Cast<Match>().Select(match => match.Value).ToList();
                        }
                    }

                }
            }
            Program.Logger.Debug("Finished reading mercenaries data.");

            //HOLY ORDER REGIMENTS
            
            foreach(var army in attacker_armies)
            {
                var attacker_holyorder_regiments = army.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments)
                                              .Where(regiment => regiment.isMercenary() && regiment.Culture == null);
                foreach (var holy_regiment in attacker_holyorder_regiments)
                {
                    if (army.Owner?.GetCulture() != null) // Added null check for army.Owner.GetCulture()
                    {
                        holy_regiment.SetCulture(army.Owner.GetCulture().ID);
                    }
                }
            }

            foreach (var army in defender_armies)
            {
                var defender_holyorder_regiments = army.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments)
                                              .Where(regiment => regiment.isMercenary() && regiment.Culture == null);
                foreach (var holy_regiment in defender_holyorder_regiments)
                {
                    if (army.Owner?.GetCulture() != null) // Added null check for army.Owner.GetCulture()
                    {
                        holy_regiment.SetCulture(army.Owner.GetCulture().ID);
                    }
                }
            }
        }

        private static void ReadCultureManager()
        {
            Program.Logger.Debug("Reading culture manager data...");
            Armies_Functions.ReadArmiesCultures(attacker_armies);
            Armies_Functions.ReadArmiesCultures(defender_armies);
            Program.Logger.Debug("Finished reading culture manager data.");
        }

        private static void ReadCountiesManager()
        {

            Program.Logger.Debug("Reading counties manager data...");
            List<(string county_key, string culture_id)> FoundCounties = new List<(string county_key, string culture_id)>();

            bool isSearchStared = false;
            string county_key = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Counties_Path()))
            {
                while (true)
                {
                    string? line = sr.ReadLine();
                    if (line == null) break;

                    //County Line
                    if(Regex.IsMatch(line,@"\t\t\w+={") && !isSearchStared)
                    {
                        county_key = Regex.Match(line, @"\t\t(\w+)={").Groups[1].Value;

                        isSearchStared =  Armies_Functions.SearchCounty(county_key, attacker_armies);
                        if (!isSearchStared)
                        {
                            isSearchStared = Armies_Functions.SearchCounty(county_key, defender_armies);
                        }
                        
                    }

                    //Culture ID
                    else if(isSearchStared && line.Contains("\t\t\tculture=")) 
                    {
                        string culture_id = Regex.Match(line, @"\t\t\tculture=(\d+)").Groups[1].Value;
                        FoundCounties.Add((county_key, culture_id));                        
                    }

                    // County End Line
                    else if(isSearchStared && line == "\t\t}")
                    {
                        isSearchStared = false;
                    }


                }

                
                //Populate regiments with culture id's
                Armies_Functions.PopulateRegimentsWithCultures(FoundCounties, attacker_armies);
                Armies_Functions.PopulateRegimentsWithCultures(FoundCounties, defender_armies);
 
            }
            Program.Logger.Debug("Finished reading counties manager data.");
        }
        


        private static void ReadRegiments()
        {
            bool isSearchStarted = false;
            List<Regiment> foundRegiments = new List<Regiment>();

            int index = -1;

            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Regiments_Path()))
            {
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    // Regiment ID Line
                    if (Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStarted)
                    {
                        string regiment_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;

                        // Find ALL regiments that match this ID across both attacker and defender armies
                        foundRegiments = attacker_armies.SelectMany(army => army.ArmyRegiments)
                                             .SelectMany(armyRegiment => armyRegiment.Regiments)
                                             .Where(reg => reg.ID == regiment_id)
                                             .ToList();
                        
                        if (foundRegiments.Count == 0)
                        {
                            foundRegiments = defender_armies.SelectMany(army => army.ArmyRegiments)
                                                 .SelectMany(armyRegiment => armyRegiment.Regiments)
                                                 .Where(reg => reg.ID == regiment_id)
                                                 .ToList();
                        }

                        if (foundRegiments.Count > 0)
                        {
                            isSearchStarted = true;
                        }
                    }

                    // Index Counter
                    else if (line == "\t\t\t\t{" && isSearchStarted)
                    {
                        index++;
                    }

                    // isMercenary 
                    else if (isSearchStarted && line.Contains("\t\t\tsource=hired"))
                    {
                        foreach (var reg in foundRegiments)
                        {
                            reg.isMercenary(true);
                        }
                    }

                    // isGarrison 
                    else if (isSearchStarted && line.Contains("\t\t\tsource=garrison"))
                    {
                        foreach (var reg in foundRegiments)
                        {
                            reg.IsGarrison(true);
                        }
                    }
                    // Origin 
                    else if (isSearchStarted && line.Contains("\t\t\torigin="))
                    {
                        string origin = Regex.Match(line, @"\d+").Value;
                        foreach (var reg in foundRegiments)
                        {
                            reg.SetOrigin(origin);
                        }
                    }
                    // Owner 
                    else if (isSearchStarted && line.Contains("\t\t\towner="))
                    {
                        string owner = Regex.Match(line, @"\d+").Value;
                        foreach (var reg in foundRegiments)
                        {
                            reg.SetOwner(owner);
                        }
                    }
                    else if(isSearchStarted && line.Contains("\t\t\towning_title="))
                    {
                        string owiningTitle = Regex.Match(line, @"\d+").Value;
                        foreach (var reg in foundRegiments)
                        {
                            reg.SetOwningTitle(owiningTitle);
                        }
                    }
                    // Max
                    else if (isSearchStarted && line.Contains("\t\t\t\t\tmax="))
                    {
                        string max = Regex.Match(line, @"\d+").Value;
                        foreach (var reg in foundRegiments)
                        {
                            reg.SetMax(max);
                        }
                    }

                    // Soldiers
                    else if (isSearchStarted && (line.Contains("\t\t\t\t\tcurrent=") || line.Contains("\t\t\tsize=")))
                    {
                        string current = Regex.Match(line, @"\d+").Value;
                        
                        // Process each found regiment to check if this chunk applies to it
                        foreach (var reg in foundRegiments)
                        {
                            // Calculate reg_chunk_index for this specific regiment
                            int reg_chunk_index = 0;
                            if (!string.IsNullOrEmpty(reg.Index))
                            {
                                reg_chunk_index = Int32.Parse(reg.Index);
                            }
                            
                            // Only set soldiers if this chunk index matches the regiment's required index
                            if (index == reg_chunk_index || (index == -1 && reg_chunk_index == 0))
                            {
                                reg.SetSoldiers(current);
                            }
                        }

                        if(line.Contains("\t\t\tsize="))
                        {
                            foreach (var reg in foundRegiments)
                            {
                                reg.SetMax(current);
                            }
                        }
                    }

                    //Regiment End Line
                    else if (isSearchStarted && line == "\t\t}")
                    {
                        isSearchStarted = false;
                        index = -1;
                        foundRegiments.Clear();
                    }
                }
            }
            RemoveGarrisonRegiments(attacker_armies, defender_armies);
        }

        static void RemoveGarrisonRegiments(List<Army> attacker_armies, List<Army> defender_armies)
        {
            for (int i = 0; i < attacker_armies.Count; i++)
            {
                attacker_armies[i].RemoveGarrisonRegiments();
            }
            for (int i = 0; i < defender_armies.Count; i++)
            {
                defender_armies[i].RemoveGarrisonRegiments();
            }
        }


        private static void ReadArmiesUnits()
        {
            bool isInsideBlock = false;
            string armyId = "";
            string ownerId = "";

            using (StreamReader SR = new StreamReader(Writter.DataFilesPaths.Units_Path()))
            {
                string? line;
                while ((line = SR.ReadLine()) != null)
                {
                    if (Regex.IsMatch(line, @"\t\d+={") && !isInsideBlock)
                    {
                        isInsideBlock = true;
                        armyId = "";
                        ownerId = "";
                    }
                    else if (isInsideBlock)
                    {
                        if (line.Contains("\t\towner="))
                        {
                            ownerId = Regex.Match(line, @"\d+").Value;
                        }
                        else if (line.Contains("\t\tarmy="))
                        {
                            armyId = Regex.Match(line, @"\d+").Value;
                        }
                        else if (line == "\t}")
                        {
                            if (!string.IsNullOrEmpty(armyId) && !string.IsNullOrEmpty(ownerId))
                            {
                                var (searchHasStarted, army) = Armies_Functions.SearchUnit(armyId, attacker_armies);
                                if (!searchHasStarted)
                                {
                                    (searchHasStarted, army) = Armies_Functions.SearchUnit(armyId, defender_armies);
                                }

                                if (searchHasStarted && army != null)
                                {
                                    army.SetOwner(ownerId);
                                }
                            }
                            isInsideBlock = false;
                            armyId = "";
                            ownerId = "";
                        }
                    }
                }
            }
        }

        
        private static void ReadArmyRegiments()
        {
            List<Regiment> found_regiments = new List<Regiment>();

            bool isSearchStarted = false;
            ArmyRegiment? armyRegiment = null;

            string regiment_id = "";
            string index = "";

            bool isNameSet = false;
            bool isReadingChunks = false;

            using (StreamReader SR = new StreamReader(Writter.DataFilesPaths.ArmyRegiments_Path()))
            {
                while (true)
                {
                    string? line = SR.ReadLine();

                    if (line == null) break;

                    // Army Regiment ID Line
                    if (Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStarted)
                    {
                        string army_regiment_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;
                        var searchingData = Armies_Functions.SearchArmyRegiments(army_regiment_id, attacker_armies);
                        if(searchingData.searchHasStarted)
                        {
                            isSearchStarted = true;
                            armyRegiment = searchingData.regiment;
                        }
                        else
                        {
                            searchingData = Armies_Functions.SearchArmyRegiments(army_regiment_id, defender_armies);
                            if(searchingData.searchHasStarted)
                            {
                                isSearchStarted = true;
                                armyRegiment = searchingData.regiment;
                            }
                        }
                    }

                    //Regiment ID
                    if(isSearchStarted && line.Contains("\t\t\t\t\tregiment="))
                    {
                        if(isNameSet == false)
                        {
                            if(armyRegiment != null) armyRegiment.SetType(RegimentType.Levy);
                        }

                        regiment_id = Regex.Match(line, @"(\d+)").Groups[1].Value;                    

                    }

                    else if(isSearchStarted && line.Contains("\t\t\tchunks={"))
                    {
                        isReadingChunks = true;
                    }

                    //Regiment Index
                    else if (isSearchStarted && line.Contains("\t\t\t\t\tindex="))
                    {
                        index = Regex.Match(line, @"(\d+)").Groups[1].Value;
                    }

                    //Add Found Regiment
                    else if (isSearchStarted && line == "\t\t\t\t}" && isReadingChunks)
                    {
                        Regiment regiment = new Regiment(regiment_id, index);
                        found_regiments.Add(regiment);
                    }
                    else if (isSearchStarted && line == " }" && isReadingChunks)
                    {
                        isReadingChunks = false;
                    }

                    //Current Number
                    else if(isSearchStarted && line.Contains("\t\t\t\tcurrent="))
                    {
                        string currentNum = Regex.Match(line, @"\d+").Value;
                        if (armyRegiment != null) armyRegiment.CurrentNum = int.Parse(currentNum);
                    }

                    //Max
                    else if (isSearchStarted && line.Contains("\t\t\t\tmax="))
                    {
                        string max = Regex.Match(line, @"\d+").Value;
                        armyRegiment?.SetMax(max);
                    }

                    //Men At Arms
                    else if (isSearchStarted && line.Contains("\t\t\ttype="))
                    {
                        string type = Regex.Match(line, "type=(.+)").Groups[1].Value;
                        armyRegiment?.SetType(RegimentType.MenAtArms, type);
                        isNameSet = true;
                    }

                    //Knight
                    else if (isSearchStarted && line.Contains("\t\t\tknight="))
                    {
                        string character_id = Regex.Match(line, @"knight=(\d+)").Groups[1].Value;
                        armyRegiment?.SetType(RegimentType.Knight, character_id);
                        isNameSet = true;
                    }

                    
                    //Levies
                    else if (isSearchStarted && line == "\t\t\t\tlevies={")
                    {
                        armyRegiment?.SetType(RegimentType.Levy);
                        isNameSet = true;
                    }

                    // Army Regiment End Line
                    else if (line == "\t\t}" && isSearchStarted)
                    {
                        //Debug purposes, remove later...
                        if(found_regiments != null)
                        {
                            armyRegiment?.SetRegiments(found_regiments);
                        }

                        found_regiments = new List<Regiment>();
                        regiment_id = "";
                        index = "";
                        isSearchStarted = false;
                        isNameSet= false;
                        isReadingChunks = false;
                    }

                }
            }

            ClearNullArmyRegiments();
        }


        private static void ReadArmiesData()
        {
            bool isSearchStarted = false;
            bool isDefender = false, isAttacker = false;
            int index = 0;
            using (StreamReader SR = new StreamReader(Writter.DataFilesPaths.Armies_Path()))
            {
                while(true)
                {
                    string? line = SR.ReadLine();
                    if (line == null) break;

                    // Army ID Line
                    if(Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStarted)
                    {
                        // Check if it's a battle army

                        string army_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;
                        for (int i = 0; i < attacker_armies.Count; i++)
                        {
                            if (attacker_armies[i].ID == army_id)
                            {
                                index = i;
                                isAttacker = true;
                                isDefender = false;
                                isSearchStarted = true;
                                break;
                            }

                        }
                        if(!isSearchStarted)
                        {
                            for (int i = 0; i < defender_armies.Count; i++)
                            {
                                if (defender_armies[i].ID == army_id)
                                {
                                    index = i;
                                    isDefender = true;
                                    isAttacker = false;
                                    isSearchStarted = true;
                                    break;
                                }
                            }
                        }

                    }

                    // Regiments ID's Line
                    if (isSearchStarted && line.Contains("\t\t\tregiments={"))
                    {
                        MatchCollection regiments_ids = Regex.Matches(line, @"(\d+) ");
                        List<ArmyRegiment> army_regiments = new List<ArmyRegiment>();
                        foreach(Match match in regiments_ids)
                        {
                            string id_ = match.Groups[1].Value;
                            ArmyRegiment army_regiment = new ArmyRegiment(id_);
                            army_regiments.Add(army_regiment);
                        }

                        if(isAttacker)
                        {
                            attacker_armies[index].SetArmyRegiments(army_regiments);
                        }
                        else if(isDefender)
                        {
                            defender_armies[index].SetArmyRegiments(army_regiments);
                        }

                    }
                    else if(isSearchStarted && line.Contains("\t\t\tcommander="))
                    {
                        string id = Regex.Match(line, @"commander=(\d+)").Groups[1].Value;                                                                                                                                                              
                        if (isAttacker)
                        {
                            attacker_armies[index].CommanderID = id;
                        }
                        else if (isDefender)
                        {
                            defender_armies[index].CommanderID = id;
                        }
                    }
                    else if (isSearchStarted && line.Contains("\t\t\tunit="))
                    {
                        string armyUnitId = Regex.Match(line, @"\d+").Value;
                        if(isAttacker)
                        {
                            attacker_armies[index].ArmyUnitID = armyUnitId;
                        }
                        else if(isDefender)
                        {
                            defender_armies[index].ArmyUnitID = armyUnitId;
                        }
                    }



                    // Army End Line
                    if (isSearchStarted && line == "\t\t}")
                    {
                        index = 0;
                        isAttacker = false;
                        isDefender = false;
                        isSearchStarted = false;
                    }

                }
            }
        }

        private static void ReadCombatArmies(string g)
        {
            bool isAttacker = false, isDefender = false;

            using (StringReader SR = new StringReader(g))//Player_Combat
            {
                while (true)
                {
                    string? line = SR.ReadLine();
                    if (line == null) break;

                    if (line.Contains("\t\t\tattacker={"))
                    {
                        isAttacker = true;
                        isDefender = false;
                    }
                    else if (line.Contains("\t\t\tdefender={"))
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

        private static string? FindReliefFieldCombatBlock()
        {
            Program.Logger.Debug("Searching for relief field combat block in Combats.txt...");
            try
            {
                string combatsContent = File.ReadAllText(Writter.DataFilesPaths.Combats_Path());
                string[] combatBlocks = Regex.Split(combatsContent, @"(?=^\s*\d+={)", RegexOptions.Multiline);

                // 1. Get all mobile (non-garrison) army IDs from the current battle
                var mobileArmyIdsInBattle = new HashSet<string>(
                    attacker_armies.Where(a => !a.IsGarrison()).Select(a => a.ID)
                    .Concat(defender_armies.Where(a => !a.IsGarrison()).Select(a => a.ID))
                );

                if (!mobileArmyIdsInBattle.Any())
                {
                    Program.Logger.Debug("No mobile armies found in the battle. Cannot search for a field combat block.");
                    return null;
                }
                
                Program.Logger.Debug($"Mobile armies in battle: [{string.Join(", ", mobileArmyIdsInBattle)}]");

                foreach (string block in combatBlocks)
                {
                    if (string.IsNullOrWhiteSpace(block)) continue;

                    // 2. Check if the block is in the correct province
                    if (!block.Contains($"province={BattleResult.ProvinceID}"))
                    {
                        continue;
                    }

                    // 3. Check for attacker and defender armies
                    if (!block.Contains("attacker={") || !block.Contains("defender={"))
                    {
                        continue;
                    }

                    // 4. Extract all army IDs from this block
                    var armyIdsInBlock = new HashSet<string>();
                    MatchCollection armiesMatches = Regex.Matches(block, @"armies={\s*([\d\s]+)\s*}");
            
                    if (armiesMatches.Count < 2) continue; // Must have at least attacker and defender armies

                    foreach (Match m in armiesMatches)
                    {
                        var ids = m.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var id in ids)
                        {
                            armyIdsInBlock.Add(id);
                        }
                    }

                    // 5. Compare the set of IDs
                    if (mobileArmyIdsInBattle.SetEquals(armyIdsInBlock))
                    {
                        Program.Logger.Debug($"Found matching field combat block for province {BattleResult.ProvinceID}.");
                        return block;
                    }
                }

                Program.Logger.Debug("No matching relief field combat block found.");
                return null;
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error reading or parsing Combats.txt for relief battle: {ex.Message}");
                return null;
            }
        }

        static void ReadCombatSoldiersNum(string? combat_string)
        {
            if (combat_string == null) return;

            string? combatStringToParse = combat_string; // Keep this variable for the using statement
            
            bool isAttacker = false, isDefender = false;
            string? searchingArmyRegiment = null;
            using (StringReader SR = new StringReader(combatStringToParse))//Player_Combat
            {
                while (true)
                {
                    string? line = SR.ReadLine();
                    if (line == null) break;

                    if (line.Contains("\t\t\tattacker={"))
                    {
                        isAttacker = true;
                        isDefender = false;
                    }
                    else if (line.Contains("\t\t\tdefender={"))
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
                        string startingNum = Regex.Match(line, @"\d+").Value;
                        if (searchingArmyRegiment != null)
                        {
                            foreach(var army in attacker_armies)
                            {
                                var regToUpdate = army.ArmyRegiments.Where(x => x != null).FirstOrDefault(x => x.ID == searchingArmyRegiment);
                                if (regToUpdate != null)
                                {
                                    regToUpdate.StartingNum = int.Parse(startingNum);
                                }
                            }
                        }
                    }
                    else if(isDefender && line.Contains("\t\t\t\t\t\tstarting="))
                    {
                        string startingNum = Regex.Match(line, @"\d+").Value;
                        if (searchingArmyRegiment != null)
                        {
                            foreach (var army in defender_armies)
                            {
                                var regToUpdate = army.ArmyRegiments.Where(x => x != null).FirstOrDefault(x => x.ID == searchingArmyRegiment);
                                if (regToUpdate != null)
                                {
                                    regToUpdate.StartingNum = int.Parse(startingNum);
                                }
                            }
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
