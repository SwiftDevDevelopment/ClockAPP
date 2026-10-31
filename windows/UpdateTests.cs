using System.Net;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace SimpleClockOverlay;
internal static class UpdateTests
{
    internal static void Run(string report)
    {
        var lines = new List<string>();
        var root = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!, "update-tests-" + Guid.NewGuid());
        try
        {
            void Check(bool value, string name) { if (!value) throw new Exception(name); lines.Add("PASS: " + name); }
            var now = DateTimeOffset.UtcNow;
            Check(UpdateService.IsDue(null, now), "First run checks immediately");
            Check(!UpdateService.IsDue(now.AddHours(-47.99), now), "No check before 48 hours");
            Check(UpdateService.IsDue(now.AddHours(-48), now), "Check due at 48 hours");
            var executable = new byte[] { 77, 90, 1, 2, 3 };
            using var zipBytes = new MemoryStream();
            using (var archive = new ZipArchive(zipBytes, ZipArchiveMode.Create, true))
            { using var entry = archive.CreateEntry("SimpleClockOverlay.exe").Open(); entry.Write(executable); }
            var payload = zipBytes.ToArray();
            var manifest = new UpdateManifest("99.0.0", "SimpleClockOverlay-win-x64.zip", Convert.ToHexString(SHA256.HashData(payload)));
            var handler = new FakeHandler(manifest, payload);
            using (var service = new UpdateService(new HttpClient(handler), root))
            {
                service.CheckAsync(false).GetAwaiter().GetResult();
                Check(handler.Count == 0, "Disabled updates make no requests");
                service.CheckAsync(true).GetAwaiter().GetResult();
                Check(File.ReadAllBytes(Path.Combine(service.DownloadFolder, "SimpleClockOverlay.exe")).SequenceEqual(executable), "New version downloaded, checksum verified and extracted");
                Check(service.CanInstall, "Verified newer update enables installation");
                service.CheckAsync(true).GetAwaiter().GetResult();
                Check(handler.Count == 2, "Repeated ticks do not repeat network requests");
            }
            using (var restarted = new UpdateService(new HttpClient(handler), root)) { restarted.CheckAsync(true).GetAwaiter().GetResult(); Check(handler.Count == 2, "48-hour schedule persists across restarts"); }
            var badRoot = Path.Combine(root, "bad");
            using (var bad = new UpdateService(new HttpClient(new FakeHandler(manifest with { Sha256 = new string('0', 64) }, payload)), badRoot))
            {
                bad.CheckAsync(true).GetAwaiter().GetResult();
                Check(!File.Exists(Path.Combine(bad.DownloadFolder, "SimpleClockOverlay.exe")) && !File.Exists(Path.Combine(bad.DownloadFolder, "download.zip.partial")), "Checksum mismatch rejected and partial download removed");
            }
            var offline = new FakeHandler(manifest, payload) { Offline = true };
            using (var service = new UpdateService(new HttpClient(offline), Path.Combine(root, "offline")))
            {
                service.CheckAsync(true).GetAwaiter().GetResult(); service.CheckAsync(true).GetAwaiter().GetResult();
                Check(offline.Count == 1, "Network failures also wait 48 hours");
            }
            var current = new FakeHandler(manifest with { Version = UpdateService.CurrentVersion.ToString() }, payload);
            using (var service = new UpdateService(new HttpClient(current), Path.Combine(root, "current")))
            {
                service.CheckAsync(true).GetAwaiter().GetResult(); Check(current.Count == 1, "Current version does not download binary");
                Check(!service.CanInstall, "Current version does not enable installation");
            }
            var oldFile = Path.Combine(root, "installed.exe");
            var newFile = Path.Combine(root, "downloaded.exe");
            File.WriteAllText(oldFile, "old version"); File.WriteAllBytes(newFile, executable);
            var exeHash = Convert.ToHexString(SHA256.HashData(executable));
            UpdateInstaller.Replace(newFile, oldFile, exeHash);
            Check(File.ReadAllBytes(oldFile).SequenceEqual(executable) && File.ReadAllText(oldFile + ".previous") == "old version", "Installer replaces target and retains recoverable backup");
            File.WriteAllText(newFile, "corrupted download");
            var rejected = false;
            try { UpdateInstaller.Replace(newFile, oldFile, exeHash); } catch (InvalidDataException) { rejected = true; }
            Check(rejected && File.ReadAllBytes(oldFile).SequenceEqual(executable), "Tampered update rejected before changing installed app");
            lines.Add("All update checks passed. Network responses were simulated; no live update was installed.");
        }
        catch (Exception e) { lines.Add("FAIL: " + e); Environment.ExitCode = 1; }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); File.WriteAllLines(report, lines); }
    }
    sealed class FakeHandler(UpdateManifest manifest, byte[] payload) : HttpMessageHandler
    {
        public int Count;
        public bool Offline;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            if (Offline) throw new HttpRequestException("Simulated offline");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = request.RequestUri!.AbsolutePath.EndsWith("update.json") ? new StringContent(JsonSerializer.Serialize(manifest)) : new ByteArrayContent(payload) });
        }
    }
}
