using System;
using System.IO;

namespace CWUpdater
{
    public static class Logger
    {
        private static readonly string logFilePath = Path.GetFullPath(@".\data\debug_cwupdater.log");

        public static void Log(string message)
        {
            try
            {
                // Ensure the directory exists before writing the log file
                string logDirectory = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(logFilePath, logMessage);
            }
            catch (Exception ex)
            {
                // If logging fails, write to debug output as a fallback.
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}
