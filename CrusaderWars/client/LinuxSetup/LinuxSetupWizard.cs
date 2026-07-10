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
        private Models.LinuxSetupConfig _config = new Models.LinuxSetupConfig();

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
            bool isProton = _linuxEnvDetector.IsRunningUnderProton();
            bool isNativeLinux = _linuxEnvDetector.IsRunningOnNativeLinux();

            // Determine Linux display string based on environment type
            string linuxDisplay;
            if (!isLinux)
            {
                linuxDisplay = "Not Detected (Windows mode)";
            }
            else if (isProton)
            {
                linuxDisplay = "Detected (Proton)";
            }
            else if (isNativeLinux)
            {
                linuxDisplay = "Detected (Native)";
            }
            else
            {
                linuxDisplay = "Detected (Wine)";
            }

            _detectionStep.SetDetectionResult("Linux", linuxDisplay, isLinux);
            await Task.Delay(200);
            if (!isLinux)
            {
                lblStatus.Text = "Error: Not running on Linux. This wizard is for Linux/Proton users.";
                return false;
            }
            progressBar.Value = 10;

            // Proton detection
            string? protonPrefix = _linuxEnvDetector.GetProtonPrefix();
            string protonDisplay = isProton && !string.IsNullOrEmpty(protonPrefix)
                ? $"Detected ({protonPrefix})"
                : (isProton ? "Detected (prefix unknown)" : "Not Detected");
            _detectionStep.SetDetectionResult("Proton", protonDisplay, isProton);
            await Task.Delay(200);
            progressBar.Value = 12;

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

            // Populate LinuxSetupConfig with detection results
            _config.IsProton = isProton;
            _config.ProtonPrefix = protonPrefix;

            if (!wineFound && !isProton)
            {
                lblStatus.Text = "Error: Wine was not found and not running under Proton. Cannot proceed.";
                return false;
            }

            if (!steamFound || !attilaFound)
            {
                lblStatus.Text = "Warning: Steam/Attila not found. Manual path configuration will be required in the main launcher.";
            }
            else
            {
                lblStatus.Text = "Environment detection complete.";
            }

            return true;
        }
        private async Task<bool> RunWinePrefixStep()
        {
            lblStatus.Text = "Step 2: Configuring Wine Prefix...";
            _detectionStep.Visible = false;
            _winePrefixStep.Visible = true;
            progressBar.Value = 35;

            // If running under Proton with a known prefix, reuse it
            if (_config.IsProton && !string.IsNullOrEmpty(_config.ProtonPrefix))
            {
                string protonPrefix = _config.ProtonPrefix!;
                _winePrefixStep.SetStatus($"Using existing Proton prefix: {protonPrefix}", true);
                Program.Logger.Debug($"Reusing existing Proton prefix at {protonPrefix}");

                // Verify the prefix directory exists
                if (System.IO.Directory.Exists(protonPrefix))
                {
                    _config.WinePrefix = protonPrefix;
                    progressBar.Value = 40;
                    lblStatus.Text = "Proton prefix detected and will be used.";
                    return true;
                }
                else
                {
                    Program.Logger.Debug($"Proton prefix path {protonPrefix} does not exist on disk. Falling back to creating dedicated prefix.");
                    _winePrefixStep.SetStatus($"Proton prefix path not found: {protonPrefix}. Creating dedicated prefix instead.", true);
                }
            }

            // Create dedicated prefix (either native Linux/Wine or Proton without known prefix)
            string prefixPath = _config.WinePrefix;
            _winePrefixStep.SetStatus($"Creating Wine prefix at {prefixPath}...", true);
            bool success = await _wineManager.CreatePrefix(prefixPath);

            if (success)
            {
                _winePrefixStep.SetStatus($"Wine prefix created successfully at {prefixPath}.", true);
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
            lblStatus.Text = "Step 3: Checking .NET Framework...";
            _winePrefixStep.Visible = false;
            _dotNetInstallStep.Visible = true;
            progressBar.Value = 45;

            string prefixPath = _config.WinePrefix;

            // Pre-check: See if .NET is already installed in the prefix
            _dotNetInstallStep.UpdateStatus("Checking if .NET is already installed...");
            bool dotNetAlreadyInstalled = await CheckDotNetInstalled(prefixPath);

            if (dotNetAlreadyInstalled)
            {
                _dotNetInstallStep.UpdateStatus(".NET Framework is already installed in the prefix. Skipping installation.");
                _dotNetInstallStep.SetSuccess();
                progressBar.Value = 60;
                lblStatus.Text = ".NET already installed — skipping.";
                return;
            }

            // .NET not found — proceed with installation
            lblStatus.Text = "Step 3: Installing .NET Framework...";
            _dotNetInstallStep.UpdateStatus(".NET not detected. Installing .NET 4.7.2 via winetricks (this may take several minutes)...");

            var progress = new Progress<string>(status =>
            {
                _dotNetInstallStep.UpdateStatus(status);
            });

            bool success = await _wineManager.InstallDotNet472(prefixPath, progress);

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

        /// <summary>
        /// Checks whether .NET Framework is already installed in the specified Wine prefix.
        /// Uses multiple detection strategies:
        /// 1. Check for .NET registry keys via 'wine reg query'
        /// 2. Check for known .NET installation directories in drive_c
        /// 3. Check for mscoree.dll in the prefix's system32
        /// </summary>
        private async Task<bool> CheckDotNetInstalled(string prefixPath)
        {
            try
            {
                string? home = _linuxEnvDetector.GetHomeDirectory();
                if (string.IsNullOrEmpty(home)) return false;

                string fullPrefixPath = prefixPath.Replace("~", home);

                // Strategy 1: Check for .NET Framework directories in drive_c
                string driveC = System.IO.Path.Combine(fullPrefixPath, "drive_c");
                if (System.IO.Directory.Exists(driveC))
                {
                    // Check for Microsoft.NET directory
                    string dotNetFrameworkDir = System.IO.Path.Combine(driveC, "windows", "Microsoft.NET", "Framework");
                    if (System.IO.Directory.Exists(dotNetFrameworkDir))
                    {
                        // Look for v4.0.* directories (indicating .NET 4.x is installed)
                        var dirs = System.IO.Directory.GetDirectories(dotNetFrameworkDir, "v4.0.*");
                        if (dirs.Length > 0)
                        {
                            Program.Logger.Debug($".NET Framework detected via directory check: {dirs[0]}");
                            return true;
                        }
                    }

                    // Check for mscoree.dll in system32 (core .NET runtime DLL)
                    string mscoreePath = System.IO.Path.Combine(driveC, "windows", "system32", "mscoree.dll");
                    if (System.IO.File.Exists(mscoreePath))
                    {
                        Program.Logger.Debug(".NET Framework detected via mscoree.dll presence.");
                        return true;
                    }

                    // Check for .NET assemblies in the GAC or assembly directory
                    string assemblyDir = System.IO.Path.Combine(driveC, "windows", "assembly");
                    if (System.IO.Directory.Exists(assemblyDir))
                    {
                        // Check for any System.* DLLs that indicate .NET is present
                        string gacDir = System.IO.Path.Combine(driveC, "windows", "Microsoft.NET", "assembly", "GAC_MSIL");
                        if (System.IO.Directory.Exists(gacDir))
                        {
                            Program.Logger.Debug(".NET Framework detected via GAC directory presence.");
                            return true;
                        }
                    }
                }

                // Strategy 2: Try wine reg query to check for .NET registry keys
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "/bin/bash",
                            Arguments = $"-c \"WINEPREFIX='{fullPrefixPath}' wine reg query 'HKLM\\\\Software\\\\Microsoft\\\\NET Framework Setup\\\\NDP\\\\v4\\\\Full' 2>/dev/null\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(output) && output.Contains("Install") && output.Contains("REG_DWORD"))
                    {
                        Program.Logger.Debug(".NET Framework v4 detected via registry query.");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Registry check for .NET failed (non-fatal): {ex.Message}");
                }

                // Strategy 3: Try running a simple .NET check via wine
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "/bin/bash",
                            Arguments = $"-c \"WINEPREFIX='{fullPrefixPath}' wine dotnet --version 2>/dev/null || wine dotnet --info 2>/dev/null\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        }
                    };
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(output) && !output.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        Program.Logger.Debug($".NET detected via 'wine dotnet --version': {output.Trim()}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"dotnet version check failed (non-fatal): {ex.Message}");
                }

                Program.Logger.Debug("No .NET Framework installation detected in prefix.");
                return false;
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error during .NET pre-check: {ex.Message}");
                // If the check itself fails, assume .NET is not installed and proceed with installation
                return false;
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
                _modSymlinkStep.SetStatus("Skipping: Could not find Attila's Steam workshop or data paths.\nYou may need to configure mods manually if you use a non-Steam version.", true);
                lblStatus.Text = "Mod symlinking skipped.";
                progressBar.Value = 75;
                await Task.Delay(2000); // Give user time to read
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
            lblStatus.Text = "Step 5: Add to Steam";
            _modSymlinkStep.Visible = false;
            _shortcutCreationStep.Visible = true;
            progressBar.Value = 85;

            string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "start-linux.sh");

            string instructions = "Please add the game to Steam:\n\n" +
                                  "1. In Steam: Games -> 'Add a Non-Steam Game to My Library...'\n" +
                                  $"2. Click 'Browse...' and select:\n{scriptPath}\n" +
                                  "3. Click 'Add Selected Programs'.\n" +
                                  "4. Find 'start-linux.sh' in your library, right-click -> Properties.\n" +
                                  "5. Optional: Rename it to 'Crusader Conflicts'.\n\n" +
                                  "You do NOT need to enable Proton for this entry.";

            _shortcutCreationStep.SetStatus(instructions, true);
            
            lblStatus.Text = "Awaiting user action...";
            progressBar.Value = 90;

            // This step is now instructional, so we assume success and allow the user to proceed.
            await Task.CompletedTask;
        }

        private async Task RunSteamConfigStep()
        {
            lblStatus.Text = "Step 6: Attila Configuration...";
            _shortcutCreationStep.Visible = false;
            _steamConfigStep.Visible = true;
            progressBar.Value = 95;

            _steamConfigStep.SetStatus("Final step! Please configure Total War: Attila in Steam.\n\n" +
                                     "Under Properties -> General -> Launch Options, set the following:\n\n" +
                                     "%command% used_mods_cw.txt\n\n" +
                                     "This allows Crusader Conflicts to manage which mods are active for battles.");
            await _steamManager.SetLaunchOptions("325610", "%command% used_mods_cw.txt");
            
            lblStatus.Text = "Attila configuration instructions provided.";
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
