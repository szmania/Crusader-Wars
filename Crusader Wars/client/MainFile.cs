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
using Crusader_Wars.client;
using Crusader_Wars.locs;
using Crusader_Wars.data.attila_settings;
using Crusader_Wars.data.save_file;
using Crusader_Wars.unit_mapper;
using Crusader_Wars.terrain;
using System.Threading;
using Crusader_Wars.mod_manager;
using System.Xml;
using IWshRuntimeLibrary;
using System.Web;
using System.Windows.Media;
using System.Drawing.Text;


namespace Crusader_Wars
{
    public partial class HomePage : Form
    {


        const string SEARCH_KEY = "CRUSADERWARS3";

        private int _myVariable = 0;
        public HomePage()
        {
            LoadFont();
            InitializeComponent();
            
            Thread.Sleep(1000);

            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            debugLog_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\console_history.txt";
            saveGames_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\save games";

            //Icon
            this.Icon = Properties.Resources.logo;

            Properties.Settings.Default.VAR_log_attila = string.Empty;
            Properties.Settings.Default.VAR_dir_save = string.Empty;
            Properties.Settings.Default.VAR_log_ck3 = string.Empty;


            Properties.Settings.Default.VAR_dir_save = saveGames_Path;
            Properties.Settings.Default.VAR_log_ck3 = debugLog_Path;
            Properties.Settings.Default.Save();

            Updater Updater = new Updater();
            Updater.CheckAppVersion();
            Updater.CheckUnitMappersVersion();
            labelVersion.Text = $"V{Updater.AppVersion}";

            var _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 500; // check variable every second
            _timer.Tick += Timer_Tick;
            _timer.Start();
            Original_Color = infoLabel.ForeColor;
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
                MessageBox.Show("Font file not found.");
            }
        }



        System.Drawing.Color Original_Color;

