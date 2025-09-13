using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

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
        string FullPath {  get; set; }
        bool RequiredMod {  get; set; }
        bool LoadingMod {  get; set; }
        int LoadOrder { get; set; }

        public Mod(bool isEnabled, Bitmap? pngImg, string name, ModLocalization local, string fullPath) // Changed to nullable Bitmap
        {
            Enabled = isEnabled;
            Image = pngImg;
            Name = name;
            Localization = local;
            FullPath = fullPath;
        }
        public void ChangeEnabledState(bool yn) {  Enabled = yn; }
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

        public void DisposeThumbnail() { Image?.Dispose(); Image = null; } // Used null-conditional operator
    }

    public static class AttilaModManager
    {
        static DataGridView? ModManagerControl { get; set; }
        static List<Mod> ModsPaths { get; set; } = new List<Mod>();
        
        public static void SetControlReference(DataGridView dataGrid)
        {
            ModManagerControl = dataGrid;
        }

        static void RemoveRequiredMods()
        {
            string[] unitMappers_folders = Directory.GetDirectories(@".\unit mappers\");
            ModsPaths.RemoveAll(x => x.GetName() == "CrusaderConflicts.pack");

            foreach (var mapper in unitMappers_folders)
            {
                string[] files  = Directory.GetFiles(mapper);
                foreach(var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if(fileName == "Mods.xml")
                    {
                        XmlDocument ModsFile = new XmlDocument();
                        ModsFile.Load(file);
                        foreach(XmlNode modNode in ModsFile.DocumentElement.ChildNodes)
                        {
                            ModsPaths.FirstOrDefault(x => x.GetName() == modNode.InnerText)?.IsRequiredMod(true);
                            //ModsPaths.RemoveAll(x => x.GetName() == modNode.InnerText);
                        }                        
                    }
                }
            }
        }

        public static void SetLoadingRequiredMods(List<string> requiredMods)
        {
            // Reset all mods' loading required status before setting new ones
            foreach (var mod in ModsPaths)
            {
                mod.IsLoadingRequiredMod(false);
            }

            foreach(var mod in ModsPaths)
            {
                if(mod.IsRequiredMod())
                {
                    foreach(var requiredMod in requiredMods)
                    {
                        if(mod.GetName() == requiredMod)
                        {
                            mod.IsLoadingRequiredMod(true);
                            mod.SetLoadOrderValue(requiredMods.IndexOf(requiredMod));
                            break;
                        }
                    }
                }
            }
        }

        public static string? GetWorkshopFolderPath()
        {
            string attilaPath = Properties.Settings.Default.VAR_attila_path;
            if (string.IsNullOrEmpty(attilaPath) || !System.IO.File.Exists(attilaPath))
            {
                return null; 
            }

            DirectoryInfo? dirInfo = new DirectoryInfo(Path.GetDirectoryName(attilaPath)!);

            // Search upwards for the "steamapps" folder
            while (dirInfo != null && dirInfo.Name.ToLower() != "steamapps")
            {
                dirInfo = dirInfo.Parent;
            }

            // If "steamapps" was found, construct the workshop path
            if (dirInfo != null)
            {
                return Path.Combine(dirInfo.FullName, "workshop", "content", "325610");
            }

            return null; // "steamapps" not found
        }

        public static void CreateUserModsFile()
        {
            Program.Logger.Debug("Creating user mods file for Attila...");
            // CREATE ESSENTIAL FILE TO OPEN ATTILA AUTOMATICALLY
            string steam_app_id_path = Properties.Settings.Default.VAR_attila_path.Replace("Attila.exe", "steam_appid.txt");
            if (!File.Exists(steam_app_id_path))
            {
                File.WriteAllText(steam_app_id_path, "325610");
            }

            string userMods_path = Properties.Settings.Default.VAR_attila_path.Replace("Attila.exe", "used_mods_cc.txt");

            // Clear existing content
            File.WriteAllText(userMods_path, "");

            // Get ordered lists of mods
            var requiredMods = ModsPaths.Where(x => x.IsLoadingModRequiredMod())
                                       .OrderBy(x => x.GetLoadOrderValue())
                                       .ToList();

            // Optional mods are those enabled by the user AND NOT required by the current playthrough
            var optionalMods = ModsPaths.Where(x => x.IsEnabled() && !x.IsLoadingModRequiredMod())
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
                        sw.WriteLine($"add_working_directory \"{workingDirectory}\";");
                        Program.Logger.Debug($"  - {type} WD: {workingDirectory}");
                    }
                    sw.WriteLine($"mod \"{mod.GetName()}\";");
                    Program.Logger.Debug($"  - {type} Mod: {mod.GetName()}");
                };

                // 1. CrusaderConflicts.pack (Highest priority, written first in file, loaded last by game)
                sw.WriteLine($"mod \"CrusaderConflicts.pack\";");
                Program.Logger.Debug("  - Mod: CrusaderConflicts.pack (Highest Priority)");

                // 2. Optional (user-selected) mods
                foreach (var mod in optionalMods)
                {
                    writeModEntry(mod, "Optional");
                }

                // 3. Required mods for the playthrough (Lowest priority, written last in file, loaded first by game)
                foreach (var mod in requiredMods)
                {
                    writeModEntry(mod, "Required");
                }

                sw.Dispose();
                sw.Close();
            }
        }
        public static void ReadInstalledMods()
        {
            Program.Logger.Debug("Reading installed Attila mods...");
            string data_folder_path = Properties.Settings.Default.VAR_attila_path.Replace("Attila.exe", @"data\");
            string? workshop_folder_path = GetWorkshopFolderPath();

            ModsPaths = new List<Mod>();
            //Read data folder
            var dataModsPaths = Directory.GetFiles(data_folder_path);
            foreach(var file in dataModsPaths)
            {
                var fileName = Path.GetFileName(file);
                if(Path.GetExtension(fileName) == ".pack")
                {
                    // Skip Attila Packs
                    if(fileName == "belisarius.pack" ||
                       fileName == "boot.pack" ||
                       fileName == "charlemagne.pack"||
                       fileName == "data.pack" ||
                       fileName == "local_en.pack" ||
                       fileName == "local_en_shared_rome2.pack" ||
                       fileName == "models.pack" ||
                       fileName == "models1.pack" ||
                       fileName == "models2.pack" ||
                       fileName == "models3.pack" ||
                       fileName == "movies.pack" ||
                       fileName == "music.pack" ||
                       fileName == "music_en_shared_rome2.pack" ||
                       fileName == "slavs.pack" ||
                       fileName == "sound.pack" ||
                       fileName == "terrain.pack" ||
                       fileName == "terrain2.pack" ||
                       fileName == "tiles.pack" ||
                       fileName == "tiles2.pack" ||
                       fileName == "tiles3.pack" ||
                       fileName == "tiles4.pack" ||
                       fileName == "blood.pack")
                    {
                        continue;
                    }
                    else
                    {
                        ModsPaths.Add(new Mod(false, LoadBitmapWithReducedSize(@".\data\mod manager\noimage.png"), fileName, ModLocalization.Data, file));
                    }
                }
            }

            // Read steam workshop folder
            if(Directory.Exists(workshop_folder_path))
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
                    if(name != string.Empty)
                    {
                        // Line 308 - Add null check
                        Bitmap? thumbnail = LoadBitmapWithReducedSize(image_path);
                        if (thumbnail != null)
                        {
                            ModsPaths.Add(new Mod(false, thumbnail, name, ModLocalization.Steam, fullPath));
                        }
                    }
                }
            }

            //  REMOVE REQUIRED MODS
            //  to only show optional mods
            RemoveRequiredMods();


            //SET ACTIVE MODS
            SetActiveMods();
            Program.Logger.Debug($"Found {ModsPaths.Count} installed mods.");
        }

        public static void ReadInstalledModsAndPopulateModManager()
        {
            ReadInstalledMods();

            //  SET AT MOD MANAGER
            Bitmap steamImg = LoadBitmapWithReducedSize(@".\data\mod manager\steamlogo.png");
            Bitmap dataImg = LoadBitmapWithReducedSize(@".\data\mod manager\folder.png");
            if (ModManagerControl != null)
            {
                foreach (var mod in ModsPaths)
                {
                    // Line 82 - Add null check
                    if (mod != null && !mod.IsRequiredMod())
                    {
                        object[] rowData = new object[] { 
                            mod.IsEnabled(), 
                            mod.GetThumbnail(), 
                            mod.GetName(), 
                            mod.GetLocalization() == ModLocalization.Steam ? steamImg : dataImg 
                        };

                        // Check if all items in rowData are not null before adding
                        if (rowData.All(item => item != null))
                        {
                            ModManagerControl.Rows.Add(rowData);
                        }
                    }
                }
            }
        }

        static Bitmap LoadBitmapWithReducedSize(string path)
        {
            try
            {
                using (var originalImage = new Bitmap(path))
                {
                    // Create a smaller version of the image (thumbnail)
                    int thumbnailWidth = originalImage.Width / 2; // Adjust as needed
                    int thumbnailHeight = originalImage.Height / 2; // Adjust as needed
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
                Program.Logger.Debug($"Error loading image: {ex.Message}");
                return null!; // Return null! to indicate it might be null, but the caller should handle it.
            }
        }


        static void DisposeImages()
        {
            foreach(var mod in ModsPaths)
            {
                mod.DisposeThumbnail();
            }
        }
        public static void SaveActiveMods()
        {
            var activeMods = ModsPaths.Where(mod => mod.IsEnabled()).Select(x => x.GetName()).ToArray();
            File.WriteAllLines(@".\data\mod manager\active_mods.txt", activeMods);
            DisposeImages();
        }

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

        static void SetActiveMods()
        {
            var activeMods = File.ReadAllLines(@".\data\mod manager\active_mods.txt").ToList();
            foreach(Mod mod in ModsPaths) { 
                string name = mod.GetName();
                foreach(var x in activeMods) { 
                    if(name == x)
                    {
                        mod.ChangeEnabledState(true);
                        break;
                    }
                }
            }
        }
    }
}
