using System.Xml;

namespace CrusaderWars.Tests.TestHelpers
{
    /// <summary>
    /// Helper methods for creating and manipulating XML documents in tests.
    /// </summary>
    public static class TestXmlHelper
    {
        /// <summary>
        /// Creates a minimal valid XmlDocument for testing.
        /// </summary>
        public static XmlDocument CreateValidUnitMappersXml()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?><UMOptions><UnitMappers name=\"DefaultCK3\">True</UnitMappers><UnitMappers name=\"BookmarksPlus\">False</UnitMappers></UMOptions>");
            return doc;
        }

        /// <summary>
        /// Creates an XmlDocument with XPath-breaking characters in a tag name (security test).
        /// </summary>
        public static XmlDocument CreateXPathInjectionXml()
        {
            var doc = new XmlDocument();
            // Simulates a malicious tag name that could break XPath queries
            doc.LoadXml("<?xml version=\"1.0\"?><UMOptions><UnitMappers name=\"BookmarksPlus' or '1'='1\">True</UnitMappers></UMOptions>");
            return doc;
        }

        /// <summary>
        /// Creates an XmlDocument with path traversal characters in a value.
        /// </summary>
        public static XmlDocument CreatePathTraversalXml()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<?xml version=\"1.0\"?><UMOptions><UnitMappers name=\"Custom\">../../../Windows/System32</UnitMappers></UMOptions>");
            return doc;
        }
    }
}