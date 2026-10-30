# Performance

Measured on 30 October 2026 with seconds visible and Settings closed.

| Environment | Memory | CPU sample |
| --- | --- | --- |
| Windows, software rendering | 109.5 MiB working set; 40 MiB private memory | Below the process counter's resolution during a settled 20-second sample |
| Ubuntu WSLg, software rendering | 121.8 MiB resident memory | 1.5% of one CPU core over 20 seconds, after a 5-second warmup |

These are short samples, not minimum requirements or guarantees. Startup, display scaling, fonts, settings and downloads affect usage. WSLg results may differ from a regular Linux desktop. macOS has not been benchmarked.

Windows and X11 use software rendering. The default GPU renderer used around 191 MiB working set in the Windows test. The original Windows Forms edition remains available for users who prefer it.

The clock updates once per second, with no continuous animations, web runtime, background service or updater daemon. Update scheduling uses a separate one-minute timer and only contacts GitHub when the 48-hour interval is due.
