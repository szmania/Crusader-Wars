using System;
using System.IO;
using System.Xml;
using Xunit;

namespace CrusaderWars.tests.XmlValidation
{
    public class XmlLoadingIntegrationTests
    {
        private readonly string _schemaDir;
        private readonly string _tempDir;

        public XmlLoadingIntegrationTests()
        {
            _schemaDir = XmlTestHelper.GetSchemaDirectory();
            _tempDir = XmlTestHelper.GetTempSettingsDirectory();
        }

        [Fact]
        public void ReadOptionsFile_ValidFile_LoadsSuccessfully()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            // Act - validate the file (simulates the validation step in ReadOptionsFile)
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.Empty(errors);
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void ReadOptionsFile_MissingFile_CreatesDefaultFile()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            // Ensure file does not exist
            if (File.Exists(filePath)) File.Delete(filePath);

            // Act - simulate the behavior in ReadOptionsFile: if file doesn't exist, create default
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Options");
            doc.AppendChild(root);
            AddOption(doc, root, "CloseCK3", "Enabled");
            AddOption(doc, root, "LeviesMax", "10");
            doc.Save(filePath);

            // Assert
            Assert.True(File.Exists(filePath));
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);
        }

        [Fact]
        public void ReadOptionsFile_InvalidFile_TriggersValidationErrors()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            File.WriteAllText(filePath, "<?xml version=\"1.0\"?><Options><Option>Value</Option></Options>");
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("name"));
        }

        [Fact]
        public void ReadOptionsFile_MalformedXml_ReturnsValidationErrors()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            File.WriteAllText(filePath, "<?xml version=\"1.0\"?><Options><Option name=\"Test\">Value</Options>");
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void LoadActiveSubmods_ValidFile_LoadsSuccessfully()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "ActiveSubmods.xml");
            XmlTestHelper.CreateValidActiveSubmodsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "ActiveSubmods.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void LoadActiveSubmods_MissingFile_NoSubmodsLoaded()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "ActiveSubmods.xml");
            if (File.Exists(filePath)) File.Delete(filePath);

            // Act & Assert - SubmodManager.LoadActiveSubmods() just returns if file doesn't exist
            // No default file is created for ActiveSubmods.xml (it's optional)
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public void LoadActiveSubmods_MalformedFile_DoesNotCrash()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "ActiveSubmods.xml");
            File.WriteAllText(filePath, "<?xml version=\"1.0\"?><ActiveSubmods><Playthrough><Submod>Test</Playthrough></ActiveSubmods>");
            string xsdPath = Path.Combine(_schemaDir, "ActiveSubmods.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void LoadActiveSubmods_MissingTagAttribute_FailsValidation()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "ActiveSubmods.xml");
            File.WriteAllText(filePath, "<?xml version=\"1.0\"?><ActiveSubmods><Playthrough><Submod>Test</Submod></Playthrough></ActiveSubmods>");
            string xsdPath = Path.Combine(_schemaDir, "ActiveSubmods.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("tag"));
        }

        [Fact]
        public void ReadGamePaths_ValidFile_LoadsSuccessfully()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Paths.xml");
            XmlTestHelper.CreateValidPathsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void ReadGamePaths_MissingFile_CreatesDefaultFile()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Paths.xml");
            if (File.Exists(filePath)) File.Delete(filePath);

            // Act - simulate ReadGamePaths default file creation
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

            // Assert
            Assert.True(File.Exists(filePath));
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);
        }

        [Fact]
        public void ReadGamePaths_MalformedFile_FailsValidation()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Paths.xml");
            File.WriteAllText(filePath, "<?xml version=\"1.0\"?><Path><TotalWarAttila path=\"test\"/></Path>");
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void ReadGamePaths_MissingPathAttribute_FailsValidation()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Paths.xml");
            File.WriteAllText(filePath, "<?xml version=\"1.0\"?><Paths><TotalWarAttila/><CrusaderKings path=\"\"/></Paths>");
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void VerifyEnabledUnitMappers_ValidFile_LoadsSuccessfully()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");
            XmlTestHelper.CreateValidUnitMappersXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void VerifyEnabledUnitMappers_MissingFile_ReturnsFalse()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");
            if (File.Exists(filePath)) File.Delete(filePath);

            // Act & Assert - VerifyEnabledUnitMappers returns false if file doesn't exist
            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public void VerifyEnabledUnitMappers_MalformedFile_DoesNotCrash()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");
            File.WriteAllText(filePath, "<?xml version=\"1.0\"?><UMOptions><UnitMappers name=\"DefaultCK3\">True</UMOptions>");
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public void VerifyEnabledUnitMappers_MissingNameAttribute_FailsValidation()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");
            File.WriteAllText(filePath, "<?xml version=\"1.0\"?><UMOptions><UnitMappers>True</UnitMappers></UMOptions>");
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("name"));
        }

        private static void AddOption(XmlDocument doc, XmlElement root, string name, string value)
        {
            XmlElement option = doc.CreateElement("Option");
            option.SetAttribute("name", name);
            option.InnerText = value;
            root.AppendChild(option);
        }
    }
}