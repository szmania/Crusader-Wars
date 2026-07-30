using System;
using System.IO;
using System.Xml;
using Xunit;

namespace CrusaderWars.tests.XmlValidation
{
    public class XmlSavingIntegrationTests
    {
        private readonly string _schemaDir;
        private readonly string _tempDir;

        public XmlSavingIntegrationTests()
        {
            _schemaDir = XmlTestHelper.GetSchemaDirectory();
            _tempDir = XmlTestHelper.GetTempSettingsDirectory();
        }

        [Fact]
        public void SaveValuesToOptionsFile_SaveValidData_FileIsSavedAndValid()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            // Act - simulate saving by modifying a value
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var leviesMaxNode = doc.SelectSingleNode("//Option[@name='LeviesMax']");
            Assert.NotNull(leviesMaxNode);
            leviesMaxNode!.InnerText = "15";
            doc.Save(filePath);

            // Assert - post-save validation
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);

            // Verify the saved value
            XmlDocument loadedDoc = new XmlDocument();
            loadedDoc.Load(filePath);
            var savedNode = loadedDoc.SelectSingleNode("//Option[@name='LeviesMax']");
            Assert.NotNull(savedNode);
            Assert.Equal("15", savedNode!.InnerText);
        }

        [Fact]
        public void SaveValuesToOptionsFile_PostSaveValidation_NoErrors()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            // Act - verify the file passes validation after save
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void SaveValuesToOptionsFile_SaveWithMissingFile_GracefulHandling()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            if (File.Exists(filePath)) File.Delete(filePath);

            // Act & Assert - loading a non-existent file should throw, not crash silently
            Assert.False(File.Exists(filePath));
            Assert.Throws<FileNotFoundException>(() =>
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath); // This simulates what SaveValuesToOptionsFile does
            });
        }

        [Fact]
        public void SaveActiveSubmods_SaveValidData_FileIsSavedAndValid()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "ActiveSubmods.xml");
            XmlTestHelper.CreateValidActiveSubmodsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "ActiveSubmods.xsd");

            // Act - simulate saving by adding a new playthrough
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var root = doc.DocumentElement!;

            XmlElement playthrough = doc.CreateElement("Playthrough");
            playthrough.SetAttribute("tag", "TheFallenEagle");
            XmlElement submod = doc.CreateElement("Submod");
            submod.InnerText = "TestSubmod2";
            playthrough.AppendChild(submod);
            root.AppendChild(playthrough);
            doc.Save(filePath);

            // Assert - post-save validation
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);

            // Verify structure
            XmlDocument loadedDoc = new XmlDocument();
            loadedDoc.Load(filePath);
            var playthroughNodes = loadedDoc.SelectNodes("//Playthrough");
            Assert.NotNull(playthroughNodes);
            Assert.Equal(2, playthroughNodes!.Count);
        }

        [Fact]
        public void SaveActiveSubmods_SaveWithMissingFile_CanCreateDefault()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "ActiveSubmods.xml");
            if (File.Exists(filePath)) File.Delete(filePath);

            // Act - simulate SubmodManager.SaveActiveSubmods() creating a new file
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDecl = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(xmlDecl);
            XmlElement root = doc.CreateElement("ActiveSubmods");
            doc.AppendChild(root);
            doc.Save(filePath);

            // Assert
            Assert.True(File.Exists(filePath));
            string xsdPath = Path.Combine(_schemaDir, "ActiveSubmods.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);
        }

        [Fact]
        public void ChangePathSettings_SaveValidPath_FileIsUpdatedAndValid()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Paths.xml");
            XmlTestHelper.CreateValidPathsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");

            // Act - simulate ChangePathSettings updating the CK3 path
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var ck3Node = doc.SelectSingleNode("//CrusaderKings");
            Assert.NotNull(ck3Node);
            ((XmlElement)ck3Node!).SetAttribute("path", "C:\\NewPath\\ck3.exe");
            doc.Save(filePath);

            // Assert
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);

            XmlDocument loadedDoc = new XmlDocument();
            loadedDoc.Load(filePath);
            var savedNode = loadedDoc.SelectSingleNode("//CrusaderKings");
            Assert.NotNull(savedNode);
            Assert.Equal("C:\\NewPath\\ck3.exe", savedNode!.Attributes!["path"]!.Value);
        }

        [Fact]
        public void ChangePathSettings_SaveWithMissingFile_CreatesDefaultAndUpdates()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Paths.xml");
            if (File.Exists(filePath)) File.Delete(filePath);

            // Act - simulate ReadGamePaths creating default and then setting path
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Paths");
            doc.AppendChild(root);

            XmlElement attila = doc.CreateElement("TotalWarAttila");
            attila.SetAttribute("path", "");
            root.AppendChild(attila);

            XmlElement ck3 = doc.CreateElement("CrusaderKings");
            ck3.SetAttribute("path", "C:\\Games\\CK3\\ck3.exe");
            root.AppendChild(ck3);

            doc.Save(filePath);

            // Assert
            Assert.True(File.Exists(filePath));
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);
        }

        [Fact]
        public void WriteUnitMappersOptions_SaveValidData_FileIsSavedAndValid()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");
            XmlTestHelper.CreateValidUnitMappersXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");

            // Act - simulate WriteUnitMappersOptions toggling DefaultCK3 to True
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var defaultCK3Node = doc.SelectSingleNode("//UnitMappers[@name='DefaultCK3']");
            Assert.NotNull(defaultCK3Node);
            defaultCK3Node!.InnerText = "True";
            doc.Save(filePath);

            // Assert - post-save validation
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);

            // Verify the saved value
            XmlDocument loadedDoc = new XmlDocument();
            loadedDoc.Load(filePath);
            var savedNode = loadedDoc.SelectSingleNode("//UnitMappers[@name='DefaultCK3']");
            Assert.NotNull(savedNode);
            Assert.Equal("True", savedNode!.InnerText);
        }

        [Fact]
        public void WriteUnitMappersOptions_SaveWithMissingFile_CreatesDefault()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");
            if (File.Exists(filePath)) File.Delete(filePath);

            // Act - simulate ReadUnitMappersOptions creating the default file
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDecl = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(xmlDecl);

            XmlElement root = doc.CreateElement("UMOptions");
            doc.AppendChild(root);

            void CreateMapper(string name)
            {
                XmlElement mapper = doc.CreateElement("UnitMappers");
                mapper.SetAttribute("name", name);
                mapper.InnerText = "False";
                root.AppendChild(mapper);
            }

            CreateMapper("DefaultCK3");
            CreateMapper("TheFallenEagle");
            CreateMapper("RealmsInExile");
            CreateMapper("AGOT");
            doc.Save(filePath);

            // Assert
            Assert.True(File.Exists(filePath));
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);
        }
    }
}
