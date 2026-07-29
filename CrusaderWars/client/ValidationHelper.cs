using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CrusaderWars.client
{
    public static class ValidationHelper
    {
        /// <summary>
        /// Handles a validation failure by logging, backing up the corrupted file,
        /// prompting the user to reset or exit, and optionally creating a default file.
        /// </summary>
        /// <param name="xmlPath">Absolute path to the corrupted XML file.</param>
        /// <param name="xmlFileName">Display name of the XML file (e.g., "Options.xml").</param>
        /// <param name="validationErrors">List of validation error messages from XmlValidator.Validate.</param>
        /// <param name="createDefaultFileAction">Action that creates a valid default XML file at xmlPath.</param>
        /// <returns>True if the user chose to reset and the default file was created; False if the user chose to exit.</returns>
        public static bool HandleValidationFailure(string xmlPath, string xmlFileName, List<string> validationErrors, Action createDefaultFileAction)
        {
            if (validationErrors == null || validationErrors.Count == 0)
            {
                Program.Logger.Debug($"HandleValidationFailure called for {xmlFileName} with no validation errors. Skipping.");
                return false;
            }

            // Log the failure
            Program.Logger.Debug($"XML validation failed for {xmlFileName}: {xmlPath}");
            foreach (var error in validationErrors)
            {
                Program.Logger.Debug($"  {error}");
            }

            // Create a backup of the corrupted file
            try
            {
                string backupPath = xmlPath + ".corrupted.bak";
                File.Copy(xmlPath, backupPath, overwrite: true);
                Program.Logger.Debug($"Backup of corrupted {xmlFileName} created at {backupPath}");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Warning: Could not create backup of corrupted {xmlFileName}: {ex.Message}");
            }

            // Show MessageBox
            var dialogResult = MessageBox.Show(
                $"The file '{xmlFileName}' is corrupted or invalid and cannot be loaded.\n\nWould you like to reset it to default settings?\n\nClicking 'No' will exit the application.",
                "Configuration File Error",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dialogResult == DialogResult.Yes)
            {
                createDefaultFileAction();
                Program.Logger.Debug($"Default file created for {xmlFileName} at {xmlPath}");
                return true;
            }
            else
            {
                Program.Logger.Debug($"User chose to exit due to corrupted {xmlFileName}");
                Application.Exit();
                return false;
            }
        }
    }
}
