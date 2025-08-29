using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrusaderWars
{
    internal static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
       {
            Logger.Debug("Application starting...");
            Logger.Debug(AppDomain.CurrentDomain.BaseDirectory);
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Subscribe to global exception events
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            
            Application.Run(new HomePage());
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            // Handle the exception
            Logger.Debug($"Global thread exception caught: {e.Exception.Message}");
            MessageBox.Show("An unexpected error occurred. Please try again.", "Crusader Wars: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Log the exception for troubleshooting
            Logger.Log(e.Exception);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Handle the exception
            Logger.Debug($"Unhandled domain exception caught: {((Exception)e.ExceptionObject).Message}");
            MessageBox.Show("An unexpected error occurred. Please try again.", "Crusader Wars: Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Log the exception for troubleshooting
            Logger.Log((Exception)e.ExceptionObject);
        }

        public static class Logger
        {
            private static string logFilePath = @".\data\error.log"; // Path to the log file
            private static string debugLogFilePath = @".\data\debug.log";

            static Logger()
            {
                try
                {
                    // Ensure the directory exists.
                    string logDirectory = Path.GetDirectoryName(debugLogFilePath);
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    // Clear the log file by creating it (or overwriting it if it exists).
                    File.WriteAllText(debugLogFilePath, string.Empty);
                }
                catch (Exception ex)
                {
                    // If logging can't be initialized, write to console.
                    // This is a fallback for critical startup errors.
                    Console.WriteLine($"FATAL: Could not initialize logger. {ex.Message}");
                }
            }

            public static void Debug(string message)
            {
                try
                {
                    string logMessage = $"[{DateTime.Now}] DEBUG: {message}";
                    Console.WriteLine(logMessage);
                    using (StreamWriter writer = new StreamWriter(debugLogFilePath, true))
                    {
                        writer.WriteLine(logMessage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FATAL: Could not write to debug log. {ex.Message}");
                }
            }

            public static void Log(Exception ex)
            {
                try
                {
                    // Create or append to the log file
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        writer.WriteLine($"[{DateTime.Now}] {ex.GetType()}: {ex.Message}");
                        writer.WriteLine($"StackTrace: {ex.StackTrace}");
                        writer.WriteLine(); // Empty line for better readability
                    }
                }
                catch (Exception logEx)
                {
                     Console.WriteLine($"FATAL: Could not write to error log. {logEx.Message}");
                }
            }
        }

    }
}
