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
            string testXmlPath = Path.Combine(TestConfiguration.GetTestDirectory(), "malicious.xml");

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.XmlResolver = null; 
                xmlDoc.Load(testXmlPath);
            }
            catch (Exception ex)
            {
                Assert.True(false, $"XML parsing with a secure resolver should not fail. Details: {ex.Message}");
            }
        }

        [Fact]
        public void TestProcessLaunchWithInvalidPath()
        {
            string invalidPath = "notepad.exe";

            try
            {
                Process.Start(invalidPath);
                Assert.True(false, "Process.Start should fail for non-executable paths when not in PATH.");
            }
            catch (Exception ex)
            {
                Assert.IsType<System.ComponentModel.Win32Exception>(ex);
            }
        }
    }
}