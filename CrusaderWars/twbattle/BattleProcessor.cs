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

            // State for the current unit being fixed
            public List<string>? HeritageReplacementFactions { get; set; } // List of unique factions from the same heritage to try as replacements.
            public int NextHeritageFactionIndex { get; set; } = 0; // Index for the above list.
            public int FailureCount { get; set; } = 0;
            public string LastAppliedFixDescription { get; set; } = "";
            public bool KeepMapSizeHuge { get; set; } = false;
            public bool KeepTryingAutomatically { get; set; } = false;
        }

        public static async Task<bool> ProcessBattle(HomePage form, List<Army> attacker_armies, List<Army> defender_armies, CancellationToken token, bool regenerateAndRestart = true, AutofixState? autofixState = null)
        {
            if (autofixState == null)
            {
                AutofixReplacements.Clear(); // Clear fixes for a new battle
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
                                        string rankInfo = "";
                                        if (unit.GetRegimentType() == RegimentType.Commander || unit.GetRegimentType() == RegimentType.Knight)
                                        {
                                            rankInfo = $", Rank: {unit.CharacterRank}";
                                        }
                                        Program.Logger.Debug($"  - CK3 Unit: {unit.GetName()}, Type: {unit.GetRegimentType()}, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}");
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
                                        string rankInfo = "";
                                        if (unit.GetRegimentType() == RegimentType.Commander || unit.GetRegimentType() == RegimentType.Knight)
                                        {
                                            rankInfo = $", Rank: {unit.CharacterRank}";
                                        }
                                        Program.Logger.Debug($"  - CK3 Unit: {unit.GetName()}, Type: {unit.GetRegimentType()}, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}");
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

                    // Autofix logic starts here
                    if (autofixState == null) // First crash
                    {
                        Program.Logger.Debug("First crash detected. Prompting user for autofix.");
                        DialogResult userResponse = DialogResult.No;
                        form.Invoke((MethodInvoker)delegate
                        {
                            userResponse = ShowAutofixPrompt(form);
                        });

                        if (userResponse == DialogResult.No)
                        {
                            Program.Logger.Debug("User declined autofix. Aborting battle.");
                            return false; // User cancelled
                        }

                        Program.Logger.Debug("User accepted autofix. Initializing autofix process.");
                        autofixState = new AutofixState();
                        autofixState.FailureCount = 1;
                        if (userResponse == DialogResult.Retry) // "Yes (Don't Ask Again)"
                        {
                            autofixState.KeepTryingAutomatically = true;
                            Program.Logger.Debug("Autofix mode set to 'Keep Trying Automatically'.");
                        }

                        // Build a comprehensive list of all possible custom unit keys in the battle
                        var (fresh_attackers_for_keys, fresh_defenders_for_keys) = ArmiesReader.ReadBattleArmies();
                        var allArmiesForKeys = fresh_attackers_for_keys.Concat(fresh_defenders_for_keys);
                        var allUnitKeys = new HashSet<string>();
                        var allFactions = new HashSet<string>();

                        foreach (var army in allArmiesForKeys)
                        {
                            if (army.Owner == null) continue;

                            // Get faction from commander
                            if (army.Commander != null)
                            {
                                var commander = army.Commander;
                                Unit temp_commander_unit = new Unit("General", commander.GetUnitSoldiers(), commander.GetCultureObj(), RegimentType.Commander, false, army.Owner);
                                string faction = UnitMappers_BETA.GetAttilaFaction(commander.GetCultureName(), commander.GetHeritageName());
                                temp_commander_unit.SetAttilaFaction(faction);
                                allFactions.Add(faction);
                                var (commanderKey, _) = UnitMappers_BETA.GetUnitKey(temp_commander_unit);
                                if (!string.IsNullOrEmpty(commanderKey) && commanderKey != UnitMappers_BETA.NOT_FOUND_KEY)
                                {
                                    allUnitKeys.Add(commanderKey);
                                }
                            }

                            // Get faction from knights
                            if (army.Knights != null && army.Knights.GetKnightsList()?.Count > 0)
                            {
                                Unit temp_knights_unit;
                                if (army.Knights.GetMajorCulture() != null)
                                    temp_knights_unit = new Unit("Knight", army.Knights.GetKnightsSoldiers(), army.Knights.GetMajorCulture(), RegimentType.Knight, false, army.Owner);
                                else
                                    temp_knights_unit = new Unit("Knight", army.Knights.GetKnightsSoldiers(), army.Owner.GetCulture(), RegimentType.Knight, false, army.Owner);

                                string faction = UnitMappers_BETA.GetAttilaFaction(temp_knights_unit.GetCulture(), temp_knights_unit.GetHeritage());
                                temp_knights_unit.SetAttilaFaction(faction);
                                allFactions.Add(faction);
                                var (knightKey, _) = UnitMappers_BETA.GetUnitKey(temp_knights_unit);
                                if (!string.IsNullOrEmpty(knightKey) && knightKey != UnitMappers_BETA.NOT_FOUND_KEY)
                                {
                                    allUnitKeys.Add(knightKey);
                                }
                            }

                            // Get factions and keys from all other units (MAA, Levy/Garrison placeholders)
                            foreach (var unit in army.Units)
                            {
                                string faction = unit.GetAttilaFaction();
                                if (!string.IsNullOrEmpty(faction) && faction != UnitMappers_BETA.NOT_FOUND_KEY)
                                {
                                    allFactions.Add(faction);
                                }

                                // Only MAA have a direct, replaceable unit key at this stage
                                if (unit.GetRegimentType() == RegimentType.MenAtArms)
                                {
                                    string maaKey = unit.GetAttilaUnitKey();
                                    if (!string.IsNullOrEmpty(maaKey) && maaKey != UnitMappers_BETA.NOT_FOUND_KEY)
                                    {
                                        allUnitKeys.Add(maaKey);
                                    }
                                }
                            }
                        }

                        // Now, for every faction we found, add all their possible levy and garrison units to the list
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

                            // Garrisons depend on holding level, so we check all levels
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
                    else // Subsequent crash
                    {
                        autofixState.FailureCount++;
                        Program.Logger.Debug($"Subsequent crash detected. Autofix has now failed {autofixState.FailureCount - 1} time(s).");
                        if (!autofixState.KeepTryingAutomatically)
                        {
                            DialogResult userResponse = DialogResult.No;
                            form.Invoke((MethodInvoker)delegate
                            {
                                userResponse = MessageBox.Show(form,
                                    $"The automatic fix has failed {autofixState.FailureCount - 1} time(s). Would you like to continue trying?",
                                    "Crusader Conflicts: Continue Autofix?",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question);
                            });

                            if (userResponse == DialogResult.No)
                            {
                                Program.Logger.Debug("User declined to continue autofix. Aborting battle.");
                                return false; // User cancelled
                            }
                        }
                        Program.Logger.Debug("User chose to continue autofix (or it was automatic).");
                    }

                    // Common Autofix Logic
                    BattleState.ClearAutofixOverrides();
                    if (autofixState.KeepMapSizeHuge)
                    {
                        BattleState.AutofixDeploymentSizeOverride = "Huge";
                        Program.Logger.Debug("Autofix: Persisting 'Huge' map size from previous fix.");
                    }
                    string fixDescription = "";
                    bool isSizeOrDeploymentFix = false;

                    // --- Determine Original Map Size (before any overrides) ---
                    string originalMapSize;
                    string option_map_size = ModOptions.DeploymentsZones();
                    if (option_map_size == "Dynamic")
                    {
                        // Reread armies to get original soldier counts for dynamic calculation
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

                    // --- STAGE 1: Map Size Fixes ---
                    if (autofixState.FailureCount == 1 && originalMapSize == "Medium")
                    {
                        fixDescription = "increasing the battle map size to 'Big'";
                        BattleState.AutofixDeploymentSizeOverride = "Big";
                        isSizeOrDeploymentFix = true;
                    }
                    else if (autofixState.FailureCount == 1 && originalMapSize == "Big")
                    {
                        fixDescription = "increasing the battle map size to 'Huge'";
                        BattleState.AutofixDeploymentSizeOverride = "Huge";
                        isSizeOrDeploymentFix = true;
                        autofixState.KeepMapSizeHuge = true;
                    }
                    else if (autofixState.FailureCount == 2 && originalMapSize == "Medium")
                    {
                        fixDescription = "increasing the battle map size to 'Huge'";
                        BattleState.AutofixDeploymentSizeOverride = "Huge";
                        isSizeOrDeploymentFix = true;
                        autofixState.KeepMapSizeHuge = true;
                    }
                    else
                    {
                        // --- STAGE 2: Deployment Fixes ---
                        int deploymentFixOffset = 0;
                        if (originalMapSize == "Medium") deploymentFixOffset = 2;
                        else if (originalMapSize == "Big") deploymentFixOffset = 1;

                        int deploymentFailureCount = autofixState.FailureCount - deploymentFixOffset;

                        if (!twbattle.BattleState.IsSiegeBattle && deploymentFailureCount == 1)
                        {
                            fixDescription = "switching deployment from North/South to East/West (or vice-versa)";
                            BattleState.AutofixDeploymentRotationOverride = true;
                            isSizeOrDeploymentFix = true;
                        }
                        else if (twbattle.BattleState.IsSiegeBattle && deploymentFailureCount >= 1 && deploymentFailureCount <= 4)
                        {
                            string[] directions = { "N", "S", "E", "W" };
                            string direction = directions[deploymentFailureCount - 1];
                            fixDescription = $"setting the besieger's attack direction to '{direction}'";
                            BattleState.AutofixAttackerDirectionOverride = direction;
                            isSizeOrDeploymentFix = true;
                        }
                    }


                    if (isSizeOrDeploymentFix)
                    {
                        autofixState.LastAppliedFixDescription = fixDescription;
                        if (!autofixState.KeepTryingAutomatically)
                        {
                            form.Invoke((MethodInvoker)delegate
                            {
                                form.infoLabel.Text = $"Attila crashed. Attempting automatic fix #{autofixState.FailureCount}...";
                                form.Text = $"Crusader Conflicts (Attempting fix #{autofixState.FailureCount})";
                                string messageText = $"Attempting automatic fix #{autofixState.FailureCount}.\n\nThe application will now try {fixDescription} and restart the battle.\n\nPlease note this information if you plan to report a bug on our Discord server:";
                                string discordUrl = "https://discord.gg/eFZTprHh3j";
                                ShowClickableLinkMessageBox(form, messageText, "Crusader Conflicts: Applying Autofix", "Report on Discord: " + discordUrl, discordUrl);
                            });
                        }
                        else
                        {
                            Program.Logger.Debug($"Automatically applying fix #{autofixState.FailureCount}: {fixDescription}. Skipping user prompt.");
                            form.Invoke((MethodInvoker)delegate
                            {
                                form.infoLabel.Text = $"Attila crashed. Attempting automatic fix #{autofixState.FailureCount}...";
                                form.Text = $"Crusader Conflicts (Attempting fix #{autofixState.FailureCount})";
                            });
                        }

                        Program.Logger.Debug($"Relaunching battle after autofix ({fixDescription}).");
                        var (fresh_attackers, fresh_defenders) = ArmiesReader.ReadBattleArmies();
                        return await ProcessBattle(form, fresh_attackers, fresh_defenders, token, true, autofixState);
                    }

                    // --- STAGE 3: Unit Replacements ---
                    while (true) // Loop until we find a fix to apply, or run out of all options.
                    {
                        if (autofixState.NextUnitKeyIndexToReplace >= autofixState.ProblematicUnitKeys.Count)
                        {
                            // We've tried to fix all problematic keys. Break the loop to show the final failure message.
                            break;
                        }

                        string keyToReplace = autofixState.ProblematicUnitKeys[autofixState.NextUnitKeyIndexToReplace];
                        Program.Logger.Debug($"--- Autofix: Starting process for problematic key: {keyToReplace} ---");

                        // Reread armies for a clean state, which also regenerates garrisons correctly.
                        var (fresh_attackers, fresh_defenders) = ArmiesReader.ReadBattleArmies();
                        var allArmies = fresh_attackers.Concat(fresh_defenders);
                        var representativeUnit = allArmies.SelectMany(a => a.Units).FirstOrDefault(u => u.GetAttilaUnitKey() == keyToReplace);

                        if (representativeUnit == null)
                        {
                            Program.Logger.Debug($"Could not find a representative unit for key '{keyToReplace}'. Skipping to next key.");
                            autofixState.NextUnitKeyIndexToReplace++;
                            autofixState.HeritageReplacementFactions = null; // Reset for next key
                            autofixState.NextHeritageFactionIndex = 0;
                            continue;
                        }

                        // Initialize heritage faction search if it's the first time for this key
                        if (autofixState.HeritageReplacementFactions == null)
                        {
                            string heritage = representativeUnit.GetHeritage();
                            string originalFaction = representativeUnit.GetAttilaFaction();
                            Program.Logger.Debug($"Finding heritage factions for heritage '{heritage}', excluding original faction '{originalFaction}'.");

                            var heritageFactions = unit_mapper.UnitMappers_BETA.GetFactionsByHeritage(heritage);
                            autofixState.HeritageReplacementFactions = heritageFactions
                                .Where(f => f != originalFaction && f != "Default" && f != "DEFAULT")
                                .Distinct()
                                .ToList();

                            autofixState.NextHeritageFactionIndex = 0;
                            Program.Logger.Debug($"Found {autofixState.HeritageReplacementFactions.Count} alternative heritage factions to try.");
                        }

                        string replacementKey = UnitMappers_BETA.NOT_FOUND_KEY;
                        bool replacementIsSiege = false;
                        fixDescription = "";

                        // Stage 1: Try heritage factions
                        if (autofixState.HeritageReplacementFactions != null && autofixState.NextHeritageFactionIndex < autofixState.HeritageReplacementFactions.Count)
                        {
                            string replacementFaction = autofixState.HeritageReplacementFactions[autofixState.NextHeritageFactionIndex];
                            Program.Logger.Debug($"Attempting heritage replacement using faction: '{replacementFaction}'");

                            (replacementKey, replacementIsSiege) = UnitMappers_BETA.GetReplacementUnitKeyFromFaction(representativeUnit, replacementFaction, keyToReplace);

                            autofixState.NextHeritageFactionIndex++; // Move to next faction for the next attempt

                            if (replacementKey != UnitMappers_BETA.NOT_FOUND_KEY)
                            {
                                fixDescription = $"replacing unit key '{keyToReplace}' with a unit from heritage faction '{replacementFaction}' ('{replacementKey}')";
                            }
                        }
                        // Stage 2: If no heritage replacement was found, try default
                        else
                        {
                            Program.Logger.Debug($"Heritage replacements exhausted or failed. Trying 'Default' faction replacement.");
                            (replacementKey, replacementIsSiege) = UnitMappers_BETA.GetDefaultUnitKey(representativeUnit, keyToReplace);

                            if (replacementKey != UnitMappers_BETA.NOT_FOUND_KEY)
                            {
                                fixDescription = $"replacing unit key '{keyToReplace}' with default unit '{replacementKey}'";
                            }

                            // We've tried all options for this key, so prepare to move to the next one on the next crash.
                            autofixState.NextUnitKeyIndexToReplace++;
                            autofixState.HeritageReplacementFactions = null;
                            autofixState.NextHeritageFactionIndex = 0;
                        }

                        // If we found a replacement (from either stage), apply it and relaunch.
                        if (replacementKey != UnitMappers_BETA.NOT_FOUND_KEY)
                        {
                            autofixState.LastAppliedFixDescription = fixDescription;
                            if (!autofixState.KeepTryingAutomatically)
                            {
                                form.Invoke((MethodInvoker)delegate
                                {
                                    form.infoLabel.Text = $"Attila crashed. Attempting automatic fix #{autofixState.FailureCount}...";
                                    form.Text = $"Crusader Conflicts (Attempting fix #{autofixState.FailureCount})";
                                    string messageText = $"Attempting automatic fix #{autofixState.FailureCount}.\n\nThe application will now try {fixDescription} and restart the battle.\n\nPlease note this information if you plan to report a bug on our Discord server:";
                                    string discordUrl = "https://discord.gg/eFZTprHh3j";
                                    ShowClickableLinkMessageBox(form, messageText, "Crusader Conflicts: Applying Autofix", "Report on Discord: " + discordUrl, discordUrl, keyToReplace, replacementKey);
                                });
                            }
                            else
                            {
                                Program.Logger.Debug($"Automatically applying fix #{autofixState.FailureCount}: {fixDescription}. Skipping user prompt.");
                                form.Invoke((MethodInvoker)delegate
                                {
                                    form.infoLabel.Text = $"Attila crashed. Attempting automatic fix #{autofixState.FailureCount}...";
                                    form.Text = $"Crusader Conflicts (Attempting fix #{autofixState.FailureCount})";
                                });
                            }

                            // Store the new fix.
                            AutofixReplacements[keyToReplace] = (replacementKey, replacementIsSiege);
                            Program.Logger.Debug($"Stored fix: Replace '{keyToReplace}' with '{replacementKey}'. Total fixes: {AutofixReplacements.Count}");

                            // Apply all cumulative fixes to the fresh armies.
                            var allFreshArmies = fresh_attackers.Concat(fresh_defenders);
                            foreach (var fix in AutofixReplacements)
                            {
                                string originalKey = fix.Key;
                                (string newKey, bool newIsSiege) = fix.Value;
                                Program.Logger.Debug($"Applying cumulative fix: Replacing all instances of '{originalKey}' with '{newKey}'.");
                                // This only affects MAA units. Commander/Knight/Levy/Garrison units are handled
                                // during their generation in UnitsFile.cs by checking the AutofixReplacements dictionary.
                                foreach (var unit in allFreshArmies.SelectMany(a => a.Units).Where(u => u.GetAttilaUnitKey() == originalKey))
                                {
                                    unit.SetUnitKey(newKey);
                                    unit.SetIsSiege(newIsSiege);
                                }
                            }

                            Program.Logger.Debug($"Relaunching battle after autofix ({fixDescription}).");
                            // Pass the fresh, cumulatively modified armies to the next battle attempt.
                            return await ProcessBattle(form, fresh_attackers, fresh_defenders, token, true, autofixState);
                        }
                        // If no replacement was found in this attempt, the loop will continue and try the next heritage faction, or default, or the next key.
                    }

                    // If the loop completes, it means all options are exhausted.
                    Program.Logger.Debug("Autofix failed. All problematic units have been checked, but Attila continues to crash or no replacements could be found.");
                    form.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show(form,
                            "The automatic fix failed. All potentially problematic units were checked, but either the game still crashed or no suitable replacements could be found.\n\nThe crash may be caused by a more fundamental issue.\n\nThe battle will be aborted.",
                            "Crusader Conflicts: Autofix Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    });
                    return false;
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
                        BattleResult.CheckForDeathCommanders(army, path_log_attila);
                        BattleResult.CheckKnightsKills(army);
                        BattleResult.CheckForDeathKnights(army);
                    }
                    Program.Logger.Debug("Setting casualties for defender armies...");
                    foreach (var army in defender_armies)
                    {
                        Program.Logger.Debug($"Processing army ID: {army.ID}");
                        BattleResult.ReadAttilaResults(army, path_log_attila);
                        BattleResult.CheckForDeathCommanders(army, path_log_attila);
                        BattleResult.CheckKnightsKills(army);
                        BattleResult.CheckForDeathKnights(army);

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
                        BattleResult.EditCombatFile(mobile_attacker_armies, mobile_defender_armies, left_side[0].CombatSide, right_side[0].CombatSide, path_log_attila);
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
                        string messageText = $"The battle was successful after an automatic fix.\n\nThe fix that worked was: {autofixState.LastAppliedFixDescription}.\n\nPlease report this on the Crusader Conflicts Discord server so it can be fixed in future updates.";
                        string discordUrl = "https://discord.gg/eFZTprHh3j";

                        form.Invoke((MethodInvoker)delegate {
                            ShowClickableLinkMessageBox(form, messageText, "Crusader Conflicts: Autofix Successful", "Report on Discord: " + discordUrl, discordUrl, autofixState.LastAppliedFixDescription);
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


            await Task.Delay(10);

            Program.Logger.Debug("Resetting unit sizes for next battle.");
            ArmyProportions.ResetUnitSizes();
            GC.Collect();

            // Clear battle state after successful completion
            BattleState.ClearBattleState();

            return true; // Success
        }

        private static DialogResult ShowAutofixPrompt(IWin32Window owner)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 500;
                prompt.Height = 220;
                prompt.Text = "Crusader Conflicts: Attila Crash Detected";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label() { 
                    Left = 20, 
                    Top = 20, 
                    Width = 460, 
                    Height = 80, 
                    Text = "It appears Total War: Attila has crashed or was closed prematurely. This is often caused by an incompatible custom unit.\n\nWould you like to attempt an automatic fix?\n\nThe application will replace one potentially problematic unit type at a time with a safe default, then restart the battle." 
                };

                Button btnYesKeepTrying = new Button() { Text = "Yes (Don't Ask Again)", Left = 30, Width = 150, Top = 130, DialogResult = DialogResult.Retry };
                Button btnYesOnce = new Button() { Text = "Yes", Left = 200, Width = 100, Top = 130, DialogResult = DialogResult.Yes };
                Button btnNo = new Button() { Text = "No", Left = 320, Width = 100, Top = 130, DialogResult = DialogResult.No };

                btnYesKeepTrying.Click += (sender, e) => { prompt.Close(); };
                btnYesOnce.Click += (sender, e) => { prompt.Close(); };
                btnNo.Click += (sender, e) => { prompt.Close(); };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(btnYesKeepTrying);
                prompt.Controls.Add(btnYesOnce);
                prompt.Controls.Add(btnNo);
                prompt.AcceptButton = btnYesKeepTrying; // Default button
                prompt.CancelButton = btnNo;

                return prompt.ShowDialog(owner);
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
    }
}
