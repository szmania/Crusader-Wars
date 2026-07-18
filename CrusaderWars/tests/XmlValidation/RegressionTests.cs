using System;
using System.IO;
using System.Xml;
using Xunit;

namespace CrusaderWars.tests.XmlValidation
{
    public class RegressionTests
    {
        private readonly string _schemaDir;
        private readonly string _tempDir;

        public RegressionTests()
        {
            _schemaDir = XmlTestHelper.GetSchemaDirectory();
            _tempDir = XmlTestHelper.GetTempSettingsDirectory();
        }

        [Fact]
        public void OptionsLoading_ValidFile_AllOptionsReadCorrectly()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            // Assert - verify all expected option keys exist
            Assert.Empty(errors);
            Assert.NotNull(doc.SelectSingleNode("//Option[@name='CloseCK3']"));
            Assert.NotNull(doc.SelectSingleNode("//Option[@name='CloseAttila']"));
            Assert.NotNull(doc.SelectSingleNode("//Option[@name='LeviesMax']"));
            Assert.NotNull(doc.SelectSingleNode("//Option[@name='RangedMax']"));
            Assert.NotNull(doc.SelectSingleNode("//Option[@name='InfantryMax']"));
            Assert.NotNull(doc.SelectSingleNode("//Option[@name='CavalryMax']"));
        }

        [Fact]
        public void SubmodManagerLoading_ValidFile_ActiveSubmodsAccessible()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "ActiveSubmods.xml");
            XmlTestHelper.CreateValidActiveSubmodsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "ActiveSubmods.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var playthroughTag = doc.SelectSingleNode("//Playthrough/@tag")?.Value;
            var submodNode = doc.SelectSingleNode("//Submod");

            // Assert
            Assert.Empty(errors);
            Assert.NotNull(playthroughTag);
            Assert.Equal("DefaultCK3", playthroughTag);
            Assert.NotNull(submodNode);
            Assert.Equal("TestSubmod", submodNode!.InnerText);
        }

        [Fact]
        public void PathLoading_ValidFile_PathsLoadedCorrectly()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Paths.xml");
            XmlTestHelper.CreateValidPathsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var attilaPath = doc.SelectSingleNode("//TotalWarAttila/@path")?.Value;
            var ck3Path = doc.SelectSingleNode("//CrusaderKings/@path")?.Value;

            // Assert
            Assert.Empty(errors);
            Assert.NotNull(attilaPath);
            Assert.NotNull(ck3Path);
            Assert.Contains("Attila", attilaPath);
            Assert.Contains("ck3", ck3Path);
        }

        [Fact]
        public void UnitMapperLoading_ValidFile_ToggleStatesReadCorrectly()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");
            XmlTestHelper.CreateValidUnitMappersXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");

            // Act
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            // Assert - all unit mappers should be set to False in default
            Assert.Empty(errors);
            var defaultCK3Node = doc.SelectSingleNode("//UnitMappers[@name='DefaultCK3']");
            Assert.NotNull(defaultCK3Node);
            Assert.Equal("False", defaultCK3Node!.InnerText);

            var theFallenEagleNode = doc.SelectSingleNode("//UnitMappers[@name='TheFallenEagle']");
            Assert.NotNull(theFallenEagleNode);
            Assert.Equal("False", theFallenEagleNode!.InnerText);
        }

        [Fact]
        public void ApplicationStartup_AllValidFiles_NoValidationErrors()
        {
            // Arrange - create all valid config files
            string optionsPath = Path.Combine(_tempDir, "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(optionsPath);

            string submodsPath = Path.Combine(_tempDir, "ActiveSubmods.xml");
            XmlTestHelper.CreateValidActiveSubmodsXml(submodsPath);

            string pathsPath = Path.Combine(_tempDir, "Paths.xml");
            XmlTestHelper.CreateValidPathsXml(pathsPath);

            string unitMappersPath = Path.Combine(_tempDir, "UnitMappers.xml");
            XmlTestHelper.CreateValidUnitMappersXml(unitMappersPath);

            // Act & Assert - all files should pass validation
            Assert.Empty(XmlTestHelper.ValidateXml(optionsPath, Path.Combine(_schemaDir, "Options.xsd")));
            Assert.Empty(XmlTestHelper.ValidateXml(submodsPath, Path.Combine(_schemaDir, "ActiveSubmods.xsd")));
            Assert.Empty(XmlTestHelper.ValidateXml(pathsPath, Path.Combine(_schemaDir, "Paths.xsd")));
            Assert.Empty(XmlTestHelper.ValidateXml(unitMappersPath, Path.Combine(_schemaDir, "UnitMappers.xsd")));
        }

        [Fact]
        public void OptionsRoundTrip_ChangeAndPersist_ValuesSurviveRoundtrip()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            // Act - change a value and save
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var leviesMaxNode = doc.SelectSingleNode("//Option[@name='LeviesMax']");
            Assert.NotNull(leviesMaxNode);
            leviesMaxNode!.InnerText = "25";
            doc.Save(filePath);

            // Reload and verify
            XmlDocument reloaded = new XmlDocument();
            reloaded.Load(filePath);
            var savedNode = reloaded.SelectSingleNode("//Option[@name='LeviesMax']");

            // Assert
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);
            Assert.NotNull(savedNode);
            Assert.Equal("25", savedNode!.InnerText);
        }

        [Fact]
        public void UnitMappersRoundTrip_ToggleAndPersist_StateSurvivesRoundtrip()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "UnitMappers.xml");
            XmlTestHelper.CreateValidUnitMappersXml(filePath);
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");

            // Act - toggle a value and save
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            var tfeNode = doc.SelectSingleNode("//UnitMappers[@name='TheFallenEagle']");
            Assert.NotNull(tfeNode);
            tfeNode!.InnerText = "True";
            doc.Save(filePath);

            // Reload and verify
            XmlDocument reloaded = new XmlDocument();
            reloaded.Load(filePath);
            var savedNode = reloaded.SelectSingleNode("//UnitMappers[@name='TheFallenEagle']");

            // Assert
            var errors = XmlTestHelper.ValidateXml(filePath, xsdPath);
            Assert.Empty(errors);
            Assert.NotNull(savedNode);
            Assert.Equal("True", savedNode!.InnerText);
        }
    }
}