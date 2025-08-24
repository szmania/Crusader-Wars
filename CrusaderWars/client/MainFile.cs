using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Media;
using System.Linq;
using System.Drawing;
using CrusaderWars.client;
using CrusaderWars.locs;
using CrusaderWars.data.attila_settings;
using CrusaderWars.data.save_file;
using CrusaderWars.unit_mapper;
using CrusaderWars.terrain;
using CrusaderWars.twbattle;
using System.Threading;
using CrusaderWars.mod_manager;
using System.Xml;
using IWshRuntimeLibrary;
using System.Web;
using System.Windows.Media;
using System.Drawing.Text;


namespace CrusaderWars
{
    public partial class HomePage : Form
    {
        private LoadingScreen loadingScreen;
        private Thread loadingThread;
        private string log;  // For CK3 log content
        private bool _programmaticClick = false;
        private bool battleJustCompleted = false;
        private string _appVersion;
        private string _umVersion;
        private Updater _updater;
        

        const string SEARCH_KEY = "CRUSADERWARS3";

        private int _myVariable = 0;
        public HomePage()
        {
            Program.Logger.Debug("HomePage initializing...");
            LoadFont();
            InitializeComponent();
            
            // Add hover effects for links
            viewLogsLink.MouseEnter += (sender, e) => viewLogsLink.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150);
            viewLogsLink.MouseLeave += (sender, e) => viewLogsLink.ForeColor = System.Drawing.Color.WhiteSmoke;
            discordLink.MouseEnter += (sender, e) => discordLink.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150);
            discordLink.MouseLeave += (sender, e) => discordLink.ForeColor = System.Drawing.Color.WhiteSmoke;

