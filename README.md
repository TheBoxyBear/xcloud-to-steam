# xCloud To Steam

**xCloud To Stream** is a cross-platform app for browsing the Xbox Cloud library and adding your favorite games to your Steam library as non-Steam games.

## Features
- Fetches the official catalog from the Xbox Cloud Gaming API
- Includes games across the **Game Pass** and **Stream your own game** catalog
- Fully customizable shortcuts with a template-based configuration
- Downloads and assigns official cover and hero artwork from Xbox
- Identifies existing shortcuts to maintain your controller settings, custom artwork, etc.
- Browse the catalog for your own region


## Building from source
1. Install the **dotnet sdk 10.0**
2. Install a C++ compiler such as *clang* or *gcc*
2. Navigate to *src/xcloud-to-steam.app*
3. Run `dotnet publish -c Release -o [output directory]`

> [!IMPORTANT]
> The project is configured to compile to a native app. This process requires the compilation be done on the same OS family and CPU architecture as the target device that will be running the app.

## Setup

> [!WARNING]
> As the app is still in development, it is highly recommendes you make a backup of your current Steam shortcuts file. It can be found under *[Steam]/userdata/[Account ID]/config/shortcuts.vdf*


### Windows
The app is ready to use out of the box. The default configuration will launch the official web app on the Edge browser in full screen kiosk mode.

## Linux
The default profile relies on the `browser` and `kiosk` shell scripts found in [this repository](https://github.com/theboxybear/deck-xcloud-scripts). Follow the setup guide, while making sure to place the scripts in your Home directory. This configuration has been primarily tested on StemOS and may not work on other distributions.

## Per-game configurations
The app currently doesn't support using different configuration profiles on a per-game basis. Selecting Apply will overwrite all existing and new shortcuts with the currently selected profile. You may however create a profile that routes shortcuts to a custom program/script to launch the shortcut differently based on the game.

Alternatively, you can create your own config profile for your preferred environment and add it to **config.json**

## MacOS
The app doesn't currently include a default config profile for macOS. You must create your own and add it to **config.json**

## Known bugs
- Random crashes when running for a few minutes. Apply your changes every now and then to keep your selwctions
- General instability when running on a weak Internet connection or during an Xbox service outage
- Crash when **config.json** is missing or cannot find a config profile for the host OS
- App fails to identify the device's country on SteamOS