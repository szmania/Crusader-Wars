using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Net;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Linq;
using System.Net.Http; // Added this line
using System.Collections.Generic; // Added for HashSet

namespace CWUpdater
{
    public partial class AutoUpdater : Form
    {
        private static readonly HttpClient httpClient = new HttpClient(); // Added this line

        private string? DownloadUrl { get; set; } // Made nullable
        private string? UpdateVersion { get; set; } // Made nullable
        private bool IsUnitMappers { get; set; }

        public AutoUpdater()
        {
            Logger.Log("Initializing AutoUpdater form.");
            if(GetArguments())
            {
                
                InitializeComponent();
                this.TopMost = true;
                

                if(IsUnitMappers)
                {
                    TitleLabel.Text = "New Unit Mappers Update Available!";
                    WarningLabel.Hide();
                }
                this.TopMost = false;
            }                
            else
            {
                Logger.Log("Failed to get required arguments. Exiting.");
                Environment.Exit(1);
            }
        }

        bool GetArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            Logger.Log($"Arguments received: {string.Join(" ", args)}");

            if (args.Length == 3) // Check if at least 2 arguments are present
            {
                DownloadUrl = args[1];
                UpdateVersion = args[2];
                IsUnitMappers = false;
                Logger.Log($"App update detected. URL: {DownloadUrl}, Version: {UpdateVersion}");
                return true;
            }
            else if (args.Length == 4) // Check if at least 3 arguments are present
            {
                DownloadUrl = args[1];
                UpdateVersion = args[2];
                IsUnitMappers = true;
                Logger.Log($"Unit Mappers update detected. URL: {DownloadUrl}, Version: {UpdateVersion}");
                return true;
            }
            Logger.Log("Invalid number of arguments.");
            return false;
        }

