using System;
using System.IO;

namespace CrusaderWars.client
{
    public static class Program
    {
        public static Logger Logger { get; } = new Logger();

        [STAThread]
        static void Main()
        {
            // Example of how to use the logger
            Logger.Debug("Application started.");
            // Application.Run(new HomePage()); // Assuming HomePage is the main form
            // Logger.Debug("Application exited.");
        }
    }

    public class Logger
    {
        private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "debug.log");

        public Logger()
        {
            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
            // Clear log file on startup for fresh logs
            if (File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, string.Empty);
            }
        }

        public void Debug(string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [DEBUG] {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Fallback if logging fails (e.g., file locked)
                Console.WriteLine($"ERROR: Failed to write to log file: {ex.Message} - Original message: {message}");
            }
        }
    }
}
