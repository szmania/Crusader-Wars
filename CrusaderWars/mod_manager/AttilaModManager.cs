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
        Bitmap Image { get; set; }
        string Name { get; set; }
        ModLocalization Localization { get; set; }
        string FullPath {  get; set; }
        bool RequiredMod {  get; set; }
        bool LoadingMod {  get; set; }
        int LoadOrder { get; set; }

        public Mod(bool isEnabled, Bitmap pngImg, string name, ModLocalization local, string fullPath)
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
        public Bitmap GetThumbnail() { return Image; }
        public string GetName() { return Name; }
        public ModLocalization GetLocalization() { return Localization; }
        public string GetFullPath() { return FullPath; }

        public void DisposeThumbnail() {  Image.Dispose();Image = null; }
    }

    public static class AttilaModManager
    {
        static DataGridView ModManagerControl { get; set; }
        static List<Mod> ModsPaths { get; set; }
        
        public static void SetControlReference(DataGridView dataGrid)
        {
            ModManagerControl = dataGrid;
        }

        static void RemoveRequiredMods()
        {
            string[] unitMappers_folders = Directory.GetDirectories(@".\unit mappers\");
            ModsPaths.RemoveAll(x => x.GetName() == "CrusaderWars.pack");

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

        public static string GetWorkshopFolderPath()
        {
            string attilaPath = Properties.Settings.Default.VAR_attila_path;
            if (string.IsNullOrEmpty(attilaPath) || !System.IO.File.Exists(attilaPath))
            {
                return null; 
            }

            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(attilaPath));

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

            /*
             *  ....................  
             *      OPTIONAL MODS  
             *  ....................
             */

            string userMods_path = Properties.Settings.Default.VAR_attila_path.Replace("Attila.exe", "used_mods_cw.txt");
            string[] workingDirectories = null;
            string[] steamModNames = null;
            string[] dataModNames = null;

            //Working Directories
            var steamMods = ModsPaths.Where(x => x.GetLocalization() == ModLocalization.Steam && x.IsEnabled()).ToList();
            if(steamMods != null || steamMods.Count != 0)
            {
                workingDirectories = steamMods.Select(x => x.GetFullPath()).ToArray();
                steamModNames = steamMods.Select(x => x.GetName()).ToArray();
            }

            //Data Mods
            var dataMods = ModsPaths.Where(x => x.GetLocalization() == ModLocalization.Data && x.IsEnabled()).ToList();
            if(dataMods != null || dataMods.Count != 0)
            {
                dataModNames = dataMods.Select(x => x.GetName()).ToArray();
            }

            /*
             *  ....................  
             *      REQUIRED MODS  
             *  ....................
             */
            string[] workingDirectoriesRequiredMods = null;
            string[] steamModNamesRequiredMods = null;
            string[] dataModNamesRequiredMods = null;
            //Working Directories
            
            var steamModsRequiredMods = ModsPaths.Where(x => x.GetLocalization() == ModLocalization.Steam && x.IsRequiredMod() && x.IsLoadingModRequiredMod())
                                       .ToList()
                                       .OrderBy(x => x.GetLoadOrderValue());
            if (steamModsRequiredMods != null)
            {
                workingDirectoriesRequiredMods = steamModsRequiredMods.Select(x => x.GetFullPath()).ToArray();
                steamModNamesRequiredMods = steamModsRequiredMods.Select(x => x.GetName()).ToArray();
            }

            //Data Mods
            var dataModsRequiredMods = ModsPaths.Where(x => x.GetLocalization() == ModLocalization.Data && x.IsRequiredMod() && x.IsLoadingModRequiredMod())
                                      .ToList()
                                      .OrderBy(x => x.GetLoadOrderValue());
            if (dataModsRequiredMods != null)
            {
                dataModNamesRequiredMods = dataModsRequiredMods.Select(x => x.GetName()).ToArray();
            }

            if (!File.Exists(userMods_path)) { 
                File.Create(userMods_path).Close(); 
            }
            else
            {
                File.WriteAllText(userMods_path, "");
            }

            using (FileStream modsFile = File.Open(userMods_path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter sw = new StreamWriter(modsFile))
            {
                sw.NewLine = "\n";
                Program.Logger.Debug("Writing mods to used_mods_cw.txt...");
                foreach (string wD  in workingDirectories)
                {
                    string t = wD.Replace(@"\", @"/");
                    sw.WriteLine($"add_working_directory \"{t}\";");
                    Program.Logger.Debug($"  - WD: {t}");
                }

                foreach (string wD in workingDirectoriesRequiredMods)
                {
                    string t = wD.Replace(@"\", @"/");
                    sw.WriteLine($"add_working_directory \"{t}\";");
                    Program.Logger.Debug($"  - Required WD: {t}");
                }

                sw.WriteLine($"mod \"CrusaderWars.pack\";");
                Program.Logger.Debug("  - Mod: CrusaderWars.pack");

                foreach (string mod in dataModNames)
                {
                    sw.WriteLine($"mod \"{mod}\";");
                    Program.Logger.Debug($"  - Data Mod: {mod}");
                }
                foreach (string mod in steamModNames)
                {
                    sw.WriteLine($"mod \"{mod}\";");
                    Program.Logger.Debug($"  - Steam Mod: {mod}");
                }

                foreach (string mod in dataModNamesRequiredMods)
                {
                    sw.WriteLine($"mod \"{mod}\";");
                    Program.Logger.Debug($"  - Required Data Mod: {mod}");
                }
                foreach (string mod in steamModNamesRequiredMods)
                {
                    sw.WriteLine($"mod \"{mod}\";");
                    Program.Logger.Debug($"  - Required Steam Mod: {mod}");
                }

                sw.Dispose();
                sw.Close();
            };
        }
        public static void ReadInstalledMods()
        {
            Program.Logger.Debug("Reading installed Attila mods...");
            string data_folder_path = Properties.Settings.Default.VAR_attila_path.Replace("Attila.exe", @"data\");
            string workshop_folder_path = GetWorkshopFolderPath();

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
                        ModsPaths.Add(new Mod(false, LoadBitmapWithReducedSize(image_path), name, ModLocalization.Steam, fullPath));
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
            foreach (var mod in ModsPaths)
            {
                if (mod.IsRequiredMod()) continue;

                if (mod.GetLocalization() == ModLocalization.Steam)
                    ModManagerControl.Rows.Add(mod.IsEnabled(), mod.GetThumbnail(), mod.GetName(), steamImg);
                else
                    ModManagerControl.Rows.Add(mod.IsEnabled(), mod.GetThumbnail(), mod.GetName(), dataImg);
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
                return null;
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
            string checkboxValue = (String)row.Cells[0].Value;
            bool value;
            if (checkboxValue == "Active")
                value = true;
            else
                value = false;
                
            string name = (String)row.Cells[2].Value;

            ModsPaths.FirstOrDefault(x => x.GetName() == name)?.ChangeEnabledState(value);
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
