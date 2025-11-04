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
        public List<string> Replaces { get; set; } = new List<string>();
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
        public List<SettlementVariant> Variants { get; private set; } = new List<SettlementVariant>();
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

        internal TerrainsUM(string attilaMap, List<(string building, string x, string y)> historicalMaps, List<(string terrain, string x, string y)> normalMaps, List<SettlementMap> settlementMaps, List<UniqueSettlementMap> uniqueSettlementMaps)
        {
            AttilaMap = attilaMap;
            HistoricalMaps = historicalMaps;    
            NormalMaps = normalMaps;
            SettlementMaps = settlementMaps;
            UniqueSettlementMaps = uniqueSettlementMaps;
        }

        public string GetAttilaMap() { return AttilaMap; }
        public List<(string building, string x, string y)> GetHistoricalMaps() { return HistoricalMaps; }
        public List<(string terrain, string x, string y)> GetNormalMaps() { return NormalMaps; }
        public List<SettlementMap> GetSettlementMaps() { return SettlementMaps; }
        public List<UniqueSettlementMap> GetUniqueSettlementMaps() { return UniqueSettlementMaps; }

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

        public static (List<ModFile> requiredMods, List<Submod> submods) GetUnitMappersModsCollectionFromTag(string tag)
        {
            var unit_mappers_folder = Directory.GetDirectories(@".\unit mappers");
            var requiredMods = new List<ModFile>();
            var submods = new List<Submod>();

            foreach (var mapper in unit_mappers_folder)
            {
                string? mapperName = Path.GetDirectoryName(mapper);
                var files = Directory.GetFiles(mapper);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName == "tag.txt")
                    {
                        string fileTag = File.ReadAllText(file).Trim(); // Trim whitespace from the file content
                        if (tag == fileTag)
                        {
                            string modsPath = mapper + @"\Mods.xml";
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
                                                if(submod_modNode.Name == "Mod")
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
                                            if(!string.IsNullOrEmpty(submod.Tag))
                                            {
                                                submods.Add(submod);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show($"Mods.xml was not found in {mapper}", "Crusader Conflicts: Crusader Conflicts: Unit Mappers Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            }
                            break;
                        }
                    }
                }
            }

            return (requiredMods, submods);
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
                case "OfficialCC_TheFallenEagle_FireforgedEmpires":
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

        private static int GetDeterministicIndex(string input, int listCount)
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

            if (LoadedUnitMapper_FolderPath == null || !Directory.Exists($@"{LoadedUnitMapper_FolderPath}\terrains")) { Terrains = null; return; }

            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var terrainFiles = GetSortedFilePaths($@"{LoadedUnitMapper_FolderPath}\terrains", priorityFilePattern);

            try
            {
                string attilaMap = "";
                var historicMaps = new Dictionary<string, (string x, string y)>();
                var normalMapsByTerrain = new Dictionary<string, List<(string x, string y)>>();
                var settlementMapsByCompositeKey = new Dictionary<(string faction, string battleType, string provinces), SettlementMap>();
                var uniqueSettlementMapsByCompositeKey = new Dictionary<(string battleType, string provinces), UniqueSettlementMap>();
                var siegeEngines = new Dictionary<string, SiegeEngine>();

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
                    }
                }

                var historicMapsList = historicMaps.Select(kvp => (kvp.Key, kvp.Value.x, kvp.Value.y)).ToList();
                var normalMapsList = normalMapsByTerrain.SelectMany(kvp => kvp.Value.Select(map => (terrain: kvp.Key, x: map.x, y: map.y))).ToList();
                var settlementMapsList = settlementMapsByCompositeKey.Values.ToList();
                var uniqueSettlementMapsList = uniqueSettlementMapsByCompositeKey.Values.ToList();
                SiegeEngines.AddRange(siegeEngines.Values);

                Terrains = new TerrainsUM(attilaMap, historicMapsList, normalMapsList, settlementMapsList, uniqueSettlementMapsList);
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
            var unit_mappers_folder = Directory.GetDirectories(@".\unit mappers");
            List<string> requiredMods = new List<string>();

            foreach (var mapper in unit_mappers_folder)
            {
                string mapperName = Path.GetFileName(mapper); // Changed GetDirectoryName to GetFileName
                var files = Directory.GetFiles(mapper);
                foreach (var file in files)
                    {
                    string fileName = Path.GetFileName(file);
                    if (fileName == "tag.txt")
                    {
                        string fileTag = File.ReadAllText(file).Trim();
                        if (tag == fileTag)
                        {
                            // TIME PERIOD
                            int startYear = -1, endYear = -1;
                            bool isDefault = false;
                            string timePeriodPath = mapper + @"\Time Period.xml";
                            if (File.Exists(timePeriodPath))
                            {
                                XmlDocument xmlDocument = new XmlDocument();
                                xmlDocument.Load(timePeriodPath);
                                if (xmlDocument.DocumentElement == null) continue; // Added null check
                                string startYearStr = xmlDocument.SelectSingleNode("//StartDate")?.InnerText ?? "Default";
                                string endYearStr = xmlDocument.SelectSingleNode("//EndDate")?.InnerText ?? "Default";
                                
                                if(startYearStr == "Default" || startYearStr == "DEFAULT")
                                {
                                    isDefault = true;
                                    startYear = 0;
                                    endYear = 0;
                                }

                                if(!int.TryParse(startYearStr, out startYear))
                                {
                                    isDefault = true;
                                    startYear = 0;
                                    endYear = 0;
                                }

                                if (!int.TryParse(endYearStr, out endYear))
                                {
                                    isDefault = true;
                                    startYear = 0;
                                    endYear = 0;
                                }

                                if(startYear != -1 && endYear != -1)
                                {

                                    if((Date.Year >= startYear && Date.Year <= endYear) || isDefault)
                                    {
                                        //  MODS
                                        string modsPath = mapper + @"\Mods.xml";
                                        if (File.Exists(modsPath))
                                        {
                                            XmlDocument xmlMods = new XmlDocument();
                                            xmlMods.Load(modsPath);
                                            if (xmlMods.DocumentElement != null) // Added null check
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
                                                        // This part is for adding active submod files to the load order
                                                        string? submodTag = node.Attributes?["submod_tag"]?.Value;
                                                        if (!string.IsNullOrEmpty(submodTag) && activeSubmods.Contains(submodTag))
                                                        {
                                                            foreach (XmlNode submod_modNode in node.ChildNodes)
                                                            {
                                                                if (submod_modNode.Name == "Mod")
                                                                {
                                                                    requiredMods.Add(submod_modNode.InnerText);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            LoadedUnitMapper_FolderPath = mapper;
                                            ReadTerrainsFile();
                                            return requiredMods;
                                        }
                                        else
                                        {
                                            MessageBox.Show($"Mods.xml was not found in {mapper}", "Crusader Conflicts: Unit Mappers Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show($"Time Period.xml was not found in {mapper}", "Crusader Conflicts: Unit Mappers Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            }
                            break;
                        }
                    }
                }
            }

            return requiredMods;
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
            if (LoadedUnitMapper_Model API Response Error. Please retry the previous request