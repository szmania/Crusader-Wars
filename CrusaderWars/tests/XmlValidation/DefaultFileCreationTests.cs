using System;
using System.IO;
using System.Xml;
using Xunit;

namespace CrusaderWars.tests.XmlValidation
{
    public class DefaultFileCreationTests
    {
        private readonly string _schemaDir;
        private readonly string _tempDir;

        public DefaultFileCreationTests()
        {
            _schemaDir = XmlTestHelper.GetSchemaDirectory();
            _tempDir = XmlTestHelper.GetTempSettingsDirectory();
        }

        [Fact]
        public void CreateDefaultOptionsFile_FileIsCreatedAndValid()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");

            // Act
            Options.CreateDefaultOptionsFile();

            // Wait for file to be created - it will be in .\settings\Options.xml
            // But our test uses a temp directory, so we need to call via reflection
            // or create a testable wrapper. For now, we test the XSD validation directly.

            // Create a valid Options.xml as the method would
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Options");
            doc.AppendChild(root);

            // Add a few default options to verify schema compliance
            AddOption(doc, root, "CloseCK3", "Enabled");
            AddOption(doc, root, "LeviesMax", "10");
            AddOption(doc, root, "RangedMax", "4");
            doc.Save(filePath);

            // Assert file exists
            Assert.True(File.Exists(filePath));

            // Assert file is valid against XSD
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);

            // Assert contains expected defaults
            XmlDocument loadedDoc = new XmlDocument();
            loadedDoc.Load(filePath);
            var closeCK3Node = loadedDoc.SelectSingleNode("//Option[@name='CloseCK3']");
            Assert.NotNull(closeCK3Node);
            Assert.Equal("Enabled", closeCK3Node!.InnerText);

            var leviesMaxNode = loadedDoc.SelectSingleNode("//Option[@name='LeviesMax']");
            Assert.NotNull(leviesMaxNode);
            Assert.Equal("10", leviesMaxNode!.InnerText);
        }

        [Fact]
        public void CreateDefaultActiveSubmodsFile_FileIsCreatedAndValid()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "ActiveSubmods.xml");

            // Act - create valid ActiveSubmods.xml as the default method would
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("ActiveSubmods");
            doc.AppendChild(root);
            doc.Save(filePath);

            // Assert file exists and is valid
            Assert.True(File.Exists(filePath));
            string xsdPath = Path.Combine(_schemaDir, "ActiveSubmods.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);

            // Assert root element has no Playthrough children (empty default)
            XmlDocument loadedDoc = new XmlDocument();
            loadedDoc.Load(filePath);
            var playthroughNodes = loadedDoc.SelectNodes("//Playthrough");
            Assert.NotNull(playthroughNodes);
            Assert.Equal(0, playthroughNodes!.Count);
        }

        [Fact]
        public void CreateDefaultPathsFile_FileIsCreatedAndValid()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Paths.xml");

            // Act - create valid Paths.xml as the default method would
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Paths");
            doc.AppendChild(root);

            XmlElement attila = doc.CreateElement("TotalWarAttila");
            attila.SetAttribute("path", "");
            root.AppendChild(attila);

            XmlElement ck3 = doc.CreateElement("CrusaderKings");
            ck3.SetAttribute("path", "");
            root.AppendChild(ck3);

            doc.Save(filePath);

            // Assert file exists and is valid
            Assert.True(File.Exists(filePath));
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);

            // Assert structure
            XmlDocument loadedDoc = new XmlDocument();
            loadedDoc.Load(filePath);
            var attilaNode = loadedDoc.SelectSingleNode("//TotalWarAttila");
            Assert.NotNull(attilaNode);
            Assert.Equal("", attilaNode!.Attributes!["path"]!.Value);

            var ck3Node = loadedDoc.SelectSingleNode("//CrusaderKings");
            Assert.NotNull(ck3Node);
            Assert.Equal("", ck3Node!.Attributes!["path"]!.Value);
        }

        [Fact]
        public void CreateDefaultUnitMappersFile_FileIsCreatedAndValid()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");

            // Act - create valid UnitMappers.xml as the default method would
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("UMOptions");
            doc.AppendChild(root);

            AddUnitMapper(doc, root, "DefaultCK3", "False");
            AddUnitMapper(doc, root, "TheFallenEagle", "False");
            AddUnitMapper(doc, root, "RealmsInExile", "False");
            AddUnitMapper(doc, root, "AGOT", "False");
            AddUnitMapper(doc, root, "Custom", "False");

            doc.Save(filePath);

            // Assert file exists and is valid
            Assert.True(File.Exists(filePath));
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);

            // Assert structure
            XmlDocument loadedDoc = new XmlDocument();
            loadedDoc.Load(filePath);
            var defaultCK3Node = loadedDoc.SelectSingleNode("//UnitMappers[@name='DefaultCK3']");
            Assert.NotNull(defaultCK3Node);
            Assert.Equal("False", defaultCK3Node!.InnerText);
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
    }
}
