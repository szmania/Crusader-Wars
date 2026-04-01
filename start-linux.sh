#!/bin/bash
# This script provides an easy way to launch Crusader Conflicts on Linux using Wine.
# It ensures that the application is run from the correct directory.

# Change to the script's directory to ensure paths are correct
cd "$(dirname "$0")"

# Launch the application using Wine
wine "CrusaderConflicts.exe"
