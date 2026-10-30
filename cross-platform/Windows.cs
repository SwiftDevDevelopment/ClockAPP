using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace ClockOverlay;
internal sealed class ClockWindow : Window
{
    Preferences preferences;
    readonly bool testing;
    readonly TextBlock text = new() { Foreground = Brushes.White, FontFamily = new FontFamily("Segoe UI, Noto Sans, DejaVu Sans, Helvetica Neue, sans-serif"), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
    readonly Border background = new() { CornerRadius = new CornerRadius(12), Padding = new Thickness(16, 8) };
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    readonly Updates updates = new();
    readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    bool settingsOpen;
    string lastText = "";
    internal ClockWindow(bool testing = false)
    {
        this.testing = testing;
        preferences = testing ? new Preferences() : Preferences.Load();
        Title = "Simple Clock Overlay (cross-platform preview)";
        WindowDecorations = Avalonia.Controls.WindowDecorations.None; ShowInTaskbar = false;
        CanResize = false; ShowActivated = false; SizeToContent = SizeToContent.WidthAndHeight;
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        background.Child = text; Content = background;
        var menu = new ContextMenu();
        var settings = new MenuItem { Header = "Settings…" };
        settings.Click += async (_, _) => await OpenSettings();
        var exit = new MenuItem { Header = "Exit" }; exit.Click += (_, _) => Close();
        menu.Items.Add(settings); menu.Items.Add(exit); background.ContextMenu = menu;
        PointerPressed += (_, e) => { if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e); };
        PointerReleased += (_, _) => Save();
        timer.Tick += (_, _) => Tick();
        updateTimer.Tick += async (_, _) => await updates.Check(preferences.AutoUpdates);
        Opened += async (_, _) => { Apply(); RestorePosition(); timer.Start(); if (!testing) { updateTimer.Start(); await updates.Check(preferences.AutoUpdates); } };
        Closed += (_, _) => { timer.Stop(); updateTimer.Stop(); updates.Dispose(); Save(); };
    }
    void Tick()
    {
        var current = preferences.Format(DateTimeOffset.UtcNow);
        if (current != lastText) { text.Text = current; lastText = current; }
    }
    void Apply()
    {
        Topmost = preferences.AlwaysOnTop; text.FontSize = preferences.FontSize * 96d / 72;
        text.Foreground = new SolidColorBrush(Color.Parse(preferences.TextColor));
        background.Background = new SolidColorBrush(preferences.Background);
        lastText = ""; Tick();
    }
    void RestorePosition()
    {
        var screens = Screens.All;
        if (preferences.X is int x && preferences.Y is int y)
        {
            var point = new PixelPoint(x, y);
            if (screens.Any(s => s.WorkingArea.Contains(point))) { Position = point; return; }
        }
        var primary = Screens.Primary ?? screens.FirstOrDefault();
        if (primary is not null)
        {
            var width = Math.Max(240, (int)(Bounds.Width * primary.Scaling));
            Position = new PixelPoint(primary.WorkingArea.Right - width - 24, primary.WorkingArea.Y + 24);
        }
    }
    void Save()
    {
        if (testing) return;
        preferences.X = Position.X; preferences.Y = Position.Y;
        try { preferences.Save(); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { System.Diagnostics.Trace.WriteLine(e.Message); }
    }
    async Task OpenSettings()
    {
        if (settingsOpen) return;
        settingsOpen = true; Topmost = false;
        try
        {
            var dialog = new SettingsWindow(preferences.Copy(), updates.Status, updates.CanInstall);
            var accepted = await dialog.ShowDialog<bool>(this);
            if (!accepted) return;
            preferences = dialog.Value; Apply(); Save();
            if (dialog.InstallRequested)
            {
                try { IsEnabled = false; await updates.Install(); Close(); }
                catch (Exception e) { IsEnabled = true; await ShowMessage("Update could not start. The clock stays open.\n" + e.Message); }
            }
        }
        finally { settingsOpen = false; Topmost = preferences.AlwaysOnTop; }
    }
    internal async Task ShowMessage(string message)
    {
        var window = new Window { Title = "Clock Overlay", Width = 440, Height = 180, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(20) } };
        await window.ShowDialog(this);
    }
}

