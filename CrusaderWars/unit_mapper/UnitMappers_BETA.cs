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

namespace CrusaderWars.unit_mapper
{
    internal class SettlementVariant
    {
        public string Key { get; set; } = string.Empty;
        public string X { get; set; } = string.Empty;
        public string Y { get; set; } = string.Empty;
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

        public static TerrainsUM? Terrains { get;private set; }  
        static string? LoadedUnitMapper_FolderPath { get; set; }
        public static string? ActivePlaythroughTag { get; private set; }
        public const string NOT_FOUND_KEY = "not_found";
        private static readonly Random _random = new Random();
        private static Dictionary<string, (string X, string Y)> _provinceMapCache = new Dictionary<string, (string X, string Y)>();

        public static List<SiegeEngine> SiegeEngines { get; private set; } = new List<SiegeEngine>();

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
                return Math.Abs(hashAsInt % listCount);
            }
        }

        private static List<string> GetSortedFilePaths(string directoryPath, string priorityFilePattern)
        {
            var allFiles = Directory.GetFiles(directoryPath, "*.xml").ToList();

            if (string.IsNullOrEmpty(priorityFilePattern))
            {
                // If no pattern, just sort alphabetically
                return allFiles.OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase).ToList();
            }

            var priorityFiles = new List<string>();
            var otherFiles = new List<string>();

            // Simple wildcard matching for '*' at the end
            string patternStart = priorityFilePattern.TrimEnd('*');

            foreach (var file in allFiles)
            {
                if (Path.GetFileName(file).StartsWith(patternStart, StringComparison.OrdinalIgnoreCase))
                {
                    priorityFiles.Add(file);
                }
                else
                {
                    otherFiles.Add(file);
                }
            }

            // Sort both lists alphabetically
            priorityFiles.Sort((a, b) => String.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
            otherFiles.Sort((a, b) => String.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));

            // Combine the lists
            priorityFiles.AddRange(otherFiles);
            return priorityFiles;
        }

        private static void ReadTerrainsFile()
        {
            SiegeEngines.Clear(); // Clear the list to prevent duplicate data on re-read

            if(LoadedUnitMapper_FolderPath == null || !Directory.Exists($@"{LoadedUnitMapper_FolderPath}\terrains")) { Terrains = null; return; }

            var terrainFiles = Directory.GetFiles($@"{LoadedUnitMapper_FolderPath}\terrains");

            try
            {
                string attilaMap = "";
                var historicMaps = new List<(string building, string x, string y)>();
                var normalMaps = new List<(string terrain, string x, string y)>();
                var settlementMaps = new List<SettlementMap>();
                var uniqueSettlementMaps = new List<UniqueSettlementMap>(); // Declare new list for unique settlement maps

                foreach (var file in terrainFiles)
                {
                    XmlDocument TerrainsFile = new XmlDocument();
                    TerrainsFile.Load(file);
                    if (TerrainsFile.DocumentElement == null) continue; // Added null check

                    foreach (XmlElement Element in TerrainsFile.DocumentElement.ChildNodes)
                    {
                        if (Element.Name == "Attila_Map" || Element.Name == "Map") // Updated condition
                        {
                            if (Element.Name == "Map")
                            {
                                attilaMap = Element.Attributes?["name"]?.Value ?? string.Empty;
                            }
                            else
                            {
                                attilaMap = Element.InnerText;
                            }
                        }
                        else if (Element.Name == "Historic_Maps")
                        {
                            foreach (XmlElement historic_map in Element.ChildNodes)
                            {
                                string building = historic_map.Attributes["ck3_building_key"]?.Value ?? string.Empty;
                                string x = historic_map.Attributes["x"]?.Value ?? string.Empty;
                                string y = historic_map.Attributes["y"]?.Value ?? string.Empty;
                                historicMaps.Add((building, x, y));
                            }
                        }
                        else if (Element.Name == "Normal_Maps")
                        {
                            foreach (XmlElement terrain_type in Element.ChildNodes)
                            {
                                string terrain = terrain_type.Attributes["ck3_name"]?.Value ?? string.Empty;
                                foreach (XmlElement map in terrain_type.ChildNodes)
                                {
                                    string x = map.Attributes["x"]?.Value ?? string.Empty;
                                    string y = map.Attributes["y"]?.Value ?? string.Empty;
                                    normalMaps.Add((terrain, x, y));

                                }
                            }
                        }
                        else if (Element.Name == "Settlement_Maps") // Block for generic Settlement_Maps
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
                                    string provinceNamesAttr = settlementNode.Attributes?["province_names"]?.Value;
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
                                                // Removed IsUnique property parsing
                                            };

                                            XmlElement? mapNode = variantNode.SelectSingleNode("Map") as XmlElement;
                                            if (mapNode != null)
                                            {
                                                settlementVariant.X = mapNode.Attributes?["x"]?.Value ?? string.Empty;
                                                settlementVariant.Y = mapNode.Attributes?["y"]?.Value ?? string.Empty;
                                            }
                                            settlementMap.Variants.Add(settlementVariant);
                                        }
                                    }
                                    settlementMaps.Add(settlementMap);
                                }
                            }
                        }
                        else if (Element.Name == "Settlement_Maps_Unique") // New block for Unique Settlement Maps
                        {
                            foreach (XmlElement settlementUniqueNode in Element.ChildNodes)
                            {
                                if (settlementUniqueNode.Name == "Settlement_Unique")
                                {
                                    var uniqueSettlementMap = new UniqueSettlementMap
                                    {
                                        BattleType = settlementUniqueNode.Attributes?["battle_type"]?.Value ?? string.Empty
                                    };
                                    string provinceNamesAttr = settlementUniqueNode.Attributes?["province_names"]?.Value;
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
                                                Key = variantNode.Attributes?["key"]?.Value ?? string.Empty
                                            };

                                            XmlElement? mapNode = variantNode.SelectSingleNode("Map") as XmlElement;
                                            if (mapNode != null)
                                            {
                                                settlementVariant.X = mapNode.Attributes?["x"]?.Value ?? string.Empty;
                                                settlementVariant.Y = mapNode.Attributes?["y"]?.Value ?? string.Empty;
                                            }
                                            uniqueSettlementMap.Variants.Add(settlementVariant);
                                        }
                                    }
                                    uniqueSettlementMaps.Add(uniqueSettlementMap);
                                }
                            }
                        }
                        else if (twbattle.BattleState.IsSiegeBattle && Element.Name == "Siege_Engines")
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
                                    SiegeEngines.Add(siegeEngine);
                                }
                            }
                        }
                    }
                }

                Terrains = new TerrainsUM(attilaMap, historicMaps, normalMaps, settlementMaps, uniqueSettlementMaps); // Updated constructor call
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading {GetLoadedUnitMapperName()} terrains file: {ex.Message}", "Crusader Conflicts: Unit Mapper Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }

        }

        public static List<string> GetUnitMapperModFromTagAndTimePeriod(string tag)
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
                Program.Logger.Debug("CRITICAL ERROR: LoadedUnitMapper_FolderPath is not set. Cannot get unit max.");
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
                Program.Logger.Debug("CRITICAL ERROR: LoadedUnitMapper_FolderPath is not set. Cannot get subculture.");
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

                if (garrison_node.Attributes?["key"]?.Value is string keyStr)
                {
                    key = keyStr;
                }
                else
                {
                    Program.Logger.Debug($"WARNING: Missing 'key' attribute for garrison in faction '{attila_faction}'. Defaulting to empty string.");
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


        public static List<(int porcentage, string unit_key, string name, string max)> GetFactionLevies(string attila_faction)
        {
            Program.Logger.Debug($"Getting faction levies: '{attila_faction}'");
            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("CRITICAL ERROR: LoadedUnitMapper_FolderPath is not set. Cannot get faction levies.");
                throw new Exception("Unit mapper folder path not configured");
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            List<(int porcentage, string unit_key, string name, string max)> specificLevies = new List<(int porcentage, string unit_key, string name, string max)>();
            List<(int porcentage, string unit_key, string name, string max)> defaultLevies = new List<(int porcentage, string unit_key, string name, string max)>();

            foreach (var xml_file in files_paths)
            {
                if (Path.GetExtension(xml_file) == ".xml")
                {
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue;

                    // Check for specific faction levies and overwrite if found
                    var foundSpecific = Levies(FactionsFile, attila_faction);
                    if (foundSpecific.Any())
                    {
                        specificLevies = foundSpecific;
                        Program.Logger.Debug($"Found/overwrote specific levy definitions for faction '{attila_faction}' from file '{Path.GetFileName(xml_file)}'.");
                    }

                    // Check for default faction levies and overwrite if found
                    var foundDefault = Levies(FactionsFile, "Default");
                    if (foundDefault.Any())
                    {
                        defaultLevies = foundDefault;
                        Program.Logger.Debug($"Found/overwrote default levy definitions from file '{Path.GetFileName(xml_file)}'.");
                    }
                }
            }

            // Prioritize specific levies over default ones
            if (specificLevies.Any())
            {
                Program.Logger.Debug($"Using specific levy definitions for faction '{attila_faction}'.");
                return specificLevies;
            }

            if (defaultLevies.Any())
            {
                Program.Logger.Debug($"No specific levy definitions found for faction '{attila_faction}'. Using 'Default' faction definitions.");
                return defaultLevies;
            }


            // If neither loop finds any levies, throw an exception
            throw new Exception($"Unit Mapper Error: Could not find any levy definitions for faction '{attila_faction}' or for the 'Default' faction. Please check your unit mapper configuration.");
        }

        public static List<(int percentage, string unit_key, string name, string max)> GetFactionGarrison(string attila_faction, int holdingLevel)
        {
            if (!twbattle.BattleState.IsSiegeBattle)
            {
                Program.Logger.Debug("GetFactionGarrison called for a non-siege battle. Returning empty list as a safeguard.");
                return new List<(int, string, string, string)>();
            }

            Program.Logger.Debug($"Getting faction garrison for: '{attila_faction}' at holding level: {holdingLevel}");
            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("CRITICAL ERROR: LoadedUnitMapper_FolderPath is not set. Cannot get faction garrison.");
                throw new Exception("Unit mapper folder path not configured");
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            
            List<(int percentage, string unit_key, string name, string max, int level)> allGarrisonDefinitions = new List<(int percentage, string unit_key, string name, string max, int level)>();

            foreach (var xml_file in files_paths)
            {
                if (Path.GetExtension(xml_file) == ".xml")
                {
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue;

                    // Collect specific faction garrisons
                    var foundSpecific = Garrison(FactionsFile, attila_faction);
                    if (foundSpecific.Any())
                    {
                        allGarrisonDefinitions.AddRange(foundSpecific);
                        Program.Logger.Debug($"Found specific garrison definitions for faction '{attila_faction}' from file '{Path.GetFileName(xml_file)}'.");
                    }

                    // Collect default faction garrisons
                    var foundDefault = Garrison(FactionsFile, "Default");
                    if (foundDefault.Any())
                    {
                        allGarrisonDefinitions.AddRange(foundDefault);
                        Program.Logger.Debug($"Found default garrison definitions from file '{Path.GetFileName(xml_file)}'.");
                    }
                }
            }

            List<(int percentage, string unit_key, string name, string max)> finalGarrisonComposition;

            // Attempt to find garrisons at or below the current holding level
            var suitableGarrisons = allGarrisonDefinitions.Where(g => g.level <= holdingLevel).ToList();

            if (suitableGarrisons.Any())
            {
                // If suitable garrisons are found, use the highest level among them
                int highestAvailableLevel = suitableGarrisons.Max(g => g.level);
                Program.Logger.Debug($"Found suitable garrisons. Using highest available level: {highestAvailableLevel}");
                finalGarrisonComposition = suitableGarrisons
                                            .Where(g => g.level == highestAvailableLevel)
                                            .Select(g => (g.percentage, g.unit_key, g.name, g.max))
                                            .ToList();
            }
            else
            {
                // Fallback: No garrisons at or below the holding level, so use the lowest level available
                if (!allGarrisonDefinitions.Any())
                {
                    throw new Exception($"Unit Mapper Error: No garrison definitions found at all for faction '{attila_faction}'.");
                }

                int lowestAvailableLevel = allGarrisonDefinitions.Min(g => g.level);
                Program.Logger.Debug($"No garrisons found at or below holding level {holdingLevel}. Falling back to lowest available level: {lowestAvailableLevel}");
                finalGarrisonComposition = allGarrisonDefinitions
                                            .Where(g => g.level == lowestAvailableLevel)
                                            .Select(g => (g.percentage, g.unit_key, g.name, g.max))
                                            .ToList();
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
                Program.Logger.Debug("CRITICAL ERROR: LoadedUnitMapper_FolderPath is not set. Cannot search in titles file.");
                return (NOT_FOUND_KEY, false);
            }

            string titles_folder_path = LoadedUnitMapper_FolderPath + @"\Titles";
            if (!Directory.Exists(titles_folder_path)) return (NOT_FOUND_KEY, false);
            var files_paths = Directory.GetFiles(titles_folder_path);

            var owner = unit.GetOwner();
            if(owner == null || owner.GetPrimaryTitleKey() == string.Empty)
                return (NOT_FOUND_KEY, false);
            
            //LEVIES skip
            if (unit.GetRegimentType() == RegimentType.Levy) return (NOT_FOUND_KEY, false);
            //Garrison units also skip this, as their keys are set directly
            if (unit.GetRegimentType() == RegimentType.Garrison) return (NOT_FOUND_KEY, false); // Changed from unit.GetName() == "Garrison"

            (string key, bool isSiege) unit_data = (string.Empty, false);
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
                            foreach (XmlNode node in element.ChildNodes)
                            {
                                unit_data = FindUnitKeyInFaction(element, unit);
                                return unit_data;
                            }
                        }
                    }
                }
            }

            return (NOT_FOUND_KEY, false);
        }

        static (string, bool) SearchInFactionFiles(Unit unit)
        {
            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("CRITICAL ERROR: LoadedUnitMapper_FolderPath is not set. Cannot search in faction files.");
                return (NOT_FOUND_KEY, false);
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);

            //LEVIES skip
            if (unit.GetRegimentType() == RegimentType.Levy) return (NOT_FOUND_KEY, false) ;
            //Garrison units also skip this, as their keys are set directly
            if (unit.GetRegimentType() == RegimentType.Garrison) return (NOT_FOUND_KEY, false); // Changed from unit.GetName() == "Garrison"

            (string key, bool isSiege) specific_unit = (NOT_FOUND_KEY, false);
            (string key, bool isSiege) default_unit = (NOT_FOUND_KEY, false);
            foreach (var xml_file in files_paths)
            {
                if (Path.GetExtension(xml_file) == ".xml")
                {
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue; // Added null check

                    //MAA|COMMANDER|KNIGHT
                    foreach (XmlNode element in FactionsFile.DocumentElement.ChildNodes)
                    {
                        if (element is XmlComment) continue;
                        if (element is XmlElement)
                        {
                            string faction = element.Attributes?["name"]?.Value ?? string.Empty;

                            //Store Default unit key first
                            if (faction == "Default" || faction == "DEFAULT")
                            {
                                var (foundKey, foundIsSiege) = FindUnitKeyInFaction(element, unit);
                                if (foundKey != NOT_FOUND_KEY)
                                {
                                    default_unit = (foundKey, foundIsSiege); // Overwrite default key
                                }
                            }
                            //Then stores culture specific unit key
                            else if (faction == unit.GetAttilaFaction())
                            {
                                var (foundKey, foundIsSiege) = FindUnitKeyInFaction(element, unit);
                                if (foundKey != NOT_FOUND_KEY)
                                {
                                    specific_unit = (foundKey, foundIsSiege); // Overwrite specific key
                                }
                            }
                        }
                    }
                }
            }

            if (specific_unit.key != NOT_FOUND_KEY)
            {
                return specific_unit;
            }

            return default_unit;
        }

        static (string, bool) FindUnitKeyInFaction(XmlNode factionElement, Unit unit)
        {
            if (unit.GetRegimentType() == RegimentType.Commander)
            {
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
                    // Map CK3 title Rank to a required rank level for the general unit
                    int requiredRank = 1;
                    if (unit.CharacterRank >= 4) requiredRank = 3; // King or Emperor gets rank 3 general
                    else if (unit.CharacterRank == 3) requiredRank = 2; // Duke gets rank 2 general

                    // Find all candidates at or below the required rank
                    var suitableCandidates = generalRanks.Where(t => t.rank <= requiredRank).ToList();

                    List<(int rank, string key)> finalSelectionPool;

                    if (suitableCandidates.Any())
                    {
                        // Find the best rank among the suitable candidates
                        int bestRank = suitableCandidates.Max(t => t.rank);
                        finalSelectionPool = suitableCandidates.Where(t => t.rank == bestRank).ToList();
                    }
                    else
                    {
                        // Fallback: No suitable rank found, so use the lowest available rank overall
                        int lowestRank = generalRanks.Min(t => t.rank);
                        finalSelectionPool = generalRanks.Where(t => t.rank == lowestRank).ToList();
                    }

                    // Randomly select one candidate from the final pool
                    if (finalSelectionPool.Any())
                    {
                        int index = _random.Next(finalSelectionPool.Count);
                        return (finalSelectionPool[index].key, false);
                    }
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

                    var suitableCandidates = knightRanks.Where(t => t.rank <= requiredRank).ToList();
                    List<(int rank, string key)> finalSelectionPool;

                    if (suitableCandidates.Any())
                    {
                        int bestRank = suitableCandidates.Max(t => t.rank);
                        finalSelectionPool = suitableCandidates.Where(t => t.rank == bestRank).ToList();
                    }
                    else
                    {
                        int lowestRank = knightRanks.Min(t => t.rank);
                        finalSelectionPool = knightRanks.Where(t => t.rank == lowestRank).ToList();
                    }

                    // Randomly select one candidate from the final pool
                    if (finalSelectionPool.Any())
                    {
                        int index = _random.Next(finalSelectionPool.Count);
                        return (finalSelectionPool[index].key, false);
                    }
                }
            }
            else
            {
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
                            if (node?.Attributes?["key"] != null)
                            {
                                string? unit_key = node.Attributes["key"]?.Value;
                                bool isSiege = node.Attributes?["siege"]?.Value == "true";
                                if (unit_key != null) return (unit_key, isSiege);
                            }
                        }
                    }
                }
            }

            return (NOT_FOUND_KEY, false);
        }

        private static int CalculateAttilaSiegeUnitSoldiers(int ck3SiegeWeaponCount)
        {
            // This formula calculates the minimum number of soldiers required to produce
            // the target number of siege engines, based on Attila's rounding logic
            // (round(soldiers / 4.0) = engines).
            if (ck3SiegeWeaponCount <= 0) return 0;
            if (ck3SiegeWeaponCount == 1) return 3; // Special case for 1 engine, minimum crew is 3.
            return (ck3SiegeWeaponCount * 4) - 2;
        }

        private static (string, bool) ProcessUnitKeyResult(Unit unit, string key, bool isSiege)
        {
            unit.SetIsSiege(isSiege);
            if (isSiege)
            {
                int machineCount = unit.GetSoldiers();
                int attilaSoldiers = CalculateAttilaSiegeUnitSoldiers(machineCount);
                unit.SetSoldiers(attilaSoldiers);
                Program.Logger.Debug($"  - SIEGE UNIT: Converted '{unit.GetName()}' from {machineCount} machines to {attilaSoldiers} soldiers.");
            }
            return (key, isSiege);
        }


        public static (string, bool) GetUnitKey(Unit unit)
        {
            // If the unit is a Garrison unit, its key is already set directly from the XML
            if (unit.GetRegimentType() == RegimentType.Garrison && unit.GetAttilaUnitKey() != string.Empty) // Changed from unit.GetName() == "Garrison"
            {
                return ProcessUnitKeyResult(unit, unit.GetAttilaUnitKey(), false); // Garrison units are not siege weapons
            }

            var (unit_key, isSiege) = SearchInTitlesFile(unit);
            if (unit_key != NOT_FOUND_KEY)
            {
                return ProcessUnitKeyResult(unit, unit_key, isSiege);
            }

            (unit_key, isSiege) = SearchInFactionFiles(unit);
            if (unit_key != NOT_FOUND_KEY)
            {
                return ProcessUnitKeyResult(unit, unit_key, isSiege);
            }

            // Fallback to default unit if no specific mapping is found
            var (default_key, defaultIsSiege) = GetDefaultUnitKey(unit.GetRegimentType());
            if (default_key != NOT_FOUND_KEY)
            {
                Program.Logger.Debug($"  - INFO: Could not map CK3 Unit '{unit.GetName()}' (Type: {unit.GetRegimentType()}). Substituting with default Attila unit '{default_key}'.");
                return ProcessUnitKeyResult(unit, default_key, defaultIsSiege);
            }

            Program.Logger.Debug($"  - CRITICAL: Could not map CK3 Unit '{unit.GetName()}' (Type: {unit.GetRegimentType()}). All mapping attempts including default fallback failed.");
            return ProcessUnitKeyResult(unit, NOT_FOUND_KEY, false); // This will be the found key or NOT_FOUND_KEY
        }

        public static (string, bool) GetDefaultUnitKey(RegimentType type)
        {
            if (type == RegimentType.Levy) return (NOT_FOUND_KEY, false); // Levies are handled separately
            if (type == RegimentType.Garrison) return (NOT_FOUND_KEY, false); // Garrison units are handled separately // Changed from `if (type == RegimentType.Levy && type == RegimentType.MenAtArms)`

            if (LoadedUnitMapper_FolderPath == null)
            {
                Program.Logger.Debug("CRITICAL ERROR: LoadedUnitMapper_FolderPath is not set. Cannot get default unit key.");
                return (NOT_FOUND_KEY, false);
            }

            string factions_folder_path = LoadedUnitMapper_FolderPath + @"\Factions";
            if (!Directory.Exists(factions_folder_path)) return (NOT_FOUND_KEY, false);

            string priorityFilePattern = !string.IsNullOrEmpty(ActivePlaythroughTag) ? $"OfficialCC_{ActivePlaythroughTag}_*" : string.Empty;
            var files_paths = GetSortedFilePaths(factions_folder_path, priorityFilePattern);
            string found_key = NOT_FOUND_KEY;

            foreach (var xml_file in files_paths)
            {
                if (Path.GetExtension(xml_file) == ".xml")
                {
                    XmlDocument FactionsFile = new XmlDocument();
                    FactionsFile.Load(xml_file);
                    if (FactionsFile.DocumentElement == null) continue; // Added null check

                    foreach (XmlNode element in FactionsFile.DocumentElement.ChildNodes)
                    {
                        if (element is XmlComment) continue;
                        
                        var nameAttr = element.Attributes?["name"];
                        if (nameAttr == null) continue;
                        string faction = nameAttr.Value;

                        if (faction == "Default" || faction == "DEFAULT")
                        {
                            // Found the default faction, now find a suitable unit and overwrite if found
                            foreach (XmlNode node in element.ChildNodes)
                            {
                                if (node is XmlComment) continue;

                                string? current_key = node.Attributes?["key"]?.Value;
                                if (current_key == null) continue;
                                
                                if (type == RegimentType.Commander && node.Name == "General")
                                {
                                    found_key = current_key;
                                }
                                else if (type == RegimentType.Knight && node.Name == "Knights")
                                {
                                    found_key = current_key;
                                }
                                else if (type == RegimentType.MenAtArms && node.Name == "MenAtArm")
                                {
                                    // Overwrite with the last MAA unit found as a generic fallback
                                    found_key = current_key;
                                }
                                else if (node.Name == "Garrison") // Default garrison unit
                                {
                                    // This is a generic fallback for garrison if no specific one is found.
                                    // The actual garrison units are determined by GetFactionGarrison.
                                    // This might be used if a unit is somehow created as "Garrison" but without a specific key.
                                    found_key = current_key;
                                }
                            }
                        }
                    }
                }
            }
            return (found_key, false); // Return last found key, default to not siege weapon
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

            string heritage_faction = "";
            string culture_faction = "";

            if (string.IsNullOrEmpty(LoadedUnitMapper_FolderPath))
            {
                Program.Logger.Debug("CRITICAL ERROR: LoadedUnitMapper_FolderPath is not set. Cannot get Attila faction.");
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
                    XmlDocument CulturesFile = new XmlDocument();
                    CulturesFile.Load(xml_file);

                    if (CulturesFile.DocumentElement == null) continue; // Add null check for DocumentElement
                    foreach(XmlNode heritage in CulturesFile.DocumentElement.ChildNodes)
                    {
                        if (heritage is XmlComment) continue;

                        string heritage_name = heritage.Attributes?["name"]?.Value ?? string.Empty;                       

                        if(heritage_name == HeritageName)
                        {
                            string found_heritage_faction = heritage.Attributes?["faction"]?.Value ?? string.Empty;
                            if (!string.IsNullOrEmpty(found_heritage_faction))
                            {
                                heritage_faction = found_heritage_faction;
                                Program.Logger.Debug($"Matched heritage: {HeritageName}->faction:{heritage_faction}");
                            }

                            foreach(XmlNode culture in heritage.ChildNodes)
                            {
                                if (culture is XmlComment) continue; 
                                string culture_name = culture.Attributes?["name"]?.Value ?? string.Empty;

                                if (culture_name == CultureName && !string.IsNullOrEmpty(CultureName))
                                {
                                    string found_culture_faction = culture.Attributes?["faction"]?.Value ?? string.Empty;
                                    if (!string.IsNullOrEmpty(found_culture_faction))
                                    {
                                        culture_faction = found_culture_faction;
                                        Program.Logger.Debug($"Matched culture: {CultureName}->faction:{culture_faction}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            string faction = "";
            if (!string.IsNullOrEmpty(culture_faction))
            {
                faction = culture_faction;
            }
            else if (!string.IsNullOrEmpty(heritage_faction))
            {
                faction = heritage_faction;
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
                Program.Logger.Debug($"Faction not found for Culture '{CultureName}', Heritage '{HeritageName}' and fallback to Default/Default failed.");
            }
            else
            {
                Program.Logger.Debug($"Found faction '{faction}' for Culture '{CultureName}', Heritage '{HeritageName}'.");
            }

            return faction;
        }

        public static (string X, string Y)? GetSettlementMap(string faction, string battleType, string provinceName)
        {
            Program.Logger.Debug($"Attempting to get settlement map for Faction: '{faction}', BattleType: '{battleType}', Province: '{provinceName}'");

            string cacheKey = $"{provinceName}_{battleType}";
            if (_provinceMapCache.TryGetValue(cacheKey, out var cachedMap))
            {
                Program.Logger.Debug($"Found cached settlement map for '{cacheKey}'. Coordinates: ({cachedMap.X}, {cachedMap.Y})");
                return cachedMap;
            }

            // Priority 1: Unique Map by province_names attribute
            if (Terrains?.UniqueSettlementMaps != null)
            {
                var uniqueMapByProvName = Terrains.UniqueSettlementMaps
                    .FirstOrDefault(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase) &&
                                           sm.ProvinceNames.Any(p => provinceName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0));
                
                if (uniqueMapByProvName != null && uniqueMapByProvName.Variants.Any())
                {
                    Program.Logger.Debug($"Found unique settlement map by 'province_names' attribute for Province '{provinceName}'.");
                    int deterministicIndex = GetDeterministicIndex(provinceName, uniqueMapByProvName.Variants.Count);
                    var selectedVariant = uniqueMapByProvName.Variants[deterministicIndex];
                    _provinceMapCache[cacheKey] = (selectedVariant.X, selectedVariant.Y);
                    return (selectedVariant.X, selectedVariant.Y);
                }
            }

            // Priority 2: Unique Map by Variant key (existing logic)
            if (Terrains?.UniqueSettlementMaps != null)
            {
                var matchingUniqueMaps = Terrains.UniqueSettlementMaps
                                         .Where(sm => sm.BattleType.Equals(battleType, StringComparison.OrdinalIgnoreCase))
                                         .ToList();

                foreach (var uniqueMap in matchingUniqueMaps)
                {
                    var uniqueMatch = uniqueMap.Variants.FirstOrDefault(v => v.Key.IndexOf(provinceName, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (uniqueMatch != null)
                    {
                        Program.Logger.Debug($"Found unique settlement map variant '{uniqueMatch.Key}' for Province '{provinceName}'. Coordinates: ({uniqueMatch.X}, {uniqueMatch.Y})");
                        _provinceMapCache[cacheKey] = (uniqueMatch.X, uniqueMatch.Y);
                        return (uniqueMatch.X, uniqueMatch.Y);
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

                if (genericMapByProvName != null && genericMapByProvName.Variants.Any())
                {
                    Program.Logger.Debug($"Found generic settlement map for Faction '{genericMapByProvName.Faction}' by 'province_names' attribute for Province '{provinceName}'.");
                    int deterministicIndex = GetDeterministicIndex(provinceName, genericMapByProvName.Variants.Count);
                    var selectedVariant = genericMapByProvName.Variants[deterministicIndex];
                    _provinceMapCache[cacheKey] = (selectedVariant.X, selectedVariant.Y);
                    return (selectedVariant.X, selectedVariant.Y);
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
                string usedFactionForLog = faction;

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
                        var selectedVariant = allGenericVariants[deterministicIndex];
                        Program.Logger.Debug($"Deterministically selected settlement map variant '{selectedVariant.Key}' for Faction '{usedFactionForLog}', BattleType '{battleType}'. Coordinates: ({selectedVariant.X}, {selectedVariant.Y})");
                        _provinceMapCache[cacheKey] = (selectedVariant.X, selectedVariant.Y);
                        return (selectedVariant.X, selectedVariant.Y);
                    }
                }
            }

            // Final Fallback
            Program.Logger.Debug($"No suitable settlement map variant found for Faction: '{faction}', BattleType: '{battleType}', Province: '{provinceName}'. Returning null.");
            return null;
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
    }
}
