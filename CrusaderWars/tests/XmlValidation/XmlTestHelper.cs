using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace CrusaderWars.tests.XmlValidation
{
    public static class XmlTestHelper
    {
        public static string GetTestDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string testDir = Path.Combine(baseDir, "XmlValidation");
            if (!Directory.Exists(testDir))
            {
                Directory.CreateDirectory(testDir);
            }
            return testDir;
        }

        public static string GetTempSettingsDirectory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "CrusaderWars_Test_Settings", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            return tempDir;
        }

public static string GetSchemaDirectory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "CrusaderWars_Test_Schemas", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDir = currentDir;
            while (solutionDir != null && !Directory.GetFiles(solutionDir, "*.sln").Any())
            {
                solutionDir = Directory.GetParent(solutionDir)?.FullName;
            }

            if (solutionDir == null)
            {
                throw new DirectoryNotFoundException("Could not find the solution directory. The test environment is not set up correctly.");
            }
            
            string sourceDir = Path.Combine(solutionDir, "CrusaderWars", "tests", "XmlValidation", "Schemas");

            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"Schema source directory not found at the expected path: {sourceDir}");
            }
            foreach (string file in Directory.GetFiles(sourceDir, "*.xsd"))
            {
                string dest = Path.Combine(tempDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            return tempDir;
        }

        public static string CopySettingsFilesToTestDirectory(string settingsSourceDir, string testSettingsDir)
        {
            if (!Directory.Exists(settingsSourceDir))
            {
                Directory.CreateDirectory(settingsSourceDir);
            }

            if (!Directory.Exists(testSettingsDir))
            {
                Directory.CreateDirectory(testSettingsDir);
            }

            string[] files = { "Options.xml", "ActiveSubmods.xml", "Paths.xml", "UnitMappers.xml" };
            foreach (string file in files)
            {
                string sourcePath = Path.Combine(settingsSourceDir, file);
                string destPath = Path.Combine(testSettingsDir, file);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, true);
                }
            }

            return testSettingsDir;
        }

        public static void RestoreSettingsFilesFromBackup(string testSettingsDir, string backupDir)
        {
            if (!Directory.Exists(backupDir) || !Directory.Exists(testSettingsDir))
            {
                return;
            }

            string[] files = { "Options.xml", "ActiveSubmods.xml", "Paths.xml", "UnitMappers.xml" };
            foreach (string file in files)
            {
                string backupPath = Path.Combine(backupDir, file);
                string destPath = Path.Combine(testSettingsDir, file);
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, destPath, true);
                }
            }
        }

        public static void CleanupTemporarySettingsDirectory(string tempDir)
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not clean up temp directory {tempDir}: {ex.Message}");
            }
        }

        public static string CreateValidOptionsXml(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Options");
            doc.AppendChild(root);

            AddOption(doc, root, "CloseCK3", "Enabled");
            AddOption(doc, root, "CloseAttila", "Enabled");
            AddOption(doc, root, "LeviesMax", "10");
            AddOption(doc, root, "RangedMax", "4");
            AddOption(doc, root, "InfantryMax", "8");
            AddOption(doc, root, "CavalryMax", "4");

            doc.Save(path);
            return path;
        }

        public static string CreateValidActiveSubmodsXml(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("ActiveSubmods");
            doc.AppendChild(root);

            XmlElement playthrough = doc.CreateElement("Playthrough");
            playthrough.SetAttribute("tag", "DefaultCK3");
            XmlElement submod = doc.CreateElement("Submod");
            submod.InnerText = "TestSubmod";
            playthrough.AppendChild(submod);
            root.AppendChild(playthrough);

            doc.Save(path);
            return path;
        }

        public static string CreateValidPathsXml(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Paths");
            doc.AppendChild(root);

            XmlElement attila = doc.CreateElement("TotalWarAttila");
            attila.SetAttribute("path", "C:\\Games\\Attila\\Attila.exe");
            root.AppendChild(attila);

            XmlElement ck3 = doc.CreateElement("CrusaderKings");
            ck3.SetAttribute("path", "C:\\Games\\CK3\\ck3.exe");
            root.AppendChild(ck3);

            doc.Save(path);
            return path;
        }

        public static string CreateValidUnitMappersXml(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("UMOptions");
            doc.AppendChild(root);

            AddUnitMapper(doc, root, "DefaultCK3", "False");
            AddUnitMapper(doc, root, "TheFallenEagle", "False");
            AddUnitMapper(doc, root, "RealmsInExile", "False");
            AddUnitMapper(doc, root, "AGOT", "False");
            AddUnitMapper(doc, root, "Custom", "False");

            doc.Save(path);
            return path;
        }

        private static void AddOption(XmlDocument doc, XmlElement root, string name, string value)
        {
            XmlElement option = doc.CreateElement("Option");
            option.SetAttribute("name", name);
            option.InnerText = value;
            root.AppendChild(option);
        }

        private static void AddUnitMapper(XmlDocument doc, XmlElement root, string name, string value)
        {
            XmlElement mapper = doc.CreateElement("UnitMappers");
            mapper.SetAttribute("name", name);
            mapper.InnerText = value;
            root.AppendChild(mapper);
        }

        public static List<string> ValidateXml(string xmlPath, string xsdPath)
        {
            return CrusaderWars.mod_manager.XmlValidator.Validate(xmlPath, xsdPath);
        }
    }
}