internal sealed class SettingsWindow : Window
{
    internal Preferences Value { get; }
    internal bool InstallRequested { get; private set; }
    internal SettingsWindow(Preferences value, string updateStatus, bool canInstall)
    {
        Value = value;
        Title = "Clock Overlay " + Program.Version + " · Settings";
        Width = 500; Height = 760; MinWidth = 380; MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var stack = new StackPanel { Spacing = 10, Margin = new Thickness(20) };
        var root = new DockPanel();
        Content = root;
        root.Children.Add(new ScrollViewer { Content = stack, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled });
        void Add(string title, Control control) { stack.Children.Add(new TextBlock { Text = title }); stack.Children.Add(control); }
        var zones = TimeZoneInfo.GetSystemTimeZones().ToList();
        var selected = TimeZoneInfo.FindSystemTimeZoneById(value.TimeZoneId);
        if (!zones.Any(z => z.Id == selected.Id)) zones.Insert(0, selected);
        var zone = new ComboBox { ItemsSource = zones, SelectedItem = zones.First(z => z.Id == selected.Id), HorizontalAlignment = HorizontalAlignment.Stretch };
        Add("Timezone", zone);
        var top = new CheckBox { Content = "Always on Top", IsChecked = value.AlwaysOnTop }; stack.Children.Add(top);
        var format = new ComboBox { ItemsSource = new[] { "24-hour", "12-hour (AM/PM)" }, SelectedIndex = value.Use24Hour ? 0 : 1 }; Add("Time format", format);
        var seconds = new CheckBox { Content = "Show seconds", IsChecked = value.ShowSeconds }; stack.Children.Add(seconds);
        var size = new NumericUpDown { Minimum = 14, Maximum = 96, Value = value.FontSize, FormatString = "0" }; Add("Font size (points)", size);
        var opacity = new Slider { Minimum = 0, Maximum = 100, Value = value.BackgroundOpacity, TickFrequency = 5, IsSnapToTickEnabled = true }; Add("Background opacity (0–100%)", opacity);
        var foreground = new TextBox { Text = value.TextColor, MaxLength = 7 };
        var backgroundColor = new TextBox { Text = value.BackgroundColor, MaxLength = 7 };
        Add("Text color (#RRGGBB)", foreground); Add("Background color (#RRGGBB)", backgroundColor);
        var reset = new Button { Content = "Reset all colors" }; reset.Click += (_, _) => { foreground.Text = "#FFFFFF"; backgroundColor.Text = "#121926"; }; stack.Children.Add(reset);
        var auto = new CheckBox { Content = Updates.Supported ? "Check for updates every 48 hours" : "macOS preview: manual updates only", IsChecked = Updates.Supported && value.AutoUpdates, IsEnabled = Updates.Supported }; stack.Children.Add(auto);
        stack.Children.Add(new TextBlock { Text = Updates.Notice, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        var policy = new Button { Content = "GitHub Privacy Statement ↗" };
        policy.Click += async (_, _) => { if (Launcher is not null) await Launcher.LaunchUriAsync(new Uri("https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement")); }; stack.Children.Add(policy);
        if (!Updates.Supported)
        {
            var releases = new Button { Content = "Download updates from GitHub Releases ↗" };
            releases.Click += async (_, _) => { if (Launcher is not null) await Launcher.LaunchUriAsync(new Uri(Updates.Repository + "/releases")); };
            stack.Children.Add(releases);
        }
        stack.Children.Add(new TextBlock { Text = updateStatus, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        var platformNotice = OperatingSystem.IsLinux() ? "Experimental Linux support: intended for X11/XWayland. Positioning, transparency and Always on Top depend on your desktop/compositor; native Wayland is not supported by this preview. Additional code may be needed from the maintainer or community. Pull requests welcome." : OperatingSystem.IsMacOS() ? "Experimental macOS build: unsigned and not verified on a Mac. Spaces/fullscreen behavior and updating require testing. Pull requests welcome." : "Cross-platform preview: separate from the stable, lighter Windows Forms edition.";
        stack.Children.Add(new TextBlock { Text = platformNotice, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        var error = new TextBlock { Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap }; stack.Children.Add(error);
        bool SaveValue()
        {
            if (!Preferences.ValidColor(foreground.Text) || !Preferences.ValidColor(backgroundColor.Text)) { error.Text = "Enter each color as #RRGGBB, for example #FFFFFF."; return false; }
            value.TimeZoneId = ((TimeZoneInfo)zone.SelectedItem!).Id; value.AlwaysOnTop = top.IsChecked == true;
            value.Use24Hour = format.SelectedIndex == 0; value.ShowSeconds = seconds.IsChecked == true;
            value.FontSize = (int)(size.Value ?? 32); value.BackgroundOpacity = (int)opacity.Value;
            value.TextColor = foreground.Text!; value.BackgroundColor = backgroundColor.Text!; value.AutoUpdates = auto.IsChecked == true;
            value.Normalize(); return true;
        }
        var update = new Button { Content = "Update and restart", IsEnabled = canInstall };
        update.Click += (_, _) => { if (SaveValue()) { InstallRequested = true; Close(true); } }; stack.Children.Add(update);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var save = new Button { Content = "Save" }; save.Click += (_, _) => { if (SaveValue()) Close(true); };
        var cancel = new Button { Content = "Cancel" }; cancel.Click += (_, _) => Close(false);
        buttons.Children.Add(save); buttons.Children.Add(cancel);
        buttons.Margin = new Thickness(20, 8, 20, 16);
        DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Insert(0, buttons);
    }
}

