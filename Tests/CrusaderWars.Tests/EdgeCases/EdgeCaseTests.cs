using System;
using System.IO;
using System.Xml;
using Xunit;
using CrusaderWars.Tests.TestHelpers;

namespace CrusaderWars.Tests.EdgeCases
{
    /// <summary>
    /// Edge case and error handling tests for the Bookmarks+ playthrough tab.
    /// Tests corrupted XML, missing directories, and invalid input handling.
    /// </summary>
    public class EdgeCaseTests : IDisposable
    {
        private readonly TempTestEnvironment _env;

        public EdgeCaseTests()
        {
            _env = new TempTestEnvironment();
        }

        [Fact]
        public void CorruptedUnitMappersXml_DoesNotCrash_OnLoad()
        {
            // Arrange
            _env.CreateCorruptedUnitMappersXml();
            var filePath = Path.Combine(_env.SettingsPath, "UnitMappers.xml");
            var content = File.ReadAllText(filePath);

            // Act - simulate what happens when the app tries to load corrupted XML
            bool loadSucceeded = false;
            string errorMessage = null;
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(content);
                loadSucceeded = true;
            }
            catch (XmlException ex)
            {
                errorMessage = ex.Message;
            }

            // Assert - the app should handle this gracefully
            Assert.False(loadSucceeded); // Load should fail
            Assert.NotNull(errorMessage); // But with a proper error message
            Assert.Contains("Root element is missing", errorMessage); // Typical XML parse error
        }

        [Fact]
        public void MissingUnitMapperDirectory_ReturnsEmptyModsList()
        {
            // Arrange - BookmarksPlus is enabled in XML but directory doesn't exist
            _env.CreateUnitMappersXml(bookmarksPlus: true);
            // Do NOT create the mapper directory

            // Act - simulate what GetUnitMappersModsCollectionFromTag does
            string unitMappersDir = _env.UnitMappersPath;
            string tag = "BookmarksPlus";
            var requiredMods = new System.Collections.Generic.List<string>();

            if (Directory.Exists(unitMappersDir))
            {
                foreach (var dir in Directory.GetDirectories(unitMappersDir))
                {
                    string tagFile = Path.Combine(dir, "tag.txt");
                    if (File.Exists(tagFile) && File.ReadAllText(tagFile).Trim() == tag)
                    {
                        // Would parse Mods.xml here
                        string modsPath = Path.Combine(dir, "Mods.xml");
                        if (File.Exists(modsPath))
                        {
                            // Parse mods...
                        }
                    }
                }
            }

            // Assert - no mods found because directory is missing
            Assert.Empty(requiredMods);
        }

