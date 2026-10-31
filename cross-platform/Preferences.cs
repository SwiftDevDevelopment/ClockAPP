using System.Globalization;
using System.Text.Json;
using Avalonia.Media;

namespace ClockOverlay;
internal sealed class Preferences
{
    public string TimeZoneId { get; set; } = "Europe/Copenhagen";
    public bool AlwaysOnTop { get; set; } = true;
    public bool Use24Hour { get; set; } = true;
    public bool ShowSeconds { get; set; } = true;
    public bool AutoUpdates { get; set; } = true;
    public int FontSize { get; set; } = 32;
    public int BackgroundOpacity { get; set; } = 80;
    public string TextColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#121926";
    public int? X { get; set; }
    public int? Y { get; set; }
    internal static string Folder
    {
        get
        {
            if (OperatingSystem.IsLinux())
            {
                var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                return Path.Combine(!string.IsNullOrEmpty(xdg) && Path.IsPathFullyQualified(xdg) ? xdg : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config"), "simple-clock-overlay");
            }
            if (OperatingSystem.IsMacOS()) return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "SimpleClockOverlay.CrossPlatform");
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleClockOverlay.CrossPlatform");
        }
    }
    internal static string FilePath => Path.Combine(Folder, "settings.json");
    internal void Normalize()
    {
        FontSize = Math.Clamp(FontSize, 14, 96); BackgroundOpacity = Math.Clamp(BackgroundOpacity, 0, 100);
        if (!ValidColor(TextColor)) TextColor = "#FFFFFF";
        if (!ValidColor(BackgroundColor)) BackgroundColor = "#121926";
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); }
        catch { TimeZoneId = "Europe/Copenhagen"; }
    }
    internal static bool ValidColor(string? text) => text is { Length: 7 } && text[0] == '#' && text.Skip(1).All(Uri.IsHexDigit);
    internal static Preferences Load(string? path = null)
    {
        try { var value = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path ?? FilePath)) ?? new(); value.Normalize(); return value; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    internal void Save(string? path = null)
    {
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(path + ".tmp", path, true);
    }
    internal Preferences Copy() => (Preferences)MemberwiseClone();
    internal string Format(DateTimeOffset now) => TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId)).ToString(Use24Hour ? (ShowSeconds ? "HH:mm:ss" : "HH:mm") : (ShowSeconds ? "hh:mm:ss tt" : "hh:mm tt"), CultureInfo.InvariantCulture);
    internal string Date(DateTimeOffset now) => TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId)).ToString("d", CultureInfo.CurrentCulture);
    internal Color Background { get { var color = Color.Parse(BackgroundColor); return Color.FromArgb((byte)(BackgroundOpacity * 255 / 100), color.R, color.G, color.B); } }
}
