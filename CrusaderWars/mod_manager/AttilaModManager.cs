using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.Versioning;
using System.Xml.Linq;
using System.Xml.Schema;
using CrusaderWars.games;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.mod_manager
{
    enum ModLocalization
    {
        Steam,
        Data
    };
    class Mod
    {
        bool Enabled { get; set; }
        Bitmap? Image { get; set; } // Changed to nullable Bitmap
        string Name { get; set; }
        ModLocalization Localization { get; set; }
        string FullPath { get; set; }
        bool RequiredMod { get; set; }
        bool LoadingMod { get; set; }
        int LoadOrder { get; set; }

        public Mod(bool isEnabled, Bitmap? pngImg, string name, ModLocalization local, string fullPath) // Changed to nullable Bitmap
        {
            Enabled = isEnabled;
            Image = pngImg;
            Name = name;
            Localization = local;
            FullPath = fullPath;
        }
        public void ChangeEnabledState(bool yn) { Enabled = yn; }
        public void IsRequiredMod(bool yn) { RequiredMod = yn; }
        public void IsLoadingRequiredMod(bool yn) { LoadingMod = yn; }
        public void SetLoadOrderValue(int orderNum) { LoadOrder = orderNum; }

        public int GetLoadOrderValue() { return LoadOrder; }
        public bool IsEnabled() { return Enabled; }
        public bool IsRequiredMod() { return RequiredMod; }
        public bool IsLoadingModRequiredMod() { return LoadingMod; }
        public Bitmap? GetThumbnail() { return Image; } // Changed return type to nullable Bitmap
        public string GetName() { return Name; }
        public ModLocalization GetLocalization() { return Localization; }
        public string GetFullPath() { return FullPath; }

        [SupportedOSPlatform("windows")]
        public void DisposeThumbnail() { Image?.Dispose(); Image = null; } // Used null-conditional operator
    }

    public class VerificationResult
    {
        public List<(string FileName, string? ScreenName, string? Url, string Game)> MissingFiles { get; } = new List<(string, string?, string?, string)>();
        public List<(string FileName, string ExpectedSha, string? ScreenName, string? Url)> MismatchedFiles { get; } = new List<(string, string, string?, string?)>();
    }

    [SupportedOSPlatform("windows")]
    public static class AttilaModManager
    {
        static DataGridView? ModManagerControl { get; set; }
        static List<Mod> ModsPaths { get; set; } = new List<Mod>();

        public static void SetControlReference(DataGridView dataGrid)
        {
            ModManagerControl = dataGrid;
        }

        [SupportedOSPlatform("windows")]
        static void RemoveRequiredMods(string gameType)
        {
            string[] unitMappers_folders = UnitMappers_BETA.GetMapperDirectories(gameType);

            foreach (var mapper in unitMappers_folders)
            {
                string[] files = Directory.GetFiles(mapper);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName == "Mods.xml")
                    {
                        try
                        {
                            XmlDocument ModsFile = new XmlDocument();
                            ModsFile.Load(file);
                            if (ModsFile.DocumentElement != null) // Added null check
                            {
                                foreach (XmlNode modNode in ModsFile.DocumentElement.ChildNodes)
                                {
                                    // Also mark submod packs as "required" so they are hidden from the optional list
                                    if (modNode.Name == "Mod")
                                    {
                                        ModsPaths.FirstOrDefault(x => x.GetName() == modNode.InnerText)?.IsRequiredMod(true);
                                    }
                                    else if (modNode.Name == "Submod")
                                    {
                                        foreach (XmlNode submod_modNode in modNode.ChildNodes)
                                        {
                                            if (submod_modNode.Name == "Mod")
                                            {
                                                ModsPaths.FirstOrDefault(x => x.GetName() == submod_modNode.InnerText)?.IsRequiredMod(true);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (XmlException ex)
                        {
                            string errorMessage = $"Error parsing XML file: {file}\n\nThis file is likely corrupted or has a syntax error. Please check the file and correct it.\n\nDetails: {ex.Message}";
                            Program.Logger.Debug($"[XML PARSE ERROR] {errorMessage}");
                            MessageBox.Show(errorMessage, "Crusader Conflicts: XML Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        [SupportedOSPlatform("windows")]
        public static void SetLoadingRequiredMods(List<string> requiredMods)
        {
            // Reset all mods' loading required status before setting new ones
            foreach (var mod in ModsPaths)
            {
                mod.IsLoadingRequiredMod(false);
            }

            foreach (var mod in ModsPaths)
            {
                if (mod.IsRequiredMod())
                {
                    foreach (var requiredMod in requiredMods)
                    {
                        if (mod.GetName() == requiredMod)
                        {
                            mod.IsLoadingRequiredMod(true);
                            mod.SetLoadOrderValue(requiredMods.IndexOf(requiredMod));
                            break;
                        }
                    }
                }
            }
        }

        public static string? GetWorkshopFolderPath(string gameType = "attila")
        {
            var gameService = games.GameServiceFactory.GetGameService(gameType);
            string gamePath = gameService.GetGamePath();
            if (string.IsNullOrEmpty(gamePath) || !System.IO.File.Exists(gamePath))
                return null;

            DirectoryInfo? dirInfo = new DirectoryInfo(Path.GetDirectoryName(gamePath)!);

            // Search upwards for the "steamapps" folder
            while (dirInfo != null && dirInfo.Name.ToLower() != "steamapps")
                dirInfo = dirInfo.Parent;

            // If "steamapps" was found, construct the workshop path
            if (dirInfo != null)
                return Path.Combine(dirInfo.FullName, "workshop", "content", gameService.GetSteamAppId());

            return null; // "steamapps" not found
        }

        [SupportedOSPlatform("windows")]
        public static void CreateUserModsFile(string gameType = "attila")
        {
            var gameService = games.GameServiceFactory.GetGameService(gameType);
            Program.Logger.Debug($"Creating user mods file for {gameType}...");
            // CREATE ESSENTIAL FILE TO OPEN THE GAME AUTOMATICALLY
            string gamePath = gameService.GetGamePath();
            string steam_app_id_path = gamePath.Replace(gameService.GetExecutableName(), "steam_appid.txt");
            if (!File.Exists(steam_app_id_path))
            {
                File.WriteAllText(steam_app_id_path, gameService.GetSteamAppId());
            }

            string userMods_path = gamePath.Replace(gameService.GetExecutableName(), gameService.GetModActivationFileName());

            // Clear existing content
            File.WriteAllText(userMods_path, "");

            // Determine which mods to exclude based on active submods with a 'replace' attribute.
            var modsToExclude = new HashSet<string>();
            if (UnitMappers_BETA.ActivePlaythroughTag != null)
            {
                var activeSubmodTags = SubmodManager.GetActiveSubmodsForPlaythrough(UnitMappers_BETA.ActivePlaythroughTag);
                if (activeSubmodTags.Any() && UnitMappers_BETA.AvailableSubmods.Any())
                {
                    var activeSubmodsWithReplacements = UnitMappers_BETA.AvailableSubmods
                        .Where(s => activeSubmodTags.Contains(s.Tag) && s.Replaces.Any());

                    foreach (var submod in activeSubmodsWithReplacements)
                    {
                        foreach (var modToReplace in submod.Replaces)
                        {
                            modsToExclude.Add(modToReplace);
                            Program.Logger.Debug($"Submod '{submod.Tag}' is active, excluding required mod '{modToReplace}'.");
                        }
                    }
                }
            }

            // Get ordered lists of mods
            var requiredMods = ModsPaths.Where(x => x.IsLoadingModRequiredMod()
                                                && !modsToExclude.Contains(x.GetName()))
                                       .OrderBy(x => x.GetLoadOrderValue())
                                       .ToList();

            // Optional mods are those enabled by the user AND NOT required by the current playthrough
            var optionalMods = ModsPaths.Where(x => x.IsEnabled() && !x.IsRequiredMod())
                                       .ToList();

            using (FileStream modsFile = File.Open(userMods_path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter sw = new StreamWriter(modsFile))
            {
                sw.NewLine = "\n";
                Program.Logger.Debug("Writing mods to used_mods_cc.txt with new priority order...");

                // Local function to write a single mod entry
                Action<Mod, string> writeModEntry = (mod, type) =>
                {
                    if (mod.GetLocalization() == ModLocalization.Steam)
                    {
                        string workingDirectory = mod.GetFullPath().Replace(@"\", @"/");
                        sw.Write($"add_working_directory \"{workingDirectory}\";\n");
                        Program.Logger.Debug($"  - {type} WD: {workingDirectory}");
                    }
                    sw.Write($"mod \"{mod.GetName()}\";\n");
                    Program.Logger.Debug($"  - {type} Mod: {mod.GetName()}");
                };

                // 1. CrusaderConflicts.pack (Highest priority, written first in file, loaded last by game)
                sw.Write($"mod \"CrusaderConflicts.pack\";\n");
                Program.Logger.Debug("  - Mod: CrusaderConflicts.pack (Highest Priority)");

                // 2. Optional (user-selected) mods
                foreach (var mod in optionalMods)
                {
                    writeModEntry(mod, "Optional");
                }

                // 3. Required mods for the playthrough (Lowest priority, written last in file, loaded first by game)
                foreach (var mod in requiredMods)
                {
                    writeModEntry(mod, "Required/Submod");
                }

                sw.Dispose();
                sw.Close();
            }
        }
        [SupportedOSPlatform("windows")]
        public static void ReadInstalledMods(string gameType = "attila")
        {
            var gameService = games.GameServiceFactory.GetGameService(gameType);
            Program.Logger.Debug($"Reading installed {gameType} mods...");
            string data_folder_path = gameService.GetModDataPath();
            string? workshop_folder_path = GetWorkshopFolderPath(gameType);

            ModsPaths = new List<Mod>();
            //Read data folder
            var dataModsPaths = Directory.GetFiles(data_folder_path);
            foreach (var file in dataModsPaths)
            {
                var fileName = Path.GetFileName(file);
                if (Path.GetExtension(fileName) == ".pack")
                {
                    // Skip vanilla game packs (base + DLC) — never shown as optional mods
                    if (IsBaseGamePack(fileName, gameType))
                    {
                        continue;
                    }
                    else
                    {
                        ModsPaths.Add(new Mod(false, LoadBitmapWithReducedSize(@".\data\mod_manager\noimage.png"), fileName, ModLocalization.Data, file));
                    }
                }
            }

            // Read steam workshop folder
            if (Directory.Exists(workshop_folder_path))
            {
                var steamModsFoldersPaths = Directory.GetDirectories(workshop_folder_path);
                foreach (var folder in steamModsFoldersPaths)
                {
                    var files = Directory.GetFiles(folder);
                    string name = "";
                    string image_path = "";
                    string fullPath = "";
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);

                        if (Path.GetExtension(fileName) == ".pack")
                        {
                            name = fileName;
                            fullPath = file.Replace($@"\{fileName}", "");
                        }
                        else if (Path.GetExtension(fileName) == ".png")
                        {
                            image_path = file;
                        }
                    }
                    if (name != string.Empty)
                    {
                        Bitmap? thumbnail = LoadBitmapWithReducedSize(image_path);
                        ModsPaths.Add(new Mod(false, thumbnail, name, ModLocalization.Steam, fullPath));
                    }
                }
            }

            //  REMOVE REQUIRED MODS
            //  to only show optional mods
            RemoveRequiredMods(gameType);


            //SET ACTIVE MODS
            SetActiveMods();
            Program.Logger.Debug($"Found {ModsPaths.Count} installed mods.");
        }

        [SupportedOSPlatform("windows")]
        public static void ReadInstalledModsAndPopulateModManager(string gameType = "attila")
        {
            ReadInstalledMods(gameType);

            //  SET AT MOD MANAGER
            Bitmap? steamImg = LoadBitmapWithReducedSize(@".\data\mod_manager\steamlogo.png");
            Bitmap? dataImg = LoadBitmapWithReducedSize(@".\data\mod_manager\folder.png");
            if (ModManagerControl != null)
            {
                foreach (var mod in ModsPaths)
                {
                    // Line 82 - Add null check
                    {
                        object[] rowData = new object[] { // Changed from object?[] to object[]
                            mod.IsEnabled(),
                            mod.GetThumbnail(),
                            mod.GetName(),
                            mod.GetLocalization() == ModLocalization.Steam ? steamImg : dataImg
                        };
                        ModManagerControl.Rows.Add(rowData);
                    }
                }
            }
        }

        static Bitmap? LoadBitmapWithReducedSize(string path)
        {
            string imageToLoad = path;
            if (string.IsNullOrEmpty(imageToLoad) || !File.Exists(imageToLoad))
            {
                imageToLoad = @".\data\mod_manager\noimage.png";
            }

            try
            {
                // Load into memory stream to avoid file lock
                byte[] imageBytes = File.ReadAllBytes(imageToLoad);
                using (var ms = new MemoryStream(imageBytes))
                using (var originalImage = new Bitmap(ms))
                {
                    // Create a smaller version of the image (thumbnail)
                    int thumbnailWidth = originalImage.Width / 2;
                    int thumbnailHeight = originalImage.Height / 2;
                    var thumbnail = new Bitmap(thumbnailWidth, thumbnailHeight);

                    using (var graphics = Graphics.FromImage(thumbnail))
                    {
                        graphics.DrawImage(originalImage, 0, 0, thumbnailWidth, thumbnailHeight);
                    }

                    return thumbnail;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Failed to load image '{imageToLoad}': {ex.Message}");
                return null;
            }
        }


        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("windows")]
        static void DisposeImages()
        {
            foreach (var mod in ModsPaths)
            {
                mod.DisposeThumbnail();
            }
        }
        [SupportedOSPlatform("windows")]
        public static void SaveActiveMods()
        {
            string activeModsPath = @".\data\mod_manager\active_mods.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(activeModsPath)!); // Ensure the folder exists before writing
            var activeMods = ModsPaths.Where(mod => mod.IsEnabled()).Select(x => x.GetName()).ToArray();
            File.WriteAllLines(activeModsPath, activeMods);
            DisposeImages();
        }

        [SupportedOSPlatform("windows")]
        public static void ChangeEnabledState(DataGridViewRow row)
        {
            string? stringValue = row.Cells[0].Value?.ToString();
            bool value = string.Equals(stringValue, "True", StringComparison.OrdinalIgnoreCase) || string.Equals(stringValue, "Active", StringComparison.OrdinalIgnoreCase);

            string? name = row.Cells[2].Value as string;

            if (name != null)
            {
                ModsPaths.FirstOrDefault(x => x.GetName() == name)?.ChangeEnabledState(value);
            }
        }

        [SupportedOSPlatform("windows")]
        static void SetActiveMods()
        {
            string activeModsPath = @".\data\mod_manager\active_mods.txt";
            if (!File.Exists(activeModsPath))
            {
                // First run or fresh install: no saved active mods yet, so leave all mods disabled.
                Program.Logger.Debug($"Active mods file not found at '{activeModsPath}'. No mods will be marked active.");
                return;
            }
            var activeMods = File.ReadAllLines(activeModsPath).ToList();
            foreach (Mod mod in ModsPaths)
            {
                string name = mod.GetName();
                foreach (var x in activeMods)
                {
                    if (name == x)
                    {
                        mod.ChangeEnabledState(true);
                        break;
                    }
                }
            }
        }

        // Base (vanilla) game pack files that must never appear in the mod manager list.
        // The two games ship different packs, so the list is selected by game type.
        private static readonly HashSet<string> AttilaBaseGamePacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "belisarius.pack", "boot.pack", "charlemagne.pack", "data.pack",
            "local_en.pack", "local_en_shared_rome2.pack", "models.pack", "models1.pack",
            "models2.pack", "models3.pack", "movies.pack", "music.pack",
            "music_en_shared_rome2.pack", "slavs.pack", "sound.pack", "terrain.pack",
            "terrain2.pack", "tiles.pack", "tiles2.pack", "tiles3.pack",
            "tiles4.pack", "blood.pack"
        };

        // Rome 2 base packs share the Warscape naming conventions (data_rome2.pack, etc.).
        // ponytail: contains the well-known base/DLC packs; any missed pack still just shows
        // up in the optional list, it is never auto-enabled.
        private static readonly HashSet<string> Rome2BaseGamePacks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "blood_rome2.pack", "boot.pack", "data.pack", "data_rome2.pack", "divided.pack", "gaul.pack", "greeks.pack",
            "invasion.pack", "language.txt", "local_en.pack", "local_en_shared_rome2.pack", "manifest.txt",
            "models.pack", "models_rome2.pack", "models2.pack", "models2_rome2.pack", "models3_rome2.pack",
            "movies.pack", "movies_rome2.pack", "music.pack", "music_en_shared_rome2.pack", "music_rome2.pack",
            "punic.pack", "sound.pack", "sound_rome2.pack", "terrain.pack", "terrain_rome2.pack", "terrain2.pack",
            "terrain2_rome2.pack", "terrain3_rome2.pack", "tiles.pack", "tiles_rome2.pack", "tiles2.pack",
            "tiles2_rome2.pack", "tiles3.pack", "tiles3_rome2.pack", "tiles4.pack", "tiles4_rome2.pack"
        };

        private static bool IsBaseGamePack(string fileName, string gameType)
        {
            return gameType == "rome_2"
                ? Rome2BaseGamePacks.Contains(fileName)
                : AttilaBaseGamePacks.Contains(fileName);
        }

        public static string CalculateSHA256(string filePath)
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

        [SupportedOSPlatform("windows")]
        /// <summary>Display label for a game type, matching the progress-reporter prefixes
        /// used by UC_UnitMapper.VerifyAllErasBothGamesAsync.</summary>
        public static string GetGameLabel(string gameType)
        {
            return gameType == "rome_2" ? "[TW: Rome 2]" : "[TW: Attila]";
        }

        public static VerificationResult VerifyModFiles(List<(string FileName, string Sha256, string? ScreenName, string? Url)> modsToVerifyList, IProgress<string>? progress, string gameType = "attila")
        {
            Program.Logger.Debug("Verifying mod files...");
            var result = new VerificationResult();

            // Create a dictionary to track verification state for each mod
            var modStates = modsToVerifyList
                .GroupBy(item => item.FileName)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Info = g.First(),
                        Found = false,
                        Matched = false
                    }
                );

            Program.Logger.Debug($"Mods to verify: {string.Join(", ", modStates.Keys)}");

            // Verify data folder (priority location)
            var gameService = games.GameServiceFactory.GetGameService(gameType);
            string data_folder_path = gameService.GetModDataPath();
            Program.Logger.Debug($"Checking {gameType} data folder: {data_folder_path}");
            if (Directory.Exists(data_folder_path))
            {
                var dataModsPaths = Directory.GetFiles(data_folder_path);
                foreach (var file in dataModsPaths)
                {
                    var fileName = Path.GetFileName(file);
                    if (modStates.ContainsKey(fileName) && Path.GetExtension(fileName) == ".pack")
                    {
                        var modState = modStates[fileName];
                        var screenName = modState.Info.ScreenName;

                        string progressMessage = string.IsNullOrEmpty(screenName)
                            ? $"Verifying: {fileName}"
                            : $"Verifying: {screenName} - {fileName}";
                        progress?.Report(progressMessage);

                        // Mark as found
                        modStates[fileName] = new { Info = modState.Info, Found = true, Matched = modState.Matched };

                        if (!string.IsNullOrEmpty(modState.Info.Sha256))
                        {
                            string actualSha = CalculateSHA256(file);
                            if (string.Equals(modState.Info.Sha256, actualSha, StringComparison.OrdinalIgnoreCase))
                            {
                                Program.Logger.Debug($"Found required mod in data folder with matching hash: {fileName}");
                                // Mark as matched and stop searching for this mod
                                modStates[fileName] = new { Info = modState.Info, Found = true, Matched = true };
                            }
                            else
                            {
                                Program.Logger.Debug($"Found required mod '{fileName}' in data folder but hash mismatched. Expected: {modState.Info.Sha256}, Actual: {actualSha}");
                                // Since data folder has priority, this is a definitive mismatch
                                result.MismatchedFiles.Add((fileName, modState.Info.Sha256, screenName, modState.Info.Url));
                                // Mark as matched to stop searching (because data folder takes precedence)
                                modStates[fileName] = new { Info = modState.Info, Found = true, Matched = true };
                            }
                        }
                        else // No hash provided, just check for existence
                        {
                            Program.Logger.Debug($"Found required mod in data folder (no hash check): {fileName}");
                            // Mark as matched and stop searching
                            modStates[fileName] = new { Info = modState.Info, Found = true, Matched = true };
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show($"Error reading {gameType} data folder. This is caused by wrong {gameType} path.", "Crusader Conflicts: Game Paths Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }

            // Verify workshop folder (only for mods not yet matched)
            string? workshop_folder_path = GetWorkshopFolderPath(gameType);
            Program.Logger.Debug($"Checking {gameType} workshop folder: {workshop_folder_path}");
            if (Directory.Exists(workshop_folder_path))
            {
                var steamModsFoldersPaths = Directory.GetDirectories(workshop_folder_path);
                foreach (var folder in steamModsFoldersPaths)
                {
                    var files = Directory.GetFiles(folder);
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        if (modStates.ContainsKey(fileName) && Path.GetExtension(fileName) == ".pack")
                        {
                            var modState = modStates[fileName];

                            // Skip if already matched (found in data folder or already matched in workshop)
                            if (modState.Matched) continue;

                            var screenName = modState.Info.ScreenName;

                            string progressMessage = string.IsNullOrEmpty(screenName)
                                ? $"Verifying: {fileName}"
                                : $"Verifying: {screenName} - {fileName}";
                            progress?.Report(progressMessage);

                            // Mark as found
                            modStates[fileName] = new { Info = modState.Info, Found = true, Matched = modState.Matched };

                            if (!string.IsNullOrEmpty(modState.Info.Sha256))
                            {
                                string actualSha = CalculateSHA256(file);
                                if (string.Equals(modState.Info.Sha256, actualSha, StringComparison.OrdinalIgnoreCase))
                                {
                                    Program.Logger.Debug($"Found required mod in workshop folder with matching hash: {fileName}");
                                    // Mark as matched
                                    modStates[fileName] = new { Info = modState.Info, Found = true, Matched = true };
                                }
                                else
                                {
                                    Program.Logger.Debug($"Found required mod '{fileName}' in workshop folder but hash mismatched. Expected: {modState.Info.Sha256}, Actual: {actualSha}");
                                    // Store mismatch info but keep searching other workshop folders
                                    // We'll add it to MismatchedFiles later only if no correct version is found
                                }
                            }
                            else // No hash provided, just check for existence
                            {
                                Program.Logger.Debug($"Found required mod in workshop folder (no hash check): {fileName}");
                                // Mark as matched
                                modStates[fileName] = new { Info = modState.Info, Found = true, Matched = true };
                            }
                        }
                    }
                }
            }

            // Process final states
            foreach (var kvp in modStates)
            {
                string fileName = kvp.Key;
                var modState = kvp.Value;
                var info = modState.Info;

                if (!modState.Found)
                {
                    // Mod not found in any location
                    result.MissingFiles.Add((fileName, info.ScreenName, info.Url, gameType));
                }
                else if (!modState.Matched)
                {
                    // Mod found but no version matched - this means all found versions had hash mismatches
                    // Only report mismatch if we actually found the file (this prevents double reporting)
                    if (!string.IsNullOrEmpty(info.Sha256))
                    {
                        result.MismatchedFiles.Add((fileName, info.Sha256, info.ScreenName, info.Url));
                    }
                }
            }

            if (result.MissingFiles.Any()) Program.Logger.Debug($"Mods not found: {string.Join(", ", result.MissingFiles.Select(f => f.FileName))}");
            if (result.MismatchedFiles.Any()) Program.Logger.Debug($"Mismatched mods: {string.Join(", ", result.MismatchedFiles.Select(m => m.FileName))}");
            if (!result.MissingFiles.Any() && !result.MismatchedFiles.Any()) Program.Logger.Debug("All required mods were found and hashes match.");
            return result;
        }
    }
}
