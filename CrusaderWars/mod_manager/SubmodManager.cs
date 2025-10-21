using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace CrusaderWars.mod_manager
{
    public static class SubmodManager
    {
        private static readonly string ActiveSubmodsFilePath = @".\settings\ActiveSubmods.xml";
        private static Dictionary<string, List<string>> ActiveSubmodsByPlaythrough { get; set; } = new Dictionary<string, List<string>>();

        public static void LoadActiveSubmods()
        {
            Program.Logger.Debug("Loading active submods from ActiveSubmods.xml...");
            ActiveSubmodsByPlaythrough.Clear();

            if (!File.Exists(ActiveSubmodsFilePath))
            {
                Program.Logger.Debug("ActiveSubmods.xml not found. No submods will be active.");
                return;
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
                            activeSubmods.Add(submodNode.InnerText);
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

                    foreach (var submodTag in entry.Value)
                    {
                        XmlElement submodElement = xmlDoc.CreateElement("Submod");
                        submodElement.InnerText = submodTag;
                        playthroughElement.AppendChild(submodElement);
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
    }
}
