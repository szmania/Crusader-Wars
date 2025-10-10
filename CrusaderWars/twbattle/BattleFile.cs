using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Windows.Forms;
using CrusaderWars.armies;
using CrusaderWars.client;
using CrusaderWars.data.save_file;
using CrusaderWars.locs;
using CrusaderWars.sieges;
using CrusaderWars.terrain;
using CrusaderWars.twbattle;
using CrusaderWars.unit_mapper;
using static CrusaderWars.terrain.Lands;
using System.Text;
using CrusaderWars.data.battle_results;


namespace CrusaderWars
{
    public static class BattleFile
    {

        public static string? Unit_Script_Name { get; set; }

        //Get User Path
        static string battlePath = Directory.GetFiles("data\\battle files\\script", "tut_battle.xml", SearchOption.AllDirectories)[0];

        public static void ClearFile()
        {
            
            bool isCreated = false;
            if (isCreated == false)
            {
                using (FileStream logFile = File.Open(battlePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    //File.Create(path_log); 
                    isCreated = true;
                }

            }
        }

        public static void SetArmiesSides(List<Army> attacker_armies, List<Army> defender_armies)
        {
            if (twbattle.BattleState.IsSiegeBattle)
            {
                // New logic for siege battles
                string player_character_id = DataSearch.Player_Character.GetID();
                // In the CK3 log, the RightSide always represents the initial besieged force.
                // If the player is besieged, the mod swaps them to the RightSide.
                // Therefore, the player is the defender in the Attila battle if their ID is on the RightSide of the log.
                bool playerIsDefender = CK3LogData.RightSide.GetMainParticipant().id == player_character_id ||
                                        CK3LogData.RightSide.GetCommander().id == player_character_id;

                if (playerIsDefender)
                {
                    Program.Logger.Debug("Siege battle: Player is defending. Assigning defender_armies as player side, attacker_armies as enemy side.");
                    foreach (var army in defender_armies) { army.IsPlayer(true); }
                    foreach (var army in attacker_armies) { army.IsEnemy(true); }
                }
                else
                {
                    Program.Logger.Debug("Siege battle: Player is attacking. Assigning attacker_armies as player side, defender_armies as enemy side.");
                    foreach (var army in attacker_armies) { army.IsPlayer(true); }
                    foreach (var army in defender_armies) { army.IsEnemy(true); }
                }
            }
            else
            {
                // Original logic for field battles
                string player_commander_id = CK3LogData.LeftSide.GetCommander().id;

                bool isPlayerTheAttacker = attacker_armies.Any(army => army.CommanderID == player_commander_id);

                if (isPlayerTheAttacker)
                {
                    // Player is attacker, enemy is defender
                    foreach (var army in attacker_armies) { army.IsPlayer(true); }
                    foreach (var army in defender_armies) { army.IsEnemy(true); }
                }
                else
                {
                    // Player is defender, enemy is attacker
                    foreach (var army in attacker_armies) { army.IsEnemy(true); }
                    foreach (var army in defender_armies) { army.IsPlayer(true); } // Corrected line
                }
            }
        }

        static void AllControledArmies(List<Army> temp_attacker_armies, List<Army> temp_defender_armies, Army player_army, Army enemy_main_army, int total_soldiers, (string X, string Y, string[] attPositions, string[] defPositions) battleMap, Dictionary<string, int> siegeEngines)
        {
            //----------------------------------------------
            //  Merge armies until there are only one      
            //----------------------------------------------
            ArmiesControl.MergeIntoOneArmy(temp_attacker_armies);
            if (!twbattle.BattleState.IsSiegeBattle)
            {
                ArmiesControl.MergeIntoOneArmy(temp_defender_armies);
            }
            else
            {
                ArmiesControl.MergeSiegeDefendersForCombinedArmyTag(temp_defender_armies);
            }

            // WRITE DECLARATIONS
            DeclarationsFile.CreateAlliances(temp_attacker_armies, temp_defender_armies);

            //Write essential data
            OpenBattle();
            //Write essential data
            OpenPlayerAlliance();


            if (player_army.CombatSide == "attacker")
            {
                //#### WRITE HUMAN PLAYER ARMY
                WriteArmy(temp_attacker_armies[0], total_soldiers, temp_attacker_armies[0].IsReinforcementArmy(), "stark", siegeEngines);
            }
            else if (player_army.CombatSide == "defender")
            {
                foreach(var army in temp_defender_armies)
                {
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "stark", siegeEngines);
                }
            }

            //Write essential data
            SetVictoryCondition(player_army);
            //Write essential data
            CloseAlliance();
            //Write essential data
            OpenEnemyAlliance();

            if (enemy_main_army.CombatSide == "attacker")
            {
                //#### WRITE HUMAN PLAYER ARMY
                WriteArmy(temp_attacker_armies[0], total_soldiers, temp_attacker_armies[0].IsReinforcementArmy(), "bolton", siegeEngines);
            }
            else if (enemy_main_army.CombatSide == "defender")
            {
                foreach (var army in temp_defender_armies)
                {
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "bolton", siegeEngines);
                }
            }

            //Write essential data
            SetVictoryCondition(enemy_main_army);
            //Write essential data
            CloseAlliance();
            //Write battle description
            SetBattleDescription(player_army, total_soldiers);
            //Write battle map
            SetBattleTerrain(battleMap.X, battleMap.Y, Weather.GetWeather(), GetAttilaMap());
            //Write essential data
            CloseBattle();
        }

