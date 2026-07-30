using System;
using System.IO;
using System.Xml.Schema;
using Xunit;

namespace CrusaderWars.tests.XmlValidation
{
    public class XmlValidatorTests
    {
        private readonly string _schemaDir;

        public XmlValidatorTests()
        {
            _schemaDir = XmlTestHelper.GetSchemaDirectory();
        }

        [Fact]
        public void Validate_ValidOptionsXml_ReturnsNoErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(xmlPath);
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ValidActiveSubmodsXml_ReturnsNoErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "ActiveSubmods.xml");
            XmlTestHelper.CreateValidActiveSubmodsXml(xmlPath);
            string xsdPath = Path.Combine(_schemaDir, "ActiveSubmods.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ValidPathsXml_ReturnsNoErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Paths.xml");
            XmlTestHelper.CreateValidPathsXml(xmlPath);
            string xsdPath = Path.Combine(_schemaDir, "Paths.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_ValidUnitMappersXml_ReturnsNoErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "UnitMappers.xml");
            XmlTestHelper.CreateValidUnitMappersXml(xmlPath);
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_MissingXmlFile_ReturnsFileNotFoundError()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Nonexistent.xml");
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.Single(errors);
            Assert.Contains("XML file not found", errors[0]);
        }

        [Fact]
        public void Validate_MissingXsdFile_ReturnsSchemaNotFoundError()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(xmlPath);
            string xsdPath = Path.Combine(_schemaDir, "Nonexistent.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.Single(errors);
            Assert.Contains("XSD schema file not found", errors[0]);
        }

        [Fact]
        public void Validate_NullXmlPath_ThrowsArgumentException()
        {
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            Assert.Throws<ArgumentException>(() => XmlTestHelper.ValidateXml(null!, xsdPath));
        }

        [Fact]
        public void Validate_EmptyXmlPath_ThrowsArgumentException()
        {
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            Assert.Throws<ArgumentException>(() => XmlTestHelper.ValidateXml(string.Empty, xsdPath));
        }

        [Fact]
        public void Validate_NullXsdPath_ThrowsArgumentException()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(xmlPath);

            Assert.Throws<ArgumentException>(() => XmlTestHelper.ValidateXml(xmlPath, null!));
        }

        [Fact]
        public void Validate_EmptyXsdPath_ThrowsArgumentException()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Options.xml");
            XmlTestHelper.CreateValidOptionsXml(xmlPath);

            Assert.Throws<ArgumentException>(() => XmlTestHelper.ValidateXml(xmlPath, string.Empty));
        }

        [Fact]
        public void Validate_MalformedXml_ReturnsValidationErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Malformed.xml");
            File.WriteAllText(xmlPath, "<?xml version=\\\"1.0\\\"?><Options><Option name=\\\"Test\\\">Value</Option></Options>");
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void Validate_WrongRootElement_ReturnsValidationErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "WrongRoot.xml");
            File.WriteAllText(xmlPath, "<?xml version=\\\"1.0\\\"?><WrongRoot><Option name=\\\"Test\\\">Value</Option></WrongRoot>");
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("Root element"));
        }

        [Fact]
        public void Validate_MissingRequiredAttribute_ReturnsValidationErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "MissingAttr.xml");
            File.WriteAllText(xmlPath, "<?xml version=\\\"1.0\\\"?><Options><Option>Value</Option></Options>");
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("name"));
        }

        [Fact]
        public void Validate_InvalidElementContent_ReturnsValidationErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "InvalidContent.xml");
            File.WriteAllText(xmlPath, "<?xml version=\\\"1.0\\\"?><UMOptions><UnitMappers name=\\\"DefaultCK3\\\" value=\\\"Maybe\\\"/></UMOptions>");
            string xsdPath = Path.Combine(_schemaDir, "UnitMappers.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void Validate_ExtraElements_ReturnsValidationErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "ExtraElements.xml");
            File.WriteAllText(xmlPath,
                "<?xml version=\"1.0\"?>" +
                "<Options>" +
                "<Option name=\"Test\">Value</Option>" +
                "<ExtraElement>ShouldNotBeHere</ExtraElement>" +
                "</Options>");
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void Validate_EmptyXmlFile_ReturnsValidationErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Empty.xml");
            File.WriteAllText(xmlPath, string.Empty);
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void Validate_XmlWithBom_ReturnsNoErrors()
        {
            string xmlPath = Path.Combine(XmlTestHelper.GetTempSettingsDirectory(), "Bom.xml");
            string xmlContent = "<?xml version=\\\"1.0\\\" encoding=\\\"UTF-8\\\"?><Options><Option name=\\\"Test\\\">Value</Option></Options>";
            File.WriteAllText(xmlPath, xmlContent, new System.Text.UTF8Encoding(true));
            string xsdPath = Path.Combine(_schemaDir, "Options.xsd");

            var errors = XmlTestHelper.ValidateXml(xmlPath, xsdPath);

            Assert.Empty(errors);
        }
    }
}
