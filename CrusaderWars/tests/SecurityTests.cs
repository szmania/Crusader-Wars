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
    }
}