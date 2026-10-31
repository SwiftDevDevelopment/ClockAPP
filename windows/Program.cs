using System.Globalization;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Text.Json;

namespace SimpleClockOverlay;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--apply-update") { UpdateInstaller.Run(args[1]); return; }
        if (args.Length == 2 && args[0] == "--update-tests") { UpdateTests.Run(args[1]); return; }
        if (args.Length == 2 && args[0] == "--self-test") { SelfTests.Run(args[1]); return; }
        Application.Run(new ClockWindow());
    }
}

public sealed class Settings
{
    public string TimeZoneId { get; set; } = "Europe/Copenhagen";
    public bool AlwaysOnTop { get; set; } = true;
    public bool Use24Hour { get; set; } = true;
    public bool ShowSeconds { get; set; } = true;
    public bool AutoUpdates { get; set; } = true;
    public int FontSize { get; set; } = 32;
    public int BackgroundOpacity { get; set; } = 80;
    public int TextColorArgb { get; set; } = Color.White.ToArgb();
    public int BackgroundColorArgb { get; set; } = Color.FromArgb(18, 25, 38).ToArgb();
    public int? X { get; set; }
    public int? Y { get; set; }
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleClockOverlay", "settings.json");
    public void Normalize()
    {
        FontSize = Math.Clamp(FontSize, 14, 96);
        BackgroundOpacity = Math.Clamp(BackgroundOpacity, 0, 100);
        TextColorArgb = Color.FromArgb(255, Color.FromArgb(TextColorArgb)).ToArgb();
        BackgroundColorArgb = Color.FromArgb(255, Color.FromArgb(BackgroundColorArgb)).ToArgb();
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); }
        catch { TimeZoneId = "Europe/Copenhagen"; }
    }
    public static Settings Load(string? path = null)
    {
        try { var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path ?? DefaultPath)) ?? new(); s.Normalize(); return s; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(path + ".tmp", path, true);
    }
}

public static class ClockText
{
    public static string Date(DateTimeOffset utc, Settings s) => TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById(s.TimeZoneId)).ToString("d", CultureInfo.CurrentCulture);
    public static string Format(DateTimeOffset utc, Settings s) => TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById(s.TimeZoneId)).ToString(s.Use24Hour ? (s.ShowSeconds ? "HH:mm:ss" : "HH:mm") : (s.ShowSeconds ? "hh:mm:ss tt" : "hh:mm tt"), CultureInfo.InvariantCulture);
}

