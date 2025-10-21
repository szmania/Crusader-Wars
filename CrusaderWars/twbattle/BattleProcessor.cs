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
            public List<string> ProblematicUnitKeys { get; set; } = new List<string>();
            public int NextUnitKeyIndexToReplace { get; set; } = 0;
            public int FailureCount { get; set; } = 0;
            public string LastAppliedFixDescription { get; set; } = "";
            public int MapVariantOffset { get; set; } = 0;
            public bool HasTriedSwitchingToGeneric { get; set; } = false;
            public string OriginalMapDescription { get; set; } = "";
            public string OriginalFieldMapDescription { get; set; } = "";

            // New properties for strategy-based autofix
            public enum AutofixStrategy { MapSize, Deployment, Units, MapVariant }
            public AutofixStrategy? CurrentStrategy { get; set; } = null;
            public HashSet<AutofixStrategy> TriedStrategies { get; set; } = new HashSet<AutofixStrategy>();


            // State for individual strategies
            public int MapSizeFixAttempts { get; set; } = 0;
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
                BattleStateBridge.Clear();
                BattleState.ClearAutofixOverrides();
            }
            TerrainGenerator.Clear();
            UnitsFile.ResetProcessedArmies(); // Reset tracker for each battle processing attempt.
            var left_side = ArmiesReader.GetSideArmies("left", attacker_armies, defender_armies);
            var right_side = ArmiesReader.GetSideArmies("right", attacker_armies, defender_armies);

            if (left_side is null || !left_side.Any() || right_side is null || !right_side.Any())
            {
                Program.Logger.Debug("Could not determine battle sides or one side is empty. Aborting battle processing.");
                MessageBox.Show(form, "Could not determine player and enemy sides for the battle, or one side has no armies. The battle cannot proceed.", "Crusader Conflicts: Battle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; // break
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
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Warning: Some CK3 units could not be mapped to Total War: Attila units and were dropped from the battle.");
                        sb.AppendLine("This usually means a unit from a CK3 mod is not supported by the active Unit Mapper playthrough.");
                        sb.AppendLine();
                        sb.AppendLine("Unmapped Units:");
                        foreach (var u in unmappedUnits.Distinct().ToList())
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
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
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
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
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
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
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
                            AutofixState.AutofixStrategy.MapVariant
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

                        Program.Logger.Debug($"Relaunching battle after autofix ({fixDescription}). Re-reading army data from save files to apply changes.");
                        var (fresh_attackers, fresh_defenders) = ArmiesReader.ReadBattleArmies();
                        return await ProcessBattle(form, fresh_attackers, fresh_defenders, token, true, autofixState);
                    }
                    else
                    {
                        // The current strategy is exhausted.
                        Program.Logger.Debug($"--- Autofix: Strategy {autofixState.CurrentStrategy} exhausted. ---");
                        autofixState.TriedStrategies.Add(autofixState.CurrentStrategy.Value);
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

                    form.infoLabel.Text = "Processing TW:Attila battleModel API Response Error. Please retry the previous request