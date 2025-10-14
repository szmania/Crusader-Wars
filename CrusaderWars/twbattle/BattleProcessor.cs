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
        internal class AutofixState
        {
            public List<string> ProblematicUnitKeys { get; set; } = new List<string>();
            public int NextUnitKeyIndexToReplace { get; set; } = 0;
            public int NextDeploymentFixLevel { get; set; } = 1; // Starts at 1, max 5
            public bool IsNextFixUnitReplacement { get; set; } = true;
        }

        public static async Task<bool> ProcessBattle(HomePage form, List<Army> attacker_armies, List<Army> defender_armies, CancellationToken token, bool regenerateAndRestart = true, AutofixState? autofixState = null)
        {
            if (autofixState == null)
            {
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
                    BattleScript.EraseScript();
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
                        sb.AppendLine("Please report this bug to the Crusader Conflicts Development Team at https://discord.gg/eFZTprHh3j");
                        sb.AppendLine();
                        sb.AppendLine("The battle will proceed without these units.");

                        form.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                            MessageBox.Show(form, sb.ToString(), "Crusader Conflicts: Unit Mapping Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                    // NEW: Add siege event listeners if it's a siege battle
                    if (twbattle.BattleState.IsSiegeBattle)
                    {
                        Program.Logger.Debug("Adding siege event listeners to script...");
                        BattleScript.AddSiegeEvents();
                    }

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
                BattleScript.EraseScript();
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
                            userResponse = MessageBox.Show(form,
                                "It appears Total War: Attila has crashed or was closed prematurely. This is often caused by an incompatible custom unit or a problematic map deployment.\n\nWould you like to attempt an automatic fix?\n\nThe application will alternate between replacing one potentially problematic unit type at a time with a safe default, and adjusting the battle map size/layout, then restart the battle.",
                                "Crusader Conflicts: Attila Crash Detected",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);
                        });

                        if (userResponse == DialogResult.No)
                        {
                            Program.Logger.Debug("User declined autofix. Aborting battle.");
                            return false; // User cancelled
                        }

                        Program.Logger.Debug("User accepted autofix. Initializing autofix process.");
                        autofixState = new AutofixState();

                        var allUnits = attacker_armies.SelectMany(a => a.Units).Concat(defender_armies.SelectMany(a => a.Units));
                        autofixState.ProblematicUnitKeys = allUnits
                            .Where(u => u.GetRegimentType() == RegimentType.MenAtArms || u.GetRegimentType() == RegimentType.Levy || u.GetRegimentType() == RegimentType.Garrison)
                            .Select(u => u.GetAttilaUnitKey())
                            .Where(key => !string.IsNullOrEmpty(key) && key != UnitMappers_BETA.NOT_FOUND_KEY)
                            .Distinct()
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
                        Program.Logger.Debug("Subsequent crash during autofix process. Continuing automatically.");
                    }

                    // Common Autofix Logic
                    bool canDoUnitFix = autofixState.NextUnitKeyIndexToReplace < autofixState.ProblematicUnitKeys.Count;
                    const int maxDeploymentFixLevel = 5;
                    bool canDoDeploymentFix = autofixState.NextDeploymentFixLevel <= maxDeploymentFixLevel;

                    if (!canDoUnitFix && !canDoDeploymentFix)
                    {
                        Program.Logger.Debug("Autofix failed. All problematic units have been replaced and deployment variations tried, but Attila continues to crash.");
                        form.Invoke((MethodInvoker)delegate
                        {
                            MessageBox.Show(form,
                                "The automatic fix failed. All potentially problematic units were replaced and all deployment variations were tried, but the game still crashed.\n\nThe crash may be caused by a more fundamental issue.\n\nThe battle will be aborted.",
                                "Crusader Conflicts: Autofix Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        });
                        return false;
                    }

                    string fixDescription = "";

                    if (autofixState.IsNextFixUnitReplacement && canDoUnitFix)
                    {
                        // Apply unit fix
                        string keyToReplace = autofixState.ProblematicUnitKeys[autofixState.NextUnitKeyIndexToReplace];
                        fixDescription = $"replacing unit key '{keyToReplace}'";
                        Program.Logger.Debug($"Autofix attempt: {fixDescription}.");

                        var allArmies = attacker_armies.Concat(defender_armies);
                        foreach (var army in allArmies)
                        {
                            foreach (var unit in army.Units)
                            {
                                if (unit.GetAttilaUnitKey() == keyToReplace)
                                {
                                    var (defaultKey, isSiege) = UnitMappers_BETA.GetDefaultUnitKey(unit);
                                    if (defaultKey != UnitMappers_BETA.NOT_FOUND_KEY)
                                    {
                                        Program.Logger.Debug($"  - In army {army.ID}, replacing unit '{unit.GetName()}' key '{keyToReplace}' with default '{defaultKey}'.");
                                        unit.SetUnitKey(defaultKey);
                                        unit.SetIsSiege(isSiege);
                                    }
                                    else
                                    {
                                        Program.Logger.Debug($"  - WARNING: Could not find a default unit for type {unit.GetRegimentType()} to replace '{keyToReplace}'. The unit may be dropped.");
                                    }
                                }
                            }
                        }
                        autofixState.NextUnitKeyIndexToReplace++;
                        autofixState.IsNextFixUnitReplacement = false;
                    }
                    else if (canDoDeploymentFix)
                    {
                        // Apply deployment fix
                        bool rotate = false;
                        string? size = null;
                        switch (autofixState.NextDeploymentFixLevel)
                        {
                            case 1: size = "Big"; rotate = false; break;
                            case 2: size = "Medium"; rotate = false; break;
                            case 3: size = null; rotate = true; break; // Use default size, but rotate
                            case 4: size = "Big"; rotate = true; break;
                            case 5: size = "Medium"; rotate = true; break;
                        }
                        fixDescription = $"changing deployment (Size: {size ?? "Default"}, Rotate: {rotate})";
                        Program.Logger.Debug($"Autofix attempt: {fixDescription}.");
                        BattleState.AutofixDeploymentSizeOverride = size;
                        BattleState.AutofixDeploymentRotationOverride = rotate;

                        autofixState.NextDeploymentFixLevel++;
                        autofixState.IsNextFixUnitReplacement = true;
                    }
                    else // Fallback to unit fix if deployment fixes are exhausted
                    {
                        // Apply unit fix
                        string keyToReplace = autofixState.ProblematicUnitKeys[autofixState.NextUnitKeyIndexToReplace];
                        fixDescription = $"replacing unit key '{keyToReplace}' (deployment fixes exhausted)";
                        Program.Logger.Debug($"Autofix attempt: {fixDescription}.");

                        var allArmies = attacker_armies.Concat(defender_armies);
                        foreach (var army in allArmies)
                        {
                            foreach (var unit in army.Units)
                            {
                                if (unit.GetAttilaUnitKey() == keyToReplace)
                                {
                                    var (defaultKey, isSiege) = UnitMappers_BETA.GetDefaultUnitKey(unit);
                                    if (defaultKey != UnitMappers_BETA.NOT_FOUND_KEY)
                                    {
                                        Program.Logger.Debug($"  - In army {army.ID}, replacing unit '{unit.GetName()}' key '{keyToReplace}' with default '{defaultKey}'.");
                                        unit.SetUnitKey(defaultKey);
                                        unit.SetIsSiege(isSiege);
                                    }
                                    else
                                    {
                                        Program.Logger.Debug($"  - WARNING: Could not find a default unit for type {unit.GetRegimentType()} to replace '{keyToReplace}'. The unit may be dropped.");
                                    }
                                }
                            }
                        }
                        autofixState.NextUnitKeyIndexToReplace++;
                        autofixState.IsNextFixUnitReplacement = false;
                    }

                    Program.Logger.Debug($"Relaunching battle after autofix ({fixDescription}).");
                    // Recursive call to restart the battle process
                    return await ProcessBattle(form, attacker_armies, defender_armies, token, true, autofixState);
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
    }
}
