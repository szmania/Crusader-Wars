#!/bin/bash
# This script provides an easy way to launch Crusader Conflicts on Linux using Wine.
# It ensures that the application is run from the correct directory.

# Check if Wine is installed
command -v wine &> /dev/null || {
    echo "Wine could not be found. Please install Wine to run this application."
    echo "You can try one of the following commands depending on your distribution:"
    echo "For Debian/Ubuntu: sudo apt update && sudo apt install wine"
    echo "For Fedora: sudo dnf install wine"
    echo "For Arch Linux: sudo pacman -S wine"
    exit 1
}

# Change to the script's directory to ensure paths are correct
cd "$(dirname "$0")"

# Launch the application using Wine
wine "CrusaderConflicts.exe"
