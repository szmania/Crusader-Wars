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
                Environment.Exit(1);
            }
        }

        bool GetArguments()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length == 3) // Check if at least 2 arguments are present
            {
                DownloadUrl = args[1];
                UpdateVersion = args[2];
                IsUnitMappers = false;
                return true;
            }
            else if (args.Length == 4) // Check if at least 3 arguments are present
            {
                DownloadUrl = args[1];
                UpdateVersion = args[2];
                IsUnitMappers = true;
                return true;
            }
            return false;
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                btnUpdate.Enabled = false;
                btnUpdate.Text = "Updating..";
                await DownloadUpdateAsync(DownloadUrl);
            }
            catch 
            {
                Process.Start(@".\CrusaderWars.exe");
                Environment.Exit(1);
            }
            
        }

        public async Task DownloadUpdateAsync(string downloadUrl)
        {
 
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

                    if(!IsUnitMappers) {
                        ApplyUpdate(downloadPath, AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", ""));
                    }
                    else
                    {
                        ApplyUpdate(downloadPath, AppDomain.CurrentDomain.BaseDirectory.Replace(@"\data\updater", @"\unit mappers"));
                    }
                    
                };

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading update: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                this.Close();
            }
            
        }

        public void ApplyUpdate(string updateFilePath, string applicationPath)
        {
            label1.Text = "Applying update...";
            string backupPath = Path.Combine(Path.GetTempPath(), "app_backup");
            string tempDirectory = Path.Combine(Path.GetTempPath(), "update");
            try
            {
                // Step 1: Backup existing files
                BackupApplicationFiles(applicationPath, backupPath);

                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
                ZipFile.ExtractToDirectory(updateFilePath, tempDirectory);

                // Delete obsolete files and directories
                DeleteObsoleteFilesAndDirectories(applicationPath, tempDirectory);

                // Copy new and updated files
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
                Directory.Delete(backupPath, true);

                Console.WriteLine("Update applied successfully.");
                RestartApplication();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying update: {ex.Message}{ex.TargetSite}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                // Step 6: Rollback to the backup if an error occurs
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

        }


        //
        //  BACKUP FUNCTIONS
        //

        private void BackupApplicationFiles(string applicationPath, string backupPath)
        {
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
        }

        private void RestoreBackup(string backupPath, string applicationPath)
        {
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
        }

        public void RestartApplication()
        {

            if(!IsUnitMappers)
            {
                //Update application .txt file version
                string version_path = Directory.GetCurrentDirectory() + "\\app_version.txt";
                File.WriteAllText(version_path, $"version=\"{UpdateVersion}\"");
            }
            else if(IsUnitMappers)
            {
                //Update unit mappers .txt file version
                string version_path = Directory.GetCurrentDirectory() + "\\um_version.txt";
                File.WriteAllText(version_path, $"version=\"{UpdateVersion}\"");
            }

            //Reopen CW
            Process.Start(@".\CrusaderWars.exe");

            //Close Updater
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
