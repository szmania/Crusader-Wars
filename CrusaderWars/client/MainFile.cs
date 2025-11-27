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
using CrusaderWars.twbattle; // Added for BattleProcessor
using CrusaderWars.terrain;
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
        public bool battleJustCompleted = false; // Changed to public for BattleProcessor access
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
        private Label playthroughSubmodsTitleLabel = null!;
        private Label playthroughSubmodsListLabel = null!;

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
                Directory.CreateDirectory(@".\unit mappers\schemas");
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
            LaunchAutoFixerButton.Font = new Font("Yu Gothic UI", 12f, FontStyle.Bold);
            LaunchAutoFixerButton.Text = "Battle Tools";
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
            LaunchAutoFixerButton.FlatStyle = FlatStyle.Flat;
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

            // LaunchAutoFixerButton is now initialized in the designer, its click event is also there.
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
            int pulseValue = (_preReleasePulseStep < 10 ? _preReleasePulseStep * 10 : (20 - _preReleasePulseStep) * 10);
            int blueComponent = 150 - pulseValue;
            blueComponent = Math.Max(0, Math.Min(255, blueComponent)); // Clamp the value
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

        private async Task<bool> ValidateActiveUnitMapper()
        {
            string activePlaythroughTag = GetActivePlaythroughTag();
            if (string.IsNullOrEmpty(activePlaythroughTag))
            {
                MessageBox.Show("No Unit Mapper has been selected. Please select a playthrough in the Mod Settings.", "No Playthrough Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (activePlaythroughTag == "Custom")
            {
                string customMapperTag = client.ModOptions.GetSelectedCustomMapper();
                if (string.IsNullOrEmpty(customMapperTag))
                {
                    MessageBox.Show("The 'Custom' playthrough is active, but no custom unit mapper has been selected from the dropdown in Mod Settings.", "Custom Mapper Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            // Create and show a simple status form
            Form statusForm = new Form
            {
                Width = 300,
                Height = 100,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Verification in Progress",
                StartPosition = FormStartPosition.CenterParent,
                ControlBox = false
            };
            Label statusLabel = new Label
            {
                Text = "Validating Unit Mapper XML files...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            statusForm.Controls.Add(statusLabel);
            statusForm.Show(this);
            statusForm.Update();

            try
            {
                var allErrors = await Task.Run(() =>
                {
                    string unitMappersBaseDir = @".\unit mappers";
                    var unitMapperDirectories = new List<string>();
                    var errors = new List<string>();

                    if (activePlaythroughTag == "Custom")
                    {
                        string customMapperTag = client.ModOptions.GetSelectedCustomMapper();
                        if (Directory.Exists(unitMappersBaseDir))
                        {
                            unitMapperDirectories.AddRange(
                                Directory.GetDirectories(unitMappersBaseDir)
                                         .Where(dir =>
                                         {
                                             string tagFile = Path.Combine(dir, "tag.txt");
                                             return File.Exists(tagFile) && File.ReadAllText(tagFile).Trim().Equals(customMapperTag, StringComparison.OrdinalIgnoreCase);
                                         })
                            );
                        }
                    }
                    else
                    {
                        if (Directory.Exists(unitMappersBaseDir))
                        {
                            foreach (var dir in Directory.GetDirectories(unitMappersBaseDir))
                            {
                                string tagFile = Path.Combine(dir, "tag.txt");
                                if (File.Exists(tagFile) && File.ReadAllText(tagFile).Trim() == activePlaythroughTag)
                                {
                                    unitMapperDirectories.Add(dir);
                                }
                            }
                        }
                    }

                    if (unitMapperDirectories.Any())
                    {
                        foreach (var dir in unitMapperDirectories)
                        {
                            errors.AddRange(XmlValidator.ValidateUnitMapper(dir));
                        }
                    }
                    else
                    {
                        Program.Logger.Debug($"Unit mapper directory not found for playthrough '{activePlaythroughTag}'. Skipping validation.");
                    }
                    return errors;
                });

                if (allErrors.Any())
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("The selected unit mapper has validation errors and cannot be used.");
                    sb.AppendLine("Please fix the following issues or select a different playthrough:");
                    sb.AppendLine();

                    var groupedErrors = allErrors
                        .Select(e => {
                            var parts = e.Split(new[] { ", Error: " }, 2, StringSplitOptions.None);
                            var filePart = parts[0].Replace("File: ", "").Trim();
                            var messagePart = parts.Length > 1 ? parts[1] : filePart;
                            return new { FilePath = filePart, Message = messagePart };
                        })
                        .GroupBy(e => e.FilePath)
                        .OrderBy(g => g.Key);

                    foreach (var group in groupedErrors)
                    {
                        sb.AppendLine($"File: {group.Key}");
                        foreach (var error in group)
                        {
                            sb.AppendLine($"  - {error.Message}");
                        }
                        sb.AppendLine();
                    }

                    MessageBox.Show(sb.ToString(), "Unit Mapper Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            finally
            {
                statusForm.Close();
            }

            return true;
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

        public string path_editedSave = null!; // Changed to public for BattleProcessor access

        static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static string debugLog_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\console_history.txt";
        string saveGames_Path = documentsPath + "\\Paradox Interactive\\Crusader Kings III\\save games";
        private async void Form1_Load(object sender, EventArgs e)
        {
            Program.Logger.Debug("Form1_Load event triggered.");
            //Load Game Paths
            Options.ReadGamePaths();
            SubmodManager.LoadActiveSubmods();

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
            // infoLabel.Size = new Size(199, 31); // REMOVED THIS LINE
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
            LaunchAutoFixerButton.BackgroundImageLayout = ImageLayout.Zoom;
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
            playthroughSubmodsTitleLabel = new Label();
            playthroughSubmodsListLabel = new Label();

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
            playthroughNameLabel.AutoSize = true;
            playthroughNameLabel.MaximumSize = new Size(190, 0); // 0 for unlimited height

            // Submods Title Label
            playthroughSubmodsTitleLabel.Text = "Optional Sub-Mods:";
            playthroughSubmodsTitleLabel.Font = new Font("Microsoft Sans Serif", 9f, FontStyle.Bold | FontStyle.Underline);
            playthroughSubmodsTitleLabel.ForeColor = Color.WhiteSmoke;
            playthroughSubmodsTitleLabel.Location = new Point(playthroughPictureBox.Right + 10, playthroughNameLabel.Bottom + 5);
            playthroughSubmodsTitleLabel.AutoSize = true;
            playthroughSubmodsTitleLabel.Visible = false; // Initially hidden

            // Submods List Label
            playthroughSubmodsListLabel.Font = new Font("Microsoft Sans Serif", 8f, FontStyle.Italic);
            playthroughSubmodsListLabel.ForeColor = Color.WhiteSmoke;
            playthroughSubmodsListLabel.Location = new Point(playthroughPictureBox.Right + 10, playthroughSubmodsTitleLabel.Bottom + 2);
            playthroughSubmodsListLabel.AutoSize = true; // To control size and wrapping
            playthroughSubmodsListLabel.MaximumSize = new Size(190, 0); // Allow for multiple lines
            playthroughSubmodsListLabel.Visible = false; // Initially hidden

            // Add controls
            playthroughPanel.Controls.Add(playthroughPictureBox);
            playthroughPanel.Controls.Add(playthroughTitleLabel);
            playthroughPanel.Controls.Add(playthroughNameLabel);
            playthroughPanel.Controls.Add(playthroughSubmodsTitleLabel);
            playthroughPanel.Controls.Add(playthroughSubmodsListLabel);
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
                    string tagToFind = activePlaythroughTag;
                    if (activePlaythroughTag == "Custom")
                    {
                        tagToFind = ModOptions.GetSelectedCustomMapper();
                        Program.Logger.Debug($"Custom playthrough is active. Searching for folder with tag: '{tagToFind}'");
                    }

                    // Find the directory whose tag.txt matches the tagToFind
                    foreach (var dir in Directory.GetDirectories(unitMappersDir))
                    {
                        string tagFile = Path.Combine(dir, "tag.txt");
                        if (File.Exists(tagFile))
                        {
                            if (File.ReadAllText(tagFile).Trim().Equals(tagToFind, StringComparison.OrdinalIgnoreCase))
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
                    if (activePlaythroughTag == "Custom")
                    {
                        userFriendlyName = $"Custom ({ModOptions.GetSelectedCustomMapper()})";
                    }
                    else
                    {
                        userFriendlyName = System.Text.RegularExpressions.Regex.Replace(activePlaythroughTag, "(\\B[A-Z])", " $1");
                    }
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
                playthroughSubmodsTitleLabel.Visible = false;
                playthroughSubmodsListLabel.Visible = false;
                playthroughPanel.Size = new Size(280, 80);
            }
            else
            {
                // Handle sub-mods display
                var activeSubmodTags = mod_manager.SubmodManager.GetActiveSubmodsForPlaythrough(activePlaythroughTag);
                if (activeSubmodTags.Any())
                {
                    var allAvailableSubmods = unit_mapper.UnitMappers_BETA.GetUnitMappersModsCollectionFromTag(activePlaythroughTag).submods;
                    var activeSubmodScreenNames = allAvailableSubmods
                        .Where(s => activeSubmodTags.Contains(s.Tag))
                        .GroupBy(s => s.Tag) // Group by tag to handle duplicates
                        .Select(g => g.First()) // Select the first submod from each group
                        .Select(s => $"• {s.ScreenName}")
                        .ToList();

                    if (activeSubmodScreenNames.Any())
                    {
                        playthroughSubmodsTitleLabel.Location = new Point(playthroughPictureBox.Right + 10, playthroughNameLabel.Bottom + 5);
                        playthroughSubmodsTitleLabel.Visible = true;
                        playthroughSubmodsListLabel.Location = new Point(playthroughPictureBox.Right + 10, playthroughSubmodsTitleLabel.Bottom + 2);
                        playthroughSubmodsListLabel.Visible = true;
                        playthroughSubmodsListLabel.Text = string.Join("\n", activeSubmodScreenNames);

                        // Adjust panel size
                        playthroughPanel.Size = new Size(280, playthroughSubmodsListLabel.Bottom + 10);
                    }
                    else
                    {
                        playthroughSubmodsTitleLabel.Visible = false;
                        playthroughSubmodsListLabel.Visible = false;
                        playthroughPanel.Size = new Size(280, playthroughNameLabel.Bottom + 10);
                    }
                }
                else
                {
                    playthroughSubmodsTitleLabel.Visible = false;
                    playthroughSubmodsListLabel.Visible = false;
                    playthroughPanel.Size = new Size(280, playthroughNameLabel.Bottom + 10);
                }
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
                using (Form notificationForm = new Form())
                {
                    notificationForm.Text = "Crusader Conflicts: Latest Updates";
                    notificationForm.ClientSize = new Size(550, 500);
                    notificationForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    notificationForm.StartPosition = FormStartPosition.CenterParent;
                    notificationForm.MaximizeBox = false;
                    notificationForm.MinimizeBox = false;
                    notificationForm.ShowInTaskbar = false;
                    notificationForm.Icon = this.Icon;

                    // Panel for the button at the bottom
                    Panel bottomPanel = new Panel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 50,
                    };
                    notificationForm.Controls.Add(bottomPanel);

                    // Panel for the scrollable content, fills the rest of the space
                    Panel scrollPanel = new Panel
                    {
                        Dock = DockStyle.Fill,
                        AutoScroll = true,
                        Padding = new Padding(10)
                    };
                    notificationForm.Controls.Add(scrollPanel);

                    Label messageLabel = new Label
                    {
                        Text = "Custom Mapper Playthroughs!\n\n" +
                               "• New Custom Mapper playthroughs supported, including Mapper file validation, Custom mapper submod support and the ability to maintain mulitple custom mappers at one time. Custom mappers will not be overwritten during mapper updates.\n" +
                               "----------------------------------------------------------\n\n" +
                               "Manual Unit Replacer Tool!\n\n" +
                               "• New Manual Unit Replacer tool in the autofixer, giving players full control of army composition, after a crash.\n" +
                               "----------------------------------------------------------\n\n" +
                               "River, Strait, and Coastal Battles!\n\n" +
                               "• Armies crossing rivers, straits, or fighting in coastal provinces will now battle on unique, immersive maps that reflect the terrain.\n" +
                               "----------------------------------------------------------\n\n" +
                               "Prisoners of War & Slain in Battle!\n\n" +
                               "• Characters can now be slain or taken prisoner on the battlefield, with outcomes influenced by their prowess, traits, and the battle's result.\n",
                        Font = new Font("Microsoft Sans Serif", 10f),
                        AutoSize = true,
                        MaximumSize = new Size(notificationForm.ClientSize.Width - 40, 0),
                        Location = new Point(0, 0)
                    };
                    scrollPanel.Controls.Add(messageLabel);

                    LinkLabel appLink = new LinkLabel
                    {
                        Text = "View App Release Notes",
                        Font = new Font("Microsoft Sans Serif", 10f),
                        AutoSize = true,
                        Location = new Point(0, messageLabel.Bottom + 20)
                    };
                    appLink.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo("https://github.com/szmania/Crusader-Wars/releases/latest") { UseShellExecute = true });
                    scrollPanel.Controls.Add(appLink);

                    LinkLabel mappersLink = new LinkLabel
                    {
                        Text = "View Mappers Release Notes",
                        Font = new Font("Microsoft Sans Serif", 10f),
                        AutoSize = true,
                        Location = new Point(0, appLink.Bottom + 10)
                    };
                    mappersLink.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo("https://github.com/szmania/CC-Mappers/releases/latest") { UseShellExecute = true });
                    scrollPanel.Controls.Add(mappersLink);

                    Button okButton = new Button
                    {
                        Text = "OK",
                        DialogResult = DialogResult.OK,
                        Size = new Size(100, 30),
                        Location = new Point((bottomPanel.ClientSize.Width - 100) / 2, 10), // Centered in bottom panel
                        Anchor = AnchorStyles.None // Let the panel handle positioning
                    };
                    bottomPanel.Controls.Add(okButton);
                    notificationForm.AcceptButton = okButton;

                    notificationForm.ShowDialog(this);
                }

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
            LaunchAutoFixerButton.Visible = battleInProgress;

            if (battleInProgress)
            {
                ExecuteButton.Text = "Start CK3";
                ContinueBattleButton.Text = "Continue Battle";
                infoLabel.Text = "A battle is in progress!";

                // Resize buttons to fit side-by-by
                ExecuteButton.Size = new Size(197, 115);
                ContinueBattleButton.Size = new Size(197, 115);
                LaunchAutoFixerButton.Size = new Size(197, 50);
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
            if (await ValidateActiveUnitMapper() == false) { return; }
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
                             
                             // NEW: Check for incompatible Paradox Plaza version
                             if (enabledMods.Contains("pdx_120158.mod"))
                             {
                                 Program.Logger.Debug("Incompatible Paradox Plaza version of Crusader Conflicts mod found in enabled_mods.");
                                 MessageBox.Show("It appears you have the Paradox Plaza version of the 'Crusader Conflicts' mod enabled in your Paradox Launcher playset.\n\n" +
                                                 "This version is incompatible with the Crusader Conflicts application.\n\n" +
                                                 "Please unsubscribe from the Paradox Plaza version and enable the local 'crusader_conflicts.mod' provided with this application instead.",
                                                 "Incompatible Mod Version Detected",
                                                 MessageBoxButtons.OK,
                                                 MessageBoxIcon.Error);
                                 return; // Stop execution
                             }

                             // Check for base mod
                             if (!enabledMods.Contains("crusader_conflicts.mod") && !enabledMods.Contains("ugc_3612451961.mod"))
                             {
                                 Program.Logger.Debug("Crusader Conflicts mod (local or steam) not found in enabled_mods in dlc_load.json.");
                                 var result = MessageBox.Show("It appears the Crusader Conflicts CK3 mod is not enabled in your Paradox Launcher playset. Be sure to enable the mod and run the playset at least once in CK3 before starting Crusader Conflicts. Do you still want to continue?",
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
                                 if (!enabledMods.Contains(requiredPatch) && !enabledMods.Contains("ugc_3612526842.mod"))
                                 {
                                     Program.Logger.Debug($"Required AGOT compatibility patch (local or steam) not found in dlc_load.json.");
                                     var result = MessageBox.Show($"You have the '{playthroughName}' playthrough selected, but the required compatibility patch is not enabled in your Paradox Launcher playset.\n\nRequired patch: {requiredPatch} (or its Steam Workshop version)\n\nDo you still want to continue?",
                                                                  "Compatibility Patch Not Enabled",
                                                                  MessageBoxButtons.YesNo,
                                                                  MessageBoxIcon.Warning);
                                     if (result == DialogResult.No)
                                     {
                                         Program.Logger.Debug("User cancelled execution because AGOT compatibility patch is not enabled.");
                                         return; // Stop execution
                                     }
                                 }
                                 else
                                 {
                                     Program.Logger.Debug($"Required AGOT compatibility patch is enabled.");
                                 }
                             }
                             else if (activePlaythrough == "RealmsInExile")
                             {
                                 requiredPatch = "crusader_conflicts_realms_in_exile_compat_patch.mod";
                                 playthroughName = "Realms in Exile (LOTR)";
                                 if (!enabledMods.Contains(requiredPatch) && !enabledMods.Contains("ugc_3612526762.mod"))
                                 {
                                     Program.Logger.Debug($"Required Realms in Exile compatibility patch (local or steam) not found in dlc_load.json.");
                                     var result = MessageBox.Show($"You have the '{playthroughName}' playthrough selected, but the required compatibility patch is not enabled in your Paradox Launcher playset.\n\nRequired patch: {requiredPatch} (or its Steam Workshop version)\n\nDo you still want to continue?",
                                                                  "Compatibility Patch Not Enabled",
                                                                  MessageBoxButtons.YesNo,
                                                                  MessageBoxIcon.Warning);
                                     if (result == DialogResult.No)
                                     {
                                         Program.Logger.Debug("User cancelled execution because Realms in Exile compatibility patch is not enabled.");
                                         return; // Stop execution
                                     }
                                 }
                                 else
                                 {
                                     Program.Logger.Debug($"Required Realms in Exile compatibility patch is enabled.");
                                 }
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

                             // Check for recommended load order
                             var enabledModsList = new List<string>();
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
                                             enabledModsList.Add(Path.GetFileName(modPath));
                                         }
                                     }
                                 }
                             }

                             if (enabledModsList.Any())
                             {
                                 string mainModLocal = "crusader_conflicts.mod";
                                 string mainModSteam = "ugc_3612451961.mod";
                                 string agotPatchLocal = "crusader_conflicts_agot_compat_patch.mod";
                                 string agotPatchSteam = "ugc_3612526842.mod";
                                 string lotrPatchLocal = "crusader_conflicts_realms_in_exile_compat_patch.mod";
                                 string lotrPatchSteam = "ugc_3612526762.mod";

                                 int mainModIndex = enabledModsList.FindLastIndex(m => m.Equals(mainModLocal, StringComparison.OrdinalIgnoreCase) || m.Equals(mainModSteam, StringComparison.OrdinalIgnoreCase));

                                 bool loadOrderCorrect = true;
                                 string expectedOrderMessage = "";

                                 if (activePlaythrough == "AGOT")
                                 {
                                     int agotPatchIndex = enabledModsList.FindLastIndex(m => m.Equals(agotPatchLocal, StringComparison.OrdinalIgnoreCase) || m.Equals(agotPatchSteam, StringComparison.OrdinalIgnoreCase));
                                     if (agotPatchIndex != enabledModsList.Count - 1 || mainModIndex != agotPatchIndex - 1)
                                     {
                                         loadOrderCorrect = false;
                                         expectedOrderMessage = "For the AGOT playthrough, it is recommended to have the 'Crusader Conflicts' mod loaded just before the 'AGOT Compatibility Patch', and the patch should be last in your playset.";
                                     }
                                 }
                                 else if (activePlaythrough == "RealmsInExile")
                                 {
                                     int lotrPatchIndex = enabledModsList.FindLastIndex(m => m.Equals(lotrPatchLocal, StringComparison.OrdinalIgnoreCase) || m.Equals(lotrPatchSteam, StringComparison.OrdinalIgnoreCase));
                                     if (lotrPatchIndex != enabledModsList.Count - 1 || mainModIndex != lotrPatchIndex - 1)
                                     {
                                         loadOrderCorrect = false;
                                         expectedOrderMessage = "For the Realms in Exile (LOTR) playthrough, it is recommended to have the 'Crusader Conflicts' mod loaded just before the 'Realms in Exile Compatibility Patch', and the patch should be last in your playset.";
                                     }
                                 }
                                 else // Default case
                                 {
                                     if (mainModIndex != enabledModsList.Count - 1)
                                     {
                                         loadOrderCorrect = false;
                                         expectedOrderMessage = "For maximum compatibility, it is recommended to place the 'Crusader Conflicts' mod at the very end of your playset's load order.";
                                     }
                                 }

                                 if (!loadOrderCorrect)
                                 {
                                     Program.Logger.Debug("Incorrect mod load order detected.");
                                     var result = MessageBox.Show($"{expectedOrderMessage}\n\nYour current load order might cause issues.\n\nDo you still want to continue?",
                                                                  "Mod Load Order Warning",
                                                                  MessageBoxButtons.YesNo,
                                                                  MessageBoxIcon.Warning);
                                     if (result == DialogResult.No)
                                     {
                                         Program.Logger.Debug("User cancelled execution due to incorrect mod load order.");
                                         return; // Stop execution
                                     }
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

            UnitMappers_BETA.ClearProvinceMapCache();

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
                            SetPlaythrough(); // Re-initialize playthrough after reset

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

                    // Find the latest save file
                    var directory = new DirectoryInfo(saveGames_Path);
                    var lastSave = directory.GetFiles("*.ck3")
                        .OrderByDescending(f => f.LastWriteTime)
                        .FirstOrDefault();

                    if (lastSave == null)
                    {
                        throw new FileNotFoundException("No CK3 save file found in the save games directory.");
                    }

                    // NEW: Wait for the file to be accessible
                    if (await WaitForFileAccess(lastSave.FullName, token) == false)
                    {
                        throw new IOException("Timed out waiting for CK3 to finish writing the save file. The file may be locked or corrupted.");
                    }
                    
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
                catch (InvalidDataException ex) // SPECIFIC CATCH for zip errors
                {
                    Program.Logger.Debug($"Error reading save file (InvalidDataException): {ex.Message}");
                    this.Show();
                    if (loadingScreen != null) CloseLoadingScreen();
                    string errorMessage = "Error reading the save file: The file is not a valid save or is corrupted.\n\n" +
                                          "This often happens with Ironman or Cloud saves, which are not supported.\n\n" +
                                          "Troubleshooting:\n" +
                                          "1. Disable Ironman mode.\n" +
                                          "2. Use local saves instead of Steam Cloud.\n" +
                                          "3. Ensure the game has fully saved before a battle starts.\n" +
                                          "4. Verify game files in Steam.";
                    MessageBox.Show($"{errorMessage}\n\nTechnical Details: {ex.Message}", "Crusader Conflicts: Invalid Save File",
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
                    SetPlaythrough(); // Re-initialize playthrough after reset

                    continue;
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
                    SetPlaythrough(); // Re-initialize playthrough after reset

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
                            "❌ Unsupported CK3 mods\n" +
                            "❌ Crusader Conflicts CK3 mod is not at the bottom of your playset\n\n" +
                            "Troubleshooting:\n" +
                            "1. Disable Ironman mode\n" +
                            "2. Use local saves instead of Steam Cloud\n" +
                            "3. Start a new non-debug game\n" +
                            "4. Verify game files in Steam\n" +
                            "5. Try a different save file\n" +
                            "6. Place Crusader Conflicts at the bottom of your playset";

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
                    SetPlaythrough(); // Re-initialize playthrough after reset

                    continue;
                }

                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("Skipping ProcessBattle due to cancellation.");
                    break;
                }

                ExecuteButton.Enabled = false;
                ContinueBattleButton.Enabled = false;
                if (!await BattleProcessor.ProcessBattle(this, attacker_armies, defender_armies, token))
                {
                    break;
                }

                // Battle processed successfully. Loop will continue.
                // Manually reset some UI elements for the next iteration,
                // without touching infoLabel.
                await BattleProcessor.CleanupAfterBattle();
                battleJustCompleted = true;
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

        private async Task<bool> WaitForFileAccess(string filePath, CancellationToken token)
        {
            UpdateLoadingScreenMessage("Waiting for CK3 to finish saving...");
            Program.Logger.Debug($"Waiting for file access to: {filePath}");

            long lastSize = -1;
            int stableCounter = 0;
            const int checksForStability = 3; // e.g., 3 checks * 500ms = 1.5 seconds of stability

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalSeconds < 30)
            {
                if (token.IsCancellationRequested)
                {
                    Program.Logger.Debug("File access wait cancelled by user.");
                    return false;
                }

                try
                {
                    long currentSize = new FileInfo(filePath).Length;

                    if (lastSize == currentSize)
                    {
                        stableCounter++;
                        Program.Logger.Debug($"File size stable at {currentSize} bytes. Count: {stableCounter}/{checksForStability}");
                    }
                    else
                    {
                        stableCounter = 0;
                        Program.Logger.Debug($"File size changed to {currentSize} bytes. Resetting stability count.");
                    }

                    lastSize = currentSize;

                    if (stableCounter >= checksForStability)
                    {
                        // Now that size is stable, try to get exclusive access as a final check
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {
                            fs.Close();
                            Program.Logger.Debug("File size stable and exclusive access acquired. Proceeding.");
                            return true;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    Program.Logger.Debug("Save file not found yet. Waiting...");
                    stableCounter = 0; // Reset if file disappears
                }
                catch (IOException)
                {
                    // File is locked, wait and try again.
                    Program.Logger.Debug("Save file is locked by another process. Waiting...");
                    stableCounter = 0; // Reset stability count if we lose access
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"An unexpected error occurred while waiting for file access: {ex.Message}");
                    return false;
                }

                await Task.Delay(500, token); // Wait 500ms before the next check
            }

            Program.Logger.Debug("Timed out waiting for file access after 30 seconds.");
            return false; // Timeout reached
        }

        private async void ContinueBattleButton_Click(object sender, EventArgs e)
        {
            if (await ValidateActiveUnitMapper() == false) { return; }
            Program.Logger.Debug("Continue Battle button clicked.");
            ContinueBattleButton.Enabled = false;
            ExecuteButton.Enabled = false;
            LaunchAutoFixerButton.Enabled = false;

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
            await Task.Delay(50); // Allow UI to update

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
            Reader.ReadMetaData();
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
                                 "• No: Continue with the current session. (If the CC launcher closed unexpectedly)\n" +
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

            ExecuteButton.Enabled = false;
            ContinueBattleButton.Enabled = false;
            _programmaticClick = true;
            if (await BattleProcessor.ProcessBattle(this, attacker_armies, defender_armies, token, regenerateAndRestart))
            {
                await BattleProcessor.CleanupAfterBattle();
                battleJustCompleted = true;
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

        public void UpdateInfoLabel(string? message)
        {
            if (infoLabel != null && !infoLabel.IsDisposed && infoLabel.IsHandleCreated)
            {
                infoLabel.BeginInvoke(new Action(() => {
                    if (infoLabel != null && !infoLabel.IsDisposed)
                    {
                        infoLabel.Text = message ?? string.Empty;
                    }
                }));
            }
        }

        public void SetBattleButtonsEnabled(bool enabled)
        {
            if (ExecuteButton != null && !ExecuteButton.IsDisposed && ExecuteButton.IsHandleCreated)
            {
                ExecuteButton.BeginInvoke(new Action(() => {
                    ExecuteButton.Enabled = enabled;
                    if (ContinueBattleButton != null && !ContinueBattleButton.IsDisposed)
                    {
                        ContinueBattleButton.Enabled = enabled;
                    }
                }));
            }
        }

        /*---------------------------------------------
         * :::::::::::::PROCESS COMMANDS:::::::::::::::
         ---------------------------------------------*/

        public struct ProcessCommands // Changed to public for BattleProcessor access
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
                case "Custom":
                    return "Custom";
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
            var activeSubmods = mod_manager.SubmodManager.GetActiveSubmodsForPlaythrough(tagName);
            AttilaModManager.SetLoadingRequiredMods(UnitMappers_BETA.GetUnitMapperModFromTagAndTimePeriod(tagName, activeSubmods));
        }



        /*---------------------------------------------
         * :::::::::::GAMES INITIALIZATION:::::::::::::
         ---------------------------------------------*/
        public struct Games // Changed to public for BattleProcessor access
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

                await Task.Delay(1);
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
            Options.ReadOptionsFile();
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
            Process.Start(new ProcessStartInfo("https://steamcommunity.com/sharedfiles/filedetails/?id=3612451961") { UseShellExecute = true });
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

        private void LaunchAutoFixerButton_MouseEnter(object sender, EventArgs e)
        {
            LaunchAutoFixerButton.BackgroundImage = Properties.Resources.start_new_hover;
        }

        private void LaunchAutoFixerButton_MouseLeave(object sender, EventArgs e)
        {
            LaunchAutoFixerButton.BackgroundImage = Properties.Resources.start_new;
        }

        #region CK3 Mod Updater Logic

        private class ModUpdateInfo
        {
            public string Name { get; set; } = "";
            public string OldVersion { get; set; } = "0.0.0";
            public string NewVersion { get; set; } = "0.0.0";
            public string SourceModFile { get; set; } = "";
            public string SourceModDir { get; set; = "";
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

        private void LaunchAutoFixerButton_Click(object sender, EventArgs e)
        {
            string originalInfoText = infoLabel.Text;
            infoLabel.Text = "Loading Battle Tools...";

            // Load log snippet to determine if it's a siege battle to correctly populate strategies.
            string? logSnippet = BattleState.LoadLogSnippet();
            if (logSnippet == null)
            {
                MessageBox.Show(this, "Could not find saved battle information. The tools cannot be used until a battle has been started and saved.", "Battle Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                infoLabel.Text = originalInfoText;
                return;
            }
            DataSearch.Search(logSnippet); // This sets BattleState.IsSiegeBattle

            var allStrategies = Enum.GetValues(typeof(BattleProcessor.AutofixState.AutofixStrategy))
                                   .Cast<BattleProcessor.AutofixState.AutofixStrategy>()
                                   .ToList();

            if (BattleState.IsSiegeBattle)
            {
                allStrategies.Remove(BattleProcessor.AutofixState.AutofixStrategy.MapSize);
            }

            var (userResponse, chosenStrategy) = BattleProcessor.ShowPostCrashAutofixPrompt(this, allStrategies, isAfterCrash: false);

            if (userResponse == DialogResult.No || chosenStrategy == null)
            {
                Program.Logger.Debug("User cancelled tool selection.");
                infoLabel.Text = originalInfoText; // Reset label
                return;
            }

            switch (chosenStrategy)
            {
                case BattleProcessor.AutofixState.AutofixStrategy.ManualUnitReplacement:
                    LaunchUnitReplacerTool();
                    break;
                case BattleProcessor.AutofixState.AutofixStrategy.DeploymentZoneEditor:
                    LaunchDeploymentZoneEditor();
                    break;
                case BattleProcessor.AutofixState.AutofixStrategy.MapSize:
                    // For now, cycle through Big -> Huge -> default
                    string nextSize = BattleState.AutofixDeploymentSizeOverride == "Big" ? "Huge" : "Big";
                    BattleState.AutofixDeploymentSizeOverride = nextSize;
                    MessageBox.Show($"Autofix setting applied: Map size will be forced to '{nextSize}' for the next battle.", "Autofix Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case BattleProcessor.AutofixState.AutofixStrategy.Deployment:
                    BattleState.AutofixDeploymentRotationOverride = !(BattleState.AutofixDeploymentRotationOverride ?? false);
                    string rotationState = BattleState.AutofixDeploymentRotationOverride == true ? "enabled" : "disabled";
                    MessageBox.Show($"Autofix setting applied: Deployment rotation is now {rotationState} for the next battle.", "Autofix Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case BattleProcessor.AutofixState.AutofixStrategy.MapVariant:
                    BattleState.AutofixMapVariantOffset++;
                    MessageBox.Show($"Autofix setting applied: Map variant offset will be increased by 1 for the next battle (current offset: {BattleState.AutofixMapVariantOffset}).", "Autofix Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case BattleProcessor.AutofixState.AutofixStrategy.Units:
                    MessageBox.Show("The 'Change Units' strategy is an automatic process that runs after a crash and cannot be configured beforehand. Please use the 'Unit Replacer Tool' for manual changes.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
            infoLabel.Text = originalInfoText; // Reset label after tool is used or cancelled
        }

        private void LaunchUnitReplacerTool()
        {
            try
            {
                Program.Logger.Debug("--- Manual AutoFixer Launched from UI ---");
                Options.ReadOptionsFile();

                // Load log snippet to restore context before reading armies
                string? logSnippet = BattleState.LoadLogSnippet();
                if (logSnippet == null)
                {
                    MessageBox.Show("Could not find the saved battle information (log snippet). The AutoFixer cannot run without it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                DataSearch.Search(logSnippet);

                // Manually determine and set the active playthrough for the autofixer context
                string? activePlaythroughTag = GetActivePlaythroughTag();
                if (!string.IsNullOrEmpty(activePlaythroughTag))
                {
                    if (ModOptions.optionsValuesCollection.ContainsKey("Playthrough"))
                    {
                        ModOptions.optionsValuesCollection["Playthrough"] = activePlaythroughTag;
                    }
                    else
                    {
                        ModOptions.optionsValuesCollection.Add("Playthrough", activePlaythroughTag);
                    }
                    Program.Logger.Debug($"Autofixer context: Active playthrough set to '{activePlaythroughTag}'.");
                }

                // Load the unit mapper before reading armies
                string? selectedPlaythrough = ModOptions.GetSelectedPlaythrough();
                if (string.IsNullOrEmpty(selectedPlaythrough))
                {
                    MessageBox.Show("No playthrough is selected. The AutoFixer cannot run without knowing which unit mapper to use.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UnitMappers_BETA.ClearFactionCache(); // Clear any old data
                var activeSubmods = SubmodManager.GetActiveSubmodsForPlaythrough(selectedPlaythrough);
                UnitMappers_BETA.GetUnitMapperModFromTagAndTimePeriod(selectedPlaythrough, activeSubmods);

                if (string.IsNullOrEmpty(UnitMappers_BETA.GetLoadedUnitMapperName()))
                {
                    MessageBox.Show($"Could not load the unit mapper for the selected playthrough '{selectedPlaythrough}'. It might not be compatible with the current game year.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Program.Logger.Debug($"LaunchAutoFixer: Loaded unit mapper '{UnitMappers_BETA.GetLoadedUnitMapperName()}' for playthrough '{selectedPlaythrough}'.");


                // 1. Read battle armies
                var (attackerArmies, defenderArmies) = ArmiesReader.ReadBattleArmies();
                if (attackerArmies == null || defenderArmies == null || !attackerArmies.Any() || !defenderArmies.Any())
                {
                    MessageBox.Show("Could not read army data. Ensure a battle is properly saved and ready to continue.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                DataSearch.FindSiegeCombatID();
                Program.Logger.Debug($"LaunchAutoFixer: Read {attackerArmies.Count} attacker and {defenderArmies.Count} defender armies.");


                // 2. Set army sides and collect data
                BattleFile.SetArmiesSides(attackerArmies, defenderArmies);
                Program.Logger.Debug("LaunchAutoFixer: Army sides set.");
                var allArmies = attackerArmies.Concat(defenderArmies).ToList();
                Program.Logger.Debug($"LaunchAutoFixer: Collected {allArmies.Count} total armies.");

                var currentUnits = allArmies.Where(a => a.Units != null).SelectMany(a => a.Units)
                                            .Where(u => u != null && !string.IsNullOrEmpty(u.GetAttilaUnitKey()) && u.GetAttilaUnitKey() != UnitMappers_BETA.NOT_FOUND_KEY)
                                            .ToList();
                var allAvailableUnits = UnitMappers_BETA.GetAllAvailableUnits();
                var unitScreenNames = UnitsCardsNames.GetUnitScreenNames(UnitMappers_BETA.GetLoadedUnitMapperName() ?? "");

                if (unitScreenNames is null)
                {
                    Program.Logger.Debug("ERROR: unitScreenNames is null, cannot launch UnitReplacerForm.");
                    MessageBox.Show(this, "Could not load unit names required for the manual replacer. The process cannot continue.", "Crusader Conflicts: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (allAvailableUnits is null)
                {
                    Program.Logger.Debug("ERROR: allAvailableUnits is null, cannot launch UnitReplacerForm.");
                    MessageBox.Show(this, "Could not load the list of available units. The process cannot continue.", "Crusader Conflicts: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Filter units to only those that have a screen name to prevent crashes inside the form.
                var availableUnits = allAvailableUnits.Where(u => u != null && !string.IsNullOrEmpty(u.AttilaUnitKey) && unitScreenNames.ContainsKey(u.AttilaUnitKey)).ToList();
                Program.Logger.Debug($"LaunchAutoFixer: Collected {currentUnits.Count} current units with valid keys.");
                Program.Logger.Debug($"LaunchAutoFixer: Collected {allAvailableUnits.Count} total available units, filtered down to {availableUnits.Count} with screen names.");
                Program.Logger.Debug($"LaunchAutoFixer: Collected {unitScreenNames.Count} unit screen names.");


                // 3. Show form
                Program.Logger.Debug("LaunchAutoFixer: Creating UnitReplacerForm...");
                using (var replacerForm = new client.UnitReplacerForm(currentUnits, availableUnits, BattleState.ManualUnitReplacements, unitScreenNames))
                {
                    Program.Logger.Debug("LaunchAutoFixer: UnitReplacerForm created. Showing dialog...");
                    if (replacerForm.ShowDialog(this) == DialogResult.OK)
                    {
                        // 4. Process results
                        var replacements = replacerForm.Replacements;
                        if (replacements.Any())
                        {
                            Program.Logger.Debug($"Applying {replacements.Count} manual unit replacements from UI.");
                            BattleState.ManualUnitReplacements.Clear(); // Clear previous manual fixes

                            foreach (var replacement in replacements)
                            {
                                BattleState.ManualUnitReplacements[replacement.Key] = replacement.Value;
                                Program.Logger.Debug($"  - Storing replacement for '{replacement.Key.originalKey}' with '{replacement.Value.replacementKey}' for {(replacement.Key.isPlayerAlliance ? "Player" : "Enemy")}");
                            }
                            MessageBox.Show("Unit replacements have been saved. They will be applied when you click 'Continue Battle'.", "Replacements Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            BattleState.ManualUnitReplacements.Clear();
                            Program.Logger.Debug("Manual unit replacement window was closed with OK, but no replacements were made. Clearing any existing replacements.");
                        }
                    }
                    else
                    {
                        Program.Logger.Debug("Manual unit replacement was cancelled.");
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error in LaunchUnitReplacerTool: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"An unexpected error occurred while launching the Unit Replacer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchDeploymentZoneEditor()
        {
            try
            {
                Program.Logger.Debug("--- Manual Deployment Zone Editor Launched from UI ---");
                Options.ReadOptionsFile();

                // Load log snippet to restore context
                string? logSnippet = BattleState.LoadLogSnippet();
                if (logSnippet == null)
                {
                    MessageBox.Show("Could not find the saved battle information (log snippet). The tool cannot run without it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                DataSearch.Search(logSnippet);

                // Set playthrough
                string? activePlaythroughTag = GetActivePlaythroughTag();
                if (!string.IsNullOrEmpty(activePlaythroughTag))
                {
                    if (ModOptions.optionsValuesCollection.ContainsKey("Playthrough"))
                    {
                        ModOptions.optionsValuesCollection["Playthrough"] = activePlaythroughTag;
                    }
                    else
                    {
                        ModOptions.optionsValuesCollection.Add("Playthrough", activePlaythroughTag);
                    }
                }

                // Load the unit mapper before reading armies
                string? selectedPlaythrough = ModOptions.GetSelectedPlaythrough();
                if (string.IsNullOrEmpty(selectedPlaythrough))
                {
                    MessageBox.Show("No playthrough is selected. The tool cannot run without knowing which unit mapper to use.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UnitMappers_BETA.ClearFactionCache(); // Clear any old data
                var activeSubmods = SubmodManager.GetActiveSubmodsForPlaythrough(selectedPlaythrough);
                UnitMappers_BETA.GetUnitMapperModFromTagAndTimePeriod(selectedPlaythrough, activeSubmods);

                if (string.IsNullOrEmpty(UnitMappers_BETA.GetLoadedUnitMapperName()))
                {
                    MessageBox.Show($"Could not load the unit mapper for the selected playthrough '{selectedPlaythrough}'. It might not be compatible with the current game year.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Program.Logger.Debug($"LaunchDeploymentZoneEditor: Loaded unit mapper '{UnitMappers_BETA.GetLoadedUnitMapperName()}' for playthrough '{selectedPlaythrough}'.");


                // Read armies to get total soldier count for map size calculation
                var (attackerArmies, defenderArmies) = ArmiesReader.ReadBattleArmies();
                if (attackerArmies == null || defenderArmies == null || !attackerArmies.Any() || !defenderArmies.Any())
                {
                    MessageBox.Show("Could not read army data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                DataSearch.FindSiegeCombatID();
                BattleResult.ReadCombatBlockByProvinceID(); // Read province name from combat block
                int total_soldiers = attackerArmies.Sum(a => a.GetTotalSoldiers()) + defenderArmies.Sum(a => a.GetTotalSoldiers());
                string option_map_size = ModOptions.DeploymentsZones();

                // Determine which side is the player
                BattleFile.SetArmiesSides(attackerArmies, defenderArmies);
                bool isAttackerPlayer = attackerArmies.First().IsPlayer();

                // Generate initial deployment areas
                string attacker_direction = BattleState.IsSiegeBattle ? (BattleState.OriginalSiegeAttackerDirection ?? "N") : "N";
                string defender_direction = "S";

                DeploymentArea attackerArea = new DeploymentArea(attacker_direction, option_map_size, total_soldiers);
                DeploymentArea defenderArea = new DeploymentArea(defender_direction, option_map_size, total_soldiers, BattleState.IsSiegeBattle);
                float map_dimension = float.Parse(ModOptions.SetMapSize(total_soldiers, BattleState.IsSiegeBattle), System.Globalization.CultureInfo.InvariantCulture);

                // Get Battle Details
                if (!BattleState.IsSiegeBattle)
                {
                    TerrainGenerator.CheckForSpecialCrossingBattle(attackerArmies, defenderArmies);
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


                using (var toolForm = new client.DeploymentZoneToolForm(attackerArea, defenderArea, map_dimension, isAttackerPlayer, BattleState.IsSiegeBattle, battleDate, battleType, provinceName, mapX, mapY))
                {
                    if (toolForm.ShowDialog(this) == DialogResult.OK)
                    {
                        var attackerValues = toolForm.GetAttackerValues();
                        var defenderValues = toolForm.GetDefenderValues();

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

                        MessageBox.Show("Deployment zones have been saved. They will be applied when you click 'Continue Battle'.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error in LaunchDeploymentZoneEditor: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
    }
}
