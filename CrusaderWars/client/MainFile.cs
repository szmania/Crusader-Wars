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
using CrusaderWars.twbattle;
using System.Threading; // Added for CancellationToken
using System.Text.Json; // Added for playset check
using CrusaderWars.mod_manager;
using System.Xml;
using System.Web;
using System.Drawing.Text;
using CrusaderWars.sieges; // Added for SiegeEngineGenerator
using CrusaderWars.data.battle_results; // Added for BattleResult class


namespace CrusaderWars
{
    public partial class HomePage : Form
    {
        private LoadingScreen? loadingScreen;
        private Thread? loadingThread;
        private string log = null!;  // For CK3 log content
        private bool _programmaticClick = false;
        private bool battleJustCompleted = false;
        private string _appVersion = null!;
        private string? _umVersion = null; // Made nullable
        private Updater _updater = null!;
        private System.Windows.Forms.Timer _pulseTimer = null!;
        private bool _isPulsing = false;
        private int _pulseStep = 0;
        private Color _originalInfoLabelBackColor;
        private CancellationTokenSource? _battleMonitoringCts; // Added cancellation token source

        // Playthrough Display UI Elements
        private Panel playthroughPanel = null!;
        private PictureBox playthroughPictureBox = null!;
        private Label playthroughTitleLabel = null!;
        private Label playthroughNameLabel = null!;

        // Pre-release opt-in animation fields
        private System.Windows.Forms.Timer? _preReleasePulseTimer;
        private int _preReleasePulseStep = 0;


        private int _myVariable = 0;

