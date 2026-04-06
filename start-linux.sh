#!/bin/bash
# This script provides an easy way to launch Crusader Conflicts on Linux using Wine.
# It ensures that the application is run from the correct directory.

# Change to the script's directory to ensure paths are correct
cd "$(dirname "$0")"

# Check if Wine is installed
if ! command -v wine &> /dev/null; then
    echo "Wine could not be found. Please install Wine to run this application."
    echo "You can try one of the following commands depending on your distribution:"
    echo "For Debian/Ubuntu: sudo apt update && sudo apt install wine"
    echo "For Fedora: sudo dnf install wine"
    echo "For Arch Linux: sudo pacman -S wine"
    exit 1
fi

# Check if the executable exists
if [ ! -f "CrusaderConflicts.exe" ]; then
    echo "Error: CrusaderConflicts.exe not found in the current directory."
    echo "Please ensure you are running this script from the same directory as the application."
    exit 1
fi

# Launch the application using Wine
echo "Launching Crusader Conflicts with Wine..."
WINEPREFIX=~/.crusader-conflicts-net-pfx wine "CrusaderConflicts.exe"
