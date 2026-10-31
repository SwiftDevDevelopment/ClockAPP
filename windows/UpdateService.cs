using System.Net.Http;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace SimpleClockOverlay;

internal sealed record UpdateManifest(string Version, string File, string Sha256);
internal sealed class UpdateState
{
    public DateTimeOffset? LastAttemptUtc { get; set; }
    public string? DownloadedVersion { get; set; }
    public string? ExecutableSha256 { get; set; }
}

internal sealed class UpdateService : IDisposable
{
    internal const string BaseUrl = "https://raw.githubusercontent.com/SwiftDevDevelopment/ClockAPP/main/";
    internal const string Notice = "Auto updates: while running, checks GitHub at most once every 48 hours, including after restarts. Newer versions download automatically. Click Update and restart to close the clock, install the download and restart with your settings. Keep the app in a writable folder. GitHub receives normal connection information, including your IP address. Offline checks wait another 48 hours. Your clock settings are not uploaded.";
    internal static readonly Version CurrentVersion = new(Application.ProductVersion.Split('+')[0]);
    readonly HttpClient client;
    readonly string root;
    readonly CancellationTokenSource cancellation = new();
    bool checking;
    internal string Status { get; private set; } = "No update downloaded.";
    internal string DownloadFolder => Path.Combine(root, "Updates");
    internal UpdateService(HttpClient? client = null, string? root = null)
    {
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        this.root = root ?? Path.GetDirectoryName(Settings.DefaultPath)!;
        try
        {
            var state = ReadState();
            if (Version.TryParse(state.DownloadedVersion, out var downloaded) && downloaded > CurrentVersion && File.Exists(Path.Combine(DownloadFolder, "SimpleClockOverlay.exe")))
                Status = $"Version {state.DownloadedVersion} ready to install.";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
    }
    internal static bool IsDue(DateTimeOffset? last, DateTimeOffset now) => last is null || now - last.Value >= TimeSpan.FromHours(48);
    internal bool CanInstall
    {
        get
        {
            try { var state = ReadState(); return !checking && Version.TryParse(state.DownloadedVersion, out var version) && version > CurrentVersion && state.ExecutableSha256 is { Length: 64 } && File.Exists(Path.Combine(DownloadFolder, "SimpleClockOverlay.exe")); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return false; }
        }
    }
    internal Task InstallAsync()
    {
        if (!CanInstall) throw new IOException("No verified newer update is ready.");
        var state = ReadState();
        return UpdateInstaller.StartAsync(Path.Combine(DownloadFolder, "SimpleClockOverlay.exe"), state.ExecutableSha256!);
    }
    UpdateState ReadState() => File.Exists(Path.Combine(root, "update-state.json")) ? JsonSerializer.Deserialize<UpdateState>(File.ReadAllText(Path.Combine(root, "update-state.json"))) ?? new() : new();
    void SaveState(UpdateState state)
    {
        var path = Path.Combine(root, "update-state.json");
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(state));
        File.Move(path + ".tmp", path, true);
    }
    internal async Task CheckAsync(bool enabled, bool force = false)
    {
        if ((!enabled && !force) || checking || cancellation.IsCancellationRequested) return;
        checking = true;
        FileStream? checkLock = null;
        string? partial = null;
        try
        {
            Directory.CreateDirectory(root);
            // An exclusive file lock also prevents parallel app instances from checking twice.
            checkLock = new FileStream(Path.Combine(root, "update.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            UpdateState state;
            try { state = ReadState(); } catch (JsonException) { state = new(); }
            var now = DateTimeOffset.UtcNow;
            if (!force && !IsDue(state.LastAttemptUtc, now)) return;
            state.LastAttemptUtc = now;
            SaveState(state); // Persist before contacting GitHub, including failed attempts.
            var token = cancellation.Token;
            using var manifestResponse = await client.GetAsync(BaseUrl + "update.json?check=" + now.ToUnixTimeSeconds(), HttpCompletionOption.ResponseHeadersRead, token);
            manifestResponse.EnsureSuccessStatusCode();
            await using var manifestStream = await manifestResponse.Content.ReadAsStreamAsync(token);
            var manifestBytes = new byte[8193];
            var count = await manifestStream.ReadAtLeastAsync(manifestBytes, manifestBytes.Length, false, token);
            if (count > 8192) throw new InvalidDataException("Update manifest too large.");
            var manifest = JsonSerializer.Deserialize<UpdateManifest>(manifestBytes.AsSpan(0, count)) ?? throw new InvalidDataException("Missing manifest.");
            if (!Version.TryParse(manifest.Version, out var version) || manifest.File != "SimpleClockOverlay-win-x64.zip" || string.IsNullOrEmpty(manifest.Sha256) || manifest.Sha256.Length != 64 || !manifest.Sha256.All(Uri.IsHexDigit))
                throw new InvalidDataException("Invalid update manifest.");
            if (version <= CurrentVersion) { Status = "You have the latest version."; return; }
            var target = Path.Combine(DownloadFolder, "SimpleClockOverlay.exe");
            if (state.DownloadedVersion == manifest.Version && state.ExecutableSha256 is not null && File.Exists(target) && UpdateInstaller.MatchesHash(target, state.ExecutableSha256)) return;
            Directory.CreateDirectory(DownloadFolder);
            partial = Path.Combine(DownloadFolder, "download.zip.partial");
            using var response = await client.GetAsync(BaseUrl + manifest.File + "?version=" + Uri.EscapeDataString(manifest.Version), HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using (var input = await response.Content.ReadAsStreamAsync(token))
            await using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920]; long total = 0; int read;
                while ((read = await input.ReadAsync(buffer, token)) > 0)
                {
                    total += read;
                    if (total > 200 * 1024 * 1024) throw new InvalidDataException("Download too large.");
                    await output.WriteAsync(buffer.AsMemory(0, read), token);
                }
            }
            var hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(partial, token)));
            if (!hash.Equals(manifest.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Download checksum mismatch.");
            using (var archive = ZipFile.OpenRead(partial))
            {
                var entry = archive.GetEntry("SimpleClockOverlay.exe") ?? throw new InvalidDataException("Update executable missing.");
                if (entry.Length > 200 * 1024 * 1024) throw new InvalidDataException("Update executable too large.");
                entry.ExtractToFile(target + ".tmp", true);
            }
            File.Move(target + ".tmp", target, true);
            state.DownloadedVersion = manifest.Version;
            state.ExecutableSha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(target, token)));
            SaveState(state);
            Status = $"Version {manifest.Version} ready to install.";
        }
        catch (Exception e) when (e is HttpRequestException or IOException or InvalidDataException or UnauthorizedAccessException or JsonException or OperationCanceledException or ArgumentException)
        {
            Status = "Update check unavailable. Next attempt after 48 hours; clock continues normally.";
        }
        finally
        {
            if (partial is not null) { try { File.Delete(partial); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
            checkLock?.Dispose(); checking = false;
        }
    }
    public void Dispose() { cancellation.Cancel(); client.Dispose(); }
}
