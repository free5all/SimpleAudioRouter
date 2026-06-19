# Simple Audio Router

Send the left and right channels of your system audio to two different speakers or headphones.

Windows plays everything through one virtual cable. Simple Audio Router splits that signal and sends the left channel to one output device and the right channel to another. Handy if you want, say, a game on one pair of cans and Discord on another.

## Requirements

- Windows 10 or 11 (64-bit)
- [Virtual Audio Cable](https://vac.muzychenko.net/en/download.htm) — the app walks you through this on first run
- Admin rights — needed to switch the default playback device to the virtual cable

## Install

Download the latest `SimpleAudioRouter-Setup-{version}.exe` from [GitHub Releases](https://github.com/free5all/SimpleAudioRouter/releases).

## Build from source

Needs [.NET 10 SDK](https://dotnet.microsoft.com/download) and Windows.

```powershell
dotnet build SimpleAudioRouter.sln -c Release
dotnet run --project src/SimpleAudioRouter/SimpleAudioRouter.csproj
```

For the proper exe icon and notification name, run the built exe directly:

```powershell
.\src\SimpleAudioRouter\bin\Release\net10.0-windows\SimpleAudioRouter.exe
```

### Installer

Also needs [Inno Setup 6](https://jrsoftware.org/isinfo.php):

```powershell
.\installer\build-installer.ps1
```

Output: `dist/SimpleAudioRouter-Setup-<version>.exe` (version comes from the csproj).

## Usage

1. Pick a **left** and **right** output device (they must be different).
2. Routing starts automatically once both are set and the virtual cable driver is installed.
3. Use the gain sliders to mix how much of each input channel goes to each output.
4. **Test** sends a short beep through the route so you can confirm a device works.

The app lives in the system tray when the window is closed (unless you turned that off in Settings).

### Settings

| Option | What it does |
|--------|----------------|
| Start with Windows | Adds a shortcut to your Startup folder |
| Start minimized to tray | Any launch opens straight to the tray |
| Minimize to tray when closing | Close button hides to tray instead of quitting |

## How it works

System audio → Virtual Audio Cable (default device) → Simple Audio Router captures it → left/right split → your two chosen outputs.

When you quit for real, the previous default playback device is restored.

## Releases

Pushes to `main` upload the installer as a **workflow artifact** (for testing).

To ship a public release:

1. Bump `Version` in `src/SimpleAudioRouter/SimpleAudioRouter.csproj` and merge to `main`.
2. Tag that commit and push:

```powershell
git tag v1.1.0
git push origin v1.1.0
```

CI builds the installer and publishes it to [GitHub Releases](https://github.com/free5all/SimpleAudioRouter/releases) automatically. The tag must match the csproj version (`v1.1.0` → `1.1.0`).

Downloads always come from **Releases**, not the Actions artifacts tab.
