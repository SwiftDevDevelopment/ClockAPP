using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace ClockOverlay;
internal sealed record InstallJob(string Source, string Target, string Hash, int Parent, string? VerificationReport = null);
internal static class Installer
{
    internal static string Hash(string path) { using var file = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(file)); }
    internal static bool Matches(string path, string hash) => Hash(path).Equals(hash, StringComparison.OrdinalIgnoreCase);
    internal static void MakeExecutable(string path)
    {
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
    }
    internal static async Task Start(string source, string hash, string root)
    {
        if (!Updates.Supported) throw new PlatformNotSupportedException("macOS preview updates are manual.");
        var target = Environment.ProcessPath ?? throw new IOException("Cannot locate running application.");
        if (!Matches(source, hash)) throw new InvalidDataException("Downloaded executable changed.");
        var probe = Path.Combine(Path.GetDirectoryName(target)!, ".clock-probe-" + Guid.NewGuid().ToString("N"));
        using (File.Create(probe)) { } File.Delete(probe);
        var folder = Path.Combine(root, "Installer"); Directory.CreateDirectory(folder);
        var helper = Path.Combine(folder, Updates.ExecutableName);
        File.Copy(target, helper, true); MakeExecutable(helper);
        var path = Path.Combine(folder, "request.json");
        File.Delete(path + ".ready"); File.Delete(path + ".approved");
        File.WriteAllText(path, JsonSerializer.Serialize(new InstallJob(source, target, hash, Environment.ProcessId)));
        var start = new ProcessStartInfo(helper) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        start.ArgumentList.Add("--apply-update"); start.ArgumentList.Add(path);
        using var process = Process.Start(start) ?? throw new IOException("Could not start update helper.");
        for (var i = 0; i < 100; i++)
        {
            if (File.Exists(path + ".ready")) { File.WriteAllText(path + ".approved", "approved"); return; }
            if (process.HasExited) throw new IOException("Update helper could not start; the clock stays open.");
            await Task.Delay(100);
        }
        throw new IOException("Update helper timed out; the clock stays open.");
    }
    internal static void Replace(string source, string target, string hash)
    {
        if (!Matches(source, hash)) throw new InvalidDataException("Executable checksum mismatch.");
        var temporary = target + ".new";
        try
        {
            File.Copy(source, temporary, true); MakeExecutable(temporary);
            if (!Matches(temporary, hash)) throw new InvalidDataException("Copied executable checksum mismatch.");
            File.Copy(target, target + ".previous", true);
            File.Move(temporary, target, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static int Apply(string path)
    {
        InstallJob? job = null; bool replaced = false; bool parentExited = false;
        try
        {
            if (!Updates.Supported) throw new PlatformNotSupportedException("macOS updates are manual.");
            job = JsonSerializer.Deserialize<InstallJob>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid install request.");
            if (!Path.IsPathFullyQualified(job.Target) || !Path.IsPathFullyQualified(job.Source) || !Matches(job.Source, job.Hash)) throw new InvalidDataException("Invalid downloaded executable.");
            using var parent = Process.GetProcessById(job.Parent);
            File.WriteAllText(path + ".ready", "ready");
            if (!parent.WaitForExit(30000)) throw new IOException("Clock did not close; update cancelled.");
            parentExited = true;
            if (!File.Exists(path + ".approved")) throw new IOException("Update not approved.");
            for (var i = 0; ; i++)
            {
                try { Replace(job.Source, job.Target, job.Hash); replaced = true; break; }
                catch (IOException) when (i < 19) { Thread.Sleep(500); }
            }
            Launch(job.Target, job.VerificationReport); return 0;
        }
        catch (Exception e)
        {
            try { File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!, "install-error.txt"), e.Message); } catch { }
            if (job is not null && replaced)
            {
                try { File.Copy(job.Target + ".previous", job.Target, true); MakeExecutable(job.Target); Launch(job.Target); } catch { }
            }
            else if (job is not null && parentExited) { try { Launch(job.Target); } catch { } }
            return 1;
        }
        finally { try { File.Delete(path + ".ready"); File.Delete(path + ".approved"); File.Delete(path); } catch { } }
    }
    static void Launch(string target, string? verificationReport = null)
    {
        var start = new ProcessStartInfo(target) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(target)! };
        if (verificationReport is not null) { start.ArgumentList.Add("--self-test"); start.ArgumentList.Add(verificationReport); }
        Process.Start(start);
    }
}