        static void FriendliesOnlyArmies(List<Army> temp_attacker_armies, List<Army> temp_defender_armies, Army player_main_army, Army enemy_main_army, int total_soldiers, (string X, string Y, string[] attPositions, string[] defPositions) battleMap, Dictionary<string, int> siegeEngines)
        {
            Program.Logger.Debug("--- Starting FriendliesOnlyArmies setup ---");
            Program.Logger.Debug($"Initial armies: {temp_attacker_armies.Count} attackers, {temp_defender_armies.Count} defenders.");
            Program.Logger.Debug($"Player main army: {player_main_army.ID}, Enemy main army: {enemy_main_army.ID}");

            //----------------------------------------------
            //  Merge friendly armies to main army     
            //----------------------------------------------
            bool isUserAlly = false; Army? userAlliedArmy = null;
            if (player_main_army.Owner?.GetID() == DataSearch.Player_Character?.GetID())
            {
                Program.Logger.Debug("Player character is the main commander of their side.");
                isUserAlly = false;
                if (player_main_army.CombatSide == "attacker")
                {
                    Program.Logger.Debug($"Merging friendly attacker armies into player's main army ({player_main_army.ID}).");
                    player_main_army = ArmiesControl.MergeFriendlies(temp_attacker_armies, player_main_army);
                }
                else if (player_main_army.CombatSide == "defender")
                {
                    Program.Logger.Debug($"Merging friendly defender armies into player's main army ({player_main_army.ID}).");
                    player_main_army = ArmiesControl.MergeFriendlies(temp_defender_armies, player_main_army);
                }
            }
            else // Player character is an ally, not the main commander
            {
                Program.Logger.Debug("Player character is an ally, not the main commander. Prompting user to change 'Armies Control' setting.");

                string message = "To control your army as an ally, the 'Armies Control' setting must be 'All Controlled'.\n\n" +
                                 "Do you want to change it automatically and proceed with the battle?";
                string title = "Crusader Conflicts: Ally Army Control";
                DialogResult result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);

                if (result == DialogResult.Yes)
                {
                    Program.Logger.Debug("User agreed to change 'Armies Control' to 'All Controlled'.");
                    // Update the setting programmatically
                    Options.SetArmiesControl(ModOptions.ArmiesSetup.All_Controled);
                    Program.Logger.Debug($"'Armies Control' setting updated to: {ModOptions.SeparateArmies()}");

                    // Now call AllControledArmies and return
                    AllControledArmies(temp_attacker_armies, temp_defender_armies, player_main_army!, enemy_main_army!, total_soldiers, battleMap, siegeEngines);
                    return; // Exit FriendliesOnlyArmies after calling AllControledArmies
                }
                else // User clicked No
                {
                    Program.Logger.Debug("User declined to change 'Armies Control' setting. Aborting battle generation.");
                    MessageBox.Show("None of your armies are present in this battle! (If this error is unexpected, confirm your \"Armies Control\" selection in the mod settings)", "Crusader Conflicts: No Player Army",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    throw new Exception("Battle aborted: Player declined to change 'Armies Control' setting.");
                }
            }

            if (enemy_main_army.CombatSide == "attacker")
            {
                Program.Logger.Debug($"Merging friendly attacker armies into enemy's main army ({enemy_main_army.ID}).");
                enemy_main_army = ArmiesControl.MergeFriendlies(temp_attacker_armies, enemy_main_army);
            }
            else if (enemy_main_army.CombatSide == "defender")
            {
                Program.Logger.Debug($"Merging friendly defender armies into enemy's main army ({enemy_main_army.ID}).");
                enemy_main_army = ArmiesControl.MergeFriendlies(temp_defender_armies, enemy_main_army);
            }

            if (twbattle.BattleState.IsSiegeBattle)
            {
                ArmiesControl.MergeSiegeDefendersForCombinedArmyTag(temp_defender_armies);
            }

            //----------------------------------------------
            //  Merge armies until there are only three      
            //----------------------------------------------
            Program.Logger.Debug($"Armies before merging down to three: {temp_attacker_armies.Count} attackers, {temp_defender_armies.Count} defenders.");
            ArmiesControl.MergeArmiesUntilThree(temp_attacker_armies);
            ArmiesControl.MergeArmiesUntilThree(temp_defender_armies);
            Program.Logger.Debug($"Armies after merging down to three: {temp_attacker_armies.Count} attackers, {temp_defender_armies.Count} defenders.");

            // WRITE DECLARATIONS
            if(!isUserAlly)
                DeclarationsFile.CreateAlliances(temp_attacker_armies, temp_defender_armies, player_main_army, enemy_main_army);
            else
                DeclarationsFile.CreateAlliances(temp_attacker_armies, temp_defender_armies, userAlliedArmy!, enemy_main_army);

            //Write essential data
            OpenBattle();
            //Write essential data
            Program.Logger.Debug("Writing player alliance to battle file...");
            OpenPlayerAlliance();

            //#### WRITE HUMAN PLAYER ARMY
            if(!isUserAlly)
            {
                Program.Logger.Debug($"Writing player main army {player_main_army.ID} to file.");
                WriteArmy(player_main_army, total_soldiers, player_main_army.IsReinforcementArmy(), "stark", siegeEngines);
            }
            else
            {
                Program.Logger.Debug($"Writing player allied army {userAlliedArmy!.ID} to file.");
                WriteArmy(userAlliedArmy!, total_soldiers, userAlliedArmy.IsReinforcementArmy(), "stark", siegeEngines);
            }


            //#### WRITE AI ALLIED ARMIES
            Program.Logger.Debug("Writing AI allied armies for player's side...");
            if (player_main_army.CombatSide == "attacker")
            {
                if (!isUserAlly)
                {
                    if (player_main_army != null) temp_attacker_armies.Remove(player_main_army);
                }
                else
                {
                    if (userAlliedArmy != null) temp_attacker_armies.Remove(userAlliedArmy);
                }
                foreach (var army in temp_attacker_armies.Where(a => a != null))
                {
                    Program.Logger.Debug($"Writing attacker/defender allied army {army.ID} to file.");
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "stark", siegeEngines);

                }
            }
            else if (player_main_army.CombatSide == "defender")
            {
                if (!isUserAlly)
                {
                    if (player_main_army != null) temp_defender_armies.Remove(player_main_army);
                }
                else
                {
                    if (userAlliedArmy != null) temp_defender_armies.Remove(userAlliedArmy);
                }
                foreach (var army in temp_defender_armies.Where(a => a != null))
                {
                    Program.Logger.Debug($"Writing attacker/defender allied army {army.ID} to file.");
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "stark", siegeEngines);
                }
            }

            //Write essential data
            SetVictoryCondition(player_main_army);
            //Write essential data
            CloseAlliance();
            //Write essential data
            Program.Logger.Debug("Writing enemy alliance to battle file...");
            OpenEnemyAlliance();



            //#### WRITE ENEMY MAIN ARMY
            Program.Logger.Debug($"Writing enemy main army {enemy_main_army.ID} to file.");
            WriteArmy(enemy_main_army, total_soldiers, enemy_main_army.IsReinforcementArmy(), "bolton", siegeEngines);

            //#### WRITE ENEMY ALLIED ARMIES
            if (enemy_main_army.CombatSide == "attacker")
            {
                if (enemy_main_army != null) temp_attacker_armies.Remove(enemy_main_army);
                foreach (var army in temp_attacker_armies.Where(a => a != null))
                {
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "bolton", siegeEngines);
                }
            }
            else if (enemy_main_army.CombatSide == "defender")
            {
                if (enemy_main_army != null) temp_defender_armies.Remove(enemy_main_army);
                foreach (var army in temp_defender_armies.Where(a => a != null))
                {
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "bolton", siegeEngines);
                }
            }

            //Write essential data
            SetVictoryCondition(enemy_main_army);
            //Write essential data
            CloseAlliance();
            //Write battle description
            SetBattleDescription(player_main_army!, total_soldiers);
            //Write battle map
            SetBattleTerrain(battleMap.X, battleMap.Y, Weather.GetWeather(), GetAttilaMap());
            //Write essential data
            CloseBattle();
            Program.Logger.Debug("--- Finished FriendliesOnlyArmies setup ---");

        }

        static void AllSeparateArmies(List<Army> temp_attacker_armies, List<Army> temp_defender_armies, Army player_main_army, Army enemy_main_army, int total_soldiers, (string X, string Y, string[] attPositions, string[] defPositions) battleMap, Dictionary<string, int> siegeEngines)
        {
            bool isUserAlly = false; Army? userAlliedArmy = null;
            //----------------------------------------------
            //  Merge armies until there are only four      
            //----------------------------------------------

            if(player_main_army.Owner?.GetID() == DataSearch.Player_Character?.GetID())
            {
                isUserAlly = false;
                ArmiesControl.MergeArmiesUntilFour(temp_attacker_armies);
                if(!twbattle.BattleState.IsSiegeBattle)
                {
                    ArmiesControl.MergeArmiesUntilFour(temp_defender_armies);
                }
            }
            else
            {
                isUserAlly = true;
                userAlliedArmy = temp_attacker_armies.Find(x => x.Owner?.GetID() == DataSearch.Player_Character?.GetID()) ?? temp_defender_armies.Find(x => x.Owner?.GetID() == DataSearch.Player_Character?.GetID());

                if (userAlliedArmy == null)
                {
                    // This should not happen anymore due to the pre-check in MainFile.cs, but serves as a safeguard.
                    throw new InvalidOperationException("Player army not found in battle, but the pre-check passed. This indicates a logic error.");
                }

                if (userAlliedArmy.CombatSide == "attacker")
                {
                    temp_attacker_armies.Remove(userAlliedArmy);
                    temp_attacker_armies.Insert(0, userAlliedArmy);
                }
                else
                {
                    temp_defender_armies.Remove(userAlliedArmy);
                    temp_defender_armies.Insert(0, userAlliedArmy);
                }

                ArmiesControl.MergeArmiesUntilFour(temp_attacker_armies);
                if(!twbattle.BattleState.IsSiegeBattle)
                {
                    ArmiesControl.MergeArmiesUntilFour(temp_defender_armies);
                }
            }
            

            // WRITE DECLARATIONS
            DeclarationsFile.CreateAlliances(temp_attacker_armies, temp_defender_armies);

            //Write essential data
            OpenBattle();
            //Write essential data
            OpenPlayerAlliance();

            //#### WRITE HUMAN PLAYER ARMY
            if(!isUserAlly)
                WriteArmy(player_main_army, total_soldiers, player_main_army.IsReinforcementArmy(), "stark", siegeEngines);
            else
                WriteArmy(userAlliedArmy!, total_soldiers, userAlliedArmy.IsReinforcementArmy(), "stark", siegeEngines);

            //#### WRITE AI ALLIED ARMIES
            if (player_main_army.CombatSide == "attacker")
            {
                if (!isUserAlly)
                {
                    if (player_main_army != null) temp_attacker_armies.Remove(player_main_army);
                }
                else
                {
                    if (userAlliedArmy != null) temp_attacker_armies.Remove(userAlliedArmy);
                }
                foreach (var army in temp_attacker_armies.Where(a => a != null))
                {
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "stark", siegeEngines);

                }
            }
            else if (player_main_army.CombatSide == "defender")
            {
                if (!isUserAlly)
                {
                    if (player_main_army != null) temp_defender_armies.Remove(player_main_army);
                }
                else
                {
                    if (userAlliedArmy != null) temp_defender_armies.Remove(userAlliedArmy);
                }
                foreach (var army in temp_defender_armies.Where(a => a != null))
                {
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "stark", siegeEngines);
                }
            }

            //Write essential data
            SetVictoryCondition(player_main_army);
            //Write essential data
            CloseAlliance();
            //Write essential data
            OpenEnemyAlliance();



            //#### WRITE ENEMY MAIN ARMY
            WriteArmy(enemy_main_army, total_soldiers, enemy_main_army.IsReinforcementArmy(), "bolton", siegeEngines);

            //#### WRITE ENEMY ALLIED ARMIES
            if (enemy_main_army.CombatSide == "attacker")
            {
                if (enemy_main_army != null) temp_attacker_armies.Remove(enemy_main_army);
                foreach (var army in temp_attacker_armies.Where(a => a != null))
                {
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "bolton", siegeEngines);
                }
            }
            else if (enemy_main_army.CombatSide == "defender")
            {
                if (enemy_main_army != null) temp_defender_armies.Remove(enemy_main_army);
                foreach (var army in temp_defender_armies.Where(a => a != null))
                {
                    WriteArmy(army, total_soldiers, army.IsReinforcementArmy(), "bolton", siegeEngines);
                }
            }

            //Write essential data
            SetVictoryCondition(enemy_main_army);
            //Write essential data
            CloseAlliance();
            //Write battle description
            SetBattleDescription(player_main_army!, total_soldiers);
            //Write battle map
            SetBattleTerrain(battleMap.X, battleMap.Y, Weather.GetWeather(), GetAttilaMap());
            //Write essential data
            CloseBattle();
        }


        public static void BETA_CreateBattle(List<Army> attacker_armies, List<Army> defender_armies)
        {
            Program.Logger.Debug("Starting TW:Attila battle file creation...");
            //  TEMP OBJETS TO USE HERE
            List<Army> temp_attacker_armies = new List<Army>(),
                       temp_defender_armies = new List<Army>();

            temp_attacker_armies.AddRange(attacker_armies);
            temp_defender_armies.AddRange(defender_armies);

            // SIDES MAIN ARMIES
            Army? player_main_army = null;
            Army? enemy_main_army = null;
            player_main_army = temp_attacker_armies.FirstOrDefault(x => x.IsPlayer() && x.isMainArmy);
            if (player_main_army == null) player_main_army = temp_defender_armies.FirstOrDefault(x => x.IsPlayer() && x.isMainArmy);

            enemy_main_army = temp_attacker_armies.FirstOrDefault(x => x.IsEnemy() && x.isMainArmy);
            if (enemy_main_army == null) enemy_main_army = temp_defender_armies.FirstOrDefault(x => x.IsEnemy() && x.isMainArmy);


            // Fallback logic for main armies
            if (player_main_army == null)
            {
                Program.Logger.Debug("Main player army not found, attempting to find any player army as fallback.");
                player_main_army = temp_attacker_armies.FirstOrDefault(x => x.IsPlayer()) ?? temp_defender_armies.FirstOrDefault(x => x.IsPlayer());
            }
            if (enemy_main_army == null)
            {
                Program.Logger.Debug("Main enemy army not found, attempting to find any enemy army as fallback.");
                enemy_main_army = temp_attacker_armies.FirstOrDefault(x => x.IsEnemy()) ?? temp_defender_armies.FirstOrDefault(x => x.IsEnemy());
            }

            if (player_main_army == null || enemy_main_army == null)
            {
                throw new InvalidOperationException("Could not determine main player and enemy armies for the battle.");
            }

            //
            if(player_main_army.Commander!=null)  {
                UnitsFile.PlayerCommanderTraits = player_main_army.Commander.CommanderTraits;
            }
            else { 
                UnitsFile.PlayerCommanderTraits = null;
            }
            if (enemy_main_army.Commander != null) {
                UnitsFile.EnemyCommanderTraits = enemy_main_army.Commander.CommanderTraits;
            }
            else {
                UnitsFile.EnemyCommanderTraits = null;
            }



            // TOTAL SOLDIERS
            int total_soldiers = 0;
            total_soldiers = temp_attacker_armies.SelectMany(army => army.Units).Sum(unit => unit.GetSoldiers()) +
                             temp_defender_armies.SelectMany(army => army.Units).Sum(unit => unit.GetSoldiers());
            Program.Logger.Debug($"Total soldiers for battle: {total_soldiers}");

            //  BATTLE MAP
            (string X, string Y, string[] attPositions, string[] defPositions) battleMap = ("", "", new string[0], new string[0]); // Initialize as mutable tuple
            Dictionary<string, int> siegeEngines = new Dictionary<string, int>(); // Initialize siegeEngines

            if (twbattle.BattleState.IsSiegeBattle)
            {
                Program.Logger.Debug("Siege battle detected. Attempting to find custom settlement map.");
                string defenderAttilaFaction = UnitMappers_BETA.GetAttilaFaction(twbattle.Sieges.GetGarrisonCulture(), twbattle.Sieges.GetGarrisonHeritage());
                
                string siegeBattleType;
                int holdingLevel = twbattle.Sieges.GetHoldingLevel();
                if (holdingLevel > 1)
                {
                    siegeBattleType = "settlement_standard";
                }
                else
                {
                    siegeBattleType = "settlement_unfortified";
                }

                // Get the province name for unique map selection
                string provinceName = BattleResult.ProvinceName ?? "";

                var customSettlementMap = UnitMappers_BETA.GetSettlementMap(defenderAttilaFaction, siegeBattleType, provinceName);

                if (customSettlementMap.HasValue)
                {
                    battleMap.X = customSettlementMap.Value.X;
                    battleMap.Y = customSettlementMap.Value.Y;
                    battleMap.attPositions = new string[] { "All", "All" }; // Default for custom maps if not specified
                    battleMap.defPositions = new string[] { "All", "All" }; // Default for custom maps if not specified
                    Program.Logger.Debug($"Custom settlement map found for Faction '{defenderAttilaFaction}', BattleType '{siegeBattleType}', Province '{provinceName}': ({battleMap.X}, {battleMap.Y})");
                }
                else
                {
                    Program.Logger.Debug($"No custom settlement map found for Faction '{defenderAttilaFaction}', BattleType '{siegeBattleType}', Province '{provinceName}'. Falling back to TerrainGenerator.GetBattleMap().");
                    battleMap = TerrainGenerator.GetBattleMap(); // Fallback to existing logic
                }
                Program.Logger.Debug("Setting up siege-specific deployment...");
                Deployments.beta_SetSiegeDeployment(battleMap, total_soldiers);

                // Generate siege engines for the attacker
                siegeEngines = SiegeEngineGenerator.Generate(attacker_armies); // Pass attacker_armies
                Program.Logger.Debug($"Generated {siegeEngines.Count} siege engine types for the attacker.");
            }
            else
            {
                Program.Logger.Debug("Land battle detected. Using TerrainGenerator.GetBattleMap().");
                battleMap = TerrainGenerator.GetBattleMap(); // Existing logic for land battles
            
                var playerCommanderTraits = UnitsFile.GetCommanderTraitsObj(true);
                var enemyCommanderTraits = UnitsFile.GetCommanderTraitsObj(false);

                bool shouldPlayerRotateDeployment = playerCommanderTraits?.ShouldRotateDeployment(player_main_army.CombatSide, TerrainGenerator.TerrainType) ?? false;
                bool shouldEnemyRotateDeployment = enemyCommanderTraits?.ShouldRotateDeployment(enemy_main_army.CombatSide, TerrainGenerator.TerrainType) ?? false;

                if (shouldPlayerRotateDeployment || shouldEnemyRotateDeployment)
                {
                    Deployments.beta_SetSidesDirections(total_soldiers, battleMap, true);
                }
                else
                    Deployments.beta_SetSidesDirections(total_soldiers, battleMap, false);
            }




            //  ALL CONTROLED ARMIES
            //
            if (ModOptions.SeparateArmies() == ModOptions.ArmiesSetup.All_Controled)
            {
                AllControledArmies(temp_attacker_armies, temp_defender_armies, player_main_army!, enemy_main_army!, total_soldiers, battleMap, siegeEngines);
            }
            //  FRIENDLIES ONLY ARMIES
            //
            else if (ModOptions.SeparateArmies() == ModOptions.ArmiesSetup.Friendly_Only)
            {
                FriendliesOnlyArmies(temp_attacker_armies, temp_defender_armies, player_main_army!, enemy_main_army!, total_soldiers, battleMap, siegeEngines);
            }
            //  ALL SEPARATE ARMIES
            //
            else if (ModOptions.SeparateArmies() == ModOptions.ArmiesSetup.All_Separate)
            {
                AllSeparateArmies(temp_attacker_armies, temp_defender_armies, player_main_army!, enemy_main_army!, total_soldiers, battleMap, siegeEngines);
            }

            if (ModOptions.UnitCards())
            {
                string mapperName = UnitMappers_BETA.GetLoadedUnitMapperName() ?? "default_mapper";
                UnitsCardsNames.ChangeUnitsCardsNames(mapperName, attacker_armies, defender_armies);
            }

            UnitMappers_BETA.SetMapperImage();

        }

        private static void WriteArmy(Army army, int total_soldiers, bool isReinforcement, string x, Dictionary<string, int> siegeEngines)
        {
            Program.Logger.Debug($"Writing army {army.ID} to battle file. Side: {army.CombatSide}, Alliance: {x}, IsReinforcement: {isReinforcement}");
            
            // Determine player and enemy realm names dynamically
            string playerRealmName;
            string enemyRealmName;
            string playerCharId = DataSearch.Player_Character.GetID();

            // The player is on LeftSide if their character ID matches the main participant or commander of LeftSide.
            // This logic needs to be consistent with how playerIsDefender is determined in SetArmiesSides.
            // If playerIsDefender is true, then the player is on the defender_armies side.
            // If playerIsDefender is false, then the player is on the attacker_armies side.
            // The CK3LogData.LeftSide/RightSide refers to the log's perspective, which might be swapped for besieged player.
            // We need to use the `army.IsPlayer()` and `army.IsEnemy()` flags set by `SetArmiesSides` for consistency.

            if (army.IsPlayer())
            {
                // If this army is a player army, its realm name is the player's realm name.
                // The enemy realm name is the other side's realm name from the log.
                if (CK3LogData.LeftSide.GetMainParticipant().id == playerCharId || CK3LogData.LeftSide.GetCommander().id == playerCharId)
                {
                    playerRealmName = CK3LogData.LeftSide.GetRealmName();
                    enemyRealmName = CK3LogData.RightSide.GetRealmName();
                }
                else
                {
                    playerRealmName = CK3LogData.RightSide.GetRealmName();
                    enemyRealmName = CK3LogData.LeftSide.GetRealmName();
                }
            }
            else // This is an enemy army
            {
                // If this army is an enemy army, its realm name is the enemy's realm name.
                // The player realm name is the player's realm name from the log.
                if (CK3LogData.LeftSide.GetMainParticipant().id == playerCharId || CK3LogData.LeftSide.GetCommander().id == playerCharId)
                {
                    playerRealmName = CK3LogData.LeftSide.GetRealmName();
                    enemyRealmName = CK3LogData.RightSide.GetRealmName();
                }
                else
                {
                    playerRealmName = CK3LogData.RightSide.GetRealmName();
                    enemyRealmName = CK3LogData.LeftSide.GetRealmName();
                }
            }


            //Write essential data
            if (isReinforcement)
                OpenReinforcementArmy();
            else
                OpenArmy();

            //Write army faction name
            if(army.IsPlayer() && army.isMainArmy)
                AddArmyName(playerRealmName);
            else if(army.IsEnemy() && army.isMainArmy)
                AddArmyName(enemyRealmName);     
            else
                AddArmyName("Allied Army");


            //Write essential data
            if (x == "stark")
                SetPlayerFaction(army);
            else
                SetEnemyFaction(army);

            // Set deployment area and unit positions
            string deploymentDirection;

            if (isReinforcement)
            {
                // This logic applies to any reinforcement army (currently only in sieges)
                deploymentDirection = Deployments.GetOppositeDirection(Deployments.beta_GeDirection("attacker"));
                
                // Only add a deployment area for reinforcements in a siege battle, as requested.
                if (twbattle.BattleState.IsSiegeBattle)
                {
                    string? reinforcementDeployment = Deployments.beta_GetReinforcementDeployment(deploymentDirection, total_soldiers);
                    if (reinforcementDeployment != null)
                    {
                        File.AppendAllText(battlePath, reinforcementDeployment);
                    }
                }
            }
            else // Not a reinforcement army
            {
                deploymentDirection = Deployments.beta_GeDirection(army.CombatSide);
                SetDeploymentArea(army.CombatSide);
                AddDeployablesDefenses(army);
            }

            SetPositions(total_soldiers, deploymentDirection, army.IsReinforcementArmy());
            
            //Write all player army units
            UnitsFile.BETA_ConvertandAddArmyUnits(army);

            // Add siege equipment if this is the attacking army in a siege battle
            if (army.CombatSide == "attacker" && siegeEngines != null && siegeEngines.Any())
            {
                AddAssaultEquipment(siegeEngines);
            }

            // Process merged armies (for combined siege defender scenarios)
            if (army.MergedArmies.Any())
            {
                foreach (var mergedArmy in army.MergedArmies)
                {
                    if (mergedArmy.IsReinforcementArmy())
                    {
                        // Set position for reinforcement units to spawn at the map edge
                        string deploymentDirectionForMerged = Deployments.GetOppositeDirection(Deployments.beta_GeDirection("attacker"));
                        SetPositions(total_soldiers, deploymentDirectionForMerged, true);
                        
                        // Write the units for this merged reinforcement army
                        UnitsFile.BETA_ConvertandAddArmyUnits(mergedArmy);
                    }
                }
            }

            //Write essential data
            if (isReinforcement)
                CloseReinforcementArmy();
            else
                CloseArmy();
        }


        private static string GetAttilaMap()
        {
            string default_attila_map = "Terrain/battles/main_attila_map/";
            string? attilaMap = UnitMappers_BETA.Terrains?.GetAttilaMap();

            if (!string.IsNullOrEmpty(attilaMap))
            {
                return attilaMap;
            }
            else
            {
                return default_attila_map;
            }
        }

        private static void OpenBattle()
        {
            string PR_CreateBattle = "<?xml version=\"1.0\"?>\n" +
                                     "<battle>\n";

            File.AppendAllText(battlePath, PR_CreateBattle);

        }

        private static void OpenPlayerAlliance()
        {
            string PR_OpenAlliance = "<alliance id=\"0\">\n";

            File.AppendAllText(battlePath, PR_OpenAlliance);

        }

        private static void OpenArmy()
        {
            string PR_OpenArmy = "<army>\n\n";

            File.AppendAllText(battlePath, PR_OpenArmy);
        }

        private static void OpenReinforcementArmy()
        {
            string PR_OpenArmy = "<army>\n\n";
            File.AppendAllText(battlePath, PR_OpenArmy);
        }

        private static void AddArmyName(string name)
        {
            if(name != String.Empty)
            {
                string PR_ArmyName = $"<army_name>{name}</army_name>\n\n";

                File.AppendAllText(battlePath, PR_ArmyName);
            }
            
        }

        private static void SetPlayerFaction(Army army)
        {
            string PR_PlayerFaction = $"<faction>{GetAOJFaction(army, true)}</faction>\n\n";

            File.AppendAllText(battlePath, PR_PlayerFaction);
        }

        // TEMPORARY CODE FOR AGE OF JUSTINIAN REPEATED UNIT KEYS
        static List<(string? AttilaFaction, string? Faction)> aoj_list = new List<(string? AttilaFaction, string? Faction)>()
        {
                ("Copt", "att_fact_ghassanids"),
                ("Bedouin", "att_fact_lakhmids"),
                ("Abbasid", "att_fact_lakhmids"),
                ("Himyarite", "att_fact_himyar"),
                ("Sahelian", "att_fact_garamantes"),
                ("Syriac", "att_fact_ghassanids"),
                ("Horn African", "att_fact_axum"),
                ("Coptic", "att_fact_axum"),
                ("Kurdish", "att_fact_mazun"),
                ("Burmese", "att_fact_white_huns"),
                ("Tibetan", "att_fact_white_huns"),
                ("Turkic", "att_fact_parthia"),
                ("Eastern Steppe", "att_fact_white_huns"),
                ("Western Steppe", "att_fact_hunni"),
                ("Bulgarian", "att_fact_langobardi"),
                ("Bolghar", "att_fact_hunni"),
                ("Hephthalite", "att_fact_white_huns"),
                ("Permian", "att_fact_hunni"),
                ("Gothic", "att_fact_greuthingi"),
                ("Roman African", "att_fact_mauri"),
                ("Wendish", "att_fact_ostrogothi"),
                ("South Arabian" ,"att_fact_lakhmids")
        };
        private static string GetAOJFaction(Army army, bool isPlayer)
        {
            string faction = isPlayer ? "historical_house_stark" : "historical_house_bolton";
            
            // Add null check for army.Units
            if (army.Units == null) return faction;
            
            foreach (Unit unit in army.Units)
            {
                if (unit != null)
                {
                    var cultureObj = unit.GetObjCulture();
                    if (cultureObj != null)
                    {
                        string culture = cultureObj!.GetCultureName();
                        string heritage = cultureObj!.GetHeritageName();
                        string attila_faction = UnitMappers_BETA.GetAttilaFaction(culture, heritage);

                        foreach (var item in aoj_list)
                        {
                            if (item.AttilaFaction == attila_faction)
                            {
                                faction = item.Faction ?? faction;
                                break;
                            }
                        }
                    }
                }
            }
            return faction;
        }

        private static void SetEnemyFaction(Army army)
        {
            string PR_EnemyFaction = $"<faction>{GetAOJFaction(army, false)}</faction>\n\n";

            File.AppendAllText(battlePath, PR_EnemyFaction);
        }

        private static void SetDeploymentArea(string combat_side)
        {
            string? PR_Deployment = Deployments.beta_GetDeployment(combat_side); 
            if (PR_Deployment != null)
            {
                File.AppendAllText(battlePath, PR_Deployment);
            }
        }

        private static void AddDeployablesDefenses(Army army)
        {
            if (army.CombatSide == "defender" && ModOptions.DefensiveDeployables() is true && army.Commander != null)
            {
                int deployables_boost = UnitsFile.GetCommanderTraitsObj(army.IsPlayer())?.GetDeployablesBoost() ?? 0;
                int army_soldiers = army.Units.Sum(unit => unit.GetSoldiers());
                army.SetDefences(new DefensiveSystem(army_soldiers, army.Commander.Martial, deployables_boost));
                string PR_DefensiveDeployments = army.Defences.GetText();
                File.AppendAllText(battlePath, PR_DefensiveDeployments);
            }
        }



        static UnitsDeploymentsPosition? Position;


        static string west_rotation = "1.57";
        static string east_rotation = "4.71";
        static string south_rotation = "0.00";
        static string north_rotation = "3.14";


        static string? Rotation;

        public static void SetPositions(int total_soldiers, string direction, bool isReinforcement)
        {

            UnitsDeploymentsPosition UnitsPosition = new UnitsDeploymentsPosition(direction, ModOptions.DeploymentsZones(), total_soldiers, isReinforcement) ;

            if (UnitsPosition.Direction == "N")
            {
                Position = UnitsPosition;
                Rotation = north_rotation;
            }
            else if (UnitsPosition.Direction == "S")
            {
                Position = UnitsPosition;
                Rotation = south_rotation;
            }
            else if (UnitsPosition.Direction == "E")
            {

                Position = UnitsPosition;
                Rotation = east_rotation;
            }
            else if (UnitsPosition.Direction == "W")
            {
                Position = UnitsPosition;
                Rotation = west_rotation;
            }
        }


        public static void AddUnit(string troopKey, int numSoldiers, int numUnits, int numRest, string unitScript, string unit_experience, string direction)
        {
            if (Position == null)
            {
                Program.Logger.Debug("CRITICAL: BattleFile.Position was null when trying to add a unit. Aborting unit addition.");
                return;
            }

            if(numSoldiers <= 1 || numUnits == 0) return;


            if (Int32.Parse(unit_experience) < 0) unit_experience = "0";
            if (Int32.Parse(unit_experience) > 9) unit_experience = "9";

            for (int i = 0; i < numUnits; i++)
            {
                //Adding the rest soldiers to the first unit
                if (i == 0) numSoldiers += numRest;

                Unit_Script_Name = unitScript + i.ToString();
                string PR_Unit = $"<unit num_soldiers= \"{numSoldiers}\" script_name= \"{Unit_Script_Name}\">\n" +
                 $"<unit_type type=\"{troopKey}\"/>\n" +
                 $"<position x=\"{Position.X}\" y=\"{Position.Y}\"/>\n" +
                 $"<orientation radians=\"{Rotation}\"/>\n" +
                 "<width metres=\"21.70\"/>\n" +
                 $"<unit_experience level=\"{unit_experience}\"/>\n" +
                 "</unit>\n\n";

                //Add horizontal spacing between units
                if(direction is "N" || direction is "S")
                    Position.AddUnitXSpacing(direction);
                else
                {
                    Position.AddUnitYSpacing(direction);
                }

                //Reset soldiers num to normal
                if (i == 0) numSoldiers -= numRest;

                //Adds Declarations and Locals to the Battle Files
                DeclarationsFile.AddUnitDeclaration("UNIT" + Unit_Script_Name, Unit_Script_Name);
                BattleScript.SetLocals(Unit_Script_Name, "UNIT" + Unit_Script_Name);
                Data.units_scripts.Add((Unit_Script_Name, "UNIT" + Unit_Script_Name));
                
                //Write to file
                File.AppendAllText(battlePath, PR_Unit);
            }

            //Add vertical spacing between units
            if (direction is "N" || direction is "S")
            Position.AddUnitYSpacing(direction);
            else
            Position.AddUnitXSpacing(direction);

        }

        public static void AddGeneralUnit(CommanderSystem Commander, string troopType, string unitScript, int experience, string direction)
        {
            if (Position == null)
                {
                Program.Logger.Debug("CRITICAL: BattleFile.Position was null when trying to add a unit. Aborting unit addition.");
                return;
            }

            if(Commander != null)
            {
                string name = Commander.Name;
                int numberOfSoldiers = Commander.GetUnitSoldiers();
                var accolade = Commander.GetAccolade();

                if (numberOfSoldiers < 1) return;

                if (experience < 0) experience = 0;
                if (experience > 9) experience = 9;

                for (int i = 0; i < 1; i++)
                {
                    Unit_Script_Name = unitScript + i.ToString();

                    string PR_General = $"<unit num_soldiers= \"{numberOfSoldiers}\" script_name= \"{Unit_Script_Name}\">\n" +
                     $"<unit_type type=\"{troopType}\"/>\n" +
                     $"<position x=\"{Position.X}\" y=\"{Position.Y}\"/>\n" +
                     $"<orientation radians=\"{Rotation}\"/>\n" +
                     "<width metres=\"21.70\"/>\n" +
                     $"<unit_experience level=\"{experience}\"/>\n" +
                     "<unit_capabilities>\n" +
                     "<special_ability></special_ability>\n";


                    if(accolade != null)
                    {
                        var special_ability = AccoladesAbilities.ReturnAbilitiesKeys(accolade);
                        if (special_ability.primaryKey != "null")
                        {
                            PR_General += $"<special_ability>{special_ability.primaryKey}</special_ability>\n";
                        }
                        if (special_ability.secundaryKey != "null")
                        PR_General += $"<special_ability>{special_ability.secundaryKey}</special_ability>\n";

                    }

                     PR_General += 
                     "</unit_capabilities>\n" +
                     "<general>\n" +
                     $"<name>{name}</name>\n" +
                     $"<star_rating level=\"{Commander.GetCommanderStarRating()}\"/>\n" +
                     "</general>\n" +
                     "</unit>\n\n";

                    //Add horizontal spacing between units
                    if (direction is "N" || direction is "S")
                        Position.AddUnitXSpacing(direction);
                    else
                    {
                        Position.AddUnitYSpacing(direction);
                    }

                    DeclarationsFile.AddUnitDeclaration("UNIT" + Unit_Script_Name, Unit_Script_Name);
                    BattleScript.SetLocals(Unit_Script_Name, "UNIT" + Unit_Script_Name);
                    Data.units_scripts.Add((Unit_Script_Name, "UNIT" + Unit_Script_Name));
                    File.AppendAllText(battlePath, PR_General);
                }

                //Add vertical spacing between units
                if (direction is "N" || direction is "S")
                    Position.AddUnitYSpacing(direction);
                else
                    Position.AddUnitXSpacing(direction);
            }

        }

        public static void AddKnightUnit(KnightSystem Knights, string troopType, string unitScript, int experience, string direction)
        {
            if (Position == null)
            {
                Program.Logger.Debug("CRITICAL: BattleFile.Position was null when trying to add a unit. Aborting unit addition.");
                return;
            }

            int numberOfSoldiers = Knights.GetKnightsSoldiers();

            Knights.SetAccolades();
            var accoladesList = Knights.GetAccolades();


            if (numberOfSoldiers == 0) return;


            if (experience < 0) experience = 0;
            if (experience > 9) experience = 9;

            for (int i = 0; i < 1; i++)
            {
                Unit_Script_Name = unitScript + i.ToString();

                string PR_Unit = $"<unit num_soldiers= \"{numberOfSoldiers}\" script_name= \"{Unit_Script_Name}\">\n" +
                 $"<unit_type type=\"{troopType}\"/>\n" +
                 $"<position x=\"{Position.X}\" y=\"{Position.Y}\"/>\n" +
                 $"<orientation radians=\"{Rotation}\"/>\n" +
                 "<width metres=\"21.70\"/>\n" +
                 $"<unit_experience level=\"{experience}\"/>\n";
                
                PR_Unit += "<unit_capabilities>\n";
                PR_Unit += "<special_ability></special_ability>\n"; //dummy ability to remove all abilities from this unit

                //accolades special abilities
                if(accoladesList != null)
                {
                    foreach (var accolade in accoladesList)
                    {
                        var accoladeAbilites = AccoladesAbilities.ReturnAbilitiesKeys(accolade);
                        if (accoladeAbilites.primaryKey != "null")
                        {
                            PR_Unit += $"<special_ability>{accoladeAbilites.primaryKey}</special_ability>\n";
                        }
                        if (accoladeAbilites.secundaryKey != "null")
                        {
                            PR_Unit += $"<special_ability>{accoladeAbilites.secundaryKey}</special_ability>\n";
                        }

                    }
                }


                PR_Unit += "</unit_capabilities>\n";
                PR_Unit += "</unit>\n\n";

                //Add vertical spacing between units
                if (direction is "N" || direction is "S")
                    Position.AddUnitYSpacing(direction);
                else
                    Position.AddUnitXSpacing(direction);


                DeclarationsFile.AddUnitDeclaration("UNIT" + Unit_Script_Name, Unit_Script_Name);
                BattleScript.SetLocals(Unit_Script_Name, "UNIT" + Unit_Script_Name);
                Data.units_scripts.Add((Unit_Script_Name, "UNIT" + Unit_Script_Name));
                File.AppendAllText(battlePath, PR_Unit);
            }

            //Add vertical spacing between units
            if (direction is "N" || direction is "S")
                Position.AddUnitYSpacing(direction);
            else
                Position.AddUnitXSpacing(direction);

        }

        private static void AddAssaultEquipment(Dictionary<string, int> siegeEngines)
        {
            Program.Logger.Debug($"Adding {siegeEngines.Count} types of assault equipment.");
            if (Position == null || Rotation == null)
            {
                Program.Logger.Debug("CRITICAL: Position or Rotation is null when trying to add assault equipment. Aborting.");
                return;
            }

            File.AppendAllText(battlePath, "<assault_equipment>\n");

            foreach (var entry in siegeEngines)
            {
                string engineKey = entry.Key;
                int quantity = entry.Value;
                Program.Logger.Debug($"Adding {quantity} units of '{engineKey}'.");

                for (int i = 0; i < quantity; i++)
                {
                    string PR_Equipment = $"<assault_equipment_item equipment_name=\"{engineKey}\">\n" +
                                          $"<position x=\"{Position.X}\" y=\"{Position.Y}\"/>\n" +
                                          $"<orientation radians=\"{Rotation}\"/>\n" +
                                          "</assault_equipment_item>\n\n";
                    File.AppendAllText(battlePath, PR_Equipment);

                    // Advance position for the next equipment item
                    Position.AddUnitXSpacing(Deployments.beta_GeDirection("attacker"));
                }
            }

            File.AppendAllText(battlePath, "</assault_equipment>\n\n");
            Program.Logger.Debug("Finished adding assault equipment.");
        }


        private static (string x, string y) GetRoutPositionCoordinates(string direction)
        {
            string x = "0.00";
            string y = "0.00";
            string farDistance = "5000.00"; // A large distance off the map

            switch (direction)
            {
                case "N":
                    y = farDistance;
                    break;
                case "S":
                    y = "-" + farDistance;
                    break;
                case "E":
                    x = farDistance;
                    break;
                case "W":
                    x = "-" + farDistance;
                    break;
            }
            return (x, y);
        }

        private static void SetVictoryCondition(Army army)
        {
            StringBuilder victoryConditions = new StringBuilder();

            // Always add kill_or_rout_enemy
            victoryConditions.AppendLine("<victory_condition>");
            victoryConditions.AppendLine("<kill_or_rout_enemy></kill_or_rout_enemy>");
            victoryConditions.AppendLine("</victory_condition>");

            if (twbattle.BattleState.IsSiegeBattle)
            {
                if (army.CombatSide == "attacker")
                {
                    // Attacker specific conditions for siege
                    victoryConditions.AppendLine("<victory_condition>");
                    victoryConditions.AppendLine("<capture_settlement></capture_settlement>");
                    victoryConditions.AppendLine("</victory_condition>");

                    // Rout position for attacker (same side as deployment)
                    string attackerDeploymentDirection = Deployments.beta_GeDirection("attacker");
                    (string routX, string stringY) = GetRoutPositionCoordinates(attackerDeploymentDirection);
                    victoryConditions.AppendLine($"<rout_position x=\"{routX}\" y=\"{stringY}\"/>");
                }
                else // Defender specific conditions for siege
                {
                    victoryConditions.AppendLine("<starting_tickets>150</starting_tickets>");

                    // Rout position for defender (opposite side of attacker's deployment)
                    string attackerDeploymentDirection = Deployments.beta_GeDirection("attacker");
                    string defenderRoutDirection = Deployments.GetOppositeDirection(attackerDeploymentDirection);
                    (string routX, string routY) = GetRoutPositionCoordinates(defenderRoutDirection);
                    victoryConditions.AppendLine($"<rout_position x=\"{routX}\" y=\"{routY}\"/>");
                }

                if (army.IsPlayer())
                {
                    victoryConditions.AppendLine("<deploys_first></deploys_first>");
                }
            }
            else // Not a siege battle (field battle)
            {
                // Default rout position for field battles
                victoryConditions.AppendLine("<rout_position x=\"0.00\" y=\"0.00\"/>");
            }

            victoryConditions.AppendLine(); // Add an extra newline for formatting

            File.AppendAllText(battlePath, victoryConditions.ToString());
        }

        private static void CloseArmy()
        {
            string PR_CloseArmy = "</army>\n\n";

            File.AppendAllText(battlePath, PR_CloseArmy);

        }
        private static void CloseReinforcementArmy()
        {
            string PR_CloseArmy = "</army>\n\n";

            File.AppendAllText(battlePath, PR_CloseArmy);

        }

        private static void CloseAlliance()
        {
            string PR_CloseAlliance = "</alliance>\n\n";

            File.AppendAllText(battlePath, PR_CloseAlliance);


        }

        private static void OpenEnemyAlliance()
        {
            string PR_OpenAlliance = "<alliance id=\"1\">\n";

            File.AppendAllText(battlePath, PR_OpenAlliance);
        }

        private static void SetBattleDescription(Army army, int total_soldiers)
        {
            switch (army.CombatSide)
            {
                // 0 = player defender 
                // 1 = enemy defender
                case "defender":
                    SetBattleDescription("0", total_soldiers);
                    break;
                case "attacker":
                    SetBattleDescription("1", total_soldiers);
                    break;
                default:
                    SetBattleDescription("1", total_soldiers);
                    break;
            }
        }

        private static void SetBattleDescription(string combat_side, int total_soldiers)
        {
            // 0 = player defender 
            // 1 = enemy defender

            string battleType = "land_normal";
            string fortificationDamageTags = "";
            string subcultureTag = "";
            string battleScript = "tut_start.lua";

            if (twbattle.BattleState.IsSiegeBattle)
            {
                DeclarationsFile.DeclareSiegeVariables();
                int holdingLevel = twbattle.Sieges.GetHoldingLevel();
                if (holdingLevel > 1)
                {
                    battleType = "settlement_standard";
                }
                else
                {
                    battleType = "settlement_unfortified";
                }

                string escalationLevel = twbattle.Sieges.GetHoldingEscalation();
                fortificationDamageTags = Sieges_DataTypes.Fortification.GetFortificationDamageTags(escalationLevel);

                string garrisonCulture = twbattle.Sieges.GetGarrisonCulture();
                string garrisonHeritage = twbattle.Sieges.GetGarrisonHeritage();
                string attilaFaction = UnitMappers_BETA.GetAttilaFaction(garrisonCulture, garrisonHeritage);
                string subculture = UnitMappers_BETA.GetSubculture(attilaFaction);
                if (!string.IsNullOrEmpty(subculture))
                {
                    subcultureTag = $"<subculture>{subculture}</subculture>\n";
                }
                else
                {
                    MessageBox.Show($"Could not determine a subculture for the defending faction '{attilaFaction}'.\n\nThe battle might not load correctly in Total War: Attila.\n\nPlease check your Unit Mapper files.",
                                    "Crusader Conflicts: Subculture Warning",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
            }


            string PR_BattleDescription = "<battle_description>\n" +
                                          $"<battle_script prepare_for_fade_in=\"false\">{battleScript}</battle_script>\n" +
                                          "<time_of_day>day</time_of_day>\n" +
                                          "<season>Summer</season>\n" +
                                          "<precipitation_type>snow</precipitation_type>\n" +
                                          $"<type>{battleType}</type>\n" +
                                          subcultureTag +
                                          ModOptions.TimeLimit() +
                                          $"<timeout_winning_alliance_index>{combat_side}</timeout_winning_alliance_index>\n" +
                                          "<boiling_oil></boiling_oil>\n" +
                                          fortificationDamageTags +
                                          "</battle_description>\n\n";
            
            string PR_PlayableArea = $"<playable_area dimension=\"{ModOptions.SetMapSize(total_soldiers, twbattle.BattleState.IsSiegeBattle)}\"/>\n\n";

            File.AppendAllText(battlePath, PR_BattleDescription + PR_PlayableArea);
        }

        private static void SetBattleTerrain(string X, string Y, string weather_key, string attila_map)
        {
            string PR_BattleTerrain;
            string battleMapDefinitionContent;

            if (twbattle.BattleState.IsSiegeBattle)
            {
                var levelUpgradeTag = twbattle.Sieges.GetSettlementBattleMap();
                string escalationLevel = twbattle.Sieges.GetHoldingEscalation();
                string escalationTag = Sieges_DataTypes.Escalation.GetEscalationTileUpgrade(escalationLevel);

                battleMapDefinitionContent = $"<name>{attila_map}</name>\n" +
                                             $"<tile_map_position x=\"{X}\" y=\"{Y}\">/</tile_map_position>\n" +
                                             $"{levelUpgradeTag}\n" +
                                             $"{escalationTag}\n";
            }
            else
            {
                battleMapDefinitionContent = $"<name>{attila_map}</name>\n" +
                                             $"<tile_map_position x=\"{X}\" y=\"{Y}\">/</tile_map_position>\n";
            }

            PR_BattleTerrain =   "<weather>\n" +
                                        $"<environment_key>{weather_key}</environment_key>\n" +
                                        "<prevailing_wind x=\"1.00\" y=\"0.00\"/>\n" +
                                        "</weather>\n\n" +

                                        "<sea_surface_name>wind_level_4</sea_surface_name>\n\n" +

                                        "<battle_map_definition>\n" +
                                        battleMapDefinitionContent +
                                        "</battle_map_definition>\n\n";

            File.AppendAllText(battlePath, PR_BattleTerrain);
        }

        private static void CloseBattle()
        {
            string PR_CloseBattle = "</battle>\n";

            File.AppendAllText(battlePath, PR_CloseBattle);
        }


    }
}
