# Performance

Measured on 30 October 2026 with seconds visible and Settings closed.

| Environment | Memory | CPU sample |
| --- | --- | --- |
| Windows, software rendering | 109.5 MiB working set; 40 MiB private memory | Below the process counter's resolution during a settled 20-second sample |
| Ubuntu WSLg, software rendering | 121.8 MiB resident memory | 1.5% of one CPU core over 20 seconds, after a 5-second warmup |

These are short samples, not minimum requirements or guarantees. Startup, display scaling, fonts, settings and downloads affect usage. WSLg results may differ from a regular Linux desktop. Mac measurements are listed below.

Windows and X11 use software rendering. The default GPU renderer used around 191 MiB working set in the Windows test. The original Windows Forms edition remains available for users who prefer it.

The clock updates once per second, with no continuous animations, web runtime, background service or updater daemon. Update scheduling uses a separate one-minute timer and only contacts GitHub when the 48-hour interval is due.

## Mac — GitHub hosted runners

Measured on 30 October 2026 using the corrected Mac package of **2.0.0-preview.2 (mac.1)**. The real clock was running, and its onscreen window was verified at the start and end of every sample.

| Mac runner | Seconds | CPU (% of one core) | Resident memory (MiB) |
| --- | --- | ---: | ---: |
| Intel i7-8700B, 4 logical CPUs, macOS 15.7.9 | On | 2.574 | 120.6 |
| Intel i7-8700B, 4 logical CPUs, macOS 15.7.9 | Off | 0.639 | 121.5 |
| Apple M1 (Virtual), 3 logical CPUs, macOS 26.6.2 | On | 2.213 | 178.9 |
| Apple M1 (Virtual), 3 logical CPUs, macOS 26.6.2 | Off | 0.673 | 175.8 |

Figures are medians of three 30-second samples per mode, each after a 10-second warmup. CPU is process CPU time divided by elapsed time; 100% means one fully occupied core, not the entire machine. RAM is process RSS, not private allocations or total graphics/system memory. The Mac build uses its default renderer; it is not configured like the software-rendered Windows/X11 builds above.

| Mac runner | First launch to visible window | Subsequent launches, seconds on | Subsequent launches, seconds off |
| --- | ---: | ---: | ---: |
| Intel | 10.209 s | 2.219 s | 2.174 s |
| Apple Silicon | 3.477 s | 1.635 s | 2.036 s |

Subsequent-launch values are medians, excluding the first invocation. The first launch can include bundled-runtime extraction; OS caches were not controlled. These are hosted-runner measurements, not promises for a physical Mac. They establish GUI startup and idle resource use in these environments, not Gatekeeper approval or complete desktop compatibility.

[Benchmark run](https://github.com/SwiftDevDevelopment/ClockAPP/actions/runs/41056822383) · [Method](../benchmarks/README.md) · [Raw Intel samples](../benchmarks/results/macos-2026-10-30-intel.json) · [Raw Apple Silicon samples](../benchmarks/results/macos-2026-10-30-apple-silicon.json)

The first attempt exposed missing graphics libraries in the original Mac ZIPs. Use the [corrected Mac package](https://github.com/SwiftDevDevelopment/ClockAPP/releases/tag/v2.0.0-preview.2-mac.1); the Windows/Linux packages and updater versions were not changed.
