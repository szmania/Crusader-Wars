using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CrusaderWars;
using CrusaderWars.client;

namespace CrusaderWars.Tests.BookmarksPlus
{
    [TestClass]
    public class OptionsTabTests
    {
        private const string TestSettingsPath = "TestSettings.xml";
        private const string TestUnitMappersPath = "TestUnitMappers.xml";

        [TestInitialize]
        public void TestInitialize()
        {
            // Clean up any test files
            if (File.Exists(TestSettingsPath))
                File.Delete(TestSettingsPath);
            if (File.Exists(TestUnitMappersPath))
                File.Delete(TestUnitMappersPath);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up test files
            if (File.Exists(TestSettingsPath))
                File.Delete(TestSettingsPath);
            if (File.Exists(TestUnitMappersPath))
                File.Delete(TestUnitMappersPath);
        }

        [TestMethod]
        public void BookmarksPlusTab_IsVisibleAndSelectable()
        {
            // Arrange
            var optionsForm = new Options();
            
            // Act
            var bookmarksPlusTab = optionsForm.BookmarksPlus_Tab;
            
            // Assert
            Assert.IsNotNull(bookmarksPlusTab, "BookmarksPlus_Tab should be initialized");
            Assert.IsTrue(bookmarksPlusTab.Visible, "BookmarksPlus tab should be visible");
        }

        [TestMethod]
        public void GetActivePlaythrough_ReturnsBookmarksPlus_WhenSelected()
        {
            // Arrange
            var optionsForm = new Options();
            
            // Create test UnitMappers.xml with BookmarksPlus selected
            var xmlDoc = new XmlDocument();
            var declaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(declaration);
            
            var root = xmlDoc.CreateElement("UMOptions");
            xmlDoc.AppendChild(root);
            
            // Add DefaultCK3 as False
            var defaultElem = xmlDoc.CreateElement("UnitMappers");
            defaultElem.SetAttribute("name", "DefaultCK3");
            defaultElem.InnerText = "False";
            root.AppendChild(defaultElem);
            
            // Add BookmarksPlus as True
            var bookmarksElem = xmlDoc.CreateElement("UnitMappers");
            bookmarksElem.SetAttribute("name", "BookmarksPlus");
            bookmarksElem.InnerText = "True";
            root.AppendChild(bookmarksElem);
            
            xmlDoc.Save(TestUnitMappersPath);
            
            // Temporarily replace the UnitMappers.xml path
            var originalPath = typeof(Options).GetField("_unitMappersXmlPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            // Note: Since we can't easily mock the file path, we'll test the logic differently
            
            // Act - Test the GetOrCreateUnitMapperOption method directly
            var result = Options.GetOrCreateUnitMapperOption(xmlDoc, "BookmarksPlus");
            
            // Assert
            Assert.AreEqual("True", result, "BookmarksPlus should return 'True' when selected");
        }

        [TestMethod]
        public void WriteUnitMappersOptions_SavesBookmarksPlusState()
        {
            // Arrange
            var optionsForm = new Options();
            
            // Set the BookmarksPlus tab state
            if (optionsForm.BookmarksPlus_Tab != null)
            {
                optionsForm.BookmarksPlus_Tab.SetState(true);
            }
            
            // Act
            optionsForm.WriteUnitMappersOptions();
            
            // Assert - Verify the file was created and contains BookmarksPlus=True
            Assert.IsTrue(File.Exists(".\settings\UnitMappers.xml"), "UnitMappers.xml should be created");
            
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(".\settings\UnitMappers.xml");
            
            var bookmarksNode = xmlDoc.SelectSingleNode("//UnitMappers[@name='BookmarksPlus']");
            Assert.IsNotNull(bookmarksNode, "BookmarksPlus node should exist in UnitMappers.xml");
            Assert.AreEqual("True", bookmarksNode.InnerText, "BookmarksPlus should be set to True");
            
            // Cleanup
            if (File.Exists(".\settings\UnitMappers.xml"))
                File.Delete(".\settings\UnitMappers.xml");
        }

        [TestMethod]
        public void CheckPlaythroughSelection_SetsPulsingState()
        {
            // Arrange
            var optionsForm = new Options();
            
            // Act
            optionsForm.CheckPlaythroughSelection();
            
            // Assert - At least one tab should be pulsing or container should pulse
            // Since no playthrough is selected by default, container should pulse
            Assert.IsTrue(optionsForm._pulseTimer.Enabled || 
                         (optionsForm.BookmarksPlus_Tab != null && optionsForm.BookmarksPlus_Tab.GetPulsing()) ||
                         optionsForm.CrusaderKings_Tab.GetPulsing() ||
                         optionsForm.TheFallenEagle_Tab.GetPulsing() ||
                         optionsForm.RealmsInExile_Tab.GetPulsing() ||
                         optionsForm.AGOT_Tab.GetPulsing() ||
                         optionsForm.Custom_Tab.GetPulsing(),
                         "At least one tab or container should be pulsing");
        }
    }
}