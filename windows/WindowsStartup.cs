using Microsoft.Win32;
using System.Runtime.Versioning;

namespace SimpleClockOverlay;

internal static class WindowsStartup
{
    internal const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string EntryName = "SimpleClockOverlay";

    [SupportedOSPlatform("windows")]
    internal static bool Enabled(string keyPath = RunKey)
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        return key?.GetValue(EntryName) is string command && command == Command();
    }

    internal static string Command() => "\"" + (Environment.ProcessPath ?? throw new InvalidOperationException("Cannot find the clock executable.")) + "\"";

    [SupportedOSPlatform("windows")]
    internal static void SetEnabled(bool enabled, string keyPath = RunKey)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath, true);
        if (enabled) key.SetValue(EntryName, Command(), RegistryValueKind.String);
        else key.DeleteValue(EntryName, false);
    }

    [SupportedOSPlatform("windows")]
    internal static void Check(Action<bool, string> check)
    {
        var path = @"Software\SimpleClockOverlay.Tests\" + Guid.NewGuid().ToString("N");
        try
        {
            check(!Enabled(path), "Startup defaults to disabled");
            SetEnabled(true, path);
            check(Enabled(path), "Startup registration enables with quoted executable path");
            using (var key = Registry.CurrentUser.OpenSubKey(path))
                check((string?)key?.GetValue(EntryName) == Command(), "Startup points to current executable");
            SetEnabled(false, path); SetEnabled(false, path);
            check(!Enabled(path), "Startup registration removes safely and repeatedly");
        }
        finally { Registry.CurrentUser.DeleteSubKeyTree(path, false); }
    }
}
