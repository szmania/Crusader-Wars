using System;
using System.IO;

namespace CWUpdater
{
    public static class Logger
    {
        private static readonly string logFilePath = "debug_cwupdater.log";

        public static void Log(string message)
        {
            try
            {
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
