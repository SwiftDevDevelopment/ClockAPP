# Mac benchmark method

The manual **Mac resource benchmark** workflow downloads the corrected Mac preview package and runs it on GitHub's Intel and Apple Silicon Mac runners. It does not rebuild the app or change its update version.

For each architecture, the script launches the real clock six times, alternating seconds on/off. Every launch must produce an onscreen window belonging to that process, checked through CoreGraphics. It then waits 10 seconds and samples resident memory and process CPU time for 30 seconds. The report gives the median of three samples per mode.

- CPU is a percentage of one core; 100% means one fully occupied core.
- Memory is process RSS, not private memory or total system/graphics memory.
- Startup is time from launching the executable to detecting its onscreen window. The first launch is listed separately; OS caches are not controlled.
- The downloaded Mac package is used as shipped, with its default renderer and fonts, 32-point text, 80% background opacity and Always on Top.
- Automatic updates are disabled on macOS. Settings are also set to disable checks during the benchmark.

Results describe a hosted runner, not every physical Mac. Other host activity, virtualization, graphics configuration, display scaling and fonts can affect them. These tests do not establish Gatekeeper approval, notarization, user interaction or behavior across Spaces/fullscreen applications.

Run through **Actions → Mac resource benchmark → Run workflow**. JSON, per-process logs and summaries are attached to the run. The script is intended for disposable runners; it temporarily changes the preview's settings and restores the original file on exit. Run it separately from any existing clock instance.

Published figures and links to the raw samples are in [Performance](../cross-platform/PERFORMANCE.md).
