using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimpleAudioRouter;

internal static class AppIconHelper
{
    private const string LogoFileName = "SimpleAudioRouter.png";

    private static Icon? _cachedIcon;
    private static readonly Dictionary<TrayIconState, Icon> TrayIcons = new();

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon GetAppIcon() => _cachedIcon ??= LoadMasterIcon();

    public static Bitmap GetMenuBitmap()
    {
        using var icon = (Icon)GetAppIcon().Clone();
        using var source = icon.ToBitmap();
        return new Bitmap(source, new System.Drawing.Size(16, 16));
    }

    public static ImageSource GetAppImageSource()
    {
        using var icon = (Icon)GetAppIcon().Clone();
        using var bitmap = icon.ToBitmap();
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    public static Icon GetTrayIcon(TrayIconState state)
    {
        if (TrayIcons.TryGetValue(state, out var cached))
            return cached;

        var icon = CreateTrayIcon(state);
        TrayIcons[state] = icon;
        return icon;
    }

    public static void ClearCache()
    {
        _cachedIcon?.Dispose();
        _cachedIcon = null;

        foreach (var icon in TrayIcons.Values)
            icon.Dispose();

        TrayIcons.Clear();
    }

    private static Icon LoadMasterIcon()
    {
        foreach (var loader in new Func<Icon?>[]
        {
            LoadFromLogoPng,
            LoadFromAppExecutable,
            () => null,
        })
        {
            try
            {
                var icon = loader();
                if (icon is not null)
                    return icon;
            }
            catch
            {
                // Try the next source.
            }
        }

        return CreateFallbackIcon();
    }

    private static Icon? LoadFromLogoPng()
    {
        foreach (var path in GetAssetCandidates(LogoFileName))
        {
            if (!File.Exists(path))
                continue;

            using var bitmap = new Bitmap(path);
            return IconFromBitmap(bitmap, 32);
        }

        return null;
    }

    private static IEnumerable<string> GetAssetCandidates(string fileName)
    {
        yield return Path.Combine(AppContext.BaseDirectory, "assets", fileName);

        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrWhiteSpace(assemblyDir))
            yield return Path.Combine(assemblyDir, "assets", fileName);
    }

    private static Icon? LoadFromAppExecutable()
    {
        var exePath = System.Windows.Forms.Application.ExecutablePath;
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            return null;

        if (IsDotNetHost(exePath))
            return null;

        var icon = Icon.ExtractAssociatedIcon(exePath);
        if (icon is null)
            return null;

        var clone = (Icon)icon.Clone();
        icon.Dispose();
        return clone;
    }

    private static bool IsDotNetHost(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            || name.Equals("net", StringComparison.OrdinalIgnoreCase);
    }

    private static Icon IconFromBitmap(Bitmap bitmap, int size)
    {
        using var sized = new Bitmap(bitmap, new System.Drawing.Size(size, size));
        var handle = sized.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static Icon CreateFallbackIcon()
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(System.Drawing.Color.FromArgb(255, 17, 17, 27));

            var margin = 4;
            var mid = size / 2;
            using var leftBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 148, 226, 213));
            using var rightBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 137, 180, 250));
            graphics.FillPolygon(leftBrush, new[]
            {
                new System.Drawing.Point(mid, margin + 2),
                new System.Drawing.Point(margin, size - margin),
                new System.Drawing.Point(mid, size - margin),
            });
            graphics.FillPolygon(rightBrush, new[]
            {
                new System.Drawing.Point(mid, margin + 2),
                new System.Drawing.Point(size - margin, size - margin),
                new System.Drawing.Point(mid, size - margin),
            });
        }

        return IconFromBitmap(bitmap, size);
    }

    private static Icon CreateTrayIcon(TrayIconState state)
    {
        using var source = (Icon)GetAppIcon().Clone();
        using var sourceBitmap = source.ToBitmap();

        const int size = 16;
        using var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawImage(sourceBitmap, new Rectangle(0, 0, size, size));

            if (state != TrayIconState.Idle)
            {
                var badgeColor = state switch
                {
                    TrayIconState.Routing => System.Drawing.Color.FromArgb(255, 148, 226, 213),
                    TrayIconState.Warning => System.Drawing.Color.FromArgb(255, 249, 226, 175),
                    TrayIconState.Error => System.Drawing.Color.FromArgb(255, 243, 139, 168),
                    _ => System.Drawing.Color.Transparent,
                };

                using var brush = new SolidBrush(badgeColor);
                graphics.FillEllipse(brush, size - 7, size - 7, 6, 6);
            }
        }

        return IconFromBitmap(bitmap, size);
    }
}
