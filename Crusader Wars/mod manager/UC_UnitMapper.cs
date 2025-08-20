using Crusader_Wars.client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Crusader_Wars.mod_manager
{
    public partial class UC_UnitMapper : UserControl
    {
        List<UC_UnitMapper> AllControlsReferences { get; set; }

        string SteamCollectionLink {  get; set; }
        List<string> RequiredModsList { get; set; }
        
        public UC_UnitMapper(Bitmap image, string steamCollectionLink, List<string> requiredMods,bool state)
        {
            InitializeComponent();

            pictureBox1.BackgroundImage = image;
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            SteamCollectionLink = steamCollectionLink;
            uC_Toggle1.SetState(state);
            RequiredModsList = requiredMods;
        }

        internal void SetOtherControlsReferences(UC_UnitMapper[] references)
        {
            AllControlsReferences = new List<UC_UnitMapper>();
            for (int i = 0; i < references.Length; i++) { AllControlsReferences.Add(references[i]); }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start(SteamCollectionLink);
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

                MessageBox.Show($"You are missing these mods:\n{missingMods}", "Crusader Wars: Missing Mods!",
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

                    MessageBox.Show($"You are missing these mods:\n{missingMods}", "Crusader Wars: Missing Mods!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    uC_Toggle1.SetState(false);
                }
                else if (notFoundMods.Count == 0) // all installed
                {
                    MessageBox.Show("All mods are installed, you are good to go!", "Crusader Wars: All mods installed!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
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
                MessageBox.Show("Error reading Attila data folder. This is caused by wrong Attila path.", "Crusader Wars: Game Paths Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }

            //Verify workshop folder
            string workshop_folder_path = AttilaModManager.GetWorkshopFolderPath();
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
