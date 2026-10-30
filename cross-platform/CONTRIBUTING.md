# Contributing to the preview

Basic Linux support is experimental. Maintainer improvements and pull requests are welcome, especially for X11/XWayland window behavior, multi-monitor positioning, dependencies, distribution packaging, and native tests. Native Wayland and macOS signing are separate future work.

Keep contributions small and practical: no web runtime, background service, continuous animation or additional features unrelated to the clock. Include your OS, desktop/compositor, X11 versus Wayland session, architecture, and exact reproduction steps in bug reports. Measure idle resources before and after renderer/timer changes.

Use the .NET 9 SDK and Avalonia 12.1.4 to build this preview:

```
dotnet publish ClockOverlay.csproj -c Release -r linux-x64 -o publish
./publish/ClockOverlay --self-test test-results.txt
```

Other targets: win-x64, osx-x64, osx-arm64. The existing development machine has SDK 9.0.304; no new SDK was installed. A supported LTS SDK/runtime migration should be considered for maintenance; this preview currently retains the installed .NET 9 toolchain. NuGet restores framework and self-contained runtime dependencies on the first build.

Run `ClockOverlay --ui-check ui-results.txt` inside a desktop session to render Settings to a PNG and exit without starting update checks. On Linux CI this is run under Xvfb; that is not a substitute for testing on a real desktop.

Package with the host's built executable:

```
ClockOverlay --package linux-x64 ./publish ./ClockOverlay-linux-x64.zip
```

The built-in packager preserves Unix executable permissions and constructs a basic unsigned .app bundle for macOS. ZIP assets contain only the app and a basic manual. Source and build instructions live separately in the repository.

Updates use a separate preview manifest. Publish immutable version-tagged release assets first, then update cross-platform/update.json with the exact platform ZIP checksums. Never replace an already-published version's ZIP with different bytes. Never change the stable Windows update.json when releasing this preview. Keep macOS update checks/download/install disabled until native signing/packaging behavior is reviewed and tested.

Platform references:
- https://docs.avaloniaui.net/docs/overview/supported-platforms
- https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement
