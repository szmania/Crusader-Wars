namespace CrusaderWars.process;

/// <summary>
/// Cross-platform interface for suspending and resuming processes.
/// Provides platform-specific implementations for Windows (pssuspend64.exe)
/// and Linux/Proton (kill -STOP / kill -CONT via pgrep).
/// </summary>
public interface IProcessController
{
    /// <summary>
    /// Suspends (pauses) the specified process by name.
    /// On Windows: uses pssuspend64.exe.
    /// On Linux: uses kill -STOP after finding the PID via pgrep.
    /// </summary>
    /// <param name="processName">The name of the process to suspend (e.g., "ck3.exe").</param>
    /// <exception cref="ArgumentException">Thrown if processName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the process cannot be found or suspended.</exception>
    void SuspendProcess(string processName);
    
    /// <summary>
    /// Resumes a previously suspended process by name.
    /// On Windows: uses pssuspend64.exe /r.
    /// On Linux: uses kill -CONT after finding the PID via pgrep.
    /// </summary>
    /// <param name="processName">The name of the process to resume (e.g., "ck3.exe").</param>
    /// <exception cref="ArgumentException">Thrown if processName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the process cannot be found or resumed.</exception>
    void ResumeProcess(string processName);
    
    /// <summary>
    /// Returns true if this controller's mechanism is supported on the current platform.
    /// When false, the application should fall back to alternative behavior
    /// (e.g., closing CK3 instead of suspending it).
    /// </summary>
    bool IsSupported { get; }
}