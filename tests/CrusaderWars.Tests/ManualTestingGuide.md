# CC-282: Manual Testing Guide for Fedora/Proton Users

## Overview

This guide is for **corvinoctis** (and any other Linux/Proton users) to manually verify that the CC-282 fix works correctly on a real Fedora/Proton system.

**Prerequisites**:
- Fedora Linux with Steam installed
- Crusader Kings 3 installed and running through Steam Proton
- Total War: Attila installed and running through Steam Proton
- Crusader Conflicts (CC) application built from the CC-282 branch

---

## Test 1: Environment Detection

### Objective
Verify that CC correctly detects the Proton environment.

### Steps

1. **Launch CC through Steam** (as a Non-Steam Game shortcut):
   - In Steam: Games → "Add a Non-Steam Game to My Library..."
   - Browse to the CC executable (`CrusaderWars.exe`)
   - **Important**: Do NOT enable Proton for this entry — CC runs natively through Wine/Proton

2. **Observe the Linux Setup Wizard**:
   - If this is the first launch, the Linux Setup Wizard should appear automatically
   - If the wizard does not appear, check `settings/Options.xml` for `LinuxSetupCompleted` — set it to `false` to force the wizard

3. **Check Detection Step (Step 1)**:
   - **Linux**: Should show "Detected (Proton)"
   - **Proton**: Should show "Detected" with the Proton prefix path (e.g., `/home/user/.steam/steam/steamapps/compatdata/1158310/pfx`)
   - **Wine**: Should show the Wine version (e.g., "wine-8.0")
   - **Desktop**: Should show your desktop environment (e.g., "GNOME", "KDE")
   - **Steam**: Should show your Steam path
   - **Attila**: Should show your Attila installation path

### Expected Results
- ✅ All detection fields show correct values
- ✅ No error messages
- ✅ "Environment detection complete" status message

### Failure Indicators
- ❌ "Linux: Not Detected (Windows mode)" — Proton env vars not set; ensure CC is launched through Steam
- ❌ "Error: Not running on Linux" — CC thinks it's on Windows
- ❌ "Error: Wine was not found and not running under Proton" — Wine not available

---

## Test 2: Wine Prefix Reuse

### Objective
Verify that CC reuses the existing Proton prefix instead of creating a new one.

### Steps

1. **Continue from Test 1** (or restart wizard)
2. **Observe Wine Prefix Step (Step 2)**:
   - Should show: "Using existing Proton prefix: `<path>`"
   - Should NOT show: "Creating Wine prefix at..."

3. **Verify the prefix path**:
   - The path shown should match your CK3 Proton prefix (typically under `steamapps/compatdata/1158310/pfx`)

### Expected Results
- ✅ "Proton prefix detected and will be used" status message
- ✅ No new prefix created (check `~/.crusader-conflicts-net-pfx` — should NOT exist)

### Failure Indicators
- ❌ "Proton prefix path not found" — the `STEAM_COMPAT_DATA_PATH` points to a non-existent directory
- ❌ "Creating Wine prefix at..." — CC is creating a new prefix instead of reusing Proton's

---

## Test 3: .NET Pre-Check

### Objective
Verify that CC detects existing .NET installation and skips the install step.

### Steps

1. **Continue from Test 2** (or restart wizard)
2. **Observe .NET Install Step (Step 3)**:
   - Should show: "Checking if .NET is already installed..."
   - If .NET is already installed: Should show ".NET Framework is already installed in the prefix. Skipping installation."
   - If .NET is NOT installed: Should show ".NET not detected. Installing .NET 4.7.2 via winetricks..."

### Expected Results
- ✅ If you previously ran `winetricks dotnet9`: .NET should be detected and installation skipped
- ✅ If .NET is not installed: winetricks should run and install .NET 4.7.2

### Failure Indicators
- ❌ .NET installation fails — check logs for winetricks errors
- ❌ .NET pre-check hangs — check logs for wine command errors

---

## Test 4: Mod Symlinking

### Objective
Verify that mod symlinks are created automatically.

### Steps

1. **Continue from Test 3** (or restart wizard)
2. **Observe Mod Symlink Step (Step 4)**:
   - Should show: "Creating symlinks from `<workshop_path>` to `<attila_data_path>`..."
   - Should show: "Created X mod symlinks"

3. **Verify symlinks exist**:
   ```bash
   ls -la "<attila_data_path>" | grep "^l"
   ```
   - Should see symlinks pointing to workshop .pack files

### Expected Results
- ✅ Symlinks created without errors
- ✅ No manual `ln -s` commands needed

### Failure Indicators
- ❌ "Skipping: Could not find Attila's Steam workshop or data paths" — Steam/Attila paths not detected
- ❌ Symlink creation fails — check permissions on workshop and Attila directories

---

## Test 5: Battle Suspend/Resume Flow

