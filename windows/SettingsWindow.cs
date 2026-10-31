namespace SimpleClockOverlay;

public sealed class SettingsWindow : Form
{
    public bool InstallUpdateRequested { get; private set; }
    public SettingsWindow(Settings settings, string updateStatus = "", string? downloadFolder = null, bool canInstall = false, Func<Task<(string Status, bool CanInstall)>>? checkUpdates = null)
    {
        Text = $"Clock Overlay {UpdateService.CurrentVersion} · Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(530, 775);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(22, 29, 42); ForeColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 2, RowCount = 15, AutoScroll = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        Controls.Add(layout);
        void Add(string label, Control control) { var row = layout.Controls.Count / 2; layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row); control.Anchor = AnchorStyles.Left | AnchorStyles.Right; layout.Controls.Add(control, 1, row); }
        var zones = TimeZoneInfo.GetSystemTimeZones().ToList();
        var selectedZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        if (!zones.Any(z => z.Id == selectedZone.Id)) zones.Insert(0, selectedZone);
        var zone = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "DisplayName", DataSource = zones, DropDownWidth = 650 };
        zone.SelectedItem = zones.First(z => z.Id == selectedZone.Id);
        var top = new CheckBox { Text = "Enabled", Checked = settings.AlwaysOnTop, AutoSize = true };
        var format = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        format.Items.AddRange(["24-hour", "12-hour (AM/PM)"]); format.SelectedIndex = settings.Use24Hour ? 0 : 1;
        var seconds = new CheckBox { Text = "Show seconds", Checked = settings.ShowSeconds, AutoSize = true };
        var fontSize = new NumericUpDown { Minimum = 14, Maximum = 96, Value = settings.FontSize };
        var opacity = new NumericUpDown { Minimum = 0, Maximum = 100, Value = settings.BackgroundOpacity, Increment = 5 };
        Add("Timezone", zone); Add("Always on Top", top); Add("Time format", format); Add("Seconds", seconds); Add("Font size (pt)", fontSize); Add("Background (%)", opacity);
        var textColor = Color.FromArgb(settings.TextColorArgb);
        var backgroundColor = Color.FromArgb(settings.BackgroundColorArgb);
        void Preview(Button button, Color color)
        {
            button.BackColor = color;
            button.ForeColor = color.GetBrightness() > 0.5f ? Color.Black : Color.White;
            button.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2} · Choose…";
        }
        Button ColorButton(Color initial, Action<Color> select)
        {
            var button = new Button { AutoSize = true, UseVisualStyleBackColor = false };
            Preview(button, initial);
            button.Click += (_, _) =>
            {
                using var picker = new ColorDialog { Color = button.BackColor, FullOpen = true };
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                select(picker.Color);
                Preview(button, picker.Color);
            };
            return button;
        }
        var textColorButton = ColorButton(textColor, color => textColor = color);
        var backgroundColorButton = ColorButton(backgroundColor, color => backgroundColor = color);
        Add("Text color", textColorButton);
        Add("Background color", backgroundColorButton);
        var resetColors = new Button { Text = "Reset all colors", AutoSize = true, ForeColor = Color.Black, BackColor = Color.White };
        resetColors.Click += (_, _) =>
        {
            var defaults = new Settings();
            textColor = Color.FromArgb(defaults.TextColorArgb);
            backgroundColor = Color.FromArgb(defaults.BackgroundColorArgb);
            Preview(textColorButton, textColor);
            Preview(backgroundColorButton, backgroundColor);
        };
        Add("", resetColors);
        var startup = new CheckBox { Text = "Start with Windows", AutoSize = true };
        try { startup.Checked = WindowsStartup.Enabled(); } catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException) { startup.Enabled = false; }
        Add("Startup", startup);
        var autoUpdates = new CheckBox { Text = "Check every 48 hours", Checked = settings.AutoUpdates, AutoSize = true };
        Add("Auto updates", autoUpdates);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        var noticePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        noticePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        noticePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        var updateNotice = new Label { Text = UpdateService.Notice, Dock = DockStyle.Fill, ForeColor = Color.LightSteelBlue, Margin = Padding.Empty };
        var privacyLink = new LinkLabel { Text = "GitHub Privacy Statement (data handling)", Dock = DockStyle.Fill, LinkColor = Color.LightSkyBlue, ActiveLinkColor = Color.White, VisitedLinkColor = Color.LightSkyBlue, TextAlign = ContentAlignment.MiddleLeft, AccessibleName = "Read GitHub's privacy statement", Margin = Padding.Empty };
        privacyLink.LinkClicked += (_, _) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement") { UseShellExecute = true }); }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
            { MessageBox.Show(this, "Could not open your browser.\nhttps://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement", "GitHub privacy statement"); }
        };
        noticePanel.Controls.Add(updateNotice, 0, 0);
        noticePanel.Controls.Add(privacyLink, 0, 1);
        layout.Controls.Add(noticePanel, 0, 11); layout.SetColumnSpan(noticePanel, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        var updateDownloads = new Button { Text = canInstall ? "Update and restart" : updateStatus, Dock = DockStyle.Fill, Enabled = canInstall, ForeColor = Color.Black, BackColor = Color.White };
        var forceCheck = new Button { Text = "Force update check", Dock = DockStyle.Fill, Enabled = checkUpdates is not null, ForeColor = Color.Black, BackColor = Color.White };
        layout.Controls.Add(forceCheck, 0, 12); layout.Controls.Add(updateDownloads, 1, 12);
        forceCheck.Click += async (_, _) =>
        {
            forceCheck.Enabled = false; updateDownloads.Enabled = false; forceCheck.Text = "Checking…";
            try
            {
                var result = await checkUpdates!();
                if (IsDisposed) return;
                updateDownloads.Text = result.CanInstall ? "Update and restart" : result.Status;
                updateDownloads.Enabled = result.CanInstall;
            }
            catch (Exception e) { if (!IsDisposed) updateDownloads.Text = "Check failed: " + e.Message; }
            finally { if (!IsDisposed) { forceCheck.Text = "Force update check"; forceCheck.Enabled = true; } }
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        var hint = new Label { Text = "Drag the clock to move it. Right-click to exit.\n0% background leaves only the clock text visible.", AutoSize = true, ForeColor = Color.LightSteelBlue, Dock = DockStyle.Fill };
        layout.Controls.Add(hint, 0, 13); layout.SetColumnSpan(hint, 2);
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var save = new Button { Text = "Save", AutoSize = true, ForeColor = Color.Black, BackColor = Color.White };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel, ForeColor = Color.Black, BackColor = Color.White };
        buttons.Controls.Add(save); buttons.Controls.Add(cancel); layout.Controls.Add(buttons, 0, 14); layout.SetColumnSpan(buttons, 2);
        AcceptButton = save; CancelButton = cancel;

        save.Click += (_, _) => {
            try { if (startup.Enabled) WindowsStartup.SetEnabled(startup.Checked); }
            catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            { MessageBox.Show(this, "Could not change Windows startup.\n" + e.Message, "Clock Overlay"); return; }
            settings.AutoUpdates = autoUpdates.Checked;
            settings.TimeZoneId = ((TimeZoneInfo)zone.SelectedItem!).Id; settings.AlwaysOnTop = top.Checked; settings.Use24Hour = format.SelectedIndex == 0; settings.ShowSeconds = seconds.Checked; settings.FontSize = (int)fontSize.Value; settings.BackgroundOpacity = (int)opacity.Value; settings.TextColorArgb = textColor.ToArgb(); settings.BackgroundColorArgb = backgroundColor.ToArgb(); DialogResult = DialogResult.OK; Close(); };
        updateDownloads.Click += (_, _) => { InstallUpdateRequested = true; save.PerformClick(); };
    }
}
