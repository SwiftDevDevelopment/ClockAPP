# Measured preview resource use

Windows test, 30 October 2026. Software-rendered self-contained win-x64 preview, default font, seconds visible, settings closed. After startup settled, a 20-second sample measured:

- Working set: 109.5 MiB
- Private committed memory: 40.0 MiB
- CPU time delta: 0 ms at Windows process CPU-counter resolution in that sample

This short idle sample is not a guarantee of zero CPU. An earlier startup sample measured 0.94% of one CPU core over 20 seconds. Window visibility, display scale, fonts, OS, graphics drivers, settings interactions and update downloads change these values. macOS has not been benchmarked; a separate Linux sample is recorded below.

Default GPU rendering used about 191 MiB working set in a warm sample. The preview therefore selects software rendering on Windows and X11. No GPU-rendering or low-memory claims are made for macOS.

A cross-platform graphics toolkit has a memory cost. Keep the original Windows Forms edition if minimizing Windows memory is more important than using the cross-platform preview. No continuous animation, app-wide polling loop, background service, web runtime or updater daemon is used.

A native Linux WSLg sample (20 seconds after a 5-second warmup, seconds visible) measured 121.8 MiB resident memory and 1.5% of one CPU core. This includes a virtualized WSLg environment; it is not a normal-desktop guarantee.

