@echo Off
setlocal

:: This script should be placed in the same folder as your "Crusader Wars.sln" file.

:: 1. Set up the Visual Studio build environment.
::    NOTE: This path is hardcoded to your VS 2012 installation. 
::    Update it if you use a different version of Visual Studio.
call "D:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\Tools\VsDevCmd.bat"

:: 2. Check if the environment was set up correctly.
if %errorlevel% neq 0 (
    echo ERROR: Failed to initialize the Visual Studio environment.
    exit /b %errorlevel%
)

:: 3. Build the solution in Release mode.
::    This assumes the script is in the same directory as the .sln file.
msbuild "Crusader Wars.sln" /p:Configuration=Release

echo.
echo Build complete.