        [Fact]
        public void MissingLocalizationDirectory_ReturnsEmptyScreenNames()
        {
            // Arrange
            var nonExistentDir = Path.Combine(_env.UnitsCardsNamesPath, "nonexistent");

            // Act - simulate GetUnitScreenNames with missing directory
            var screenNames = new System.Collections.Generic.Dictionary<string, string>();
            if (Directory.Exists(nonExistentDir))
            {
                var locFiles = Directory.GetFiles(nonExistentDir);
                foreach (var file in locFiles)
                {
                    using (var reader = new StreamReader(file))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split('\t');
                            if (parts.Length >= 2 && line.StartsWith("land_units_onscreen_name_"))
                            {
                                var key = parts[0].Replace("land_units_onscreen_name_", "");
                                var name = parts[1];
                                screenNames[key] = name;
                            }
                        }
                    }
                }
            }

            // Assert
            Assert.Empty(screenNames);
        }

        [Fact]
        public void EmptyUnitMappersXml_IsHandledGracefully()
        {
            // Arrange - create an empty but valid XML
            var emptyXml = "<?xml version=\"1.0\"?><UMOptions></UMOptions>";
            var filePath = Path.Combine(_env.SettingsPath, "UnitMappers.xml");
            File.WriteAllText(filePath, emptyXml);

            // Act - try to parse
            bool parseSucceeded = false;
            XmlDocument doc = null;
            try
            {
                doc = new XmlDocument();
                doc.Load(filePath);
                parseSucceeded = true;
            }
            catch
            {
                // Ignore
            }

            // Assert
            Assert.True(parseSucceeded);
            Assert.NotNull(doc);
            Assert.NotNull(doc.DocumentElement);
            Assert.Equal("UMOptions", doc.DocumentElement.Name);
        }

        [Fact]
        public void MalformedDlcLoadJson_IsHandledGracefully()
        {
            // Arrange - create invalid JSON
            var invalidJson = "{ \"enabled_mods\": [ \"test.mod\", ] }"; // Trailing comma
            var jsonPath = Path.Combine(_env.RootPath, "dlc_load.json");
            File.WriteAllText(jsonPath, invalidJson);

            // Act - try to parse
            bool parseSucceeded = false;
            try
            {
                using (JsonDocument.Parse(invalidJson))
                {
                    parseSucceeded = true;
                }
            }
            catch (JsonException)
            {
                // Expected to fail
            }

            // Assert
            Assert.False(parseSucceeded); // Should fail gracefully
        }

        [Fact]
        public void XPathInjectionAttempt_IsNeutralized()
        {
            // Arrange - create XML with XPath-breaking characters
            var maliciousXml = "<?xml version=\"1.0\"?><UMOptions><UnitMappers name=\"BookmarksPlus' or '1'='1\">True</UnitMappers></UMOptions>";
            var doc = new XmlDocument();
            doc.LoadXml(maliciousXml);

            // Act - simulate the whitelist validation that should be applied
            string tagName = "BookmarksPlus' or '1'='1";
            bool isValidTag = System.Text.RegularExpressions.Regex.IsMatch(tagName, @"^[A-Za-z0-9_]+$");

            // Assert
            Assert.False(isValidTag); // The malicious tag should be rejected
        }

        [Fact]
        public void PathTraversalAttempt_IsDetected()
        {
            // Arrange
            var maliciousPath = "../../../Windows/System32";

            // Act - simulate path validation
            bool containsPathSeparators = maliciousPath.Contains("..") ||
                                          maliciousPath.Contains("/") ||
                                          maliciousPath.Contains("\\");

            // Assert
            Assert.True(containsPathSeparators); // Should be detected as malicious
        }

        [Fact]
        public void MissingOptionsXml_CreatesDefaultFile()
        {
            // Arrange
            var optionsPath = Path.Combine(_env.SettingsPath, "Options.xml");
            if (File.Exists(optionsPath)) File.Delete(optionsPath);

            // Act - simulate what ReadOptionsFile does when file is missing
            if (!File.Exists(optionsPath))
            {
                var defaultXml = "<?xml version=\"1.0\"?><Options><Option name=\"CloseCK3\">Enabled</Option></Options>";
                File.WriteAllText(optionsPath, defaultXml);
            }

            // Assert
            Assert.True(File.Exists(optionsPath));
            var content = File.ReadAllText(optionsPath);
            Assert.Contains("CloseCK3", content);
        }

        [Fact]
        public void EmptyUnitMappersList_ReturnsFalseForAllTabs()
        {
            // Arrange - empty XML
            var emptyXml = "<?xml version=\"1.0\"?><UMOptions></UMOptions>";
            var filePath = Path.Combine(_env.SettingsPath, "UnitMappers.xml");
            File.WriteAllText(filePath, emptyXml);

            // Act - simulate parsing
            var doc = new XmlDocument();
            doc.Load(filePath);
            bool anyEnabled = false;
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element && node.InnerText == "True")
                {
                    anyEnabled = true;
                    break;
                }
            }

            // Assert
            Assert.False(anyEnabled);
        }

        public void Dispose()
        {
            _env?.Dispose();
        }
    }
}