# Simple Clock Overlay for Windows
A simple floating digital clock for Windows 10 and Windows 11 (64-bit). Keep the time visible above other apps with an always-on-top desktop overlay, customizable text and background colors, transparency, and timezone support.

[Download the latest Windows release](https://github.com/SwiftDevDevelopment/ClockAPP/releases/latest) — extract the ZIP and run the EXE. No installation required.

## Get Started
1. Extract the ZIP into a folder of your choice.
2. Double-click SimpleClockOverlay.exe.
That's it. No installation or separate .NET download is needed.

## Move The Clock

Hold the left mouse button on the clock and drag it anywhere.
Your position is remembered the next time you launch it.

## Settings
Right-click the clock and choose Settings.
- Timezone: choose where the clock gets its local time.
  Default: Copenhagen. Daylight saving is handled automatically.
- Always on Top: keep the clock above normal application windows.
- Time format: choose 24-hour or 12-hour (AM/PM).
- Seconds: show or hide seconds.
- Font size: make the clock larger or smaller.
- Background (%): adjust background opacity. 0% hides the background.
- Text color: choose the clock text color.
- Background color: choose the clock background color.
- Reset all colors: restore white text and the original dark background.
Click Save to apply changes, or Cancel to discard them.
At 0% background opacity, click directly on the clock text to interact.

## Exit
Right-click the clock and choose Exit.

## Update
Exit the old clock before launching a new version.
Your saved settings are retained.

## Settings Location
%LOCALAPPDATA%\SimpleClockOverlay\settings.json

## Uninstall
Exit the clock and delete its application folder.
Optionally delete the SimpleClockOverlay folder in Local AppData.

## Notes
Some exclusive fullscreen games may cover desktop overlays.
The application is unsigned; Windows may show an unknown-publisher notice


## Auto Updates
While running, the clock checks SwiftDevDevelopment/ClockAPP on GitHub at most once every 48 hours. The schedule is remembered across restarts, and failed/offline attempts also wait 48 hours. No checks run while the app is closed.

Newer versions download automatically over HTTPS, with checksum verification, to %LOCALAPPDATA%\SimpleClockOverlay\Updates. When an update is ready, open Settings and click Update and restart. The clock saves your settings, closes, installs the downloaded version and restarts automatically. No file copying is needed. Keep the application in a folder you can write to, such as Downloads or a folder in your user profile. If installation fails, the old application is kept or restored, with a message explaining the problem.

Auto updates are enabled by default. Disable them in Settings and click Save to stop future checks. GitHub receives normal connection information, including your IP address. Clock settings are not uploaded. The clock continues to work offline.



[GitHub Privacy Statement — data collection and handling](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement)

