using System;
using System.Diagnostics;
using System.IO;

namespace CrusaderWars.process;

/// <summary>
/// Windows implementation of IProcessController using pssuspend64.exe (Sysinternals).
/// Preserves the exact existing behavior from the ProcessCommands struct in MainFile.cs.
/// </summary>
public class WindowsProcessController : IProcessController
{
    /// <summary>
    /// Always true on Windows — pssuspend64.exe is bundled with the application.
    /// </summary>
    public bool IsSupported => true;

    /// <summary>
    /// Executes pssuspend64.exe with the given command-line arguments.
    /// </summary>
    /// <param name="command">The command to pass to pssuspend64.exe (e.g., "ck3.exe" or "/r ck3.exe").</param>
    /// <returns>The standard output from the command.</returns>
    /// <exception cref="FileNotFoundException">Thrown if pssuspend64.exe is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the command fails (non-zero exit code).</exception>
    private static string ProcessRuntime(string command)
    {
        // Locate pssuspend64.exe in the data/runtime directory
        string[] files = Directory.GetFiles(@".\data\runtime", "pssuspend64.exe", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            throw new FileNotFoundException(
                "pssuspend64.exe not found in .\\data\\runtime. " +
                "This tool is required for process suspend/resume on Windows.");
        }

        string filePath = files[0];

        // Automatically accept the EULA on first run
        string finalCommand = $"-accepteula {command}";

        ProcessStartInfo procStartInfo = new ProcessStartInfo(filePath, finalCommand)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process proc = new Process())
        {
            proc.StartInfo = procStartInfo;
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"pssuspend64.exe failed with exit code {proc.ExitCode}. Output: {output}");
            }

            return output;
        }
    }

    /// <inheritdoc/>
    public void SuspendProcess(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            throw new ArgumentException("Process name cannot be null or empty.", nameof(processName));

        Program.Logger.Debug($"Suspending {processName} via pssuspend64.exe.");
        ProcessRuntime(processName);
        Program.Logger.Debug($"Successfully suspended {processName}.");
    }

    /// <inheritdoc/>
    public void ResumeProcess(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            throw new ArgumentException("Process name cannot be null or empty.", nameof(processName));

        Program.Logger.Debug($"Attempting to resume {processName} via pssuspend64.exe (will be ignored if not suspended).");
        try
        {
            ProcessRuntime($"/r {processName}");
            Program.Logger.Debug($"Successfully resumed {processName}.");
        }
        catch (InvalidOperationException ex)
        {
            // This error is expected if the process is not suspended.
            // We log it for debugging but don't throw, allowing the program to continue.
            Program.Logger.Debug($"Could not resume '{processName}', it was likely not suspended. Error: {ex.Message}");
        }
    }
}
