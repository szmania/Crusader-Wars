**This guide is mainly for players running the games through Steam and Proton for Gnome. For those running it through other platforms, the setup should be similar. Adapt the steps accordingly **

**When following instructions, change `user` to your username. Example `cd /home/user` -> `cd /home/johnny` **

**For simplicity I have created a `Games` folder in my home directory and a symlink within Games to the Steam Games folder**

**Wine mounts Z:\ to your linux root path / . So Z:\home\user is actually /home/user**

```sh
cd /home/user
mkdir Games
cd Games
ln -s /home/user/.steam/root/steamapps/common/ SteamGames
```

# Crusader Wars - First Time Setup for Linux

This wizard automates the complex setup process for running Crusader Conflicts on Linux.

## Step 1: Launch the Application

1.  **Make the script executable:**
    *   Right-click on `start-linux.sh`.
    *   Go to `Properties` -> `Permissions`.
    *   Check the box that says "Allow executing file as program".
2.  **Run the script:**
    *   Double-click `start-linux.sh` to launch Crusader Conflicts.

The application will detect you are on Linux and automatically start the **Linux Setup Wizard**.

## Step 2: Follow the Wizard

The wizard will guide you through the entire setup process, which includes:
-   Creating and configuring a dedicated Wine environment.
-   Installing the required .NET Framework.
-   Automatically creating shortcuts and symlinks for Total War: Attila mods.
-   Providing instructions for Steam configuration.

Once the wizard is complete, you can launch Crusader Conflicts at any time by double-clicking the `start-linux.sh` script.

---
# Manual Installation (Advanced Users)

If you prefer to set up the environment manually, follow the steps below.

# CK3

## Step 1 - Install and Subscribe to needed mods

## Step 2 - Run CK3 and Activate Mods/Playset

## Step 3 - Start a new game to make sure everything works and to generate debug.log

## Step 4 - Create a symilnk in your `Documents` folder to the Proton prefix

```sh
cd /home/user/Documents
ln -s "/home/user/.steam/steam/steamapps/compatdata/1158310/pfx/drive_c/users/steamuser/Documents/Paradox Interactive" "Paradox Interactive"
```

# Attila

## Step 1 - Install and Subscribe to needed mods

## Step 2 - Make necessary changes to preferences.script for a smoother experience. Below a few recommendations. Read performance guides

`gfx_video_memory -4000` - For the game to recognize 4GB of VRAM
`number_of_threads 8` - Instead of 8, put the number of **_PHYSICAL_** cores you have available

## Step 3 - Run Attila through Steam and make sure it runs and the mods are loaded.

**⚠️ WARNING: If you have Attila installed through Steam you need to also run it through Steam to load the DLCs**

# Crusader Wars

Prerequisites: Update your system through the package manager and make sure you have the latest wine version installed

## Step 1 - Create Wine Prefix

- Run the command below. This creates a prefix in your home folder - Make sure you have the latest wine - update through your package manager. We will be using .NET 4.7.2 since CW user some fancy classes that need .NET and don't work properly in Mono

```sh
# Create prefix
WINEPREFIX=~/.crusader-wars-net-pfx winecfg

# Remove Mono if it's installed
WINEPREFIX=~/.crusader-wars-net-pfx wine uninstaller

# Install .NET. It will take a while since it will install multiple prerequisites and previous versions
WINEPREFIX=~/.crusader-wars-net-pfx winetricks dotnet472
```

## Step 2 - Create Attila Mod symlinks

- Create symlinks in the Attila data folder to mods downloaded through workshop. Alternatively just copy the .pack files in the Attila data folder. You can do this manually or use the provided `generate-mod-symlinks.sh`

```sh
sh generate-mod-symlinks.sh "/home/user/.steam/root/steamapps/workshop/content/325610/" "/home/user/Games/SteamGames/Total War Attila/data/"
```

## Step 3 - Unpack and Run Crusader Wars

- Unpack Crusader Wars in /home/user/Games/crusader-wars and Run CW.

```sh
WINEPREFIX=~/.crusader-wars-net-pfx cd ~/Games/crusader-wars && wine Crusader\ Wars.exe
```

- Set paths to the games
  CK3 - Z:\home\user\Games\SteamGames\Crusader Kings III\binaries\ck3.exe
  Attila - Z:\home\user\Games\SteamGames\Total War Attila\Attila.exe

- Select and enable playthrough
- Tick mods that you want to use. Make sure you have the required ones according to your playthrough
- Click the Sword button in CW to launch CK3. Try to launch a battle in Attila by starting one in CK3 and clicking the "Fight the battle yourself!" button

**Make sure the shortcut file is created so CW does not overwrite it later. Path - /home/user/Games/crusader-wars/data/attila/Attila CW.lnk**

**At this stage, both CK3 and Attila run through the default wine prefix. CK3 seems to run well for me, but Attila needs Proton and other tweaks for better performance and compatibility. See below further steps for Attila.**

## Step 4 - Tweaks to launch Attila through Steam and Proton

- Copy `Attila-Launcher.bat` and `launch-attila.sh` to the Attila root folder (/home/user/Games/SteamGames/Total War Attila/)

- launch-attila.sh` is tweaked for gnome to launch the shortcut .desktop file created by Steam. For other desktop environments please adapt.

- Create a new shortcut file (.lnk) named Attila CW.lnk and copy it in /crusader-wars/data/attila/, overwritting the existing one.

```sh
sh create-lnk.sh "Z:\home\user\Games\SteamGames\Total War Attila\Attila-Launcher.bat" "Attila CW.lnk"
```

- Add Launch Options in Steam for Attila.
  %command% used_mods_cw.txt

- Additional options that work for me:
  PULSE_LATENCY_MSEC=60 PROTON_ENABLE_WAYLAND=1 PROTON_ENABLE_WAYLAND=1 mangohud gamemoderun %command% used_mods_cw.txt

PULSE_LATENCY_MSEC=60 - For pipewire audio crackling and popping
PROTON_ENABLE_WAYLAND=1 - To make the window run under Wayland, not XWayland. Mode stable and Alt-Tab and others work smoothly
DXVK_FRAME_RATE=30 - framerate limiting
mangohud - if you have mangohud installed to see FPS and other metrics overlay
gamemoderun - sets process priority and other games optimizations