public sealed class ClockWindow : Form
{
    readonly Settings settings;
    readonly UpdateService updates = new();
    readonly System.Windows.Forms.Timer updateTimer = new() { Interval = 60000 };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    readonly ContextMenuStrip menu = new();
    bool dragging;
    Point dragStart;
    public ClockWindow(Settings? initial = null)
    {
        settings = initial ?? Settings.Load();
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = settings.AlwaysOnTop;
        AccessibleName = "Clock overlay";
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        menu.Items.Add("Exit", null, (_, _) => Close());
        timer.Tick += (_, _) => RenderClock();
        updateTimer.Tick += async (_, _) => await updates.CheckAsync(settings.AutoUpdates);
        Shown += async (_, _) => { if (initial is null) { updateTimer.Start(); await updates.CheckAsync(settings.AutoUpdates); } };
        Shown += (_, _) => { RenderClock(); RestorePosition(); RenderClock(); timer.Start(); };
        FormClosed += (_, _) => { timer.Dispose(); updateTimer.Dispose(); updates.Dispose(); menu.Dispose(); SavePosition(); };
    }
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams { get { var p = base.CreateParams; p.ExStyle |= 0x80000 | 0x80 | 0x08000000; return p; } }
    void RestorePosition()
    {
        var area = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(settings.X ?? area.Right - Width - 24, settings.Y ?? area.Top + 24);
        var screen = Screen.AllScreens.FirstOrDefault(s => s.WorkingArea.Contains(new Rectangle(Location, Size)));
        if (screen == null) { Location = new Point(area.Right - Width - 24, area.Top + 24); }
    }
    void SavePosition()
    {
        settings.X = Left; settings.Y = Top;
        try { settings.Save(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { MessageBox.Show("Could not save clock settings.\n" + e.Message, "Clock Overlay"); }
    }
    void OpenSettings()
    {
        using var dialog = new SettingsWindow(settings, updates.Status, updates.DownloadFolder, updates.CanInstall);
        TopMost = false;
        DialogResult result;
        try
        {
            result = dialog.ShowDialog(this);
        }
        finally
        {
            TopMost = settings.AlwaysOnTop;
        }
        if (result != DialogResult.OK) return;
        RenderClock(); RestorePositionToScreen(); SavePosition();
        if (dialog.InstallUpdateRequested) _ = InstallUpdateAsync();
    }
    async Task InstallUpdateAsync()
    {
        try
        {
            Enabled = false;
            await updates.InstallAsync();
            Close();
        }
        catch (Exception e)
        {
            Enabled = true;
            MessageBox.Show("Could not start the update. The clock will stay open.\n" + e.Message, "Clock Overlay update");
        }
    }
    void RestorePositionToScreen()
    {
        var a = Screen.FromRectangle(Bounds).WorkingArea;
        Location = new Point(Math.Clamp(Left, a.Left, Math.Max(a.Left, a.Right - Width)), Math.Clamp(Top, a.Top, Math.Max(a.Top, a.Bottom - Height)));
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x21) { m.Result = (IntPtr)3; return; } // MA_NOACTIVATE
        base.WndProc(ref m);
    }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { dragging = true; dragStart = e.Location; Capture = true; } }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging) { Location = new Point(Left + e.X - dragStart.X, Top + e.Y - dragStart.Y); RenderClock(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if (dragging) { dragging = false; Capture = false; SavePosition(); } if (e.Button == MouseButtons.Right) menu.Show(this, e.Location); }
    public void RenderClock()
    {
        using var font = new Font("Segoe UI", settings.FontSize, FontStyle.Regular, GraphicsUnit.Point);
        var now = DateTimeOffset.UtcNow;
        var text = ClockText.Format(now, settings);
        var date = ClockText.Date(now, settings);
        using var dateFont = new Font("Segoe UI", settings.FontSize * 0.65f, FontStyle.Regular, GraphicsUnit.Point);
        var dateMeasured = TextRenderer.MeasureText(date, dateFont);
        var measured = TextRenderer.MeasureText(text, font);
        Size = new Size(Math.Max(measured.Width, dateMeasured.Width) + 32, measured.Height + dateMeasured.Height + 22);
        using var bitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            const int d = 24;
            path.AddArc(0, 0, d, d, 180, 90); path.AddArc(Width-d-1, 0, d, d, 270, 90); path.AddArc(Width-d-1, Height-d-1, d, d, 0, 90); path.AddArc(0, Height-d-1, d, d, 90, 90); path.CloseFigure();
            using var brush = new SolidBrush(Color.FromArgb(settings.BackgroundOpacity * 255 / 100, Color.FromArgb(settings.BackgroundColorArgb)));
            g.FillPath(brush, path);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var textBrush = new SolidBrush(Color.FromArgb(settings.TextColorArgb));
            g.DrawString(text, font, textBrush, new RectangleF(0, 8, Width, measured.Height), format);
            g.DrawString(date, dateFont, textBrush, new RectangleF(0, 10 + measured.Height, Width, dateMeasured.Height), format);
        }
        var dc = Native.GetDC(IntPtr.Zero); var memory = Native.CreateCompatibleDC(dc); var hb = bitmap.GetHbitmap(Color.FromArgb(0)); var old = Native.SelectObject(memory, hb);
        try { var pos = new Native.Point(Left, Top); var size = new Native.Point(Width, Height); var source = new Native.Point(0, 0); var blend = new Native.Blend { Alpha = 255, Format = 1 }; Native.UpdateLayeredWindow(Handle, dc, ref pos, ref size, memory, ref source, 0, ref blend, 2); }
        finally { Native.SelectObject(memory, old); Native.DeleteObject(hb); Native.DeleteDC(memory); Native.ReleaseDC(IntPtr.Zero, dc); }
    }
}

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; public Point(int x, int y) { X=x; Y=y; } }
    [StructLayout(LayoutKind.Sequential, Pack=1)] internal struct Blend { public byte Op, Flags, Alpha, Format; }
    [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr h, IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")] internal static extern bool UpdateLayeredWindow(IntPtr h, IntPtr dc, ref Point position, ref Point size, IntPtr sourceDc, ref Point source, uint key, ref Blend blend, uint flags);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] internal static extern int GetWindowLong(IntPtr h, int index);
}
