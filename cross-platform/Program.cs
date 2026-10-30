using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Simple;
using Avalonia.Styling;

namespace ClockOverlay;
internal static class Program
{
    internal const string Version = "2.0.0-preview.2";
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--self-test") return Checks.Run(args[1]);
        if (args.Length == 2 && args[0] == "--apply-update") return Installer.Apply(args[1]);
        if (args.Length == 4 && args[0] == "--package") { Distribution.Create(args[1], args[2], args[3]); return 0; }
        return AppBuilder.Configure<ClockApplication>().UsePlatformDetect()
            .With(new Win32PlatformOptions { RenderingMode = new[] { Win32RenderingMode.Software } })
            .With(new X11PlatformOptions { RenderingMode = new[] { X11RenderingMode.Software }, EnableSessionManagement = false, UseDBusMenu = false, UseDBusFilePicker = false })
            .LogToTrace().StartWithClassicDesktopLifetime(args);
    }
}
internal sealed class ClockApplication : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new SimpleTheme());
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.Args is [var mode, var report] && mode is "--ui-check" or "--clock-check")
            {
                Avalonia.Controls.Window window = mode == "--clock-check" ? new ClockWindow(true) : new SettingsWindow(new Preferences(), "UI validation — no update requests made.", false);
                desktop.MainWindow = window;
                window.Opened += async (_, _) =>
                {
                    try
                    {
                        await Task.Delay(500);
                        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height));
                        bitmap.Render(window); bitmap.Save(report + ".png", Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                        File.WriteAllText(report, $"PASS: {mode} window rendered at {window.Bounds.Width} × {window.Bounds.Height}.\n");
                    }
                    catch (Exception e) { File.WriteAllText(report, "FAIL: " + e); }
                    finally { desktop.Shutdown(); }
                };
            }
            else desktop.MainWindow = new ClockWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}



