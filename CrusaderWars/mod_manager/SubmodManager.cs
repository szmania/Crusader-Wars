using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using CrusaderWars.client;

namespace CrusaderWars.mod_manager
{
    public static class SubmodManager
    {
        private static readonly string ActiveSubmodsFilePath = @".\\settings\\ActiveSubmods.xml";
        private static readonly string ActiveSubmodsXsdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings", "schemas", "ActiveSubmods.xsd");
        private static bool _isRetryingSubmodsLoad = false;
        private static Dictionary<string, List<string>> ActiveSubmodsByPlaythrough { get; set; } = new Dictionary<string, List<string>>();

        public static void LoadActiveSubmods()
        {
            Program.Logger.Debug("Loading active submods from ActiveSubmods.xml...");
            ActiveSubmodsByPlaythrough.Clear();

            if (!File.Exists(ActiveSubmodsFilePath))
            {
                Program.Logger.Debug("ActiveSubmods.xml not found. Creating default file.");
                CreateDefaultActiveSubmodsFile();
                return;
            }
            else
            {
                var validationErrors = mod_manager.XmlValidator.Validate(ActiveSubmodsFilePath, ActiveSubmodsXsdPath);
                if (validationErrors.Count > 0)
                {
                    if (_isRetryingSubmodsLoad)
                    {
                        MessageBox.Show("The default ActiveSubmods.xml file is also invalid. The application will now exit.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                        return;
                    }

                    bool reset = ValidationHelper.HandleValidationFailure(ActiveSubmodsFilePath, "ActiveSubmods.xml", validationErrors, CreateDefaultActiveSubmodsFile);
                    if (reset)
                    {
                        _isRetryingSubmodsLoad = true;
                        LoadActiveSubmods(); // Re-run to load the new default file
                    }
                    return; // Exit the current execution path
                }
            }

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(ActiveSubmodsFilePath);
                var playthroughNodes = xmlDoc.SelectNodes("/ActiveSubmods/Playthrough");
                if (playthroughNodes == null) return;

                foreach (XmlNode playthroughNode in playthroughNodes)
                {
                    string? playthroughTag = playthroughNode.Attributes?["tag"]?.Value;
                    if (string.IsNullOrEmpty(playthroughTag)) continue;

                    var activeSubmods = new List<string>();
                    var submodNodes = playthroughNode.SelectNodes("Submod");
                    if (submodNodes != null)
                    {
                        foreach (XmlNode submodNode in submodNodes)
                        {
                            string submodTag = submodNode.InnerText;
                            activeSubmods.Add(submodTag);
                            Program.Logger.Debug($"  Loading submod '{submodTag}' for playthrough '{playthroughTag}'.");
                        }
                    }
                    ActiveSubmodsByPlaythrough[playthroughTag] = activeSubmods;
                    Program.Logger.Debug($"Loaded {activeSubmods.Count} active submods for playthrough '{playthroughTag}'.");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error loading ActiveSubmods.xml: {ex.Message}");
            }
        }

        public static void SaveActiveSubmods()
        {
            Program.Logger.Debug("Saving active submods to ActiveSubmods.xml...");
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement root = xmlDoc.CreateElement("ActiveSubmods");
                xmlDoc.AppendChild(root);

                foreach (var entry in ActiveSubmodsByPlaythrough)
                {
                    XmlElement playthroughElement = xmlDoc.CreateElement("Playthrough");
                    playthroughElement.SetAttribute("tag", entry.Key);
                    Program.Logger.Debug($"Saving {entry.Value.Count} active submods for playthrough '{entry.Key}'.");

                    foreach (var submodTag in entry.Value)
                    {
                        XmlElement submodElement = xmlDoc.CreateElement("Submod");
                        submodElement.InnerText = submodTag;
                        playthroughElement.AppendChild(submodElement);
                        Program.Logger.Debug($"  Saving submod '{submodTag}' for playthrough '{entry.Key}'.");
                    }
                    root.AppendChild(playthroughElement);
                }

                xmlDoc.Save(ActiveSubmodsFilePath);
                Program.Logger.Debug("Successfully saved active submods.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error saving ActiveSubmods.xml: {ex.Message}");
            }
        }

        public static List<string> GetActiveSubmodsForPlaythrough(string playthroughTag)
        {
            if (ActiveSubmodsByPlaythrough.TryGetValue(playthroughTag, out var activeSubmods))
            {
                return activeSubmods;
            }
            return new List<string>();
        }

        public static void SetActiveSubmodsForPlaythrough(string playthroughTag, List<string> activeSubmodTags)
        {
            if (activeSubmodTags == null || !activeSubmodTags.Any())
            {
                ActiveSubmodsByPlaythrough.Remove(playthroughTag);
            }
            else
            {
                ActiveSubmodsByPlaythrough[playthroughTag] = activeSubmodTags;
            }
        }

        private static void CreateDefaultActiveSubmodsFile()
        {
            try
            {
                Program.Logger.Debug("Creating default ActiveSubmods.xml file...");
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement root = xmlDoc.CreateElement("ActiveSubmods");
                xmlDoc.AppendChild(root);

                xmlDoc.Save(ActiveSubmodsFilePath);
                Program.Logger.Debug("Default ActiveSubmods.xml file created successfully.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error creating default ActiveSubmods.xml: {ex.Message}");
                MessageBox.Show($"Error creating default ActiveSubmods.xml: {ex.Message}", "File Creation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
