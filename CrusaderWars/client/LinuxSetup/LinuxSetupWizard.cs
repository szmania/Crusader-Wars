using CrusaderWars.client.LinuxSetup.Services;
using CrusaderWars.client.LinuxSetup.Steps;
using CrusaderWars.client.LinuxSetup.Services;
using CrusaderWars.client.LinuxSetup.Steps;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace CrusaderWars.client.LinuxSetup
{
    public partial class LinuxSetupWizard : Form
    {
        private readonly ILinuxEnvironmentDetector _linuxEnvDetector;
        private readonly IWineManager _wineManager;
        private readonly ISteamManager _steamManager;
        private readonly ISymlinkManager _symlinkManager;
        private readonly IShortcutManager _shortcutManager;
        private DetectionStepControl _detectionStep;
        private WinePrefixStepControl _winePrefixStep;
        private DotNetInstallStepControl _dotNetInstallStep;
        private ModSymlinkStepControl _modSymlinkStep;
        private ShortcutCreationStepControl _shortcutCreationStep;
        private SteamConfigStepControl _steamConfigStep;
        private CompletionStepControl _completionStep;

        public LinuxSetupWizard()
        {
            InitializeComponent();

            _linuxEnvDetector = new LinuxEnvironmentDetector();
            _wineManager = new WineManager(_linuxEnvDetector);
            _steamManager = new SteamManager(_linuxEnvDetector);
            _symlinkManager = new SymlinkManager();
            _shortcutManager = new ShortcutManager(_wineManager, _linuxEnvDetector);

            _detectionStep = new DetectionStepControl();
            pnlSteps.Controls.Add(_detectionStep);
            _detectionStep.Dock = DockStyle.Fill;

            _winePrefixStep = new WinePrefixStepControl();
            pnlSteps.Controls.Add(_winePrefixStep);
            _winePrefixStep.Dock = DockStyle.Fill;

            _dotNetInstallStep = new DotNetInstallStepControl();
            pnlSteps.Controls.Add(_dotNetInstallStep);
            _dotNetInstallStep.Dock = DockStyle.Fill;

            _modSymlinkStep = new ModSymlinkStepControl();
            pnlSteps.Controls.Add(_modSymlinkStep);
            _modSymlinkStep.Dock = DockStyle.Fill;

            _shortcutCreationStep = new ShortcutCreationStepControl();
            pnlSteps.Controls.Add(_shortcutCreationStep);
            _shortcutCreationStep.Dock = DockStyle.Fill;

            _steamConfigStep = new SteamConfigStepControl();
            pnlSteps.Controls.Add(_steamConfigStep);
            _steamConfigStep.Dock = DockStyle.Fill;

            _completionStep = new CompletionStepControl();
            pnlSteps.Controls.Add(_completionStep);
            _completionStep.Dock = DockStyle.Fill;
        }

        private void LinuxSetupWizard_Load(object sender, EventArgs e)
        {
            _ = StartWizard();
        }

        private async Task StartWizard()
        {
            lblStatus.Text = "Starting setup wizard...";
            _detectionStep.Visible = true;
            _winePrefixStep.Visible = false;
            _dotNetInstallStep.Visible = false;
            _modSymlinkStep.Visible = false;
            _shortcutCreationStep.Visible = false;
            _steamConfigStep.Visible = false;
            _completionStep.Visible = false;

            await Task.Delay(1000);

            bool detectionSuccess = await RunDetectionStep();
            if (!detectionSuccess)
            {
                btnNext.Enabled = false;
                btnBack.Enabled = false;
                return;
            }

            bool winePrefixSuccess = await RunWinePrefixStep();
            if (!winePrefixSuccess)
            {
                btnNext.Enabled = false;
                btnBack.Enabled = false;
                return;
            }

            await RunDotNetInstallStep();

            await RunModSymlinkStep();

            await RunShortcutCreationStep();

            await RunSteamConfigStep();

            RunCompletionStep();
        }

        private async Task<bool> RunDetectionStep()
        {
            lblStatus.Text = "Step 1: Detecting environment...";
            progressBar.Value = 5;

            // Is Linux?
            bool isLinux = _linuxEnvDetector.IsRunningOnLinux();
            _detectionStep.SetDetectionResult("Linux", isLinux ? "Detected" : "Not Detected", isLinux);
            await Task.Delay(200);
            if (!isLinux)
            {
                lblStatus.Text = "Error: Not running on Linux. This wizard is for Linux/Proton users.";
                return false;
            }
            progressBar.Value = 10;

            // Wine version
            string? wineVersion = _linuxEnvDetector.GetWineVersion() ?? "Not Found";
            bool wineFound = wineVersion != "Not Found";
            _detectionStep.SetDetectionResult("Wine", wineVersion, wineFound);
            await Task.Delay(200);
            progressBar.Value = 15;

            // Desktop Env
            var de = _linuxEnvDetector.GetDesktopEnvironment();
            _detectionStep.SetDetectionResult("Desktop", de.ToString(), de != Models.DesktopEnvironment.Unknown);
            await Task.Delay(200);
            progressBar.Value = 20;

            // Steam Path
            string? steamPath = _steamManager.GetSteamPath() ?? "Not Found";
            bool steamFound = steamPath != "Not Found";
            _detectionStep.SetDetectionResult("Steam", steamPath, steamFound);
            await Task.Delay(200);
            progressBar.Value = 25;

            // Attila Path
            string? attilaPath = _steamManager.GetAttilaPath() ?? "Not Found";
            bool attilaFound = attilaPath != "Not Found";
            _detectionStep.SetDetectionResult("Attila", attilaPath, attilaFound);
            progressBar.Value = 30;

            lblStatus.Text = "Environment detection complete.";

            if (!wineFound || !steamFound || !attilaFound)
            {
                lblStatus.Text = "Error: Some required components were not found. Cannot proceed.";
                return false;
            }

            return true;
        }

        private async Task<bool> RunWinePrefixStep()
        {
            lblStatus.Text = "Step 2: Creating Wine Prefix...";
            _detectionStep.Visible = false;
            _winePrefixStep.Visible = true;
            progressBar.Value = 35;

            _winePrefixStep.SetStatus("Creating Wine prefix at ~/.crusader-conflicts-net-pfx...", true);
            bool success = await _wineManager.CreatePrefix("~/.crusader-conflicts-net-pfx");

            if (success)
            {
                _winePrefixStep.SetStatus("Wine prefix created successfully.", true);
                progressBar.Value = 40;
                lblStatus.Text = "Wine prefix step complete.";
                return true;
            }
            else
            {
                _winePrefixStep.SetStatus("Failed to create Wine prefix. See logs for details.", false);
                lblStatus.Text = "Error: Could not create Wine prefix.";
                return false;
            }
        }

        private async Task RunDotNetInstallStep()
        {
            lblStatus.Text = "Step 3: Installing .NET Framework...";
            _winePrefixStep.Visible = false;
            _dotNetInstallStep.Visible = true;
            progressBar.Value = 45;

            var progress = new Progress<string>(status =>
            {
                _dotNetInstallStep.UpdateStatus(status);
            });

            bool success = await _wineManager.InstallDotNet472("~/.crusader-conflicts-net-pfx", progress);

            if (success)
            {
                _dotNetInstallStep.UpdateStatus(".NET 4.7.2 installed successfully.");
                _dotNetInstallStep.SetSuccess();
                progressBar.Value = 60;
                lblStatus.Text = ".NET installation complete.";
            }
            else
            {
                _dotNetInstallStep.UpdateStatus("Failed to install .NET 4.7.2. See logs for details.");
                _dotNetInstallStep.SetError();
                lblStatus.Text = "Error: .NET installation failed.";
                btnNext.Enabled = false;
            }
        }


        private async Task RunModSymlinkStep()
        {
            lblStatus.Text = "Step 4: Creating Mod Symlinks...";
            _dotNetInstallStep.Visible = false;
            _modSymlinkStep.Visible = true;
            progressBar.Value = 65;

            string? workshopPath = _steamManager.GetWorkshopModsPath();
            string? attilaDataPath = _steamManager.GetAttilaPath() != null ? System.IO.Path.Combine(_steamManager.GetAttilaPath(), "data") : null;

            if (string.IsNullOrEmpty(workshopPath) || string.IsNullOrEmpty(attilaDataPath))
            {
                _modSymlinkStep.SetStatus("Could not find Attila workshop or data paths.", false);
                lblStatus.Text = "Error: Mod path detection failed.";
                btnNext.Enabled = false;
                return;
            }

            _modSymlinkStep.SetStatus($"Creating symlinks from {workshopPath} to {attilaDataPath}...", true);
            int symlinksCreated = await _symlinkManager.CreateModSymlinks(workshopPath, attilaDataPath);

            _modSymlinkStep.SetStatus($"Created {symlinksCreated} mod symlinks.", true);
            progressBar.Value = 75;
            lblStatus.Text = "Mod symlinking complete.";
        }

        private async Task RunShortcutCreationStep()
        {
            lblStatus.Text = "Step 5: Creating Launcher Shortcut...";
            _modSymlinkStep.Visible = false;
            _shortcutCreationStep.Visible = true;
            progressBar.Value = 80;

            string? attilaPath = _steamManager.GetAttilaPath();
            if (string.IsNullOrEmpty(attilaPath))
            {
                _shortcutCreationStep.SetStatus("Attila path not found.", false);
                lblStatus.Text = "Error: Cannot create shortcut without Attila path.";
                btnNext.Enabled = false;
                return;
            }

            string crusaderConflictsDataPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "attila");
            System.IO.Directory.CreateDirectory(crusaderConflictsDataPath);
            string shortcutOutputPath = System.IO.Path.Combine(crusaderConflictsDataPath, "Attila CW.lnk");

            _shortcutCreationStep.SetStatus("Creating launcher scripts and shortcut...", true);
            bool success = await _shortcutManager.CreateAttilaLauncherShortcut(attilaPath, shortcutOutputPath);

            if (success)
            {
                _shortcutCreationStep.SetStatus("Launcher shortcut created successfully.", true);
                progressBar.Value = 90;
                lblStatus.Text = "Shortcut creation complete.";
            }
            else
            {
                _shortcutCreationStep.SetStatus("Failed to create launcher shortcut.", false);
                lblStatus.Text = "Error: Shortcut creation failed.";
                btnNext.Enabled = false;
            }
        }

        private async Task RunSteamConfigStep()
        {
            lblStatus.Text = "Step 6: Steam Configuration...";
            _shortcutCreationStep.Visible = false;
            _steamConfigStep.Visible = true;
            progressBar.Value = 95;

            _steamConfigStep.SetStatus("Please set the following launch options for Total War: Attila in Steam under Properties -> General -> Launch Options:\n\n%command% used_mods_cw.txt\n\nThis allows Crusader Conflicts to manage which mods are active.");
            await _steamManager.SetLaunchOptions("325610", "%command% used_mods_cw.txt");
            
            lblStatus.Text = "Steam configuration instructions provided.";
        }

        private void RunCompletionStep()
        {
            lblStatus.Text = "Setup Complete!";
            _steamConfigStep.Visible = false;
            _completionStep.Visible = true;
            progressBar.Value = 100;

            _completionStep.SetMessage("Linux setup is complete! You can now close this wizard.");
            btnNext.Text = "Finish";
            btnBack.Enabled = false;
            btnNext.Click += FinishButton_Click;
        }

        private void FinishButton_Click(object sender, EventArgs e)
        {
            SetLinuxSetupCompleted(true);
            this.Close();
        }

        private void SetLinuxSetupCompleted(bool completed)
        {
            try
            {
                string file = @".\settings\Options.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                var node = xmlDoc.SelectSingleNode("//Option [@name='LinuxSetupCompleted']");
                if (node != null)
                {
                    node.InnerText = completed.ToString();
                }
                else
                {
                    XmlElement newOption = xmlDoc.CreateElement("Option");
                    newOption.SetAttribute("name", "LinuxSetupCompleted");
                    newOption.InnerText = completed.ToString();
                    xmlDoc.DocumentElement?.AppendChild(newOption);
                }
                xmlDoc.Save(file);

                if(ModOptions.optionsValuesCollection.ContainsKey("LinuxSetupCompleted"))
                {
                    ModOptions.optionsValuesCollection["LinuxSetupCompleted"] = completed.ToString();
                } 
                else
                {
                    ModOptions.optionsValuesCollection.Add("LinuxSetupCompleted", completed.ToString());
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to save LinuxSetupCompleted setting: {ex.Message}");
            }
        }
    }
}
