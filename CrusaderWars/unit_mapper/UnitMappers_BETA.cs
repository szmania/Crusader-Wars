using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using CrusaderWars.client;
using CrusaderWars.data.save_file;
using CrusaderWars.twbattle;

namespace CrusaderWars.unit_mapper
{
    public class ModFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public string? ScreenName { get; set; }
        public string? Url { get; set; }
    }

    public class Submod
    {
        public string Tag { get; set; } = string.Empty;
        public string ScreenName { get; set; } = string.Empty;
        public List<ModFile> Mods { get; set; } = new List<ModFile>();
        public List<string> Replaces { get; set; }  = new List<string>();
    }

    internal class LandBridgeMap
    {
        public string Name { get; set; } = string.Empty;
        public string ProvinceFrom { get; set; } = string.Empty;
        public string ProvinceTo { get; set; } = string.Empty;
        public string CK3Type { get; set; } = string.Empty;
        public string BattleType { get; set; } = string.Empty;
        public List<SettlementVariant> Variants { get; private set; }  = new List<SettlementVariant>();
    }

    internal class CoastalMap
    {
        public string Name { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string BattleType { get; set; } = string.Empty;
        public List<SettlementVariant> Variants { get; private set; }  = new List<SettlementVariant>();
    }

    public static class BattleStateBridge
    {
        public static string? BesiegedDeploymentWidth { get; set; }
        public static string? BesiegedDeploymentHeight { get; set; }

        public static void Clear()
        {
            BesiegedDeploymentWidth = null;
            BesiegedDeploymentHeight = null;
        }
    }
    internal class SettlementVariant
    {
        public string Key { get; set; } = string.Empty;
        public string X { get; set; } = string.Empty;
        public string Y { get; set; } = string.Empty;
        public List<string>? BesiegerOrientations { get; set; }
        public List<string>? Orientations { get; set; }
        public string? BesiegedDeploymentZoneWidth { get; set; }
        public string? BesiegedDeploymentZoneHeight { get; set; }
    }

    internal class SettlementMap
    {
        public string Faction { get; set; } = string.Empty;
        public string BattleType { get; set; } = string.Empty;
        public List<string> ProvinceNames { get; set; } = new List<string>();
        public List<SettlementVariant> Variants { get; private set; } = new List<SettlementVariant>();
    }

    internal class UniqueSettlementMap
    {
        public string BattleType { get; set; } = string.Empty;
        public List<string> ProvinceNames { get; set; } = new List<string>();
        public List<SettlementVariant> Variants { get; private set; }  = new List<SettlementVariant>();
    }

    internal class SiegeEngine
    {
        public string Key { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int SiegeEffortCost { get; set; }
    }

    class TerrainsUM
    {
        string AttilaMap { get; set; }
        List<(string building, string x, string y)> HistoricalMaps { get; set; }
        List<(string terrain, string x, string y)> NormalMaps { get; set; }
        public List<SettlementMap> SettlementMaps { get; private set; }
        public List<UniqueSettlementMap> UniqueSettlementMaps { get; private set; }
        public List<LandBridgeMap> LandBridgeMaps { get; private set; }
        public List<CoastalMap> CoastalMaps { get; private set; }

        internal TerrainsUM(string attilaMap, List<(string building, string x, string y)> historicalMaps, List<(string terrain, string x, string y)> normalMaps, List<SettlementMap> settlementMaps, List<UniqueSettlementMap> uniqueSettlementMaps, List<LandBridgeMap> landBridgeMaps, List<CoastalMap> coastalMaps)
        {
            AttilaMap = attilaMap;
            HistoricalMaps = historicalMaps;    
            NormalMaps = normalMaps;
            SettlementMaps = settlementMaps;
            UniqueSettlementMaps = uniqueSettlementMaps;
            LandBridgeMaps = landBridgeMaps;
            CoastalMaps = coastalMaps;
        }

        public string GetAttilaMap() { return AttilaMap; }
        public List<(string building, string x, string y)> GetHistoricalMaps() { return HistoricalMaps; }
        public List<(string terrain, string x, string y)> GetNormalMaps() { return NormalMaps; }
        public List<SettlementMap> GetSettlementMaps() { return SettlementMaps; }
        public List<UniqueSettlementMap> GetUniqueSettlementMaps() { return UniqueSettlementMaps; }

    }

    public class AvailableUnit
    {
        public string FactionName { get; set; }
        public string UnitType { get; set; } // e.g., General, Knights, MenAtArm
        public string AttilaUnitKey { get; set; }
        public string DisplayName { get; set; } // For MenAtArm, this will be the 'type' attribute
        public int? Rank { get; set; }
        public int? Level { get; set; }
        public string? MaxCategory { get; set; }
        public bool IsSiege { get; set; }
    }

    internal static class UnitMappers_BETA
    {
        /*----------------------------------------------------------------
         * TO DO:
         * House files reader for AGOT
         ----------------------------------------------------------------*/

        public static List<Submod> AvailableSubmods { get; private set; } = new List<Submod>();
        public static TerrainsUM? Terrains { get;private set; }  
        static string? LoadedUnitMapper_FolderPath { get; set; }
        public static string? ActivePlaythroughTag { get; private set; }
        public const string NOT_FOUND_KEY = "not_found";
        private static readonly Random _random = new Random();
        private static Dictionary<string, (string X, string Y, List<string>? orientations)> _provinceMapCache = new Dictionary<string, (string X, string Y, List<string>? orientations)>();

        public static List<SiegeEngine> SiegeEngines { get; private set; } = new List<SiegeEngine>();

        private static Dictionary<string, XmlDocument>? _factionFileCache;
        private static Dictionary<string, (List<(int porcentage, string unit_key, string name, string max)>, string)>? _levyCache;
        private static Dictionary<string, List<(int percentage, string unit_key, string name, string max, int level)>>? _garrisonCache;

        public static void ClearFactionCache()
        {
            _factionFileCache = null;
            _levyCache = null;
            _garrisonCache = null;
        }

        private static void EnsureFactionCacheLoaded()
        {
            if (_factionFileCache != null) return; // Cache is already loaded

            _factionFileCache = new Dictionary<string, XmlDocument>();
            _levyCache = new Dictionary<string, (List<(int porcentage, string unit_key, string name, string max)>, string)>();
            _garrisonCache = new Dictionary<string, List<(int percentage, string unit_key, string name, string max, int level)>>();

            if (LoadedUnitMapper_FolderPath == null) return;

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);

            foreach (var file in files_paths)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                _factionFileCache[file] = doc;

                foreach (XmlNode factionNode in doc.SelectNodes("/Factions/Faction"))
                {
                    string factionName = factionNode.Attributes?["name"]?.Value ?? string.Empty;
                    if (string.IsNullOrEmpty(factionName)) continue;

                    var levies = Levies(doc, factionName);
                    if (levies.Any())
                    {
                        _levyCache[factionName] = (levies, factionName);
                    }

                    var garrisons = Garrison(doc, factionName);
                    if (garrisons.Any())
                    {
                        _garrisonCache[factionName] = garrisons;
                    }
                }
            }
        }

        private static (List<ModFile> requiredMods, List<Submod> submods) ParseModsFileFromMapperPath(string mapperPath)
        {
            var requiredMods = new List<ModFile>();
            var submods = new List<Submod>();
            string modsPath = Path.Combine(mapperPath, "Mods.xml");
            if (File.Exists(modsPath))
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(modsPath);
                if (xmlDocument.DocumentElement != null)
                {
                    foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                    {
                        if (node is XmlComment) continue;
                        if (node.Name == "Mod")
                        {
                            var modFile = new ModFile
                            {
                                FileName = node.InnerText,
                                Sha256 = node.Attributes?["sha256"]?.Value ?? string.Empty,
                                ScreenName = node.Attributes?["screen_name"]?.Value,
                                Url = node.Attributes?["url"]?.Value
                            };
                            requiredMods.Add(modFile);
                        }
                        else if (node.Name == "Submod")
                        {
                            var submod = new Submod
                            {
                                Tag = node.Attributes?["submod_tag"]?.Value ?? string.Empty,
                                ScreenName = node.Attributes?["screen_name"]?.Value ?? string.Empty,
                            };
                            string? replaceAttr = node.Attributes?["replace"]?.Value;
                            if (!string.IsNullOrEmpty(replaceAttr))
                            {
                                submod.Replaces.AddRange(replaceAttr.Split(',').Select(m => m.Trim()));
                            }
                            foreach (XmlNode submod_modNode in node.ChildNodes)
                            {
                                if (submod_modNode.Name == "Mod")
                                {
                                    var modFile = new ModFile
                                    {
                                        FileName = submod_modNode.InnerText,
                                        Sha256 = submod_modNode.Attributes?["sha256"]?.Value ?? string.Empty,
                                        ScreenName = submod_modNode.Attributes?["screen_name"]?.Value,
                                        Url = submod_modNode.Attributes?["url"]?.Value
                                    };
                                    submod.Mods.Add(modFile);
                                }
                            }
                            if (!string.IsNullOrEmpty(submod.Tag))
                            {
                                submods.Add(submod);
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show($"Mods.xml was not found in {mapperPath}", "Crusader Conflicts: Crusader Conflicts: Unit Mappers Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
            return (requiredMods, submods);
        }
        public static (List<ModFile> requiredMods, List<Submod> submods) GetUnitMappersModsCollectionFromTag(string tag)
        {
            var allRequiredMods = new List<ModFile>();
            var allSubmods = new List<Submod>();

            var unit_mappers_folder = Directory.GetDirectories(@".\unit mappers");
            if (tag == "Custom")
            {
                string selectedMapperTag = ModOptions.GetSelectedCustomMapper();
                if (!string.IsNullOrEmpty(selectedMapperTag))
                {
                    foreach (var mapper in unit_mappers_folder)
                    {
                        string tagFile = Path.Combine(mapper, "tag.txt");
                        if (File.Exists(tagFile))
                        {
                            string fileTag = File.ReadAllText(tagFile).Trim();
                            if (selectedMapperTag.Equals(fileTag, StringComparison.OrdinalIgnoreCase))
                            {
                                var (mods, submods) = ParseModsFileFromMapperPath(mapper);
                                allRequiredMods.AddRange(mods);
                                foreach (var newSubmod in submods)
                                {
                                    var existingSubmod = allSubmods.FirstOrDefault(s => s.Tag == newSubmod.Tag);
                                    if (existingSubmod != null)
                                    {
                                        // Merge Mods, ensuring no duplicates
                                        foreach (var newModFile in newSubmod.Mods)
                                        {
                                            if (!existingSubmod.Mods.Any(m => m.FileName == newModFile.FileName))
                                            {
                                                existingSubmod.Mods.Add(newModFile);
                                            }
                                        }
                                        // Merge Replaces, ensuring no duplicates
                                        foreach (var newReplace in newSubmod.Replaces)
                                        {
                                            if (!existingSubmod.Replaces.Contains(newReplace))
                                            {
                                                existingSubmod.Replaces.Add(newReplace);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        allSubmods.Add(newSubmod);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var mapper in unit_mappers_folder)
                {
                    string tagFile = Path.Combine(mapper, "tag.txt");
                    if (File.Exists(tagFile))
                    {
                        string fileTag = File.ReadAllText(tagFile).Trim();
                        if (tag == fileTag)
                        {
                            var (mods, submods) = ParseModsFileFromMapperPath(mapper);
                            allRequiredMods.AddRange(mods);
                            foreach (var newSubmod in submods)
                            {
                                var existingSubmod = allSubmods.FirstOrDefault(s => s.Tag == newSubmod.Tag);
                                if (existingSubmod != null)
                                {
                                    // Merge Mods, ensuring no duplicates
                                    foreach (var newModFile in newSubmod.Mods)
                                    {
                                        if (!existingSubmod.Mods.Any(m => m.FileName == newModFile.FileName))
                                        {
                                            existingSubmod.Mods.Add(newModFile);
                                        }
                                    }
                                    // Merge Replaces, ensuring no duplicates
                                    foreach (var newReplace in newSubmod.Replaces)
                                    {
                                        if (!existingSubmod.Replaces.Contains(newReplace))
                                        {
                                            existingSubmod.Replaces.Add(newReplace);
                                        }
                                    }
                                }
                                else
                                {
                                    allSubmods.Add(newSubmod);
                                }
                            }
                        }
                    }
                }
            }

            return (allRequiredMods, allSubmods);
        }

        // Fix for CS8602 and CS8600
        public static string? GetLoadedUnitMapperName() { return LoadedUnitMapper_FolderPath is string path ? Path.GetFileName(path) : null; }
        public static string? GetLoadedUnitMapperString() { 
            switch(GetLoadedUnitMapperName())
            {
                case "OfficialCC_DefaultCK3_EarlyMedieval_919Mod":
                    return "EARLY MEDIEVAL";
                case "OfficialCC_DefaultCK3_HighMedieval_MK1212Mod":
                    return "HIGH MEDIEVAL";
                case "OfficialCC_DefaultCK3_LateMedieval_MK1212Mod":
                    return "LATE MEDIEVAL";
                case "OfficialCC_DefaultCK3_Renaissance_MK1212Mod":
                    return "RENAISSANCE";
                case "OfficialCC_TheFallenEagle_AgeOfJustinain":
                    return "DARK AGES";
                case "OfficialCC_TheFallenEagle_FallofTheEagle":
                case "OfficialCC_TheFallenEagle_FireforgedEmpire":
                    return "LATE ANTIQUITY";
                case "OfficialCC_RealmsInExile_TheDawnlessDays":
                    return "SECOND AGE";
                case "OfficialCC_AGOT_SevenKingdoms":
                    return "AGE OF THE TARGARYENS";
                default:
                    return null;
            }
            
        }

        public static void ClearProvinceMapCache()
        {
            _provinceMapCache.Clear();
            Program.Logger.Debug("Province settlement map cache has been cleared for the new battle.");
        }

        public static int GetDeterministicIndex(string input, int listCount)
        {
            if (listCount <= 0) return 0;

            using (SHA256 sha256 = SHA256.Create())
            {
                // 1. Convert the input string to a byte array.
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);

                // 2. Compute the hash.
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // 3. Convert the first 4 bytes of the hash to an integer.
                // This gives us a stable, well-distributed number.
                int hashAsInt = BitConverter.ToInt32(hashBytes, 0);

                // 4. Use the absolute value and the modulo operator to get a valid index.
                int baseIndex = Math.Abs(hashAsInt % listCount);

                int offset = BattleState.AutofixMapVariantOffset;
                if (offset > 0)
                {
                    if (listCount <= 1) return baseIndex; // Cannot find an alternative if there's only one option

                    var alternativeIndices = Enumerable.Range(0, listCount).ToList();
                    alternativeIndices.Remove(baseIndex);

                    int newIndexInAlternatives = (offset - 1) % alternativeIndices.Count;
                    return alternativeIndices[newIndexInAlternatives];
                }
                else
                {
                    return baseIndex;
                }
            }
        }

        private static List<string> GetSortedFilePaths(string directoryPath, string priorityFilePattern)
        {
            var allFiles = Directory.GetFiles(directoryPath, "*.xml").ToList();
            var activeSubmods = string.IsNullOrEmpty(ActivePlaythroughTag)
                ? new List<string>()
                : CrusaderWars.mod_manager.SubmodManager.GetActiveSubmodsForPlaythrough(ActivePlaythroughTag);

            var priorityFiles = new List<string>();
            var otherFiles = new List<string>();
            var submodFiles = new List<string>();
            var addonFiles = new List<string>(); // New list for add-ons

            // Simple wildcard matching for '*' at the end
            string patternStart = priorityFilePattern.TrimEnd('*');

            foreach (var file in allFiles)
            {
                string fileName = Path.GetFileName(file);

                if (!string.IsNullOrEmpty(patternStart) && fileName.StartsWith(patternStart, StringComparison.OrdinalIgnoreCase))
                {
                    priorityFiles.Add(file);
                    continue;
                }

                try
                {
                    using (var reader = XmlReader.Create(file))
                    {
                        reader.MoveToContent();
                        string? addonForTag = reader.GetAttribute("submod_addon_tag");
                        string? submodTag = reader.GetAttribute("submod_tag");

                        if (!string.IsNullOrEmpty(addonForTag) && activeSubmods.Contains(addonForTag))
                        {
                            // File is an add-on for an active sub-mod.
                            addonFiles.Add(file);
                        }
                        else if (!string.IsNullOrEmpty(submodTag) && activeSubmods.Contains(submodTag))
                        {
                            // File is a main file for an active sub-mod.
                            submodFiles.Add(file);
                        }
                        else if (string.IsNullOrEmpty(addonForTag) && string.IsNullOrEmpty(submodTag))
                        {
                            // File has no sub-mod tags, it's an "Other" file.
                            otherFiles.Add(file);
                        }
                        else
                        {
                            // File has a tag for an inactive sub-mod, so we skip it.
                            if (!string.IsNullOrEmpty(addonForTag))
                            {
                                Program.Logger.Debug($"Skipping file '{fileName}' because its submod_addon_tag tag '{addonForTag}' is not active.");
                            }
                            if (!string.IsNullOrEmpty(submodTag))
                            {
                                Program.Logger.Debug($"Skipping file '{fileName}' because its submod_tag '{submodTag}' is not active.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error reading XML file '{fileName}' for sorting/filtering. Skipping. Error: {ex.Message}");
                }
            }

            // Sort all lists alphabetically
            priorityFiles.Sort((a, b) => String.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
            otherFiles.Sort((a, b) => String.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
            submodFiles.Sort((a, b) => String.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
            addonFiles.Sort((a, b) => String.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));

            // Combine the lists in the new specified order
            var combinedList = new List<string>();
            combinedList.AddRange(priorityFiles);
            combinedList.AddRange(otherFiles);
            combinedList.AddRange(submodFiles);
            combinedList.AddRange(addonFiles);
            
            return combinedList;
        }

        private static void ReadTerrainsFile()
        {
            SiegeEngines.Clear(); // Clear the list to prevent duplicate data on re-read

            if (LoadedUnitMapper_FolderPath == null) { Terrains = null; return; }

            string terrainsFolderPath = Path.Combine(LoadedUnitMapper_FolderPath, "terrains");
            if (!Directory.Exists(terrainsFolderPath))
            {
                terrainsFolderPath = Path.Combine(LoadedUnitMapper_FolderPath, "Terrains");
                if (!Directory.Exists(terrainsFolderPath)) 
                { 
                    Terrains = null; 
                    return; 
                }
            }

            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var terrainFiles = GetSortedFilePaths(terrainsFolderPath, priorityFilePattern);

            try
            {
                string attilaMap = "";
                var historicMaps = new Dictionary<string, (string x, string y)>();
                var normalMapsByTerrain = new Dictionary<string, List<(string x, string y)>>();
                var settlementMapsByCompositeKey = new Dictionary<(string faction, string battleType, string provinces), SettlementMap>();
                var uniqueSettlementMapsByCompositeKey = new Dictionary<(string battleType, string provinces), UniqueSettlementMap>();
                var siegeEngines = new Dictionary<string, SiegeEngine>();
                var landBridgeMaps = new List<LandBridgeMap>();
                var coastalMaps = new List<CoastalMap>();

                foreach (var file in terrainFiles)
                {
                    XmlDocument TerrainsFile = new XmlDocument();
                    TerrainsFile.Load(file);
                    if (TerrainsFile.DocumentElement == null) continue;

                    foreach (XmlElement Element in TerrainsFile.DocumentElement.ChildNodes)
                    {
                        if (Element.Name == "Attila_Map" || Element.Name == "Map")
                        {
                            attilaMap = Element.Name == "Map" ? Element.Attributes?["name"]?.Value ?? string.Empty : Element.InnerText;
                        }
                        else if (Element.Name == "Historic_Maps")
                        {
                            foreach (XmlElement historic_map in Element.ChildNodes)
                            {
                                string building = historic_map.Attributes["ck3_building_key"]?.Value ?? string.Empty;
                                if (!string.IsNullOrEmpty(building))
                                {
                                    historicMaps[building] = (historic_map.Attributes["x"]?.Value ?? string.Empty, historic_map.Attributes["y"]?.Value ?? string.Empty);
                                }
                            }
                        }
                        else if (Element.Name == "Normal_Maps")
                        {
                            foreach (XmlElement terrain_type in Element.ChildNodes)
                            {
                                string terrain = terrain_type.Attributes["ck3_name"]?.Value ?? string.Empty;
                                if (!string.IsNullOrEmpty(terrain))
                                {
                                    var mapsForTerrain = new List<(string x, string y)>();
                                    foreach (XmlElement map in terrain_type.ChildNodes)
                                    {
                                        mapsForTerrain.Add((map.Attributes["x"]?.Value ?? string.Empty, map.Attributes["y"]?.Value ?? string.Empty));
                                    }
                                    normalMapsByTerrain[terrain] = mapsForTerrain;
                                }
                            }
                        }
                        else if (Element.Name == "Settlement_Maps")
                        {
                            foreach (XmlElement settlementNode in Element.ChildNodes)
                            {
                                if (settlementNode.Name == "Settlement")
                                {
                                    var settlementMap = new SettlementMap
                                    {
                                        Faction = settlementNode.Attributes?["faction"]?.Value ?? string.Empty,
                                        BattleType = settlementNode.Attributes?["battle_type"]?.Value ?? string.Empty
                                    };
                                    string provinceNamesAttr = settlementNode.Attributes?["province_names"]?.Value ?? "";
                                    if (!string.IsNullOrEmpty(provinceNamesAttr))
                                    {
                                        settlementMap.ProvinceNames.AddRange(provinceNamesAttr.Split(',').Select(p => p.Trim()));
                                    }

                                    foreach (XmlElement variantNode in settlementNode.ChildNodes)
                                    {
                                        if (variantNode.Name == "Variant")
                                        {
                                            var settlementVariant = new SettlementVariant
                                            {
                                                Key = variantNode.Attributes?["key"]?.Value ?? string.Empty,
                                                BesiegedDeploymentZoneWidth = variantNode.Attributes?["besieged_deployment_zone_width"]?.Value,
                                                BesiegedDeploymentZoneHeight = variantNode.Attributes?["besieged_deployment_zone_height"]?.Value
                                            };

                                            string? orientationsAttr = variantNode.Attributes?["besieger_orientations"]?.Value;
                                            if (!string.IsNullOrEmpty(orientationsAttr))
                                            {
                                                settlementVariant.BesiegerOrientations = orientationsAttr.Split(',').Select(o => o.Trim()).ToList();
                                            }

                                            XmlElement? mapNode = variantNode.SelectSingleNode("Map") as XmlElement;
                                            if (mapNode != null)
                                            {
                                                settlementVariant.X = mapNode.Attributes?["x"]?.Value ?? string.Empty;
                                                settlementVariant.Y = mapNode.Attributes?["y"]?.Value ?? string.Empty;
                                            }
                                            settlementMap.Variants.Add(settlementVariant);
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(settlementMap.Faction) && !string.IsNullOrEmpty(settlementMap.BattleType))
                                    {
                                        var key = (settlementMap.Faction, settlementMap.BattleType, provinceNamesAttr);
                                        settlementMapsByCompositeKey[key] = settlementMap;
                                    }
                                }
                            }
                        }
                        else if (Element.Name == "Settlement_Maps_Unique")
                        {
                            foreach (XmlElement settlementUniqueNode in Element.ChildNodes)
                            {
                                if (settlementUniqueNode.Name == "Settlement_Unique")
                                {
                                    var uniqueSettlementMap = new UniqueSettlementMap
                                    {
                                        BattleType = settlementUniqueNode.Attributes?["battle_type"]?.Value ?? string.Empty
                                    };
                                    string provinceNamesAttr = settlementUniqueNode.Attributes?["province_names"]?.Value ?? "";
                                    if (!string.IsNullOrEmpty(provinceNamesAttr))
                                    {
                                        uniqueSettlementMap.ProvinceNames.AddRange(provinceNamesAttr.Split(',').Select(p => p.Trim()));
                                    }

                                    foreach (XmlElement variantNode in settlementUniqueNode.ChildNodes)
                                    {
                                        if (variantNode.Name == "Variant")
                                        {
                                            var settlementVariant = new SettlementVariant
                                            {
                                                Key = variantNode.Attributes?["key"]?.Value ?? string.Empty,
                                                BesiegedDeploymentZoneWidth = variantNode.Attributes?["besieged_deployment_zone_width"]?.Value,
                                                BesiegedDeploymentZoneHeight = variantNode.Attributes?["besieged_deployment_zone_height"]?.Value
                                            };

                                            string? orientationsAttr = variantNode.Attributes?["besieger_orientations"]?.Value;
                                            if (!string.IsNullOrEmpty(orientationsAttr))
                                            {
                                                settlementVariant.BesiegerOrientations = orientationsAttr.Split(',').Select(o => o.Trim()).ToList();
                                            }

                                            XmlElement? mapNode = variantNode.SelectSingleNode("Map") as XmlElement;
                                            if (mapNode != null)
                                            {
                                                settlementVariant.X = mapNode.Attributes?["x"]?.Value ?? string.Empty;
                                                settlementVariant.Y = mapNode.Attributes?["y"]?.Value ?? string.Empty;
                                            }
                                            uniqueSettlementMap.Variants.Add(settlementVariant);
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(uniqueSettlementMap.BattleType) && !string.IsNullOrEmpty(provinceNamesAttr))
                                    {
                                        var key = (uniqueSettlementMap.BattleType, provinceNamesAttr);
                                        uniqueSettlementMapsByCompositeKey[key] = uniqueSettlementMap;
                                    }
                                }
                            }
                        }
                        else if (BattleState.IsSiegeBattle && Element.Name == "Siege_Engines")
                        {
                            foreach (XmlElement siegeEngineNode in Element.ChildNodes)
                            {
                                if (siegeEngineNode.Name == "Siege_Engine")
                                {
                                    var siegeEngine = new SiegeEngine
                                    {
                                        Key = siegeEngineNode.Attributes?["key"]?.Value ?? string.Empty,
                                        Type = siegeEngineNode.Attributes?["type"]?.Value ?? string.Empty
                                    };

                                    if (siegeEngineNode.Attributes?["siege_effort_cost"]?.Value is string costStr && int.TryParse(costStr, out int cost))
                                    {
                                        siegeEngine.SiegeEffortCost = cost;
                                    }
                                    else
                                    {
                                        Program.Logger.Debug($"Warning: Missing or invalid 'siege_effort_cost' for siege engine '{siegeEngine.Key}'. Defaulting to 0.");
                                        siegeEngine.SiegeEffortCost = 0;
                                    }
                                    if (!string.IsNullOrEmpty(siegeEngine.Key))
                                    {
                                        siegeEngines[siegeEngine.Key] = siegeEngine;
                                    }
                                }
                            }
                        }
                        else if (Element.Name == "Land_Bridge_Maps")
                        {
                            foreach (XmlElement landBridgeNode in Element.ChildNodes)
                            {
                                if (landBridgeNode.Name == "Land_Bridge")
                                {
                                    var landBridgeMap = new LandBridgeMap
                                    {
                                        Name = landBridgeNode.Attributes?["name"]?.Value ?? string.Empty,
                                        ProvinceFrom = landBridgeNode.Attributes?["ck3_province_from"]?.Value ?? string.Empty,
                                        ProvinceTo = landBridgeNode.Attributes?["ck3_province_to"]?.Value ?? string.Empty,
                                        CK3Type = landBridgeNode.Attributes?["ck3_type"]?.Value ?? string.Empty,
                                        BattleType = landBridgeNode.Attributes?["battle_type"]?.Value ?? string.Empty,
                                    };

                                    foreach (XmlElement variantNode in landBridgeNode.ChildNodes)
                                    {
                                        if (variantNode.Name == "Variant")
                                        {
                                            var variant = new SettlementVariant
                                            {
                                                Key = variantNode.Attributes?["key"]?.Value ?? string.Empty,
                                            };
                                            string? orientationsAttr = variantNode.Attributes?["orientations"]?.Value;
                                            if (!string.IsNullOrEmpty(orientationsAttr))
                                            {
                                                variant.Orientations = orientationsAttr.Split(',').Select(o => o.Trim()).ToList();
                                            }
                                            XmlElement? mapNode = variantNode.SelectSingleNode("Map") as XmlElement;
                                            if (mapNode != null)
                                            {
                                                variant.X = mapNode.Attributes?["x"]?.Value ?? string.Empty;
                                                variant.Y = mapNode.Attributes?["y"]?.Value ?? string.Empty;
                                            }
                                            landBridgeMap.Variants.Add(variant);
                                        }
                                    }
                                    landBridgeMaps.Add(landBridgeMap);
                                }
                            }
                        }
                        else if (Element.Name == "Coastal_Maps")
                        {
                            foreach (XmlElement coastalNode in Element.ChildNodes)
                            {
                                if (coastalNode.Name == "Coastal")
                                {
                                    var coastalMap = new CoastalMap
                                    {
                                        Name = coastalNode.Attributes?["name"]?.Value ?? string.Empty,
                                        Province = coastalNode.Attributes?["ck3_province"]?.Value ?? string.Empty,
                                        BattleType = coastalNode.Attributes?["battle_type"]?.Value ?? string.Empty,
                                    };

                                    foreach (XmlElement variantNode in coastalNode.ChildNodes)
                                    {
                                        if (variantNode.Name == "Variant")
                                        {
                                            var variant = new SettlementVariant
                                            {
                                                Key = variantNode.Attributes?["key"]?.Value ?? string.Empty,
                                            };
                                            string? orientationsAttr = variantNode.Attributes?["orientations"]?.Value;
                                            if (!string.IsNullOrEmpty(orientationsAttr))
                                            {
                                                variant.Orientations = orientationsAttr.Split(',').Select(o => o.Trim()).ToList();
                                            }
                                            XmlElement? mapNode = variantNode.SelectSingleNode("Map") as XmlElement;
                                            if (mapNode != null)
                                            {
                                                variant.X = mapNode.Attributes?["x"]?.Value ?? string.Empty;
                                                variant.Y = mapNode.Attributes?["y"]?.Value ?? string.Empty;
                                            }
                                            coastalMap.Variants.Add(variant);
                                        }
                                    }
                                    coastalMaps.Add(coastalMap);
                                }
                            }
                        }
                    }
                }

                var historicMapsList = historicMaps.Select(kvp => (kvp.Key, kvp.Value.x, kvp.Value.y)).ToList();
                var normalMapsList = normalMapsByTerrain.SelectMany(kvp => kvp.Value.Select(map => (terrain: kvp.Key, x: map.x, y: map.y))).ToList();
                var settlementMapsList = settlementMapsByCompositeKey.Values.ToList();
                var uniqueSettlementMapsList = uniqueSettlementMapsByCompositeKey.Values.ToList();
                SiegeEngines.AddRange(siegeEngines.Values);

                Terrains = new TerrainsUM(attilaMap, historicMapsList, normalMapsList, settlementMapsList, uniqueSettlementMapsList, landBridgeMaps, coastalMaps);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading {GetLoadedUnitMapperName()} terrains file: {ex.Message}", "Crusader Conflicts: Unit Mapper Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        public static List<string> GetUnitMapperModFromTagAndTimePeriod(string tag, List<string> activeSubmods)
        {
            ActivePlaythroughTag = tag;
            AvailableSubmods.Clear();
            List<string> requiredMods = new List<string>();

            var unit_mappers_folder = Directory.GetDirectories(@".\unit mappers");
            if (tag == "Custom")
            {
                string customMapperTag = ModOptions.GetSelectedCustomMapper();
                if (string.IsNullOrEmpty(customMapperTag))
                {
                    LoadedUnitMapper_FolderPath = null;
                    Terrains = null;
                    return requiredMods;
                }

                var matchingMappers = new List<string>();
                foreach (var mapper in unit_mappers_folder)
                {
                    string tagFilePath = Path.Combine(mapper, "tag.txt");
                    if (File.Exists(tagFilePath))
                    {
                        string fileTag = File.ReadAllText(tagFilePath).Trim();
                        if (customMapperTag.Equals(fileTag, StringComparison.OrdinalIgnoreCase))
                        {
                            matchingMappers.Add(mapper);
                        }
                    }
                }

                if (matchingMappers.Any())
                {
                    foreach (var mapperPath in matchingMappers)
                    {
                        var mods = ProcessMapper(mapperPath);
                        if (mods.Any()) // ProcessMapper returns empty list on time period mismatch
                        {
                            requiredMods.AddRange(mods); // Aggregate mods
                        }
                    }
                }

                if (!requiredMods.Any()) // If no mods were found after checking all custom mappers
                {
                    MessageBox.Show($"The selected custom unit mapper '{customMapperTag}' does not have a valid configuration for the current in-game year ({Date.Year}).", "Crusader Conflicts: Configuration Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                }
                return requiredMods;
            }

            foreach (var mapper in unit_mappers_folder)
            {
                string tagFilePath = Path.Combine(mapper, "tag.txt");
                if (File.Exists(tagFilePath))
                {
                    string fileTag = File.ReadAllText(tagFilePath).Trim();
                    if (tag == fileTag)
                    {
                        var mods = ProcessMapper(mapper);
                        if (mods.Any()) // ProcessMapper returns empty list on time period mismatch
                        {
                            requiredMods.AddRange(mods); // Aggregate mods
                        }
                    }
                }
            }

            return requiredMods; // Return empty list if no suitable mapper is found
        }

        private static List<string> ProcessMapper(string mapperPath)
        {
            List<string> requiredMods = new List<string>();

            // TIME PERIOD CHECK
            int startYear = -1, endYear = -1;
            bool isDefault = false;
            string timePeriodPath = Path.Combine(mapperPath, "Time Period.xml");
            if (!File.Exists(timePeriodPath))
            {
                timePeriodPath = Path.Combine(mapperPath, "TimePeriod.xml");
            }

            if (File.Exists(timePeriodPath))
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(timePeriodPath);
                if (xmlDocument.DocumentElement == null) return requiredMods;

                string startYearStr = xmlDocument.SelectSingleNode("//StartDate")?.InnerText ?? "Default";
                string endYearStr = xmlDocument.SelectSingleNode("//EndDate")?.InnerText ?? "Default";

                if (startYearStr.Equals("Default", StringComparison.OrdinalIgnoreCase)) isDefault = true;
                if (!int.TryParse(startYearStr, out startYear)) isDefault = true;
                if (!int.TryParse(endYearStr, out endYear)) isDefault = true;

                if (isDefault) { startYear = 0; endYear = 9999; }

                if ((Date.Year >= startYear && Date.Year <= endYear))
                {
                    // MODS and SUBMODS
                    string modsPath = Path.Combine(mapperPath, "Mods.xml");
                    if (File.Exists(modsPath))
                    {
                        XmlDocument xmlMods = new XmlDocument();
                        xmlMods.Load(modsPath);
                        if (xmlMods.DocumentElement != null)
                        {
                            foreach (XmlNode node in xmlMods.DocumentElement.ChildNodes)
                            {
                                if (node is XmlComment) continue;
                                if (node.Name == "Mod")
                                {
                                    requiredMods.Add(node.InnerText);
                                }
                                else if (node.Name == "Submod")
                                {
                                    var submod = new Submod
                                    {
                                        Tag = node.Attributes?["submod_tag"]?.Value ?? string.Empty,
                                        ScreenName = node.Attributes?["screen_name"]?.Value ?? string.Empty,
                                    };
                                    string? replaceAttr = node.Attributes?["replace"]?.Value;
                                    if (!string.IsNullOrEmpty(replaceAttr))
                                    {
                                        submod.Replaces.AddRange(replaceAttr.Split(',').Select(m => m.Trim()));
                                    }
                                    foreach (XmlNode submod_modNode in node.ChildNodes)
                                    {
                                        if (submod_modNode.Name == "Mod")
                                        {
                                            var modFile = new ModFile
                                            {
                                                FileName = submod_modNode.InnerText,
                                                Sha256 = submod_modNode.Attributes?["sha256"]?.Value ?? string.Empty,
                                                ScreenName = submod_modNode.Attributes?["screen_name"]?.Value,
                                                Url = submod_modNode.Attributes?["url"]?.Value
                                            };
                                            submod.Mods.Add(modFile);
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(submod.Tag))
                                    {
                                        AvailableSubmods.Add(submod);
                                    }
                                }
                            }
                        }
                        LoadedUnitMapper_FolderPath = mapperPath; // This will only store the last loaded mapper path.
                        ReadTerrainsFile(); // This will only read terrains from the last loaded mapper path.
                    }
                    else
                    {
                        MessageBox.Show($"Mods.xml was not found in {mapperPath}", "Crusader Conflicts: Unit Mappers Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                    }
                }
            }
            else
            {
                MessageBox.Show($"Time Period.xml or TimePeriod.xml was not found in {mapperPath}", "Crusader Conflicts: Unit Mappers Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }

            return requiredMods;
        }

        public static LandBridgeMap? GetLandBridgeMap(string provinceId)
        {
            if (Terrains?.LandBridgeMaps == null) return null;

            return Terrains.LandBridgeMaps.FirstOrDefault(lb => lb.ProvinceFrom == provinceId || lb.ProvinceTo == provinceId);
        }

        public static CoastalMap? GetCoastalMap(string provinceId)
        {
            if (Terrains?.CoastalMaps == null) return null;

            return Terrains.CoastalMaps.FirstOrDefault(cm => cm.Province == provinceId);
        }

        struct MaxType
        {
            public static int GetMax(string MaxValue)
            {
                switch (MaxValue)
                {
                    case "INFANTRY":
                        return ModOptions.GetInfantryMax();
                    case "RANGED":
                        return ModOptions.GetRangedMax();
                    case "CAVALRY":
                        return ModOptions.GetCavalryMax();
                    case "LEVY":
                        return ModOptions.GetLevyMax();
                    case "SPECIAL":
                        return 1111;
                    default:
                        if (int.TryParse(MaxValue, out int max_int)) 
                            return max_int;
                        else
                            return ModOptions.GetInfantryMax();

                }
            }
        };
        public static int GetMax(Unit unit)
        {
            Program.Logger.Debug($"Entering GetMax for unit: {unit.GetName()} (Type: {unit.GetRegimentType()}, Faction: {unit.GetAttilaFaction()})");

            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("Error: LoadedUnitMapper_FolderPath is not set. Cannot get unit max.");
                return 0; // Or throw an exception
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);

            int max = 0;
            foreach (var xml_file in files_paths)
            {
                if (Path.GetExtension(xml_file) == ".xml")
                {
                    Program.Logger.Debug($"Processing XML file: {Path.GetFileName(xml_file)}");
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue; // Added null check

                    foreach (XmlNode element in FactionsFile.DocumentElement.ChildNodes)
                    {
                        if (element is XmlComment) continue;
                        string faction = element.Attributes?["name"]?.Value ?? string.Empty;
                        // Program.Logger.Debug($"Checking faction node: {faction}");

                        //Store Default unit max first
                        if (faction == "Default" || faction == "DEFAULT")
                        {
                            Program.Logger.Debug($"Matched 'Default' faction.");
                            foreach (XmlNode node in element.ChildNodes)
                            {
                                if (node is XmlComment) continue;
                                if (node.Name == "General" || node.Name == "Knights") continue;

                                if (node.Name == "Levies" &&
                                    node.Attributes?["max"] != null)
                                {
                                    if (unit.GetRegimentType() == RegimentType.Levy)
                                    {
                                        string maxAttrValue = node.Attributes["max"]!.Value;
                                        max = MaxType.GetMax(maxAttrValue);
                                        Program.Logger.Debug($"Assigned max for Default Levy (from attribute '{maxAttrValue}'): {max}");
                                        continue;
                                    }
                                    else
                                        continue;
                                }
                                // New Garrison handling
                                if (node.Name == "Garrison" && node.Attributes?["max"] != null)
                                {
                                    if (unit.GetRegimentType() == RegimentType.Garrison) // Changed from unit.GetName() == "Garrison"
                                    {
                                        string maxAttrValue = node.Attributes["max"]!.Value;
                                        max = MaxType.GetMax(maxAttrValue);
                                        Program.Logger.Debug($"Assigned max for Default Garrison (from attribute '{maxAttrValue}'): {max}");
                                        continue;
                                    }
                                    else
                                        continue;
                                }

                                if (node.Attributes?["type"]?.Value == unit.GetName())
                                {
                                    // Original code: max = MaxType.GetMax(node.Attributes["type"].Value);
                                    // This line is potentially problematic as "type" attribute is unit name, not a max category or number.
                                    // Logging the input and result as per instruction to not alter logic.
                                    string inputToMaxType = node.Attributes["type"]!.Value; // Null-forgiving operator added as per instruction
                                    max = MaxType.GetMax(inputToMaxType);
                                    Program.Logger.Debug($"Assigned max for Default unit '{unit.GetName()}' (input to MaxType.GetMax: '{inputToMaxType}'): {max}");
                                }
                            }
                        }
                        //Then stores culture specific unit max
                        else if (faction == unit.GetAttilaFaction())
                        {
                            Program.Logger.Debug($"Matched specific faction: {faction}.");
                            foreach (XmlNode node in element.ChildNodes)
                            {
                                if (node is XmlComment) continue;
                                if (node.Name == "General" || node.Name == "Knights") continue;

                                if (node.Name == "Levies")
                                {
                                    if(unit.GetRegimentType() == RegimentType.Levy && node.Attributes?["max"] != null) {
                                        string maxAttrValue = node.Attributes["max"]!.Value; 
                                        max = MaxType.GetMax(maxAttrValue); 
                                        Program.Logger.Debug($"Assigned max for specific faction '{faction}' Levy (from attribute '{maxAttrValue}'): {max}");
                                        continue;
                                    }
                                    else
                                        continue;
                                }
                                // New Garrison handling
                                if (node.Name == "Garrison")
                                {
                                    if (unit.GetRegimentType() == RegimentType.Garrison && node.Attributes?["max"] != null) // Changed from unit.GetName() == "Garrison"
                                    {
                                        string maxAttrValue = node.Attributes["max"]!.Value;
                                        max = MaxType.GetMax(maxAttrValue);
                                        Program.Logger.Debug($"Assigned max for specific faction '{faction}' Garrison (from attribute '{maxAttrValue}'): {max}");
                                        continue;
                                    }
                                    else
                                        continue;
                                }

                                // Line 187 - Add null check
                                if (node?.Attributes?["type"]?.Value == unit.GetName())
                                {
                                    if(node?.Attributes?["max"] != null)
                                    {
                                        string maxAttrValue = node.Attributes["max"]!.Value;
                                        max = MaxType.GetMax(maxAttrValue);
                                        Program.Logger.Debug($"Assigned max for specific faction '{faction}' unit '{unit.GetName()}' (from attribute '{maxAttrValue}'): {max}");
                                    }
                                    else
                                    {
                                        Program.Logger.Debug($"WARNING: Unit '{unit.GetName()}' in faction '{faction}' found, but 'max' attribute is missing. Keeping previous max value: {max}");
                                        break;
                                    }   
                                }
                            }
                        }
                    }
                }
            }

            Program.Logger.Debug($"Exiting GetMax for unit: {unit.GetName()}. Final max value: {max}");
            if (max == 0)
            {
                Program.Logger.Debug($"WARNING: GetMax returned 0 for unit '{unit.GetName()}'. This might indicate a missing mapping.");
            }
            return max;
        }

        public static string GetSubculture(string attila_faction)
        {
            Program.Logger.Debug($"Getting subculture for faction: '{attila_faction}'");
            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("Error: LoadedUnitMapper_FolderPath is not set. Cannot get subculture.");
                return ""; // Return empty string if not found
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);

            string specific_subculture = "";
            string default_subculture = "";

            foreach (var xml_file in files_paths)
            {
                if (Path.GetExtension(xml_file) == ".xml")
                {
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue;

                    // Check for specific faction
                    XmlNode? specificNode = FactionsFile.SelectSingleNode($"/Factions/Faction[@name='{attila_faction}']");
                    if (specificNode?.Attributes?["subculture"]?.Value is string foundSpecificSubculture)
                    {
                        specific_subculture = foundSpecificSubculture;
                        Program.Logger.Debug($"Found specific subculture '{specific_subculture}' for faction '{attila_faction}' in file '{Path.GetFileName(xml_file)}'.");
                    }

                    // Check for default faction
                    XmlNode? defaultNode = FactionsFile.SelectSingleNode($"/Factions/Faction[@name='Default']");
                    if (defaultNode?.Attributes?["subculture"]?.Value is string foundDefaultSubculture)
                    {
                        default_subculture = foundDefaultSubculture;
                        Program.Logger.Debug($"Found default subculture '{default_subculture}' in file '{Path.GetFileName(xml_file)}'.");
                    }
                }
            }

            if (!string.IsNullOrEmpty(specific_subculture))
            {
                return specific_subculture;
            }

            if (!string.IsNullOrEmpty(default_subculture))
            {
                Program.Logger.Debug($"No specific subculture found for faction '{attila_faction}'. Using default subculture '{default_subculture}'.");
                return default_subculture;
            }

            Program.Logger.Debug($"WARNING: No subculture found for faction '{attila_faction}' or for 'Default' faction.");
            return ""; // Return empty if nothing found
        }

        static List<(int porcentage, string unit_key, string name, string max)> Levies(XmlDocument factions_file, string attila_faction)
        {
            var levies_nodes = factions_file.SelectNodes($"/Factions/Faction[@name=\"{attila_faction}\"]/Levies");
            List<(int porcentage, string unit_key, string name, string max)> list = new List<(int porcentage, string unit_key, string name, string max)>();

            if (levies_nodes?.Count == 0) 
                return list;


            foreach (XmlNode levies_node in levies_nodes!)
            {
                int porcentage = 0;
                string key = string.Empty;
                string name = string.Empty;
                string max = MaxType.GetMax("LEVY").ToString();

                if (levies_node.Attributes?["percentage"]?.Value is string porcentageStr && Int32.TryParse(porcentageStr, out int parsedPorcentage))
                {
                    porcentage = parsedPorcentage;
                }
                else
                {
                    Program.Logger.Debug($"WARNING: Missing or invalid 'percentage' attribute for levy in faction '{attila_faction}'. Defaulting to 0.");
                }

                if (levies_node.Attributes?["key"]?.Value is string keyStr)
                {
                    key = keyStr;
                }
                else
                {
                    Program.Logger.Debug($"WARNING: Missing 'key' attribute for levy in faction '{attila_faction}'. Defaulting to empty string.");
                }

                name = $"Levy_{porcentage}";

                if (levies_node.Attributes?["max"] != null)
                    max = MaxType.GetMax(levies_node.Attributes["max"]!.Value).ToString();

                list.Add((porcentage, key, name, max));
            }

            return list;
        }

        private static List<(int percentage, string unit_key, string name, string max, int level)> Garrison(XmlDocument factions_file, string attila_faction)
        {
            var garrison_nodes = factions_file.SelectNodes($"/Factions/Faction[@name=\"{attila_faction}\"]/Garrison");
            List<(int percentage, string unit_key, string name, string max, int level)> list = new List<(int percentage, string unit_key, string name, string max, int level)>();

            if (garrison_nodes?.Count == 0)
                return list;

            foreach (XmlNode garrison_node in garrison_nodes!)
            {
                int percentage = 0;
                string key = string.Empty;
                string name = string.Empty;
                string max = MaxType.GetMax("LEVY").ToString(); // Garrisons are typically levies, use Levy max as default
                int level = 1; // Default level

                if (garrison_node.Attributes?["percentage"]?.Value is string percentageStr && Int32.TryParse(percentageStr, out int parsedPercentage))
                {
                    percentage = parsedPercentage;
                }
                else
                {
                    Program.Logger.Debug($"WARNING: Missing or invalid 'percentage' attribute for garrison in faction '{attila_faction}'. Defaulting to 0.");
                }

                if (garrison_node.Attributes?["key"]?.Value is string keyStr && !string.IsNullOrEmpty(keyStr))
                {
                    key = keyStr;
                }
                else
                {
                    Program.Logger.Debug($"WARNING: Missing or empty 'key' attribute for a garrison entry in faction '{attila_faction}'. This entry will be skipped.");
                    continue;
                }

                if (garrison_node.Attributes?["level"]?.Value is string levelStr && Int32.TryParse(levelStr, out int parsedLevel))
                {
                    level = parsedLevel;
                }
                else
                {
                    Program.Logger.Debug($"WARNING: Missing or invalid 'level' attribute for garrison in faction '{attila_faction}'. Defaulting to 1.");
                }

                name = $"Garrison_{percentage}"; // Naming convention for garrison units

                if (garrison_node.Attributes?["max"] != null)
                    max = MaxType.GetMax(garrison_node.Attributes["max"]!.Value).ToString();

                list.Add((percentage, key, name, max, level));
            }

            return list;
        }


        public static (List<(int porcentage, string unit_key, string name, string max)>, string) GetFactionLevies(string attila_faction)
        {
            Program.Logger.Debug($"Getting faction levies: '{attila_faction}'");
            EnsureFactionCacheLoaded();

            if (_levyCache != null && _levyCache.TryGetValue(attila_faction, out var specificLevies))
            {
                Program.Logger.Debug($"Found specific levy definitions for faction '{attila_faction}' in cache.");
                return specificLevies;
            }

            if (_levyCache != null && _levyCache.TryGetValue("Default", out var defaultLevies))
            {
                Program.Logger.Debug($"No specific levy definitions found for faction '{attila_faction}'. Using 'Default' definitions from cache.");
                return defaultLevies;
            }

            throw new Exception($"Unit Mapper Error: Could not find any levy definitions for faction '{attila_faction}' or for the 'Default' faction. Please check your unit mapper configuration.");
        }

        public static List<(int percentage, string unit_key, string name, string max)> GetFactionGarrison(string attila_faction, int holdingLevel) // refactored
        {
            if (!BattleState.IsSiegeBattle)
            {
                Program.Logger.Debug("GetFactionGarrison called for a non-siege battle. Returning empty list as a safeguard.");
                return new List<(int, string, string, string)>();
            }

            Program.Logger.Debug($"Getting faction garrison for: '{attila_faction}' at holding level: {holdingLevel}");
            EnsureFactionCacheLoaded();

            List<(int percentage, string unit_key, string name, string max, int level)> garrisonDefinitions = new List<(int percentage, string unit_key, string name, string max, int level)>();

            if (_garrisonCache != null && _garrisonCache.TryGetValue(attila_faction, out var specificGarrisons))
            {
                Program.Logger.Debug($"Found specific garrison definitions for faction '{attila_faction}' in cache.");
                garrisonDefinitions = specificGarrisons;
            }
            else if (_garrisonCache != null && _garrisonCache.TryGetValue("Default", out var defaultGarrisons))
            {
                Program.Logger.Debug($"No specific garrison definitions found for faction '{attila_faction}'. Using 'Default' definitions from cache.");
                garrisonDefinitions = defaultGarrisons;
            }

            List<(int percentage, string unit_key, string name, string max)> finalGarrisonComposition;

            // Attempt to find garrisons at or below the current holding level
            var suitableGarrisons = garrisonDefinitions.Where(g => g.level <= holdingLevel).ToList();

            if (suitableGarrisons.Any())
            {
                // If suitable garrisons are found, use the highest level among them
                int highestAvailableLevel = suitableGarrisons.Max(g => g.level);
                Program.Logger.Debug($"Found suitable garrisons. Using highest available level: {highestAvailableLevel}");
                finalGarrisonComposition = suitableGarrisons.Where(g => g.level == highestAvailableLevel).Select(g => (g.percentage, g.unit_key, g.name, g.max)).ToList();
            }
            else
            {
                // Fallback: No garrisons at or below the holding level, so use the lowest level available
                if (!garrisonDefinitions.Any())
                {
                    throw new Exception($"Unit Mapper Error: No garrison definitions found at all for faction '{attila_faction}'.");
                }

                int lowestAvailableLevel = garrisonDefinitions.Min(g => g.level);
                Program.Logger.Debug($"No garrisons found at or below holding level {holdingLevel}. Falling back to lowest available level: {lowestAvailableLevel}");
                finalGarrisonComposition = garrisonDefinitions.Where(g => g.level == lowestAvailableLevel).Select(g => (g.percentage, g.unit_key, g.name, g.max)).ToList();
            }

            if (!finalGarrisonComposition.Any())
            {
                throw new Exception($"Unit Mapper Error: After filtering, no garrison units remain for faction '{attila_faction}'. This indicates a configuration issue.");
            }

            Program.Logger.Debug($"Final garrison composition for '{attila_faction}' includes {finalGarrisonComposition.Count} unit types.");
            return finalGarrisonComposition;
        }


        public static string GetGarrisonUnit(string unitType, string culture, string heritage)
        {
            string attilaFaction = GetAttilaFaction(culture, heritage);
            // This method is now deprecated as garrison units are directly provided by GetFactionGarrison
            // However, if it's still called, we need a fallback.
            // For now, it will return NOT_FOUND_KEY, as the new system provides full unit keys.
            Program.Logger.Debug($"WARNING: GetGarrisonUnit is deprecated. Called for unitType: {unitType}, culture: {culture}, heritage: {heritage}. Returning NOT_FOUND_KEY.");
            return NOT_FOUND_KEY;
        }

        static (string, bool) SearchInTitlesFile(Unit unit)
        {
            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("Error: LoadedUnitMapper_FolderPath is not set. Cannot search in titles file.");
                return (NOT_FOUND_KEY, false);
            }

            string titles_folder_path = LoadedUnitMapper_FolderPath + @"\Titles";
            if (!Directory.Exists(titles_folder_path)) return (NOT_FOUND_KEY, false);
            
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(titles_folder_path, priorityFilePattern);

            var owner = unit.GetOwner();
            if(owner == null || owner.GetPrimaryTitleKey() == string.Empty)
                return (NOT_FOUND_KEY, false);
            
            //LEVIES skip
            if (unit.GetRegimentType() == RegimentType.Levy) return (NOT_FOUND_KEY, false);
            //Garrison units also skip this, as their keys are set directly
            if (unit.GetRegimentType() == RegimentType.Garrison) return (NOT_FOUND_KEY, false); // Changed from unit.GetName() == "Garrison"

            (string key, bool isSiege) unit_data = (NOT_FOUND_KEY, false);
            foreach (var xml_file in files_paths)
            {
                if (Path.GetExtension(xml_file) == ".xml")
                {
                    XmlDocument TitlesFile = new XmlDocument();
                    TitlesFile.Load(xml_file);
                    if (TitlesFile.DocumentElement == null) continue; // Added null check

                    //MAA|COMMANDER|KNIGHT
                    foreach (XmlNode element in TitlesFile.DocumentElement.ChildNodes)
                    {
                        if (element as XmlNode is XmlComment) continue;
                        string? titleKey = element.Attributes?["title_key"]?.Value;

                        //Then stores culture specific unit key
                        if (titleKey != null && titleKey == owner.GetPrimaryTitleKey())
                        {
                            var found_data = FindUnitKeyInFaction(element, unit, null);
                            if (found_data.Item1 != NOT_FOUND_KEY)
                            {
                                unit_data = found_data;
                            }
                        }
                    }
                }
            }

            return unit_data;
        }

        static (string, bool) SearchInFactionFiles(Unit unit)
        {
            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("Error: LoadedUnitMapper_FolderPath is not set. Cannot search in faction files.");
                return (NOT_FOUND_KEY, false);
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            files_paths.Reverse(); // Search from last-loaded (submods) to first (OfficialCC)

            //LEVIES skip
            if (unit.GetRegimentType() == RegimentType.Levy) return (NOT_FOUND_KEY, false) ;
            //Garrison units also skip this, as their keys are set directly
            if (unit.GetRegimentType() == RegimentType.Garrison) return (NOT_FOUND_KEY, false); // Changed from unit.GetName() == "Garrison"

            bool isPotentiallySiege = false; // Latch to remember siege status

            // Priority 1: Search for a specific faction mapping in reverse file order
            foreach (var xml_file in files_paths)
            {
                XmlDocument FactionsFile = new XmlDocument();
                FactionsFile.Load(xml_file);
                XmlNode? factionNode = FactionsFile.SelectSingleNode($"/Factions/Faction[@name='{unit.GetAttilaFaction()}']");

                if (factionNode != null)
                {
                    var (foundKey, foundIsSiege) = FindUnitKeyInFaction(factionNode, unit, null);
                    if (foundIsSiege) isPotentiallySiege = true; // Remember if it's ever marked as siege
                    if (foundKey != NOT_FOUND_KEY)
                    {
                        Program.Logger.Debug($"  - INFO: Found mapping for unit '{unit.GetName()}' in faction '{unit.GetAttilaFaction()}' from file '{Path.GetFileName(xml_file)}'.");
                        return (foundKey, foundIsSiege);
                    }
                }
            }

            // Priority 2: If no specific mapping found, search for a default mapping in reverse file order
            Program.Logger.Debug($"  - INFO: Unit '{unit.GetName()}' not found in its specific faction '{unit.GetAttilaFaction()}'. Searching for fallback mapping in 'Default' faction.");
            foreach (var xml_file in files_paths)
            {
                XmlDocument FactionsFile = new XmlDocument();
                FactionsFile.Load(xml_file);
                XmlNode? factionNode = FactionsFile.SelectSingleNode($"/Factions/Faction[@name='Default' or @name='DEFAULT']");

                if (factionNode != null)
                {
                    var (foundKey, foundIsSiege) = FindUnitKeyInFaction(factionNode, unit, null);
                    if (foundIsSiege) isPotentiallySiege = true; // Remember if it's ever marked as siege
                    if (foundKey != NOT_FOUND_KEY)
                    {
                        Program.Logger.Debug($"  - INFO: Found 'Default' fallback mapping for unit '{unit.GetName()}' from file '{Path.GetFileName(xml_file)}'.");
                        return (foundKey, foundIsSiege);
                    }
                }
            }

            return (NOT_FOUND_KEY, isPotentiallySiege);
        }

        private static string SelectRankedUnitKey(List<(int rank, string key)> candidates, int requiredRank, string? keyToExclude = null)
        {
            Program.Logger.Debug($"      - SelectRankedUnitKey: Called with {candidates.Count} candidates, required rank <= {requiredRank}.");
            if (!candidates.Any()) return NOT_FOUND_KEY;

            // NEW: Exclude the problematic key if provided
            if (!string.IsNullOrEmpty(keyToExclude))
            {
                candidates = candidates.Where(c => c.key != keyToExclude).ToList();
                if (!candidates.Any())
                {
                    Program.Logger.Debug($"      - SelectRankedUnitKey: All candidates were excluded (key: {keyToExclude}).");
                    return NOT_FOUND_KEY; // All candidates were the one to be excluded
                }
            }

            // Find all candidates at or below the required rank
            var suitableCandidates = candidates.Where(t => t.rank <= requiredRank).ToList();
            Program.Logger.Debug($"      - SelectRankedUnitKey: Found {suitableCandidates.Count} suitable candidates with rank <= {requiredRank}.");

            List<(int rank, string key)> finalSelectionPool;

            if (suitableCandidates.Any())
            {
                // Find the best rank among the suitable candidates
                int bestRank = suitableCandidates.Max(t => t.rank);
                finalSelectionPool = suitableCandidates.Where(t => t.rank == bestRank).ToList();
                Program.Logger.Debug($"      - SelectRankedUnitKey: Best suitable rank is {bestRank}. Final pool has {finalSelectionPool.Count} units.");
            }
            else
            {
                // Fallback: No suitable rank found, so use the lowest available rank overall
                int lowestRank = candidates.Min(t => t.rank);
                finalSelectionPool = candidates.Where(t => t.rank == lowestRank).ToList();
                Program.Logger.Debug($"      - SelectRankedUnitKey: No suitable rank found. Falling back to lowest available rank {lowestRank}. Final pool has {finalSelectionPool.Count} units.");
            }

            // Deterministically select one candidate from the final pool
            if (finalSelectionPool.Any())
            {
                string selectedKey = finalSelectionPool.OrderBy(c => c.key).First().key;
                Program.Logger.Debug($"      - SelectRankedUnitKey: Deterministically selected '{selectedKey}' from the final pool.");
                return selectedKey;
            }

            Program.Logger.Debug("      - SelectRankedUnitKey: Final selection pool was empty. Returning NOT_FOUND_KEY.");
            return NOT_FOUND_KEY;
        }


        static (string, bool) FindUnitKeyInFaction(XmlNode factionElement, Unit unit, string? keyToExclude = null)
        {
            if (unit.GetRegimentType() == RegimentType.Commander)
            {
                Program.Logger.Debug($"    - FindUnitKeyInFaction: Searching for COMMANDER unit in faction '{factionElement.Attributes?["name"]?.Value ?? "Unknown"}'.");
                var generalRanks = new List<(int rank, string key)>();
                foreach (XmlNode generalNode in factionElement.SelectNodes("General"))
                {
                    string? key = generalNode.Attributes?["key"]?.Value;
                    if (string.IsNullOrEmpty(key)) continue;

                    int rank = 1; // Default rank is 1 if attribute is missing or invalid
                    string? rankAttr = generalNode.Attributes?["rank"]?.Value;
                    if (!string.IsNullOrEmpty(rankAttr) && int.TryParse(rankAttr, out int parsedRank))
                    {
                        rank = parsedRank;
                    }
                    generalRanks.Add((rank, key));
                }

                if (generalRanks.Any())
                {
                    Program.Logger.Debug($"    - FindUnitKeyInFaction: Found {generalRanks.Count} possible general units: [{string.Join(", ", generalRanks.Select(g => $"{g.key} (rank {g.rank})"))}]");
                    // Map CK3 title Rank to a required rank level for the general unit
                    int requiredRank = 1;
                    if (unit.CharacterRank >= 4) requiredRank = 3; // King or Emperor gets rank 3 general
                    else if (unit.CharacterRank == 3) requiredRank = 2; // Duke gets rank 2 general
                    Program.Logger.Debug($"    - FindUnitKeyInFaction: Commander's CK3 rank is {unit.CharacterRank}, required Attila rank is {requiredRank}.");

                    string selectedKey = SelectRankedUnitKey(generalRanks, requiredRank, keyToExclude);
                    if (selectedKey != NOT_FOUND_KEY)
                    {
                        Program.Logger.Debug($"    - FindUnitKeyInFaction: Selected general unit key '{selectedKey}'.");
                        return (selectedKey, false);
                    }
                    Program.Logger.Debug($"    - FindUnitKeyInFaction: No suitable general unit key found for rank {requiredRank}.");
                }
                else
                {
                    Program.Logger.Debug($"    - FindUnitKeyInFaction: No <General> nodes found in this faction block.");
                }
            }
            else if (unit.GetRegimentType() == RegimentType.Knight)
            {
                var knightRanks = new List<(int rank, string key)>();
                foreach (XmlNode knightNode in factionElement.SelectNodes("Knights"))
                {
                    string? key = knightNode.Attributes?["key"]?.Value;
                    if (string.IsNullOrEmpty(key)) continue;

                    int rank = 1; // Default rank is 1 if attribute is missing or invalid
                    string? rankAttr = knightNode.Attributes?["rank"]?.Value;
                    if (!string.IsNullOrEmpty(rankAttr) && int.TryParse(rankAttr, out int parsedRank))
                    {
                        rank = parsedRank;
                    }
                    knightRanks.Add((rank, key));
                }

                if (knightRanks.Any())
                {
                    int requiredRank = unit.CharacterRank; // Directly use the 1-3 rank from Prowess

                    string selectedKey = SelectRankedUnitKey(knightRanks, requiredRank, keyToExclude);
                    if (selectedKey != NOT_FOUND_KEY)
                    {
                        return (selectedKey, false);
                    }
                }
            }
            else
            {
                bool isPotentiallySiege = false;
                // Existing logic for MenAtArms
                foreach (XmlNode node in factionElement.ChildNodes)
                {
                    if (node is XmlComment) continue;
                    if (node.Name == "Levies") continue;
                    if (node.Name == "Garrison") continue; // Skip Garrison nodes here

                    //MenAtArms
                    if (node.Name == "MenAtArm" && unit.GetRegimentType() == RegimentType.MenAtArms)
                    {
                        if (node.Attributes?["type"]?.Value == unit.GetName())
                        {
                            // We found a match for the unit type.
                            // Check if this definition marks it as a siege unit.
                            if (node.Attributes?["siege"]?.Value == "true")
                            {
                                isPotentiallySiege = true;
                            }

                            // Now, check if there's a key we can use.
                            if (node.Attributes?["key"]?.Value is string unit_key && !string.IsNullOrEmpty(unit_key))
                            {
                                if (unit_key == keyToExclude) continue;
                                
                                bool isSiege = node.Attributes?["siege"]?.Value == "true";
                                if (isSiege)
                                {
                                    bool siegeEnginePerUnit = node.Attributes?["siege_engine_per_unit"]?.Value == "true";
                                    if (siegeEnginePerUnit)
                                    {
                                        unit.SetIsSiegeEnginePerUnit(true);
                                    }
                                    if (node.Attributes?["num_guns"]?.Value is string numGunsStr && int.TryParse(numGunsStr, out int numGuns) && numGuns > 0)
                                    {
                                        unit.SetNumGuns(numGuns);
                                    }
                                }
                                return (unit_key, isSiege);
                            }
                        }
                    }
                }
                // If we exit the loop without returning, it means no key was found.
                // Return NOT_FOUND_KEY, but with the knowledge of whether it's a siege type.
                return (NOT_FOUND_KEY, isPotentiallySiege);
            }

            return (NOT_FOUND_KEY, false);
        }

        public static int ConvertMachinesToMen(int ck3SiegeWeaponCount)
        {
            // This formula calculates the minimum number of soldiers required to produce
            // the target number of siege engines, based on Attila's rounding logic
            // (round(soldiers / 4.0) = engines).
            if (ck3SiegeWeaponCount <= 0) return 0;
            if (ck3SiegeWeaponCount == 1) return 3; // Special case for 1 engine, minimum crew is 3.
            if (ck3SiegeWeaponCount == 2) return 7; // Special case for 2 engine
            return (ck3SiegeWeaponCount * 4) - 2;
        }

        private static (string, bool) ProcessUnitKeyResult(Unit unit, string key, bool isSiege)
        {
            // Check for manual replacements first.
            if (key != NOT_FOUND_KEY && BattleState.ManualUnitReplacements.TryGetValue((key, unit.IsPlayer()), out var manualReplacement))
            {
                Program.Logger.Debug($"Manual Replace: Applying replacement for unit key '{key}' with '{manualReplacement.replacementKey}' for {(unit.IsPlayer() ? "player" : "enemy")} alliance.");
                unit.SetIsSiege(manualReplacement.isSiege);
                return (manualReplacement.replacementKey, manualReplacement.isSiege);
            }

            // Check if the determined key has an autofix replacement.
            if (key != NOT_FOUND_KEY && BattleProcessor.AutofixReplacements.TryGetValue(key, out var replacement))
            {
                Program.Logger.Debug($"Autofix: Applying replacement for unit key '{key}' with '{replacement.replacementKey}'.");
                unit.SetIsSiege(replacement.isSiege);
                return (replacement.replacementKey, replacement.isSiege);
            }

            unit.SetIsSiege(isSiege);
            // Soldier conversion logic is now handled by the caller (in UnitsFile.cs)
            // to support both single- and multi-engine units.
            return (key, isSiege);
        }


        public static (string, bool) GetUnitKey(Unit unit)
        {
            (string unit_key, bool isSiege) result;
            string initial_key = unit.GetAttilaUnitKey();

            // 0. Check for manual replacements first, using the key already on the unit if available.
            // This is crucial for levies and garrisons which are handled as compositions.
            if (!string.IsNullOrEmpty(initial_key) && initial_key != NOT_FOUND_KEY)
            {
                if (BattleState.ManualUnitReplacements.TryGetValue((initial_key, unit.IsPlayer()), out var manualReplacement))
                {
                    Program.Logger.Debug($"Manual Replace (Pre-check): Applying replacement for pre-set unit key '{initial_key}' with '{manualReplacement.replacementKey}'.");
                    unit.SetIsSiege(manualReplacement.isSiege);
                    return (manualReplacement.replacementKey, manualReplacement.isSiege);
                }
            }


            // 1. Initial search in specific files
            if (unit.GetRegimentType() == RegimentType.Garrison && unit.GetAttilaUnitKey() != string.Empty)
            {
                result = (unit.GetAttilaUnitKey(), false);
            }
            else
            {
                result = SearchInTitlesFile(unit);
                if (result.unit_key == NOT_FOUND_KEY)
                {
                    result = SearchInFactionFiles(unit);
                }
            }

            // 2. Check for exclusion based on initial search result
            bool siegeEnginesInFieldBattles = !ModOptions.optionsValuesCollection.TryGetValue("SiegeEnginesInFieldBattles", out string? siegeEnginesOption) || siegeEnginesOption == "Enabled";

            if (result.isSiege && !BattleState.IsSiegeBattle && !siegeEnginesInFieldBattles)
            {
                Program.Logger.Debug($"Excluding siege unit type '{unit.GetName()}' from field battle as per option.");
                return ProcessUnitKeyResult(unit, NOT_FOUND_KEY, true); // Exclude it.
            }

            // 3. If no key found yet, try default
            if (result.unit_key == NOT_FOUND_KEY)
            {
                var (default_key, defaultIsSiege) = GetDefaultUnitKey(unit);
                if (default_key != NOT_FOUND_KEY)
                {
                    Program.Logger.Debug($"  - INFO: Could not map CK3 Unit '{unit.GetName()}' (Type: {unit.GetRegimentType()}). Substituting with default Attila unit '{default_key}'.");
                    result = (default_key, defaultIsSiege);

                    // 4. Re-check for exclusion if the default unit is a siege unit
                    if (result.isSiege && !BattleState.IsSiegeBattle && !siegeEnginesInFieldBattles)
                    {
                        Program.Logger.Debug($"Excluding default substitute (which is a siege unit, key: {result.unit_key}) for '{unit.GetName()}' from field battle as per option.");
                        return ProcessUnitKeyResult(unit, NOT_FOUND_KEY, true); // Exclude it.
                    }
                }
                else
                {
                    // No specific key, no default key. Log and return NOT_FOUND.
                    if (unit.GetRegimentType() == RegimentType.Levy || unit.GetRegimentType() == RegimentType.Garrison)
                    {
                        Program.Logger.Debug($"  - INFO: No single key mapping for '{unit.GetName()}' (Type: {unit.GetRegimentType()}). This is expected as they are processed as a composition later.");
                    }
                    else
                    {
                        Program.Logger.Debug($"  - ERROR: Could not map CK3 Unit '{unit.GetName()}' (Type: {unit.GetRegimentType()}). All mapping attempts including default fallback failed.");
                    }
                    result = (NOT_FOUND_KEY, result.isSiege); // Keep the isSiege info from initial search
                }
            }

            // 5. Return final result
            return ProcessUnitKeyResult(unit, result.unit_key, result.isSiege);
        }

        private static string GetUnitMaxCategory(Unit unit)
        {
            if (LoadedUnitMapper_FolderPath == null) return "INFANTRY";

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            files_paths.Reverse(); // Prioritize submods over base mappers

            string? maxCategory = null;

            foreach (var xml_file in files_paths)
            {
                XmlDocument FactionsFile = new XmlDocument();
                FactionsFile.Load(xml_file);
                if (FactionsFile.DocumentElement == null) continue;

                // Search for the specific unit type in any faction to determine its category
                XmlNode? unitNode = FactionsFile.SelectSingleNode($"//MenAtArm[@type='{unit.GetName()}']");
                if (unitNode?.Attributes?["max"]?.Value is string foundCategory)
                {
                    maxCategory = foundCategory.ToUpper();
                    break; // Found it, no need to search more files
                }
            }

            switch (maxCategory)
            {
                case "INFANTRY":
                case "RANGED":
                case "CAVALRY":
                    return maxCategory;
                default:
                    Program.Logger.Debug($"Could not determine max category for MAA unit '{unit.GetName()}'. Defaulting to INFANTRY.");
                    return "INFANTRY"; // Fallback
            }
        }

        public static (string, bool) GetDefaultUnitKey(Unit unitToReplace, string? keyToExclude = null)
        {
            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("Error: LoadedUnitMapper_FolderPath is not set. Cannot get default unit key.");
                return (NOT_FOUND_KEY, false);
            }

            string factionsFolderPath = Path.Combine(LoadedUnitMapper_FolderPath, "Factions");
            if (!Directory.Exists(factionsFolderPath)) return (NOT_FOUND_KEY, false);

            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var filesPaths = GetSortedFilePaths(factionsFolderPath, priorityFilePattern);

            XmlNode? defaultFactionNode = null;
            foreach (var xmlFile in filesPaths)
            {
                XmlDocument factionsFile = new XmlDocument();
                factionsFile.Load(xmlFile);
                XmlNode? currentNode = factionsFile.SelectSingleNode("/Factions/Faction[@name='Default' or @name='DEFAULT']");
                if (currentNode != null)
                {
                    defaultFactionNode = currentNode;
                }
            }

            if (defaultFactionNode == null)
            {
                Program.Logger.Debug("Error: Could not find a 'Default' faction in any unit mapper file.");
                return (NOT_FOUND_KEY, false);
            }

            switch (unitToReplace.GetRegimentType())
            {
                case RegimentType.Commander:
                    var generalRanks = new List<(int rank, string key)>();
                    foreach (XmlNode generalNode in defaultFactionNode.SelectNodes("General"))
                    {
                        string? key = generalNode.Attributes?["key"]?.Value;
                        if (string.IsNullOrEmpty(key)) continue;
                        int rank = 1;
                        if (generalNode.Attributes?["rank"]?.Value is string rankAttr && int.TryParse(rankAttr, out int parsedRank))
                        {
                            rank = parsedRank;
                        }
                        generalRanks.Add((rank, key));
                    }
                    int requiredGeneralRank = (unitToReplace.CharacterRank >= 4) ? 3 : (unitToReplace.CharacterRank == 3) ? 2 : 1;
                    return (SelectRankedUnitKey(generalRanks, requiredGeneralRank, keyToExclude), false);

                case RegimentType.Knight:
                    var knightRanks = new List<(int rank, string key)>();
                    foreach (XmlNode knightNode in defaultFactionNode.SelectNodes("Knights"))
                    {
                        string? key = knightNode.Attributes?["key"]?.Value;
                        if (string.IsNullOrEmpty(key)) continue;
                        int rank = 1;
                        if (knightNode.Attributes?["rank"]?.Value is string rankAttr && int.TryParse(rankAttr, out int parsedRank))
                        {
                            rank = parsedRank;
                        }
                        knightRanks.Add((rank, key));
                    }
                    return (SelectRankedUnitKey(knightRanks, unitToReplace.CharacterRank, keyToExclude), false);

                case RegimentType.MenAtArms:
                    string category = GetUnitMaxCategory(unitToReplace);
                    var candidates = new List<(string key, bool isSiege)>();
                    var fallbackCandidates = new List<(string key, bool isSiege)>();
                    foreach (XmlNode maaNode in defaultFactionNode.SelectNodes("MenAtArm"))
                    {
                        string? key = maaNode.Attributes?["key"]?.Value;
                        if (string.IsNullOrEmpty(key) || key == keyToExclude) continue;

                        bool isSiege = maaNode.Attributes?["siege"]?.Value == "true";

                        fallbackCandidates.Add((key, isSiege));
                        if (maaNode.Attributes?["max"]?.Value?.ToUpper() == category)
                        {
                            candidates.Add((key, isSiege));
                        }
                    }
                    if (candidates.Any()) return candidates.OrderBy(c => c.key).First();
                    if (fallbackCandidates.Any()) return fallbackCandidates.OrderBy(c => c.key).First();
                    break;

                case RegimentType.Garrison:
                    int holdingLevel = BattleState.IsSiegeBattle ? Sieges.GetHoldingLevel() : 1;
                    var allGarrisons = new List<(string key, int level)>();
                    foreach (XmlNode garrisonNode in defaultFactionNode.SelectNodes("Garrison"))
                    {
                        string? key = garrisonNode.Attributes?["key"]?.Value;
                        if (string.IsNullOrEmpty(key) || key == keyToExclude) continue;
                        int level = 1;
                        if (garrisonNode.Attributes?["level"]?.Value is string levelStr && int.TryParse(levelStr, out int parsedLevel))
                        {
                            level = parsedLevel;
                        }
                        allGarrisons.Add((key, level));
                    }
                    if (allGarrisons.Any())
                    {
                        var suitableGarrisons = allGarrisons.Where(g => g.level <= holdingLevel).ToList();
                        List<(string key, int level)> finalSelectionPool = suitableGarrisons.Any()
                            ? suitableGarrisons.Where(g => g.level == suitableGarrisons.Max(s => s.level)).ToList()
                            : allGarrisons.Where(g => g.level == allGarrisons.Min(s => s.level)).ToList();
                        if (finalSelectionPool.Any()) return (finalSelectionPool.OrderBy(g => g.key).First().key, false);
                    }
                    break;

                case RegimentType.Levy:
                    var levyKeys = defaultFactionNode.SelectNodes("Levies")?.Cast<XmlNode>()
                        .Select(node => node.Attributes?["key"]?.Value)
                        .Where(key => !string.IsNullOrEmpty(key) && key != keyToExclude)
                        .ToList();
                    if (levyKeys != null && levyKeys.Any()) return (levyKeys.OrderBy(k => k).First(), false);
                    break;
            }

            return (NOT_FOUND_KEY, false);
        }

        public static (string, bool) GetReplacementUnitKeyFromFaction(Unit unitToReplace, string targetFaction, string? keyToExclude = null)
        {
            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("Error: LoadedUnitMapper_FolderPath is not set. Cannot get replacement unit key.");
                return (NOT_FOUND_KEY, false);
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            files_paths.Reverse(); // Prioritize sub-mods

            foreach (var xml_file in files_paths)
            {
                XmlDocument FactionsFile = new XmlDocument();
                FactionsFile.Load(xml_file);
                XmlNode? factionNode = FactionsFile.SelectSingleNode($"/Factions/Faction[@name='{targetFaction}']");

                if (factionNode != null)
                {
                    var (foundKey, foundIsSiege) = FindUnitKeyInFaction(factionNode, unitToReplace, keyToExclude);
                    if (foundKey != NOT_FOUND_KEY)
                    {
                        return (foundKey, foundIsSiege);
                    }
                }
            }

            return (NOT_FOUND_KEY, false);
        }

        public static List<string> GetFactionsByHeritage(string heritageName)
        {
            var factions = new HashSet<string>();
            if (string.IsNullOrEmpty(LoadedUnitMapper_FolderPath) || string.IsNullOrEmpty(heritageName))
            {
                return new List<string>();
            }

            string cultures_folder_path = LoadedUnitMapper_FolderPath + @"\Cultures";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(cultures_folder_path, priorityFilePattern);

            foreach (var xml_file in files_paths)
            {
                XmlDocument CulturesFile = new XmlDocument();
                CulturesFile.Load(xml_file);
                if (CulturesFile.DocumentElement == null) continue;

                foreach (XmlNode heritageNode in CulturesFile.DocumentElement.SelectNodes($"Heritage[@name='{heritageName}']"))
                {
                    if (heritageNode.Attributes?["faction"]?.Value is string heritageFaction && !string.IsNullOrEmpty(heritageFaction)) factions.Add(heritageFaction);
                    foreach (XmlNode cultureNode in heritageNode.SelectNodes("Culture"))
                    {
                        if (cultureNode.Attributes?["faction"]?.Value is string cultureFaction && !string.IsNullOrEmpty(cultureFaction)) factions.Add(cultureFaction);
                    }
                }
            }
            return factions.ToList();
        }

        public static string GetAttilaFaction(string CultureName, string HeritageName)
        {
            Program.Logger.Debug($"Searching faction for Culture:{CultureName}, Heritage:{HeritageName}");
            if (string.IsNullOrEmpty(CultureName))
            {
                Program.Logger.Debug("WARNING: CultureName is null/empty");
            }
            if (string.IsNullOrEmpty(HeritageName))
            {
                Program.Logger.Debug("WARNING: HeritageName is null/empty");
            }

            (string faction, string file) heritage_mapping = ("", "");
            (string faction, string file) culture_mapping = ("", "");

            if (string.IsNullOrEmpty(LoadedUnitMapper_FolderPath))
            {
                Program.Logger.Debug("Error: LoadedUnitMapper_FolderPath is not set. Cannot get Attila faction.");
                throw new Exception("Unit mapper folder path not configured");
            }
            
            string cultures_folder_path = LoadedUnitMapper_FolderPath + @"\Cultures";
            Program.Logger.Debug($"Searching for Attila faction for Culture '{CultureName}', Heritage '{HeritageName}' in: {cultures_folder_path}");

            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(cultures_folder_path, priorityFilePattern);
            foreach (var xml_file in files_paths)
            {
                if (Path.GetExtension(xml_file) == ".xml")
                {
                    string currentFile = Path.GetFileName(xml_file);
                    Program.Logger.Debug($"Scanning cultures file: {currentFile}");
                    XmlDocument CulturesFile = new XmlDocument();
                    CulturesFile.Load(xml_file);

                    if (CulturesFile.DocumentElement == null) continue; // Add null check for DocumentElement
                    foreach(XmlNode heritage in CulturesFile.DocumentElement.ChildNodes)
                    {
                        if (heritage is XmlComment) continue;

                        string heritage_name = heritage.Attributes?["name"]?.Value ?? string.Empty;                       

                        if(heritage_name == HeritageName && !string.IsNullOrEmpty(HeritageName))
                        {
                            bool cultureMatchFoundInThisBlock = false;
                            // First, loop for a specific culture match
                            foreach(XmlNode culture in heritage.ChildNodes)
                            {
                                if (culture is XmlComment) continue; 
                                string culture_name = culture.Attributes?["name"]?.Value ?? string.Empty;

                                if (culture_name == CultureName && !string.IsNullOrEmpty(CultureName))
                                {
                                    string found_culture_faction = culture.Attributes?["faction"]?.Value ?? string.Empty;
                                    if (!string.IsNullOrEmpty(found_culture_faction))
                                    {
                                        culture_mapping = (found_culture_faction, currentFile);
                                        Program.Logger.Debug($"  - Found/Updated culture mapping: {CultureName} -> {culture_mapping.faction}");
                                        cultureMatchFoundInThisBlock = true;
                                        
                                        // Also update heritage mapping from this file, as it's the most relevant context
                                        string found_heritage_faction_context = heritage.Attributes?["faction"]?.Value ?? string.Empty;
                                        if (!string.IsNullOrEmpty(found_heritage_faction_context)) {
                                            heritage_mapping = (found_heritage_faction_context, currentFile);
                                            Program.Logger.Debug($"  - Contextual heritage mapping updated: {HeritageName} -> {heritage_mapping.faction}");
                                        }
                                    }
                                }
                            }

                            // If no specific culture was found in this block, check for a heritage-level mapping
                            if (!cultureMatchFoundInThisBlock)
                            {
                                string found_heritage_faction = heritage.Attributes?["faction"]?.Value ?? string.Empty;
                                // Only apply this heritage mapping if we haven't found a specific culture mapping in a *previous* file.
                                if (!string.IsNullOrEmpty(found_heritage_faction) && string.IsNullOrEmpty(culture_mapping.faction))
                                {
                                    heritage_mapping = (found_heritage_faction, currentFile);
                                    Program.Logger.Debug($"  - Found/Updated heritage mapping: {HeritageName} -> {heritage_mapping.faction}.");
                                }
                            }
                        }
                    }
                }
            }

            string faction = "";
            if (!string.IsNullOrEmpty(culture_mapping.faction))
            {
                faction = culture_mapping.faction;
                Program.Logger.Debug($"Resolved faction for '{CultureName}/{HeritageName}' to '{faction}' using specific culture mapping from '{culture_mapping.file}'.");
            }
            else if (!string.IsNullOrEmpty(heritage_mapping.faction))
            {
                faction = heritage_mapping.faction;
                Program.Logger.Debug($"Resolved faction for '{CultureName}/{HeritageName}' to '{faction}' using heritage mapping from '{heritage_mapping.file}' (no specific culture match).");
            }

            if (string.IsNullOrEmpty(faction))
            {
                // Try fallback to Default heritage and Default culture
                foreach (var xml_file in files_paths)
                {
                    if (Path.GetExtension(xml_file) == ".xml")
                    {
                        XmlDocument CulturesFile = new XmlDocument();
                        CulturesFile.Load(xml_file);

                        if (CulturesFile.DocumentElement == null) continue; // Add null check for DocumentElement
                        foreach(XmlNode heritage in CulturesFile.DocumentElement.ChildNodes)
                        {
                            if (heritage is XmlComment) continue;
                            string heritage_name = heritage.Attributes?["name"]?.Value ?? string.Empty;
                            if (heritage_name == "Default")
                            {
                                foreach(XmlNode culture in heritage.ChildNodes)
                                {
                                    if (culture is XmlComment) continue;
                                    string culture_name = culture.Attributes?["name"]?.Value ?? string.Empty;
                                    if (culture_name == "Default" && heritage.Attributes?["faction"]?.Value != null)
                                    {
                                        faction = heritage.Attributes["faction"]!.Value;
                                        if (!string.IsNullOrEmpty(faction))
                                        {
                                            Program.Logger.Debug($"Resolved faction for '{CultureName}/{HeritageName}' to '{faction}' using fallback 'Default/Default' mapping from '{Path.GetFileName(xml_file)}'.");
                                            break;
                                        }
                                    }
                                }
                                if (!string.IsNullOrEmpty(faction)) break;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(faction)) break;
                }
            }

            if (string.IsNullOrEmpty(faction))
            {
                Program.Logger.Debug($"ERROR: Faction not found for Culture '{CultureName}', Heritage '{HeritageName}'. All fallbacks failed.");
            }
            else
            {
                Program.Logger.Debug($"Final faction for '{CultureName}/{HeritageName}' is '{faction}'.");
            }

            return faction;
        }

        public static (string X, string Y, List<string>? orientations)? GetSettlementMap(string faction, string battleType, string provinceName)
        {
            BattleStateBridge.Clear(); // Clear any previous overrides at the start of a new map search.

            // Set defaults first
            if (battleType == "settlement_standard")
            {
                BattleStateBridge.BesiegedDeploymentWidth = "1500";
                BattleStateBridge.BesiegedDeploymentHeight = "1500";
            }
            else if (battleType == "settlement_unfortified")
            {
                BattleStateBridge.BesiegedDeploymentWidth = "1350";
                BattleStateBridge.BesiegedDeploymentHeight = "1350";
            }

            Program.Logger.Debug($"Attempting to get settlement map for Faction: '{faction}', BattleType: '{battleType}', Province: '{provinceName}'");

            bool forceGeneric = BattleState.AutofixForceGenericMap;
            if (forceGeneric)
            {
                Program.Logger.Debug("Autofix: Forcing use of generic settlement map.");
            }

            string cacheKey = $"{provinceName}_{battleType}";
            if (BattleState.AutofixMapVariantOffset > 0) cacheKey += $"_offset{BattleState.AutofixMapVariantOffset}";
            if (forceGeneric) cacheKey += "_forcegeneric";

            if (_provinceMapCache.TryGetValue(cacheKey, out var cachedMap))
            {
                Program.Logger.Debug($"Found cached settlement map for '{cacheKey}'. Coordinates: ({cachedMap.X}, {cachedMap.Y})");
                return cachedMap;
            }

            SettlementVariant? selectedVariant = null;
            string usedFactionForLog = faction; // For logging

            // Priority 1: Unique Map by province_names attribute
            if (!forceGeneric && Terrains?.UniqueSettlementMaps != null)
            {
                var uniqueMapByProvName = Terrains.UniqueSettlementMaps
                    .FirstOrDefault(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                           sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
                
                if (uniqueMapByProvName != null && uniqueMapByProvName.Variants.Any())
                {
                    Program.Logger.Debug($"Found unique settlement map by 'province_names' attribute for Province '{provinceName}'.");
                    int deterministicIndex = GetDeterministicIndex(provinceName, uniqueMapByProvName.Variants.Count);
                    selectedVariant = uniqueMapByProvName.Variants[deterministicIndex];
                }
            }

            // Priority 2: Unique Map by Variant key (existing logic)
            if (selectedVariant == null && !forceGeneric && Terrains?.UniqueSettlementMaps != null)
            {
                var matchingUniqueMaps = Terrains.UniqueSettlementMaps
                                         .Where(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase))
                                         .ToList();

                foreach (var uniqueMap in matchingUniqueMaps)
                {
                    var uniqueMatch = uniqueMap.Variants.FirstOrDefault(v => provinceName.IndexOf(v.Key, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (uniqueMatch != null)
                    {
                        Program.Logger.Debug($"Found unique settlement map variant '{uniqueMatch.Key}' for Province '{provinceName}'.");
                        selectedVariant = uniqueMatch;
                        break;
                    }
                }
            }

            // Priority 3: Generic Map by province_names attribute (Specific Faction then Default)
            if (selectedVariant == null && Terrains?.SettlementMaps != null)
            {
                var genericMapByProvName = Terrains.SettlementMaps
                    .FirstOrDefault(sm => sm.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                                           sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                           sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
                
                if (genericMapByProvName == null)
                {
                    genericMapByProvName = Terrains.SettlementMaps
                        .FirstOrDefault(sm => sm.Faction.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                                               sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                               sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                if (genericMapByProvName != null && genericMapByProvName.Variants.Any())
                {
                    Program.Logger.Debug($"Found generic settlement map for Faction '{genericMapByProvName.Faction}' by 'province_names' attribute for Province '{provinceName}'.");
                    int deterministicIndex = GetDeterministicIndex(provinceName, genericMapByProvName.Variants.Count);
                    selectedVariant = genericMapByProvName.Variants[deterministicIndex];
                }
            }

            // Priority 4 & 5: Generic Map by faction (existing logic, excluding those with province_names)
            if (selectedVariant == null && Terrains?.SettlementMaps != null)
            {
                var matchingGenericMaps = Terrains.SettlementMaps
                                          .Where(sm => sm.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                                                       sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                                       !sm.ProvinceNames.Any())
                                          .ToList();

                if (!matchingGenericMaps.Any())
                {
                    matchingGenericMaps = Terrains.SettlementMaps
                                          .Where(sm => sm.Faction.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                                                       sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                                       !sm.ProvinceNames.Any())
                                          .ToList();
                    usedFactionForLog = "Default";
                }

                if (matchingGenericMaps.Any())
                {
                    var allGenericVariants = matchingGenericMaps.SelectMany(sm => sm.Variants).ToList();
                    if (allGenericVariants.Any())
                    {
                        int deterministicIndex = GetDeterministicIndex(provinceName, allGenericVariants.Count);
                        selectedVariant = allGenericVariants[deterministicIndex];
                        Program.Logger.Debug($"Deterministically selected settlement map variant '{selectedVariant.Key}' for Faction '{usedFactionForLog}', BattleType '{battleType}'.");
                    }
                }
            }

            // If a variant was found by any method, apply overrides and return it
            if (selectedVariant != null)
            {
                if (!string.IsNullOrEmpty(selectedVariant.BesiegedDeploymentZoneWidth))
                {
                    BattleStateBridge.BesiegedDeploymentWidth = selectedVariant.BesiegedDeploymentZoneWidth;
                    Program.Logger.Debug($"Overriding besieged deployment width to: {selectedVariant.BesiegedDeploymentZoneWidth}");
                }
                if (!string.IsNullOrEmpty(selectedVariant.BesiegedDeploymentZoneHeight))
                {
                    BattleStateBridge.BesiegedDeploymentHeight = selectedVariant.BesiegedDeploymentZoneHeight;
                    Program.Logger.Debug($"Overriding besieged deployment height to: {selectedVariant.BesiegedDeploymentZoneHeight}");
                }

                Program.Logger.Debug($"Coordinates: ({selectedVariant.X}, {selectedVariant.Y})");
                _provinceMapCache[cacheKey] = (selectedVariant.X, selectedVariant.Y, selectedVariant.BesiegerOrientations);
                return (selectedVariant.X, selectedVariant.Y, selectedVariant.BesiegerOrientations);
            }
            else
            {
                // Final Fallback
                Program.Logger.Debug($"No suitable settlement map variant found for Faction: '{faction}', BattleType: '{battleType}', Province: '{provinceName}'. Returning null.");
                return null;
            }
        }

        public static string? GetSiegeBattleType(string faction, string battleType, string provinceName)
        {
            Program.Logger.Debug($"Attempting to get siege battle type for Faction: '{faction}', BattleType: '{battleType}', Province: '{provinceName}'");

            bool forceGeneric = BattleState.AutofixForceGenericMap;

            // Priority 1: Unique Map by province_names attribute
            if (!forceGeneric && Terrains?.UniqueSettlementMaps != null)
            {
                var uniqueMapByProvName = Terrains.UniqueSettlementMaps
                    .FirstOrDefault(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                           sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
                
                if (uniqueMapByProvName != null)
                {
                    Program.Logger.Debug($"Found siege battle type '{uniqueMapByProvName.BattleType}' from unique map by province name.");
                    return uniqueMapByProvName.BattleType;
                }
            }

            // Priority 2: Unique Map by Variant key (existing logic)
            if (!forceGeneric && Terrains?.UniqueSettlementMaps != null)
            {
                var matchingUniqueMaps = Terrains.UniqueSettlementMaps
                                         .Where(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase))
                                         .ToList();

                foreach (var uniqueMap in matchingUniqueMaps)
                {
                    var uniqueMatch = uniqueMap.Variants.FirstOrDefault(v => provinceName.IndexOf(v.Key, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (uniqueMatch != null)
                    {
                        Program.Logger.Debug($"Found siege battle type '{uniqueMap.BattleType}' from unique map by variant key '{uniqueMatch.Key}'.");
                        return uniqueMap.BattleType;
                    }
                }
            }

            // Priority 3: Generic Map by province_names attribute (Specific Faction then Default)
            if (Terrains?.SettlementMaps != null)
            {
                var genericMapByProvName = Terrains.SettlementMaps
                    .FirstOrDefault(sm => sm.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                                           sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                           sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
                
                if (genericMapByProvName == null)
                {
                    genericMapByProvName = Terrains.SettlementMaps
                        .FirstOrDefault(sm => sm.Faction.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                                               sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                               sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                if (genericMapByProvName != null)
                {
                    Program.Logger.Debug($"Found siege battle type '{genericMapByProvName.BattleType}' from generic map by province name for faction '{genericMapByProvName.Faction}'.");
                    return genericMapByProvName.BattleType;
                }
            }

            // Priority 4 & 5: Generic Map by faction (existing logic, excluding those with province_names)
            if (Terrains?.SettlementMaps != null)
            {
                var matchingGenericMaps = Terrains.SettlementMaps
                                          .Where(sm => sm.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                                                       sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                                       !sm.ProvinceNames.Any())
                                          .ToList();

                if (matchingGenericMaps.Any())
                {
                    Program.Logger.Debug($"Found siege battle type '{matchingGenericMaps.First().BattleType}' from generic map for faction '{faction}'.");
                    return matchingGenericMaps.First().BattleType;
                }

                matchingGenericMaps = Terrains.SettlementMaps
                                          .Where(sm => sm.Faction.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                                                       sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                                       !sm.ProvinceNames.Any())
                                          .ToList();
                
                if (matchingGenericMaps.Any())
                {
                    Program.Logger.Debug($"Found siege battle type '{matchingGenericMaps.First().BattleType}' from generic map for 'Default' faction.");
                    return matchingGenericMaps.First().BattleType;
                }
            }

            Program.Logger.Debug($"No specific siege battle type found for Faction: '{faction}', BattleType: '{battleType}', Province: '{provinceName}'. Returning null.");
            return null; // Fallback
        }

        public static string GetSettlementMapDescription(string faction, string battleType, string provinceName)
        {
            if (Terrains == null) return "Unknown Map";

            // This method replicates the logic of GetSettlementMap to find the *original* map, ignoring autofix overrides.

            // Local function to get the original deterministic index without autofix offsets.
            int GetOriginalDeterministicIndex(string input, int listCount)
            {
                if (listCount <= 0) return 0;
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = sha256.ComputeHash(inputBytes);
                    int hashAsInt = BitConverter.ToInt32(hashBytes, 0);
                    return Math.Abs(hashAsInt % listCount);
                }
            }

            // Priority 1 & 2: Unique Maps
            var uniqueMapByProvName = Terrains.UniqueSettlementMaps
                .FirstOrDefault(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                       sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
            if (uniqueMapByProvName != null && uniqueMapByProvName.Variants.Any())
            {
                int index = GetOriginalDeterministicIndex(provinceName, uniqueMapByProvName.Variants.Count);
                return $"Unique Map ('{uniqueMapByProvName.Variants[index].Key}')";
            }

            var matchingUniqueMaps = Terrains.UniqueSettlementMaps
                                     .Where(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase))
                                     .ToList();
            foreach (var uniqueMap in matchingUniqueMaps)
            {
                var uniqueMatch = uniqueMap.Variants.FirstOrDefault(v => provinceName.IndexOf(v.Key, StringComparison.OrdinalIgnoreCase) >= 0);
                if (uniqueMatch != null)
                {
                    return $"Unique Map ('{uniqueMatch.Key}')";
                }
            }

            // Priority 3: Generic Map by province_names
            var genericMapByProvName = Terrains.SettlementMaps
                .FirstOrDefault(sm => sm.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                                       sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                       sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
            if (genericMapByProvName != null && genericMapByProvName.Variants.Any())
            {
                int index = GetOriginalDeterministicIndex(provinceName, genericMapByProvName.Variants.Count);
                return $"Generic Map ('{genericMapByProvName.Variants[index].Key}')";
            }

            var defaultGenericMapByProvName = Terrains.SettlementMaps
                .FirstOrDefault(sm => sm.Faction.Equals("Default", StringComparison.OrdinalIgnoreCase) &&
                                       sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                       sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
            if (defaultGenericMapByProvName != null && defaultGenericMapByProvName.Variants.Any())
            {
                int index = GetOriginalDeterministicIndex(provinceName, defaultGenericMapByProvName.Variants.Count);
                return $"Generic Map ('{defaultGenericMapByProvName.Variants[index].Key}')";
            }

            // Priority 4 & 5: Generic Map by faction
            var matchingGenericMaps = Terrains.SettlementMaps
                                      .Where(sm => sm.Faction.Equals(faction, StringComparison.OrdinalIgnoreCase) &&
                                                   sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                                   !sm.ProvinceNames.Any())
                                      .ToList();
            if (matchingGenericMaps.Any())
            {
                var allGenericVariants = matchingGenericMaps.SelectMany(sm => sm.Variants).ToList();
                if (allGenericVariants.Any())
                {
                    int index = GetOriginalDeterministicIndex(provinceName, allGenericVariants.Count);
                    return $"Generic Map ('{allGenericVariants[index].Key}')";
                }
            }

            return "Unknown Map";
        }

        public static void SetMapperImage()
        {
            string destination_path = Directory.GetCurrentDirectory() + @"\data\battle files\campaign_maps\main_attila_map\main_attila_map.png";
            try
            {
                if (LoadedUnitMapper_FolderPath == null)
                {
                    Program.Logger.Debug("LoadedUnitMapper_FolderPath is null. Cannot set mapper image.");
                    throw new InvalidOperationException("LoadedUnitMapper_FolderPath is not set.");
                }

                string mapperFolderPath = LoadedUnitMapper_FolderPath; // Local variable for compiler analysis
                var image_path = Directory.GetFiles(mapperFolderPath).Where(x => x.EndsWith(".png")).FirstOrDefault();
                
                if (image_path != null)
                {
                    File.Copy(image_path, destination_path, true);
                    return;
                }
                else
                {
                    Program.Logger.Debug($"No image found in {mapperFolderPath}. Falling back to default image.");
                    throw new FileNotFoundException($"No .png image found in {mapperFolderPath}");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error setting mapper image: {ex.Message}. Using default image.");
                // In case of error or no image found, use default image
                string default_image_path = Directory.GetCurrentDirectory() + "\\settings\\main_attila_map.png";
                File.Copy(default_image_path, destination_path, true);
                return;
            }
        }

        public static bool IsUnitTypeSiege(RegimentType regimentType, string unitName, string attilaFaction)
        {
            // Only Men-at-Arms can be siege units according to the current mapping logic.
            if (regimentType != RegimentType.MenAtArms)
            {
                return false;
            }

            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("Error: LoadedUnitMapper_FolderPath is not set. Cannot determine if unit is siege type.");
                return false;
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            files_paths.Reverse(); // Search from last-loaded (submods) to first (OfficialCC)

            // This function will check a given faction node for the siege attribute.
            bool CheckFactionNodeForSiege(XmlNode factionNode, string maaName)
            {
                XmlNode? maaNode = factionNode.SelectSingleNode($"MenAtArm[@type='{maaName}']");
                if (maaNode != null)
                {
                    if (maaNode.Attributes?["siege"]?.Value == "true")
                    {
                        return true;
                    }
                }
                return false;
            }

            // Priority 1: Search for a specific faction mapping in reverse file order
            foreach (var xml_file in files_paths)
            {
                XmlDocument FactionsFile = new XmlDocument();
                FactionsFile.Load(xml_file);
                XmlNode? factionNode = FactionsFile.SelectSingleNode($"/Factions/Faction[@name='{attilaFaction}']");

                if (factionNode != null)
                {
                    if (CheckFactionNodeForSiege(factionNode, unitName))
                    {
                        return true;
                    }
                }
            }

            // Priority 2: If no specific mapping found, search for a default mapping in reverse file order
            foreach (var xml_file in files_paths)
            {
                XmlDocument FactionsFile = new XmlDocument();
                FactionsFile.Load(xml_file);
                XmlNode? factionNode = FactionsFile.SelectSingleNode($"/Factions/Faction[@name='Default' or @name='DEFAULT']");

                if (factionNode != null)
                {
                    if (CheckFactionNodeForSiege(factionNode, unitName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static List<AvailableUnit> GetAllAvailableUnits()
        {
            var allUnits = new List<AvailableUnit>();
            var uniqueUnitTracker = new HashSet<(string, string)>(); // To track faction + key to avoid duplicates

            if (LoadedUnitMapper_FolderPath == null) return allUnits;

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            if (!Directory.Exists(factions_folder_path)) return allUnits;

            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);

            foreach (var xml_file in files_paths)
            {
                try
                {
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue;

                    foreach (XmlNode factionNode in FactionsFile.SelectNodes("/Factions/Faction"))
                    {
                        string factionName = factionNode.Attributes?["name"]?.Value ?? "Unknown";

                        foreach (XmlNode unitNode in factionNode.ChildNodes)
                        {
                            if (unitNode is XmlComment) continue;

                            string? key = unitNode.Attributes?["key"]?.Value;
                            if (string.IsNullOrEmpty(key)) continue;

                            if (uniqueUnitTracker.Contains((factionName, key))) continue; // Skip duplicates from files with lower priority

                            var availableUnit = new AvailableUnit
                            {
                                FactionName = factionName,
                                AttilaUnitKey = key,
                                UnitType = unitNode.Name,
                                DisplayName = key, // Default display name is the key
                                IsSiege = false
                            };

                            if (unitNode.Name == "MenAtArm")
                            {
                                availableUnit.DisplayName = unitNode.Attributes?["type"]?.Value ?? key;
                                availableUnit.MaxCategory = unitNode.Attributes?["max"]?.Value;
                                availableUnit.IsSiege = unitNode.Attributes?["siege"]?.Value == "true";
                            }
                            else if (unitNode.Name == "General" || unitNode.Name == "Knights")
                            {
                                if(unitNode.Attributes?["rank"]?.Value is string rankStr && int.TryParse(rankStr, out int rank))
                                {
                                    availableUnit.Rank = rank;
                                }
                            }
                            else if (unitNode.Name == "Garrison")
                            {
                                if (unitNode.Attributes?["level"]?.Value is string levelStr && int.TryParse(levelStr, out int level))
                                {
                                    availableUnit.Level = level;
                                }
                            }

                            allUnits.Add(availableUnit);
                            uniqueUnitTracker.Add((factionName, key));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error reading or parsing faction file '{Path.GetFileName(xml_file)}' in GetAllAvailableUnits: {ex.Message}");
                }
            }

            return allUnits.OrderBy(u => u.FactionName).ThenBy(u => u.UnitType).ThenBy(u => u.DisplayName).ToList();
        }

        public static bool IsUnitKeySiege(string unitKey)
        {
            if (string.IsNullOrEmpty(unitKey) || LoadedUnitMapper_FolderPath == null) return false;

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            if (!Directory.Exists(factions_folder_path)) return false;

            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            files_paths.Reverse(); // Prioritize submods, as they can override base files

            foreach (var xml_file in files_paths)
            {
                try
                {
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue;

                    // Find a MenAtArm node with the matching key
                    XmlNode maaNode = FactionsFile.SelectSingleNode($"//MenAtArm[@key='{unitKey}']");
                    if (maaNode != null)
                    {
                        // If we find the key, we have our answer.
                        return maaNode.Attributes?["siege"]?.Value == "true";
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error reading or parsing faction file '{Path.GetFileName(xml_file)}' in IsUnitKeySiege: {ex.Message}");
                }
            }

            // If the key is not found in any file, it's not a siege unit (or not a MenAtArm).
            return false;
        }
        public static string? GetMenAtArmMaxCategory(string menAtArmType)
        {
            if (string.IsNullOrEmpty(menAtArmType) || LoadedUnitMapper_FolderPath == null) return null;

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            if (!Directory.Exists(factions_folder_path)) return null;

            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            files_paths.Reverse(); // Prioritize submods

            foreach (var xml_file in files_paths)
            {
                try
                {
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue;

                    // Find a MenAtArm node with the matching type attribute
                    XmlNode maaNode = FactionsFile.SelectSingleNode($"//MenAtArm[@type='{menAtArmType}']");
                    if (maaNode != null)
                    {
                        return maaNode.Attributes?["max"]?.Value;
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Debug($"Error reading or parsing faction file '{Path.GetFileName(xml_file)}' in GetMenAtArmMaxCategory: {ex.Message}");
                }
            }

            return null; // Not found
        }
    }
}
