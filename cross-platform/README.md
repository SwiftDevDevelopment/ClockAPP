# Cross-platform preview

This is the separate Avalonia edition of Simple Clock Overlay. It keeps the application small in scope: one clock, a right-click Settings/Exit menu, and local preferences. The Windows Forms edition is maintained separately.

## Downloads and platform status

Windows, Linux and Mac downloads: [v2.0.0-preview.3](https://github.com/SwiftDevDevelopment/ClockAPP/releases/tag/v2.0.0-preview.3). Choose Apple Silicon for M-series Macs or Intel for Intel Macs.

| Download | Status | Updates |
| --- | --- | --- |
| ClockOverlay-win-x64.zip | Windows preview; runtime and settings render checked here | 48-hour download check; click Update and restart |
| ClockOverlay-linux-x64.zip | Experimental basic Linux, X11/XWayland; logic and renders checked in Ubuntu WSLg | Same updater implemented; native installation flow still needs Linux testing |
| ClockOverlay-Mac-Intel.zip | Experimental Intel Mac build; native GUI startup and idle resources benchmarked; desktop interaction remains experimental | Manual only |
| ClockOverlay-Mac-Apple-Silicon.zip | Experimental Apple Silicon Mac build; native GUI startup and idle resources benchmarked; desktop interaction remains experimental | Manual only |

Extract the ZIP before launching. Windows: double-click ClockOverlay.exe. Linux: run `./ClockOverlay`; if the archive tool dropped executable permissions, run `chmod +x ClockOverlay` first. macOS: open Clock Overlay.app. No separate .NET installation is required. macOS builds are unsigned and not notarized, so Gatekeeper may refuse them. This preview does not weaken OS security settings or attempt to bypass them.

On Linux, install your distribution's normal X11/font support if missing (`libX11`, `libICE`, `libSM`, `fontconfig`, glibc). Debian/Ubuntu package names typically include `libx11-6 libice6 libsm6 libfontconfig1`. A compositor is needed for transparent windows. XWayland may allow the app to run in a Wayland session, but behavior depends on the desktop. Native Wayland is not supported by this preview.

**Linux support is intentionally basic.** Window placement, mouse dragging, topmost behavior and transparency may need more code from the maintainer or community. Pull requests welcome. Do not assume parity with the Windows version or exclusive-fullscreen support.

## Basic manual

Drag the clock to move it. Right-click for Settings or Exit. Settings includes timezone, 12/24-hour format, seconds, font size, background opacity, text/background colors, and Reset all colors. Enter colors as `#RRGGBB`. Save applies changes; Cancel discards them. The settings window scrolls on small displays. DST uses the operating system's timezone database.

Settings are separate from the original app:

- Windows: `%LOCALAPPDATA%\SimpleClockOverlay.CrossPlatform\settings.json`
- Linux: `$XDG_CONFIG_HOME/simple-clock-overlay/settings.json`, or `~/.config/simple-clock-overlay/settings.json`
- macOS: `~/Library/Application Support/SimpleClockOverlay.CrossPlatform/settings.json`

## Updates and data handling

Windows/Linux preview checks at most once every 48 hours while running, including failures and restarts. Only a newer version downloads a platform-specific ZIP; the download is checksum-verified. Click Update and restart to save settings, close, replace the executable and relaunch. Keep the application in a user-writable folder. One helper, downloaded executable and previous-version backup are reused; temporary archives are removed. If replacement/launch fails, the helper attempts to restore the old executable. Installer errors are recorded in the profile's `Installer/install-error.txt`.

macOS makes **no automatic update-check or download requests**, and its automatic install path is disabled. Use GitHub Releases to obtain updates manually. This is a deliberate preview limitation pending signing and native testing, not a claim that macOS prohibits standalone auto-updaters.

The preview uses `cross-platform/update.json`, independently of the Windows v1 update manifest. Existing Windows users are not automatically migrated. Disable automatic checks in Settings to stop future requests. GitHub receives normal connection data, including your IP address; settings are not uploaded. [GitHub Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

## Resource use

No Electron/web runtime, background service, updater daemon, telemetry SDK, MVVM framework or continuous animation loop. The clock uses one 1-second timer and changes its text only when the displayed time changes. A separate 1-minute timer checks the saved 48-hour deadline locally. Windows/X11 use software rendering to avoid a GPU context for a tiny clock. Resources still include .NET, Avalonia and its text/graphics stack; this is not a near-zero-memory application. The original Windows Forms app is still the smaller-scope Windows option.

See PERFORMANCE.md for the measured Windows sample. Linux WSLg measured about 122 MiB resident memory and 1.5% of one CPU core in a short sample; Mac hosted-runner CPU, memory and startup measurements are now available in PERFORMANCE.md. Compressed single-file runtime/native libraries can extract into the user's temporary .net directory on first launch; this is separate from updater storage.

## Validation

Windows passed 19 logic/update/packaging checks. Linux passed 20 native checks (including executable permissions) in the existing Ubuntu WSL environment. Clock and Settings were rendered and visually inspected on both Windows and Ubuntu WSLg. The minimal WSL environment needed libICE/libSM; isolated copies were used for testing without installing system packages. This is not equivalent to validation across Linux desktops. Cross-compiled release binaries also passed native logic checks on Intel Mac, Apple Silicon Mac and Linux in CI (run 41051912955), after correcting executable permission handling in the test. Preview.2 fixes ZIP creator metadata so Unix extractors preserve executable permissions directly. Mac GUI startup and idle resource use were subsequently benchmarked on hosted runners; interactive desktop and Gatekeeper behavior still need testing. Native Linux and Mac tests remain required, particularly dragging, transparent backgrounds, topmost, mixed-DPI monitors, focus, macOS opening restrictions and Linux update/restart. The installer helper was also exercised on Windows and Linux with isolated copied executables: wait for parent exit, replace, retain backup, and restart into the self-test runner. Interactive Update-button behavior and failures across Linux distributions still need field testing.

The native CI workflow can be run manually and checks contributions to this edition. See CONTRIBUTING.md. Final native CI passed on all four targets, including execution of the actual published Unix ZIPs without chmod: [run 41052534607](https://github.com/SwiftDevDevelopment/ClockAPP/actions/runs/41052534607). This validates the preview.2 archive permission fix; it does not establish macOS desktop/Gatekeeper behavior or universal Linux compositor compatibility.

### Windows startup and date (2.0.0-preview.3)

In Settings, enable **Start with Windows** and click Save. It is off by default and applies only to your Windows account. Disable it and save before removing the app. Keep the executable in its permanent folder; after moving it, open it there and save this setting again. Windows Task Manager can also disable startup apps. This option is unavailable on Linux and Mac.

The clock also shows the date below the time at 65% of the clock font size. It follows the selected timezone and the system's short-date format, including regional customizations. Linux and Mac use the system locale through .NET. Native logic and clock-render checks passed on all four preview platforms: [release build results](https://github.com/SwiftDevDevelopment/ClockAPP/actions/runs/41183149262). Hosted tests do not cover every desktop environment or Gatekeeper behavior.
