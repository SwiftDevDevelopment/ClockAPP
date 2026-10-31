namespace SimpleClockOverlay;

internal static class SelfTests
{
    public static void Run(string report)
    {
        var lines = new List<string>();
        var temporary = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!, "clock-test-" + Guid.NewGuid() + ".json");
        try
        {
            void Check(bool value, string name) { if (!value) throw new Exception(name); lines.Add("PASS: " + name); }
            WindowsStartup.Check(Check);
            var s = new Settings();
            var savedCulture = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                var instant = DateTimeOffset.Parse("2026-01-15T23:30:00Z");
                foreach (var locale in new[] { "da-DK", "en-US", "ja-JP" })
                {
                    var culture = new System.Globalization.CultureInfo(locale);
                    System.Globalization.CultureInfo.CurrentCulture = culture;
                    Check(ClockText.Date(instant, new Settings()) == new DateTime(2026, 1, 16).ToString("d", culture), "Regional date and timezone midnight: " + locale);
                    culture.DateTimeFormat.ShortDatePattern = "yyyy'/'MM'/'dd";
                    Check(ClockText.Date(instant, new Settings()) == "2026/01/16", "Custom regional date pattern: " + locale);
                }
            }
            finally { System.Globalization.CultureInfo.CurrentCulture = savedCulture; }
            Check(ClockText.Format(DateTimeOffset.Parse("2026-01-15T12:00:00Z"), s) == "13:00:00", "Copenhagen winter UTC+1");
            Check(ClockText.Format(DateTimeOffset.Parse("2026-07-15T12:00:00Z"), s) == "14:00:00", "Copenhagen summer UTC+2");
            Check(ClockText.Format(DateTimeOffset.Parse("2026-03-29T00:59:59Z"), s) == "01:59:59", "Before spring DST transition");
            Check(ClockText.Format(DateTimeOffset.Parse("2026-03-29T01:00:00Z"), s) == "03:00:00", "After spring DST transition");
            Check(ClockText.Format(DateTimeOffset.Parse("2026-10-25T01:00:00Z"), s) == "02:00:00", "Autumn DST transition");
            s.TimeZoneId = "UTC"; s.Use24Hour = false; s.ShowSeconds = false;
            Check(ClockText.Format(DateTimeOffset.Parse("2026-01-15T00:00:00Z"), s) == "12:00 AM", "12-hour midnight without seconds");
            Check(ClockText.Format(DateTimeOffset.Parse("2026-01-15T12:00:00Z"), s) == "12:00 PM", "12-hour noon");
            s.TextColorArgb = Color.Coral.ToArgb(); s.BackgroundColorArgb = Color.Navy.ToArgb(); s.X = -123; s.Y = 456; s.FontSize = 44; s.BackgroundOpacity = 35; s.AlwaysOnTop = false; s.Save(temporary);
            var loaded = Settings.Load(temporary);
            Check(loaded.TimeZoneId == "UTC" && loaded.X == -123 && loaded.Y == 456 && loaded.FontSize == 44 && loaded.BackgroundOpacity == 35 && !loaded.AlwaysOnTop && !loaded.Use24Hour && !loaded.ShowSeconds, "Settings round-trip including negative monitor position");
            Check(loaded.TextColorArgb == Color.Coral.ToArgb() && loaded.BackgroundColorArgb == Color.Navy.ToArgb(), "Independent text and background colors persist");
            File.WriteAllText(temporary, "{}");
            var legacy = Settings.Load(temporary);
            Check(legacy.TextColorArgb == Color.White.ToArgb() && legacy.BackgroundColorArgb == Color.FromArgb(18, 25, 38).ToArgb(), "Existing settings retain default colors");
            File.WriteAllText(temporary, "invalid JSON"); Check(Settings.Load(temporary).TimeZoneId == "Europe/Copenhagen", "Corrupt settings fallback");
            s.TimeZoneId = "invalid"; s.FontSize = 10000; s.BackgroundOpacity = -1; s.Normalize();
            Check(s.TimeZoneId == "Europe/Copenhagen" && s.FontSize == 96 && s.BackgroundOpacity == 0, "Settings validation");
            using var window = new ClockWindow(new Settings());
            window.Show(); Application.DoEvents();
            Check((Native.GetWindowLong(window.Handle, -20) & 8) != 0, "Native HWND topmost enabled");
            window.TopMost = false; Application.DoEvents();
            Check((Native.GetWindowLong(window.Handle, -20) & 8) == 0, "Native HWND topmost disabled");
            Check((Native.GetWindowLong(window.Handle, -20) & 0x08000000) != 0, "Overlay uses no-activate style");
            Check(window.Width > 100 && window.Height > 30, "Clock renders and sizes its layered window");
            // Dispose without Close so the test does not write production settings.
            lines.Add("All checks passed.");
        }
        catch (Exception e) { lines.Add("FAIL: " + e); Environment.ExitCode = 1; }
        finally { if (File.Exists(temporary)) File.Delete(temporary); File.WriteAllLines(report, lines); }
    }
}