### Objective
Verify that the battle suspend/resume flow works correctly under Proton without crashing.

### Steps

1. **Complete the setup wizard** (or skip if already completed)

2. **Launch CK3** through Steam (normally, with Proton)

3. **Start a battle in CC**:
   - In CC, select a save game
   - Click "Start Battle" or equivalent
   - CC should process the save and launch Attila

4. **Observe CK3 during battle processing**:
   - CK3 should be suspended (frozen) while CC processes the battle
   - Check CC logs for: "Suspending ck3.exe via kill -STOP"
   - Check CC logs for: "Successfully suspended ck3.exe (PID XXXX)"

5. **Resume the battle**:
   - In CC, click "Continue Battle" or "Resume Battle"
   - CK3 should be resumed (unfrozen)
   - Check CC logs for: "Resuming ck3.exe via kill -CONT"
   - Check CC logs for: "Successfully resumed ck3.exe (PID XXXX)"

6. **Verify CK3 is responsive after resume**:
   - Switch to CK3 window
   - Verify the game is running normally (not frozen)
   - Verify no crash occurred

### Expected Results
- ✅ CK3 suspends during battle processing (game freezes)
- ✅ CK3 resumes after battle (game unfreezes)
- ✅ No crash when clicking "Continue Battle"
- ✅ Logs show successful kill -STOP and kill -CONT operations

### Failure Indicators
- ❌ CK3 crashes when clicking "Continue Battle" — the original bug
- ❌ Logs show "ProcessCommands.ResumeProcess: Controller does not support resume on this platform. Skipping." — controller not initialized correctly
- ❌ Logs show "Process 'ck3.exe' not found" — pgrep couldn't find the CK3 process
- ❌ Logs show "Permission denied while trying to resume 'ck3.exe'" — user doesn't own the CK3 process

### Debugging Commands

If the battle resume fails, run these commands in a terminal to verify process detection:

```bash
# Check if CK3 is running
pgrep -f ck3.exe

# Check if you can signal the process
kill -0 $(pgrep -f ck3.exe | head -1)

# Check Proton environment variables
echo "STEAM_COMPAT_CLIENT_INSTALL_PATH: $STEAM_COMPAT_CLIENT_INSTALL_PATH"
echo "STEAM_COMPAT_DATA_PATH: $STEAM_COMPAT_DATA_PATH"

# Check CC logs
cat "<cc_install_dir>/logs/debug.log" | grep -i "linuxprocesscontroller"
```

---

## Test 6: Full End-to-End Flow

### Objective
Verify the complete user experience from setup to battle completion.

### Steps

1. **Fresh start** (if possible):
   - Delete or rename `settings/Options.xml` to reset configuration
   - Launch CC through Steam

2. **Complete the wizard**:
   - Verify all steps complete successfully
   - Verify no manual intervention needed

3. **Run a full battle cycle**:
   - Start a battle
   - Verify CK3 suspends
   - Complete the battle in Attila
   - Resume CK3
   - Verify CK3 is responsive

4. **Repeat battle cycle 3 times**:
   - Verify consistent behavior
   - Verify no crashes
   - Verify no performance degradation

### Expected Results
- ✅ Wizard completes without errors
- ✅ All 3 battle cycles complete successfully
- ✅ No crashes, no manual steps needed

---

## Reporting Results

Please report your test results with the following information:

1. **System Information**:
   - Fedora version
   - Desktop environment
   - Steam version
   - Proton version used for CK3

2. **Test Results**:
   - For each test (1-6): ✅ Pass or ❌ Fail
   - For failures: describe what happened and attach relevant logs

3. **Logs to attach**:
   - CC debug log: `<cc_install_dir>/logs/debug.log`
   - Any error messages shown in the wizard
   - Terminal output from debugging commands (if applicable)

---

## Quick Reference: Expected Log Messages

### Successful Proton Detection
```
LinuxProcessController: Found PID 12345 for 'ck3.exe' via pgrep -f.
```

### Successful Suspend
```
Suspending ck3.exe via kill -STOP.
Successfully suspended ck3.exe (PID 12345).
```

### Successful Resume
```
Resuming ck3.exe via kill -CONT.
Successfully resumed ck3.exe (PID 12345).
```

### Graceful Degradation (if kill/pgrep not available)
```
LinuxProcessController: kill and/or pgrep commands not available. Process suspend/resume will not be supported.
ProcessCommands.SuspendProcess: Controller does not support suspend on this platform. Skipping.
```

### Process Not Found
```
LinuxProcessController: pgrep -f failed: <error>. Trying pgrep without -f.
LinuxProcessController: pgrep failed: <error>. Trying managed fallback.
LinuxProcessController: Managed fallback failed: <error>.
Process 'ck3.exe' not found. Ensure the process is running before attempting to suspend/resume it.
```