        private void CreateRequiredDirectories()
        {
            Program.Logger.Debug("Creating required directories...");
            try
            {
                Directory.CreateDirectory(@".\data\save_file_data\gamestate_file");
                Directory.CreateDirectory(@".\data\save_file_data\temp");
                Directory.CreateDirectory(@".\data\dlls");
                Directory.CreateDirectory(@".\data\runtime");
                Directory.CreateDirectory(@".\data\sounds");
                Directory.CreateDirectory(@".\font");
                Directory.CreateDirectory(@".\settings");
                Directory.CreateDirectory(@".\unit mappers");
                Program.Logger.Debug("Required directories created/verified.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error creating directories: {ex.Message}");
                MessageBox.Show($"Could not create required application directories. Please check permissions.\n\nError: {ex.Message}", "Crusader Conflicts: Directory Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }
        public HomePage()
        {
            Options.ReadOptionsFile(); // Moved to the beginning of the constructor
            Program.Logger.Debug("HomePage initializing...");
            CreateRequiredDirectories();
            LoadFont();
            InitializeComponent();
            this.Font = new Font("Microsoft Sans Serif", 8.25f);
            
            // Set fonts programmatically
            ExecuteButton.Font = new Font("Yu Gothic UI", 16f, FontStyle.Bold);
            ContinueBattleButton.Font = new Font("Yu Gothic UI", 12f, FontStyle.Bold);
            btt_debug.Font = new Font("Microsoft Sans Serif", 12f);
            infoLabel.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Bold);
            viewLogsLink.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Bold);
            labelVersion.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Bold);
            labelMappersVersion.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Bold);
            EA_Label.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Bold); // Programmatically set EA_Label font
            labelSeparatorLeft.Font = new Font("Microsoft Sans Serif", 16f, FontStyle.Bold);
            labelSeparatorRight.Font = new Font("Microsoft Sans Serif", 16f, FontStyle.Bold);
            linkOptInPreReleases.Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Underline); // Set font for new label

            // Set FlatStyle programmatically
            ExecuteButton.FlatStyle = FlatStyle.Flat;
            ContinueBattleButton.FlatStyle = FlatStyle.Flat;
            btt_debug.FlatStyle = FlatStyle.Flat;
            SettingsBtn.FlatStyle = FlatStyle.Flat;
            viewLogsLink.FlatStyle = FlatStyle.Flat;
            WebsiteBTN.FlatStyle = FlatStyle.Flat;
            SteamBTN.FlatStyle = FlatStyle.Flat;
            discordLink.FlatStyle = FlatStyle.Flat;
            labelVersion.FlatStyle = FlatStyle.Flat;
            labelMappersVersion.FlatStyle = FlatStyle.Flat;
            // linkOptInPreReleases is a Label, no FlatStyle needed

            // Add hover effects for links
            viewLogsLink.MouseEnter += (sender, e) => viewLogsLink.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150);
            viewLogsLink.MouseLeave += (sender, e) => viewLogsLink.ForeColor = System.Drawing.Color.WhiteSmoke;
            discordLink.MouseEnter += (sender, e) => discordLink.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150);
            discordLink.MouseLeave += (sender, e) => discordLink.ForeColor = System.Drawing.Color.WhiteSmoke;

            labelVersion.MouseEnter += (sender, e) => { labelVersion.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150); };
            labelVersion.MouseLeave += (sender, e) => { labelVersion.ForeColor = System.Drawing.Color.WhiteSmoke; };
            labelMappersVersion.MouseEnter += (sender, e) => { labelMappersVersion.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150); };
            labelMappersVersion.MouseLeave += (sender, e) => { labelMappersVersion.ForeColor = System.Drawing.Color.WhiteSmoke; };
            // NEW HOVER EFFECTS FOR linkOptInPreReleases
            linkOptInPreReleases.MouseEnter += (sender, e) => {
                _preReleasePulseTimer?.Stop();
                linkOptInPreReleases.ForeColor = System.Drawing.Color.FromArgb(200, 200, 150); 
            };
            linkOptInPreReleases.MouseLeave += (sender, e) => {
                if (ModOptions.GetOptInPreReleases()) {
                    linkOptInPreReleases.ForeColor = Color.Gold;
                } else {
                    _preReleasePulseTimer?.Start();
                }
            };
            linkOptInPreReleases.Click += new EventHandler(linkOptInPreReleases_Click); // Add click event handler

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

            _updater = new Updater();
            _updater.GetAppVersion();
            _updater.GetUnitMappersVersion();


            _appVersion = _updater.AppVersion;

            // Fallback for _appVersion if Updater fails
            if (string.IsNullOrEmpty(_appVersion))
            {
                try
                {
                    _appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"; // Added null-coalescing
                    Program.Logger.Debug($"Fallback: Retrieved app version from assembly: {_appVersion}");
                }
                catch (Exception ex)
                {
                    _appVersion = "1.0.0"; // Hardcoded default if assembly version also fails
                    Program.Logger.Debug($"Fallback: Could not retrieve app version from assembly. Defaulting to {_appVersion}. Error: {ex.Message}");
                }
            }

            labelVersion.Text = $"v{_appVersion.TrimStart('v')}";

            // Line 175 - Add null check
            _umVersion = _updater?.UMVersion; // Make nullable
            if (!string.IsNullOrWhiteSpace(_umVersion))
            {
                labelMappersVersion.Text = $"(mappers v{_umVersion.TrimStart('v')})";
            }
            else
            {
                labelMappersVersion.Text = "(mappers version unknown)"; // Default
            }
            Program.Logger.Debug($"Current App Version: {_updater?.AppVersion ?? "unknown"}");

            var _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1000; // check variable every second
            _timer.Tick += Timer_Tick;
            _timer.Start();
            Original_Color = infoLabel.ForeColor;
            Program.Logger.Debug("HomePage initialization complete.");

            _pulseTimer = new System.Windows.Forms.Timer();
            _pulseTimer.Interval = 100;
            _pulseTimer.Tick += PulseTimer_Tick;

            _preReleasePulseTimer = new System.Windows.Forms.Timer();
            _preReleasePulseTimer.Interval = 75; // Sets the pulse speed
            _preReleasePulseTimer.Tick += PreReleasePulseTimer_Tick;
        }

        private void PulseTimer_Tick(object? sender, EventArgs e)
        {
            _pulseStep = (_pulseStep + 1) % 20; // 20 steps for a full cycle (10 up, 10 down)
            int redComponent = 120 + (_pulseStep < 10 ? _pulseStep * 10 : (20 - _pulseStep) * 10);
            SettingsBtn.FlatAppearance.BorderColor = Color.FromArgb(redComponent, 30, 30);
            SettingsBtn.FlatAppearance.BorderSize = 2;
        }

        private void PreReleasePulseTimer_Tick(object? sender, EventArgs e)
        {
            if (ModOptions.GetOptInPreReleases()) return; // Safety check

            _preReleasePulseStep = (_preReleasePulseStep + 1) % 20; // 20 steps for a full cycle
            // Pulse between a bright yellow (255, 255, 150) and a slightly dimmer yellow (255, 255, 50)
            int blueComponent = 150 - (_preReleasePulseStep < 10 ? _preReleasePulseStep * 10 : (20 - _pulseStep) * 10);
            linkOptInPreReleases.ForeColor = Color.FromArgb(255, 255, blueComponent);
        }

        private PrivateFontCollection fonts = new PrivateFontCollection();
        private Font customFont = null!;

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
                MessageBox.Show("Font file not found.", "Crusader Conflicts: Font error");
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
            string filePath = @".\settings\UnitMappers.xml";
            if (!File.Exists(filePath))
            {
                return false; // If file doesn't exist, no mappers are enabled.
            }

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);
                var root = xmlDoc.DocumentElement;
                if (root != null)
                {
                    foreach (XmlNode node in root.ChildNodes)
                    {
                        if (node is XmlComment) continue;
                        if (node.InnerText == "True")
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error reading UnitMappers.xml in VerifyEnabledUnitMappers: {ex.Message}");
                return false; // Treat errors as no mappers enabled
            }


            return false;
        }

        private void Timer_Tick(object? sender, EventArgs e)
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
                    if(!gamePaths) infoLabel.Text = "Games Paths Missing! Select your game paths on the Mod Settings screen.";
                    else infoLabel.Text = "No Unit Mappers Enabled! Select a Playthrough on the Mod Settings screen.";
                    ExecuteButton.Enabled = false;
                    infoLabel.ForeColor = Color.White;
                    infoLabel.BackColor = Color.FromArgb(180, 74, 0, 0);

                    if(!_isPulsing)
                    {
                        _isPulsing = true;
                        _pulseTimer.Start();
                    }
                }
                else if(gamePaths && unitMappers)
                {
                    infoLabel.AutoSize = true;
                    ExecuteButton.Enabled = true;
                    infoLabel.Text = "Ready to Start!";
                    infoLabel.ForeColor = Original_Color;
                    infoLabel.BackColor = _originalInfoLabelBackColor;
                    if (_isPulsing)
                    {
                        _isPulsing = false;
                        _pulseTimer.Stop();
                        SettingsBtn.FlatAppearance.BorderSize = 0;
                        SettingsBtn.Invalidate();
                    }
                }
            }

        }

        string path_editedSave = null!;

        static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static string debugLog_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\console_history.txt";
        string saveGames_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\save games";
        private async void Form1_Load(object sender, EventArgs e)
        {
            Program.Logger.Debug("Form1_Load event triggered.");
            //Load Game Paths
            Options.ReadGamePaths();    

            // Set locations programmatically
            btt_debug.Location = new Point(273, 16);
            infoLabel.Location = new Point(52, 493);
            pictureBox1.Location = new Point(4, 4);
            WebsiteBTN.Location = new Point(46, 353);
            SteamBTN.Location = new Point(46, 180); // Adjusted from 186
            SettingsBtn.Location = new Point(4, 4);
            EA_Label.Location = new Point(75, 0);
            discordLink.Location = new Point(378, 0);
            MainPanelLayout.Location = new Point(460, 0);
            BottomPanelLayout.Location = new Point(0, 668);
            tableLayoutPanel1.Location = new Point(0, 0);
            labelSeparatorLeft.Location = new Point(100, 0); // Example, adjust as needed
            labelSeparatorRight.Location = new Point(100, 0); // Example, adjust as needed
            // Positioning for FlowLayoutPanel is handled by the panel itself, no need to set here.


            // Set sizes programmatically
            btt_debug.Size = new Size(179, 39);
            infoLabel.Size = new Size(199, 31);
            SettingsBtn.Size = new Size(248, 158);
            pictureBox1.Size = new Size(295, 300);
            discordLink.Size = new Size(32, 32);
            MainPanelLayout.Size = new Size(299, 705); // Programmatically set MainPanelLayout size
            tableLayoutPanel1.Size = new Size(256, 668); // Programmatically set tableLayoutPanel1 size
            this.ClientSize = new Size(1219, 705); // Programmatically set form ClientSize

            // Set anchors programmatically
            ExecuteButton.Anchor = AnchorStyles.None;
            ContinueBattleButton.Anchor = AnchorStyles.None;
            btt_debug.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            infoLabel.Anchor = AnchorStyles.None;
            viewLogsLink.Anchor = AnchorStyles.None;
            labelVersion.Anchor = AnchorStyles.None;
            labelMappersVersion.Anchor = AnchorStyles.None;
            linkOptInPreReleases.Anchor = AnchorStyles.None; // Anchor for new label
            pictureBox1.Anchor = AnchorStyles.None;
            EA_Label.Anchor = AnchorStyles.None;
            discordLink.Anchor = AnchorStyles.None;
            labelSeparatorLeft.Anchor = AnchorStyles.None;
            labelSeparatorRight.Anchor = AnchorStyles.None;

            // Set DockStyle for buttons in tableLayoutPanel1 to make them fill the cell
            SettingsBtn.Dock = DockStyle.Fill;
            WebsiteBTN.Dock = DockStyle.Fill;
            SteamBTN.Dock = DockStyle.Fill;
            MainPanelLayout.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            BottomPanelLayout.Dock = DockStyle.Bottom;
            tableLayoutPanel1.Dock = DockStyle.Left;

            // Set RightToLeft property programmatically
            WebsiteBTN.RightToLeft = RightToLeft.No;

            // Set BackgroundImageLayout properties programmatically
            ExecuteButton.BackgroundImageLayout = ImageLayout.Zoom;
            ContinueBattleButton.BackgroundImageLayout = ImageLayout.Zoom;
            SettingsBtn.BackgroundImageLayout = ImageLayout.Zoom;
            WebsiteBTN.BackgroundImageLayout = ImageLayout.Zoom;
            SteamBTN.BackgroundImageLayout = ImageLayout.Zoom;
            pictureBox1.BackgroundImageLayout = ImageLayout.Center;
            this.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

            // Set TextAlign properties programmatically
            infoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            labelVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            labelMappersVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            linkOptInPreReleases.TextAlign = ContentAlignment.MiddleLeft; // TextAlign for new label
            EA_Label.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            viewLogsLink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            labelSeparatorLeft.TextAlign = ContentAlignment.MiddleCenter;
            labelSeparatorRight.TextAlign = ContentAlignment.MiddleCenter;

            // Set margins and paddings programmatically
            ExecuteButton.Margin = new Padding(4, 4, 4, 4);
            ContinueBattleButton.Margin = new Padding(4, 4, 4, 4);
            btt_debug.Margin = new Padding(4, 4, 4, 4);
            infoLabel.Margin = new Padding(4, 0, 4, 0);
            infoLabel.Padding = new Padding(3, 3, 3, 3);
            SettingsBtn.Margin = new Padding(4, 4, 4, 4);
            viewLogsLink.Margin = new Padding(4, 3, 4, 0);
            viewLogsLink.Padding = new Padding(3, 3, 3, 3);
            labelVersion.Margin = new Padding(0, 3, 4, 0);
            labelMappersVersion.Margin = new Padding(4, 3, 4, 0);
            linkOptInPreReleases.Margin = new Padding(0, 3, 4, 5); // Margin for new label
            linkOptInPreReleases.Padding = new Padding(0, 3, 3, 3); // Padding for new label
            pictureBox1.Margin = new Padding(4, 4, 4, 4);
            MainPanelLayout.Margin = new Padding(4, 4, 4, 4);
            EA_Label.Margin = new Padding(4, 0, 4, 0);
            EA_Label.Padding = new Padding(3, 3, 3, 3);
            discordLink.Margin = new Padding(4, 3, 4, 0);
            BottomPanelLayout.Margin = new Padding(0, 4, 4, 4);
            BottomPanelLayout.Padding = new Padding(0); // Added this line
            WebsiteBTN.Margin = new Padding(4, 4, 4, 4);
            SteamBTN.Margin = new Padding(4, 4, 4, 4);
            tableLayoutPanel1.Margin = new Padding(4, 4, 4, 4);
            this.Margin = new Padding(4, 4, 4, 4); // For the form itself
            BottomLeftFlowPanel.Padding = new Padding(0, 5, 0, 0);
            BottomLeftFlowPanel.Margin = new Padding(0, 3, 3, 3); // Added this line
            BottomRightFlowPanel.Padding = new Padding(0, 5, 0, 0);
            labelSeparatorLeft.Margin = new Padding(4, 3, 4, 0);
            labelSeparatorRight.Margin = new Padding(4, 3, 4, 0);


            // Configure tableLayoutPanel1 layout settings programmatically
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Clear();
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Clear();
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));

            //Hide debug button
            btt_debug.Visible = false;

            //Early Access label visibility
            EA_Label.Visible = false;

            System.Drawing.Color myColor = System.Drawing.Color.FromArgb(53, 25, 5, 5);
            _originalInfoLabelBackColor = myColor;
            infoLabel.BackColor = myColor;
            labelVersion.BackColor = myColor;
            labelMappersVersion.BackColor = myColor;
            VersionInfoFlowPanel.BackColor = Color.Transparent; // NEW

            // Initialize and configure Playthrough Display
            InitializePlaythroughDisplay();

            // Options.ReadOptionsFile(); // REMOVED: Moved to constructor
            // Line 452 - Add null check
            // Removed the block:
            // if (Options.optionsValuesCollection != null)
            // {
            //     ModOptions.StoreOptionsValues(Options.optionsValuesCollection);
            // }
            AttilaPreferences.ChangeUnitSizes();
            AttilaPreferences.ValidateOnStartup();

            // Set initial state for Opt-in to pre-releases button
            UpdatePreReleaseLinkState();

            UpdateUIForBattleState();
            UpdatePlaythroughDisplay(); // Initial update

            // Set ToolTips
            InformationToolTip.SetToolTip(ExecuteButton, "Start or continue a Crusader Conflicts campaign by launching Crusader Kings 3.");
            InformationToolTip.SetToolTip(ContinueBattleButton, "Restart the current Total War: Attila battle without reloading Crusader Kings 3. Use this if the battle crashed or you want to try again.");
            InformationToolTip.SetToolTip(SettingsBtn, "Configure game paths, battle options, and unit mappers.");
            InformationToolTip.SetToolTip(WebsiteBTN, "Visit the official Crusader Conflicts website for news and updates.");
            InformationToolTip.SetToolTip(SteamBTN, "View the Crusader Conflicts mod on the Steam Workshop.");
            InformationToolTip.SetToolTip(viewLogsLink, "Click to find the debug.log file. Please share this file on our Discord for troubleshooting help.");
            InformationToolTip.SetToolTip(discordLink, "Join our Discord community for help and updates.");

            InformationToolTip.SetToolTip(labelVersion, "Crusader Conflicts application version.");
            InformationToolTip.SetToolTip(labelMappersVersion, "Version of the installed Unit Mappers.");
            // NEW TOOLTIPS
            InformationToolTip.SetToolTip(linkOptInPreReleases, "Click to get early access to new features via pre-release updates."); // Updated tooltip

            infoLabel.MaximumSize = new Size(MainPanelLayout.Width - 10, 0);

            Program.Logger.Debug("Starting updater checks...");
            Program.Logger.Debug("Initiating app and unit mappers version checks.");
            await _updater.CheckAppVersion();
            // If an app update is found, the process will exit and the next line won't be reached.
            await _updater.CheckUnitMappersVersion();
            await CheckForCK3ModUpdatesAsync(); // MOVED TO Form1_Load

            Program.Logger.Debug("Form1_Load complete.");

            ShowOneTimeNotifications();
        }

        private void UpdatePreReleaseLinkState()
        {
            bool isOptedIn = ModOptions.GetOptInPreReleases();
            if (isOptedIn)
            {
                _preReleasePulseTimer?.Stop();
                linkOptInPreReleases.Text = "Early Access Enabled (click to disable)";
                linkOptInPreReleases.ForeColor = Color.Gold;
            }
            else
            {
                linkOptInPreReleases.Text = "Opt-in for Early Access to New Features";
                _preReleasePulseTimer?.Start();
            }
        }

        private void InitializePlaythroughDisplay()
        {
            playthroughPanel = new Panel();
            playthroughPictureBox = new PictureBox();
            playthroughTitleLabel = new Label();
            playthroughNameLabel = new Label();

            // Panel
            playthroughPanel.SuspendLayout();
            playthroughPanel.Size = new Size(280, 80);
            playthroughPanel.Location = new Point(this.ClientSize.Width - playthroughPanel.Width - 10, 10);
            playthroughPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            playthroughPanel.BackColor = Color.FromArgb(50, 0, 0, 0); // Semi-transparent black
            playthroughPanel.BorderStyle = BorderStyle.FixedSingle;

            // PictureBox
            playthroughPictureBox.Size = new Size(60, 60);
            playthroughPictureBox.Location = new Point(10, 10);
            playthroughPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            playthroughPictureBox.BackColor = Color.Transparent;

            // Title Label
            playthroughTitleLabel.Text = "Active Playthrough:";
            playthroughTitleLabel.Font = new Font("Microsoft Sans Serif", 10f, FontStyle.Bold);
            playthroughTitleLabel.ForeColor = Color.WhiteSmoke;
            playthroughTitleLabel.Location = new Point(playthroughPictureBox.Right + 10, 10);
            playthroughTitleLabel.AutoSize = true;

            // Name Label
            playthroughNameLabel.Font = new Font("Microsoft Sans Serif", 12f, FontStyle.Italic);
            playthroughNameLabel.ForeColor = Color.WhiteSmoke;
            playthroughNameLabel.Location = new Point(playthroughPictureBox.Right + 10, playthroughTitleLabel.Bottom + 5);
            playthroughNameLabel.AutoSize = false; // Changed to false for fixed size and wrapping
            playthroughNameLabel.Size = new Size(190, 40); // Fixed size to allow two lines of text

            // Add controls
            playthroughPanel.Controls.Add(playthroughPictureBox);
            playthroughPanel.Controls.Add(playthroughTitleLabel);
            playthroughPanel.Controls.Add(playthroughNameLabel);
            this.Controls.Add(playthroughPanel);
            playthroughPanel.BringToFront();
            playthroughPanel.ResumeLayout(false);
            playthroughPanel.PerformLayout();
        }

        private void UpdatePlaythroughDisplay()
        {
            Program.Logger.Debug("Updating playthrough display on main screen.");
            string activePlaythroughTag = "";
            string userFriendlyName = "";

            // 1. Find active playthrough tag from UnitMappers.xml
            string um_file = @".\settings\UnitMappers.xml";
            if (File.Exists(um_file))
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(um_file);
                    var root = xmlDoc.DocumentElement;
                    if (root != null)
                    {
                        foreach (XmlNode node in root.ChildNodes)
                        {
                            if (node is XmlComment) continue;
                            if (node.InnerText == "True")
                            {
                                activePlaythroughTag = node.Attributes?["name"]?.Value ?? "";
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error reading UnitMappers.xml for display: {ex.Message}");
                    activePlaythroughTag = ""; // Reset on error
                }
            }

            // 2. If a playthrough is active, find its folder and image
            if (!string.IsNullOrEmpty(activePlaythroughTag))
            {
                string playthroughFolderPath = "";
                string unitMappersDir = @".\unit mappers";
                if (Directory.Exists(unitMappersDir))
                {
                    // Find the directory whose tag.txt matches the active playthrough tag
                    foreach (var dir in Directory.GetDirectories(unitMappersDir))
                    {
                        string tagFile = Path.Combine(dir, "tag.txt");
                        if (File.Exists(tagFile))
                        {
                            if (File.ReadAllText(tagFile).Trim() == activePlaythroughTag)
                            {
                                playthroughFolderPath = dir;
                                break;
                            }
                        }
                    }
                }

                // 3. Update UI if folder was found
                if (!string.IsNullOrEmpty(playthroughFolderPath))
                {
                    Program.Logger.Debug($"Active playthrough found: {activePlaythroughTag}");
                    // Find and load image, avoiding file lock
                    var imagePath = Directory.GetFiles(playthroughFolderPath, "*.png").FirstOrDefault();
                    if (imagePath != null)
                    {
                        try
                        {
                            // Load image via a memory stream to prevent file locking.
                            byte[] imageBytes = File.ReadAllBytes(imagePath);
                            using (var ms = new MemoryStream(imageBytes))
                            {
                                // Dispose previous image if it exists
                                playthroughPictureBox.Image?.Dispose();
                                // Create a new Bitmap from the stream. This loads the data into memory.
                                playthroughPictureBox.Image = new Bitmap(ms);
                            }
                            playthroughPictureBox.Visible = true;
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.Debug($"Error loading playthrough image from {imagePath}: {ex.Message}");
                            playthroughPictureBox.Image?.Dispose(); // Ensure clean state on error
                            playthroughPictureBox.Image = null;
                            playthroughPictureBox.Visible = false;
                        }
                    }
                    else
                    {
                        playthroughPictureBox.Image?.Dispose();
                        playthroughPictureBox.Image = null;
                        playthroughPictureBox.Visible = false;
                    }

                    // Format name and update label
                    userFriendlyName = System.Text.RegularExpressions.Regex.Replace(activePlaythroughTag, "(\\B[A-Z])", " $1");
                    playthroughNameLabel.Text = userFriendlyName;
                    playthroughNameLabel.ForeColor = Color.WhiteSmoke;
                    playthroughPanel.Visible = true;
                }
                else
                {
                    Program.Logger.Debug($"Active playthrough '{activePlaythroughTag}' is set, but its folder was not found in 'unit mappers'.");
                    activePlaythroughTag = ""; // Folder not found, treat as no playthrough selected
                }
            }

            // 4. Handle case where no playthrough is selected
            if (string.IsNullOrEmpty(activePlaythroughTag))
            {
                Program.Logger.Debug("No active playthrough selected.");
                playthroughPictureBox.Image?.Dispose();
                playthroughPictureBox.Image = null;
                playthroughPictureBox.Visible = false;
                playthroughNameLabel.Text = "None Selected - Go to Mod Settings";
                playthroughNameLabel.ForeColor = Color.FromArgb(255, 128, 128); // Soft red
                playthroughPanel.Visible = true;
            }
        }

        private Version ParseVersionString(string? versionString, string versionType) // Made versionString nullable
        {
            if (string.IsNullOrEmpty(versionString))
            {
                Program.Logger.Debug($"Version string for '{versionType}' is null or empty. Defaulting to 0.0.0.");
                return new Version("0.0.0");
            }

            string sanitizedVersionString = versionString.TrimStart('v');
            int hyphenIndex = sanitizedVersionString.IndexOf('-');
            if (hyphenIndex >= 0)
            {
                sanitizedVersionString = sanitizedVersionString.Substring(0, hyphenIndex);
            }

            try
            {
                return new Version(sanitizedVersionString);
            }
            catch (FormatException ex)
            {
                Program.Logger.Debug($"Invalid {versionType} version format: {versionString}. Error: {ex.Message}. Defaulting to 0.0.0.");
                return new Version("0.0.0"); // Default if malformed
            }
        }

        private void ShowOneTimeNotifications()
        {
            // Get current versions
            Version currentAppVersion = ParseVersionString(_appVersion, "Current App");
            Version currentUMVersion = ParseVersionString(_umVersion, "Current Unit Mapper");

            // Get last notified versions from settings
            Version lastNotifiedAppVersion = ParseVersionString(Properties.Settings.Default.LastNotifiedVersion, "Last Notified App");
            Version lastNotifiedUMVersion = ParseVersionString(Properties.Settings.Default.LastNotifiedUMVersion, "Last Notified Unit Mapper");

            bool newAppVersionDetected = currentAppVersion > lastNotifiedAppVersion;
            bool newUMVersionDetected = currentUMVersion > lastNotifiedUMVersion;

            // Compare versions
            if (newAppVersionDetected || newUMVersionDetected)
            {
                if (newAppVersionDetected) Program.Logger.Debug($"New application version detected ({currentAppVersion} > {lastNotifiedAppVersion}).");
                if (newUMVersionDetected) Program.Logger.Debug($"New unit mapper version detected ({currentUMVersion} > {lastNotifiedUMVersion}).");
                Program.Logger.Debug("Displaying update notification.");

                // Create a custom form for the notification
                Form notificationForm = new Form();
                notificationForm.Text = "Crusader Conflicts: Latest Updates";
                notificationForm.ClientSize = new Size(450, 340);
                notificationForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                notificationForm.StartPosition = FormStartPosition.CenterParent;
                notificationForm.MaximizeBox = false;
                notificationForm.MinimizeBox = false;
                notificationForm.ShowInTaskbar = false;
                notificationForm.Icon = this.Icon; // Use the main form's icon

                Label messageLabel = new Label();
                messageLabel.Text = 
                    "AGOT Playthrough Update!\n\n" +
                    "• The AGOT playthrough now requires the 'Lord of the Tides' submod, which adds House Velaryon.\n" +
                    "• Download it here: https://www.moddb.com/downloads/lord-of-the-tides-v04\n" +
                    "----------------------------------------------------------\n\n" +
                    "Sieges Added!\n\n" +
                    "• Siege battles can be fought and won in all your favorite playthroughs!.\n" +
                    "----------------------------------------------------------\n\n" +
                    "New Playthroughs Added!\n\n" +
                    "• A Game of Thrones (AGOT) playthrough is now available.\n" +
                    "• The Lord of the Rings (LOTR) playthrough is now fully supported.\n\n" +
                    "----------------------------------------------------------\n\n";
                messageLabel.Location = new Point(10, 10);
                messageLabel.AutoSize = true;
                messageLabel.MaximumSize = new Size(notificationForm.ClientSize.Width - 20, 0);
                messageLabel.Font = new Font("Microsoft Sans Serif", 10f);
                notificationForm.Controls.Add(messageLabel);

                Button okButton = new Button();
                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                okButton.Size = new Size(100, 30);
                // Position the button below the label, centered horizontally.
                okButton.Location = new Point((notificationForm.ClientSize.Width - okButton.Width) / 2, messageLabel.Bottom + 15);
                notificationForm.Controls.Add(okButton);

                // Resize the form to fit the label and the button with padding.
                notificationForm.ClientSize = new Size(notificationForm.ClientSize.Width, okButton.Bottom + 15);
                notificationForm.AcceptButton = okButton;

                notificationForm.ShowDialog(this);

                // Update both settings to the current versions
                Properties.Settings.Default.LastNotifiedVersion = _appVersion.TrimStart('v');
                Properties.Settings.Default.LastNotifiedUMVersion = _umVersion?.TrimStart('v') ?? "0.0.0"; // Added null-coalescing
                Properties.Settings.Default.Save();
                Program.Logger.Debug($"LastNotifiedVersion updated to: {Properties.Settings.Default.LastNotifiedVersion}");
                Program.Logger.Debug($"LastNotifiedUMVersion updated to: {Properties.Settings.Default.LastNotifiedUMVersion}");
            }
            else
            {
                Program.Logger.Debug($"Application version ({currentAppVersion}) is not newer than last notified version ({lastNotifiedAppVersion}).");
                Program.Logger.Debug($"Unit mapper version ({currentUMVersion}) is not newer than last notified version ({lastNotifiedUMVersion}).");
                Program.Logger.Debug("Skipping update notification.");
            }
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

                // Resize buttons to fit side-by-by
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
            Culture? culture = null;
            culture!.GetCultureName();
        }

        // This method creates a shortcut for Attila
        private void CreateAttilaShortcut()
        {
            string shortcutPath = @".\CW.lnk";
            string targetPath = Properties.Settings.Default.VAR_attila_path;
            string workingDirectory = Properties.Settings.Default.VAR_attila_path.Replace(@"\Attila.exe", "");
            string arguments = "used_mods_cc.txt";
            string description = "Shortcut with all user enabled mods and required unit mappers mods for Total War: Attila";

            bool needsUpdate = true;

            if (System.IO.File.Exists(shortcutPath))
            {
                Program.Logger.Debug("Attila shortcut found. Validating its properties...");
                try
                {
                    Type? t = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(t!)!;
                    var shortcut = shell.CreateShortcut(shortcutPath);

                    // Compare properties
                    if (shortcut.TargetPath == targetPath &&
                        shortcut.WorkingDirectory == workingDirectory &&
                        shortcut.Arguments == arguments)
                    {
                        needsUpdate = false;
                        Program.Logger.Debug("Attila shortcut is up-to-date.");
                    }
                    else
                    {
                        Program.Logger.Debug("Attila shortcut is outdated or incorrect. Recreating...");
                        Program.Logger.Debug($"  - Current TargetPath: {shortcut.TargetPath}, Expected: {targetPath}");
                        Program.Logger.Debug($"  - Current WorkingDirectory: {shortcut.WorkingDirectory}, Expected: {workingDirectory}");
                        Program.Logger.Debug($"  - Current Arguments: {shortcut.Arguments}, Expected: {arguments}");
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error reading existing Attila shortcut properties: {ex.Message}. Shortcut will be recreated.");
                    needsUpdate = true; // Ensure it's recreated on error
                }
            }
            else
            {
                Program.Logger.Debug("Attila shortcut not found. Creating...");
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                try
                    {
                    CreateShortcut(shortcutPath, targetPath, workingDirectory, arguments, description);
                    Program.Logger.Debug("Attila shortcut created/updated successfully.");
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error creating/updating Attila shortcut: {ex.Message}");
                    // Optionally re-throw or handle more gracefully if shortcut creation is critical
                }
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string arguments, string description)
        {
            // Use Windows Script Host to create shortcut
            try
            {
                Type? t = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(t!)!;
                var shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDirectory;
                shortcut.Arguments = arguments;
                shortcut.Description = description;
                shortcut.Save();
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error creating shortcut with WSH: {ex.Message}");
                // Fallback: create a simple batch file
                try
                {
                    string batchPath = shortcutPath.Replace(".lnk", ".bat");
                    string batchContent = $"@echo off\n" +
                                         $"cd /d \"{workingDirectory}\"\n" +
                                         $"start \"\" \"{targetPath}\" {arguments}\n";
                    System.IO.File.WriteAllText(batchPath, batchContent);
                    Program.Logger.Debug($"Created batch file fallback: {batchPath}");
                }
                catch (Exception batchEx)
                {
                    Program.Logger.Debug($"Error creating batch file fallback: {batchEx.Message}");
                }
            }
        }

        private void HomePage_Shown(object sender, EventArgs e)
        {
            // Empty event handler to satisfy designer
        }

        List<Army> attacker_armies = null!;
        List<Army> defender_armies = null!;
        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Execute button clicked.");

            // Check if Crusader Conflicts mod is enabled in the playset
            string ck3SaveGameDir = Properties.Settings.Default.VAR_dir_save;
            if (!string.IsNullOrEmpty(ck3SaveGameDir))
            {
                 string? parentDir = Path.GetDirectoryName(ck3SaveGameDir);
                 if (parentDir != null && Directory.Exists(parentDir))
                 {
                     string dlcLoadPath = Path.Combine(parentDir, "dlc_load.json");
                     if (File.Exists(dlcLoadPath))
                     {
                         try
                         {
                             string jsonContent = File.ReadAllText(dlcLoadPath);
                             var enabledMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                             using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                             {
                                 JsonElement root = doc.RootElement;
                                 if (root.TryGetProperty("enabled_mods", out JsonElement enabledModsElement) && enabledModsElement.ValueKind == JsonValueKind.Array)
                                 {
                                     foreach (JsonElement modEntry in enabledModsElement.EnumerateArray())
                                     {
                                         string? modPath = modEntry.GetString();
                                         if (modPath != null)
                                         {
                                             // The path is like "mod/crusader_conflicts.mod", GetFileName extracts the .mod file name
                                             enabledMods.Add(Path.GetFileName(modPath));
                                         }
                                     }
                                 }
                             }
 
                             // Check for base mod
                             if (!enabledMods.Contains("crusader_conflicts.mod"))
                             {
                                 Program.Logger.Debug("Crusader Conflicts mod not found in enabled_mods in dlc_load.json.");
                                 var result = MessageBox.Show("It appears the Crusader Conflicts CK3 mod is not enabled in your Paradox Launcher playset. Do you still want to continue?",
                                                              "Crusader Conflicts Mod Not Enabled",
                                                              MessageBoxButtons.YesNo,
                                                              MessageBoxIcon.Warning);
                                 if (result == DialogResult.No)
                                 {
                                     Program.Logger.Debug("User cancelled execution because mod is not enabled in current Paradox Launcher playset.");
                                     return; // Stop execution
                                 }
                             }
                             else
                             {
                                 Program.Logger.Debug("Crusader Conflicts mod is enabled in the current Paradox Launcher playset.");
                             }
 
                             // Check for compatibility patches based on playthrough
                             string activePlaythrough = GetActivePlaythroughTag();
                             string requiredPatch = "";
                             string playthroughName = "";
 
                             if (activePlaythrough == "AGOT")
                             {
                                 requiredPatch = "crusader_conflicts_agot_compat_patch.mod";
                                 playthroughName = "A Game of Thrones (AGOT)";
                             }
                             else if (activePlaythrough == "RealmsInExile")
                             {
                                 requiredPatch = "crusader_conflicts_realms_in_exile_compat_patch.mod";
                                 playthroughName = "Realms in Exile (LOTR)";
                             }
 
                             if (!string.IsNullOrEmpty(requiredPatch) && !enabledMods.Contains(requiredPatch))
                             {
                                 Program.Logger.Debug($"Required compatibility patch '{requiredPatch}' for playthrough '{playthroughName}' not found in dlc_load.json.");
                                 var result = MessageBox.Show($"You have the '{playthroughName}' playthrough selected, but the required compatibility patch is not enabled in your Paradox Launcher playset.\n\nRequired patch: {requiredPatch}\n\nDo you still want to continue?",
                                                              "Compatibility Patch Not Enabled",
                                                              MessageBoxButtons.YesNo,
                                                              MessageBoxIcon.Warning);
                                 if (result == DialogResult.No)
                                 {
                                     Program.Logger.Debug("User cancelled execution because compatibility patch is not enabled.");
                                     return; // Stop execution
                                 }
                             }
                             else if (!string.IsNullOrEmpty(requiredPatch))
                             {
                                 Program.Logger.Debug($"Required compatibility patch '{requiredPatch}' for playthrough '{playthroughName}' is enabled.");
                             }
 
                             // Check for incorrectly enabled compatibility patches
                             string agotPatch = "crusader_conflicts_agot_compat_patch.mod";
                             string lotrPatch = "crusader_conflicts_realms_in_exile_compat_patch.mod";
 
                             if (activePlaythrough == "AGOT" && enabledMods.Contains(lotrPatch))
                             {
                                 Program.Logger.Debug("AGOT playthrough is active, but Realms in Exile patch is also enabled.");
                                 MessageBox.Show("You have the 'A Game of Thrones' playthrough selected, but the compatibility patch for 'Realms in Exile (LOTR)' is also enabled in your Paradox Launcher playset.\n\nThis can cause issues. Please disable the 'Realms in Exile' patch before continuing.",
                                                 "Incorrect Compatibility Patch Enabled",
                                                 MessageBoxButtons.OK,
                                                 MessageBoxIcon.Warning);
                                 return;
                             }
 
                             if (activePlaythrough == "RealmsInExile" && enabledMods.Contains(agotPatch))
                             {
                                 Program.Logger.Debug("Realms in Exile playthrough is active, but AGOT patch is also enabled.");
                                 MessageBox.Show("You have the 'Realms in Exile (LOTR)' playthrough selected, but the compatibility patch for 'A Game of Thrones' is also enabled in your Paradox Launcher playset.\n\nThis can cause issues. Please disable the 'A Game of Thrones' patch before continuing.",
                                                 "Incorrect Compatibility Patch Enabled",
                                                 MessageBoxButtons.OK,
                                                 MessageBoxIcon.Warning);
                                 return;
                             }
 
                             if (activePlaythrough != "AGOT" && activePlaythrough != "RealmsInExile")
                             {
                                 if (enabledMods.Contains(agotPatch))
                                 {
                                     Program.Logger.Debug($"'{activePlaythrough}' playthrough is active, but AGOT patch is also enabled.");
                                     MessageBox.Show($"You have the '{GetFriendlyPlaythroughName(activePlaythrough)}' playthrough selected, but the compatibility patch for 'A Game of Thrones' is enabled in your Paradox Launcher playset.\n\nThis can cause issues. Please disable the 'A Game of Thrones' patch before continuing.",
                                                     "Incorrect Compatibility Patch Enabled",
                                                     MessageBoxButtons.OK,
                                                     MessageBoxIcon.Warning);
                                     return;
                                 }
                                 if (enabledMods.Contains(lotrPatch))
                                 {
                                     Program.Logger.Debug($"'{activePlaythrough}' playthrough is active, but Realms in Exile patch is also enabled.");
                                     MessageBox.Show($"You have the '{GetFriendlyPlaythroughName(activePlaythrough)}' playthrough selected, but the compatibility patch for 'Realms in Exile (LOTR)' is enabled in your Paradox Launcher playset.\n\nThis can cause issues. Please disable the 'Realms in Exile' patch before continuing.",
                                                     "Incorrect Compatibility Patch Enabled",
                                                     MessageBoxButtons.OK,
                                                     MessageBoxIcon.Warning);
                                 return;
                                 }
                             }
                         }
                         catch (Exception ex)
                         {
                             Program.Logger.Debug($"Error checking dlc_load.json: {ex.Message}. Proceeding without check.");
                         }
                     }
                     else
                     {
                         Program.Logger.Debug($"dlc_load.json not found at '{dlcLoadPath}'. Skipping playset check.");
                     }
                 }
            }


            // Cancel any previous monitoring operation
            _battleMonitoringCts?.Cancel();
            _battleMonitoringCts?.Dispose();
            _battleMonitoringCts = new CancellationTokenSource();
            CancellationToken token = _battleMonitoringCts.Token;

            if (BattleState.IsBattleInProgress())
            {
                var confirmResult = MessageBox.Show("Starting a new battle will discard your progress from the current one in TW:Attila. Are you sure you want to continue?",
                                                     "Confirm Start CK3",
                                                     MessageBoxButtons.YesNo,
                                                     MessageBoxIcon.Warning);
                if (confirmResult == DialogResult.No)
                {
                    _battleMonitoringCts.Cancel(); // Cancel the newly created CTS if user aborts
                    return;
                }
            }

            if (!_programmaticClick)
            {
                PlaySound(@".\data\sounds\sword-slash-with-metal-shield-impact-185433.wav");
            }
            _programmaticClick = false; // Always reset after check

            _myVariable = 1;
            ExecuteButton.Enabled = false;
            ExecuteButton.BackgroundImage = Properties.Resources.start_new_disabled;
            ProcessCommands.ResumeProcess();

            BattleResult.Player_Combat = null; // Reset the static combat data
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

            string tempSiegesFile = CrusaderWars.data.save_file.Writter.DataTEMPFilesPaths.Sieges_Path();
            if (System.IO.File.Exists(tempSiegesFile))
            {
                System.IO.File.Delete(tempSiegesFile);
                Program.Logger.Debug("Deleted stale temp Sieges.txt file.");
            }

            // UnitsCardsNames.RemoveFiles(); // Moved to ProcessBattle

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("ExecuteButton_Click loop cancelled.");
                    break;
                }

                Program.Logger.Debug("Starting main loop, waiting for CK3 battle.");
                this.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

                bool filesCleared = false;
                int maxRetries = 3;
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        DataSearch.ClearLogFile();
                        // DeclarationsFile.Erase(); // Moved to ProcessBattle
                        // BattleScript.EraseScript(); // Moved to ProcessBattle
                        // BattleResult.ClearAttilaLog(); // Moved to ProcessBattle
                        Program.Logger.Debug("CK3 log snippet file cleared.");
                        filesCleared = true;
                        break; // Exit retry loop on success
                    }
                    catch (IOException ex)
                    {
                        Program.Logger.Debug($"Attempt {i + 1} to clear log files failed due to file lock: {ex.Message}. Retrying in 500ms...");
                        if (i == maxRetries - 1)
                        {
                            Program.Logger.Debug("Final attempt to clear log files failed.");
                            MessageBox.Show($"Could not clear temporary battle files. Another process may be locking them. Please restart the application.\n\nError: {ex.Message}", "Crusader Conflicts: File Lock Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        }
                        else
                        {
                            Program.Logger.Debug($"Retrying file clear in 500ms...");
                            await Task.Delay(500); // Wait before retrying
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Debug($"Error clearing log files: {ex.Message}");
                        MessageBox.Show("An unexpected error occurred while clearing log files!", "Crusader Conflicts: Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                        break; // Exit retry loop on unexpected error
                    }
                }

                if (!filesCleared)
                {
                    // If files could not be cleared after retries, break from the main loop
                    infoLabel.Text = "Ready to start!";
                    ExecuteButton.Enabled = true;
                    this.Text = "Crusader Conflicts";
                    break;
                }

                try
                {
                    CreateAttilaShortcut();
                    Program.Logger.Debug("Attila shortcut created/verified.");
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error creating Attila shortcut: {ex.Message}");
                    MessageBox.Show("Error creating Attila shortcut!", "Crusader Conflicts: File Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Ready to start!";
                    ExecuteButton.Enabled = true;
                    this.Text = "Crusader Conflicts";
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
                    MessageBox.Show("Couldn't find 'ck3.exe'. Change the Crusader Kings 3 path. ", "Crusader Conflicts: Path Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    infoLabel.Text = "Ready to start!";
                    ExecuteButton.Enabled = true;
                    this.Text = "Crusader Conflicts";
                    break;
                }

                // BattleFile.ClearFile(); // Moved to ProcessBattle

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
                            await Task.Delay(5000); // Delay to allow CK3 to recognize the new save file
                            infoLabel.Text = "Waiting for CK3 battle... Battle complete! In CK3, use 'Continue' or load 'battle_results.ck3' save.";
                        }
                        else
                        {
                            infoLabel.Text = "Waiting for CK3 battle...";
                        }
                        // Paste the line here
                        ExecuteButton.Size = new Size(197, 115); 

                        Program.Logger.Debug("Waiting for CRUSADERCONFLICTS keyword in CK3 log...");
                        try
                        {
                            //Wait for CW keyword
                            while (!battleHasStarted)
                            {
                                if (token.IsCancellationRequested)
                                {
                                    Program.Logger.Debug("Waiting for CK3 battle loop cancelled.");
                                    break;
                                }

                                //Read each line
                                while (!reader.EndOfStream)
                                {
                                    string? line = reader.ReadLine();
                                    if (line == null) continue; // Ensure line is not null

                                    //If Battle Started
                                    if (line.Contains("CRUSADERWARS3") || line.Contains("CRUSADERCONFLICTS"))
                                    {
                                        Program.Logger.Debug("Battle keyword found in CK3 log. ");
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
                                    this.Text = "Crusader Conflicts";
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
                            MessageBox.Show("Error searching for battle. ", "Crusader Conflicts: Critical Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            infoLabel.Text = "Ready to start!";
                            ExecuteButton.Enabled = true;
                            this.Text = "Crusader Conflicts";
                            if (loadingScreen != null) CloseLoadingScreen();
                            break;
                        }

                        if (token.IsCancellationRequested)
                        {
                            Program.Logger.Debug("Skipping battle data reading due to cancellation.");
                            break;
                        }

                        try
                        {
                            UpdateLoadingScreenMessage("Getting data from CK3 save file...");
                            StartLoadingScreen();

                            infoLabel.Text = "Reading CK3 battle data...";
                            this.Text = "Crusader Conflicts (Reading CK3 battle data...)";
                            this.Hide();

                            logFile.Position = 0;
                            reader.DiscardBufferedData();
                            log = reader.ReadToEnd();
                            log = RemoveASCII(log);
                            BattleState.SaveLogSnippet(log);

                            if (battleHasStarted)
                            {
                                Program.Logger.Debug("Reading CK3 battle data from log.");
                                Data.Reset(); // ADDED as per plan
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
                            MessageBox.Show($"Error reading TW:Attila battle data: {ex.Message}", "Crusader Conflicts: Data Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            infoLabel.Text = "Waiting for CK3 battle...";
                            this.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

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

                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("Skipping save file processing due to cancellation.");
                    break;
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
                    DataSearch.FindSiegeCombatID();
                    if (!twbattle.BattleState.IsSiegeBattle)
                    {
                        BattleResult.GetPlayerCombatResult();
                        BattleResult.ReadPlayerCombat(CK3LogData.LeftSide.GetCommander().id);
                    }
                }
                catch(Exception ex)
                {
                    Program.Logger.Debug($"Error reading save file: {ex.Message}");
                    this.Show();
                    if (loadingScreen != null) CloseLoadingScreen();
                    MessageBox.Show($"Error reading the save file: {ex.Message}", "Crusader Conflicts: Save File Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
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

                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("Skipping army data processing due to cancellation.");
                    break;
                }

                try
                {
                    //1.0 Beta Debug
                    UpdateLoadingScreenMessage("Reading CK3 save file data...");
                    Program.Logger.Debug("Reading battle armies from CK3 save file.");
                    var armies = ArmiesReader.ReadBattleArmies();
                    attacker_armies = armies.attacker;
                    defender_armies = armies.defender;

                    // The previous siege garrison generation logic has been removed as ArmiesReader now correctly identifies siege defenders.

                    Program.Logger.Debug($"Found {attacker_armies.Count} attacker armies and {defender_armies.Count} defender armies.");
            
                    // Mark battle as started only if setup succeeded
                    BattleState.MarkBattleStarted();
                    UpdateUIForBattleState();
                }
                catch(Exception ex)
                {
                    string errorDetails = $"Error reading battle armies: {ex.Message}";
                    string? stackTrace = ex.StackTrace;
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

                    MessageBox.Show($"{errorMessage}\n\nTechnical Details: {ex.Message}", "Crusader Conflicts: Army Data Error",
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
                    this.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

                    //Data Clear
                    Data.Reset();

                    continue;
                }

                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("Skipping ProcessBattle due to cancellation.");
                    break;
                }

                if (!await ProcessBattle(token))
                {
                    break;
                }

                // Battle processed successfully. Loop will continue.
                // Manually reset some UI elements for the next iteration,
                // without touching infoLabel.
                ContinueBattleButton.Visible = false;
                ExecuteButton.Text = "";
                // The line `ExecuteButton.Size = new Size(197, 115);` was moved from here.
            }

            // Reset UI if the main loop is broken by a critical error or cancellation
            _myVariable = 0;
            ExecuteButton.Enabled = true;
            if (ExecuteButton.Enabled)
            {
                ExecuteButton.BackgroundImage = Properties.Resources.start_new;
            }
            UpdateUIForBattleState();
            this.Text = "Crusader Conflicts";
        }

        private async Task<bool> ProcessBattle(CancellationToken token, bool regenerateAndRestart = true)
        {
            UnitsFile.ResetProcessedArmies(); // Reset tracker for each battle processing attempt.
            var left_side = ArmiesReader.GetSideArmies("left", attacker_armies, defender_armies);
            var right_side = ArmiesReader.GetSideArmies("right", attacker_armies, defender_armies);

            if (left_side is null || !left_side.Any() || right_side is null || !right_side.Any())
            {
                Program.Logger.Debug("Could not determine battle sides or one side is empty. Aborting battle processing.");
                MessageBox.Show("Could not determine player and enemy sides for the battle, or one side has no armies. The battle cannot proceed.", "Crusader Conflicts: Battle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; // break
            }

            int left_side_total = left_side.Sum(army => army.GetTotalSoldiers());
            int right_side_total = right_side.Sum(army => army.GetTotalSoldiers());
            string left_side_combat_side = left_side[0].CombatSide;
            Program.Logger.Debug($"******************** BATTLE SIDES (Original CK3 Sizes) ********************");
            Program.Logger.Debug($"LEFT SIDE ({left_side_combat_side}) TOTAL SOLDIERS: {left_side_total}");
            Program.Logger.Debug($"RIGHT SIDE ({right_side[0].CombatSide}) TOTAL SOLDIERS: {right_side_total}");
            Program.Logger.Debug($"*************************************************************************");


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

                    BattleLog.Reset();
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

                        this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                            MessageBox.Show(this, sb.ToString(), "Crusader Conflicts: Unit Mapping Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    this.Show();
                    if (loadingScreen != null) CloseLoadingScreen();
                    MessageBox.Show($"Error creating the battle: {ex.Message}", "Crusader Conflicts: Data Error",
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
                    this.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

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
                        this.Show();
                        if (loadingScreen != null) CloseLoadingScreen();
                        ProcessCommands.ResumeProcess();
                        infoLabel.Text = "Waiting for CK3 battle...";
                        this.Text = "Crusader Conflicts (Waiting for CK3 battle...)";
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
                    this.Show();
                    if (loadingScreen != null) CloseLoadingScreen();
                    MessageBox.Show("Couldn't find 'Attila.exe'. Change the Total War Attila path. ", "Crusader Conflicts: Path Error",
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
                    this.Text = "Crusader Conflicts";
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

                if (loadingScreen != null) CloseLoadingScreen();
                this.Show();

            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error during cleanup before battle: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Crusader Conflicts: Application Error",
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
                this.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

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
            this.Text = "Crusader Conflicts (Waiting for TW:Attila battle to end...)";
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
                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("Battle monitoring cancelled by user action.");
                    return false; // Indicate cancellation
                }
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

                    infoLabel.Text = "Processing TW:Attila battle results...";
                    this.Text = "Crusader Conflicts (Processing results)";

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

                    //  EDIT SIEGES FILE
                    if (twbattle.BattleState.IsSiegeBattle)
                    {
                        Program.Logger.Debug("Editing Sieges.txt file...");
                        BattleResult.EditSiegesFile(path_log_attila, left_side[0].CombatSide, right_side[0].CombatSide, defender_armies);
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

                    this.Text = "Crusader Conflicts (Battle Complete)";
                    battleJustCompleted = true;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error retrieving TW:Attila battle results: {ex.Message}");
                MessageBox.Show($"Error retrieving TW:Attila battle results: {ex.Message}", "Crusader Conflicts: TW:Attila Battle Results Error",
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
                this.Text = "Crusader Conflicts (Waiting for CK3 battle...)";

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

            // Cancel any previous monitoring operation
            _battleMonitoringCts?.Cancel();
            _battleMonitoringCts?.Dispose();
            _battleMonitoringCts = new CancellationTokenSource();
            CancellationToken token = _battleMonitoringCts.Token;

            PlaySound(@".\data\sounds\sword-slash-with-metal-shield-impact-185444.wav");
            _myVariable = 1;
            ExecuteButton.Enabled = false;
            ContinueBattleButton.Enabled = false;
            ExecuteButton.BackgroundImage = Properties.Resources.start_new_disabled;

            // Update status label immediately
            infoLabel.Text = "Preparing TW:Attila battle...";
            this.Text = "Crusader Conflicts (Preparing TW:Attila battle...)";

            // Ensure Attila shortcut exists
            CreateAttilaShortcut();

            // Restore battle context from saved log snippet
            Program.Logger.Debug("Restoring battle context from log snippet...");
            string? logSnippet = BattleState.LoadLogSnippet(); // Changed to nullable string
            if (string.IsNullOrEmpty(logSnippet))
            {
                Program.Logger.Debug("Failed to load battle context. Log snippet is missing or empty.");
                MessageBox.Show("Could not continue the battle. The battle context file is missing.", "Crusader Conflicts: Continue Battle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _myVariable = 0;
                ExecuteButton.Enabled = true;
                ContinueBattleButton.Enabled = true;
                ExecuteButton.BackgroundImage = Properties.Resources.start_new;
                infoLabel.Text = "A battle is in progress!"; // Reset status on early exit
                this.Text = "Crusader Conflicts"; // Reset status on early exit
                return;
            }
            Data.Reset(); // ADDED as per plan
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

                DataSearch.FindSiegeCombatID();
                if (!twbattle.BattleState.IsSiegeBattle)
                {
                    BattleResult.GetPlayerCombatResult();
                    BattleResult.ReadPlayerCombat(CK3LogData.LeftSide.GetCommander().id);
                }
                var armies = ArmiesReader.ReadBattleArmies();
                attacker_armies = armies.attacker;
                defender_armies = armies.defender;
                Program.Logger.Debug($"Successfully re-loaded {attacker_armies.Count} attacker and {defender_armies.Count} defender armies.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to re-load army data: {ex.Message}");
                MessageBox.Show($"Could not continue the battle. Failed to load army data.\n\nError: {ex.Message}", "Crusader Conflicts: Continue Battle Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Reset UI state
                _myVariable = 0;
                ExecuteButton.Enabled = true;
                ContinueBattleButton.Enabled = true;
                ExecuteButton.BackgroundImage = Properties.Resources.start_new;
                infoLabel.Text = "A battle is in progress!"; // Reset status on early exit
                this.Text = "Crusader Conflicts"; // Reset status on early exit
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
                    infoLabel.Text = "A battle is in progress!"; // Reset status on early exit
                    this.Text = "Crusader Conflicts"; // Reset status on early exit
                    _battleMonitoringCts.Cancel(); // Cancel the newly created CTS if user aborts
                    return;
                }
            }

            _programmaticClick = true;
            if (await ProcessBattle(token, regenerateAndRestart))
            {
                // The battle finished successfully, start the main loop to wait for the next one.
                ExecuteButton.PerformClick();
            }
            else
            {
                // A critical error occurred (e.g., couldn't start Attila) or was cancelled. Reset UI.
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
        private string GetActivePlaythroughTag()
        {
            string um_file = @".\settings\UnitMappers.xml";
            if (File.Exists(um_file))
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(um_file);
                    var root = xmlDoc.DocumentElement;
                    if (root != null)
                    {
                        foreach (XmlNode node in root.ChildNodes)
                        {
                            if (node is XmlComment) continue;
                            if (node.InnerText == "True")
                            {
                                return node.Attributes?["name"]?.Value ?? "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error reading UnitMappers.xml for playthrough tag: {ex.Message}");
                }
            }
            return "";
        }
        private string GetFriendlyPlaythroughName(string tag)
        {
            switch (tag)
            {
                case "DefaultCK3":
                    return "Crusader Kings";
                case "TheFallenEagle":
                    return "The Fallen Eagle";
                case "RealmsInExile":
                    return "Realms in Exile (LOTR)";
                case "AGOT":
                    return "A Game of Thrones (AGOT)";
                default:
                    return "Selected"; // A safe default
            }
        }
        void SetPlaythrough()
        {
            Program.Logger.Debug("Setting playthrough...");
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@".\settings\UnitMappers.xml");
            string tagName = "";
            var root = xmlDoc.DocumentElement;
            if (root != null)
            {
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node is XmlComment) continue;
                    if (node.InnerText == "True")
                    {
                        tagName = node.Attributes?["name"]?.Value ?? "";
                        Program.Logger.Debug($"Playthrough tag found: {tagName}");
                        break;
                    }
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
                    Process.Start(new ProcessStartInfo(Properties.Settings.Default.VAR_ck3_path) { UseShellExecute = true });
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
                Process[] process_ck3 = Process.GetProcessesByName("ck3");
                if (process_ck3.Length == 0)
                {
                    Program.Logger.Debug("CK3 process not found. Starting CK3 with --continuelastsave.");
                    string ck3_path = Properties.Settings.Default.VAR_ck3_path;
                    Process.Start(new ProcessStartInfo(ck3_path, "--continuelastsave") { UseShellExecute = true });
                }
                else
                {
                    Program.Logger.Debug("CK3 process is already running. Cannot automatically load last save. Please continue manually in CK3.");
                }
            }

            public static void StartTotalWArAttilaProcess()
            {
                Program.Logger.Debug("Starting Total War: Attila process via shortcut...");
                Process.Start(new ProcessStartInfo(@".\CW.lnk") { UseShellExecute = true });
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
            string file = @".\settings\UnitMappers.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            var ck3ToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='DefaultCK3']")!.InnerText;
            var tfeToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='TheFallenEagle']")!.InnerText;
            var lotrToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='RealmsInExile']")!.InnerText;
            var agotToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='AGOT']")!.InnerText; // Added AGOT tab

            string playthrough = "";
            if (ck3ToggleStateStr == "True") playthrough = "Medieval";
            if (tfeToggleStateStr == "True") playthrough = "LateAntiquity";
            if (lotrToggleStateStr == "True") playthrough = "Lotr";
            if (agotToggleStateStr == "True") playthrough = "AGOT"; // Added AGOT tab

            Program.Logger.Debug($"Playthrough detected: {playthrough}. Setting background image.");
            switch (playthrough)
            {
                case "Medieval":
                    loadingScreen!.BackgroundImage = Properties.Resources.LS_medieval;
                    break;
                case "LateAntiquity":
                    loadingScreen!.BackgroundImage = Properties.Resources.LS_late_antiquity;
                    break;
                case "Lotr":
                    loadingScreen!.BackgroundImage = Properties.Resources.LS_lotr;
                    break;
                case "AGOT": // Added AGOT tab
                    loadingScreen!.BackgroundImage = Properties.Resources.LS_agot;
                    break;
                default:
                    loadingScreen!.BackgroundImage = Properties.Resources.LS_medieval;
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

        public void UpdateLoadingScreenMessage(string? message)
        {
            if (loadingScreen != null && loadingScreen.IsHandleCreated)
            {
                loadingScreen.BeginInvoke(new Action(() => loadingScreen.ChangeMessage(message ?? string.Empty)));
            }
        }

        public void UpdateLoadingScreenUnitMapperMessage(string? message)
        {
            if (loadingScreen != null && loadingScreen.IsHandleCreated)
            {
                loadingScreen.BeginInvoke(new Action(() => loadingScreen.ChangeUnitMapperMessage(message ?? string.Empty)));
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
            PlaySound(@".\data\sounds\metal-dagger-hit-185444.wav");
            
            Options optionsChild = new Options();
            optionsChild.ShowDialog();
            UpdatePlaythroughDisplay(); // Update display after settings are closed
        }


        private void HomePage_FormClosing(object sender, EventArgs e)
        {
            Program.Logger.Debug("HomePage form closing.");
            _preReleasePulseTimer?.Stop(); // Add this line
            _battleMonitoringCts?.Cancel(); // Cancel any active monitoring
            _battleMonitoringCts?.Dispose(); // Dispose the CTS
            ProcessCommands.ResumeProcess();
        }

        private void PlaySound(string soundFilePath)
        {
            try
            {
                if (File.Exists(soundFilePath))
                {
                    new SoundPlayer(soundFilePath).Play();
                }
                else
                {
                    Program.Logger.Debug($"Sound file not found: {soundFilePath}");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error playing sound {soundFilePath}: {ex.Message}");
            }
        }

        private void viewLogsLink_Click(object sender, EventArgs e)
        {
            PlaySound(@".\data\sounds\metal-dagger-hit-185444.wav");
            
            string logPath = Path.GetFullPath(@".\data\debug.log");
            if (System.IO.File.Exists(logPath))
            {
                // Open explorer and highlight debug.log
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select, \"{logPath}\"") { UseShellExecute = true });
            }
            else
            {
                // Fallback if file not found
                string folderPath = Path.GetFullPath(@".\data");
                if (Directory.Exists(folderPath))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", folderPath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Log folder not found! Please report this to developers.",
                                    "Crusader Conflicts: Log Location Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

        private void discordLink_Click(object sender, EventArgs e)
        {
            PlaySound(@".\data\sounds\metal-dagger-hit-185444.wav");
            Process.Start(new ProcessStartInfo("https://discord.gg/eFZTprHh3j") { UseShellExecute = true });
        }

        private void WebsiteBTN_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Website button clicked.");
            WebsiteBTN.BackgroundImage = Properties.Resources.website_btn_new_click;
            PlaySound(@"..\data\sounds\metal-dagger-hit-185444.wav");
            Process.Start(new ProcessStartInfo("https://crusader-conflicts-website.vercel.app/") { UseShellExecute = true });

        }

        private void SteamBTN_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Steam button clicked.");
            SteamBTN.BackgroundImage = Properties.Resources.steam_btn_new_click;
            PlaySound(@".\data\sounds\metal-dagger-hit-185444.wav");
            Process.Start(new ProcessStartInfo("https://mods.paradoxplaza.com/mods/120158/Any") { UseShellExecute = true });
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

        private async void labelVersion_Click(object sender, EventArgs e)
        {
            var version = _appVersion;
            if (!string.IsNullOrEmpty(version))
            {
                string? url = await _updater.GetReleaseUrlForVersion(version, false);
                if(!string.IsNullOrEmpty(url))
                {
                    Process.Start(new ProcessStartInfo(url!) { UseShellExecute = true });
                }
            }
        }

        private async void labelMappersVersion_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_umVersion))
            {
                string? url = await _updater.GetReleaseUrlForVersion(_umVersion, true);
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(new ProcessStartInfo(url!) { UseShellExecute = true });
                }
            }
        }

        private async void linkOptInPreReleases_Click(object sender, EventArgs e)
        {
            PlaySound(@".\data\sounds\metal-dagger-hit-185444.wav");

            // Toggle the setting
            bool currentState = ModOptions.GetOptInPreReleases();
            Options.SetOptInPreReleases(!currentState);

            // Update the button's appearance immediately
            UpdatePreReleaseLinkState();

            // If the user just opted IN, check for updates
            if (ModOptions.GetOptInPreReleases())
            {
                try
                {
                    linkOptInPreReleases.Enabled = false;
                    infoLabel.Text = "Checking for pre-release updates...";
                    Program.Logger.Debug("Triggering immediate update check due to pre-release opt-in.");
                    await _updater.CheckAppVersion();
                    await _updater.CheckUnitMappersVersion();
                    infoLabel.Text = "Update check complete.";
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error during immediate update check: {ex.Message}");
                    infoLabel.Text = "Error during update check.";
                }
                finally
                {
                    linkOptInPreReleases.Enabled = true;
                    await Task.Delay(2000);
                    infoLabel.Text = "Ready to Start!";
                }
            }
        }

        #region CK3 Mod Updater Logic

        private class ModUpdateInfo
        {
            public string Name { get; set; } = "";
            public string OldVersion { get; set; } = "0.0.0";
            public string NewVersion { get; set; } = "0.0.0";
            public string SourceModFile { get; set; } = "";
            public string SourceModDir { get; set; } = "";
            public string TargetModFile { get; set; } = "";
            public string TargetModDir { get; set; } = "";
            public string ModDirectoryName { get; set; } = "";
        }

        private (string version, string name, string pathDir) ParseModFile(string modFilePath)
        {
            if (!File.Exists(modFilePath))
            {
                return ("0.0.0", "", "");
            }

            string version = "0.0.0";
            string name = "";
            string pathDir = "";

            try
            {
                var lines = File.ReadAllLines(modFilePath);
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("version="))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"version\s*=\s*""([^""]*)""");
                        if (match.Success)
                        {
                            version = match.Groups[1].Value;
                        }
                    }
                    else if (line.Trim().StartsWith("name="))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"name\s*=\s*""([^""]*)""");
                        if (match.Success)
                        {
                            name = match.Groups[1].Value;
                        }
                    }
                    else if (line.Trim().StartsWith("path="))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"path\s*=\s*""([^""]*)""");
                        if (match.Success)
                        {
                            pathDir = Path.GetFileName(match.Groups[1].Value.TrimEnd('/', '\\'));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error parsing .mod file '{modFilePath}': {ex.Message}");
                return ("0.0.0", "", "");
            }

            return (version, name, pathDir);
        }

        private async Task<bool> CheckForCK3ModUpdatesAsync()
        {
            Program.Logger.Debug("Checking for CK3 mod updates...");
            string sourceModsDir = @".\ck3_mods";

            string ck3SaveGameDir = Properties.Settings.Default.VAR_dir_save;
            if (string.IsNullOrEmpty(ck3SaveGameDir) || !ck3SaveGameDir.EndsWith("save games"))
            {
                Program.Logger.Debug("CK3 save game directory is not configured correctly. Skipping CK3 mod update check.");
                return true; // Allow execution to continue if path is bad
            }
            string targetModsDir = ck3SaveGameDir.Replace("save games", "mod");

            if (!Directory.Exists(sourceModsDir))
            {
                Program.Logger.Debug("Source ck3_mods directory not found. Skipping check.");
                return true; // Allow execution to continue
            }
            if (!Directory.Exists(targetModsDir))
            {
                Program.Logger.Debug($"Target CK3 mod directory not found at '{targetModsDir}'. Skipping check.");
                return true; // Allow execution to continue
            }

            var modsToUpdate = new List<ModUpdateInfo>();

            try
            {
                foreach (var sourceModFile in Directory.GetFiles(sourceModsDir, "*.mod"))
                {
                    var (newVersion, modName, modDirName) = ParseModFile(sourceModFile);

                    if (string.IsNullOrEmpty(modName) || string.IsNullOrEmpty(modDirName))
                    {
                        Program.Logger.Debug($"Could not parse name or path from '{sourceModFile}'. Skipping.");
                        continue;
                    }

                    string targetModFile = Path.Combine(targetModsDir, Path.GetFileName(sourceModFile));
                    var (oldVersion, _, _) = ParseModFile(targetModFile);

                    if (_updater.IsNewerVersion(oldVersion, newVersion))
                    {
                        Program.Logger.Debug($"Found newer version for mod '{modName}'. Old: {oldVersion}, New: {newVersion}");
                        modsToUpdate.Add(new ModUpdateInfo
                        {
                            Name = modName,
                            OldVersion = oldVersion,
                            NewVersion = newVersion,
                            SourceModFile = sourceModFile,
                            TargetModFile = targetModFile,
                            ModDirectoryName = modDirName,
                            SourceModDir = Path.Combine(sourceModsDir, modDirName),
                            TargetModDir = Path.Combine(targetModsDir, modDirName)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"An error occurred while checking for mod updates: {ex.Message}");
                return true; // Allow execution to continue on unexpected error
            }

            if (modsToUpdate.Any())
            {
                var sb = new StringBuilder();
                sb.AppendLine("Updates are available for your Crusader Kings III mods managed by this app.");
                sb.AppendLine();
                sb.AppendLine("The following mods will be updated:");
                foreach (var mod in modsToUpdate)
                {
                    sb.AppendLine($"  • {mod.Name} (v{mod.OldVersion} -> v{mod.NewVersion})");
                }
                sb.AppendLine();
                sb.AppendLine($"Mods will be updated in the following directory:");
                sb.AppendLine(targetModsDir);
                sb.AppendLine();
                sb.AppendLine("Crusader Kings III must be closed to perform this update.");
                sb.AppendLine();
                sb.AppendLine("Do you want to update these mods now?");

                var result = MessageBox.Show(sb.ToString(), "CK3 Mod Updates Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    return await PerformModUpdateAsync(modsToUpdate, targetModsDir);
                }
                else
                {
                    Program.Logger.Debug("User declined CK3 mod updates.");
                    return true; // User declined, but allow execution to continue
                }
            }
            else
            {
                Program.Logger.Debug("All CK3 mods are up-to-date.");
                return true; // No updates needed, allow execution to continue
            }
        }

        private async Task<bool> PerformModUpdateAsync(List<ModUpdateInfo> modsToUpdate, string targetModsDir)
        {
            Program.Logger.Debug("Starting CK3 mod update process...");

            if (Process.GetProcessesByName("ck3").Length > 0)
            {
                var closeResult = MessageBox.Show("Crusader Kings III is currently running. It must be closed to update the mods.\n\nDo you want to close it now?", "Close Crusader Kings III?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (closeResult == DialogResult.Yes)
                {
                    Games.CloseCrusaderKingsProcess();
                    await Task.Delay(1000); // Give it a moment to close
                }
                else
                {
                    MessageBox.Show("Mod update cancelled because Crusader Kings III was not closed.", "Update Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Program.Logger.Debug("Mod update cancelled by user because CK3 is running.");
                    return false; // User cancelled
                }
            }

            int successCount = 0;
            foreach (var mod in modsToUpdate)
            {
                try
                {
                    // Delete old files
                    if (File.Exists(mod.TargetModFile))
                    {
                        Program.Logger.Debug($"Deleting old mod file: {mod.TargetModFile}");
                        File.Delete(mod.TargetModFile);
                    }
                    if (Directory.Exists(mod.TargetModDir))
                    {
                        Program.Logger.Debug($"Deleting old mod directory: {mod.TargetModDir}");
                        Directory.Delete(mod.TargetModDir, true);
                    }

                    // Copy new files
                    Program.Logger.Debug($"Copying new mod file from {mod.SourceModFile} to {mod.TargetModFile}");
                    File.Copy(mod.SourceModFile, mod.TargetModFile);

                    if (Directory.Exists(mod.SourceModDir))
                    {
                        Program.Logger.Debug($"Copying new mod directory from {mod.SourceModDir} to {mod.TargetModDir}");
                        CopyDirectory(mod.SourceModDir, mod.TargetModDir, true);
                    }
                    Program.Logger.Debug($"Successfully updated mod '{mod.Name}'.");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Failed to update mod '{mod.Name}'. Error: {ex.Message}");
                    MessageBox.Show($"Failed to update the mod '{mod.Name}'.\n\nPlease ensure you have the correct permissions for the CK3 mod directory and that no other programs (like antivirus) are blocking access.\n\nError: {ex.Message}", "Mod Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Stop on first error to prevent further issues
                    return false; // Update failed
                }
            }

            if (successCount == modsToUpdate.Count)
            {
                MessageBox.Show("The selected CK3 mods have been successfully updated.", "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Program.Logger.Debug("CK3 mod update process completed successfully.");
                return true; // All updates successful
            }
            return false; // Should not be reached if successCount != modsToUpdate.Count and no exception was thrown.
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                return;

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        #endregion
    }
}
