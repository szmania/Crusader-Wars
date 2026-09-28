using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CrusaderWars.data
{
    /// <summary>
    /// Handles one-time data migration tasks for the Crusader Conflicts application.
    /// Currently migrates legacy flat unit mappers directory structure into
    /// game-specific subdirectories (attila/ and rome_2/).
    /// </summary>
    public static class MigrationService
    {
        /// <summary>
        /// One-time migration: moves legacy flat unit mappers into game-specific subdirectories.
        /// Called once on startup. If unit mappers/attila already exists, migration is skipped.
        /// </summary>
        public static void MigrateUnitMappersDirectory()
        {
            string unitMappersRoot = @".\unit mappers";
            string attilaDir = Path.Combine(unitMappersRoot, "attila");
            string rome2Dir = Path.Combine(unitMappersRoot, "rome_2");
            string backupDir = @".\unit mappers_backup";

            // If attila subdirectory already exists, migration has already been performed
            if (Directory.Exists(attilaDir))
            {
                Program.Logger.Debug("Unit mappers migration already completed. Skipping.");
                return;
            }

            Program.Logger.Debug("Checking for legacy flat unit mappers structure...");

            // Check for legacy flat structure: subdirectories (excluding 'schemas', 'attila', 'rome_2') directly in unit mappers/
            var subdirs = new List<string>();
            if (Directory.Exists(unitMappersRoot))
            {
                subdirs = Directory.GetDirectories(unitMappersRoot)
                    .Select(d => Path.GetFileName(d))
                    .Where(name => !name.Equals("schemas", StringComparison.OrdinalIgnoreCase)
                                && !name.Equals("attila", StringComparison.OrdinalIgnoreCase)
                                && !name.Equals("rome_2", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!subdirs.Any())
            {
                Program.Logger.Debug("No legacy flat unit mappers found. Migration not needed.");
                // Ensure game subdirectories exist (they should already from CreateRequiredDirectories)
                Directory.CreateDirectory(attilaDir);
                Directory.CreateDirectory(rome2Dir);
                return;
            }

            Program.Logger.Debug($"Legacy flat structure detected with {subdirs.Count} mapper directories. Starting migration...");

            try
            {
                // Step 1: Create backup of entire unit mappers/ directory
                Program.Logger.Debug($"Creating backup at '{backupDir}'...");
                if (Directory.Exists(backupDir))
                {
                    Directory.Delete(backupDir, true);
                }
                CopyDirectoryRecursive(unitMappersRoot, backupDir);
                Program.Logger.Debug("Backup created successfully.");

                // Step 2: Ensure game subdirectories exist
                Directory.CreateDirectory(attilaDir);
                Directory.CreateDirectory(rome2Dir);

                // Step 3: Move all subdirectories (except schemas, attila, rome_2) into attila/
                int movedCount = 0;
                foreach (var subdirName in subdirs)
                {
                    string sourcePath = Path.Combine(unitMappersRoot, subdirName);
                    string destPath = Path.Combine(attilaDir, subdirName);

                    Program.Logger.Debug($"Moving '{sourcePath}' to '{destPath}'...");

                    // If destination already exists (unlikely), skip
                    if (Directory.Exists(destPath))
                    {
                        Program.Logger.Debug($"Destination '{destPath}' already exists. Skipping '{subdirName}'.");
                        continue;
                    }

                    Directory.Move(sourcePath, destPath);
                    movedCount++;
                    Program.Logger.Debug($"Moved '{subdirName}' to attila/.");
                }

                Program.Logger.Debug($"Migration complete. Moved {movedCount} directories to 'unit mappers/attila/'. Backup preserved at '{backupDir}'.");
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Migration failed: {ex.Message}");

                // Attempt rollback: restore from backup
                try
                {
                    if (Directory.Exists(backupDir))
                    {
                        Program.Logger.Debug("Attempting rollback from backup...");
                        if (Directory.Exists(unitMappersRoot))
                        {
                            Directory.Delete(unitMappersRoot, true);
                        }
                        CopyDirectoryRecursive(backupDir, unitMappersRoot);
                        Program.Logger.Debug("Rollback successful. Original structure restored.");
                    }
                }
                catch (Exception rollbackEx)
                {
                    Program.Logger.Debug($"Rollback also failed: {rollbackEx.Message}. Backup remains at '{backupDir}'.");
                }

                MessageBox.Show(
                    $"Failed to migrate unit mappers to the new directory structure.\n\n" +
                    $"Your original files have been preserved. A backup exists at '{backupDir}'.\n\n" +
                    $"Please report this issue on the Crusader Conflicts Discord.\n\nTechnical Details: {ex.Message}",
                    "Crusader Conflicts: Migration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Recursively copies a directory and all its contents to a destination.
        /// </summary>
        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubdir = Path.Combine(destDir, Path.GetFileName(subdir));
                CopyDirectoryRecursive(subdir, destSubdir);
            }
        }
    }
}