#!/bin/bash
# This script provides an easy way to launch Crusader Conflicts on Linux using Wine.
# It ensures that the application is run from the correct directory.

# --- Debugging Start ---
echo "Script starting..."
echo "Current directory: $(pwd)"
echo "User: $(whoami)"
echo "PATH: $PATH"
# --- Debugging End ---

# Change to the script's directory to ensure paths are correct
cd "$(dirname "$0")" || exit 1

# Explicitly check for wine in the known location
WINE_EXECUTABLE="/usr/bin/wine"

if [ ! -x "$WINE_EXECUTABLE" ]; then
    echo "Wine executable not found or not executable at $WINE_EXECUTABLE."
    echo "Please ensure Wine is installed correctly."
    exit 1
fi

echo "Found Wine at: $WINE_EXECUTABLE"

# Check if the executable exists
EXECUTABLE_NAME="CrusaderConflicts.exe"
if [ ! -f "$EXECUTABLE_NAME" ]; then
    echo "Error: $EXECUTABLE_NAME not found in the current directory."
    echo "Please ensure you are running this script from the same directory as the application."
    exit 1
fi

echo "Found application: $EXECUTABLE_NAME"

# Launch the application using Wine
echo "Launching Crusader Conflicts with Wine... This may take a moment."
WINEPREFIX=~/.crusader-conflicts-net-pfx "$WINE_EXECUTABLE" "$EXECUTABLE_NAME"

echo "Script finished."
