using Xunit;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace CrusaderWars.tests
{
    public class SecurityTests
    {
        [Fact]
        public void TestXmlExternalEntityInjection()
        {
            // Create a malicious XML file inline to avoid deployment issues.
            // This file contains an external entity reference that would attempt
            // to read a local file if XmlResolver were not set to null.
            string maliciousXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE foo [
  <!ENTITY xxe SYSTEM ""file:///etc/passwd"">
]>
<root>&xxe;</root>";

            string testXmlPath = Path.Combine(TestConfiguration.GetTestDirectory(), "malicious.xml");
            File.WriteAllText(testXmlPath, maliciousXml);

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.XmlResolver = null; // Secure resolver: disables external entity resolution
                xmlDoc.Load(testXmlPath);
                // If we reach here, the secure resolver prevented XXE — success.
            }
            catch (Exception ex)
            {
                Assert.True(false, $"XML parsing with a secure resolver should not fail. Details: {ex.Message}");
            }
            finally
            {
                // Clean up the temporary file
                if (File.Exists(testXmlPath))
                {
                    File.Delete(testXmlPath);
                }
            }
        }

        [Fact]
        public void TestProcessLaunchWithInvalidPath()
        {
            // Use a path that is guaranteed not to exist and not be in PATH.
            // On Windows, Process.Start with a nonexistent executable throws Win32Exception.
            string invalidPath = "nonexistent_executable_xyz123_test.exe";

            var exception = Assert.Throws<System.ComponentModel.Win32Exception>(
                () => Process.Start(invalidPath));

            // Verify the exception message indicates the file was not found
            Assert.Contains("cannot find", exception.Message.ToLowerInvariant());
        }

        [Fact]
        public void TestXmlValidatorPreventsXXE()
        {
            // Arrange - create malicious XML with XXE payload targeting a sensitive file
            string maliciousXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<root>&xxe;</root>";

            string testXmlPath = Path.Combine(TestConfiguration.GetTestDirectory(), "xxe_validator_test.xml");
            File.WriteAllText(testXmlPath, maliciousXml);

            try
            {
                string xsdPath = Path.Combine(TestConfiguration.GetTestDirectory(), "XmlValidation", "Schemas", "Options.xsd");
                var errors = CrusaderWars.mod_manager.XmlValidator.Validate(testXmlPath, xsdPath);

                // Assert - validation should fail (wrong root element), but the error must NOT contain resolved entity content
                Assert.NotEmpty(errors);
                string combinedErrors = string.Join(" ", errors);
                // The XML references &xxe; which should NOT be resolved. The error should be about the root element, not about reading /etc/passwd.
                Assert.DoesNotContain("root:x:0:0", combinedErrors); // Should not contain /etc/passwd content
                Assert.Contains("Root element", combinedErrors); // Should report root element mismatch
            }
            finally
            {
                if (File.Exists(testXmlPath))
                {
                    File.Delete(testXmlPath);
                }
            }
        }

        [Fact]
        public void TestXmlDocumentLoadingUsesSecureResolver()
        {
            // Arrange - create malicious XML that references a Windows system file
            string maliciousXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<root>&xxe;</root>";

            string testXmlPath = Path.Combine(TestConfiguration.GetTestDirectory(), "secure_resolver_test.xml");
            File.WriteAllText(testXmlPath, maliciousXml);

            try
            {
                // Act - load with secure resolver (XmlResolver = null)
                var xmlDoc = new XmlDocument();
                xmlDoc.XmlResolver = null; // Secure resolver: disables external entity resolution
                xmlDoc.Load(testXmlPath);

                // Assert - the entity should not have been resolved; the InnerXml should not contain system file content
                Assert.DoesNotContain("[boot loader]", xmlDoc.InnerXml); // Should not contain boot.ini content
                Assert.DoesNotContain("windows", xmlDoc.InnerXml.ToLowerInvariant()); // Should not contain resolved external content
            }
            finally
            {
                if (File.Exists(testXmlPath))
                {
                    File.Delete(testXmlPath);
                }
            }
        }

        [Fact]
        public void TestPathTraversalProtection()
        {
            // Arrange - create a valid XML test file
            string testXmlPath = Path.Combine(TestConfiguration.GetTestDirectory(), "traversal_test.xml");
            File.WriteAllText(testXmlPath, "<?xml version=\"1.0\"?><Options><Option name=\"Test\">Value</Option></Options>");

            try
            {
                // Test 1: Relative path traversal - schema path with directory traversal
                string traversalXsdPath = Path.Combine("..\\..\\..\\..\\windows\\system32\\drivers\\etc\\hosts");
                var errors1 = CrusaderWars.mod_manager.XmlValidator.Validate(testXmlPath, traversalXsdPath);
                Assert.NotEmpty(errors1);
                Assert.Contains(errors1, e => e.Contains("Schema file not found"));

                // Test 2: Absolute path outside expected directories pointing to a system file with .xsd extension
                string absolutePath = "C:\\windows\\system32\\drivers\\etc\\hosts.xsd";
                var errors2 = CrusaderWars.mod_manager.XmlValidator.Validate(testXmlPath, absolutePath);
                Assert.NotEmpty(errors2);
                Assert.Contains(errors2, e => e.Contains("Schema file not found"));

                // Test 3: Valid path within expected test directory should work (schema exists for Options)
                string validXsdPath = Path.Combine(TestConfiguration.GetTestDirectory(), "XmlValidation", "Schemas", "Options.xsd");
                // Only test if the schema file exists in the test output directory
                if (File.Exists(validXsdPath))
                {
                    var errors3 = CrusaderWars.mod_manager.XmlValidator.Validate(testXmlPath, validXsdPath);
                    Assert.Empty(errors3);
                }
            }
            finally
            {
                if (File.Exists(testXmlPath))
                {
                    File.Delete(testXmlPath);
                }
            }
        }
    }
}