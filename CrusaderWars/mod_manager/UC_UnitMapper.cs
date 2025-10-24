using CrusaderWars.client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text; // Added for StringBuilder
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.mod_manager
{
    public partial class UC_UnitMapper : UserControl
    {
        public event EventHandler? ToggleClicked;
        private bool _isPulsing;
        private bool _pulseState;
        List<UC_UnitMapper> AllControlsReferences { get; set; } = null!;

        string SteamCollectionLink {  get; set; }
        List<(string FileName, string Sha256)> RequiredModsList { get; set; }
        private ToolTip toolTip2; // Added ToolTip field
        private readonly List<Submod> _availableSubmods;
        private readonly string _playthroughTag;

        public string GetPlaythroughTag() { return _playthroughTag; }

        public UC_UnitMapper(Bitmap image, string steamCollectionLink, List<(string FileName, string Sha256)> requiredMods, bool state, string playthroughTag, List<Submod> submods)
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
            _playthroughTag = playthroughTag;
            _availableSubmods = submods;

            BtnSubmods.Visible = _availableSubmods != null && _availableSubmods.Any();
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

                if (_playthroughTag == "AGOT")
                {
                    modsMessage.AppendLine("For the 'A Game of Thrones (AGOT)' playthrough, please ensure you have the following CK3 submod installed:");
                    modsMessage.AppendLine("• Lord of the Tides (adds House Velaryon)");
                    modsMessage.AppendLine("  Download: https://www.moddb.com/downloads/lord-of-the-tides-v04");
                    modsMessage.AppendLine();
                    modsMessage.AppendLine("Additionally, the following Total War: Attila mods are required:");
                }
                else // TheFallenEagle
                {
                    modsMessage.AppendLine($"{_playthroughTag} playthrough requires the following mods for Total War: Attila:");
                }

                if (RequiredModsList != null && RequiredModsList.Count > 0)
                {
                    foreach (var (mod, _) in RequiredModsList)
                    {
                        modsMessage.AppendLine($"- {mod}");
                    }
                    modsMessage.AppendLine("\nPlease ensure these are enabled in the Attila Mod Manager.");
                }
                else
                {
                    if (_playthroughTag == "AGOT")
                    {
                        modsMessage.AppendLine("No additional Total War: Attila mods are listed as required for this playthrough.");
                    }
                    else
                    {
                        modsMessage.AppendLine($"No specific required mods are listed for '{_playthroughTag}' playthrough at this time.");
                    }
                }

                if (_playthroughTag == "AGOT")
                {
                    ShowClickableMessageBox(modsMessage.ToString(), messageBoxTitle);
                }
                else
                {
                    MessageBox.Show(modsMessage.ToString(), messageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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

        private async void uC_Toggle1_Click(object sender, EventArgs e)
        {
            // 1. Skip Verification When Deactivating a Playthrough
            if (!uC_Toggle1.State)
            {
                ToggleClicked?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Show loading state
            this.Cursor = Cursors.WaitCursor;
            uC_Toggle1.Enabled = false;
            BtnVerifyMods.Enabled = false;
            button1.Enabled = false;

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
                Text = "Validating TW:Attila mod files...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            statusForm.Controls.Add(statusLabel);
            statusForm.Show(this.FindForm());

            var progress = new Progress<string>(update => {
                statusLabel.Text = update;
            });

            try
            {
                var verificationResult = await Task.Run(() => VerifyModFiles(RequiredModsList, progress));

                // 1. Check for missing files (highest priority)
                if (verificationResult.MissingFiles.Any())
                {
                    string missingMods = string.Join("\n", verificationResult.MissingFiles);
                    MessageBox.Show($"You are missing these required mods:\n{missingMods}", "Crusader Conflicts: Missing Mods!",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    uC_Toggle1.SetState(false);
                    return; // Stop here
                }

                // 2. Check for mismatched files
                if (verificationResult.MismatchedFiles.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("One or more required Total War: Attila mod files for this playthrough have different versions than expected.");
                    sb.AppendLine("This could mean the mod is outdated, or it has been updated by the mod author and may still be compatible.");
                    sb.AppendLine("\nMismatched files:");
                    foreach (var (fileName, _) in verificationResult.MismatchedFiles)
                    {
                        sb.AppendLine($"- {fileName}");
                    }
                    sb.AppendLine("\nPlease ensure you have the latest versions of these mods from the Steam Workshop.");
                    sb.AppendLine("If your mods are up-to-date and you still see this warning, please report it to the Crusader Conflicts Development Team at https://discord.gg/eFZTprHh3j so we can update our compatibility check.");
                    sb.AppendLine("\nDo you want to activate this playthrough anyway?");

                    var dialogResult = MessageBox.Show(sb.ToString(), "Crusader Conflicts: Mod Version Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (dialogResult == DialogResult.No)
                    {
                        uC_Toggle1.SetState(false);
                        return; // User cancelled
                    }
                }

                // If we get here, either everything is fine, or the user chose to ignore mismatches.
                // 2. Correctly Deactivate Other Playthroughs on Activation
                foreach (var controlReference in AllControlsReferences)
                {
                    if (controlReference != this)
                    {
                        controlReference.uC_Toggle1.SetState(false);
                    }
                }
                ToggleClicked?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                // Restore UI state
                statusForm.Close();
                this.Cursor = Cursors.Default;
                uC_Toggle1.Enabled = true;
                BtnVerifyMods.Enabled = true;
                button1.Enabled = true;
            }
        }

        private void ShowClickableMessageBox(string text, string title)
        {
            using (Form form = new Form())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.ClientSize = new Size(480, 250);
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                RichTextBox richTextBox = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    Text = text,
                    DetectUrls = true,
                    BackColor = SystemColors.Control,
                    Font = new Font("Segoe UI", 9F),
                    Padding = new Padding(10)
                };
                richTextBox.LinkClicked += (s, args) => {
                    Process.Start(new ProcessStartInfo(args.LinkText) { UseShellExecute = true });
                };

                Button okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Size = new Size(75, 25)
                };

                TableLayoutPanel panel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    Padding = new Padding(0, 0, 0, 5)
                };
                panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
                
                panel.Controls.Add(richTextBox, 0, 0);

                FlowLayoutPanel buttonPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(0, 5, 10, 0)
                };
                buttonPanel.Controls.Add(okButton);
                
                panel.Controls.Add(buttonPanel, 0, 1);

                form.Controls.Add(panel);
                form.AcceptButton = okButton;

                form.ShowDialog(this.FindForm());
            }
        }

        private async void BtnVerifyMods_Click(object sender, EventArgs e)
        {
            if (RequiredModsList != null)
            {
                // Show loading state
                this.Cursor = Cursors.WaitCursor;
                uC_Toggle1.Enabled = false;
                BtnVerifyMods.Enabled = false;
                button1.Enabled = false;

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
                    Text = "Validating TW:Attila mod files...",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                statusForm.Controls.Add(statusLabel);
                statusForm.Show(this.FindForm());

                var progress = new Progress<string>(update => {
                    statusLabel.Text = update;
                });

                try
                {
                    var verificationResult = await Task.Run(() => VerifyModFiles(RequiredModsList, progress));

                    // 1. Check for missing files
                    if (verificationResult.MissingFiles.Any())
                    {
                        string missingMods = string.Join("\n", verificationResult.MissingFiles);
                        MessageBox.Show($"You are missing these mods:\n{missingMods}", "Crusader Conflicts: Missing Mods!",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        uC_Toggle1.SetState(false);
                    }

                    // 2. Check for mismatched files
                    if (verificationResult.MismatchedFiles.Any())
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("The following required mods have a different version than expected:");
                        foreach (var (fileName, _) in verificationResult.MismatchedFiles)
                        {
                            sb.AppendLine($"- {fileName}");
                        }
                        sb.AppendLine("\nThis may cause issues. Please ensure you have the latest versions of these mods from the Steam Workshop.");
                        sb.AppendLine("If you believe this is an error, please raise the issue on our Discord: https://discord.gg/eFZTprHh3j");
                        MessageBox.Show(sb.ToString(), "Crusader Conflicts: TW:Attila Mod Version Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    // 3. If no missing files, show success message
                    if (!verificationResult.MissingFiles.Any())
                    {
                        string message = verificationResult.MismatchedFiles.Any()
                            ? "All required mods are installed.\n(Note: Version mismatches were detected, see previous message)."
                            : "Successfully validated TW:Attila mod files.";
                        MessageBox.Show(message, "Crusader Conflicts: Mod Verification Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                finally
                {
                    // Restore UI state
                    statusForm.Close();
                    this.Cursor = Cursors.Default;
                    uC_Toggle1.Enabled = true;
                    BtnVerifyMods.Enabled = true;
                    button1.Enabled = true;
                }
            }
        }

        internal class VerificationResult
        {
            public List<string> MissingFiles { get; } = new List<string>();
            public List<(string FileName, string ExpectedSha)> MismatchedFiles { get; } = new List<(string, string)>();
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

        private string CalculateSHA256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private VerificationResult VerifyModFiles(List<(string FileName, string Sha256)> modsToVerifyList, IProgress<string>? progress)
        {
            Program.Logger.Debug("Verifying mod files...");
            var result = new VerificationResult();
            var modsToFind = modsToVerifyList.GroupBy(item => item.FileName).ToDictionary(g => g.Key, g => g.First().Sha256);
            Program.Logger.Debug($"Mods to verify: {string.Join(", ", modsToFind.Keys)}");


            //Verify data folder
            string data_folder_path = Properties.Settings.Default.VAR_attila_path.Replace("Attila.exe", @"data\");
            Program.Logger.Debug($"Checking Attila data folder: {data_folder_path}");
            if (Directory.Exists(data_folder_path))
            {
                var dataModsPaths = Directory.GetFiles(data_folder_path);
                foreach (var file in dataModsPaths)
                {
                    var fileName = Path.GetFileName(file);
                    if (modsToFind.ContainsKey(fileName) && Path.GetExtension(fileName) == ".pack")
                    {
                        progress?.Report($"Verifying: {fileName}");
                        string expectedSha = modsToFind[fileName];
                        if (!string.IsNullOrEmpty(expectedSha))
                        {
                            string actualSha = CalculateSHA256(file);
                            if (string.Equals(expectedSha, actualSha, StringComparison.OrdinalIgnoreCase))
                            {
                                Program.Logger.Debug($"Found required mod in data folder with matching hash: {fileName}");
                                modsToFind.Remove(fileName);
                            }
                            else
                            {
                                Program.Logger.Debug($"Found required mod '{fileName}' in data folder but hash mismatched. Expected: {expectedSha}, Actual: {actualSha}");
                                result.MismatchedFiles.Add((fileName, expectedSha));
                                modsToFind.Remove(fileName); // Still remove it so it's not counted as missing
                            }
                        }
                        else // No hash provided, just check for existence
                        {
                            Program.Logger.Debug($"Found required mod in data folder (no hash check): {fileName}");
                            modsToFind.Remove(fileName);
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
                        if (modsToFind.ContainsKey(fileName) && Path.GetExtension(fileName) == ".pack")
                        {
                            progress?.Report($"Verifying: {fileName}");
                            string expectedSha = modsToFind[fileName];
                            if (!string.IsNullOrEmpty(expectedSha))
                            {
                                string actualSha = CalculateSHA256(file);
                                if (string.Equals(expectedSha, actualSha, StringComparison.OrdinalIgnoreCase))
                                {
                                    Program.Logger.Debug($"Found required mod in workshop folder with matching hash: {fileName}");
                                    modsToFind.Remove(fileName);
                                }
                                else
                                {
                                    Program.Logger.Debug($"Found required mod '{fileName}' in workshop folder but hash mismatched. Expected: {expectedSha}, Actual: {actualSha}");
                                    result.MismatchedFiles.Add((fileName, expectedSha));
                                    modsToFind.Remove(fileName); // Still remove it so it's not counted as missing
                                }
                            }
                            else // No hash provided, just check for existence
                            {
                                Program.Logger.Debug($"Found required mod in workshop folder (no hash check): {fileName}");
                                modsToFind.Remove(fileName);
                            }
                        }
                    }
                }
            }

            // Any mods remaining in modsToFind are missing from both locations.
            result.MissingFiles.AddRange(modsToFind.Keys);

            if (result.MissingFiles.Any()) Program.Logger.Debug($"Mods not found: {string.Join(", ", result.MissingFiles)}");
            if (result.MismatchedFiles.Any()) Program.Logger.Debug($"Mismatched mods: {string.Join(", ", result.MismatchedFiles.Select(m => m.FileName))}");
            if (!result.MissingFiles.Any() && !result.MismatchedFiles.Any()) Program.Logger.Debug("All required mods were found and hashes match.");
            return result;
        }

        private async void BtnSubmods_Click(object sender, EventArgs e)
        {
            var activeSubmods = SubmodManager.GetActiveSubmodsForPlaythrough(_playthroughTag);

            using (var selectionForm = new SubmodSelectionForm(_availableSubmods, activeSubmods))
            {
                if (selectionForm.ShowDialog() == DialogResult.OK)
                {
                    var selectedSubmodTags = selectionForm.SelectedSubmodTags;

                    // Compare old and new selections to see if validation is needed
                    var initialSelection = new HashSet<string>(activeSubmods);
                    var newSelection = new HashSet<string>(selectedSubmodTags);

                    if (initialSelection.SetEquals(newSelection))
                    {
                        Program.Logger.Debug("Optional sub-mod selection unchanged. Skipping validation.");
                        return; // Nothing changed, so just exit.
                    }
                    Program.Logger.Debug("Optional sub-mod selection changed. Proceeding with validation.");


                    var selectedSubmods = _availableSubmods.Where(s => selectedSubmodTags.Contains(s.Tag)).ToList();
                    var modsToValidate = selectedSubmods.SelectMany(s => s.Mods).ToList();

                    if (!modsToValidate.Any())
                    {
                        SubmodManager.SetActiveSubmodsForPlaythrough(_playthroughTag, selectedSubmodTags);
                        MessageBox.Show("Optional sub-mod selection updated successfully.", "Crusader Conflicts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    this.Cursor = Cursors.WaitCursor;
                    BtnSubmods.Enabled = false;

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
                        Text = "Validating sub-mod files...",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    statusForm.Controls.Add(statusLabel);
                    statusForm.Show(this.FindForm());

                    var progress = new Progress<string>(update => {
                        statusLabel.Text = update;
                    });

                    try
                    {
                        var verificationResult = await Task.Run(() => VerifyModFiles(modsToValidate, progress));

                        if (verificationResult.MissingFiles.Any())
                        {
                            string missingMods = string.Join("\n", verificationResult.MissingFiles);
                            MessageBox.Show($"You are missing these required sub-mod files:\n{missingMods}", "Crusader Conflicts: Missing Sub-Mod Files!",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        if (verificationResult.MismatchedFiles.Any())
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("One or more required sub-mod files have different versions than expected.");
                            sb.AppendLine("This could mean the mod is outdated or has been updated by its author.");
                            sb.AppendLine("\nMismatched files:");
                            foreach (var (fileName, _) in verificationResult.MismatchedFiles)
                            {
                                sb.AppendLine($"- {fileName}");
                            }
                            sb.AppendLine("\nDo you want to activate these sub-mods anyway?");

                            var dialogResult = MessageBox.Show(sb.ToString(), "Crusader Conflicts: Sub-Mod Version Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                            if (dialogResult == DialogResult.No)
                            {
                                return;
                            }
                        }

                        SubmodManager.SetActiveSubmodsForPlaythrough(_playthroughTag, selectedSubmodTags);
                        MessageBox.Show("Optional mod selection updated successfully.", "Crusader Conflicts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    finally
                    {
                        statusForm.Close();
                        this.Cursor = Cursors.Default;
                        BtnSubmods.Enabled = true;
                    }
                }
            }
        }
    }
}
