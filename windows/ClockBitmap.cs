using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Drawing.Drawing2D;

namespace SimpleClockOverlay;

internal static class ClockBitmap
{
    internal static Bitmap Render(Settings settings, DateTimeOffset now, int dpi)
    {
        var scale = dpi / 96f;
        // Explicit pixel sizes keep measurement and drawing at the same monitor DPI.
        using var font = new Font("Segoe UI", settings.FontSize * dpi / 72f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var dateFont = new Font("Segoe UI Semibold", Math.Max(9f, settings.FontSize * 0.5f) * dpi / 72f, FontStyle.Regular, GraphicsUnit.Pixel);
        var time = ClockText.Format(now, settings);
        var date = ClockText.Date(now, settings);
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        using var probe = new Bitmap(1, 1);
        using var measure = Graphics.FromImage(probe);
        measure.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var timeSize = measure.MeasureString(time, font, int.MaxValue, format);
        var dateSize = measure.MeasureString(date, dateFont, int.MaxValue, format);
        var paddingX = (int)Math.Ceiling(12 * scale);
        var paddingY = (int)Math.Ceiling(3 * scale);
        var gap = (int)Math.Ceiling(2 * scale);
        var timeHeight = (int)Math.Ceiling(timeSize.Height);
        var dateHeight = (int)Math.Ceiling(dateSize.Height);
        var width = (int)Math.Ceiling(Math.Max(timeSize.Width, dateSize.Width)) + paddingX * 2;
        var height = timeHeight + dateHeight + gap + paddingY * 2;
        // Layered windows consume premultiplied alpha, including antialiased edges.
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = new GraphicsPath();
        var diameter = Math.Min(24 * scale, Math.Min(width - 1, height - 1));
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(width - diameter - 1, 0, diameter, diameter, 270, 90);
        path.AddArc(width - diameter - 1, height - diameter - 1, diameter, diameter, 0, 90);
        path.AddArc(0, height - diameter - 1, diameter, diameter, 90, 90); path.CloseFigure();
        using var background = new SolidBrush(Color.FromArgb(settings.BackgroundOpacity * 255 / 100, Color.FromArgb(settings.BackgroundColorArgb)));
        g.FillPath(background, path);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var foreground = new SolidBrush(Color.FromArgb(settings.TextColorArgb));
        g.DrawString(time, font, foreground, (int)Math.Round((width - timeSize.Width) / 2), paddingY, format);
        g.DrawString(date, dateFont, foreground, (int)Math.Round((width - dateSize.Width) / 2), paddingY + timeHeight + gap, format);
        return bitmap;
    }

    internal static void Check(string folder)
    {
        Directory.CreateDirectory(folder);
        var lines = new List<string>();
        var now = DateTimeOffset.Parse("2026-10-31T08:21:32Z");
        foreach (var dpi in new[] { 96, 120, 144, 192 })
        foreach (var opacity in new[] { 0, 80 })
        {
            using var bitmap = Render(new Settings { BackgroundOpacity = opacity }, now, dpi);
            bitmap.Save(Path.Combine(folder, $"clock-{dpi}dpi-{opacity}opacity.png"), ImageFormat.Png);
            if (bitmap.Width < 100 || bitmap.Height < 40 || bitmap.GetPixel(0, 0).A != 0)
                throw new Exception("Clock rendering or transparent corner failed.");
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
                var pixels = new byte[data.Stride * bitmap.Height];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                bool hasText = false;
                for (var index = 0; index < pixels.Length; index += 4)
                {
                    var alpha = pixels[index + 3];
                    if (pixels[index] > alpha || pixels[index + 1] > alpha || pixels[index + 2] > alpha)
                        throw new Exception("Layered bitmap contains non-premultiplied edge pixels.");
                    hasText |= alpha == 255;
                }
                if (!hasText) throw new Exception("Clock text missing from bitmap.");
            }
            finally { bitmap.UnlockBits(data); }
            lines.Add($"PASS: {dpi} DPI, {opacity}% background, {bitmap.Width} x {bitmap.Height}; transparent corners retained.");
        }
        File.WriteAllLines(Path.Combine(folder, "render-checks.txt"), lines);
    }
}
