using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace SimpleClockOverlay;

internal sealed record InstallRequest(string Source, string Target, string Sha256, int ParentId, string? VerificationReport = null);
internal static class UpdateInstaller
{
    internal static async Task StartAsync(string source, string hash)
    {
        var target = Environment.ProcessPath ?? throw new IOException("Cannot locate the running application.");
        if (!MatchesHash(source, hash)) throw new InvalidDataException("The downloaded update changed. Please download it again.");
        // Check write permission before asking the running clock to exit.
        var probe = Path.Combine(Path.GetDirectoryName(target)!, ".clock-write-" + Guid.NewGuid().ToString("N"));
        using (File.Create(probe)) { }
        File.Delete(probe);
        var folder = Path.Combine(Path.GetDirectoryName(Settings.DefaultPath)!, "Updates", "Installer");
        Directory.CreateDirectory(folder);
        var helper = Path.Combine(folder, "Updater.exe");
        File.Copy(target, helper, true);
        var request = Path.Combine(folder, "request.json");
        var ready = request + ".ready";
        if (File.Exists(ready)) File.Delete(ready);
        if (File.Exists(request + ".approved")) File.Delete(request + ".approved");
        File.WriteAllText(request, JsonSerializer.Serialize(new InstallRequest(source, target, hash, Environment.ProcessId)));
        var start = new ProcessStartInfo(helper) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        start.ArgumentList.Add("--apply-update"); start.ArgumentList.Add(request);
        using var process = Process.Start(start) ?? throw new IOException("Could not start update installer.");
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (File.Exists(ready)) { File.WriteAllText(request + ".approved", "approved"); return; }
            if (process.HasExited) throw new IOException("The update installer could not start. The clock has been kept open.");
            await Task.Delay(100);
        }
        // The helper itself times out without changing files if the clock stays open.
        throw new IOException("The update installer did not respond. The clock has been kept open.");
    }
    internal static bool MatchesHash(string path, string hash)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
    internal static void Replace(string source, string target, string hash)
    {
        if (!MatchesHash(source, hash)) throw new InvalidDataException("Downloaded update checksum mismatch.");
        var temporary = target + ".new-" + Guid.NewGuid().ToString("N");
        try
        {
            File.Copy(source, temporary);
            if (!MatchesHash(temporary, hash)) throw new InvalidDataException("Copied update checksum mismatch.");
            File.Replace(temporary, target, target + ".previous", true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static void Run(string requestPath)
    {
        InstallRequest? request = null;
        bool replaced = false;
        try
        {
            request = JsonSerializer.Deserialize<InstallRequest>(File.ReadAllText(requestPath)) ?? throw new InvalidDataException("Invalid update request.");
            if (!Path.IsPathFullyQualified(request.Source) || !Path.IsPathFullyQualified(request.Target) || !request.Target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid application path.");
            using var parent = Process.GetProcessById(request.ParentId);
            if (!MatchesHash(request.Source, request.Sha256)) throw new InvalidDataException("Downloaded update checksum mismatch.");
            File.WriteAllText(requestPath + ".ready", "ready");
            if (!parent.WaitForExit(30000)) throw new IOException("The clock did not close. No update was installed.");
            if (!File.Exists(requestPath + ".approved")) throw new IOException("The update was cancelled before the clock closed.");
            for (var attempt = 0; ; attempt++)
            {
                try { Replace(request.Source, request.Target, request.Sha256); replaced = true; break; }
                catch (IOException) when (attempt < 19) { Thread.Sleep(500); }
            }
            Launch(request.Target, request.VerificationReport);
        }
        catch (Exception e)
        {
            var recovery = "The existing clock has been kept.";
            if (request is not null && replaced)
            {
                try { File.Copy(request.Target + ".previous", request.Target, true); Launch(request.Target); recovery = "The previous clock was restored and restarted."; }
                catch { recovery = "Your previous clock is saved as " + request.Target + ".previous"; }
            }
            else if (request is not null)
            {
                try { using var parent = Process.GetProcessById(request.ParentId); if (parent.HasExited) Launch(request.Target); }
                catch (ArgumentException) { try { Launch(request.Target); } catch { } }
            }
            MessageBox.Show("The update could not be installed.\n" + e.Message + "\n\n" + recovery, "Clock Overlay update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            try { File.Delete(requestPath + ".ready"); File.Delete(requestPath + ".approved"); File.Delete(requestPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    static void Launch(string target, string? verificationReport = null)
    {
        var start = new ProcessStartInfo(target) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(target)! };
        // Integration tests can relaunch into the existing self-test mode without leaving a clock open.
        if (verificationReport is not null) { start.ArgumentList.Add("--self-test"); start.ArgumentList.Add(verificationReport); }
        Process.Start(start);
    }
}
