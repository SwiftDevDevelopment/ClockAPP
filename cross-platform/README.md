# Cross-platform preview

This is the separate Avalonia edition of Simple Clock Overlay. It keeps the application small in scope: one clock, a right-click Settings/Exit menu, and local preferences. The original Windows Forms release remains unchanged.

## Downloads and platform status

Use the assets on [v2.0.0-preview.1](https://github.com/SwiftDevDevelopment/ClockAPP/releases/tag/v2.0.0-preview.1).

| Download | Status | Updates |
| --- | --- | --- |
| ClockOverlay-win-x64.zip | Windows preview; runtime and settings render checked here | 48-hour download check; click Update and restart |
| ClockOverlay-linux-x64.zip | Experimental basic Linux, X11/XWayland; logic and renders checked in Ubuntu WSLg | Same updater implemented; native installation flow still needs Linux testing |
| ClockOverlay-osx-x64.zip | Experimental Intel Mac build; cross-compiled, not tested on a Mac | Manual only |
| ClockOverlay-osx-arm64.zip | Experimental Apple Silicon Mac build; cross-compiled, not tested on a Mac | Manual only |

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

See PERFORMANCE.md for the measured Windows sample. Linux/macOS memory and CPU have not been measured. Compressed single-file runtime/native libraries can extract into the user's temporary .net directory on first launch; this is separate from updater storage.

## Validation

Windows passed 19 logic/update/packaging checks. Linux passed 20 native checks (including executable permissions) in the existing Ubuntu WSL environment. Clock and Settings were rendered and visually inspected on both Windows and Ubuntu WSLg. The minimal WSL environment needed libICE/libSM; isolated copies were used for testing without installing system packages. This is not equivalent to validation across Linux desktops. macOS binaries were cross-compiled successfully, but compilation does not prove native desktop behavior. Native Linux and Mac tests remain required, particularly dragging, transparent backgrounds, topmost, mixed-DPI monitors, focus, macOS opening restrictions and Linux update/restart. The installer helper was also exercised on Windows and Linux with isolated copied executables: wait for parent exit, replace, retain backup, and restart into the self-test runner. Interactive Update-button behavior and failures across Linux distributions still need field testing.

The native CI workflow can be run manually and checks contributions to this edition. See CONTRIBUTING.md. No native CI success is claimed until the workflow has actually completed.


