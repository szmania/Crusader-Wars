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

namespace CWUpdater
{
    public partial class AutoUpdater : Form
    {
        private string DownloadUrl { get; set; }
        private string UpdateVersion { get; set; }
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

        private string GetCrusaderWarsExecutable()
        {
            string currentDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName;
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
                await DownloadUpdateAsync(DownloadUrl);
            }
            catch (Exception ex)
            {
                Logger.Log($"An error occurred in btnUpdate_Click: {ex.ToString()}");
                string executable = GetCrusaderWarsExecutable();
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

                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadProgressChanged += (sender, e) =>
                    {
                        double progress = e.ProgressPercentage;
                        label1.Text = progress.ToString() + "%";
                    };
                    // Asynchronously download the file
                    await webClient.DownloadFileTaskAsync(new Uri(downloadUrl), downloadPath);
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
                    
                };

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
                        //string parentFolder = relativePath.Split('\\')[0];
                        //relativePath = relativePath.Replace($"{parentFolder}\\", "");
                        string destinationPath = Path.Combine(applicationPath, relativePath);

                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        File.Copy(file, destinationPath, true);
                    }
                    else if(!IsUnitMappers) //<-- APP UPDATER
                    {
                        //Skip essential files
                        if (Path.GetFileName(file) == "CWUpdater.exe" ||
                            Path.GetFileName(file) == "CWUpdater.exe.config" ||
                            Path.GetFileName(file) == "Paths.xml" ||
                            Path.GetFileName(file) == "active_mods.txt")
                            continue;
                        //Skip essential directories
                        if (Path.GetDirectoryName(file) == "updater")
                            continue;

                        string relativePath = file.Substring(tempDirectory.Length + 1);
                        string parentFolder = relativePath.Split('\\')[0];
                        relativePath = relativePath.Replace($"{parentFolder}\\", "");
                        string destinationPath = Path.Combine(applicationPath, relativePath);

                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        File.Copy(file, destinationPath, true);
                    }
 
                }

                // Step 5: Clean up backup if update was successful
                Logger.Log("Update successful, deleting backup.");
                Directory.Delete(backupPath, true);

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
            // Delete obsolete files
            if(IsUnitMappers)
            {
                var existingFiles = Directory.GetFiles(applicationPath, "*", SearchOption.AllDirectories);
                var newFiles = Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in existingFiles)
                {
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
                    string relativeDirPath = dir.Substring(applicationPath.Length + 1);
                    string parentFolder = relativeDirPath.Split('\\')[0];
                    relativeDirPath = relativeDirPath.Replace($"{parentFolder}\\", "");

                    if (!newDirs.Contains(relativeDirPath) && !Directory.GetFiles(dir).Any() && !Directory.GetDirectories(dir).Any())
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
            else if (!IsUnitMappers)
            {
                var existingFiles = Directory.GetFiles(applicationPath, "*", SearchOption.AllDirectories);
                var newFiles = Directory.GetFiles(tempDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in existingFiles)
                {
                    //Skip essential files
                    if (Path.GetFileName(file) == "CWUpdater.exe" ||
                        Path.GetFileName(file) == "CWUpdater.exe.config" ||
                        Path.GetFileName(file) == "Paths.xml" ||
                        Path.GetFileName(file) == "active_mods.txt")
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
                    //Skip essential directories
                    if (Path.GetDirectoryName(dir) == "updater")
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
            if (Directory.Exists(applicationPath))
            {
                Directory.Delete(applicationPath, true);  // Clean the application directory
            }

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
                File.WriteAllText(version_path, $"version=\"{UpdateVersion}\"");
            }
            else if(IsUnitMappers)
            {
                //Update unit mappers .txt file version
                string version_path = Directory.GetCurrentDirectory() + "\\um_version.txt";
                Logger.Log($"Updating unit mappers version file: {version_path} to version {UpdateVersion}");
                File.WriteAllText(version_path, $"version=\"{UpdateVersion}\"");
            }

            //Reopen CW
            Logger.Log("Starting main application.");
            string executable = GetCrusaderWarsExecutable();
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
