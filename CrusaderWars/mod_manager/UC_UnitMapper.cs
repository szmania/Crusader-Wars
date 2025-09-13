using CrusaderWars.client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text; // Added for StringBuilder
using System.Linq;

namespace CrusaderWars.mod_manager
{
    public partial class UC_UnitMapper : UserControl
    {
        public event EventHandler? ToggleClicked;
        private bool _isPulsing;
        private bool _pulseState;
        List<UC_UnitMapper> AllControlsReferences { get; set; } = null!;

        string SteamCollectionLink {  get; set; }
        List<string> RequiredModsList { get; set; }
        private ToolTip toolTip2; // Added ToolTip field
        private string _playthroughTag; // NEW FIELD

        public UC_UnitMapper(Bitmap image, string steamCollectionLink, List<string> requiredMods, bool state, string playthroughTag) // UPDATED CONSTRUCTOR SIGNATURE
        {
            InitializeComponent();

            toolTip2 = new ToolTip(); // Initialize ToolTip
            toolTip2.AutoPopDelay = 5000;
            toolTip2.InitialDelay = 1000;
            toolTip2.ReshowDelay = 500;
            toolTip2.ShowAlways = true;

            pictureBox1.BackgroundImage = image;
            pictureBox1.BackgroundImageLayout = ImageLayout.Zoom; // Changed from Stretch to Zoom
            SteamCollectionLink = steamCollectionLink;
            uC_Toggle1.SetState(state);
            RequiredModsList = requiredMods;
            _playthroughTag = playthroughTag; // INITIALIZE NEW FIELD
        }

        public void SetPulsing(bool isPulsing)
        {
            _isPulsing = isPulsing;
            this.Invalidate();
        }

        public void Pulse()
        {
            if (!_isPulsing) return;
            _pulseState = !_pulseState;
            this.Invalidate();
        }

        private void UC_UnitMapper_Paint(object sender, PaintEventArgs e)
        {
            if (_isPulsing)
            {
                Color pulseColor = _pulseState ? Color.FromArgb(100, 255, 255, 0) : Color.FromArgb(200, 255, 215, 0); // Yellowish pulse
                using (Pen pen = new Pen(pulseColor, 3))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
                }
            }
        }

        internal void SetOtherControlsReferences(UC_UnitMapper[] references)
        {
            AllControlsReferences = new List<UC_UnitMapper>();
            for (int i = 0; i < references.Length; i++) { AllControlsReferences.Add(references[i]); }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_playthroughTag == "TheFallenEagle" || _playthroughTag == "AGOT") // Modified condition
            {
                // Build a string containing the list of mods
                StringBuilder modsMessage = new StringBuilder();
                string messageBoxTitle = $"{_playthroughTag} - Required Mods"; // Dynamic title
                if (RequiredModsList != null && RequiredModsList.Count > 0)
                {
                    modsMessage.AppendLine($"{_playthroughTag} playthrough requires the following mods for Total War: Attila:");
                    foreach (var mod in RequiredModsList)
                    {
                        modsMessage.AppendLine($"- {mod}");
                    }
                    modsMessage.AppendLine("\nPlease ensure these are enabled in the Attila Mod Manager.");
                }
                else
                {
                    modsMessage.AppendLine($"No specific required mods are listed for '{_playthroughTag}' playthrough at this time.");
                }

                MessageBox.Show(modsMessage.ToString(), messageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (!string.IsNullOrEmpty(SteamCollectionLink)) // Original logic for other playthroughs
            {
                Process.Start(new ProcessStartInfo(SteamCollectionLink) { UseShellExecute = true });
            }
        }

        public bool GetState()
        {
            return uC_Toggle1.State;
        }

        private void uC_Toggle1_Click(object sender, EventArgs e)
        {
            var notFoundMods = VerifyIfAllModsAreInstalled();

            //Print Message
            if (notFoundMods.Count > 0) // not all installed
            {
                string missingMods = "";
                foreach (var mod in notFoundMods)
                    missingMods += $"{mod}\n";

                MessageBox.Show($"You are missing these mods:\n{missingMods}", "Crusader Conflicts: Missing Mods!",
                MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                uC_Toggle1.SetState(false);
            }

            if(uC_Toggle1.State == true)
            {
                foreach(var controlReference in AllControlsReferences)
                {
                    controlReference.uC_Toggle1.SetState(false);
                }
            }
            ToggleClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnVerifyMods_Click(object sender, EventArgs e)
        {
            if(RequiredModsList != null)
            {

                var notFoundMods = VerifyIfAllModsAreInstalled();

                //Print Message
                if (notFoundMods.Count > 0) // not all installed
                {
                    string missingMods = "";
                    foreach (var mod in notFoundMods)
                        missingMods += $"{mod}\n";

                    MessageBox.Show($"You are missing these mods:\n{missingMods}", "Crusader Conflicts: Missing Mods!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    uC_Toggle1.SetState(false);
                }
                else if (notFoundMods.Count == 0) // all installed
                {
                    MessageBox.Show("All mods are installed, you are good to go!", "Crusader Conflicts: All mods installed!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        public void SetSteamLinkButtonTooltip(string text)
        {
            Button? btn = this.Controls.Find("button1", true).FirstOrDefault() as Button;
            if (btn != null)
            {
                toolTip2.SetToolTip(btn, text);
            }
            else
            {
                Program.Logger.Debug("Steam link button (button1) not found in UC_UnitMapper for tooltip setting.");
            }
        }

        List<string> VerifyIfAllModsAreInstalled()
        {
            Program.Logger.Debug("Verifying if all required mods are installed...");
            List<string> notFoundMods = new List<string>();
            notFoundMods.AddRange(RequiredModsList);
            Program.Logger.Debug($"Required mods list: {string.Join(", ", RequiredModsList)}");


            //Verify data folder
            string data_folder_path = Properties.Settings.Default.VAR_attila_path.Replace("Attila.exe", @"data\");
            Program.Logger.Debug($"Checking Attila data folder: {data_folder_path}");
            if (Directory.Exists(data_folder_path))
            {
                var dataModsPaths = Directory.GetFiles(data_folder_path);
                foreach (var file in dataModsPaths)
                {
                    var fileName = Path.GetFileName(file);
                    foreach (var mod in RequiredModsList)
                    {
                        if (mod == fileName && Path.GetExtension(fileName) == ".pack")
                        {
                            Program.Logger.Debug($"Found required mod in data folder: {fileName}");
                            notFoundMods.Remove(mod);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Error reading Attila data folder. This is caused by wrong Attila path.", "Crusader Conflicts: Game Paths Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }

            //Verify workshop folder
            string? workshop_folder_path = AttilaModManager.GetWorkshopFolderPath();
            Program.Logger.Debug($"Checking Attila workshop folder: {workshop_folder_path}");
            if (Directory.Exists(workshop_folder_path))
            {
                var steamModsFoldersPaths = Directory.GetDirectories(workshop_folder_path);
                foreach (var folder in steamModsFoldersPaths)
                {
                    var files = Directory.GetFiles(folder);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        foreach (var mod in RequiredModsList)
                        {
                            if (mod == fileName && Path.GetExtension(fileName) == ".pack")
                            {
                                Program.Logger.Debug($"Found required mod in workshop folder: {fileName}");
                                notFoundMods.Remove(mod);
                            }
                        }
                    }
                }
            }

            if (notFoundMods.Count > 0)
            {
                Program.Logger.Debug($"Mods not found: {string.Join(", ", notFoundMods)}");
            }
            else
            {
                Program.Logger.Debug("All required mods were found.");
            }
            return notFoundMods;
        }

    }
}