            labelVersion.MouseEnter += (sender, e) => { labelVersion.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150); labelVersion.Cursor = Cursors.Hand; };
            labelVersion.MouseLeave += (sender, e) => { labelVersion.ForeColor = System.Drawing.Color.WhiteSmoke; labelVersion.Cursor = Cursors.Default; };
            labelMappersVersion.MouseEnter += (sender, e) => { labelMappersVersion.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150); labelMappersVersion.Cursor = Cursors.Hand; };
            labelMappersVersion.MouseLeave += (sender, e) => { labelMappersVersion.ForeColor = System.Drawing.Color.WhiteSmoke; labelMappersVersion.Cursor = Cursors.Default; };
            
            Thread.Sleep(1000);

            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            debugLog_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\console_history.txt";
            saveGames_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\save games";
            Program.Logger.Debug($"Documents Path: {documentsPath}");
            Program.Logger.Debug($"CK3 Log Path: {debugLog_Path}");
            Program.Logger.Debug($"Save Games Path: {saveGames_Path}");

            //Icon
            this.Icon = Properties.Resources.logo;

            Properties.Settings.Default.VAR_log_attila = string.Empty;
            Properties.Settings.Default.VAR_dir_save = string.Empty;
            Properties.Settings.Default.VAR_log_ck3 = string.Empty;


            Properties.Settings.Default.VAR_dir_save = saveGames_Path;
            Properties.Settings.Default.VAR_log_ck3 = debugLog_Path;
            Properties.Settings.Default.Save();

            Program.Logger.Debug("Starting updater checks...");
            _updater = new Updater();
            _updater.CheckAppVersion();
            _updater.CheckUnitMappersVersion();
            _appVersion = _updater.AppVersion;
            _umVersion = _updater.UMVersion;
            labelVersion.Text = $"V{_appVersion}";
            labelMappersVersion.Text = $"(mappers v{_umVersion})";
            Program.Logger.Debug($"Current App Version: {_updater.AppVersion}");

            var _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1000; // check variable every second
            _timer.Tick += Timer_Tick;
            _timer.Start();
            Original_Color = infoLabel.ForeColor;
            Program.Logger.Debug("HomePage initialization complete.");
        }

        private PrivateFontCollection fonts = new PrivateFontCollection();
        private Font customFont;

        void LoadFont()
        {
            string fontPath = @".\font\Paradox_King_Script.otf";
            if (System.IO.File.Exists(fontPath))
            {
                fonts.AddFontFile(fontPath);
                customFont = new Font(fonts.Families[0], 12f); // Specify the size you want
            }
            else
            {
                MessageBox.Show("Font file not found.", "Crusader Wars: Font error");
            }
        }



        System.Drawing.Color Original_Color;

        bool VerifyGamePaths()
        {
            string ck3Executable = Path.GetFileName(Properties.Settings.Default.VAR_ck3_path).ToLower();
            string attilaExecutable = Path.GetFileName(Properties.Settings.Default.VAR_attila_path).ToLower();

            // Strict matching to make sure the executable is given.
            if ((ck3Executable == "ck3" || ck3Executable == "ck3.exe") && (attilaExecutable == "attila" || attilaExecutable == "attila.exe"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        bool VerifyEnabledUnitMappers()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@".\settings\UnitMappers.xml");
            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                if (node is XmlComment) continue;
                if (node.InnerText == "True")
                {
                    return true;
                }
            }

            return false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_myVariable == 0)
            {
                if (BattleState.IsBattleInProgress()) return;

                bool gamePaths = VerifyGamePaths();
                bool unitMappers = VerifyEnabledUnitMappers();


                if(!gamePaths || !unitMappers)
                {
                    infoLabel.AutoSize = false;
                    infoLabel.Size = new Size(MainPanelLayout.Width - 10, 80);
                    if(!gamePaths) infoLabel.Text = "Games Paths Missing! Select your game paths on the Settings screen.";
                    else infoLabel.Text = "No Unit Mappers Enabled! Select a Playthrough on the Settings screen.";
                    ExecuteButton.Enabled = false;
                    infoLabel.ForeColor = System.Drawing.Color.FromArgb(74, 0, 0);
                }
                else if(gamePaths && unitMappers)
                {
                    infoLabel.AutoSize = true;
                    ExecuteButton.Enabled = true;
                    infoLabel.Text = "Ready to Start!";
                    infoLabel.ForeColor = Original_Color;
                }
            }

        }

        string path_editedSave;

        static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static string debugLog_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\console_history.txt";
        string saveGames_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\save games";
        private void Form1_Load(object sender, EventArgs e)
        {
            Program.Logger.Debug("Form1_Load event triggered.");
            //Load Game Paths
            Options.ReadGamePaths();    

            //Hide debug button
            btt_debug.Visible = false;

            //Early Access label visibility
            EA_Label.Visible = false;

            System.Drawing.Color myColor = System.Drawing.Color.FromArgb(53, 25, 5, 5);
            infoLabel.BackColor = myColor;
            labelVersion.BackColor = myColor;
            labelMappersVersion.BackColor = myColor;
            EA_Label.BackColor = myColor;

            Options.ReadOptionsFile();
            ModOptions.StoreOptionsValues(Options.optionsValuesCollection);
            AttilaPreferences.ChangeUnitSizes();
            AttilaPreferences.ValidateOnStartup();

            UpdateUIForBattleState();

            // Set ToolTips
            InformationToolTip.SetToolTip(ExecuteButton, "Start or continue a Crusader Wars campaign by launching Crusader Kings 3.");
            InformationToolTip.SetToolTip(ContinueBattleButton, "Restart the current Total War: Attila battle without reloading Crusader Kings 3. Use this if the battle crashed or you want to try again.");
            InformationToolTip.SetToolTip(SettingsBtn, "Configure game paths, battle options, and unit mappers.");
            InformationToolTip.SetToolTip(patreonBtn, "Support the development of Crusader Wars on Patreon.");
            InformationToolTip.SetToolTip(WebsiteBTN, "Visit the official Crusader Wars website for news and updates.");
            InformationToolTip.SetToolTip(SteamBTN, "View the Crusader Wars mod on the Steam Workshop.");
            InformationToolTip.SetToolTip(viewLogsLink, "Click to find the debug.log file. Please share this file on our Discord for troubleshooting help.");
            InformationToolTip.SetToolTip(discordLink, "Join our Discord community for help and updates.");

            InformationToolTip.SetToolTip(labelVersion, "Crusader Wars application version.");
            InformationToolTip.SetToolTip(labelMappersVersion, "Version of the installed Unit Mappers.");

            infoLabel.MaximumSize = new Size(MainPanelLayout.Width - 10, 0);

            Program.Logger.Debug("Form1_Load complete.");
        }

        private void UpdateUIForBattleState()
        {
            bool battleInProgress = BattleState.IsBattleInProgress();

            ContinueBattleButton.Visible = battleInProgress;

            if (battleInProgress)
            {
                ExecuteButton.Text = "Start CK3";
                ContinueBattleButton.Text = "Continue Battle";
                infoLabel.Text = "A battle is in progress!";

                // Resize buttons to fit side-by-side
                ExecuteButton.Size = new Size(197, 115);
                ContinueBattleButton.Size = new Size(197, 115);
            }
            else
            {
                ExecuteButton.Text = ""; // Use image text
                infoLabel.Text = "Ready to Start!";

                // Restore original button size
                ExecuteButton.Size = new Size(197, 115);
            }
        }

        //---------------------------------//
        //----------DEBUG BUTTON-----------//
        //---------------------------------//



        private void btt_debug_Click(object sender, EventArgs e)
        {
            Culture culture = null;
            culture.GetCultureName();
        }

        // This method kept empty since shortcut creation is now obsolete
        private void CreateAttilaShortcut()
        {
            if(!System.IO.File.Exists(@".\CW.lnk")) {
                Program.Logger.Debug("Attila shortcut not found, creating...");
                object shDesktop = (object)"Desktop";
                WshShell shell = new WshShell();
                string shortcutAddress = @".\CW.lnk";
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "Shortcut with all user enabled mods and required unit mappers mods for Total War: Attila";
                shortcut.WorkingDirectory = Properties.Settings.Default.VAR_attila_path.Replace(@"\Attila.exe", "");
                shortcut.Arguments = "used_mods_cw.txt";
                shortcut.TargetPath = Properties.Settings.Default.VAR_attila_path;
                shortcut.Save();
                Program.Logger.Debug("Attila shortcut created successfully.");
            }
        }


        private void HomePage_Shown(object sender, EventArgs e)
        {
            // Empty event handler to satisfy designer
        }

        List<Army> attacker_armies;
        List<Army> defender_armies;
        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Execute button clicked.");

            if (BattleState.IsBattleInProgress())
            {
                var confirmResult = MessageBox.Show("Starting a new battle will discard your progress from the current one in TW:Attila. Are you sure you want to continue?",
                                                     "Confirm Start CK3",
                                                     MessageBoxButtons.YesNo,
                                                     MessageBoxIcon.Warning);
                if (confirmResult == DialogResult.No)
                {
                    return;
                }
            }

            if (!_programmaticClick)
            {
                sounds = new SoundPlayer(@".\data\sounds\sword-slash-with-metal-shield-impact-185433.wav");
                sounds.Play();
            }
            _programmaticClick = false; // Always reset after check

            _myVariable = 1;
            ExecuteButton.Enabled = false;
            ExecuteButton.BackgroundImage = Properties.Resources.start_new_disabled;
            ProcessCommands.ResumeProcess();

            BattleState.ClearBattleState();
            UpdateUIForBattleState();

            /*
             *  ERASES OLD FILES
             */
            Program.Logger.Debug("Erasing old files.");
            string gamestateFile = @".\data\save_file_data\gamestate_file\gamestate";
            string editedGamestateFile = @".\data\save_file_data\gamestate";
            string savefileZip = @".\data\save_file_data\last_save.zip";
            if (System.IO.File.Exists(gamestateFile) )
                System.IO.File.Delete(gamestateFile);
            if (System.IO.File.Exists(editedGamestateFile))
                System.IO.File.Delete(editedGamestateFile);
            if (System.IO.File.Exists(savefileZip))
                System.IO.File.Delete(savefileZip);

            UnitsCardsNames.RemoveFiles();

            while (true)
            {
                Program.Logger.Debug("Starting main loop, waiting for CK3 battle.");
                this.Text = "Crusader Wars (Waiting for CK3 battle...)";

                try
                {
                    CreateAttilaShortcut();
                    Program.Logger.Debug("Attila shortcut created/verified.");
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error creating Attila shortcut: {ex.Message}");
                    MessageBox.Show("Error creating Attila shortcut!", "Crusader Wars: File Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Ready to start!";
                    ExecuteButton.Enabled = true;
                    this.Text = "Crusader Wars";
                    break;
                }



                try
                {
                    DataSearch.ClearLogFile();
                    DeclarationsFile.Erase();
                    BattleScript.EraseScript();
                    BattleResult.ClearAttilaLog();
                    Program.Logger.Debug("Log files cleared.");
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error clearing log files: {ex.Message}");
                    MessageBox.Show("No Log File Found!", "Crusader Wars: Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Ready to start!";
                    ExecuteButton.Enabled = true;
                    this.Text = "Crusader Wars";
                    break;
                }

                try
                {
                    //Open Crusader Kings 3
                    Games.StartCrusaderKingsProcess();
                    Program.Logger.Debug("CK3 process started/verified.");
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error starting CK3: {ex.Message}");
                    MessageBox.Show("Couldn't find 'ck3.exe'. Change the Crusader Kings 3 path. ", "Crusader Wars: Path Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Ready to start!";
                    ExecuteButton.Enabled = true;
                    this.Text = "Crusader Wars";
                    break;
                }

                BattleFile.ClearFile();

                bool battleHasStarted = false;

                //Read log file and get all data from CK3
                using (FileStream logFile = System.IO.File.Open(debugLog_Path, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
                {

                    using (StreamReader reader = new StreamReader(logFile))
                    {
                        logFile.Position = 0;
                        reader.DiscardBufferedData();

                        if (battleJustCompleted && !ModOptions.CloseCK3DuringBattle())
                        {
                            infoLabel.Text = "Waiting for CK3 battle... Load save game 'battle_results.ck3' to see results.";
                        }
                        else
                        {
                            infoLabel.Text = "Waiting for CK3 battle...";
                        }
                        Program.Logger.Debug("Waiting for CRUSADERWARS3 keyword in CK3 log...");
                        try
                        {
                            //Wait for CW keyword
                            while (!battleHasStarted)
                            {
                                //Read each line
                                while (!reader.EndOfStream)
                                {
                                    string line = reader.ReadLine();

                                    //If Battle Started
                                    if (line.Contains(SEARCH_KEY))
                                    {
                                        Program.Logger.Debug("Battle keyword found in CK3 log.");
                                        battleJustCompleted = false;
                                        battleHasStarted = true;
                                        break;
                                    }

                                }

                                // Check if CK3 process is still running
                                Process[] ck3Processes = Process.GetProcessesByName("ck3");
                                if (ck3Processes.Length == 0)
                                {
                                    Program.Logger.Debug("CK3 process not found while waiting for battle. Resetting UI.");
                                    _myVariable = 0;
                                    ExecuteButton.Enabled = true;
                                    if (ExecuteButton.Enabled)
                                    {
                                        ExecuteButton.BackgroundImage = Properties.Resources.start_new;
                                    }
                                    UpdateUIForBattleState();
                                    this.Text = "Crusader Wars";
                                    return;
                                }

                                logFile.Position = 0;
                                reader.DiscardBufferedData();
                                await Task.Delay(500); // Increased delay for efficiency
                            }
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.Debug($"Error searching for battle in log: {ex.Message}");
                            MessageBox.Show("Error searching for battle. ", "Crusader Wars: Critical Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            infoLabel.Text = "Ready to start!";
                            ExecuteButton.Enabled = true;
                            this.Text = "Crusader Wars";
                            if (loadingScreen != null) CloseLoadingScreen();
                            break;
                        }


                        try
                        {
                            UpdateLoadingScreenMessage("Getting data from CK3 save file...");
                            StartLoadingScreen();

                            infoLabel.Text = "Reading CK3 battle data...";
                            this.Text = "Crusader Wars (Reading CK3 battle data...)";
                            this.Hide();

                            logFile.Position = 0;
                            reader.DiscardBufferedData();
                            log = reader.ReadToEnd();
                            log = RemoveASCII(log);
                            BattleState.SaveLogSnippet(log);

                            if (battleHasStarted)
                            {
                                Program.Logger.Debug("Reading CK3 battle data from log.");
                                Program.Logger.Debug("Searching log data...");
                                DataSearch.Search(log);
                                Program.Logger.Debug("Reading installed Attila mods...");
                                AttilaModManager.ReadInstalledMods();
                                Program.Logger.Debug("Setting playthrough...");
                                SetPlaythrough();
                                UpdateLoadingScreenUnitMapperMessage(UnitMappers_BETA.GetLoadedUnitMapperString());
                                Program.Logger.Debug("Creating user mods file for Attila...");
                                AttilaModManager.CreateUserModsFile();
                            }
                        }
                        catch(Exception ex)
                        {
                            Program.Logger.Debug($"Error reading battle data from log: {ex.Message}");
                            this.Show();
                            if (loadingScreen != null) CloseLoadingScreen();
                            MessageBox.Show($"Error reading TW:Attila battle data: {ex.Message}", "Crusader Wars: Data Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            infoLabel.Text = "Waiting for CK3 battle...";
                            this.Text = "Crusader Wars (Waiting for CK3 battle...)";

                            //Data Clear
                            Data.Reset();

                            continue;
                        }

                        logFile.Position = 0;
                        reader.DiscardBufferedData();

                        reader.Close();
                        logFile.Close();

                    }

                }



                try
                {
                    UpdateLoadingScreenMessage("Getting data from CK3 save file...");
                    await Task.Delay(2000); //Old was 3000ms
                    if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.CloseCrusaderKingsProcess();
                    }
                    else
                    {
                        ProcessCommands.SuspendProcess();
                    }

                    //path_editedSave = Properties.Settings.Default.VAR_dir_save + @"\CrusaderWars_Battle.ck3";
                    path_editedSave = @".\data\save_file_data\gamestate_file\gamestate";
                    Program.Logger.Debug("Uncompressing and reading CK3 save file.");
                    SaveFile.Uncompress();
                    Reader.ReadFile(path_editedSave);
                    BattleResult.GetPlayerCombatResult();
                    BattleResult.ReadPlayerCombat(CK3LogData.LeftSide.GetCommander().id);
                }
                catch(Exception ex)
                {
                    Program.Logger.Debug($"Error reading save file: {ex.Message}");
                    this.Show();
                    if (loadingScreen != null) CloseLoadingScreen();
                    MessageBox.Show($"Error reading the save file: {ex.Message}", "Crusader Wars: Save File Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Waiting for CK3 battle...";
                    if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.StartCrusaderKingsProcess();
                    }
                    else
                    {
                        ProcessCommands.ResumeProcess();
                    }

                    //Data Clear
                    Data.Reset();

                    continue;

                }

                try
                {

                    //1.0 Beta Debug
                    UpdateLoadingScreenMessage("Reading CK3 save file data...");
                    Program.Logger.Debug("Reading battle armies from CK3 save file.");
                    var armies = ArmiesReader.ReadBattleArmies();
                    attacker_armies = armies.attacker;
                    defender_armies = armies.defender;
                    Program.Logger.Debug($"Found {attacker_armies.Count} attacker armies and {defender_armies.Count} defender armies.");
            
                    // Mark battle as started only if setup succeeded
                    BattleState.MarkBattleStarted();
                    UpdateUIForBattleState();
                }
                catch(Exception ex)
                {
                    string errorDetails = $"Error reading battle armies: {ex.Message}";
                    string stackTrace = ex.StackTrace;
                    Program.Logger.Debug(errorDetails);
                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        // Extract relevant part of stack trace
                        int index = stackTrace.IndexOf(" at Program");
                        string relevantStack = index >= 0 ? stackTrace.Substring(0, index) : stackTrace;
                        Program.Logger.Debug($"Stack Trace: {relevantStack}");
                    }

                    this.Show();
                    if (loadingScreen != null) CloseLoadingScreen();
                    string errorMessage = "Error reading the battle armies.\n\n" +
                            "Possible causes:\n" +
                            "❌ Playing in Ironman mode\n" +
                            "❌ Using Steam Cloud saves\n" +
                            "❌ Playing in debug mode\n" +
                            "❌ Save file using old format\n" +
                            "❌ Unsupported game mods\n\n" +
                            "Troubleshooting:\n" +
                            "1. Disable Ironman mode\n" +
                            "2. Use local saves instead of Steam Cloud\n" +
                            "3. Start a new non-debug game\n" +
                            "4. Verify game files in Steam\n" +
                            "5. Try a different save file";

                    MessageBox.Show($"{errorMessage}\n\nTechnical Details: {ex.Message}", "Crusader Wars: Army Data Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.StartCrusaderKingsProcess();
                    }
                    else
                    {
                        ProcessCommands.ResumeProcess();
                    }
                    infoLabel.Text = "Waiting for CK3 battle...";
                    this.Text = "Crusader Wars (Waiting for CK3 battle...)";

                    //Data Clear
                    Data.Reset();

                    continue;
                }

                if (!await ProcessBattle())
                {
                    break;
                }

                // Battle processed successfully. Loop will continue.
                // Manually reset some UI elements for the next iteration,
                // without touching infoLabel.
                ContinueBattleButton.Visible = false;
                ExecuteButton.Text = "";
                ExecuteButton.Size = new Size(197, 115);
            }

            // Reset UI if the main loop is broken by a critical error
            _myVariable = 0;
            ExecuteButton.Enabled = true;
            if (ExecuteButton.Enabled)
            {
                ExecuteButton.BackgroundImage = Properties.Resources.start_new;
            }
            UpdateUIForBattleState();
            this.Text = "Crusader Wars";
        }

        private async Task<bool> ProcessBattle(bool regenerateAndRestart = true)
        {
            var left_side = ArmiesReader.GetSideArmies("left", attacker_armies, defender_armies);
            var right_side = ArmiesReader.GetSideArmies("right", attacker_armies, defender_armies);
            int left_side_total = left_side.Sum(army => army.GetTotalSoldiers());
            int right_side_total = right_side.Sum(army => army.GetTotalSoldiers());
            string left_side_combat_side = left_side[0].CombatSide;
            string right_side_combat_side = right_side[0].CombatSide;
            Program.Logger.Debug($"Left side ({left_side_combat_side}) total soldiers: {left_side_total}");
            Program.Logger.Debug($"Right side ({right_side_combat_side}) total soldiers: {right_side_total}");


            if (regenerateAndRestart)
            {
                try
                {
                    Program.Logger.Debug("Creating TW:Attila battle files.");
                    BattleDetails.ChangeBattleDetails(left_side_total, right_side_total, left_side_combat_side, right_side_combat_side);

                    await Games.CloseTotalWarAttilaProcess();
                    UpdateLoadingScreenMessage("Creating battle in Total War: Attila...");

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

                    //Create Battle
                    Program.Logger.Debug("Creating battle file...");
                    BattleFile.BETA_CreateBattle(attacker_armies, defender_armies);

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

                    //Creates .pack mod file
                    Program.Logger.Debug("Creating .pack file...");
                    PackFile.PackFileCreator();
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error creating Attila battle: {ex.Message}");
                    this.Show();
                    if (loadingScreen != null) CloseLoadingScreen();
                    MessageBox.Show($"Error creating the battle:{ex.Message}", "Crusader Wars: Data Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.StartCrusaderKingsProcess();
                    }
                    else
                    {
                        ProcessCommands.ResumeProcess();
                    }
                    infoLabel.Text = "Waiting for CK3 battle...";
                    this.Text = "Crusader Wars (Waiting for CK3 battle...)";

                    //Data Clear
                    Data.Reset();

                    return true; // Continue
                }

                try
                {
                    // Check for user.script.txt conflict before launching Attila
                    if (!AttilaPreferences.ValidateBeforeLaunch())
                    {
                        Program.Logger.Debug("Aborting Attila launch due to user script conflict.");
                        this.Show();
                        if (loadingScreen != null) CloseLoadingScreen();
                        ProcessCommands.ResumeProcess();
                        infoLabel.Text = "Waiting for CK3 battle...";
                        this.Text = "Crusader Wars (Waiting for CK3 battle...)";
                        Data.Reset();
                        return true; // Continue
                    }

                    //Open Total War Attila
                    Program.Logger.Debug("Starting Total War: Attila process...");
                    Games.StartTotalWArAttilaProcess();
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error starting Attila: {ex.Message}");
                    this.Show();
                    if (loadingScreen != null) CloseLoadingScreen();
                    MessageBox.Show("Couldn't find 'Attila.exe'. Change the Total War Attila path. ", "Crusader Wars: Path Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Ready to start!";
                    if (ModOptions.CloseCK3DuringBattle())
                    {
                        Games.StartCrusaderKingsProcess();
                    }
                    else
                    {
                        ProcessCommands.ResumeProcess();
                    }
                    ExecuteButton.Enabled = true;
                    this.Text = "Crusader Wars";
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
                DeclarationsFile.EraseScript();
                BattleScript.EraseScript();
                BattleResult.ClearAttilaLog();

                if (loadingScreen != null) CloseLoadingScreen();
                this.Show();

            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error during cleanup before battle: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Crusader Wars: Application Error",
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
                infoLabel.Text = "Waiting for CK3 battle...";
                this.Text = "Crusader Wars (Waiting for CK3 battle...)";

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

            infoLabel.Text = "Waiting for TW:Attila battle to end...";
            this.Text = "Crusader Wars (Waiting for TW:Attila battle to end...)";
            Program.Logger.Debug("Waiting for TW:Attila battle to end...");

            ExecuteButton.Enabled = true;
            if (ExecuteButton.Enabled)
            {
                ExecuteButton.BackgroundImage = Properties.Resources.start_new;
            }
            ContinueBattleButton.Enabled = true;

            //  Waiting for TW:Attila battle to end...
            while (battleEnded == false)
            {
                // Check if Attila process is still running
                if (Process.GetProcessesByName("Attila").Length == 0)
                {
                    Program.Logger.Debug("Attila process not found while waiting for battle to end. Assuming crash or user exit.");
                    return false; // Indicate abnormal termination. The caller will handle the UI reset.
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

                    infoLabel.Text = "Processing battle results...";
                    this.Text = "Crusader Wars (Processing results)";

                    string path_log_attila = Properties.Settings.Default.VAR_log_attila;


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

                    //  EDIT LIVING FILE
                    Program.Logger.Debug("Editing Living.txt file...");
                    BattleResult.EditLivingFile(attacker_armies, defender_armies);

                    //  EDIT COMBATS FILE
                    Program.Logger.Debug("Editing Combats.txt file...");
                    BattleResult.EditCombatFile(attacker_armies, defender_armies, left_side[0].CombatSide, right_side[0].CombatSide, path_log_attila);

                    //  EDIT COMBATS RESULTS FILE
                    Program.Logger.Debug("Editing BattleResults.txt file...");
                    BattleResult.EditCombatResultsFile(attacker_armies, defender_armies);

                    //  EDIT REGIMENTS FILE
                    Program.Logger.Debug("Editing Regiments.txt file...");
                    BattleResult.EditRegimentsFile(attacker_armies, defender_armies);

                    //  EDIT ARMY REGIMENTS FILE
                    Program.Logger.Debug("Editing ArmyRegiments.txt file...");
                    BattleResult.EditArmyRegimentsFile(attacker_armies, defender_armies);


                    //  WRITE TO CK3 SAVE FILE
                    Program.Logger.Debug("Writing results to gamestate file...");
                    BattleResult.SendToSaveFile(path_editedSave);

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

                    infoLabel.Text = "Battle complete! In CK3, use 'Continue' or load 'battle_results.ck3' save.";
                    this.Text = "Crusader Wars (Battle Complete)";
                    battleJustCompleted = true;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error retrieving TW:Attila battle results: {ex.Message}");
                MessageBox.Show($"Error retrieving TW:Attila battle results: {ex.Message}", "Crusader Wars: TW:Attila Battle Results Error",
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
                infoLabel.Text = "Waiting for CK3 battle...";
                this.Text = "Crusader Wars (Waiting for CK3 battle...)";

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

        private async void ContinueBattleButton_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Continue Battle button clicked.");
            sounds = new SoundPlayer(@".\data\sounds\sword-slash-with-metal-shield-impact-185433.wav");
            sounds.Play();
            _myVariable = 1;
            ExecuteButton.Enabled = false;
            ContinueBattleButton.Enabled = false;
            ExecuteButton.BackgroundImage = Properties.Resources.start_new_disabled;

            // Restore battle context from saved log snippet
            Program.Logger.Debug("Restoring battle context from log snippet...");
            string logSnippet = BattleState.LoadLogSnippet();
            if (string.IsNullOrEmpty(logSnippet))
            {
                Program.Logger.Debug("Failed to load battle context. Log snippet is missing or empty.");
                MessageBox.Show("Could not continue the battle. The battle context file is missing.", "Crusader Wars: Continue Battle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _myVariable = 0;
                ExecuteButton.Enabled = true;
                ContinueBattleButton.Enabled = true;
                ExecuteButton.BackgroundImage = Properties.Resources.start_new;
                return;
            }
            DataSearch.Search(logSnippet);
            Program.Logger.Debug("Battle context restored.");

            path_editedSave = @".\data\save_file_data\gamestate_file\gamestate";

            try
            {
                Program.Logger.Debug("Re-loading army data for continued battle.");

                // Re-initialize mod and unit mapper context
                AttilaModManager.ReadInstalledMods();
                SetPlaythrough();
                AttilaModManager.CreateUserModsFile();

                BattleResult.GetPlayerCombatResult();
                BattleResult.ReadPlayerCombat(CK3LogData.LeftSide.GetCommander().id);
                var armies = ArmiesReader.ReadBattleArmies();
                attacker_armies = armies.attacker;
                defender_armies = armies.defender;
                Program.Logger.Debug($"Successfully re-loaded {attacker_armies.Count} attacker and {defender_armies.Count} defender armies.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to re-load army data: {ex.Message}");
                MessageBox.Show($"Could not continue the battle. Failed to load army data.\n\nError: {ex.Message}", "Crusader Wars: Continue Battle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Reset UI state
                _myVariable = 0;
                ExecuteButton.Enabled = true;
                ContinueBattleButton.Enabled = true;
                ExecuteButton.BackgroundImage = Properties.Resources.start_new;
                return;
            }

            bool regenerateAndRestart = true; // Default behavior

            Process[] attilaProcesses = Process.GetProcessesByName("Attila");
            if (attilaProcesses.Length > 0)
            {
                string message = "Total War: Attila is already running.\n\n" +
                                 "Do you want to restart it to ensure the latest battle data is loaded?\n\n" +
                                 "• Yes: Restart Attila. (Recommended to retry the battle)\n" +
                                 "• No: Continue with the current session. (If the CW launcher closed unexpectedly)\n" +
                                 "• Cancel: Do nothing.";
                string title = "Attila is Running";
                DialogResult result = MessageBox.Show(message, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    regenerateAndRestart = true;
                }
                else if (result == DialogResult.No)
                {
                    regenerateAndRestart = false;
                }
                else // Cancel
                {
                    // Re-enable buttons and return
                    _myVariable = 0;
                    ExecuteButton.Enabled = true;
                    ContinueBattleButton.Enabled = true;
                    if (ExecuteButton.Enabled)
                    {
                        ExecuteButton.BackgroundImage = Properties.Resources.start_new;
                    }
                    return;
                }
            }

            _programmaticClick = true;
            if (await ProcessBattle(regenerateAndRestart))
            {
                // The battle finished successfully, start the main loop to wait for the next one.
                ExecuteButton.PerformClick();
            }
            else
            {
                // A critical error occurred (e.g., couldn't start Attila). Reset UI.
                UpdateUIForBattleState();
                ExecuteButton.Enabled = true;
                ContinueBattleButton.Enabled = true;
                if (ExecuteButton.Enabled)
                {
                    ExecuteButton.BackgroundImage = Properties.Resources.start_new;
                }
                _myVariable = 0;
            }
        }

        private void ContinueBattleButton_MouseEnter(object sender, EventArgs e)
        {
            ContinueBattleButton.BackgroundImage = Properties.Resources.start_new_hover;
        }

        private void ContinueBattleButton_MouseLeave(object sender, EventArgs e)
        {
            ContinueBattleButton.BackgroundImage = Properties.Resources.start_new;
        }

        /*---------------------------------------------
         * :::::::::::::PROCESS COMMANDS:::::::::::::::
         ---------------------------------------------*/

        struct ProcessCommands
        {
            private static string ProcessRuntime(string command)
            {
                //Get User Path
                string filePath = Directory.GetFiles(@".\data\runtime", "pssuspend64.exe", SearchOption.AllDirectories)[0];
                ProcessStartInfo procStartInfo = new ProcessStartInfo(filePath, command)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true

                };

                using (Process proc = new Process())
                {
                    proc.StartInfo = procStartInfo;
                    proc.Start();
                    return proc.StandardOutput.ReadToEnd();
                }

            }
            public static void SuspendProcess()
            {
                Program.Logger.Debug("Suspending ck3.exe process.");
                ProcessRuntime("ck3.exe");

            }

            public static void ResumeProcess()
            {
                Program.Logger.Debug("Resuming ck3.exe process.");
                ProcessRuntime("/r ck3.exe");
            }


        }

        /*---------------------------------------------
         * :::::::::::::: PLAYTHROUGHS ::::::::::::::::
         ---------------------------------------------*/
        void SetPlaythrough()
        {
            Program.Logger.Debug("Setting playthrough...");
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@".\settings\UnitMappers.xml");
            string tagName = "";
            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                if (node is XmlComment) continue;
                if (node.InnerText == "True")
                {
                    tagName = node.Attributes["name"].Value;
                    Program.Logger.Debug($"Playthrough tag found: {tagName}");
                    break;
                }
            }
            AttilaModManager.SetLoadingRequiredMods(UnitMappers_BETA.GetUnitMapperModFromTagAndTimePeriod(tagName));
        }



        /*---------------------------------------------
         * :::::::::::GAMES INITIALIZATION:::::::::::::
         ---------------------------------------------*/
        struct Games
        {
            public static void StartCrusaderKingsProcess()
            {
                Program.Logger.Debug("Checking for CK3 process...");
                Process[] process_ck3 = Process.GetProcessesByName("ck3");
                if (process_ck3.Length == 0)
                {
                    Program.Logger.Debug("CK3 process not found. Starting CK3.");
                    Process.Start(Properties.Settings.Default.VAR_ck3_path);
                }
                else
                {
                    Program.Logger.Debug("CK3 process already running.");
                }

            }

            public static void CloseCrusaderKingsProcess()
            {
                Program.Logger.Debug("Closing CK3 process...");
                Process[] process_ck3 = Process.GetProcessesByName("ck3");
                foreach (Process worker in process_ck3)
                {
                    worker.Kill();
                    worker.WaitForExit();
                    worker.Dispose();
                }

                Program.Logger.Debug("CK3 process closed.");
            }

            public static void LoadBattleResults()
            {
                Program.Logger.Debug("Loading CK3 with battle results...");
                string ck3_path = Properties.Settings.Default.VAR_ck3_path;
                Process.Start(ck3_path, "--continuelastsave");
            }

            public static void StartTotalWArAttilaProcess()
            {
                Program.Logger.Debug("Starting Total War: Attila process via shortcut...");
                Process.Start(@".\CW.lnk");
            }

            public async static Task CloseTotalWarAttilaProcess()
            {
                Program.Logger.Debug("Closing Total War: Attila process...");
                Process[] process_attila = Process.GetProcessesByName("Attila");
                foreach (Process worker in process_attila)
                {
                    worker.Kill();
                    worker.WaitForExit();
                    worker.Dispose();
                }

                await Task.Delay(1000);
                Program.Logger.Debug("Total War: Attila process closed.");
            }
        };

        /*---------------------------------------------
         * :::::::::::LOADING SCREEN FUNCS:::::::::::::
         ---------------------------------------------*/

        void ChangeLoadingScreenImage()
        {
            Program.Logger.Debug("Changing loading screen image based on playthrough.");
            string file = @".\Settings\UnitMappers.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            var ck3ToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='DefaultCK3']").InnerText;
            var tfeToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='TheFallenEagle']").InnerText;
            var lotrToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='RealmsInExile']").InnerText;

            string playthrough = "";
            if (ck3ToggleStateStr == "True") playthrough = "Medieval";
            if (tfeToggleStateStr == "True") playthrough = "LateAntiquity";
            if (lotrToggleStateStr == "True") playthrough = "Lotr";

            Program.Logger.Debug($"Playthrough detected: {playthrough}. Setting background image.");
            switch (playthrough)
            {
                case "Medieval":
                    loadingScreen.BackgroundImage = Properties.Resources.LS_medieval;
                    break;
                case "LateAntiquity":
                    loadingScreen.BackgroundImage = Properties.Resources.LS_late_antiquity;
                    break;
                case "Lotr":
                    loadingScreen.BackgroundImage = Properties.Resources.LS_lotr;
                    break;
                default:
                    loadingScreen.BackgroundImage = Properties.Resources.LS_medieval;
                    break;
            }
        }
        public void StartLoadingScreen()
        {
            Program.Logger.Debug("Starting loading screen thread.");
            loadingThread = new Thread(new ThreadStart(() =>
            {
                loadingScreen = new LoadingScreen();
                ChangeLoadingScreenImage();
                Application.Run(loadingScreen);
            }));

            loadingThread.IsBackground = true;
            loadingThread.SetApartmentState(ApartmentState.STA);
            loadingThread.Start();

            // Ensure the loading screen is created before continuing
            while (loadingScreen == null || !loadingScreen.IsHandleCreated)
            {
                Thread.Sleep(50);
            }
        }

        public void UpdateLoadingScreenMessage(string message)
        {
            if (loadingScreen != null && loadingScreen.IsHandleCreated)
            {
                loadingScreen.BeginInvoke(new Action(() => loadingScreen.ChangeMessage(message)));
            }
        }

        public void UpdateLoadingScreenUnitMapperMessage(string message)
        {
            if (loadingScreen != null && loadingScreen.IsHandleCreated)
            {
                loadingScreen.BeginInvoke(new Action(() => loadingScreen.ChangeUnitMapperMessage(message)));
            }
        }

        public void CloseLoadingScreen()
        {
            Program.Logger.Debug("Closing loading screen.");
            if (loadingScreen == null) return;

            if (loadingScreen.InvokeRequired)
            {
                loadingScreen.Invoke(new Action(() => loadingScreen.Close()));
            }
            else
            {
                loadingScreen.Close();
            }

            // Ensure the thread is properly cleaned up
            if (loadingThread != null)
            {
                loadingThread.Join();
                loadingThread = null;
            }
            loadingScreen = null;
        }



        /*---------------------------------------------
         * :::::::::::LOW-LEVEL FUNCTIONS  :::::::::::::
         ---------------------------------------------*/
        public static string RemoveASCII(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            string apostrophe = "'";
            foreach (char c in inputString)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_' || c == '\n' || c == '-' || c == ':' || c == ' '|| char.IsLetter(c) || c == '?' || c == apostrophe[0] || c== '%')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void LoadDLLs()
        {
            Program.Logger.Debug("Loading DLLs from data\\dlls folder.");
            try
            {
                string dll_folder = @".\data\dlls";
                foreach (string dllFile in Directory.GetFiles(dll_folder, "*.dll"))
                {
                    Program.Logger.Debug($"Loading DLL: {dllFile}");
                    Assembly assembly = Assembly.LoadFrom(dllFile);
                    AppDomain.CurrentDomain.Load(assembly.GetName());
                }
                Program.Logger.Debug("Finished loading DLLs.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error loading DLLs: {ex.Message}");
                return;
            }

        }

        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Settings button clicked.");
            SettingsBtn.BackgroundImage = Properties.Resources.options_btn_new_click;
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            
            Options optionsChild = new Options();
            optionsChild.ShowDialog();
        }


        private void HomePage_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.Logger.Debug("HomePage form closing.");
            ProcessCommands.ResumeProcess();
        }

        SoundPlayer sounds;

        private void viewLogsLink_Click(object sender, EventArgs e)
        {
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            
            string logPath = Path.GetFullPath(@".\data\debug.log");
            if (System.IO.File.Exists(logPath))
            {
                // Open explorer and highlight debug.log
                Process.Start("explorer.exe", $"/select, \"{logPath}\"");
            }
            else
            {
                // Fallback if file not found
                string folderPath = Path.GetFullPath(@".\data");
                if (Directory.Exists(folderPath))
                {
                    Process.Start("explorer.exe", folderPath);
                }
                else
                {
                    MessageBox.Show("Log folder not found! Please report this to developers.",
                                    "Crusader Wars: Log Location Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

        private void discordLink_Click(object sender, EventArgs e)
        {
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            Process.Start("https://discord.gg/WEGXdrqrpa");
        }

        private void patreonBtn_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Patreon button clicked.");
            patreonBtn.BackgroundImage = Properties.Resources.patreon_btn_clickpng;
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            Process.Start("https://www.patreon.com/user?u=83859552");

        }

        private void WebsiteBTN_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Website button clicked.");
            WebsiteBTN.BackgroundImage = Properties.Resources.website_btn_new_click;
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            Process.Start("https://www.crusaderwars.com");

        }

        private void SteamBTN_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Steam button clicked.");
            SteamBTN.BackgroundImage = Properties.Resources.steam_btn_new_click;
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            Process.Start("https://steamcommunity.com/sharedfiles/filedetails/?id=2977969008");
        }

        private void patreonBtn_MouseEnter(object sender, EventArgs e)
        {
            patreonBtn.BackgroundImage = Properties.Resources.patreon_btn_hover;
        }

        private void patreonBtn_MouseLeave(object sender, EventArgs e)
        {
            patreonBtn.BackgroundImage = Properties.Resources.patreon_btn_new;
        }

        private void patreonBtn_MouseHover_1(object sender, EventArgs e)
        {
            patreonBtn.BackgroundImage = Properties.Resources.patreon_btn_hover;
        }

        private void ExecuteButton_MouseEnter(object sender, EventArgs e)
        {
            if (ExecuteButton.Enabled)
                ExecuteButton.BackgroundImage = Properties.Resources.start_new_hover;
        }

        private void ExecuteButton_MouseHover(object sender, EventArgs e)
        {
            if(ExecuteButton.Enabled)
            ExecuteButton.BackgroundImage = Properties.Resources.start_new_hover;
        }

        private void ExecuteButton_MouseLeave(object sender, EventArgs e)
        {
            if (ExecuteButton.Enabled)
                ExecuteButton.BackgroundImage = Properties.Resources.start_new;
        }

        private void SettingsBtn_MouseHover(object sender, EventArgs e)
        {
            SettingsBtn.BackgroundImage = Properties.Resources.options_btn_new_hover;
        }

        private void SettingsBtn_MouseLeave(object sender, EventArgs e)
        {
            SettingsBtn.BackgroundImage = Properties.Resources.options_btn_new;
        }

        private void SettingsBtn_MouseEnter(object sender, EventArgs e)
        {
            SettingsBtn.BackgroundImage = Properties.Resources.options_btn_new_hover;
        }

        private void WebsiteBTN_MouseEnter(object sender, EventArgs e)
        {
            WebsiteBTN.BackgroundImage = Properties.Resources.website_btn_new_hover1;
        }

        private void WebsiteBTN_MouseHover(object sender, EventArgs e)
        {
            WebsiteBTN.BackgroundImage = Properties.Resources.website_btn_new_hover1;
        }

        private void WebsiteBTN_MouseLeave(object sender, EventArgs e)
        {
            WebsiteBTN.BackgroundImage = Properties.Resources.website_btn_new;
        }

        private void SteamBTN_MouseEnter(object sender, EventArgs e)
        {
            SteamBTN.BackgroundImage = Properties.Resources.steam_btn_new_hover1;
        }

        private void SteamBTN_MouseHover(object sender, EventArgs e)
        {
            SteamBTN.BackgroundImage = Properties.Resources.steam_btn_new_hover1;
        }

        private void SteamBTN_MouseLeave(object sender, EventArgs e)
        {
            WebsiteBTN.BackgroundImage = Properties.Resources.steam_btn_new;
        }

        private async void labelVersion_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_appVersion))
            {
                string url = await _updater.GetReleaseUrlForVersion(_appVersion, false);
                if(!string.IsNullOrEmpty(url))
                {
                    Process.Start(url);
                }
            }
        }

        private async void labelMappersVersion_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_umVersion))
            {
                string url = await _updater.GetReleaseUrlForVersion(_umVersion, true);
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(url);
                }
            }
        }
    }
}
