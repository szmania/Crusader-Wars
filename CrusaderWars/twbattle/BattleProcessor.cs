using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CrusaderWars.armies;
using CrusaderWars.client;
using CrusaderWars.data.attila_settings;
using CrusaderWars.data.battle_results;
using CrusaderWars.data.save_file;
using CrusaderWars.locs;
using CrusaderWars.mod_manager;
using CrusaderWars.sieges;
using CrusaderWars.terrain;
using CrusaderWars.unit_mapper;
using static CrusaderWars.HomePage; // To access nested static classes like Games, ProcessCommands

namespace CrusaderWars.twbattle
{
    public static class BattleProcessor
    {
        private static readonly Random _random = new Random();
        public static Dictionary<string, (string replacementKey, bool isSiege)> AutofixReplacements { get; private set; } = new Dictionary<string, (string, bool)>();

        public class AutofixState
        {
            public List<string> ProblematicUnitKeys { get; set; }  = new List<string>();
            public int NextUnitKeyIndexToReplace { get; set; } = 0;
            public int FailureCount { get; set; } = 0;
            public string LastAppliedFixDescription { get; set; = "";
            public int MapVariantOffset { get; set; } = 0;
            public bool HasTriedSwitchingToGeneric { get; set; } = false;
            public string OriginalMapDescription { get; set; } = "";
            public string OriginalFieldMapDescription { get; set; } = "";

            // New properties for strategy-based autofix
            public enum AutofixStrategy { MapSize, Deployment, Units, MapVariant, ManualUnitReplacement }
            public AutofixStrategy? CurrentStrategy { get; set; } = null;
            public HashSet<AutofixStrategy> TriedStrategies { get; set; } = new HashSet<AutofixStrategy>();


            // State for individual strategies
            public int MapSizeFixAttempts { get; set; = 0;
            public string? OriginalMapSize { get; set; }
            public bool DeploymentRotationTried { get; set; } = false;
            public int SiegeDirectionFixAttempts { get; set; } = 0;

            // Golden copy of armies for unit replacement strategy
            public List<Army> OriginalAttackerArmies { get; set; } = new List<Army>();
            public List<Army> OriginalDefenderArmies { get; set; } = new List<Army>();
        }

        public static async Task<bool> ProcessBattle(HomePage form, List<Army> attacker_armies, List<Army> defender_armies, CancellationToken token, bool regenerateAndRestart = true, AutofixState? autofixState = null)
        {
            if (autofixState == null)
            {
                AutofixReplacements.Clear(); // Clear fixes for a new battle
                CharacterDataManager.ClearCache(); // Clear dynasty name cache
                UnitMappers_BETA.ClearFactionCache(); // <-- ADD THIS LINE
                BattleStateBridge.Clear();
                BattleState.ClearAutofixOverrides();
            }
            UnitsFile.ResetProcessedArmies(); // Reset tracker for each battle processing attempt.
            var left_side = ArmiesReader.GetSideArmies("left", attacker_armies, defender_armies);
            var right_side = ArmiesReader.GetSideArmies("right", attacker_armies, defender_armies);

            if (left_side is null || !left_side.Any() || right_side is null || !right_side.Any())
            {
                Program.Logger.Debug("Could not determine battle sides or one side is empty. Aborting battle processing.");
                MessageBox.Show(form, "Could not determine player and enemy sides for the battle, or one side has no armies. The battle cannot proceed.", "Crusader Conflicts: Battle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; // break
            }

            if (!BattleState.IsSiegeBattle)
            {
                TerrainGenerator.CheckForSpecialCrossingBattle(attacker_armies, defender_armies);
            }

            int left_side_total = left_side.Sum(army => army.GetTotalSoldiers());
            int right_side_total = right_side.Sum(army => army.GetTotalSoldiers());
            string left_side_combat_side = left_side[0].CombatSide;
            Program.Logger.Debug($"******************** BATTLE SIDES (Original CK3 Sizes) ********************");
            Program.Logger.Debug($"LEFT SIDE ({left_side_combat_side}) TOTAL SOLDIERS: {left_side_total}");
            Program.Logger.Debug($"RIGHT SIDE ({right_side[0].CombatSide}) TOTAL SOLDIERS: {right_side_total}");
            Program.Logger.Debug($"*************************************************************************");

            bool potentialReliefArmy = attacker_armies.Any(a => a.IsReinforcementArmy()) || defender_armies.Any(a => a.IsReinforcementArmy());
            if (potentialReliefArmy)
            {
                Program.Logger.Debug("Potential relief army detected. Searching for corresponding combat block by province ID...");
                BattleResult.ReadCombatBlockByProvinceID();

                if (string.IsNullOrEmpty(BattleResult.Player_Combat))
                {
                    Program.Logger.Debug("No corresponding combat block found. The 'relief' army is not hostile or is retreating. Removing it from the siege battle.");
                    BattleState.HasReliefArmy = false; // This is not a relief army battle

                    // Remove the non-hostile armies from the battle lists
                    int attackersRemoved = attacker_armies.RemoveAll(a => a.IsReinforcementArmy());
                    int defendersRemoved = defender_armies.RemoveAll(a => a.IsReinforcementArmy());
                    Program.Logger.Debug($"Removed {attackersRemoved} attacker reinforcement(s) and {defendersRemoved} defender reinforcement(s).");
                }
                else
                {
                    Program.Logger.Debug("Corresponding combat block found. This is a true relief army siege.");
                    BattleState.HasReliefArmy = true; // This IS a relief army battle
                }
            }
            else
            {
                // No reinforcement armies, so it cannot be a relief army battle.
                BattleState.HasReliefArmy = false;
            }

            if (regenerateAndRestart)
            {
                try
                {
                    Program.Logger.Debug("Clearing previous battle files before regeneration...");
                    BattleFile.ClearFile();
                    DeclarationsFile.Erase();
                    BattleScript.EraseScript(twbattle.BattleState.IsSiegeBattle);
                    Data.units_scripts.Clear();
                    BattleResult.ClearAttilaLog();
                    UnitsCardsNames.RemoveFiles();
                    Program.Logger.Debug("Previous battle files cleared.");

                    Program.Logger.Debug("Creating TW:Attila battle files.");
                    BattleDetails.ChangeBattleDetails(left_side_total, right_side_total, left_side_combat_side, right_side[0].CombatSide);

                    await Games.CloseTotalWarAttilaProcess();
                    form.UpdateLoadingScreenMessage("Creating battle in Total War: Attila...");

                    //Create Remaining Soldiers Script
                    Program.Logger.Debug("Creating battle script...");
                    BattleScript.CreateScript();

                    // Set Battle Scale
                    int total_soldiers = attacker_armies.SelectMany(army => army.Units).Sum(unit => unit.GetSoldiers()) +
                                         defender_armies.SelectMany(army => army.Units).Sum(unit => unit.GetSoldiers());
                    Program.Logger.Debug($"Total soldiers for battle scale calculation: {total_soldiers}");
                    ArmyProportions.AutoSizeUnits(total_soldiers);
                    Program.Logger.Debug($"Applying battle scale: {ModOptions.GetBattleScale()}");
                    foreach (var army in attacker_armies) army.ScaleUnits(ModOptions.GetBattleScale());
                    foreach (var army in defender_armies) army.ScaleUnits(ModOptions.GetBattleScale());

                    BattleLog.Reset();
                    // Add battle log header
                    try
                    {
                        var header = new StringBuilder();
                        header.AppendLine();
                        header.AppendLine("=========================================================================");
                        header.AppendLine($"BATTLE LOG ENTRY: {DateTime.Now}");
                        header.AppendLine("=========================================================================");

                        string battleYear = Date.Year.ToString();
                        string leftRealm = CK3LogData.LeftSide.GetRealmName();
                        string rightRealm = CK3LogData.RightSide.GetRealmName();
                        string battleType = BattleState.IsSiegeBattle ? "Siege Battle" : "Field Battle";
                        string province = BattleResult.ProvinceName ?? "Unknown Province";
                        string terrain = TerrainGenerator.TerrainType ?? "Unknown Terrain";

                        header.AppendLine($"Year: {battleYear}");
                        header.AppendLine($"Battle Type: {battleType}");
                        header.AppendLine($"Location: {province} ({terrain})");
                        header.AppendLine($"Belligerents: {leftRealm} (Attackers in log) vs. {rightRealm} (Defenders in log)");
                        header.AppendLine($"Initial Strength: {left_side_total} vs. {right_side_total}");
                        header.AppendLine("-------------------------------------------------------------------------");
                        header.AppendLine("--- PRE-BATTLE ARMY COMPOSITION (Scaled for Attila) ---");
                        header.AppendLine("-------------------------------------------------------------------------");
                        File.AppendAllText(@".\data\battle.log", header.ToString());
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Debug($"Failed to write battle log header: {ex.Message}");
                    }
                    //Create Battle
                    Program.Logger.Debug("Creating battle file...");
                    BattleFile.BETA_CreateBattle(attacker_armies, defender_armies);

                    // NEW: Check for unmapped units and show alert
                    if (BattleLog.HasUnmappedUnits())
                    {
                        var unmappedUnits = BattleLog.GetUnmappedUnits();

                        bool siegeEnginesInFieldBattles = !ModOptions.optionsValuesCollection.TryGetValue("SiegeEnginesInFieldBattles", out string? siegeEnginesOption) || siegeEnginesOption == "Enabled";
                        
                        var displayableUnmappedUnits = unmappedUnits.Distinct().ToList();

                        if (!BattleState.IsSiegeBattle && !siegeEnginesInFieldBattles)
                        {
                            displayableUnmappedUnits = displayableUnmappedUnits
                                .Where(u => !UnitMappers_BETA.IsUnitTypeSiege((RegimentType)Enum.Parse(typeof(RegimentType), u.RegimentType), u.UnitName, u.AttilaFaction))
                                .ToList();
                        }

                        if (displayableUnmappedUnits.Any())
                        {
                            var sb = new System.Text.StringBuilder();
                            sb.AppendLine("Warning: Some CK3 units could not be mapped to Total War: Attila units and were dropped from the battle.");
                            sb.AppendLine("This usually means a unit from a CK3 mod is not supported by the active Unit Mapper playthrough.");
                            sb.AppendLine();
                            sb.AppendLine("Unmapped Units:");
                            foreach (var u in displayableUnmappedUnits)
                            {
                                sb.AppendLine($" - Type: {u.RegimentType}, Name: {u.UnitName}, Faction: {u.AttilaFaction} (Culture: {u.Culture})");
                            }
                            sb.AppendLine();
                            sb.AppendLine("Please report this bug to the Crusader Conflicts Development Team on our Discord server.");
                            sb.AppendLine();
                            sb.AppendLine("The battle will proceed without these units.");

                            form.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                                string messageText = sb.ToString();
                                string discordUrl = "https://discord.gg/eFZTprHh3j";
                                ShowClickableLinkMessageBox(form, messageText, "Crusader Conflicts: Unit Mapping Warning", "Report on Discord: " + discordUrl, discordUrl);
                            });
                        }
                    }

                    //Close Script
                    BattleScript.CloseScript();

                    //Set Commanders Script
                    Program.Logger.Debug("Setting commanders in script...");
                    BattleScript.SetCommandersLocals();

                    //Set Units Kills Script
                    Program.Logger.Debug("Setting unit kill trackers in script...");
                    BattleScript.SetLocalsKills(Data.units_scripts);

                    //Close Script
                    BattleScript.CloseScript();

                    Program.Logger.Debug("--- FINAL ATTACKER ARMY COMPOSITION FOR ATTILA ---");
                    foreach (var army in attacker_armies)
                    {
                        Program.Logger.Debug($"Army ID: {army.ID}, Total Soldiers: {army.GetTotalSoldiers()}");
                        if (army.Units != null)
                        {
                            bool leviesLogged = false; // Flag to ensure levies are logged only once per army
                            foreach (var unit in army.Units)
                            {
                                string unitDetails = $", Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {unit.GetAttilaFaction()}";

                                if (unit.GetRegimentType() == RegimentType.Levy)
                                {
                                    if (!leviesLogged)
                                    {
                                        int totalLevySoldiers = army.Units.Where(u => u.GetRegimentType() == RegimentType.Levy).Sum(u => u.GetSoldiers());
                                        Program.Logger.Debug($"  - Unit: (All Levies), CK3 Type: Levy, Total Soldiers: {totalLevySoldiers}");
                                        var levyDetails = BattleLog.GetLevyBreakdown(army.ID);
                                        foreach (var detail in levyDetails)
                                        {
                                            Program.Logger.Debug(detail);
                                        }
                                        leviesLogged = true;
                                    }
                                }
                                else
                                {
                                    string attilaKey = unit.GetAttilaUnitKey();
                                    if (string.IsNullOrEmpty(attilaKey) || attilaKey == UnitMappers_BETA.NOT_FOUND_KEY)
                                    {
                                        Program.Logger.Debug($"  - DROPPED: Could not map CK3 Unit '{unit.GetName()}' (Type: {unit.GetRegimentType()}). All mapping attempts failed.{unitDetails}");
                                    }
                                    else
                                    {
                                        if (unit.GetRegimentType() == RegimentType.Commander)
                                        {
                                            // Only log the main commander, which is represented by the "General" unit.
                                            if (unit.GetName() == "General")
                                            {
                                                string commanderName = army.Commander?.Name ?? "Unknown Commander";
                                                string rankInfo = $", Rank: {unit.CharacterRank}";
                                                Program.Logger.Debug($"  - CK3 Unit: General ({commanderName}), Type: Commander, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}");
                                            }
                                            // Implicitly skip logging other commander-type units that might be in the list.
                                        }
                                        else // Not a commander
                                        {
                                            string rankInfo = "";
                                            if (unit.GetRegimentType() == RegimentType.Knight)
                                            {
                                                rankInfo = $", Rank: {unit.CharacterRank}";
                                            }
                                            Program.Logger.Debug($"  - CK3 Unit: {unit.GetName()}, Type: {unit.GetRegimentType()}, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (twbattle.BattleState.IsSiegeBattle)
                    {
                        int attackerArmySize = attacker_armies.Sum(a => a.GetTotalSoldiers());
                        var siegeEngines = SiegeEngineGenerator.Generate(attacker_armies); // Pass attacker_armies
                        if (siegeEngines != null && siegeEngines.Any())
                        {
                            Program.Logger.Debug("  --- Siege Engines ---");
                            foreach (var engine in siegeEngines)
                            {
                                Program.Logger.Debug($"  - Type: {engine.Key}, Quantity: {engine.Value}");
                            }
                        }
                    }
                    Program.Logger.Debug($"TOTAL ATTACKER SOLDIERS: {attacker_armies.Sum(a => a.GetTotalSoldiers())}");
                    Program.Logger.Debug("--------------------------------------------------");

                    Program.Logger.Debug("--- FINAL DEFENDER ARMY COMPOSITION FOR ATTILA ---");
                    foreach (var army in defender_armies)
                    {
                        Program.Logger.Debug($"Army ID: {army.ID}, Total Soldiers: {army.GetTotalSoldiers()}");
                        if (army.Units != null)
                        {
                            bool leviesLogged = false; // Flag to ensure levies are logged only once per army
                            foreach (var unit in army.Units)
                            {
                                string unitDetails = $", Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {unit.GetAttilaFaction()}";

                                if (unit.GetRegimentType() == RegimentType.Levy)
                                {
                                    if (!leviesLogged)
                                    {
                                        int totalLevySoldiers = army.Units.Where(u => u.GetRegimentType() == RegimentType.Levy).Sum(u => u.GetSoldiers());
                                        Program.Logger.Debug($"  - Unit: (All Levies), CK3 Type: Levy, Total Soldiers: {totalLevySoldiers}");
                                        var levyDetails = BattleLog.GetLevyBreakdown(army.ID);
                                        foreach (var detail in levyDetails)
                                        {
                                            Program.Logger.Debug(detail);
                                        }
                                        leviesLogged = true;
                                    }
                                }
                                else
                                {
                                    string attilaKey = unit.GetAttilaUnitKey();
                                    if (string.IsNullOrEmpty(attilaKey) || attilaKey == UnitMappers_BETA.NOT_FOUND_KEY)
                                    {
                                        Program.Logger.Debug($"  - DROPPED: Could not map CK3 Unit '{unit.GetName()}' (Type: {unit.GetRegimentType()}). All mapping attempts failed.{unitDetails}");
                                    }
                                    else
                                    {
                                        if (unit.GetRegimentType() == RegimentType.Commander)
                                        {
                                            // Only log the main commander, which is represented by the "General" unit.
                                            if (unit.GetName() == "General")
                                            {
                                                string commanderName = army.Commander?.Name ?? "Unknown Commander";
                                                string rankInfo = $", Rank: {unit.CharacterRank}";
                                                Program.Logger.Debug($"  - CK3 Unit: General ({commanderName}), Type: Commander, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}");
                                            }
                                            // Implicitly skip logging other commander-type units that might be in the list.
                                        }
                                        else // Not a commander
                                        {
                                            string rankInfo = "";
                                            if (unit.GetRegimentType() == RegimentType.Knight)
                                            {
                                                rankInfo = $", Rank: {unit.CharacterRank}";
                                            }
                                            Program.Logger.Debug($"  - CK3 Unit: {unit.GetName()}, Type: {unit.GetRegimentType()}, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    Program.Logger.Debug($"TOTAL DEFENDER SOLDIERS: {defender_armies.Sum(a => a.GetTotalSoldiers())}");
                    Program.Logger.Debug("--------------------------------------------------");
                    //Creates .pack mod file
                    Program.Logger.Debug("Creating .pack file...");
                    PackFile.PackFileCreator();
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error creating Attila battle: {ex.Message}");
                    form.Show();
                    form.CloseLoadingScreen();
                    MessageBox.Show(form, $"Error creating the battle: {ex.Message}", "Crusader Conflicts: Data Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                    if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.StartCrusaderKingsProcess();
                    }
                    else
                    {
                        ProcessCommands.ResumeProcess();
                    }
                    form.infoLabel.Text = "Waiting for CK3 battle...";
                    form.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

                    //Data Clear
                    Data.Reset();

                    return true; // Continue
                }

                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("Skipping Attila launch due to cancellation.");
                    return false;
                }

                try
                {
                    // Check for user.script.txt conflict before launching Attila
                    if (!AttilaPreferences.ValidateBeforeLaunch())
                    {
                        Program.Logger.Debug("Aborting Attila launch due to user script conflict.");
                        form.Show();
                        form.CloseLoadingScreen();
                        ProcessCommands.ResumeProcess();
                        form.infoLabel.Text = "Waiting for CK3 battle...";
                        form.Text = "Crusader Conflicts (Waiting for CK3 battle...)";
                        Data.Reset();
                        return true; // Continue
                    }

                    //Open Total War Attila
                    Program.Logger.Debug("Starting Total War: Attila process via shortcut...");
                    Games.StartTotalWArAttilaProcess();
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error starting Attila: {ex.Message}");
                    form.Show();
                    form.CloseLoadingScreen();
                    MessageBox.Show(form, "Couldn't find 'Attila.exe'. Change the Total War Attila path. ", "Crusader Conflicts: Path Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                    form.infoLabel.Text = "Ready to start!";
                    if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.StartCrusaderKingsProcess();
                    }
                    else
                    {
                        ProcessCommands.ResumeProcess();
                    }
                    form.ExecuteButton.Enabled = true;
                    form.Text = "Crusader Conflicts";
                    return false; // Break
                }
            }
            else
            {
                Program.Logger.Debug("Skipping battle file regeneration and Attila restart. Using current session.");
            }


            try
            {
                DataSearch.ClearLogFile();
                DeclarationsFile.Erase();
                BattleScript.EraseScript(twbattle.BattleState.IsSiegeBattle);
                BattleResult.ClearAttilaLog();

                form.CloseLoadingScreen();
                form.Show();

            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error during cleanup before battle: {ex.Message}");
                MessageBox.Show(form, $"Error: {ex.Message}", "Crusader Conflicts: Application Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                await Games.CloseTotalWarAttilaProcess();
                if (ModOptions.CloseCK3DuringBattle())
                {
                    Games.StartCrusaderKingsProcess();
                }
                else
                {
                    ProcessCommands.ResumeProcess();
                }
                form.infoLabel.Text = "Waiting for CK3 battle...";
                form.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

                //Data Clear
                Data.Reset();
                return true; // Continue
            }


            if (ModOptions.CloseCK3DuringBattle())
            {
                        Games.CloseCrusaderKingsProcess();
            }

            Program.Logger.Debug("TW:Attila battle created successfully");

            //               Retrieve battle result to ck3
            //-----------------------------------------------------------
            //                       Battle Results                     |
            //-----------------------------------------------------------

            string attilaLogPath = Properties.Settings.Default.VAR_log_attila;

            bool battleEnded = false;

            form.infoLabel.Text = "Waiting for TW:Attila battle to end...";
            form.Text = "Crusader Conflicts (Waiting for TW:Attila battle to end...)";
            Program.Logger.Debug("Waiting for TW:Attila battle to end...");

            form.ExecuteButton.Enabled = true;
            if (form.ExecuteButton.Enabled)
            {
                form.ExecuteButton.BackgroundImage = Properties.Resources.start_new;
            }
            form.ContinueBattleButton.Enabled = true;

            //  Waiting for TW:Attila battle to end...
            while (battleEnded == false)
            {
                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("Battle monitoring cancelled by user action.");
                    return false; // Indicate cancellation
                }
                // Check if Attila process is still running
                if (Process.GetProcessesByName("Attila").Length == 0)
                {
                    Program.Logger.Debug("Attila process not found. Checking if it was a crash.");

                    // If battle ended normally, it's not a crash.
                    if (BattleResult.HasBattleEnded(attilaLogPath))
                    {
                        Program.Logger.Debug("Battle log indicates a normal exit. Proceeding with results.");
                        battleEnded = true;
                        continue; // Continue to the result processing part of the loop
                    }

                    Program.Logger.Debug("Attila process terminated without a complete battle log. Presumed crash.");

                    // --- Autofix Logic ---
                    if (autofixState == null) // First crash
                    {
                        Program.Logger.Debug("First crash detected. Initializing autofix state.");
                        autofixState = new AutofixState();
                        autofixState.OriginalAttackerArmies = new List<Army>(attacker_armies);
                        autofixState.OriginalDefenderArmies = new List<Army>(defender_armies);

                        // Build a comprehensive list of all possible custom unit keys in the battle.
                        var allArmiesForKeys = attacker_armies.Concat(defender_armies);
                        var allUnitKeys = new HashSet<string>();
                        var allFactions = new HashSet<string>();

                        foreach (var army in allArmiesForKeys)
                        {
                            if (army.Units == null) continue;
                            foreach (var unit in army.Units)
                            {
                                string faction = unit.GetAttilaFaction();
                                if (!string.IsNullOrEmpty(faction) && faction != UnitMappers_BETA.NOT_FOUND_KEY)
                                {
                                    allFactions.Add(faction);
                                }
                                var unitType = unit.GetRegimentType();
                                if (unitType == RegimentType.Commander || unitType == RegimentType.Knight || unitType == RegimentType.MenAtArms)
                                {
                                    string key = unit.GetAttilaUnitKey();
                                    if (!string.IsNullOrEmpty(key) && key != UnitMappers_BETA.NOT_FOUND_KEY)
                                    {
                                        allUnitKeys.Add(key);
                                    }
                                }
                            }
                        }
                        foreach (var faction in allFactions)
                        {
                            var (levy_porcentages, _) = UnitMappers_BETA.GetFactionLevies(faction);
                            if (levy_porcentages != null)
                            {
                                foreach (var levyData in levy_porcentages)
                                {
                                    allUnitKeys.Add(levyData.unit_key);
                                }
                            }
                            for (int level = 1; level <= 20; level++)
                            {
                                var garrison_porcentages = UnitMappers_BETA.GetFactionGarrison(faction, level);
                                if (garrison_porcentages != null)
                                    {
                                    foreach (var garrisonData in garrison_porcentages)
                                    {
                                        allUnitKeys.Add(garrisonData.unit_key);
                                    }
                                }
                            }
                        }

                        autofixState.ProblematicUnitKeys = allUnitKeys
                            .Where(key => !string.IsNullOrEmpty(key) && key != UnitMappers_BETA.NOT_FOUND_KEY)
                            .Distinct()
                            .OrderBy(key => _random.Next())
                            .ToList();

                        if (!autofixState.ProblematicUnitKeys.Any())
                        {
                            Program.Logger.Debug("Autofix initiated, but no potentially problematic custom units were found to replace.");
                            form.Invoke((MethodInvoker)delegate
                            {
                                MessageBox.Show(form,
                                    "The autofix process could not find any replaceable custom units in the armies.\n\nThe crash may be caused by something else (e.g., a game bug, outdated drivers, or a corrupted installation).\n\nThe battle will be aborted.",
                                    "Crusader Conflicts: Autofix Failed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            });
                            return false;
                        }
                        Program.Logger.Debug($"Found {autofixState.ProblematicUnitKeys.Count} unique unit keys to test: {string.Join(", ", autofixState.ProblematicUnitKeys)}");
                    }

                    autofixState.FailureCount++;
                    Program.Logger.Debug($"Crash detected. Failure count: {autofixState.FailureCount}.");

                    // --- Main Autofix Control Flow ---

                    // 1. If no strategy is active, get one from the user.
                    if (autofixState.CurrentStrategy == null)
                    {
                        Program.Logger.Debug("No active autofix strategy. Prompting user.");

                        var allStrategies = new List<AutofixState.AutofixStrategy>
                        {
                            AutofixState.AutofixStrategy.Units,
                            AutofixState.AutofixStrategy.MapSize,
                            AutofixState.AutofixStrategy.Deployment,
                            AutofixState.AutofixStrategy.MapVariant,
                            AutofixState.AutofixStrategy.ManualUnitReplacement
                        };

                        if (BattleState.IsSiegeBattle)
                        {
                            allStrategies.Remove(AutofixState.AutofixStrategy.MapSize);
                        }

                        var availableStrategies = allStrategies.Except(autofixState.TriedStrategies).ToList();

                        if (!availableStrategies.Any())
                        {
                            Program.Logger.Debug("Autofix failed. All strategies have been attempted.");
                            form.Invoke((MethodInvoker)delegate
                            {
                                MessageBox.Show(form,
                                    "The automatic fix failed. All available strategies were tried, but the game still crashed.\n\nThe battle will be aborted.",
                                    "Crusader Conflicts: Autofix Failed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                            });
                            return false; // All strategies exhausted
                        }

                        DialogResult userResponse = DialogResult.No;
                        AutofixState.AutofixStrategy? chosenStrategy = null;

                        form.Invoke((MethodInvoker)delegate
                        {
                            (userResponse, chosenStrategy) = ShowAutofixStrategyChoicePrompt(form, availableStrategies);
                        });

                        if (userResponse == DialogResult.No || chosenStrategy == null)
                        {
                            Program.Logger.Debug("User declined autofix. Aborting battle.");
                            return false; // User cancelled
                        }

                        autofixState.CurrentStrategy = chosenStrategy;
                        Program.Logger.Debug($"User chose autofix strategy: {chosenStrategy.Value}.");
                    }

                    form.Invoke((MethodInvoker)delegate
                    {
                        if (autofixState.CurrentStrategy.HasValue)
                        {
                            switch (autofixState.CurrentStrategy.Value)
                            {
                                case AutofixState.AutofixStrategy.Units:
                                    form.infoLabel.Text = "Processing: Analyzing units for replacement...";
                                    break;
                                case AutofixState.AutofixStrategy.MapSize:
                                    form.infoLabel.Text = "Processing: Changing map size...";
                                    break;
                                case AutofixState.AutofixStrategy.Deployment:
                                    form.infoLabel.Text = "Processing: Changing deployment...";
                                    break;
                                case AutofixState.AutofixStrategy.MapVariant:
                                    form.infoLabel.Text = "Processing: Changing map variant...";
                                    break;
                                case AutofixState.AutofixStrategy.ManualUnitReplacement:
                                    form.infoLabel.Text = "Loading manual unit replacer...";
                                    break;
                            }
                        }
                    });

                    // 2. A strategy is active. Try to apply a fix.
                    bool fixApplied = false;
                    string fixDescription = "";

                    // --- Determine Original Map Size (needed for MapSize fix) ---
                    string originalMapSize;
                    string option_map_size = ModOptions.DeploymentsZones();
                    if (option_map_size == "Dynamic")
                    {
                        var (fresh_attackers_for_size, fresh_defenders_for_size) = ArmiesReader.ReadBattleArmies();
                        int total_soldiers = fresh_attackers_for_size.Sum(a => a.GetTotalSoldiers()) + fresh_defenders_for_size.Sum(a => a.GetTotalSoldiers());

                        if (twbattle.BattleState.IsSiegeBattle)
                        {
                            int holdingLevel = Sieges.GetHoldingLevel();
                            if (holdingLevel <= 2) { originalMapSize = "Medium"; }
                            else if (holdingLevel <= 4) { originalMapSize = "Big"; }
                            else { originalMapSize = "Huge"; }
                        }
                        else // Field battle
                        {
                            if (total_soldiers <= 5000) { originalMapSize = "Medium"; }
                            else if (total_soldiers > 5000 && total_soldiers < 20000) { originalMapSize = "Big"; }
                            else if (total_soldiers >= 20000) { originalMapSize = "Huge"; }
                            else { originalMapSize = "Medium"; }
                        }
                    }
                    else
                    {
                        originalMapSize = option_map_size;
                    }
                    Program.Logger.Debug($"Autofix: Original map size determined as '{originalMapSize}'.");


                    switch (autofixState.CurrentStrategy)
                    {
                        case AutofixState.AutofixStrategy.MapSize:
                            (fixApplied, fixDescription) = TryMapSizeFix(autofixState, originalMapSize);
                            break;
                        case AutofixState.AutofixStrategy.Deployment:
                            (fixApplied, fixDescription) = TryDeploymentFix(autofixState);
                            break;
                        case AutofixState.AutofixStrategy.Units:
                            (fixApplied, fixDescription) = TryUnitFix(autofixState, form);
                            break;
                        case AutofixState.AutofixStrategy.MapVariant:
                            (fixApplied, fixDescription) = TryMapVariantFix(autofixState);
                            break;
                        case AutofixState.AutofixStrategy.ManualUnitReplacement:
                            (fixApplied, fixDescription) = TryManualUnitFix(autofixState, form);
                            break;
                    }

                    // 3. Handle the result of the fix attempt.
                    if (fixApplied)
                    {
                        autofixState.LastAppliedFixDescription = fixDescription;
                        form.Invoke((MethodInvoker)delegate
                        {
                            form.infoLabel.Text = $"Attila crashed. Attempting automatic fix #{autofixState.FailureCount}...";
                            form.Text = $"Crusader Conflicts (Attempting fix #{autofixState.FailureCount})";
                        });

                        form.Invoke((MethodInvoker)delegate
                        {
                            string messageText = $"Attempting automatic fix #{autofixState.FailureCount}.\n\nThe application will now try {fixDescription} and restart the battle.\n\nPlease note this information if you plan to report a bug on our Discord server:";
                            string discordUrl = "https://discord.gg/eFZTprHh3j";
                            ShowClickableLinkMessageBox(form, messageText, "Crusader Conflicts: Applying Autofix", "Report on Discord: " + discordUrl, fixDescription);
                        });

                        // Mark the current strategy as tried and clear it for the next attempt
                        if (autofixState.CurrentStrategy.HasValue)
                        {
                            autofixState.TriedStrategies.Add(autofixState.CurrentStrategy.Value);
                            autofixState.CurrentStrategy = null;
                        }

                        Program.Logger.Debug($"Relaunching battle after autofix ({fixDescription}). Re-reading army data from save files to apply changes.");
                        var (fresh_attackers, fresh_defenders) = ArmiesReader.ReadBattleArmies();
                        return await ProcessBattle(form, fresh_attackers, fresh_defenders, token, true, autofixState);
                    }
                    else
                    {
                        // The current strategy is exhausted or cancelled.
                        Program.Logger.Debug($"--- Autofix: Strategy {autofixState.CurrentStrategy} is complete for this attempt. ---");
                        if (autofixState.CurrentStrategy.HasValue && autofixState.CurrentStrategy.Value != AutofixState.AutofixStrategy.ManualUnitReplacement)
                        {
                            autofixState.TriedStrategies.Add(autofixState.CurrentStrategy.Value);
                        }
                        autofixState.CurrentStrategy = null;
                        // The `while(battleEnded == false)` loop will continue. Since the process is still dead,
                        // it will re-enter this entire `if` block on the next iteration.
                        // Because `CurrentStrategy` is now null, it will re-prompt the user.
                    }
                }

                battleEnded = BattleResult.HasBattleEnded(attilaLogPath);
                await Task.Delay(1000); // Check every second
            }
            Program.Logger.Debug("TW:Attila battle ended.");


            try
            {
                if (battleEnded)
                {
                    Program.Logger.Debug("Processing TW:Attila battle results.");
                    ModOptions.CloseAttila();

                    form.infoLabel.Text = "Processing TW:Attila battle results...";
                    form.Text = "Crusader Conflicts (Processing results)";

                    string path_log_attila = Properties.Settings.Default.VAR_log_attila;

                    // --- START: Capture pre-battle state ---
                    Dictionary<string, int> originalAttackerSizes = new Dictionary<string, int>();
                    foreach (var army in attacker_armies)
                    {
                        if (army.ArmyRegiments != null)
                        {
                            foreach (var armyRegiment in army.ArmyRegiments)
                            {
                                if (armyRegiment == null || armyRegiment.Type == RegimentType.Commander || armyRegiment.Type == RegimentType.Knight) continue;
                                if (armyRegiment.Regiments != null)
                                {
                                    foreach (var regiment in armyRegiment.Regiments)
                                    {
                                        if (regiment == null || string.IsNullOrEmpty(regiment.CurrentNum)) continue;
                                        string key = $"{army.ID}_{regiment.ID}";
                                        if (!originalAttackerSizes.ContainsKey(key))
                                        {
                                            originalAttackerSizes.Add(key, Int32.Parse(regiment.CurrentNum));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Dictionary<string, int> originalDefenderSizes = new Dictionary<string, int>();
                    foreach (var army in defender_armies)
                    {
                        if (army.ArmyRegiments != null)
                        {
                            foreach (var armyRegiment in army.ArmyRegiments)
                            {
                                if (armyRegiment == null || armyRegiment.Type == RegimentType.Commander || armyRegiment.Type == RegimentType.Knight) continue;
                                if (armyRegiment.Regiments != null)
                                {
                                    foreach (var regiment in armyRegiment.Regiments)
                                    {
                                        if (regiment == null || string.IsNullOrEmpty(regiment.CurrentNum)) continue;
                                        string key = $"{army.ID}_{regiment.ID}";
                                        if (!originalDefenderSizes.ContainsKey(key))
                                        {
                                            originalDefenderSizes.Add(key, Int32.Parse(regiment.CurrentNum));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // --- END: Capture pre-battle state ---
                    int originalTotalAttackerSoldiers = attacker_armies.Sum(a => a.GetTotalSoldiers());

                    //  SET CASUALITIES
                    Program.Logger.Debug("Setting casualties for attacker armies...");
                    foreach (var army in attacker_armies)
                    {
                        Program.Logger.Debug($"Processing army ID: {army.ID}");
                        BattleResult.ReadAttilaResults(army, path_log_attila);
                        BattleResult.CheckForSlainCommanders(army, path_log_attila);
                        BattleResult.CheckKnightsKills(army);
                        BattleResult.CheckForSlainKnights(army);
                    }
                    Program.Logger.Debug("Setting casualties for defender armies...");
                    foreach (var army in defender_armies)
                    {
                        Program.Logger.Debug($"Processing army ID: {army.ID}");
                        BattleResult.ReadAttilaResults(army, path_log_attila);
                        BattleResult.CheckForSlainCommanders(army, path_log_attila);
                        BattleResult.CheckKnightsKills(army);
                        BattleResult.CheckForSlainKnights(army);

                    }

                    // --- START: Call new logging method ---
                    BattleResult.LogPostBattleReport(attacker_armies, originalAttackerSizes, "ATTACKER");
                    if (twbattle.BattleState.IsSiegeBattle)
                    {
                        var siegeEngines = SiegeEngineGenerator.Generate(attacker_armies); // Pass attacker_armies
                        if (siegeEngines != null && siegeEngines.Any())
                        {
                            Program.Logger.Debug("  --- Siege Engines ---");
                            foreach (var engine in siegeEngines)
                            {
                                Program.Logger.Debug($"  - Type: {engine.Key}, Quantity: {engine.Value}");
                            }
                        }
                    }
                    BattleResult.LogPostBattleReport(defender_armies, originalDefenderSizes, "DEFENDER");
                    // --- END: Call new logging method ---

                    // DETERMINE WINNER FIRST so EditLivingFile can correctly calculate prisoner chances
                    string winner = BattleResult.GetAttilaWinner(path_log_attila, left_side[0].CombatSide, right_side[0].CombatSide);
                    BattleResult.IsAttackerVictorious = (winner == "attacker");
                    Program.Logger.Debug($"Battle winner determined: {winner}. IsAttackerVictorious set to: {BattleResult.IsAttackerVictorious}");


                    //  EDIT LIVING FILE
                    Program.Logger.Debug("Editing Living.txt file...");
                    BattleResult.EditLivingFile(attacker_armies, defender_armies);

                    if (!twbattle.BattleState.IsSiegeBattle || (twbattle.BattleState.IsSiegeBattle && twbattle.BattleState.HasReliefArmy))
                    {
                        // Field Battle OR Siege with Relief Army: Edit files as normal
                        Program.Logger.Debug("Field battle or siege with relief army detected. Editing combat files...");
                        
                        var mobile_attacker_armies = attacker_armies.Where(a => !a.IsGarrison()).ToList();
                        var mobile_defender_armies = defender_armies.Where(a => !a.IsGarrison()).ToList();

                        //  EDIT COMBATS FILE
                        BattleResult.EditCombatFile(mobile_attacker_armies, mobile_defender_armies);
                        //  EDIT COMBATS RESULTS FILE
                        BattleResult.EditCombatResultsFile(mobile_attacker_armies, mobile_defender_armies);
                    }
                    else
                    {
                        // Standard Siege Battle (no relief army): Skip editing and copy original files to temp to prevent corruption
                        Program.Logger.Debug("Standard siege battle (no relief army) detected. Skipping modification of Combats.txt and CombatResults.txt.");

                        string combatsSourcePath = CrusaderWars.data.save_file.Writter.DataFilesPaths.Combats_Path();
                        string combatsDestPath = CrusaderWars.data.save_file.Writter.DataTEMPFilesPaths.Combats_Path();
                        if (File.Exists(combatsSourcePath))
                        {
                            File.Copy(combatsSourcePath, combatsDestPath, true);
                            Program.Logger.Debug("Copied original Combats.txt to temp folder.");
                        }

                        string combatResultsSourcePath = CrusaderWars.data.save_file.Writter.DataFilesPaths.CombatResults_Path();
                        string combatResultsDestPath = CrusaderWars.data.save_file.Writter.DataTEMPFilesPaths.CombatResults_Path();
                        if (File.Exists(combatResultsSourcePath))
                        {
                            File.Copy(combatResultsSourcePath, combatResultsDestPath, true);
                            Program.Logger.Debug("Copied original CombatResults.txt to temp folder.");
                        }
                    }

                    //  EDIT REGIMENTS FILE
                    Program.Logger.Debug("Editing Regiments.txt file...");
                    BattleResult.EditRegimentsFile(attacker_armies, defender_armies);

                    //  EDIT ARMY REGIMENTS FILE
                    Program.Logger.Debug("Editing ArmyRegiments.txt file...");
                    BattleResult.EditArmyRegimentsFile(attacker_armies, defender_armies);

                    //  EDIT SIEGES FILE
                    if (twbattle.BattleState.IsSiegeBattle)
                    {
                        Program.Logger.Debug("Editing Sieges.txt file...");
                        BattleResult.EditSiegesFile(path_log_attila, left_side[0].CombatSide, right_side[0].CombatSide, attacker_armies, defender_armies);
                    }
                    else
                    {
                        // For non-siege battles, copy the original sieges data to the temp file
                        // to ensure it's written back to the save file unmodified.
                        string sourcePath = CrusaderWars.data.save_file.Writter.DataFilesPaths.Sieges_Path();
                        string destPath = CrusaderWars.data.save_file.Writter.DataTEMPFilesPaths.Sieges_Path();
                        if (File.Exists(sourcePath))
                        {
                            File.Copy(sourcePath, destPath, true);
                            Program.Logger.Debug("Copied original Sieges.txt to temp folder for non-siege battle.");
                        }
                    }

                    //  WRITE TO CK3 SAVE FILE
                    Program.Logger.Debug("Writing results to gamestate file...");
                    BattleResult.SendToSaveFile(form.path_editedSave);

                    // Add battle log footer
                    try
                    {
                        var footer = new StringBuilder();
                        footer.AppendLine("-------------------------------------------------------------------------");
                        footer.AppendLine("--- BATTLE COMPLETE ---");
                        footer.AppendLine("=========================================================================");
                        footer.AppendLine();
                        File.AppendAllText(@".\data\battle.log", footer.ToString());
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Debug($"Failed to write battle log footer: {ex.Message}");
                    }

                    //  COMPRESS CK3 SAVE FILE AND SEND TO CK3 SAVE FILE FOLDER
                    Program.Logger.Debug("Compressing new save file...");
                    SaveFile.Compress();
                    Program.Logger.Debug("Finalizing save file...");
                    SaveFile.Finish();

                    // Show successful autofix message if applicable
                    if (autofixState != null && !string.IsNullOrEmpty(autofixState.LastAppliedFixDescription))
                    {
                        string messageText = $"The battle was successful after an automatic fix.\n\nThe fix that worked was: {autofixState.LastAppliedFixDescription}.\n\n";
                        string originalMapInfo = "";
                        if (!string.IsNullOrEmpty(autofixState.OriginalMapDescription))
                        {
                            messageText += $"The original map ({autofixState.OriginalMapDescription}) is likely buggy. ";
                            originalMapInfo = autofixState.OriginalMapDescription;
                        }
                        else if (!string.IsNullOrEmpty(autofixState.OriginalFieldMapDescription))
                        {
                            messageText += $"The original map ({autofixState.OriginalFieldMapDescription}) is likely buggy. ";
                            originalMapInfo = autofixState.OriginalFieldMapDescription;
                        }
                        messageText += "Please report this on the Crusader Conflicts Discord server so it can be fixed in future updates.";
                        string discordUrl = "https://discord.gg/eFZTprHh3j";

                        form.Invoke((MethodInvoker)delegate {
                            ShowClickableLinkMessageBox(form, messageText, "Crusader Conflicts: Autofix Successful", "Report on Discord: " + discordUrl, autofixState.LastAppliedFixDescription, originalMapInfo);
                        });
                    }

                    //  OPEN CK3 WITH BATTLE RESULTS
                    if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.LoadBattleResults();
                    }
                    else
                    {
                        ProcessCommands.ResumeProcess();
                    }

                    form.Text = "Crusader Conflicts (Battle Complete)";
                    form.battleJustCompleted = true;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error retrieving TW:Attila battle results: {ex.Message}");
                MessageBox.Show(form, $"Error retrieving TW:Attila battle results: {ex.Message}", "Crusader Conflicts: TW:Attila Battle Results Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                await Games.CloseTotalWarAttilaProcess();
                if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.StartCrusaderKingsProcess();
                    }
                    else
                    {
                        ProcessCommands.ResumeProcess();
                    }
                form.infoLabel.Text = "Waiting for CK3 battle...";
                form.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

                //Data Clear
                Data.Reset();
                return true; // Continue
            }


            await Task.Delay(10);

            Program.Logger.Debug("Resetting unit sizes for next battle.");
            ArmyProportions.ResetUnitSizes();
            GC.Collect();

            // Clear battle state after successful completion
            BattleState.ClearBattleState();
            TerrainGenerator.Clear(); // Moved from start of method to clear state AFTER battle is processed.

            return true; // Success
        }

        private static (bool, string) TryMapSizeFix(AutofixState autofixState, string originalMapSize)
        {
            autofixState.MapSizeFixAttempts++;

            if (originalMapSize == "Medium" && autofixState.MapSizeFixAttempts == 1)
            {
                BattleState.AutofixDeploymentSizeOverride = "Big";
                return (true, "increasing the battle map size to 'Big'");
            }
            if (originalMapSize == "Medium" && autofixState.MapSizeFixAttempts == 2)
            {
                BattleState.AutofixDeploymentSizeOverride = "Huge";
                return (true, "increasing the battle map size to 'Huge'");
            }
            if (originalMapSize == "Big" && autofixState.MapSizeFixAttempts == 1)
            {
                BattleState.AutofixDeploymentSizeOverride = "Huge";
                return (true, "increasing the battle map size to 'Huge'");
            }

            return (false, ""); // No more fixes of this type
        }

        private static (bool, string) TryDeploymentFix(AutofixState autofixState)
        {
            if (!twbattle.BattleState.IsSiegeBattle) // Field battle
            {
                if (!autofixState.DeploymentRotationTried)
                {
                    autofixState.DeploymentRotationTried = true;
                    BattleState.AutofixDeploymentRotationOverride = true;
                    return (true, "switching deployment from North/South to East/West (or vice-versa)");
                }
            }
            else // Siege battle
            {
                string[] allDirections = { "N", "S", "E", "W" };
                string? originalDirection = BattleState.OriginalSiegeAttackerDirection;

                if (string.IsNullOrEmpty(originalDirection))
                {
                    Program.Logger.Debug("Autofix Error: Original siege attacker direction was not recorded. Cannot attempt direction-based fixes.");
                    return (false, "");
                }

                var directionsToTry = allDirections.Where(d => d != originalDirection).ToList();
                if (autofixState.SiegeDirectionFixAttempts < directionsToTry.Count)
                {
                    string direction = directionsToTry[autofixState.SiegeDirectionFixAttempts];

                    autofixState.SiegeDirectionFixAttempts++;
                    BattleState.AutofixAttackerDirectionOverride = direction;
                    return (true, $"setting the besieger's attack direction to '{direction}' (original was '{originalDirection}')");
                }
            }
            return (false, "");
        }

        private static (bool, string) TryUnitFix(AutofixState autofixState, HomePage form)
        {
            var allArmies = autofixState.OriginalAttackerArmies.Concat(autofixState.OriginalDefenderArmies);

            while (autofixState.NextUnitKeyIndexToReplace < autofixState.ProblematicUnitKeys.Count)
            {
                string keyToReplace = autofixState.ProblematicUnitKeys[autofixState.NextUnitKeyIndexToReplace];
                Program.Logger.Debug($"--- Autofix: Attempting to replace unit key: {keyToReplace} ---");

                // Find a representative unit to determine its properties (type, culture, heritage, etc.)
                var representativeUnit = allArmies.SelectMany(a => a.Units).FirstOrDefault(u => u.GetAttilaUnitKey() == keyToReplace);
                if (representativeUnit == null)
                {
                    // Fallback for Levies/Garrisons which don't have a key on their placeholder unit.
                    representativeUnit = allArmies.SelectMany(a => a.Units)
                                                  .FirstOrDefault(u => u.GetRegimentType() == RegimentType.Levy || u.GetRegimentType() == RegimentType.Garrison);
                    if (representativeUnit == null)
                    {
                        Program.Logger.Debug($"Could not find a representative unit for key '{keyToReplace}'. Skipping to next key.");
                        autofixState.NextUnitKeyIndexToReplace++;
                        continue;
                    }
                }

                // 1. Try to find a replacement from a faction within the same heritage.
                string heritage = representativeUnit.GetHeritage();
                string originalFaction = representativeUnit.GetAttilaFaction();
                var heritageFactions = unit_mapper.UnitMappers_BETA.GetFactionsByHeritage(heritage)
                    .Where(f => f != originalFaction && f != "Default" && f != "DEFAULT")
                    .Distinct().ToList();

                foreach (var replacementFaction in heritageFactions)
                {
                    var (replacementKey, replacementIsSiege) = UnitMappers_BETA.GetReplacementUnitKeyFromFaction(representativeUnit, replacementFaction, keyToReplace);
                    if (replacementKey != UnitMappers_BETA.NOT_FOUND_KEY)
                    {
                        string fixDescription = $"replacing unit key '{keyToReplace}' with a unit from heritage faction '{replacementFaction}' ('{replacementKey}')";
                        AutofixReplacements[keyToReplace] = (replacementKey, replacementIsSiege);
                        autofixState.NextUnitKeyIndexToReplace++; // Move to the next unit for the *next* crash
                        return (true, fixDescription);
                    }
                }

                // 2. If no heritage replacement found, fall back to a default unit.
                var (defaultKey, defaultIsSiege) = UnitMappers_BETA.GetDefaultUnitKey(representativeUnit, keyToReplace);
                if (defaultKey != UnitMappers_BETA.NOT_FOUND_KEY)
                {
                    string fixDescription = $"replacing unit key '{keyToReplace}' with default unit '{defaultKey}'";
                    AutofixReplacements[keyToReplace] = (defaultKey, defaultIsSiege);
                    autofixState.NextUnitKeyIndexToReplace++; // Move to the next unit for the *next* crash
                    return (true, fixDescription);
                }

                // 3. If no replacement found at all for this key, log it and move to the next.
                Program.Logger.Debug($"--- Autofix: Could not find any valid replacement for unit key '{keyToReplace}'. ---");
                autofixState.NextUnitKeyIndexToReplace++;
            }

            // If the loop completes, all problematic units have been tried without success.
            return (false, "");
        }

        private static (bool, string) TryMapVariantFix(AutofixState autofixState)
        {
            if (twbattle.BattleState.IsSiegeBattle)
            {
                string defenderAttilaFaction = UnitMappers_BETA.GetAttilaFaction(twbattle.Sieges.GetGarrisonCulture(), twbattle.Sieges.GetGarrisonHeritage());
                string siegeBattleType = (twbattle.Sieges.GetHoldingLevel() > 1) ? "settlement_standard" : "settlement_unfortified";
                string provinceName = BattleResult.ProvinceName ?? "";
                var (isUnique, variantCount) = IsUsingUniqueMapAndGetVariantCount(defenderAttilaFaction, siegeBattleType, provinceName);

                if (isUnique && !autofixState.HasTriedSwitchingToGeneric)
                {
                    BattleState.AutofixForceGenericMap = true;
                    autofixState.HasTriedSwitchingToGeneric = true;
                    return (true, $"switching from a unique settlement map to a generic one for faction '{defenderAttilaFaction}'");
                }
                else if (variantCount > 1 && autofixState.MapVariantOffset < variantCount - 1)
                {
                    autofixState.MapVariantOffset++;
                    BattleState.AutofixMapVariantOffset = autofixState.MapVariantOffset;
                    return (true, $"switching to a different map variant (attempt {autofixState.MapVariantOffset} of {variantCount - 1})");
                }
            }
            else // Field Battle
            {
                string? terrainType = TerrainGenerator.TerrainType;
                int variantCount = CrusaderWars.terrain.Lands.GetFieldBattleVariantCount(terrainType);
                if (variantCount > 1 && autofixState.MapVariantOffset < variantCount - 1)
                {
                    autofixState.MapVariantOffset++;
                    BattleState.AutofixMapVariantOffset = autofixState.MapVariantOffset;
                    return (true, $"switching to a different field map variant (attempt {autofixState.MapVariantOffset} of {variantCount - 1}) for terrain '{terrainType}'");
                }
            }

            return (false, "");
        }

        private static (bool, string) TryManualUnitFix(AutofixState autofixState, HomePage form)
        {
            Program.Logger.Debug("--- Autofix: Initiating Manual Unit Replacement ---");

            // 1. Set army sides to ensure IsPlayer() is correct on units
            BattleFile.SetArmiesSides(autofixState.OriginalAttackerArmies, autofixState.OriginalDefenderArmies);

            // 2. Collect data
            var allArmies = autofixState.OriginalAttackerArmies.Concat(autofixState.OriginalDefenderArmies);
            var currentUnits = allArmies.SelectMany(a => a.Units)
                                        .Where(u => u != null && !string.IsNullOrEmpty(u.GetAttilaUnitKey()) && u.GetAttilaUnitKey() != UnitMappers_BETA.NOT_FOUND_KEY)
                                        .ToList();

            var allAvailableUnits = UnitMappers_BETA.GetAllAvailableUnits();

            // 3. Show form
            Dictionary<(string originalKey, bool isPlayerAlliance), (string replacementKey, bool isSiege)> replacements = new Dictionary<(string, bool), (string, bool)>();
            bool userCommitted = false;
            form.Invoke((MethodInvoker)delegate
            {
                using (var replacerForm = new client.UnitReplacerForm(currentUnits, allAvailableUnits, BattleState.ManualUnitReplacements))
                {
                    if (replacerForm.ShowDialog(form) == DialogResult.OK)
                    {
                        replacements = replacerForm.Replacements;
                        userCommitted = true;
                    }
                }
            });

            // 4. Process results
            if (userCommitted && replacements.Any())
            {
                Program.Logger.Debug($"Applying {replacements.Count} manual unit replacements.");
                BattleState.ManualUnitReplacements.Clear(); // Clear previous manual fixes

                // Create lookups for user-friendly names
                var originalUnitLookup = currentUnits
                    .Where(u => !string.IsNullOrEmpty(u.GetAttilaUnitKey()))
                    .GroupBy(u => u.GetAttilaUnitKey())
                    .ToDictionary(g => g.Key, g => {
                        var unit = g.First();
                        string name = string.IsNullOrEmpty(unit.GetLocName()) ? unit.GetName() : unit.GetLocName();
                        if (unit.GetRegimentType() == RegimentType.MenAtArms)
                        {
                            string? maxCategory = UnitMappers_BETA.GetMenAtArmMaxCategory(unit.GetName());
                            if (!string.IsNullOrEmpty(maxCategory))
                            {
                                return $"{name} [{maxCategory}]";
                            }
                        }
                        return name;
                    });

                var replacementUnitLookup = allAvailableUnits
                    .GroupBy(u => u.AttilaUnitKey)
                    .ToDictionary(g => g.Key, g => {
                        var unit = g.First();
                        string displayText = unit.DisplayName;
                        if (unit.UnitType == "MenAtArm" && !string.IsNullOrEmpty(unit.MaxCategory))
                        {
                            displayText += $" [{unit.MaxCategory}]";
                        }
                        if (unit.Rank.HasValue)
                        {
                            displayText += $" [Rank: {unit.Rank.Value}]";
                        }
                        else if (unit.Level.HasValue)
                        {
                            displayText += $" [Level: {unit.Level.Value}]";
                        }
                        displayText += $" [{unit.AttilaUnitKey}]";

                        return displayText;
                    });

                var sb = new StringBuilder();
                sb.AppendLine("applying the following manual unit replacements:");

                var groupedReplacements = replacements
                    .GroupBy(kvp => new { ReplacementKey = kvp.Value.replacementKey, IsPlayer = kvp.Key.isPlayerAlliance })
                    .Select(g => new {
                        g.Key.ReplacementKey,
                        g.Key.IsPlayer,
                        OriginalKeys = g.Select(kvp => kvp.Key.originalKey).ToList()
                    });

                foreach (var group in groupedReplacements.OrderBy(g => !g.IsPlayer))
                {
                    string replacementName = replacementUnitLookup.TryGetValue(group.ReplacementKey, out var name) ? (name ?? group.ReplacementKey) : group.ReplacementKey;
                    var originalNames = group.OriginalKeys
                        .Select(key => originalUnitLookup.TryGetValue(key, out var origName) ? (origName ?? key) : key)
                        .Distinct()
                        .OrderBy(n => n);
                    string allianceName = group.IsPlayer ? "Player's Alliance" : "Enemy's Alliance";
                    sb.AppendLine($" - In {allianceName}: Replacing [{string.Join(", ", originalNames)}] with [{replacementName}]");
                }

                string fixDescription = sb.ToString().TrimEnd();

                // Process replacements for the game state
                foreach (var replacement in replacements)
                {
                    BattleState.ManualUnitReplacements[replacement.Key] = replacement.Value;
                    Program.Logger.Debug($"  - Replacing '{replacement.Key.originalKey}' with '{replacement.Value.replacementKey}' for {(replacement.Key.isPlayerAlliance ? "Player" : "Enemy")}");
                }

                return (true, fixDescription);
            }
            else
            {
                Program.Logger.Debug("Manual unit replacement was cancelled or no changes were made.");
                return (false, "");
            }
        }

        private static (DialogResult result, AutofixState.AutofixStrategy? strategy) ShowAutofixStrategyChoicePrompt(IWin32Window owner, List<AutofixState.AutofixStrategy> availableStrategies)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 500;
                prompt.Text = "Crusader Conflicts: Attila Crash Detected";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label() { 
                    Left = 20, Top = 20, Width = 460, Height = 70, 
                    Text = "It appears Total War: Attila has crashed. This is often caused by an incompatible custom unit or map.\n\nThe application will now attempt a fix. If it fails, you will be prompted again.\n\nPlease select which automatic fix strategy to try next:" 
                };
                prompt.Controls.Add(textLabel);

                var allStrategyControls = new Dictionary<AutofixState.AutofixStrategy, (RadioButton rb, Label lbl)>
                {
                    { AutofixState.AutofixStrategy.Units, (new RadioButton() { Text = "Change Units", AutoSize = true }, new Label() { Text = "Replaces custom mod units one-by-one with default units. Good for a specific buggy unit.", Width = 400, Height = 30, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.MapSize, (new RadioButton() { Text = "Change Map Size", AutoSize = true }, new Label() { Text = "Increases deployment area. Good for crashes with very large armies.", AutoSize = true, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.Deployment, (new RadioButton() { Text = "Change Deployment", AutoSize = true }, new Label() { Text = "Rotates deployment zones or attacker direction. Good for units spawning in bad terrain.", Width = 400, Height = 30, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.MapVariant, (new RadioButton() { Text = "Change Map", AutoSize = true }, new Label() { Text = "Switches to a different map for the same location. Good for a buggy map file.", AutoSize = true, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.ManualUnitReplacement, (new RadioButton() { Text = "Manual Unit Replacement", AutoSize = true }, new Label() { Text = "Manually replace specific units in your army with any available unit.", Width = 400, Height = 30, ForeColor = System.Drawing.Color.Gray }) }
                };

                int currentTop = 100;
                bool first = true;
                foreach (var strategy in availableStrategies)
                {
                    if (allStrategyControls.TryGetValue(strategy, out var controls))
                    {
                        controls.rb.Left = 30;
                        controls.rb.Top = currentTop;
                        if (first)
                        {
                            controls.rb.Checked = true;
                            first = false;
                        }
                        prompt.Controls.Add(controls.rb);

                        controls.lbl.Left = 50;
                        controls.lbl.Top = currentTop + 20;
                        prompt.Controls.Add(controls.lbl);

                        currentTop += 55;
                    }
                }
                
                Button btnStart = new Button() { Text = "Start Autofix", Left = 150, Width = 100, Top = currentTop + 10, DialogResult = DialogResult.Yes };
                Button btnCancel = new Button() { Text = "Cancel", Left = 270, Width = 100, Top = currentTop + 10, DialogResult = DialogResult.No };

                prompt.Height = currentTop + 80;

                btnStart.Click += (sender, e) => { prompt.Close(); };
                btnCancel.Click += (sender, e) => { prompt.Close(); };

                prompt.Controls.Add(btnStart);
                prompt.Controls.Add(btnCancel);
                prompt.AcceptButton = btnStart;
                prompt.CancelButton = btnCancel;

                var dialogResult = prompt.ShowDialog(owner);

                AutofixState.AutofixStrategy? selectedStrategy = null;
                if (dialogResult == DialogResult.Yes)
                {
                    foreach (var strategy in availableStrategies)
                    {
                        if (allStrategyControls[strategy].rb.Checked)
                        {
                            selectedStrategy = strategy;
                            break;
                        }
                    }
                }

                return (dialogResult, selectedStrategy);
            }
        }

        private static void ShowClickableLinkMessageBox(IWin32Window owner, string text, string title, string linkText, string linkUrl, params string[] boldWords)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 550;
                prompt.Height = 250;
                prompt.Text = title;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                RichTextBox richTextLabel = new RichTextBox()
                {
                    Left = 20,
                    Top = 20,
                    Width = 500,
                    Height = 120,
                    Text = text,
                    BorderStyle = BorderStyle.None,
                    ReadOnly = true,
                    BackColor = System.Drawing.SystemColors.Control,
                    DetectUrls = false
                };

                if (boldWords != null)
                {
                    foreach (string word in boldWords)
                    {
                        if (!string.IsNullOrEmpty(word))
                        {
                            int startIndex = 0;
                            while (startIndex < richTextLabel.TextLength)
                            {
                                int wordStartIndex = richTextLabel.Find(word, startIndex, RichTextBoxFinds.None);
                                if (wordStartIndex != -1)
                                {
                                    richTextLabel.SelectionStart = wordStartIndex;
                                    richTextLabel.SelectionLength = word.Length;
                                    richTextLabel.SelectionFont = new System.Drawing.Font(richTextLabel.Font, System.Drawing.FontStyle.Bold);
                                    startIndex = wordStartIndex + word.Length;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                LinkLabel linkLabel = new LinkLabel()
                {
                    Left = 20,
                    Top = 140,
                    Width = 500,
                    Text = linkText,
                    AutoSize = true
                };
                linkLabel.LinkClicked += (sender, e) => {
                    try
                    {
                        Process.Start(new ProcessStartInfo(linkUrl) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open link: {ex.Message}");
                    }
                };

                Button confirmation = new Button()
                {
                    Text = "OK",
                    Left = 225,
                    Width = 100,
                    Top = 180,
                    DialogResult = DialogResult.OK
                };

                prompt.Controls.Add(richTextLabel);
                prompt.Controls.Add(linkLabel);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                prompt.ShowDialog(owner);
            }
        }
        private static (bool isUnique, int variantCount) IsUsingUniqueMapAndGetVariantCount(string faction, string battleType, string provinceName)
        {
            if (UnitMappers_BETA.Terrains == null) return (false, 0);

            // Check for unique map match first (mirrors GetSettlementMap logic)
            var uniqueMapByProvName = UnitMappers_BETA.Terrains.UniqueSettlementMaps
                .FirstOrDefault(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                       sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
            if (uniqueMapByProvName != null && uniqueMapByProvName.Variants.Any())
            {
                return (true, uniqueMapByProvName.Variants.Count);
            }

            var matchingUniqueMaps = UnitMappers_BETA.Terrains.UniqueSettlementMaps
                                     .Where(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase))
                                     .ToList();
            foreach (var uniqueMap in matchingUniqueMaps)
            {
                var uniqueMatch = uniqueMap.Variants.FirstOrDefault(v => provinceName.IndexOf(v.Key, StringComparison.OrdinalIgnoreCase) >= 0);
                if (uniqueMatch != null)
                {
                    return (true, uniqueMap.Variants.Count);
                }
            }

            // If no unique map, check for generic map and get its variant count
            var genericMapByProvName = UnitMappers_BETA.Terrains.SettlementMaps
                .FirstOrDefault(sm => sm.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                                       sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                       sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
            if (genericMapByProvName != null && genericMapByProvName.Variants.Any())
            {
                return (false, genericMapByProvName.Variants.Count);
            }

            var defaultGenericMapByProvName = UnitMappers_BETA.Terrains.SettlementMaps
                .FirstOrDefault(sm => sm.Faction.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                                       sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                       sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
            if (defaultGenericMapByProvName != null && defaultGenericMapByProvName.Variants.Any())
            {
                return (false, defaultGenericMapByProvName.Variants.Count);
            }

            var matchingGenericMaps = UnitMappers_BETA.Terrains.SettlementMaps
                                      .Where(sm => sm.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                                                   sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                                   !sm.ProvinceNames.Any())
                                      .ToList();
            if (matchingGenericMaps.Any())
            {
                return (false, matchingGenericMaps.SelectMany(sm => sm.Variants).Count());
            }

            var matchingDefaultGenericMaps = UnitMappers_BETA.Terrains.SettlementMaps
                                              .Where(sm => sm.Faction.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                                                           sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                                           !sm.ProvinceNames.Any())
                                              .ToList();
            if (matchingDefaultGenericMaps.Any())
            {
                return (false, matchingDefaultGenericMaps.SelectMany(sm => sm.Variants).Count());
            }

            return (false, 0);
        }

    }
}