        private string? GetCrusaderWarsExecutable() // Changed return type to nullable
        {
            // Use the same robust pathing logic as the ApplyUpdate method
            string? currentDir = AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", "");
            if (string.IsNullOrEmpty(currentDir))
            {
                Logger.Log("Could not determine the application's root directory.");
                return null;
            }

            string exe1 = Path.Combine(currentDir, "CrusaderWars.exe");
            string exe2 = Path.Combine(currentDir, "Crusader Wars.exe");

            if (File.Exists(exe1)) return exe1;
            if (File.Exists(exe2)) return exe2;

            Logger.Log("Neither CrusaderWars.exe nor Crusader Wars.exe was found.");
            return null;
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                Logger.Log("Update button clicked.");
                btnUpdate.Enabled = false;
                btnUpdate.Text = "Updating..";
                if (DownloadUrl != null) // Added null check for DownloadUrl
                {
                    await DownloadUpdateAsync(DownloadUrl);
                }
                else
                {
                    Logger.Log("Download URL is null. Cannot proceed with update.");
                    MessageBox.Show("Error: Download URL is missing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"An error occurred in btnUpdate_Click: {ex.ToString()}");
                string? executable = GetCrusaderWarsExecutable(); // Changed to nullable
                if (executable != null)
                {
                    Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
                }
                Environment.Exit(1);
            }
            
        }

        public async Task DownloadUpdateAsync(string downloadUrl)
        {
            Logger.Log($"Starting download from: {downloadUrl}");
 
            try
            {
                string downloadPath = Path.Combine(Path.GetTempPath(), "update.zip");

                // Replaced WebClient with HttpClient
                using (HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;
                        
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (totalBytes.HasValue)
                            {
                                int progressPercentage = (int)((double)totalBytesRead / totalBytes.Value * 100);
                                this.Invoke((MethodInvoker)delegate {
                                    label1.Text = progressPercentage.ToString() + "%";
                                });
                            }
                        }
                    }
                }

                Console.WriteLine("Update downloaded successfully.");
                Logger.Log("Update downloaded successfully.");

                if(!IsUnitMappers) {
                    Logger.Log("Applying application update.");
                    ApplyUpdate(downloadPath, AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", ""));
                }
                else
                {
                    Logger.Log("Applying Unit Mappers update.");
                    ApplyUpdate(downloadPath, AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", @"\unit mappers"));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error downloading update: {ex.ToString()}");
                MessageBox.Show($"Error downloading update: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                this.Close();
            }
            
        }

        public void ApplyUpdate(string updateFilePath, string applicationPath)
        {
            Logger.Log($"Applying update from '{updateFilePath}' to '{applicationPath}'.");
            label1.Text = "Applying update...";
            string backupPath = Path.Combine(Path.GetTempPath(), "app_backup");
            string tempDirectory = Path.Combine(Path.GetTempPath(), "update");
            string mainAppRoot = AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", "");
            string updaterDirInMainApp = Path.Combine(mainAppRoot, "data", "updater"); // This is the directory of the running updater

            try
            {
                // Step 1: Backup existing files
                Logger.Log("Backing up application files.");
                BackupApplicationFiles(applicationPath, backupPath);

                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
                Logger.Log("Extracting update file.");
                ZipFile.ExtractToDirectory(updateFilePath, tempDirectory);

                // Delete obsolete files and directories
                Logger.Log("Deleting obsolete files and directories.");
                DeleteObsoleteFilesAndDirectories(applicationPath, tempDirectory);

                // Copy new and updated files
                Logger.Log("Copying new and updated files.");
                foreach (var file in Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories))
                {
                    if(IsUnitMappers)//<-- UNIT MAPPERS UPDATER
                    {
                        string relativePath = file.Substring(tempDirectory.Length + 1);
                        // No parentFolder logic for unit mappers, as the zip should contain the mapper's root directly
                        string destinationPath = Path.Combine(applicationPath, relativePath);

                        string? destinationDir = Path.GetDirectoryName(destinationPath);
                        if (destinationDir != null && !Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        File.Copy(file, destinationPath, true);
                    }
                    else if(!IsUnitMappers) //<-- APP UPDATER
                    {
                        string relativePath = file.Substring(tempDirectory.Length + 1);
                        string parentFolder = relativePath.Split('\\')[0];
                        string finalRelativePath = relativePath.Replace($"{parentFolder}\\", "");
                        string destinationPath = Path.Combine(applicationPath, finalRelativePath);

                        // Skip files that are part of the running updater
                        if (destinationPath.StartsWith(updaterDirInMainApp, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Log($"Skipping copy of updater file: {file} to {destinationPath}");
                            continue;
                        }

                        // Define a set of user-specific files to preserve during updates.
                        var settingsFilesToSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        {
                            @"Settings\Options.xml",
                            @"Settings\Paths.xml",
                            @"Settings\lastchecked.txt",
                            @"Settings\UnitMappers.xml",
                            "active_mods.txt"
                        };

                        // Skip overwriting essential user settings files.
                        if (settingsFilesToSkip.Contains(finalRelativePath))
                        {
                            Logger.Log($"Skipping overwrite of user settings file: {finalRelativePath}");
                            continue;
                        }

                        string? destinationDir = Path.GetDirectoryName(destinationPath);
                        if (destinationDir != null && !Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        File.Copy(file, destinationPath, true);
                    }
 
                }

                // Step 5: Clean up backup if update was successful
                Logger.Log("Update successful, deleting backup.");
                Directory.Delete(backupPath, true);

                // Add this confirmation message
                MessageBox.Show("Update completed successfully! The application will now restart.",
                                "Update Successful",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                Console.WriteLine("Update applied successfully.");
                Logger.Log("Update applied successfully.");
                RestartApplication();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error applying update: {ex.ToString()}");
                MessageBox.Show($"Error applying update: {ex.Message}{ex.TargetSite}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                // Step 6: Rollback to the backup if an error occurs
                Logger.Log("Rolling back to backup.");
                RestoreBackup(backupPath, applicationPath);
                this.Close();
            }
            finally
            {
                // Clean up the temp directory
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }

        }
        private void DeleteObsoleteFilesAndDirectories(string applicationPath, string tempDirectory)
        {
            Logger.Log("Starting deletion of obsolete files and directories.");
            string mainAppRoot = AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", "");
            string updaterDir = Path.Combine(mainAppRoot, "data", "updater"); // This is the directory of the running updater

            // Delete obsolete files
            if(IsUnitMappers)
            {
                var existingFiles = Directory.GetFiles(applicationPath, "*", SearchOption.AllDirectories);
                var newFiles = Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in existingFiles)
                {
                    string relativePath = file.Substring(applicationPath.Length + 1);
                    string correspondingNewFile = Path.Combine(tempDirectory, relativePath);

                    if (!File.Exists(correspondingNewFile))
                    {
                        File.Delete(file);
                    }
                }

                // Delete empty and obsolete directories
                var existingDirs = Directory.GetDirectories(applicationPath, "*", SearchOption.AllDirectories);
                var newDirs = Directory.GetDirectories(tempDirectory, "*", SearchOption.AllDirectories).Select(d => d.Substring(tempDirectory.Length + 1)).ToHashSet();

                foreach (var dir in existingDirs.OrderByDescending(d => d.Length))
                {
                    string relativeDirPath = dir.Substring(applicationPath.Length + 1);
                    if (!newDirs.Contains(relativeDirPath) && !Directory.GetFiles(dir).Any() && !Directory.GetDirectories(dir).Any())
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            else if (!IsUnitMappers) // Application updater
            {
                string settingsDir = Path.Combine(applicationPath, "settings"); // Added: Path to the settings directory

                var existingFiles = Directory.GetFiles(applicationPath, "*", SearchOption.AllDirectories);
                var newFiles = Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in existingFiles)
                {
                    // Skip files within the updater directory
                    if (file.StartsWith(updaterDir, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Added: Skip files within the Settings directory
                    if (file.StartsWith(settingsDir, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Define a set of user-specific files to preserve during updates.
                    var settingsFilesToSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        @"Settings\Options.xml",
                        @"Settings\Paths.xml",
                        @"Settings\lastchecked.txt",
                        @"Settings\UnitMappers.xml",
                        "active_mods.txt"
                    };

                    string relativePathForDeletionCheck = file.Substring(applicationPath.Length + 1);
                    if (settingsFilesToSkip.Contains(relativePathForDeletionCheck))
                    {
                        continue;
                    }

                    string relativePath = file.Substring(applicationPath.Length + 1);
                    string parentFolder = relativePath.Split('\\')[0];
                    relativePath = relativePath.Replace($"{parentFolder}\\", "");
                    string correspondingNewFile = Path.Combine(tempDirectory, relativePath);

                    if (!File.Exists(correspondingNewFile))
                    {
                        File.Delete(file);
                    }
                }

                // Delete empty and obsolete directories
                var existingDirs = Directory.GetDirectories(applicationPath, "*", SearchOption.AllDirectories);
                var newDirs = Directory.GetDirectories(tempDirectory, "*", SearchOption.AllDirectories).Select(d => d.Substring(tempDirectory.Length + 1)).ToHashSet();

                foreach (var dir in existingDirs.OrderByDescending(d => d.Length))
                {
                    // Skip the updater directory itself or any directory within its hierarchy (parent, self, or child)
                    if (dir.StartsWith(updaterDir, StringComparison.OrdinalIgnoreCase) || updaterDir.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Added: Skip the Settings directory itself or any directory within its hierarchy
                    if (dir.StartsWith(settingsDir, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string relativeDirPath = dir.Substring(applicationPath.Length + 1);
                    string parentFolder = relativeDirPath.Split('\\')[0];
                    relativeDirPath = relativeDirPath.Replace($"{parentFolder}\\", "");

                    if (!newDirs.Contains(relativeDirPath) && !Directory.GetFiles(dir).Any() && !Directory.GetDirectories(dir).Any())
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            Logger.Log("Finished deletion of obsolete files and directories.");

        }


        //
        //  BACKUP FUNCTIONS
        //

        private void BackupApplicationFiles(string applicationPath, string backupPath)
        {
            Logger.Log($"Backing up from '{applicationPath}' to '{backupPath}'.");
            if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, true);  // Ensure the backup directory is clean
            }

            Directory.CreateDirectory(backupPath);

            foreach (var dirPath in Directory.GetDirectories(applicationPath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(applicationPath, backupPath));
            }

            foreach (var filePath in Directory.GetFiles(applicationPath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(applicationPath, backupPath), true);
            }
            Logger.Log("Backup complete.");
        }

        private void RestoreBackup(string backupPath, string applicationPath)
        {
            Logger.Log($"Restoring backup from '{backupPath}' to '{applicationPath}'.");
            string mainAppRoot = AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", "");
            string updaterDir = Path.Combine(mainAppRoot, "data", "updater"); // This is the directory of the running updater

            if (Directory.Exists(applicationPath))
            {
                // Delete all files in applicationPath, except those within the updater directory
                foreach (var file in Directory.GetFiles(applicationPath, "*", SearchOption.AllDirectories))
                {
                    if (!file.StartsWith(updaterDir, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(file); }
                        catch (Exception ex) { Logger.Log($"Warning: Could not delete file '{file}' during rollback: {ex.Message}"); }
                    }
                }

                // Delete all directories in applicationPath, except the updater directory itself or its parents
                // Iterate in reverse order of length to delete deepest directories first
                foreach (var dir in Directory.GetDirectories(applicationPath, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                {
                    // Skip the updater directory itself or any directory within its hierarchy (parent, self, or child)
                    if (!dir.StartsWith(updaterDir, StringComparison.OrdinalIgnoreCase) && !updaterDir.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                    {
                        try { Directory.Delete(dir, true); }
                        catch (Exception ex) { Logger.Log($"Warning: Could not delete directory '{dir}' during rollback: {ex.Message}"); }
                    }
                }
            }
            // Ensure the applicationPath exists (it might have been partially deleted)
            Directory.CreateDirectory(applicationPath);

            foreach (var dirPath in Directory.GetDirectories(backupPath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(backupPath, applicationPath));
            }

            foreach (var filePath in Directory.GetFiles(backupPath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(backupPath, applicationPath), true);
            }
            Logger.Log("Restore complete.");
        }

        public void RestartApplication()
        {
            Logger.Log("Restarting application.");

            if(!IsUnitMappers)
            {
                //Update application .txt file version
                string version_path = Directory.GetCurrentDirectory() + "\\app_version.txt";
                Logger.Log($"Updating app version file: {version_path} to version {UpdateVersion}");
                if (UpdateVersion != null) // Added null check for UpdateVersion
                {
                    File.WriteAllText(version_path, $"version=\"{UpdateVersion}\"");
                }
                else
                {
                    Logger.Log("UpdateVersion is null. Cannot write app version file.");
                }
            }
            else if(IsUnitMappers)
            {
                //Update unit mappers .txt file version
                string version_path = Directory.GetCurrentDirectory() + "\\um_version.txt";
                Logger.Log($"Updating unit mappers version file: {version_path} to version {UpdateVersion}");
                if (UpdateVersion != null) // Added null check for UpdateVersion
                {
                    File.WriteAllText(version_path, $"version=\"{UpdateVersion}\"");
                }
                else
                {
                    Logger.Log("UpdateVersion is null. Cannot write unit mappers version file.");
                }
            }

            //Reopen CW
            Logger.Log("Starting main application.");
            string? executable = GetCrusaderWarsExecutable(); // Changed to nullable
            if (executable != null)
            {
                Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            }
            else
            {
                Logger.Log("No executable found to start.");
            }

            //Close Updater
            Logger.Log("Closing updater.");
            Environment.Exit(0);
        }

        //
        //  UI CLIENT MOVEMENT
        //
        Point mouseOffset;
        private void AutoUpdater_MouseDown(object sender, MouseEventArgs e)
        {
            mouseOffset = new Point(-e.X, -e.Y);
        }

        private void AutoUpdater_MouseMove(object sender, MouseEventArgs e)
        {
            // Move the form when the left mouse button is down
            if (e.Button == MouseButtons.Left)
            {
                Point mousePos = Control.MousePosition;
                mousePos.Offset(mouseOffset.X, mouseOffset.Y);
                Location = mousePos;
            }
        }
    }
}
