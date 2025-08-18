using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Crusader_Wars
{
    internal static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
       {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            /*
            // Subscribe to global exception events
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            */
            Application.Run(new HomePage());
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            // Handle the exception
            MessageBox.Show("An unexpected error occurred. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Log the exception for troubleshooting
            Logger.Log(e.Exception);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Handle the exception
            MessageBox.Show("An unexpected error occurred. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Log the exception for troubleshooting
            Logger.Log((Exception)e.ExceptionObject);
        }

        public static class Logger
        {
            private static string logFilePath = @".\data\error.log"; // Path to the log file
            private static string debugLogFilePath = @".\data\debug.log";

            public static void Debug(string message)
            {
                string logMessage = $"[{DateTime.Now}] DEBUG: {message}";
                Console.WriteLine(logMessage);
                using (StreamWriter writer = new StreamWriter(debugLogFilePath, true))
                {
                    writer.WriteLine(logMessage);
                }
            }

            public static void Log(Exception ex)
            {
                // Create or append to the log file
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] {ex.GetType()}: {ex.Message}");
                    writer.WriteLine($"StackTrace: {ex.StackTrace}");
                    writer.WriteLine(); // Empty line for better readability
                }
            }
        }

    }
}
