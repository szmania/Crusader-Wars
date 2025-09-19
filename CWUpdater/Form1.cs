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
        private string? CurrentVersion { get; set; } // Made nullable
        private bool IsUnitMappers { get; set; }

        public AutoUpdater()
        {
            Logger.Log("Initializing AutoUpdater form.");
            if(GetArguments())
            {
                // If CurrentVersion was not provided as an argument, try to read it from a local file
                if (string.IsNullOrEmpty(CurrentVersion))
                {
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", "");
                    string versionFileName = IsUnitMappers ? "um_version.txt" : "app_version.txt";
                    string versionFilePath = Path.Combine(currentDir, versionFileName);
                    CurrentVersion = ReadVersionFromFile(versionFilePath);
                    if (!string.IsNullOrEmpty(CurrentVersion))
                    {
                        Logger.Log($"Current version read from file: {CurrentVersion}");
                    }
                    else
                    {
                        Logger.Log($"Could not read current version from {versionFilePath}.");
                    }
                }

                InitializeComponent();
                this.TopMost = true;
                
                if (!string.IsNullOrEmpty(UpdateVersion))
                {
                    if (!string.IsNullOrEmpty(CurrentVersion))
                    {
                        VersionLabel.Text = $"v{CurrentVersion.Trim().TrimStart('v')} -> v{UpdateVersion.Trim().TrimStart('v')}";
                    }
                    else
                    {
                        VersionLabel.Text = $"Updating to v{UpdateVersion.Trim().TrimStart('v')}";
                    }
                }
                else
                {
                    VersionLabel.Visible = false;
                }

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

            if (args.Length == 3) // App update (older format: CWUpdater.exe <DownloadUrl> <NewVersion>)
            {
                DownloadUrl = args[1];
                UpdateVersion = args[2];
                CurrentVersion = null; // Current version not supplied in this format
                IsUnitMappers = false;
                Logger.Log($"App update (older format) detected. URL: {DownloadUrl}, New Version: {UpdateVersion}, Current Version: null");
                return true;
            }
            else if (args.Length == 4) // Could be App update: CWUpdater.exe <DownloadUrl> <CurrentVersion> <NewVersion> OR Unit Mappers update (older format): CWUpdater.exe <DownloadUrl> <NewVersion> <"unit_mapper">
            {
                if (args[3].Equals("unit_mapper", StringComparison.OrdinalIgnoreCase))
                {
                    DownloadUrl = args[1];
                    UpdateVersion = args[2];
                    CurrentVersion = null; // Current version not supplied in this older format
                    IsUnitMappers = true;
                    Logger.Log($"Unit Mappers update (older format) detected. URL: {DownloadUrl}, New Version: {UpdateVersion}, Current Version: null");
                    return true;
                }
                else
                {
                    DownloadUrl = args[1];
                    CurrentVersion = args[2]; // Swapped
                    UpdateVersion = args[3]; // Swapped
                    IsUnitMappers = false;
                    Logger.Log($"App update detected. URL: {DownloadUrl}, New Version: {UpdateVersion}, Current Version: {CurrentVersion}");
                    return true;
                }
            }
            else if (args.Length == 5) // Unit Mappers update: CWUpdater.exe <DownloadUrl> <NewVersion> <CurrentVersion> <"unit_mappers_flag">
            {
                DownloadUrl = args[1];
                UpdateVersion = args[2];
                CurrentVersion = args[3];
                IsUnitMappers = true;
                Logger.Log($"Unit Mappers update detected. URL: {DownloadUrl}, New Version: {UpdateVersion}, Current Version: {CurrentVersion}");
                return true;
            }
            Logger.Log("Invalid number of arguments.");
            return false;
        }

        private string? ReadVersionFromFile(string filePath)
        {
            Logger.Log($"Attempting to read version from file: {filePath}");
            try
            {
                if (File.Exists(filePath))
                {
                    string content = File.ReadAllText(filePath);
                    Match match = Regex.Match(content, @"version\s*=\s*""([^""]*)""");
                    if (match.Success)
                    {
                        Logger.Log($"Version '{match.Groups[1].Value}' found in file.");
                        return match.Groups[1].Value;
                    }
                    else
                    {
                        Logger.Log($"Version pattern not found in file: {filePath}");
                        return null;
                    }
                }
                else
                {
                    Logger.Log($"Version file not found: {filePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading version from file '{filePath}': {ex.Message}");
                return null;
            }
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

                // Delete the skipped version file if it exists
                string currentDir = AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", "");
                string skipFileName = IsUnitMappers ? "um_skipped_version.txt" : "app_skipped_version.txt";
                string skipFilePath = Path.Combine(currentDir, skipFileName);
                if (File.Exists(skipFilePath))
                {
                    try
                    {
                        File.Delete(skipFilePath);
                        Logger.Log($"Deleted skipped version file: {skipFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Could not delete skipped version file: {ex.Message}");
                    }
                }

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
                    await ApplyUpdate(downloadPath, AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", ""));
                }
                else
                {
                    Logger.Log("Applying Unit Mappers update.");
                    await ApplyUpdate(downloadPath, AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", @"\unit mappers"));
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

        private async Task RetryActionAsync(Action action, string actionName)
        {
            int maxRetries = 10;
            int delayMilliseconds = 1500;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    action();
                    Logger.Log($"Action '{actionName}' successful.");
                    return; // Success
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    Logger.Log($"Action '{actionName}' failed on attempt {i + 1} with error: {ex.Message}. Retrying in {delayMilliseconds}ms...");
                    await Task.Delay(delayMilliseconds);
                }
                // On the last attempt, the exception will be re-thrown and caught by the calling method.
            }
        }

        public async Task ApplyUpdate(string updateFilePath, string applicationPath)
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

                try // NEW: Specific try block for ZipException
                {
                    Logger.Log("Extracting update file.");
                    ZipFile.ExtractToDirectory(updateFilePath, tempDirectory);

                    // Safeguard: Check if the extracted directory is empty
                    if (!Directory.EnumerateFileSystemEntries(tempDirectory).Any())
                    {
                        throw new System.IO.InvalidDataException("The update archive is empty and cannot be applied.");
                    }

                    // --- START SELF-UPDATE LOGIC ---
                    // Only perform self-update check for the main application, not unit mappers
                    if (!IsUnitMappers)
                    {
                        string[] newUpdaters = Directory.GetFiles(tempDirectory, "CWUpdater.exe", SearchOption.AllDirectories);
                        if (newUpdaters.Length > 0)
                        {
                            string newUpdaterPath = newUpdaters[0];
                            string currentUpdaterPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                            if (File.Exists(newUpdaterPath) && IsNewerUpdater(newUpdaterPath, currentUpdaterPath))
                            {
                                Logger.Log("Newer updater found. Staging self-update.");
                                string tempUpdater = Path.Combine(Path.GetTempPath(), "CWUpdater_new.exe");
                                File.Copy(newUpdaterPath, tempUpdater, true);

                                // Reconstruct arguments, ensuring they are quoted
                                string originalArgs = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(a => $"\"{a}\""));

                                string batchScript = $@"
@echo off
echo Waiting for original updater to close...
timeout /t 2 /nobreak > NUL
echo Replacing updater...
move /Y ""{tempUpdater}"" ""{currentUpdaterPath}""
echo Relaunching updater to continue the update...
start """" ""{currentUpdaterPath}"" {originalArgs}
del ""%~f0""
";
                                string batchPath = Path.Combine(Path.GetTempPath(), "update_updater.bat");
                                File.WriteAllText(batchPath, batchScript);

                                Process.Start(new ProcessStartInfo(batchPath) { CreateNoWindow = true, UseShellExecute = false });
                                Logger.Log("Launched self-update script. Exiting current instance.");
                                Environment.Exit(0); // Exit to allow the batch file to replace this exe
                            }
                            else
                            {
                                Logger.Log("No new updater found or it's not newer. Proceeding with normal application update.");
                            }
                        }
                    }
                    // --- END SELF-UPDATE LOGIC ---


                    if (IsUnitMappers)
                    {
                        // For Unit Mappers, use a more robust rename-and-replace strategy with retries
                        string cleanApplicationPath = applicationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        string oldDirectory = cleanApplicationPath + "_old";
                        Logger.Log("Starting unit mapper update using rename-and-replace strategy.");

                        // 1. Clean up any leftover old directory from a previous failed update
                        if (Directory.Exists(oldDirectory))
                        {
                            Logger.Log($"Deleting leftover old directory: {oldDirectory}");
                            await RetryActionAsync(() => Directory.Delete(oldDirectory, true), "Delete leftover _old directory");
                        }

                        // 2. Rename current directory to _old
                        Logger.Log($"Renaming '{applicationPath}' to '{oldDirectory}'.");
                        await RetryActionAsync(() => Directory.Move(applicationPath, oldDirectory), "Rename current to _old");

                        // 3. Move the new directory into place
                        Logger.Log($"Moving '{tempDirectory}' to '{applicationPath}'.");
                        await RetryActionAsync(() => Directory.Move(tempDirectory, applicationPath), "Move new to current");

                        // 4. Delete the old directory
                        Logger.Log($"Update successful, deleting old directory: {oldDirectory}");
                        await RetryActionAsync(() => Directory.Delete(oldDirectory, true), "Delete _old directory");
                    }
                    else // Existing logic for App Updater
                    {
                        // Delete obsolete files and directories
                        Logger.Log("Deleting obsolete files and directories.");
                        DeleteObsoleteFilesAndDirectories(applicationPath, tempDirectory);

                        // Copy new and updated files
                        Logger.Log("Copying new and updated files.");
                        foreach (var file in Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories))
                        {
                            // This part remains unchanged for the app updater
                            string relativePath = file.Substring(tempDirectory.Length + 1);
                            string parentFolder = relativePath.Split('\\')[0];
                            string finalRelativePath = relativePath.Replace($"{parentFolder}\\", "");
                            string destinationPath = Path.Combine(applicationPath, finalRelativePath);

                            if (destinationPath.StartsWith(updaterDirInMainApp, StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.Log($"Skipping copy of updater file: {file} to {destinationPath}");
                                continue;
                            }

                            string unitMappersDir = Path.Combine(applicationPath, "unit mappers");
                            string settingsDir = Path.Combine(applicationPath, "settings");

                            if (destinationPath.StartsWith(unitMappersDir, StringComparison.OrdinalIgnoreCase) ||
                                destinationPath.StartsWith(settingsDir, StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.Log($"Skipping overwrite of file in protected directory: {finalRelativePath}");
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
                catch (System.IO.InvalidDataException ex) // Corrected exception type
                {
                    Logger.Log($"Error extracting update file (corrupt/incomplete ZIP or empty archive): {ex.ToString()}");
                    MessageBox.Show(
                        $"The downloaded update file is corrupt, incomplete, or empty. Please check your internet connection and try again.\n\nError: {ex.Message}",
                        "Crusader Conflicts: Update Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    if (File.Exists(updateFilePath))
                    {
                        try
                        {
                            File.Delete(updateFilePath);
                            Logger.Log($"Deleted corrupt update file: {updateFilePath}");
                        }
                        catch (Exception deleteEx)
                        {
                            Logger.Log($"Failed to delete corrupt update file '{updateFilePath}': {deleteEx.Message}");
                        }
                    }
                    RestartApplication(false); // Restart main app without updating version
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    Logger.Log($"Access Denied Error during update: {uaEx.ToString()}");
                    MessageBox.Show(
                        "The updater was blocked by your system.\n\n" +
                        "This is often caused by Antivirus software or Windows' 'Controlled Folder Access' feature.\n\n" +
                        "Please try the following:\n" +
                        "1. Run the main application as an Administrator.\n" +
                        "2. Add an exception for 'CrusaderWars.exe' and 'CWUpdater.exe' in your antivirus software.\n" +
                        "3. Temporarily disable 'Controlled Folder Access' in Windows Security settings.\n\n" +
                        $"Error details: {uaEx.Message}",
                        "Crusader Conflicts: Update Failed (Access Denied)",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    Logger.Log("Rolling back to backup due to UnauthorizedAccessException.");
                    RestoreBackup(backupPath, applicationPath);
                    this.Close();
                }
            }
            catch (IOException ioEx)
            {
                Logger.Log($"I/O Error during update after multiple retries: {ioEx.ToString()}");
                MessageBox.Show(
                    "The updater could not access a file or directory because it is locked by another process.\n\n" +
                    "This is often caused by Antivirus software or a cloud sync client (like Dropbox, OneDrive, or MEGA).\n\n" +
                    "Please try the following:\n" +
                    "1. Temporarily pause your cloud sync client.\n" +
                    "2. Add an exception for 'CrusaderWars.exe' and 'CWUpdater.exe' in your antivirus software.\n" +
                    "3. Close any other programs that might be accessing the application folder and try again.\n\n" +
                    $"Error details: {ioEx.Message}",
                    "Crusader Conflicts: Update Failed (File Locked)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Logger.Log("Rolling back to backup due to IOException.");
                RestoreBackup(backupPath, applicationPath);
                this.Close();
            }
            catch (Exception ex) // Existing general catch
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
                        // Explicitly check if the path is not a directory before attempting to delete as a file.
                        // This handles cases where Directory.GetFiles might unexpectedly return a directory-like path.
                        if (!Directory.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                }

                // Delete obsolete directories recursively
                var existingDirs = Directory.GetDirectories(applicationPath, "*", SearchOption.AllDirectories);
                var newDirs = Directory.GetDirectories(tempDirectory, "*", SearchOption.AllDirectories).Select(d => d.Substring(tempDirectory.Length + 1)).ToHashSet();

                foreach (var dir in existingDirs.OrderByDescending(d => d.Length))
                {
                    if (!Directory.Exists(dir)) continue; // In case a parent was already deleted

                    string relativeDirPath = dir.Substring(applicationPath.Length + 1);
                    if (!newDirs.Contains(relativeDirPath))
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            else if (!IsUnitMappers) // Application updater
            {
                string settingsDir = Path.Combine(applicationPath, "settings");
                string unitMappersDir = Path.Combine(applicationPath, "unit mappers");

                var existingFiles = Directory.GetFiles(applicationPath, "*", SearchOption.AllDirectories);
                var newFiles = Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in existingFiles)
                {
                    // Skip files within the updater, settings, and unit mappers directories to prevent accidental deletion
                    if (file.StartsWith(updaterDir, StringComparison.OrdinalIgnoreCase) ||
                        file.StartsWith(settingsDir, StringComparison.OrdinalIgnoreCase) ||
                        file.StartsWith(unitMappersDir, StringComparison.OrdinalIgnoreCase))
                        continue;

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
                    // Skip the updater, settings, and unit mappers directories
                    if (dir.StartsWith(updaterDir, StringComparison.OrdinalIgnoreCase) || updaterDir.StartsWith(dir, StringComparison.OrdinalIgnoreCase) ||
                        dir.StartsWith(settingsDir, StringComparison.OrdinalIgnoreCase) ||
                        dir.StartsWith(unitMappersDir, StringComparison.OrdinalIgnoreCase))
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

        public void RestartApplication(bool updateVersionFile = true)
        {
            Logger.Log("Restarting application.");

            if(!IsUnitMappers)
            {
                //Update application .txt file version
                if (updateVersionFile)
                {
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
            }
            else if(IsUnitMappers)
            {
                //Update unit mappers .txt file version
                if (updateVersionFile)
                {
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

        private void btnSkip_Click(object sender, EventArgs e)
        {
            Logger.Log("Update skipped by user.");
            if (!string.IsNullOrEmpty(UpdateVersion))
            {
                try
                {
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", "");
                    string skipFileName = IsUnitMappers ? "um_skipped_version.txt" : "app_skipped_version.txt";
                    string skipFilePath = Path.Combine(currentDir, skipFileName);
                    File.WriteAllText(skipFilePath, UpdateVersion);
                    Logger.Log($"Wrote skipped version {UpdateVersion} to {skipFilePath}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error writing skipped version file: {ex.Message}");
                }
            }
            RestartApplication(false);
        }

        private bool IsNewerUpdater(string newUpdaterPath, string currentUpdaterPath)
        {
            try
            {
                var newVersionInfo = FileVersionInfo.GetVersionInfo(newUpdaterPath);
                var currentVersionInfo = FileVersionInfo.GetVersionInfo(currentUpdaterPath);

                // FileVersion can be null or empty, handle this gracefully
                if (string.IsNullOrEmpty(newVersionInfo.FileVersion) || string.IsNullOrEmpty(currentVersionInfo.FileVersion))
                {
                    Logger.Log("Could not retrieve file version from one or both updaters. Cannot compare versions.");
                    return false;
                }

                var newVersion = new Version(newVersionInfo.FileVersion);
                var currentVersion = new Version(currentVersionInfo.FileVersion);
                
                Logger.Log($"Comparing updater versions. New: {newVersion}, Current: {currentVersion}");

                return newVersion > currentVersion;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error comparing updater versions: {ex.Message}");
                return false; // Fail safe, don't attempt self-update if comparison fails
            }
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
