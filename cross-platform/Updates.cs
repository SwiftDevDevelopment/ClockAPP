using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO.Compression;

namespace ClockOverlay;
internal sealed record ReleaseFile(string Url, string Sha256);
internal sealed record PreviewRelease(string Version, Dictionary<string, ReleaseFile> Files);
internal sealed class UpdateState
{
    public DateTimeOffset? LastCheck { get; set; }
    public string? Version { get; set; }
    public string? Hash { get; set; }
}
internal sealed class Updates : IDisposable
{
    internal const string Repository = "https://github.com/SwiftDevDevelopment/ClockAPP";
    internal const string Notice = "Windows/Linux: checks GitHub at most once per 48 hours while running, including failed checks and restarts. New preview versions download automatically. Click Update and restart to install. macOS: manual updates only. GitHub receives normal connection details, including your IP address; your settings are not uploaded. Downloads and one backup are reused rather than accumulated.";
    internal static string Rid => (OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux") + "-" + (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64");
    internal static bool Supported => OperatingSystem.IsWindows() || OperatingSystem.IsLinux();
    internal static string ExecutableName => OperatingSystem.IsWindows() ? "ClockOverlay.exe" : "ClockOverlay";
    readonly string folder;
    readonly HttpClient client;
    readonly CancellationTokenSource cancellation = new();
    bool busy;
    string StatePath => Path.Combine(folder, "update-state.json");
    string Download => Path.Combine(folder, "Updates", ExecutableName);
    internal string Status { get; private set; } = Supported ? "No update downloaded." : "macOS preview: download updates manually from GitHub Releases.";
    internal Updates(HttpClient? client = null, string? folder = null)
    {
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromMinutes(4) };
        this.folder = folder ?? Preferences.Folder;
        if (CanInstall) Status = "A verified preview update is ready to install.";
    }
    UpdateState Read() => File.Exists(StatePath) ? JsonSerializer.Deserialize<UpdateState>(File.ReadAllText(StatePath)) ?? new() : new();
    void Write(UpdateState state)
    {
        File.WriteAllText(StatePath + ".tmp", JsonSerializer.Serialize(state)); File.Move(StatePath + ".tmp", StatePath, true);
    }
    internal static bool Due(DateTimeOffset? last, DateTimeOffset now) => last is null || now - last.Value >= TimeSpan.FromHours(48);
    internal static bool Newer(string candidate, string current)
    {
        static (Version Version, int Preview) Parse(string text)
        {
            var split = text.Split("-preview.", StringSplitOptions.None);
            if (split.Length > 2 || !Version.TryParse(split[0], out var version)) throw new InvalidDataException("Invalid preview version.");
            var preview = split.Length == 1 ? int.MaxValue : int.Parse(split[1], System.Globalization.CultureInfo.InvariantCulture);
            return (version, preview);
        }
        try { var a = Parse(candidate); var b = Parse(current); return a.Version > b.Version || a.Version == b.Version && a.Preview > b.Preview; }
        catch (Exception e) when (e is FormatException or OverflowException) { throw new InvalidDataException("Invalid preview version.", e); }
    }
    internal bool CanInstall
    {
        get
        {
            if (!Supported || busy) return false;
            try { var state = Read(); return state.Version is not null && Newer(state.Version, Program.Version) && state.Hash is { Length: 64 } && File.Exists(Download); }
            catch (Exception e) when (e is IOException or JsonException or InvalidDataException or UnauthorizedAccessException) { return false; }
        }
    }
    internal Task Install()
    {
        if (!CanInstall) throw new IOException("No verified newer update is ready.");
        return Installer.Start(Download, Read().Hash!, folder);
    }
    internal async Task Check(bool enabled)
    {
        if (!enabled || !Supported || busy || cancellation.IsCancellationRequested) return;
        busy = true;
        FileStream? mutex = null;
        var partial = Path.Combine(folder, "download.partial");
        try
        {
            Directory.CreateDirectory(folder);
            mutex = new FileStream(Path.Combine(folder, "update.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            UpdateState state;
            try { state = Read(); } catch (JsonException) { state = new(); }
            var now = DateTimeOffset.UtcNow;
            if (!Due(state.LastCheck, now)) return;
            state.LastCheck = now; Write(state);
            var token = cancellation.Token;
            using var manifestResponse = await client.GetAsync("https://raw.githubusercontent.com/SwiftDevDevelopment/ClockAPP/main/cross-platform/update.json?check=" + now.ToUnixTimeSeconds(), HttpCompletionOption.ResponseHeadersRead, token);
            manifestResponse.EnsureSuccessStatusCode();
            using var manifestStream = await manifestResponse.Content.ReadAsStreamAsync(token);
            var bytes = new byte[65537]; var count = await manifestStream.ReadAtLeastAsync(bytes, bytes.Length, false, token);
            if (count > 65536) throw new InvalidDataException("Manifest too large.");
            var manifest = JsonSerializer.Deserialize<PreviewRelease>(bytes.AsSpan(0, count)) ?? throw new InvalidDataException("Missing preview manifest.");
            if (!Newer(manifest.Version, Program.Version)) { Status = "You have the latest preview."; return; }
            if (manifest.Files is null || !manifest.Files.TryGetValue(Rid, out var release)) { Status = "No update is available for this architecture."; return; }
            var expectedUrl = Repository + "/releases/download/v" + manifest.Version + "/ClockOverlay-" + Rid + ".zip";
            if (release.Url != expectedUrl || release.Sha256 is not { Length: 64 } || !release.Sha256.All(Uri.IsHexDigit)) throw new InvalidDataException("Invalid preview download.");
            if (state.Version == manifest.Version && state.Hash is not null && File.Exists(Download) && Installer.Matches(Download, state.Hash)) return;
            using var response = await client.GetAsync(release.Url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using (var input = await response.Content.ReadAsStreamAsync(token))
            await using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920]; long total = 0; int read;
                while ((read = await input.ReadAsync(buffer, token)) > 0)
                {
                    total += read; if (total > 300L * 1024 * 1024) throw new InvalidDataException("Download too large.");
                    await output.WriteAsync(buffer.AsMemory(0, read), token);
                }
            }
            if (!Installer.Matches(partial, release.Sha256)) throw new InvalidDataException("Download checksum mismatch.");
            Directory.CreateDirectory(Path.GetDirectoryName(Download)!);
            using (var archive = ZipFile.OpenRead(partial))
            {
                var entry = archive.GetEntry(ExecutableName) ?? throw new InvalidDataException("Executable not found.");
                if (entry.Length > 400L * 1024 * 1024) throw new InvalidDataException("Executable too large.");
                entry.ExtractToFile(Download + ".tmp", true);
            }
            Installer.MakeExecutable(Download + ".tmp");
            File.Move(Download + ".tmp", Download, true);
            state.Version = manifest.Version; state.Hash = Installer.Hash(Download); Write(state);
            Status = "Version " + manifest.Version + " ready. Click Update and restart.";
        }
        catch (Exception e) when (e is HttpRequestException or IOException or InvalidDataException or JsonException or OperationCanceledException or UnauthorizedAccessException or ArgumentException)
        { Status = "Update check failed; next attempt after 48 hours. The clock continues offline."; }
        finally
        {
            // Only the lock owner owns the shared temporary download.
            if (mutex is not null)
            {
                try { File.Delete(partial); File.Delete(Download + ".tmp"); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                mutex.Dispose();
            }
            busy = false;
        }
    }
    public void Dispose() { cancellation.Cancel(); client.Dispose(); }
}
