#!/bin/bash
# This script automates the setup and launch of Crusader Conflicts on Linux.
# It checks for dependencies, sets up the Wine environment, and then runs the application.

# --- Configuration ---
export WINEPREFIX=~/.crusader-conflicts-net-pfx
EXECUTABLE_NAME="CrusaderConflicts.exe"
WINETRICKS_URL="https://raw.githubusercontent.com/Winetricks/winetricks/master/src/winetricks"
LOCAL_WINETRICKS_PATH="./winetricks"

# --- Functions ---
ensure_winetricks() {
    echo "Checking winetricks..."
    # Check if system winetricks supports dotnet472
    if command -v winetricks &> /dev/null && winetricks list-all | grep -q "dotnet472"; then
        echo "System winetricks is up-to-date."
        WINETRICKS_CMD="winetricks"
    else
        echo "System winetricks is missing or outdated. Downloading the latest version."
        if ! wget -O "$LOCAL_WINETRICKS_PATH" "$WINETRICKS_URL"; then
            echo "Error: Failed to download winetricks. Please check your internet connection."
            exit 1
        fi
        chmod +x "$LOCAL_WINETRICKS_PATH"
        WINETRICKS_CMD="$LOCAL_WINETRICKS_PATH"
    fi
}

ensure_wine_prefix() {
    echo "Checking Wine prefix at $WINEPREFIX..."
    if [ ! -d "$WINEPREFIX" ]; then
        echo "Wine prefix not found. Creating a new one..."
        # wineboot is less interactive than winecfg
        wineboot -u
        echo "Wine prefix created."
    else
        echo "Wine prefix already exists."
    fi
}

ensure_dotnet() {
    echo "Checking for .NET 4.7.2 installation..."
    # Check if dotnet472 is already listed as installed
    if $WINETRICKS_CMD list-installed | grep -q "dotnet472"; then
        echo ".NET 4.7.2 is already installed."
    else
        echo ".NET 4.7.2 not found. Installing now... (This may take several minutes)"
        # Use -q for an unattended installation
        if ! $WINETRICKS_CMD -q dotnet472; then
            echo "Error: Failed to install .NET 4.7.2. Please see the output above for details."
            exit 1
        fi
        echo ".NET 4.7.2 installation complete."
    fi
}


# --- Main Execution ---
echo "Starting Crusader Conflicts setup and launch..."

# Change to the script's directory to ensure relative paths are correct
cd "$(dirname "$0")" || { echo "Error: Could not change to script directory."; exit 1; }
echo "Running from: $(pwd)"

# Check for main executable
if [ ! -f "$EXECUTABLE_NAME" ]; then
    echo "Error: $EXECUTABLE_NAME not found in the current directory."
    exit 1
fi

# Step 1: Ensure we have a working winetricks
ensure_winetricks

# Step 2: Ensure the Wine prefix exists
ensure_wine_prefix

# Step 3: Ensure .NET 4.7.2 is installed
ensure_dotnet

# Step 4: Launch the application
echo "Setup complete. Launching Crusader Conflicts..."
wine "$EXECUTABLE_NAME"

echo "Application exited. Script finished."
