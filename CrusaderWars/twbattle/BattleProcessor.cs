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
using System.Globalization; // Added for CultureInfo
using System.Text.RegularExpressions; // Added for Regex

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
            public string LastAppliedFixDescription { get; set; }  = "";
            public int MapVariantOffset { get; set; }  = 0;
            public bool HasTriedSwitchingToGeneric { get; set; } = false;
            public string OriginalMapDescription { get; set; } = "";
            public string OriginalFieldMapDescription { get; set; } = "";

            // New properties for strategy-based autofix
            public enum AutofixStrategy { MapSize, Deployment, Units, MapVariant, ManualUnitReplacement, DeploymentZoneEditor }

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
            Program.Logger.Debug("--- BattleProcessor: Checking for Deployment Zone Overrides ---");
            if (BattleState.DeploymentZoneOverrideAttacker != null)
            {
                var ov = BattleState.DeploymentZoneOverrideAttacker;
                Program.Logger.Debug($"Attacker Override FOUND: X={ov.X}, Y={ov.Y}, W={ov.Width}, H={ov.Height}");
            }
            else
            {
                Program.Logger.Debug("Attacker Override NOT FOUND.");
            }

            if (BattleState.DeploymentZoneOverrideDefender != null)
            {
                var ov = BattleState.DeploymentZoneOverrideDefender;
                Program.Logger.Debug($"Defender Override FOUND: X={ov.X}, Y={ov.Y}, W={ov.Width}, H={ov.Height}");
            }
            else
            {
                Program.Logger.Debug("Defender Override NOT FOUND.");
            }
            Program.Logger.Debug("----------------------------------------------------------");

            if (autofixState == null)
            {
                AutofixReplacements.Clear(); // Clear fixes for a new battle
                CharacterDataManager.ClearCache(); // Clear dynasty name cache
                UnitMappers_BETA.ClearFactionCache();
                BattleStateBridge.Clear();
                // BattleState.ClearAutofixOverrides(); // Moved to end of successful battle processing
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

            BattleFile.SetArmiesSides(attacker_armies, defender_armies);

            ProcessProminentKnights(attacker_armies, defender_armies);

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

                            if (form != null && !form.IsDisposed)
                            {
                                form.Invoke((System.Windows.Forms.MethodInvoker)delegate
                                {
                                    string messageText = sb.ToString();
                                    string discordUrl = "https://discord.gg/eFZTprHh3j";

                                    var report = new StringBuilder();
                                    report.AppendLine("--- Unmapped Units Report ---");
                                    report.AppendLine($"Date: {DateTime.Now}");
                                    report.AppendLine($"Playthrough: {ModOptions.GetSelectedPlaythrough()}");
                                    report.AppendLine($"Loaded Mapper: {UnitMappers_BETA.GetLoadedUnitMapperName()}");
                                    report.AppendLine();
                                    report.AppendLine("Unmapped Units:");
                                    foreach (var u in displayableUnmappedUnits)
                                    {
                                        report.AppendLine($" - Type: {u.RegimentType}, Name: {u.UnitName}, Faction: {u.AttilaFaction} (Culture: {u.Culture})");
                                    }

                                    ShowClickableLinkMessageBox(form, messageText, "Crusader Conflicts: Unit Mapping Warning", "Report on Discord: " + discordUrl, discordUrl, report.ToString());
                                });
                            }
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
                            bool leviesLogged = false;
                            bool knightsLogged = false;

                            foreach (var unit in army.Units)
                            {
                                string unitDetails = $", Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {unit.GetAttilaFaction()}";

                                if (unit.GetRegimentType() == RegimentType.Knight)
                                {
                                    if (!knightsLogged)
                                    {
                                        // This block will now execute only once when it sees the combined "Knight" unit.
                                        if (army.Knights != null && army.Knights.HasKnights())
                                        {
                                            var bodyguardKnights = army.Knights.GetKnightsList();
                                            int totalBodyguardSoldiers = army.Knights.GetKnightsSoldiers();
                                            string attilaKey = unit.GetAttilaUnitKey();
                                            string faction = unit.GetAttilaFaction();

                                            Program.Logger.Debug($"  - Knight Bodyguard Unit (Total Soldiers: {totalBodyguardSoldiers})");

                                            if (!string.IsNullOrEmpty(attilaKey) && attilaKey != UnitMappers_BETA.NOT_FOUND_KEY)
                                            {
                                                foreach (var knight in bodyguardKnights.OrderByDescending(k => k.GetProwess()))
                                                {
                                                    string knightUnitDetails = $", Culture: {knight.GetCultureName()}, Heritage: {knight.GetHeritageName()}, Faction: {faction}";
                                                    Program.Logger.Debug($"    - Knight: {knight.GetName()}, Attila Unit: {attilaKey}, Soldiers: {knight.GetSoldiers()}{knightUnitDetails}, Prowess: {knight.GetProwess()}");
                                                }
                                            }
                                        }
                                        knightsLogged = true;
                                    }
                                    continue;
                                }
                                else if (unit.GetRegimentType() == RegimentType.Levy)
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
                                else // Commander and MenAtArms
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
                                            if (unit.GetName() == "General")
                                            {
                                                string commanderName = army.Commander?.Name ?? "Unknown Commander";
                                                string rankInfo = $", Rank: {unit.CharacterRank}";
                                                Program.Logger.Debug($"  - CK3 Unit: General ({commanderName}), Type: Commander, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}");
                                            }
                                        }
                                        else // MenAtArms
                                        {
                                            string rankInfo = "";
                                            string knightCommanderInfo = "";
                                            if (unit.KnightCommander != null)
                                            {
                                                knightCommanderInfo = $", Led by: {unit.KnightCommander.GetName()} (Prowess: {unit.KnightCommander.GetProwess()})";
                                            }
                                            Program.Logger.Debug($"  - CK3 Unit: {unit.GetName()}, Type: {unit.GetRegimentType()}, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}{knightCommanderInfo}");
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
                            bool leviesLogged = false;
                            bool knightsLogged = false;

                            foreach (var unit in army.Units)
                            {
                                string unitDetails = $", Culture: {unit.GetCulture()}, Heritage: {unit.GetHeritage()}, Faction: {unit.GetAttilaFaction()}";

                                if (unit.GetRegimentType() == RegimentType.Knight)
                                {
                                    if (!knightsLogged)
                                    {
                                        // This block will now execute only once when it sees the combined "Knight" unit.
                                        if (army.Knights != null && army.Knights.HasKnights())
                                        {
                                            var bodyguardKnights = army.Knights.GetKnightsList();
                                            int totalBodyguardSoldiers = army.Knights.GetKnightsSoldiers();
                                            string attilaKey = unit.GetAttilaUnitKey();
                                            string faction = unit.GetAttilaFaction();

                                            Program.Logger.Debug($"  - Knight Bodyguard Unit (Total Soldiers: {totalBodyguardSoldiers})");

                                            if (!string.IsNullOrEmpty(attilaKey) && attilaKey != UnitMappers_BETA.NOT_FOUND_KEY)
                                            {
                                                foreach (var knight in bodyguardKnights.OrderByDescending(k => k.GetProwess()))
                                                {
                                                    string knightUnitDetails = $", Culture: {knight.GetCultureName()}, Heritage: {knight.GetHeritageName()}, Faction: {faction}";
                                                    Program.Logger.Debug($"    - Knight: {knight.GetName()}, Attila Unit: {attilaKey}, Soldiers: {knight.GetSoldiers()}{knightUnitDetails}, Prowess: {knight.GetProwess()}");
                                                }
                                            }
                                        }
                                        knightsLogged = true;
                                    }
                                    continue;
                                }
                                else if (unit.GetRegimentType() == RegimentType.Levy)
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
                                else // Commander and MenAtArms
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
                                            if (unit.GetName() == "General")
                                            {
                                                string commanderName = army.Commander?.Name ?? "Unknown Commander";
                                                string rankInfo = $", Rank: {unit.CharacterRank}";
                                                Program.Logger.Debug($"  - CK3 Unit: General ({commanderName}), Type: Commander, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}");
                                            }
                                        }
                                        else // MenAtArms
                                        {
                                            string rankInfo = "";
                                            string knightCommanderInfo = "";
                                            if (unit.KnightCommander != null)
                                            {
                                                knightCommanderInfo = $", Led by: {unit.KnightCommander.GetName()} (Prowess: {unit.KnightCommander.GetProwess()})";
                                            }
                                            Program.Logger.Debug($"  - CK3 Unit: {unit.GetName()}, Type: {unit.GetRegimentType()}, Attila Unit: {attilaKey}, Soldiers: {unit.GetSoldiers()}{unitDetails}{rankInfo}{knightCommanderInfo}");
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

            form.ExecuteButton.Enabled = false;
            form.ContinueBattleButton.Enabled = false;
            form.LaunchAutoFixerButton.Enabled = false;

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
                            if (form != null && !form.IsDisposed)
                            {
                                form.Invoke((MethodInvoker)delegate
                                {
                                    MessageBox.Show(form,
                                        "The autofix process could not find any replaceable custom units in the armies.\n\nThe crash may be caused by something else (e.g., a game bug, outdated drivers, or a corrupted installation).\n\nThe battle will be aborted.",
                                        "Crusader Conflicts: Autofix Failed",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                                });
                            }
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
                            AutofixState.AutofixStrategy.ManualUnitReplacement,
                            AutofixState.AutofixStrategy.DeploymentZoneEditor
                        };

                        if (BattleState.IsSiegeBattle)
                        {
                            allStrategies.Remove(AutofixState.AutofixStrategy.MapSize);
                        }

                        var availableStrategies = allStrategies.Except(autofixState.TriedStrategies).ToList();

                        if (!availableStrategies.Any())
                        {
                            Program.Logger.Debug("Autofix failed. All strategies have been attempted.");
                            if (form != null && !form.IsDisposed)
                            {
                                form.Invoke((MethodInvoker)delegate
                                {
                                    MessageBox.Show(form,
                                        "The automatic fix failed. All available strategies were tried, but the game still crashed.\n\nThe battle will be aborted.",
                                        "Crusader Conflicts: Autofix Failed",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                });
                            }
                            return false; // All strategies exhausted
                        }

                        DialogResult userResponse = DialogResult.No;
                        AutofixState.AutofixStrategy? chosenStrategy = null;

                        if (form is null || form.IsDisposed)
                        {
                            Program.Logger.Debug("Autofix Error: Form is null or disposed. Cannot show strategy choice prompt.");
                            return false; // Abort battle
                        }
                        form.Invoke((MethodInvoker)delegate
                        {
                            (userResponse, chosenStrategy) = ShowPostCrashAutofixPrompt(form, availableStrategies);
                        });

                        if (userResponse == DialogResult.No || chosenStrategy == null)
                        {
                            Program.Logger.Debug("User declined autofix. Aborting battle.");
                            return false; // User cancelled
                        }

                        autofixState.CurrentStrategy = chosenStrategy;
                        Program.Logger.Debug($"User chose autofix strategy: {chosenStrategy.Value}.");
                    }

                    if (form != null && !form.IsDisposed)
                    {
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
                                    case AutofixState.AutofixStrategy.DeploymentZoneEditor:
                                        form.infoLabel.Text = "Loading deployment zone editor...";
                                        break;
                                }
                            }
                        });
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
                        case AutofixState.AutofixStrategy.ManualUnitReplacement:
                            (fixApplied, fixDescription) = TryManualUnitFix(autofixState, form);
                            break;
                        case AutofixState.AutofixStrategy.DeploymentZoneEditor:
                            (fixApplied, fixDescription) = TryDeploymentZoneEditorFix(autofixState, form);
                            break;
                    }

                    // 3. Handle the result of the fix attempt.
                    if (fixApplied)
                    {
                        autofixState.LastAppliedFixDescription = fixDescription;
                        if (form != null && !form.IsDisposed)
                        {
                            form.Invoke((MethodInvoker)delegate
                            {
                                form.infoLabel.Text = $"Attila crashed. Attempting fix #{autofixState.FailureCount}...";
                                form.Text = $"Crusader Conflicts (Attempting fix #{autofixState.FailureCount})";
                            });

                            form.Invoke((MethodInvoker)delegate
                            {
                                string messageText = $"Attempting fix #{autofixState.FailureCount}.\n\nThe application will now try {fixDescription} and restart the battle.\n\nPlease note this information if you plan to report a bug on our Discord server:";
                                string discordUrl = "https://discord.gg/eFZTprHh3j";
                                ShowClickableLinkMessageBox(form, messageText, "Crusader Conflicts: Applying Autofix", "Report on Discord: " + discordUrl, discordUrl, fixDescription);
                            });
                        }

                        // Mark the current strategy as tried and clear it for the next attempt
                        if (autofixState.CurrentStrategy.HasValue && autofixState.CurrentStrategy.Value != AutofixState.AutofixStrategy.ManualUnitReplacement && autofixState.CurrentStrategy.Value != AutofixState.AutofixStrategy.DeploymentZoneEditor)
                        {
                            autofixState.TriedStrategies.Add(autofixState.CurrentStrategy.Value);
                        }
                        autofixState.CurrentStrategy = null;

                        Program.Logger.Debug($"Relaunching battle after autofix ({fixDescription}). Re-reading army data from save files to apply changes.");
                        var (fresh_attackers, fresh_defenders) = ArmiesReader.ReadBattleArmies();
                        return await ProcessBattle(form, fresh_attackers, fresh_defenders, token, true, autofixState);
                    }
                    else
                    {
                        // The current strategy is exhausted or cancelled.
                        Program.Logger.Debug($"--- Autofix: Strategy {autofixState.CurrentStrategy} is complete for this attempt. ---");
                        if (autofixState.CurrentStrategy.HasValue && autofixState.CurrentStrategy.Value != AutofixState.AutofixStrategy.ManualUnitReplacement && autofixState.CurrentStrategy.Value != AutofixState.AutofixStrategy.DeploymentZoneEditor)
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

                    // --- START: Capture pre-battle state for report ---
                    Dictionary<Unit, int> deployedCounts = new Dictionary<Unit, int>();
                    if (client.ModOptions.optionsValuesCollection.TryGetValue("ShowPostBattleReport", out var showReportPre) && showReportPre == "Enabled")
                    {
                        foreach (var army in attacker_armies.Concat(defender_armies))
                        {
                            if (army.Units == null) continue;
                            foreach (var unit in army.Units)
                            {
                                if (unit != null)
                                {
                                    deployedCounts[unit] = unit.GetSoldiers();
                                }
                            }
                        }
                    }
                    // --- END: Capture pre-battle state for report ---


                    // --- START: Capture pre-battle state for logging ---
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
                    // --- END: Capture pre-battle state for logging ---
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

                    // SHOW POST-BATTLE REPORT
                    if (client.ModOptions.optionsValuesCollection.TryGetValue("ShowPostBattleReport", out var showReport) && showReport == "Enabled")
                    {
                        var report = GenerateBattleReportData(attacker_armies, defender_armies, winner, deployedCounts);
                        if (form != null && !form.IsDisposed)
                        {
                            form.Invoke((MethodInvoker)delegate
                            {
                                using (var reportForm = new PostBattleReportForm(report))
                                {
                                    reportForm.ShowDialog(form);
                                }
                            });
                        }
                    }

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
                            File.Copy(combatsSourcePath, combatResultsDestPath, true);
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

                    // Show successful autofix/manual tool message if applicable
                    bool wasAutofixSuccess = autofixState != null && !string.IsNullOrEmpty(autofixState.LastAppliedFixDescription);
                    bool wasManualToolUsed = BattleState.DeploymentZoneOverrideAttacker != null || BattleState.ManualUnitReplacements.Any();

                    if (wasAutofixSuccess || wasManualToolUsed)
                    {
                        string messageText;
                        string originalMapInfo = "";
                        string discordUrl = "https://discord.gg/eFZTprHh3j";
                        string title = "Crusader Conflicts: Battle Report";

                        if (wasAutofixSuccess)
                        {
                            title = "Crusader Conflicts: Autofix Successful";
                            messageText = $"The battle was successful after an automatic fix.\n\nThe fix that worked was: {autofixState.LastAppliedFixDescription}.\n\n";
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
                        }
                        else // wasManualToolUsed
                        {
                            messageText = "The battle was successful after applying manual tool settings.\n\nBelow are the settings that were used. Please consider reporting them on Discord if they fixed a crash, so the issue can be resolved in a future update.";
                        }


                        if (form != null && !form.IsDisposed)
                        {
                            form.Invoke((MethodInvoker)delegate
                            {
                                var report = new StringBuilder();
                                report.AppendLine("--- Crusader Conflicts Battle Report ---");
                                report.AppendLine($"Date: {DateTime.Now}");
                                report.AppendLine();

                                if (wasAutofixSuccess)
                                {
                                    report.AppendLine($"[Applied Fix]: {autofixState.LastAppliedFixDescription}");
                                    if (!string.IsNullOrEmpty(originalMapInfo))
                                    {
                                        report.AppendLine($"[Original Buggy Map]: {originalMapInfo}");
                                    }
                                    report.AppendLine();
                                }

                                // Manual Unit Replacements
                                if (BattleState.ManualUnitReplacements.Any())
                                {
                                    report.AppendLine("--- Manual Unit Replacements ---");
                                    foreach (var rep in BattleState.ManualUnitReplacements)
                                    {
                                        report.AppendLine($"Replaced '{rep.Key.originalKey}' with '{rep.Value.replacementKey}' for {(rep.Key.isPlayerAlliance ? "Player" : "Enemy")}");
                                    }
                                    report.AppendLine();
                                }

                                // Deployment Zone Overrides
                                if (BattleState.DeploymentZoneOverrideAttacker != null && BattleState.DeploymentZoneOverrideDefender != null)
                                {
                                    report.AppendLine("--- Manual Deployment Zones ---");
                                    var att = BattleState.DeploymentZoneOverrideAttacker;
                                    var def = BattleState.DeploymentZoneOverrideDefender;
                                    report.AppendLine($"Attacker Zone: Center=({att.X:F2}, {att.Y:F2}), Size=({att.Width:F2} x {att.Height:F2})");
                                    report.AppendLine($"Defender Zone: Center=({def.X:F2}, {def.Y:F2}), Size=({def.Width:F2} x {def.Height:F2})");
                                    report.AppendLine();
                                }

                                // Battle Map Info
                                var (mapX, mapY, _, _) = TerrainGenerator.GetBattleMap();
                                string provinceName = BattleResult.ProvinceName ?? "Unknown";
                                report.AppendLine("--- Battle Details ---");
                                report.AppendLine($"Location: {provinceName}");
                                report.AppendLine($"Map Coordinates: ({mapX}, {mapY})");

                                ShowClickableLinkMessageBox(form, messageText, title, "Report on Discord: " + discordUrl, discordUrl, report.ToString());
                            });
                        }
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

                //Data Clearas
                Data.Reset();
                return true; // Continue
            }


            return true; // Success
        }

        public static void ProcessProminentKnights(List<Army> attacker_armies, List<Army> defender_armies)
        {
            if (ModOptions.CombineKnightsEnabled()) return;

            Program.Logger.Debug("--- Processing Prominent Knights ---");

            var allArmies = attacker_armies.Concat(defender_armies).ToList();

            foreach (var army in allArmies)
            {
                if (army.Knights == null || !army.Knights.HasKnights())
                {
                    continue;
                }

                var allKnights = army.Knights.GetKnightsList();
                var prominentKnights = allKnights.Where(k => k.GetProwess() > 15).ToList();

                if (!prominentKnights.Any())
                {
                    // No prominent knights, so all knights get the bodyguard bonus
                    foreach (var knight in allKnights) { knight.IsProminent = true; }
                    Program.Logger.Debug($"Army {army.ID} has no prominent knights. All {allKnights.Count} knights will form the combined bodyguard unit.");
                    continue;
                }

                Program.Logger.Debug($"Army {army.ID} has {prominentKnights.Count} prominent knights to process.");

                var assignableMAA = army.Units
                    .Where(u => u.GetRegimentType() == RegimentType.MenAtArms && u.KnightCommander == null && !UnitMappers_BETA.IsUnitTypeSiege(u.GetRegimentType(), u.GetName(), u.GetAttilaFaction()))
                    .ToList();

                var assignedKnights = new List<Knight>();

                if (assignableMAA.Any())
                {
                    // Assign prominent knights to MAA units
                    foreach (var knight in prominentKnights.OrderByDescending(k => k.IsAccolade()))
                    {
                        Unit bestMAA = assignableMAA
                            .OrderByDescending(u => u.GetObjCulture()?.ID == knight.GetCultureObj()?.ID) // Culture match first
                            .ThenByDescending(u => u.GetSoldiers()) // Then strongest
                            .FirstOrDefault();

                        if (bestMAA != null)
                        {
                            bestMAA.KnightCommander = knight;
                            assignedKnights.Add(knight);
                            assignableMAA.Remove(bestMAA); // Unit is now taken
                            Program.Logger.Debug($"Assigned prominent knight {knight.GetName()} ({knight.GetID()}) to command MAA unit {bestMAA.GetName()} in army {army.ID}.");

                            if (!assignableMAA.Any())
                            {
                                break; // No more units to assign to
                            }
                        }
                    }
                }

                // All knights NOT assigned to an MAA unit get the 4x bodyguard multiplier.
                var unassignedKnights = allKnights.Except(assignedKnights).ToList();
                foreach (var knight in unassignedKnights)
                {
                    knight.IsProminent = true; // This triggers the 4x soldier calculation
                }
                Program.Logger.Debug($"{unassignedKnights.Count} knights (standard and unassigned prominent) will form the combined bodyguard unit for army {army.ID}.");


                // Finally, remove the assigned knights from the main KnightSystem list.
                if (assignedKnights.Any())
                {
                    army.Knights.GetKnightsList().RemoveAll(k => assignedKnights.Contains(k));
                }
            }
            Program.Logger.Debug("--- Finished Processing Prominent Knights ---");
        }

        public static async Task CleanupAfterBattle()
        {
            await Task.Delay(10);

            Program.Logger.Debug("Resetting unit sizes for next battle.");
            ArmyProportions.ResetUnitSizes();
            GC.Collect();

            // Clear battle state after successful completion
            BattleState.ClearAutofixOverrides(); // Clear any manual or auto fixes for the next battle
            BattleState.ClearBattleState();
            TerrainGenerator.Clear();
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

            // --- START: Added processing step for consistency ---
            // This block ensures that armies are fully processed (levies expanded, commanders added, etc.)
            // before being displayed in the Unit Replacer. This makes the manual tool's behavior
            // consistent with the post-crash autofix scenario.
            Program.Logger.Debug("TryManualUnitFix: Pre-processing armies for Unit Replacer...");

            // Clear any previous temporary battle file data to avoid conflicts.
            BattleFile.ClearFile();
            DeclarationsFile.Erase();
            Data.units_scripts.Clear();

            // Determine if it's a siege battle to erase the correct script.
            // This is necessary because BattleState might not be initialized when called from the main menu.
            bool isSiegeBattle = File.Exists(Writter.DataFilesPaths.Sieges_Path()) && new FileInfo(Writter.DataFilesPaths.Sieges_Path()).Length > 10;
            BattleScript.EraseScript(isSiegeBattle);

            // Process each army to expand its unit list. This has side effects of writing to the
            // temporary battle files, but this is acceptable as they will be overwritten by the
            // actual battle generation process later.
            UnitsFile.ResetProcessedArmies();
            foreach (var army in autofixState.OriginalAttackerArmies.Concat(autofixState.OriginalDefenderArmies))
            {
                UnitsFile.BETA_ConvertandAddArmyUnits(army);
            }
            Program.Logger.Debug("TryManualUnitFix: Army pre-processing complete.");
            // --- END: Added processing step ---


            // 1. Set army sides to ensure IsPlayer() is correct on units
            BattleFile.SetArmiesSides(autofixState.OriginalAttackerArmies, autofixState.OriginalDefenderArmies);
            Program.Logger.Debug("TryManualUnitFix: Army sides set.");

            // 2. Collect data
            var allArmies = autofixState.OriginalAttackerArmies.Concat(autofixState.OriginalDefenderArmies).ToList();
            Program.Logger.Debug($"TryManualUnitFix: Collected {allArmies.Count} total armies.");

            // Expand levy placeholders into actual units so they appear in the replacer tool
            CrusaderWars.data.save_file.Armies_Functions.ExpandLevyArmies(allArmies);

            var currentUnits = allArmies.Where(a => a.Units != null).SelectMany(a => a.Units)
                                        .Where(u => u != null && !string.IsNullOrEmpty(u.GetAttilaUnitKey()) && u.GetAttilaUnitKey() != UnitMappers_BETA.NOT_FOUND_KEY)
                                        .ToList();
            Program.Logger.Debug($"TryManualUnitFix: Collected {currentUnits.Count} current units with valid keys.");

            var allAvailableUnits = UnitMappers_BETA.GetAllAvailableUnits();
            var unitScreenNames = UnitsCardsNames.GetUnitScreenNames(UnitMappers_BETA.GetLoadedUnitMapperName() ?? "");

            if (unitScreenNames is null)
            {
                Program.Logger.Debug("ERROR: unitScreenNames is null, cannot launch UnitReplacerForm.");
                MessageBox.Show(form, "Could not load unit names required for the manual replacer. The process cannot continue.", "Crusader Conflicts: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, "");
            }
            if (allAvailableUnits is null)
            {
                Program.Logger.Debug("ERROR: allAvailableUnits is null, cannot launch UnitReplacerForm.");
                MessageBox.Show(form, "Could not load the list of available units. The process cannot continue.", "Crusader Conflicts: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, "");
            }

            // Filter units to only those that have a screen name to prevent crashes inside the form.
            var availableUnits = allAvailableUnits.Where(u => u != null && !string.IsNullOrEmpty(u.AttilaUnitKey) && unitScreenNames.ContainsKey(u.AttilaUnitKey)).ToList();
            Program.Logger.Debug($"TryManualUnitFix: Collected {currentUnits.Count} current units with valid keys.");
            Program.Logger.Debug($"TryManualUnitFix: Collected {allAvailableUnits.Count} total available units, filtered down to {availableUnits.Count} with screen names.");
            Program.Logger.Debug($"TryManualUnitFix: Collected {unitScreenNames.Count} unit screen names.");


            // 3. Show form
            Dictionary<(string originalKey, bool isPlayerAlliance), (string replacementKey, bool isSiege)> replacements = new Dictionary<(string, bool), (string, bool)>();
            bool userCommitted = false;
            if (form is null || form.IsDisposed)
            {
                Program.Logger.Debug("Autofix Error: Form is null or disposed. Cannot show manual unit replacer.");
                return (false, ""); // Cannot apply this fix without a form
            }
            form.Invoke((MethodInvoker)delegate
            {
                Program.Logger.Debug("TryManualUnitFix: Invoking UnitReplacerForm creation...");
                using (var replacerForm = new client.UnitReplacerForm(currentUnits, availableUnits, BattleState.ManualUnitReplacements, unitScreenNames))
                {
                    Program.Logger.Debug("TryManualUnitFix: UnitReplacerForm created. Showing dialog...");
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
                                return $"{name} [{maxCategory}] [{g.Key}]";
                            }
                        }
                        return $"{name} [{g.Key}]";
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

        public static (DialogResult result, AutofixState.AutofixStrategy? strategy) ShowManualToolsPrompt(IWin32Window owner)
        {
            var manualStrategies = new List<AutofixState.AutofixStrategy>
            {
                AutofixState.AutofixStrategy.ManualUnitReplacement,
                AutofixState.AutofixStrategy.DeploymentZoneEditor
            };

            using (Form prompt = new Form())
            {
                prompt.Width = 750;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.Sizable;
                prompt.AutoScroll = true;
                prompt.Text = "Crusader Conflicts: Manual Battle Tools";

                Label textLabel = new Label()
                {
                    Left = 20,
                    Top = 20,
                    Width = 710,
                    Height = 50,
                    Text = "Select a tool to manually configure the next battle. These changes can help prevent crashes before they happen."
                };
                prompt.Controls.Add(textLabel);

                var manualStrategyControls = new Dictionary<AutofixState.AutofixStrategy, (RadioButton rb, Label lbl)>
                {
                    { AutofixState.AutofixStrategy.ManualUnitReplacement, (new RadioButton() { Text = "Unit Replacer", AutoSize = true }, new Label() { Text = "Manually replace specific units in your army with any available unit.", Width = 400, Height = 40, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.DeploymentZoneEditor, (new RadioButton() { Text = "Deployment Zone Editor", AutoSize = true }, new Label() { Text = "Manually adjust the size and position of deployment zones.", Width = 400, Height = 40, ForeColor = System.Drawing.Color.Gray }) }
                };

                int currentTop = textLabel.Bottom + 20;
                bool first = true;

                GroupBox manualFixGroup = new GroupBox() { Text = "Manual Tools", Left = 20, Top = currentTop, Width = 710 };
                prompt.Controls.Add(manualFixGroup);
                int currentManualFixTop = 20;

                foreach (var strategy in manualStrategies)
                {
                    if (manualStrategyControls.TryGetValue(strategy, out var controls))
                    {
                        controls.rb.Left = 10;
                        controls.rb.Top = currentManualFixTop;
                        if (first) { controls.rb.Checked = true; first = false; }
                        manualFixGroup.Controls.Add(controls.rb);

                        controls.lbl.Left = 30;
                        controls.lbl.Top = currentManualFixTop + 20;
                        manualFixGroup.Controls.Add(controls.lbl);
                        currentManualFixTop += 90;
                    }
                }
                manualFixGroup.Height = currentManualFixTop;

                int buttonsTop = manualFixGroup.Bottom + 20;

                Button btnContinue = new Button() { Text = "Continue", Left = (prompt.Width / 2) - 110, Width = 100, Top = buttonsTop, DialogResult = DialogResult.Yes };
                Button btnCancel = new Button() { Text = "Cancel", Left = (prompt.Width / 2) + 10, Width = 100, Top = buttonsTop, DialogResult = DialogResult.No };

                prompt.Height = buttonsTop + 100;

                btnContinue.Click += (sender, e) => { prompt.Close(); };
                btnCancel.Click += (sender, e) => { prompt.Close(); };

                prompt.Controls.Add(btnContinue);
                prompt.Controls.Add(btnCancel);
                prompt.AcceptButton = btnContinue;
                prompt.CancelButton = btnCancel;

                var dialogResult = prompt.ShowDialog(owner);

                AutofixState.AutofixStrategy? selectedStrategy = null;
                if (dialogResult == DialogResult.Yes)
                {
                    foreach (var strategy in manualStrategies)
                    {
                        if (manualStrategyControls[strategy].rb.Checked)
                        {
                            selectedStrategy = strategy;
                            break;
                        }
                    }
                }
                return (dialogResult, selectedStrategy);
            }
        }

        public static (DialogResult result, AutofixState.AutofixStrategy? strategy) ShowPostCrashAutofixPrompt(IWin32Window owner, List<AutofixState.AutofixStrategy> availableStrategies, bool isAfterCrash = true)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 550;
                prompt.Height = 750; // Increased height
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.Sizable;
                prompt.AutoScroll = true;

                Label textLabel = new Label()
                {
                    Left = 20,
                    Top = 20,
                    Width = 510,
                };

                if (isAfterCrash)
                {
                    prompt.Text = "Autofixer: Crusader Conflicts: Attila Crash Detected";
                    textLabel.Height = 70;
                    textLabel.Text = "It appears Total War: Attila has crashed. This is often caused by an incompatible custom unit or map.\n\nThe application will now attempt a fix. If it fails, you will be prompted again.\n\nPlease select which automatic fix strategy to try next:";
                }
                else
                {
                    prompt.Text = "Crusader Conflicts: Battle Tools";
                    textLabel.Height = 50;
                    textLabel.Text = "Select a tool to manually configure or apply a fix for the next battle. These changes can help prevent crashes before they happen.";
                }
                prompt.Controls.Add(textLabel);

                var allStrategyControls = new Dictionary<AutofixState.AutofixStrategy, (RadioButton rb, Label lbl)>
                {
                    { AutofixState.AutofixStrategy.Units, (new RadioButton() { Text = "Change Units (Automatic)", AutoSize = true }, new Label() { Text = "Replaces custom mod units one-by-one with default units. Good for a specific buggy unit.", Width = 400, Height = 40, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.MapSize, (new RadioButton() { Text = "Change Map Size (Automatic)", AutoSize = true }, new Label() { Text = "Increases deployment area. Good for crashes with very large armies.", Width = 400, Height = 40, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.Deployment, (new RadioButton() { Text = "Change Deployment (Automatic)", AutoSize = true }, new Label() { Text = "Rotates deployment zones or attacker direction. Good for units spawning in bad terrain.", Width = 400, Height = 40, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.MapVariant, (new RadioButton() { Text = "Change Map (Automatic)", AutoSize = true }, new Label() { Text = "Switches to a different map for the same location. Good for a buggy map file.", Width = 400, Height = 40, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.ManualUnitReplacement, (new RadioButton() { Text = "Unit Replacer", AutoSize = true }, new Label() { Text = "Manually replace specific units in your army with any available unit.", Width = 400, Height = 40, ForeColor = System.Drawing.Color.Gray }) },
                    { AutofixState.AutofixStrategy.DeploymentZoneEditor, (new RadioButton() { Text = "Deployment Zone Editor", AutoSize = true }, new Label() { Text = "Manually adjust the size and position of deployment zones.", Width = 400, Height = 40, ForeColor = System.Drawing.Color.Gray }) }
                };

                int currentTop = textLabel.Bottom + 20;
                bool first = true;

                // Create a single GroupBox for all tools
                GroupBox toolsGroup = new GroupBox() { Text = "Available Tools", Left = 20, Top = currentTop, Width = 510 };
                prompt.Controls.Add(toolsGroup);
                int currentToolTop = 20;

                foreach (var strategy in availableStrategies)
                {
                    if (allStrategyControls.TryGetValue(strategy, out var controls))
                    {
                        controls.rb.Left = 10;
                        controls.rb.Top = currentToolTop;
                        if (first) { controls.rb.Checked = true; first = false; }
                        toolsGroup.Controls.Add(controls.rb);

                        controls.lbl.Left = 30;
                        controls.lbl.Top = currentToolTop + 20;
                        toolsGroup.Controls.Add(controls.lbl);
                        currentToolTop += 70;
                    }
                }

                toolsGroup.Height = currentToolTop;

                int buttonsTop = toolsGroup.Bottom + 20;

                Button btnStart = new Button() { Text = "OK", Left = 175, Width = 100, Top = buttonsTop, DialogResult = DialogResult.Yes, Anchor = AnchorStyles.Bottom };
                Button btnCancel = new Button() { Text = "Cancel", Left = 295, Width = 100, Top = buttonsTop, DialogResult = DialogResult.No, Anchor = AnchorStyles.Bottom };

                // Adjust button position based on new height
                btnStart.Top = prompt.ClientSize.Height - btnStart.Height - 10;
                btnCancel.Top = prompt.ClientSize.Height - btnCancel.Height - 10;


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

        private static void ShowClickableLinkMessageBox(IWin32Window owner, string text, string title, string linkText, string linkUrl, string reportContent)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 600;
                prompt.Height = 450;
                prompt.Text = title;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.Sizable;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label()
                {
                    Left = 20,
                    Top = 20,
                    Width = 550,
                    Height = 60,
                    Text = text,
                };

                TextBox reportBox = new TextBox()
                {
                    Left = 20,
                    Top = 80,
                    Width = 550,
                    Height = 220,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    ReadOnly = true,
                    Text = reportContent,
                    Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)))
                };

                LinkLabel linkLabel = new LinkLabel()
                {
                    Left = 20,
                    Top = 310,
                    Width = 550,
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

                Button copyButton = new Button()
                {
                    Text = "Copy Details",
                    Left = 20,
                    Width = 120,
                    Top = 340,
                };
                copyButton.Click += (sender, e) => {
                    Clipboard.SetText(reportBox.Text);
                };


                Button confirmation = new Button()
                {
                    Text = "OK",
                    Left = 450,
                    Width = 120,
                    Top = 340,
                    DialogResult = DialogResult.OK
                };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(reportBox);
                prompt.Controls.Add(linkLabel);
                prompt.Controls.Add(copyButton);
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


        private static (bool, string) TryDeploymentZoneEditorFix(AutofixState autofixState, HomePage form)
        {
            Program.Logger.Debug("--- Autofix: Initiating Deployment Zone Editor ---");

            try
            {
                var allArmies = autofixState.OriginalAttackerArmies.Concat(autofixState.OriginalDefenderArmies).ToList();
                int total_soldiers = allArmies.Sum(a => a.GetTotalSoldiers());
                string option_map_size = ModOptions.DeploymentsZones();

                // Determine which side is the player
                BattleFile.SetArmiesSides(autofixState.OriginalAttackerArmies, autofixState.OriginalDefenderArmies);
                bool isAttackerPlayer = autofixState.OriginalAttackerArmies.First().IsPlayer();

                // Create temporary deployment areas to pass to the form
                // Directions are placeholders; the user will adjust the final position and size.
                string attacker_direction = BattleState.IsSiegeBattle ? (BattleState.OriginalSiegeAttackerDirection ?? "N") : "N";
                string defender_direction = "S";

                DeploymentArea attackerArea = new DeploymentArea(attacker_direction, option_map_size, total_soldiers);
                DeploymentArea defenderArea = new DeploymentArea(defender_direction, option_map_size, total_soldiers, BattleState.IsSiegeBattle);
                float map_dimension = float.Parse(ModOptions.SetMapSize(total_soldiers, BattleState.IsSiegeBattle), System.Globalization.CultureInfo.InvariantCulture);

                bool userCommitted = false;
                DeploymentZoneToolForm.DeploymentZoneValues? attackerValues = null;
                DeploymentZoneToolForm.DeploymentZoneValues? defenderValues = null;

                if (form is null || form.IsDisposed)
                {
                    Program.Logger.Debug("Autofix Error: Form is null or disposed. Cannot show deployment zone tool.");
                    return (false, "");
                }
                
                // Get Battle Details
                if (!BattleState.IsSiegeBattle)
                {
                    TerrainGenerator.CheckForSpecialCrossingBattle(autofixState.OriginalAttackerArmies, autofixState.OriginalDefenderArmies);
                }
                var (mapX, mapY, _, _) = TerrainGenerator.GetBattleMap();
                string provinceName = BattleResult.ProvinceName ?? "Unknown";
                string battleDate = $"{Date.Day}/{Date.Month}/{Date.Year}";
                string battleType;
                if (BattleState.IsSiegeBattle) {
                    battleType = "Siege Battle";
                } else if (TerrainGenerator.isRiver || TerrainGenerator.isStrait) {
                    battleType = "River/Strait Battle";
                } else if (TerrainGenerator.isCoastal) {
                    battleType = "Coastal Battle";
                } else {
                    battleType = "Field Battle";
                }

                form.Invoke((MethodInvoker)delegate
                {
                    using (var toolForm = new client.DeploymentZoneToolForm(attackerArea, defenderArea, map_dimension, isAttackerPlayer, BattleState.IsSiegeBattle, battleDate, battleType, provinceName, mapX, mapY))
                    {
                        if (toolForm.ShowDialog(form) == DialogResult.OK)
                        {
                            attackerValues = toolForm.GetAttackerValues();
                            defenderValues = toolForm.GetDefenderValues();
                            userCommitted = true;
                        }
                    }
                });

                if (userCommitted && attackerValues != null && defenderValues != null)
                {
                    BattleState.DeploymentZoneOverrideAttacker = new BattleState.ZoneOverride
                    {
                        X = (float)attackerValues.CenterX,
                        Y = (float)attackerValues.CenterY,
                        Width = (float)attackerValues.Width,
                        Height = (float)attackerValues.Height
                    };
                    BattleState.DeploymentZoneOverrideDefender = new BattleState.ZoneOverride
                    {
                        X = (float)defenderValues.CenterX,
                        Y = (float)defenderValues.CenterY,
                        Width = (float)defenderValues.Width,
                        Height = (float)defenderValues.Height
                    };

                    string fixDescription = "manually adjusting deployment zones via the tool";
                    Program.Logger.Debug($"Applying manual deployment zones: Attacker(X:{attackerValues.CenterX}, Y:{attackerValues.CenterY}, W:{attackerValues.Width}, H:{attackerValues.Height}), Defender(X:{defenderValues.CenterX}, Y:{defenderValues.CenterY}, W:{defenderValues.Width}, H:{defenderValues.Height})");
                    return (true, fixDescription);
                }
                else
                {
                    Program.Logger.Debug("Deployment zone tool was cancelled or no changes were made.");
                    return (false, "");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error in TryDeploymentZoneEditorFix: {ex.Message}");
                MessageBox.Show(form, $"An error occurred while trying to launch the Deployment Zone Editor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, "");
            }
        }

        private static BattleReport GenerateBattleReportData(List<Army> attacker_armies, List<Army> defender_armies, string winner, Dictionary<Unit, int> deployedCounts)
        {
            var report = new BattleReport();

            var playerArmy = attacker_armies.FirstOrDefault(a => a.IsPlayer()) ?? defender_armies.FirstOrDefault(a => a.IsPlayer());
            bool isPlayerAttacker = playerArmy != null && attacker_armies.Contains(playerArmy);

            if (winner == "attacker")
            {
                report.BattleResult = isPlayerAttacker ? "Victory" : "Defeat";
            }
            else // defender wins or draw
            {
                report.BattleResult = !isPlayerAttacker ? "Victory" : "Defeat";
            }

            // Populate new battle details
            report.BattleName = BattleDetails.Name ?? "Unknown Battle"; // Use actual battle name
            report.BattleDate = $"{Date.Year}-{Date.Month:D2}-{Date.Day:D2}"; // YYYY-MM-DD format
            report.LocationDetails = CultureInfo.CurrentCulture.TextInfo.ToTitleCase((TerrainGenerator.TerrainType ?? "Unknown Terrain").Replace("_", " ")); // Title case and replace underscores
            report.ProvinceName = BattleResult.ProvinceName ?? "Unknown Province"; // Initial attempt from DataSearch

            // If ProvinceName is still "Unknown Province" (meaning it wasn't found in DataSearch for field battles),
            // try to extract it from the BattleName.
            if (report.ProvinceName == "Unknown Province" && !string.IsNullOrEmpty(report.BattleName))
            {
                Match match = Regex.Match(report.BattleName, @"Battle of (the )?(?<ProvinceName>.+)");
                if (match.Success)
                {
                    string extractedProvinceName = match.Groups["ProvinceName"].Value.Trim();
                    if (!string.IsNullOrEmpty(extractedProvinceName))
                    {
                        report.ProvinceName = extractedProvinceName;
                        Program.Logger.Debug($"Extracted ProvinceName '{report.ProvinceName}' from BattleName '{report.BattleName}'.");
                    }
                }
            }

            report.TimeOfDay = "Day"; // Currently hardcoded in BattleFile.SetBattleDescription
            report.Season = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Date.GetSeason()); // Capitalize season
            report.Weather = Weather.GetWeather(); // Still generate, but not displayed in UI


            // --- Calculate Kills ---
            var unitKills = new Dictionary<Unit, int>();
            foreach (var army in attacker_armies.Concat(defender_armies))
            {
                if (army.CasualitiesReports == null) continue;

                foreach (var unit in army.Units)
                {
                    if (unit == null) continue;

                    // Find matching report
                    var casualtyReport = army.CasualitiesReports.FirstOrDefault(r =>
                        r.GetUnitType() == unit.GetRegimentType() &&
                        r.GetTypeName() == (unit.GetRegimentType() == RegimentType.Levy ? "Levy" : unit.GetName()) &&
                        r.GetCulture()?.ID == unit.GetObjCulture()?.ID);
                    
                    int kills = casualtyReport != null ? casualtyReport.GetKilled() : 0;
                    
                    // The problematic block for proportional levy kill calculation is removed here.
                    
                    unitKills[unit] = kills;
                }
            }
            // --- End Calculate Kills ---


            // Calculate actual kills from battle results
            int attackerTotalKills = defender_armies.Sum(a => a.GetTotalLosses());
            int defenderTotalKills = attacker_armies.Sum(a => a.GetTotalLosses());

            report.AttackerSide = new SideReport { SideName = "Attackers" };
            PopulateSideReport(report.AttackerSide, attacker_armies, deployedCounts, unitKills, defenderTotalKills);

            report.DefenderSide = new SideReport { SideName = "Defenders" };
            PopulateSideReport(report.DefenderSide, defender_armies, deployedCounts, unitKills, attackerTotalKills);

            // Ensure kills match losses by setting the properties directly
            report.AttackerSide.TotalDeployed = report.AttackerSide.Armies.Sum(a => a.TotalDeployed);
            report.AttackerSide.TotalLosses = report.AttackerSide.Armies.Sum(a => a.TotalLosses);
            report.AttackerSide.TotalRemaining = report.AttackerSide.Armies.Sum(a => a.TotalRemaining);
            report.AttackerSide.TotalKills = defenderTotalKills; // Attacker kills = defender losses
            
            report.DefenderSide.TotalDeployed = report.DefenderSide.Armies.Sum(a => a.TotalDeployed);
            report.DefenderSide.TotalLosses = report.DefenderSide.Armies.Sum(a => a.TotalLosses);
            report.DefenderSide.TotalRemaining = report.DefenderSide.Armies.Sum(a => a.TotalRemaining);
            report.DefenderSide.TotalKills = attackerTotalKills; // Defender kills = attacker losses

            if (BattleState.IsSiegeBattle)
            {
                string path_log_attila = Properties.Settings.Default.VAR_log_attila;
                var left_side_combat_side = attacker_armies[0].CombatSide;
                var right_side_combat_side = defender_armies[0].CombatSide;
                var (outcome, wall_damage) = BattleResult.GetSiegeOutcome(path_log_attila, left_side_combat_side, right_side_combat_side);
                
                report.SiegeResult = outcome;
                report.WallDamage = wall_damage;
            }
            else
            {
                report.SiegeResult = "N/A";
                report.WallDamage = "N/A";
            }

            return report;
        }

        private static void PopulateSideReport(SideReport sideReport, List<Army> armies, Dictionary<Unit, int> deployedCounts, Dictionary<Unit, int> unitKills, int sideTotalKills)
        {
            // Calculate side totals first
            int sideTotalDeployed = armies.Sum(a => a.GetTotalDeployed());
            int sideTotalLosses = armies.Sum(a => a.GetTotalLosses());
            int sideTotalRemaining = armies.Sum(a => a.GetTotalRemaining());
            // Note: sideTotalKills is passed as a parameter, so we don't need to calculate it again

            foreach (var army in armies)
            {
                if (army.Commander == null) continue;

                // Determine the correct army name based on BattleFile.AddArmyName logic
                string armyDisplayName;
                if (army.IsPlayer() && army.isMainArmy)
                {
                    armyDisplayName = CharacterDataManager.GetPlayerRealmName();
                }
                else if (army.IsEnemy() && army.isMainArmy)
                {
                    armyDisplayName = CharacterDataManager.GetEnemyRealmName();
                }
                else
                {
                    armyDisplayName = "Allied Army";
                }

                var armyReport = new ArmyReport
                {
                    ArmyName = armyDisplayName,
                    CommanderName = army.Commander.Name
                };

                // Calculate actual losses for this army
                int armyTotalLosses = army.GetTotalLosses();
                int armyTotalRemaining = army.GetTotalRemaining();
                int armyTotalDeployed = army.GetTotalDeployed();

                // Calculate kills for this army (proportional to its contribution to the side's total losses)
                int armyTotalKills = 0;
                if (sideTotalLosses > 0)
                {
                    double armyContribution = (double)armyTotalLosses / sideTotalLosses;
                    armyTotalKills = (int)Math.Round(sideTotalKills * armyContribution);
                }

                // Process unit-level data
                if (army.Units != null)
                {
                    foreach (var unit in army.Units)
                    {
                        if (unit == null) continue;

                        int deployed = deployedCounts.TryGetValue(unit, out var count) ? count : 0;
                        int remaining = unit.GetSoldiers();
                        int losses = Math.Max(0, deployed - remaining);
                        int kills = unitKills.TryGetValue(unit, out var k) ? k : 0;

                        var unitReport = new UnitReport
                        {
                            AttilaUnitName = string.IsNullOrEmpty(unit.GetLocName()) ? unit.GetName() : unit.GetLocName(),
                            Deployed = deployed,
                            Remaining = remaining,
                            Losses = losses,
                            Kills = kills,
                            Ck3UnitType = unit.GetRegimentType().ToString(),
                            AttilaUnitKey = unit.GetAttilaUnitKey(),
                            Ck3Heritage = unit.GetHeritage(),
                            Ck3Culture = unit.GetCulture(),
                            AttilaFaction = unit.GetAttilaFaction(),
                        };

                        if (unit.GetRegimentType() == RegimentType.Commander)
                        {
                            unitReport.Characters.Add(GetCharacterReport(army.Commander));
                        }
                        else if (unit.GetRegimentType() == RegimentType.Knight)
                        {
                            if (army.Knights != null)
                            {
                                foreach (var knight in army.Knights.GetKnightsList())
                                {
                                    unitReport.Characters.Add(GetCharacterReport(knight));
                                }
                            }
                        }

                        armyReport.Units.Add(unitReport);
                    }
                }

                // Add siege engines to the army report (only for attacker armies in siege battles)
                if (army.SiegeEngines != null && army.SiegeEngines.Any())
                {
                    foreach (var siegeEngine in army.SiegeEngines)
                    {
                        armyReport.SiegeEngines.Add(new SiegeEngineReport
                        {
                            Name = siegeEngine.Key,
                            Quantity = siegeEngine.Value
                        });
                    }
                }

                // Assign army-level totals
                armyReport.TotalDeployed = armyTotalDeployed;
                armyReport.TotalLosses = armyTotalLosses;
                armyReport.TotalRemaining = armyTotalRemaining;
                armyReport.TotalKills = armyTotalKills;

                sideReport.Armies.Add(armyReport);
            }

            // Set the side-level totals directly (since properties are now writable)
            sideReport.TotalDeployed = sideTotalDeployed;
            sideReport.TotalLosses = sideTotalLosses;
            sideReport.TotalRemaining = sideTotalRemaining;
            sideReport.TotalKills = sideTotalKills;
        }

        private static CharacterReport GetCharacterReport(dynamic character)
        {
            string characterName;
            
            // Handle different character types
            try
            {