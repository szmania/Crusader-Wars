using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Windows.Forms; // Assuming it's a Form
using CrusaderWars.client; // For ModOptions

namespace CrusaderWars
{
    // Assuming Options is a Form, based on MainFile.cs usage
    public partial class Options : Form
    {
        // Static members for managing options.xml
        public static Dictionary<string, string> optionsValuesCollection = new Dictionary<string, string>();
        private static string optionsFilePath = @".\settings\options.xml";

        // Placeholder for existing constructor and InitializeComponent
        public Options()
        {
            InitializeComponent(); // Uncommented this line
        }

        // Placeholder for ReadGamePaths if it exists in this file
        public static void ReadGamePaths()
        {
            // Placeholder implementation
            Program.Logger.Debug("Options.ReadGamePaths() called (placeholder).");
        }

        public static void ReadOptionsFile()
        {
            Program.Logger.Debug("Reading options file...");
            optionsValuesCollection.Clear();
            if (!File.Exists(optionsFilePath))
            {
                Program.Logger.Debug("options.xml not found. Creating default.");
                CreateDefaultOptionsFile();
            }

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(optionsFilePath);
                var root = xmlDoc.DocumentElement;
                if (root != null)
                {
                    foreach (XmlNode node in root.ChildNodes)
                    {
                        if (node is XmlComment) continue;
                        if (node.Name == "Setting" && node.Attributes != null)
                        {
                            string? name = node.Attributes["name"]?.Value;
                            string? value = node.Attributes["value"]?.Value;
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                            {
                                optionsValuesCollection[name] = value;
                            }
                        }
                    }
                }
                Program.Logger.Debug("Options file read successfully.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error reading options.xml: {ex.Message}");
                // Optionally recreate default or handle error
                CreateDefaultOptionsFile();
                ReadOptionsFile(); // Try reading again
            }
        }

        private static void CreateDefaultOptionsFile()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement root = xmlDoc.CreateElement("Settings");
                xmlDoc.AppendChild(root);

                // Add default settings for all known options
                AddSetting(root, "ArmiesControl", ModOptions.ArmiesSetup.Friendly_Only.ToString());
                AddSetting(root, "CloseCK3DuringBattle", "False");
                AddSetting(root, "DefensiveDeployables", "True");
                AddSetting(root, "BattleScale", "Normal");
                AddSetting(root, "TimeLimit", "");
                AddSetting(root, "UnitCards", "True");
                // Add other default settings as needed, matching ModOptions.StoreOptionsValues expectations

                xmlDoc.Save(optionsFilePath);
                Program.Logger.Debug("Default options.xml created.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error creating default options.xml: {ex.Message}");
            }
        }

        private static void AddSetting(XmlElement parent, string name, string value)
        {
            XmlElement settingElement = parent.OwnerDocument.CreateElement("Setting");
            settingElement.SetAttribute("name", name);
            settingElement.SetAttribute("value", value);
            parent.AppendChild(settingElement);
        }

        private static void SaveOptionsFile()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement root = xmlDoc.CreateElement("Settings");
                xmlDoc.AppendChild(root);

                foreach (var entry in optionsValuesCollection)
                {
                    AddSetting(root, entry.Key, entry.Value);
                }

                xmlDoc.Save(optionsFilePath);
                Program.Logger.Debug("Options file saved successfully.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error saving options.xml: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the 'ArmiesControl' setting in options.xml and refreshes ModOptions.
        /// </summary>
        /// <param name="value">The new ArmiesSetup value.</param>
        public static void SetArmiesControl(ModOptions.ArmiesSetup value)
        {
            Program.Logger.Debug($"Attempting to set ArmiesControl to: {value}");

            // Ensure optionsValuesCollection is initialized and contains the key
            if (!optionsValuesCollection.ContainsKey("ArmiesControl"))
            {
                // If not present, ensure it's read or defaulted first
                ReadOptionsFile();
                if (!optionsValuesCollection.ContainsKey("ArmiesControl"))
                {
                    optionsValuesCollection["ArmiesControl"] = ModOptions.ArmiesSetup.Friendly_Only.ToString(); // Fallback default
                }
            }

            // 1. Update the optionsValuesCollection
            optionsValuesCollection["ArmiesControl"] = value.ToString();

            // 2. Write optionsValuesCollection back to options.xml
            SaveOptionsFile();

            // 3. Call ModOptions.StoreOptionsValues to update ModOptions
            ModOptions.StoreOptionsValues(optionsValuesCollection);

            Program.Logger.Debug($"ArmiesControl setting updated in options.xml and ModOptions.");
        }

        // --- Missing Event Handler Stubs ---

        private void ck3Btn_Click(object sender, EventArgs e) { }
        private void ck3Btn_MouseHover(object sender, EventArgs e) { }
        private void CloseBtn_Click(object sender, EventArgs e) { }
        private void AttilaBtn_Click(object sender, EventArgs e) { }
        private void AttilaBtn_MouseHover(object sender, EventArgs e) { }
        private void Btn_GeneralTab_Click(object sender, EventArgs e) { }
        private void Btn_UnitsTab_Click(object sender, EventArgs e) { }
        private void Btn_BattleScaleTab_Click(object sender, EventArgs e) { }
        private void Btn_CandKTab_Click(object sender, EventArgs e) { }
        private void toggleModManagerButton_Click(object sender, EventArgs e) { }
        private void Btn_CK3Tab_Click(object sender, EventArgs e) { }
        private void Btn_TFETab_Click(object sender, EventArgs e) { }
        private void Btn_LOTRTab_Click(object sender, EventArgs e) { }
        private void Btn_AGOTTab_Click(object sender, EventArgs e) { }
        private void Options_Load(object sender, EventArgs e) { }

        private void ModManager_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e) { }
        private void ModManager_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e) { }
        private void ModManager_CellValueChanged(object sender, DataGridViewCellEventArgs e) { }

        private void TableLayoutPlaythroughs_Paint(object sender, PaintEventArgs e) { }

        private void Options_FormClosing(object sender, FormClosingEventArgs e) { }
        private void Options_MouseDown(object sender, MouseEventArgs e) { }
        private void Options_MouseMove(object sender, MouseEventArgs e) { }
    }
}
