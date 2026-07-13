using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace CrusaderWars.process;

/// <summary>
/// Linux/Proton implementation of IProcessController using POSIX signals (kill -STOP / kill -CONT).
/// Finds the target process via pgrep and sends signals to suspend/resume it.
/// Works under both native Linux and Steam Proton environments.
/// </summary>
public class LinuxProcessController : IProcessController
{
    private readonly bool _isSupported;
    
    /// <summary>
    /// Initializes the controller and checks whether kill and pgrep commands are available.
    /// </summary>
    public LinuxProcessController()
    {
        _isSupported = CheckCommandsAvailable();
        if (!_isSupported)
        {
            Program.Logger.Debug("LinuxProcessController: kill and/or pgrep commands not available. Process suspend/resume will not be supported.");
        }
    }
    
    /// <summary>
    /// Returns true if both kill and pgrep commands are available on the system.
    /// </summary>
    public bool IsSupported => _isSupported;
    
    /// <summary>
    /// Verifies that kill and pgrep commands are available by attempting to execute them.
    /// </summary>
    private static bool CheckCommandsAvailable()
    {
        try
        {
            // Check pgrep availability
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "pgrep";
                proc.StartInfo.Arguments = "-x init";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit(3000);
                // pgrep returns 0 if match found, 1 if no match — both mean the command exists
                if (proc.ExitCode != 0 && proc.ExitCode != 1)
                {
                    Program.Logger.Debug($"LinuxProcessController: pgrep check failed with exit code {proc.ExitCode}.");
                    return false;
                }
            }
            
            // Check kill availability (signal 0 = dry run, doesn't actually send a signal)
            using (var proc = new Process())
            {
                proc.StartInfo.FileName = "kill";
                proc.StartInfo.Arguments = "-0 1";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                proc.WaitForExit(3000);
                if (proc.ExitCode != 0)
                {
                    Program.Logger.Debug($"LinuxProcessController: kill check failed with exit code {proc.ExitCode}.");
                    return false;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Program.Logger.Debug($"LinuxProcessController: Command availability check failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Finds the PID of a process by name using pgrep with a fallback chain:
    /// 1. pgrep -f &lt;processName&gt; (full command-line match)
    /// 2. pgrep &lt;processName&gt; (process name match)
    /// 3. System.Diagnostics.Process.GetProcessesByName (managed fallback)
    /// If multiple PIDs are found, the lowest PID (oldest process) is selected.
    /// </summary>
    /// <param name="processName">The process name to search for (e.g., "ck3.exe").</param>
    /// <returns>The PID of the found process.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the process cannot be found.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if pgrep is not available.</exception>
    private int FindPid(string processName)
    {
        if (!_isSupported)
        {
            throw new PlatformNotSupportedException(
                "Process suspend/resume is not supported on this system. " +
                "The 'kill' and 'pgrep' commands are required but not available.");
        }
        
        int? pid = null;
        
        // Strategy 1: pgrep -f (full command-line match — best for Proton where ck3.exe is in the wine command line)
        try
        {
            pid = TryPgrep($"-f {processName}");
            if (pid.HasValue)
            {
                Program.Logger.Debug($"LinuxProcessController: Found PID {pid.Value} for '{processName}' via pgrep -f.");
                return pid.Value;
            }
        }
        catch (Exception ex)
        {
            Program.Logger.Debug($"LinuxProcessController: pgrep -f failed: {ex.Message}. Trying pgrep without -f.");
        }
        
        // Strategy 2: pgrep (process name match only)
        try
        {
            pid = TryPgrep(processName);
            if (pid.HasValue)
            {
                Program.Logger.Debug($"LinuxProcessController: Found PID {pid.Value} for '{processName}' via pgrep.");
                return pid.Value;
            }
        }
        catch (Exception ex)
        {
            Program.Logger.Debug($"LinuxProcessController: pgrep failed: {ex.Message}. Trying managed fallback.");
        }
        
        // Strategy 3: Managed fallback using System.Diagnostics.Process
        try
        {
            // Strip .exe extension for Process.GetProcessesByName compatibility
            string nameWithoutExt = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName.Substring(0, processName.Length - 4)
                : processName;
            
            var processes = Process.GetProcessesByName(nameWithoutExt);
            if (processes.Length > 0)
            {
                // Select lowest PID (oldest process)
                int lowestPid = processes.Min(p => p.Id);
                Program.Logger.Debug($"LinuxProcessController: Found PID {lowestPid} for '{processName}' via managed fallback (found {processes.Length} process(es)).");
                
                if (processes.Length > 1)
                {
                    Program.Logger.Debug($"LinuxProcessController: Warning — multiple '{processName}' processes found. Targeting lowest PID {lowestPid}.");
                }
                
                return lowestPid;
            }
        }
        catch (Exception ex)
        {
            Program.Logger.Debug($"LinuxProcessController: Managed fallback failed: {ex.Message}.");
        }
        
        throw new InvalidOperationException($"Process '{processName}' not found. Ensure the process is running before attempting to suspend/resume it.");
    }
    
    /// <summary>
    /// Executes pgrep with the given arguments and returns the first PID found, or null if none.
    /// </summary>
    private static int? TryPgrep(string arguments)
    {
        using (var proc = new Process())
        {
            proc.StartInfo.FileName = "pgrep";
            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(3000);
            
            if (proc.ExitCode != 0 || string.IsNullOrEmpty(output))
                return null;
            
            // pgrep may return multiple PIDs, one per line. Take the first (lowest PID).
            string firstLine = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (int.TryParse(firstLine, out int pid))
                return pid;
            
            return null;
        }
    }
    
    /// <summary>
    /// Sends a signal to a process by PID.
    /// </summary>
    /// <param name="pid">The process ID to signal.</param>
    /// <param name="signal">The signal to send (e.g., "STOP" or "CONT").</param>
    /// <exception cref="InvalidOperationException">Thrown if the signal fails.</exception>
    private static void SendSignal(int pid, string signal)
    {
        using (var proc = new Process())
        {
            proc.StartInfo.FileName = "kill";
            proc.StartInfo.Arguments = $"-{signal} {pid}";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(3000);
            
            if (proc.ExitCode != 0)
            {
                string errorDetail = !string.IsNullOrEmpty(stderr) ? stderr.Trim() : "Unknown error";
                throw new InvalidOperationException(
                    $"Failed to send SIG{signal} to PID {pid}. Exit code: {proc.ExitCode}. Error: {errorDetail}");
            }
        }
    }
    
    /// <inheritdoc/>
    public void SuspendProcess(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            throw new ArgumentException("Process name cannot be null or empty.", nameof(processName));
        
        if (!_isSupported)
        {
            throw new PlatformNotSupportedException(
                "Process suspend is not supported on this system. " +
                "The 'kill' and 'pgrep' commands are required but not available.");
        }
        
        Program.Logger.Debug($"Suspending {processName} via kill -STOP.");
        
        try
        {
            int pid = FindPid(processName);
            SendSignal(pid, "STOP");
            Program.Logger.Debug($"Successfully suspended {processName} (PID {pid}).");
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                $"Permission denied while trying to suspend '{processName}'. " +
                "Ensure you have permission to signal the process. Error: {ex.Message}", ex);
        }
    }
    
    /// <inheritdoc/>
    public void ResumeProcess(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            throw new ArgumentException("Process name cannot be null or empty.", nameof(processName));
        
        if (!_isSupported)
        {
            throw new PlatformNotSupportedException(
                "Process resume is not supported on this system. " +
                "The 'kill' and 'pgrep' commands are required but not available.");
        }
        
        Program.Logger.Debug($"Resuming {processName} via kill -CONT.");
        
        try
        {
            int pid = FindPid(processName);
            SendSignal(pid, "CONT");
            Program.Logger.Debug($"Successfully resumed {processName} (PID {pid}).");
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                $"Permission denied while trying to resume '{processName}'. " +
                "Ensure you have permission to signal the process. Error: {ex.Message}", ex);
        }
    }
}