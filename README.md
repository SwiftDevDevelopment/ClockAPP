# Simple Clock Overlay

A floating digital clock for Windows, with an experimental Linux and Mac edition. Drag it anywhere, keep it above other windows, and adjust the colors, size and transparency.

## Downloads

- [Windows — stable release](https://github.com/SwiftDevDevelopment/ClockAPP/releases/latest)
- [Windows, Linux and Mac — preview downloads](https://github.com/SwiftDevDevelopment/ClockAPP/releases/tag/v2.0.0-preview.2)

Download the ZIP for your computer, extract it, and open the app. No separate .NET installation is required. For Mac, choose **Apple Silicon** for M-series Macs or **Intel** for an Intel Mac.

## Quick links

[Preview manual and platform support](cross-platform/README.md) · [Performance measurements](cross-platform/PERFORMANCE.md) · [Contributing and build guide](cross-platform/CONTRIBUTING.md) · [Report a problem](https://github.com/SwiftDevDevelopment/ClockAPP/issues)

## Using the clock

Drag with the left mouse button to move it. Right-click for **Settings** or **Exit**.

Settings include timezone, Always on Top, 12/24-hour format, seconds, font size, background opacity, and separate text/background colors. **Reset all colors** restores white text and the dark background. Click **Save** to apply changes or **Cancel** to discard them.

The default timezone is Copenhagen. Daylight saving follows your system's timezone data. Position and preferences are remembered between launches.

## Platform support

The stable edition supports Windows 10/11, 64-bit. The preview adds basic Linux X11/XWayland support and Mac builds for Intel and Apple Silicon. Linux window behavior varies by desktop; fixes and pull requests are welcome. Mac builds are not notarized and desktop behavior needs further testing. See the [preview manual](cross-platform/README.md) for requirements and limitations.

## Updates and privacy

Windows and the Windows/Linux preview check GitHub at most once every 48 hours while running. Failed checks also wait 48 hours, and the schedule survives restarts. Only newer versions are downloaded. When one is ready, click **Update and restart** in Settings; the clock saves your settings, installs it and restarts.

Mac preview updates are manual. The preview uses a separate update channel and does not replace the stable Windows edition.

Automatic checks can be disabled in Settings. GitHub receives normal connection information, including your IP address; clock settings are not uploaded. The clock works offline. [GitHub Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

## Settings and removal

The stable Windows edition saves preferences to `%LOCALAPPDATA%\SimpleClockOverlay\settings.json`. Preview locations are listed in its manual.

To remove the app, exit it and delete its application folder. Optionally delete its settings folder too. The application is unsigned, so Windows may show an unknown-publisher notice. Exclusive-fullscreen apps may cover the overlay.

---
<sub>Development includes AI-assisted coding and documentation. Maintained by SwiftDevDevelopment.</sub>
