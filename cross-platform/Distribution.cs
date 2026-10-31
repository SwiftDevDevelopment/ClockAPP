using System.IO.Compression;
using System.Text;

namespace ClockOverlay;
internal static class Distribution
{
    internal static void Create(string rid, string publish, string destination)
    {
        var executable = rid.StartsWith("win-") ? "ClockOverlay.exe" : "ClockOverlay";
        var source = Path.Combine(publish, executable);
        if (!File.Exists(source)) throw new FileNotFoundException("Published executable missing.", source);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        using var file = File.Create(destination);
        using var zip = new ZipArchive(file, ZipArchiveMode.Create);
        string path = rid.StartsWith("osx-") ? "Clock Overlay.app/Contents/MacOS/ClockOverlay" : executable;
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        entry.ExternalAttributes = (0x81ED << 16); // regular file, 0755
        using (var output = entry.Open()) using (var input = File.OpenRead(source)) input.CopyTo(output);
        void Text(string name, string value)
        {
            var item = zip.CreateEntry(name); item.ExternalAttributes = (0x81A4 << 16);
            using var writer = new StreamWriter(item.Open(), new UTF8Encoding(false)); writer.Write(value);
        }
        if (rid.StartsWith("osx-"))
        {
            foreach (var library in new[] { "libAvaloniaNative.dylib", "libHarfBuzzSharp.dylib", "libSkiaSharp.dylib" })
                if (!File.Exists(Path.Combine(publish, library))) throw new FileNotFoundException("Mac publish output is missing a required native library.", library);
            foreach (var library in Directory.GetFiles(publish, "*.dylib"))
            {
                var native = zip.CreateEntry("Clock Overlay.app/Contents/MacOS/" + Path.GetFileName(library), CompressionLevel.Optimal);
                native.ExternalAttributes = (0x81ED << 16);
                using var output = native.Open();
                using var input = File.OpenRead(library);
                input.CopyTo(output);
            }
            Text("Clock Overlay.app/Contents/Info.plist", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\"><plist version=\"1.0\"><dict><key>CFBundleExecutable</key><string>ClockOverlay</string><key>CFBundleIdentifier</key><string>dev.swiftdev.clockoverlay.preview</string><key>CFBundleName</key><string>Clock Overlay</string><key>CFBundlePackageType</key><string>APPL</string><key>CFBundleShortVersionString</key><string>2.0.0</string><key>CFBundleVersion</key><string>20003</string><key>LSUIElement</key><true/><key>NSHighResolutionCapable</key><true/></dict></plist>");
        }
        Text("Manual.txt", "SIMPLE CLOCK OVERLAY " + Program.Version + " - CROSS-PLATFORM PREVIEW\n\nExtract the ZIP before launching. Windows: run ClockOverlay.exe. Linux: run ./ClockOverlay (if needed, chmod +x ClockOverlay). Mac: open Clock Overlay.app. No separate .NET installation required.\n\nDrag the clock to move it. Right-click for Settings or Exit. The date below the time follows your selected timezone and system regional short-date format. On Windows, Start with Windows is optional and off by default; enable it in Settings and Save. Disable startup before deleting the app; after moving it, open the new copy and save the startup setting again. Settings include timezone, format, seconds, font size, opacity and text/background colors (#RRGGBB). Reset all colors restores defaults. Save applies settings; Cancel discards changes.\n\n" + Updates.Notice + "\nGitHub Privacy Statement: https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement\n\nLINUX: experimental basic X11/XWayland support. Needs glibc and X11/font libraries. Native Wayland is not supported in this preview. Position, topmost and transparency vary by desktop/compositor; more code may be needed from the maintainer or users through pull requests.\n\nMAC: experimental, unsigned and not notarized; native logic checked in CI; desktop behavior still unverified. macOS may refuse to open it. No automatic update download or installation on macOS; obtain a new ZIP from GitHub Releases manually.\n\nWindows stable remains available separately; this preview uses separate settings and update metadata. No high-frequency animations, background services or web runtime. Short Windows and Linux WSLg resource samples are documented in the repository; hosted Mac runner measurements are also available.\n\nSupport/source/PRs: " + Updates.Repository + "\n");
        zip.Dispose(); file.Dispose();
        // Windows-created archives otherwise identify their creator as DOS, making unzip ignore Unix modes.
        MarkUnixCreator(destination);
    }
    internal static void MarkUnixCreator(string path)
    {
        var bytes = File.ReadAllBytes(path);
        int end = -1;
        for (var i = bytes.Length - 22; i >= Math.Max(0, bytes.Length - 65557); i--)
            if (BitConverter.ToUInt32(bytes, i) == 0x06054b50) { end = i; break; }
        if (end < 0) throw new InvalidDataException("ZIP directory missing.");
        var entries = BitConverter.ToUInt16(bytes, end + 10);
        var offset = checked((int)BitConverter.ToUInt32(bytes, end + 16));
        for (var i = 0; i < entries; i++)
        {
            if (offset < 0 || offset + 46 > bytes.Length || BitConverter.ToUInt32(bytes, offset) != 0x02014b50) throw new InvalidDataException("Invalid ZIP directory.");
            bytes[offset + 5] = 3; // Unix creator; all entry permission bits above are explicitly set.
            offset += 46 + BitConverter.ToUInt16(bytes, offset + 28) + BitConverter.ToUInt16(bytes, offset + 30) + BitConverter.ToUInt16(bytes, offset + 32);
        }
        File.WriteAllBytes(path, bytes);
    }
}



