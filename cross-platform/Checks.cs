using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace ClockOverlay;
internal static class Checks
{
    internal static int Run(string report)
    {
        var lines = new List<string>();
        var root = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!, "preview-checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            void Check(bool condition, string name) { if (!condition) throw new Exception(name); lines.Add("PASS: " + name); }
            var value = new Preferences();
            Check(value.Format(DateTimeOffset.Parse("2026-01-15T12:00:00Z")) == "13:00:00", "Copenhagen winter");
            Check(value.Format(DateTimeOffset.Parse("2026-07-15T12:00:00Z")) == "14:00:00", "Copenhagen summer");
            Check(value.Format(DateTimeOffset.Parse("2026-03-29T01:00:00Z")) == "03:00:00", "Spring DST transition");
            Check(value.Format(DateTimeOffset.Parse("2026-10-25T01:00:00Z")) == "02:00:00", "Autumn DST transition");
            value.TimeZoneId = "UTC"; value.Use24Hour = false; value.ShowSeconds = false;
            Check(value.Format(DateTimeOffset.Parse("2026-01-15T00:00:00Z")) == "12:00 AM", "12-hour format without seconds");
            value.TextColor = "#123456"; value.BackgroundColor = "#654321"; value.X = -200;
            var settingsPath = Path.Combine(root, "settings.json"); value.Save(settingsPath); var loaded = Preferences.Load(settingsPath);
            Check(loaded.TextColor == value.TextColor && loaded.BackgroundColor == value.BackgroundColor && loaded.X == -200, "Settings and colors persist");
            value.TextColor = "bad"; value.FontSize = 1000; value.Normalize();
            Check(value.TextColor == "#FFFFFF" && value.FontSize == 96, "Settings normalize invalid input");
            File.WriteAllText(settingsPath, "bad"); Check(Preferences.Load(settingsPath).TimeZoneId == "Europe/Copenhagen", "Corrupt settings recover");
            var now = DateTimeOffset.UtcNow;
            Check(Updates.Due(null, now) && !Updates.Due(now.AddHours(-47), now) && Updates.Due(now.AddHours(-48), now), "48-hour scheduling boundary");
            Check(Updates.Newer("2.0.0-preview.99", Program.Version) && !Updates.Newer(Program.Version, Program.Version) && Updates.Newer("2.0.0", Program.Version), "Preview version ordering");
            var source = Path.Combine(root, "new"); var target = Path.Combine(root, "old");
            File.WriteAllText(source, "new version"); File.WriteAllText(target, "old version");
            var hash = Installer.Hash(source); Installer.Replace(source, target, hash);
            Check(File.ReadAllText(target) == "new version" && File.ReadAllText(target + ".previous") == "old version", "Installer replaces and retains one backup");
            File.WriteAllText(source, "tampered"); bool rejected = false;
            try { Installer.Replace(source, target, hash); } catch (InvalidDataException) { rejected = true; }
            Check(rejected && File.ReadAllText(target) == "new version", "Changed download rejected before replacement");
            if (!OperatingSystem.IsWindows()) Check((File.GetUnixFileMode(target) & UnixFileMode.UserExecute) != 0, "Unix executable permissions set");
            var executable = new byte[] { 77, 90, 1, 2 };
            using var data = new MemoryStream();
            using (var archive = new ZipArchive(data, ZipArchiveMode.Create, true)) { using var item = archive.CreateEntry(Updates.ExecutableName).Open(); item.Write(executable); }
            var zipBytes = data.ToArray();
            var archivePath = Path.Combine(root, "payload.zip"); File.WriteAllBytes(archivePath, zipBytes);
            var future = "99.0.0-preview.1";
            var release = new PreviewRelease(future, new() { [Updates.Rid] = new(Updates.Repository + "/releases/download/v" + future + "/ClockOverlay-" + Updates.Rid + ".zip", Installer.Hash(archivePath)) });
            var handler = new FakeHandler(release, zipBytes);
            var updateRoot = Path.Combine(root, "updates");
            using (var updater = new Updates(new HttpClient(handler), updateRoot))
            {
                updater.Check(false).GetAwaiter().GetResult(); Check(handler.Count == 0, "Disabled checks do not contact network");
                updater.Check(true).GetAwaiter().GetResult();
                if (Updates.Supported)
                {
                    Check(updater.CanInstall && File.ReadAllBytes(Path.Combine(updateRoot, "Updates", Updates.ExecutableName)).SequenceEqual(executable), "Verified new version downloaded and staged");
                    updater.Check(true).GetAwaiter().GetResult(); Check(handler.Count == 2, "No repeated downloads within 48 hours");
                }
                else Check(handler.Count == 0 && !updater.CanInstall, "macOS performs no automatic check, download or install");
            }
            using (var updater = new Updates(new HttpClient(handler), updateRoot)) { updater.Check(true).GetAwaiter().GetResult(); Check(handler.Count == (Updates.Supported ? 2 : 0), "Check timestamp survives restart"); }
            var bad = new FakeHandler(release with { Files = new() { [Updates.Rid] = release.Files[Updates.Rid] with { Sha256 = new string('0',64) } } }, zipBytes);
            using (var updater = new Updates(new HttpClient(bad), Path.Combine(root, "bad"))) { updater.Check(true).GetAwaiter().GetResult(); Check(!updater.CanInstall, "Invalid archive checksum never enables installation"); }
            var offline = new FakeHandler(release, zipBytes) { Offline = true };
            using (var updater = new Updates(new HttpClient(offline), Path.Combine(root, "offline"))) { updater.Check(true).GetAwaiter().GetResult(); updater.Check(true).GetAwaiter().GetResult(); Check(offline.Count == (Updates.Supported ? 1 : 0), "Offline attempts respect 48-hour interval"); }
            var publish = Path.Combine(root, "publish"); Directory.CreateDirectory(publish); File.WriteAllBytes(Path.Combine(publish, "ClockOverlay"), executable);
            foreach (var library in new[] { "libAvaloniaNative.dylib", "libHarfBuzzSharp.dylib", "libSkiaSharp.dylib" }) File.WriteAllBytes(Path.Combine(publish, library), executable);
            var mac = Path.Combine(root, "mac.zip"); Distribution.Create("osx-arm64", publish, mac);
            using (var archive = ZipFile.OpenRead(mac))
            {
                var item = archive.GetEntry("Clock Overlay.app/Contents/MacOS/ClockOverlay");
                Check(item is not null && ((item.ExternalAttributes >> 16) & 0x1FF) == 0x1ED && archive.GetEntry("Clock Overlay.app/Contents/Info.plist") is not null, "Mac bundle packaging and executable mode");
                Check(archive.GetEntry("Clock Overlay.app/Contents/MacOS/libSkiaSharp.dylib") is not null && archive.GetEntry("Clock Overlay.app/Contents/MacOS/libAvaloniaNative.dylib") is not null && archive.GetEntry("Clock Overlay.app/Contents/MacOS/libHarfBuzzSharp.dylib") is not null, "Mac bundle includes native UI, graphics and text libraries");
            }
            lines.Add("All checks passed. HTTP is simulated; native desktop behavior must also be tested on each OS.");
            File.WriteAllLines(report, lines); return 0;
        }
        catch (Exception e) { lines.Add("FAIL: " + e); File.WriteAllLines(report, lines); return 1; }
        finally
        {
            if (Path.GetFullPath(root).StartsWith(Path.GetDirectoryName(Path.GetFullPath(report))! + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Directory.Delete(root, true);
        }
    }
    sealed class FakeHandler(PreviewRelease release, byte[] payload) : HttpMessageHandler
    {
        internal int Count; internal bool Offline;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Count++; if (Offline) throw new HttpRequestException("Simulated offline");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = request.RequestUri!.AbsolutePath.EndsWith("update.json") ? new StringContent(JsonSerializer.Serialize(release)) : new ByteArrayContent(payload) });
        }
    }
}

