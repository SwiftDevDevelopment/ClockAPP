# Simple Clock Overlay

A floating digital clock for Windows, with an experimental Linux and Mac edition. Drag it anywhere, keep it above other windows, and adjust the colors, size and transparency.

## Downloads

- [Windows — stable release](https://github.com/SwiftDevDevelopment/ClockAPP/releases/latest)
- [Windows/Linux preview](https://github.com/SwiftDevDevelopment/ClockAPP/releases/tag/v2.0.0-preview.4)
- [Mac preview](https://github.com/SwiftDevDevelopment/ClockAPP/releases/tag/v2.0.0-preview.4)

Download the ZIP for your computer, extract it, and open the app. No separate .NET installation is required. For Mac, choose **Apple Silicon** for M-series Macs or **Intel** for an Intel Mac.

## Quick links

[Preview manual and platform support](cross-platform/README.md) · [Performance measurements](cross-platform/PERFORMANCE.md) · [Mac benchmark method](benchmarks/README.md) · [Contributing and build guide](cross-platform/CONTRIBUTING.md) · [Report a problem](https://github.com/SwiftDevDevelopment/ClockAPP/issues)

## Using the clock

**Move:** drag the clock with the left mouse button. Position and preferences are remembered between launches.

**Settings:** right-click and choose Settings. Adjust timezone, Always on Top, 12/24-hour format, seconds, font size, background opacity and separate text/background colors. Reset all colors restores white text and the dark background. Save applies changes; Cancel discards them. At 0% background opacity, click directly on the text to interact. The default timezone is Copenhagen; daylight saving follows your system's timezone data.

**Exit:** right-click and choose Exit.

**Updates:** Windows and the Windows/Linux preview check GitHub at most once every 48 hours while running. The schedule survives restarts and also applies to failed checks. Only newer versions download. Click Update and restart in Settings to install and relaunch with your saved preferences. Checks can be disabled in Settings. Mac preview updates are manual. The preview uses a separate channel and does not replace the stable Windows edition.

**Settings location:** the stable Windows edition uses `%LOCALAPPDATA%\SimpleClockOverlay\settings.json`. Other platform locations are in the preview manual.

**Removal:** disable Start with Windows in Settings first, then exit the clock and delete its application folder. Optionally delete its settings folder too.

**Notes:** exclusive-fullscreen apps may cover the overlay. The application is unsigned, so Windows may show an unknown-publisher notice.

<details>
<summary>Latest changes and checks — Windows 1.2.4 / Preview 2.0.0-preview.4</summary>

Released 31 October 2026:

- **Date below the time:** displayed at half the clock text size (minimum 9 pt), in the same color, with tighter padding. The date follows the selected timezone and your system's regional short-date format, including custom formats.
- **Force update check:** in Settings on Windows/Linux. Checks immediately, even if automatic checks are off, and restarts the 48-hour interval. Only newer versions download; an already verified staged download is reused. Click Update and restart to install. Mac updates remain manual.
- **Start with Windows:** optional and disabled by default. Enable it in Settings and click Save. It applies only to your Windows account and needs no administrator access. Turn it off before removing the app; after moving the executable, open it from the new folder and save the setting again.

These features are implemented in the Windows Forms and cross-platform editions. Startup is Windows-only. The date uses the system locale on Linux and Mac. The downloads above include these changes.

**Validation — 31 October 2026:** [Native release builds and checks passed](https://github.com/SwiftDevDevelopment/ClockAPP/actions/runs/41183839495) on Windows, Linux, Intel Mac and Apple Silicon Mac, including regional date formats and timezone midnight rollover. Preview clock renders passed on all four platforms, and Windows startup registration/removal checks passed. A real Windows sign-out/sign-in startup test is still pending. NuGet's online vulnerability check completed for both projects, including indirect dependencies, and reported no known vulnerable packages. This dependency check is separate from the dated antivirus scan below and is not a guarantee of complete safety.

</details>

## Platform support

The stable edition supports Windows 10/11, 64-bit. The preview adds basic Linux X11/XWayland support and Mac builds for Intel and Apple Silicon. Linux window behavior varies by desktop; fixes and pull requests are welcome. Mac builds are not notarized and desktop behavior needs further testing. See the [preview manual](cross-platform/README.md) for requirements and limitations.

## Malware scan

**30 October 2026: Microsoft Defender detected no threats** in the repository, application source and release folders, including archives. The report identifies the exact files and hashes scanned on that date; it does not cover subsequent releases. [Scan details and file hashes](docs/MALWARE-SCAN.md).

A clean scan is not a guarantee of complete safety. Use at your own risk.

## Privacy

GitHub receives normal connection information, including your IP address, when the updater checks or downloads a file. Clock settings are not uploaded. The clock works offline. [GitHub Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

---
<sub>Development includes AI-assisted coding and documentation. Maintained by SwiftDevDevelopment.</sub>