        bool VerifyGamePaths()
        {
	    string ck3Executable = Properties.Settings.Default.VAR_ck3_path.ToLower().Split(' ').Last();
	    string attilaExecutable = Properties.Settings.Default.VAR_attila_path.ToLower().Split(' ').Last();

	    // Strict matching to make sure the executable is given.
	    if ((ck3Executable == "ck3" || ck3Executable == "ck3.exe") && (attilaExecutable == "attila" || attilaExecutable == "attila.exe"))
                return true;
            else
                return false;
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
                bool gamePaths = VerifyGamePaths();
                bool unitMappers = VerifyEnabledUnitMappers();


                if(!gamePaths || !unitMappers)
                {
                    if(!gamePaths) infoLabel.Text = "Games Paths Missing!";
                    else infoLabel.Text = "No Unit Mappers Enabled!";
                    ExecuteButton.Enabled = false;
                    infoLabel.ForeColor = System.Drawing.Color.FromArgb(74, 0, 0);
                }
                else if(gamePaths && unitMappers)
                {
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

            //Load Game Paths
            Options.ReadGamePaths();    

            //Hide debug button
            btt_debug.Visible = false;

            //Early Access label visibility
            EA_Label.Visible = false;

            System.Drawing.Color myColor = System.Drawing.Color.FromArgb(53, 25, 5, 5);
            infoLabel.BackColor = myColor;
            labelVersion.BackColor = myColor;
            EA_Label.BackColor = myColor;

            Options.ReadOptionsFile();
            ModOptions.StoreOptionsValues(Options.optionsValuesCollection);
            AttilaPreferences.ChangeUnitSizes();


        }

        //---------------------------------//
        //----------DEBUG BUTTON-----------//
        //---------------------------------//



        private void btt_debug_Click(object sender, EventArgs e)
        {
            Culture culture = null;
            culture.GetCultureName();
        }

        private void CreateAttilaShortcut()
        {
            if(!System.IO.File.Exists(@".\CW.lnk")) {
                object shDesktop = (object)"Desktop";
                WshShell shell = new WshShell();
                string shortcutAddress = @".\CW.lnk";
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "Shortcut with all user enabled mods and required unit mappers mods for Total War: Attila";
                shortcut.WorkingDirectory = Properties.Settings.Default.VAR_attila_path.Replace(@"\Attila.exe", "");
                shortcut.Arguments = "used_mods_cw.txt";
                shortcut.TargetPath = Properties.Settings.Default.VAR_attila_path;
                shortcut.Save();
            }
        }



        List<Army> attacker_armies;
        List<Army> defender_armies;
        private void HomePage_Shown(object sender, EventArgs e)
        {
            infoLabel.Text = "Loading DLLs...";
            ExecuteButton.Enabled= false;
            //LoadDLLs();
            ExecuteButton.Enabled= true;
            infoLabel.Text = "Ready to Start!";

            if (debugLog_Path != null)
                DataSearch.ClearLogFile();


        }


        string log;
        Thread loadingThread;
        LoadingScreen loadingScreen;
        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            sounds = new SoundPlayer(@".\data\sounds\sword-slash-with-metal-shield-impact-185433.wav");
            sounds.Play();
            _myVariable = 1;
            ExecuteButton.Enabled = false;
            ExecuteButton.BackgroundImage = Properties.Resources.start_new_disabled;
            ProcessCommands.ResumeProcess();

            /*
             *  ERASES OLD FILES
             */

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

                infoLabel.Text = "Ready to start!";
                this.Text = "Crusader Wars (Waiting for battle...)";

                try
                {
                    CreateAttilaShortcut();
                }
                catch
                {
                    MessageBox.Show("Error creating Attila shortcut!", "File Error",
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
                }
                catch
                {
                    MessageBox.Show("No Log File Found!", "Error",
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
                }
                catch
                {
                    MessageBox.Show("Couldn't find 'ck3.exe'. Change the Crusader Kings 3 path. ", "Path Error",
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

                        infoLabel.Text = "Waiting for battle...";
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
                                        battleHasStarted = true;
                                        break;
                                    }

                                }
                                logFile.Position = 0;
                                reader.DiscardBufferedData();
                                await Task.Delay(1);
                            }
                        }
                        catch
                        {
                            MessageBox.Show("Error searching for battle. ", "Critical Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            infoLabel.Text = "Ready to start!";
                            ExecuteButton.Enabled = true;
                            this.Text = "Crusader Wars";
                            CloseLoadingScreen();
                            break;
                        }


                        try
                        {
                            UpdateLoadingScreenMessage("Getting data from save file...");
                            StartLoadingScreen();

                            infoLabel.Text = "Reading battle data...";
                            this.Text = "Crusader Wars (Reading battle data...)";
                            this.Hide();

                            logFile.Position = 0;
                            reader.DiscardBufferedData();
                            log = reader.ReadToEnd();
                            log = RemoveASCII(log);

                            if (battleHasStarted)
                            {
                                DataSearch.Search(log);
                                AttilaModManager.ReadInstalledMods();
                                SetPlaythrough();
                                UpdateLoadingScreenUnitMapperMessage(UnitMappers_BETA.GetLoadedUnitMapperString());
                                AttilaModManager.CreateUserModsFile();
                            }
                        }
                        catch(Exception ex)
                        {
                            this.Show();
                            CloseLoadingScreen();
                            MessageBox.Show($"Error reading battle data: {ex.Message}", "Data Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            infoLabel.Text = "Waiting for battle...";
                            this.Text = "Crusader Wars (Waiting for battle...)";

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
                    UpdateLoadingScreenMessage("Getting data from save file...");
                    await Task.Delay(2000); //Old was 3000ms
                    ProcessCommands.SuspendProcess();

                    //path_editedSave = Properties.Settings.Default.VAR_dir_save + @"\CrusaderWars_Battle.ck3";
                    path_editedSave = @".\data\save_file_data\gamestate_file\gamestate";
                    SaveFile.Uncompress();
                    Reader.ReadFile(path_editedSave);
                    BattleResult.GetPlayerCombatResult();
                    BattleResult.ReadPlayerCombat(CK3LogData.LeftSide.GetCommander().id);
                }
                catch(Exception ex)
                {
                    this.Show();
                    CloseLoadingScreen();
                    MessageBox.Show($"Error reading the save file: {ex.Message}", "Save File Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Waiting for battle...";
                    ProcessCommands.ResumeProcess();

                    //Data Clear
                    Data.Reset();

                    continue;

                }

                try
                {

                    //1.0 Beta Debug
                    UpdateLoadingScreenMessage("Reading save file data...");
                    var armies = ArmiesReader.ReadBattleArmies();
                    attacker_armies = armies.attacker;
                    defender_armies = armies.defender;
                }
                catch(Exception ex)
                {
                    this.Show();
                    CloseLoadingScreen();
                    MessageBox.Show($"Error reading the battle armies: {ex.Message}\nDon't play in Ironman or in debug mode!\nYour Crusader Kings III saves MUST NOT be on the steam cloud.", "Beta Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    ProcessCommands.ResumeProcess();
                    infoLabel.Text = "Waiting for battle...";
                    this.Text = "Crusader Wars (Waiting for battle...)";

                    //Data Clear
                    Data.Reset();

                    continue;
                }

                var left_side = ArmiesReader.GetSideArmies("left");
                var right_side = ArmiesReader.GetSideArmies("right");
                int left_side_total = left_side.Sum(army => army.GetTotalSoldiers());
                int right_side_total = right_side.Sum(army => army.GetTotalSoldiers());
                string left_side_combat_side = left_side[0].CombatSide;
                string right_side_combat_side = right_side[0].CombatSide;




                try
                {
                    BattleDetails.ChangeBattleDetails(left_side_total, right_side_total, left_side_combat_side, right_side_combat_side);

                    Games.CloseTotalWarAttilaProcess();
                    UpdateLoadingScreenMessage("Creating battle in Total War: Attila...");

                    //Create Remaining Soldiers Script
                    BattleScript.CreateScript();

                    // Set Battle Scale
                    int total_soldiers = attacker_armies.SelectMany(army => army.Units).Sum(unit => unit.GetSoldiers()) +
                                         defender_armies.SelectMany(army => army.Units).Sum(unit => unit.GetSoldiers());
                    ArmyProportions.AutoSizeUnits(total_soldiers);
                    foreach (var army in attacker_armies) army.ScaleUnits(ModOptions.GetBattleScale());
                    foreach (var army in defender_armies) army.ScaleUnits(ModOptions.GetBattleScale());

                    //Create Battle
                    BattleFile.BETA_CreateBattle(attacker_armies, defender_armies);

                    //Close Script
                    BattleScript.CloseScript();

                    //Set Commanders Script
                    BattleScript.SetCommandersLocals();

                    //Set Units Kills Script
                    BattleScript.SetLocalsKills(Data.units_scripts);

                    //Close Script
                    BattleScript.CloseScript();

                    //Creates .pack mod file
                    PackFile.PackFileCreator();
                }
                catch(Exception ex)
                {
                    this.Show();
                    CloseLoadingScreen();
                    MessageBox.Show($"Error creating the battle:{ex.Message}", "Data Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    ProcessCommands.ResumeProcess();
                    infoLabel.Text = "Waiting for battle...";
                    this.Text = "Crusader Wars (Waiting for battle...)";

                    //Data Clear
                    Data.Reset();

                    continue;
                }

                try
                {
                    //Open Total War Attila
                    Games.StartTotalWArAttilaProcess();
                }
                catch
                {
                    this.Show();
                    CloseLoadingScreen();
                    MessageBox.Show("Couldn't find 'Attila.exe'. Change the Total War Attila path. ", "Path Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Ready to start!";
                    ProcessCommands.ResumeProcess();
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
                    
                    CloseLoadingScreen();
                    this.Show();

                }
                catch(Exception ex)
                {
                    
                    MessageBox.Show($"Error: {ex.Message}", "Application Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    Games.CloseTotalWarAttilaProcess();
                    Games.StartCrusaderKingsProcess();
                    infoLabel.Text = "Waiting for battle...";
                    this.Text = "Crusader Wars (Waiting for battle...)";

                    //Data Clear
                    Data.Reset();

                    continue;
                }

                Games.CloseCrusaderKingsProcess();

                Console.WriteLine("Battle created successfully");

                //               Retrieve battle result to ck3
                //-----------------------------------------------------------
                //                       Battle Results                     |
                //-----------------------------------------------------------

                string attilaLogPath = Properties.Settings.Default.VAR_log_attila;
                
                bool battleEnded = false;

                infoLabel.Text = "Waiting battle to end...";
                this.Text = "Crusader Wars (Waiting battle to end...)";

                //  Waiting for battle to end...
                while (battleEnded == false)
                {
                    battleEnded = BattleResult.HasBattleEnded(attilaLogPath);
                    await Task.Delay(10);
                }


                try
                {
                    if (battleEnded)
                    {
                        ModOptions.CloseAttila();

                        infoLabel.Text = "Battle has ended!";
                        this.Text = "Crusader Wars (Battle has ended)";

                        string path_log_attila = Properties.Settings.Default.VAR_log_attila;


                        //  SET CASUALITIES
                        foreach (var army in attacker_armies)
                        {
                            BattleResult.ReadAttilaResults(army, path_log_attila);
                            BattleResult.CheckForDeathCommanders(army, path_log_attila);
                            BattleResult.CheckKnightsKills(army);
                            BattleResult.CheckForDeathKnights(army);
                        }
                        foreach (var army in defender_armies)
                        {
                            BattleResult.ReadAttilaResults(army, path_log_attila);
                            BattleResult.CheckForDeathCommanders(army, path_log_attila);
                            BattleResult.CheckKnightsKills(army);
                            BattleResult.CheckForDeathKnights(army);

                        }

                        //  EDIT LIVING FILE
                        BattleResult.EditLivingFile(attacker_armies, defender_armies);

                        //  EDIT COMBATS FILE
                        BattleResult.EditCombatFile(attacker_armies, defender_armies, left_side[0].CombatSide, right_side[0].CombatSide, path_log_attila);

                        //  EDIT COMBATS RESULTS FILE
                        BattleResult.EditCombatResultsFile(attacker_armies, defender_armies);

                        //  EDIT REGIMENTS FILE
                        BattleResult.EditRegimentsFile(attacker_armies, defender_armies);

                        //  EDIT ARMY REGIMENTS FILE
                        BattleResult.EditArmyRegimentsFile(attacker_armies, defender_armies);


                        //  WRITE TO SAVE FILE
                        BattleResult.SendToSaveFile(path_editedSave);

                        //  COMPRESS SAVE FILE AND SEND TO SAVE FILE FOLDER
                        SaveFile.Compress();
                        SaveFile.Finish();

                        //  OPEN CK3 WITH BATTLE RESULTS
                        Games.LoadBattleResults();
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Error retriving battle results: {ex.Message}", "Battle Results Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    Games.CloseTotalWarAttilaProcess();
                    Games.StartCrusaderKingsProcess();
                    infoLabel.Text = "Waiting for battle...";
                    this.Text = "Crusader Wars (Waiting for battle...)";

                    //Data Clear
                    Data.Reset();
                    continue;
                }


                await Task.Delay(10);

                ArmyProportions.ResetUnitSizes();
                GC.Collect();

            }
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
                ProcessRuntime("ck3.exe");

            }

            public static void ResumeProcess()
            {
                ProcessRuntime("/r ck3.exe");
            }


        }

        /*---------------------------------------------
         * :::::::::::::: PLAYTHROUGHS ::::::::::::::::
         ---------------------------------------------*/
        void SetPlaythrough()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@".\settings\UnitMappers.xml");
            string tagName = "";
            foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                if (node is XmlComment) continue;
                if (node.InnerText == "True")
                {
                    tagName = node.Attributes["name"].Value;
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

                Process[] process_ck3 = Process.GetProcessesByName("ck3");
                if (process_ck3.Length == 0)
                {
                    Process.Start(Properties.Settings.Default.VAR_ck3_path);
                    DataSearch.ClearLogFile();
                }

            }

            public static void CloseCrusaderKingsProcess()
            {
                Process[] process_ck3 = Process.GetProcessesByName("ck3");
                foreach (Process worker in process_ck3)
                {
                    worker.Kill();
                    worker.WaitForExit();
                    worker.Dispose();
                }

             
            }

            public static void LoadBattleResults()
            {
                string ck3_path = Properties.Settings.Default.VAR_ck3_path;
                Process.Start(ck3_path, "--continuelastsave");
            }

            public static void StartTotalWArAttilaProcess()
            {
                Process.Start(@".\CW.lnk");
            }

            public async static void CloseTotalWarAttilaProcess()
            {
                Process[] process_attila = Process.GetProcessesByName("Attila");
                foreach (Process worker in process_attila)
                {
                    worker.Kill();
                    worker.WaitForExit();
                    worker.Dispose();
                }

                await Task.Delay(1000);

            }
        };

        /*---------------------------------------------
         * :::::::::::LOADING SCREEN FUNCS:::::::::::::
         ---------------------------------------------*/

        void ChangeLoadingScreenImage()
        {
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
            if (loadingScreen.InvokeRequired)
            {
                loadingScreen.Invoke(new Action(() => loadingScreen.Close()));
            }
            else
            {
                loadingScreen.Close();
            }

            // Ensure the thread is properly cleaned up
            loadingThread.Join();
            loadingThread = null;
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
            try
            {
                string dll_folder = @".\data\dlls";
                foreach (string dllFile in Directory.GetFiles(dll_folder, "*.dll"))
                {
                    Assembly assembly = Assembly.LoadFrom(dllFile);
                    AppDomain.CurrentDomain.Load(assembly.GetName());
                }
            }
            catch
            {
                return;
            }

        }

        private void SettingsBtn_Click(object sender, EventArgs e)
        {
            SettingsBtn.BackgroundImage = Properties.Resources.options_btn_new_click;
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            Options optionsChild = new Options();
            optionsChild.ShowDialog();
        }


        private void HomePage_FormClosing(object sender, FormClosingEventArgs e)
        {
            ProcessCommands.ResumeProcess();
        }

        SoundPlayer sounds;

        private void patreonBtn_Click(object sender, EventArgs e)
        {
            patreonBtn.BackgroundImage = Properties.Resources.patreon_btn_clickpng;
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            Process.Start("https://www.patreon.com/user?u=83859552");

        }

        private void WebsiteBTN_Click(object sender, EventArgs e)
        {
            WebsiteBTN.BackgroundImage = Properties.Resources.website_btn_new_click;
            sounds = new SoundPlayer(@".\data\sounds\metal-dagger-hit-185444.wav");
            sounds.Play();
            Process.Start("https://www.crusaderwars.com");

        }

        private void SteamBTN_Click(object sender, EventArgs e)
        {
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
            SteamBTN.BackgroundImage = Properties.Resources.steam_btn_new;
        }
    }
}
