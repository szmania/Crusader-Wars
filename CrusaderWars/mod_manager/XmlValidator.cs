using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Xml;
using System.Xml.Schema;

namespace CrusaderWars.mod_manager
{
    public static class XmlValidator
    {
        /// <summary>
        /// Validates an XML file against an XSD schema.
        /// </summary>
        /// <param name="xmlPath">Absolute path to the XML file to validate.</param>
        /// <param name="xsdPath">Absolute path to the XSD schema file.</param>
        /// <returns>A list of validation error messages. An empty list indicates the XML is valid.</returns>
        [SupportedOSPlatform("windows")]
        public static List<string> Validate(string xmlPath, string xsdPath)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(xmlPath))
            {
                throw new ArgumentException(nameof(xmlPath));
            }
            if (string.IsNullOrEmpty(xsdPath))
            {
                throw new ArgumentException(nameof(xsdPath));
            }

            string fullXmlPath = Path.GetFullPath(xmlPath);

            if (!File.Exists(fullXmlPath))
            {
                errors.Add($"XML file not found: {fullXmlPath}");
                return errors;
            }

            if (!File.Exists(xsdPath))
            {
                errors.Add($"XSD schema file not found: {xsdPath}");
                return errors;
            }

            try
            {
                var settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessInlineSchema | XmlSchemaValidationFlags.ProcessSchemaLocation
                };
                settings.Schemas.Add(null, xsdPath);

                settings.ValidationEventHandler += (sender, args) =>
                {
                    string message;
                    if (args.Exception != null)
                    {
                        message = $"File: {fullXmlPath}, Error: Line {args.Exception.LineNumber}, Position {args.Exception.LinePosition} - {args.Message}";
                    }
                    else
                    {
                        message = $"File: {fullXmlPath}, Error: {args.Message}";
                    }
                    if (!errors.Contains(message)) errors.Add(message);
                };

                using (var reader = XmlReader.Create(fullXmlPath, settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"An error occurred during validation of {Path.GetFileName(fullXmlPath)}: {ex.Message}");
            }

            return errors;
        }
    }
}
