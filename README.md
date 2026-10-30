# Simple Clock Overlay

A floating digital clock for Windows, with an experimental Linux and Mac edition. Drag it anywhere, keep it above other windows, and adjust the colors, size and transparency.

## Downloads

- [Windows — stable release](https://github.com/SwiftDevDevelopment/ClockAPP/releases/latest)
- [Windows/Linux preview](https://github.com/SwiftDevDevelopment/ClockAPP/releases/tag/v2.0.0-preview.2)
- [Mac preview — corrected package](https://github.com/SwiftDevDevelopment/ClockAPP/releases/tag/v2.0.0-preview.2-mac.1)

Download the ZIP for your computer, extract it, and open the app. No separate .NET installation is required. For Mac, choose **Apple Silicon** for M-series Macs or **Intel** for an Intel Mac.

## Quick links

[Preview manual and platform support](cross-platform/README.md) · [Performance measurements](cross-platform/PERFORMANCE.md) · [Mac benchmark method](benchmarks/README.md) · [Contributing and build guide](cross-platform/CONTRIBUTING.md) · [Report a problem](https://github.com/SwiftDevDevelopment/ClockAPP/issues)

## Using the clock

**Move:** drag the clock with the left mouse button. Position and preferences are remembered between launches.

**Settings:** right-click and choose Settings. Adjust timezone, Always on Top, 12/24-hour format, seconds, font size, background opacity and separate text/background colors. Reset all colors restores white text and the dark background. Save applies changes; Cancel discards them. At 0% background opacity, click directly on the text to interact. The default timezone is Copenhagen; daylight saving follows your system's timezone data.

**Exit:** right-click and choose Exit.

**Updates:** Windows and the Windows/Linux preview check GitHub at most once every 48 hours while running. The schedule survives restarts and also applies to failed checks. Only newer versions download. Click Update and restart in Settings to install and relaunch with your saved preferences. Checks can be disabled in Settings. Mac preview updates are manual. The preview uses a separate channel and does not replace the stable Windows edition.

**Settings location:** the stable Windows edition uses `%LOCALAPPDATA%\SimpleClockOverlay\settings.json`. Other platform locations are in the preview manual.

**Removal:** exit the clock and delete its application folder. Optionally delete its settings folder too.

**Notes:** exclusive-fullscreen apps may cover the overlay. The application is unsigned, so Windows may show an unknown-publisher notice.

## Platform support

The stable edition supports Windows 10/11, 64-bit. The preview adds basic Linux X11/XWayland support and Mac builds for Intel and Apple Silicon. Linux window behavior varies by desktop; fixes and pull requests are welcome. Mac builds are not notarized and desktop behavior needs further testing. See the [preview manual](cross-platform/README.md) for requirements and limitations.

## Privacy

GitHub receives normal connection information, including your IP address, when the updater checks or downloads a file. Clock settings are not uploaded. The clock works offline. [GitHub Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

---
<sub>Development includes AI-assisted coding and documentation. Maintained by SwiftDevDevelopment.</sub>


