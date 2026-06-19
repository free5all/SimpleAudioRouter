using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using MediaColor = System.Windows.Media.Color;

namespace SimpleAudioRouter;

internal static class ThemeManager
{
    private static ResourceDictionary? _colors;

    public static bool IsDarkMode { get; private set; } = true;

    public static event Action? ThemeChanged;

    public static void Initialize()
    {
        _colors = new ResourceDictionary();
        System.Windows.Application.Current.Resources.MergedDictionaries.Insert(0, _colors);
        Apply();
        RegisterForSystemChanges();
    }

    public static void Apply()
    {
        if (_colors is null || System.Windows.Application.Current is null)
            return;

        IsDarkMode = ReadSystemPrefersDark();
        var palette = IsDarkMode ? DarkPalette : LightPalette;

        _colors["BgColor"] = palette.Bg;
        _colors["SurfaceColor"] = palette.Surface;
        _colors["SurfaceRaisedColor"] = palette.SurfaceRaised;
        _colors["BorderColor"] = palette.Border;
        _colors["AccentColor"] = palette.Accent;
        _colors["TextColor"] = palette.Text;
        _colors["MutedTextColor"] = palette.MutedText;
        _colors["MeterFillColor"] = palette.MeterFill;

        _colors["BgBrush"] = CreateBrush(palette.Bg);
        _colors["SurfaceBrush"] = CreateBrush(palette.Surface);
        _colors["SurfaceRaisedBrush"] = CreateBrush(palette.SurfaceRaised);
        _colors["BorderBrush"] = CreateBrush(palette.Border);
        _colors["AccentBrush"] = CreateBrush(palette.Accent);
        _colors["TextBrush"] = CreateBrush(palette.Text);
        _colors["MutedTextBrush"] = CreateBrush(palette.MutedText);
        _colors["MeterFillBrush"] = CreateBrush(palette.MeterFill);
        _colors["StatusIdleBrush"] = CreateBrush(palette.MutedText);

        System.Windows.Application.Current.ThemeMode = IsDarkMode ? ThemeMode.Dark : ThemeMode.Light;

        WindowDarkModeHelper.RefreshAllOpenWindows();
        ThemeChanged?.Invoke();
    }

    private static SolidColorBrush CreateBrush(MediaColor color) => new(color);

    private static void RegisterForSystemChanges()
    {
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category != UserPreferenceCategory.General)
                return;

            var app = System.Windows.Application.Current;
            if (app is null)
                return;

            if (!app.Dispatcher.CheckAccess())
                app.Dispatcher.Invoke(Apply);
            else
                Apply();
        };
    }

    private static bool ReadSystemPrefersDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return true;
        }
    }

    private readonly record struct Palette(
        MediaColor Bg,
        MediaColor Surface,
        MediaColor SurfaceRaised,
        MediaColor Border,
        MediaColor Accent,
        MediaColor Text,
        MediaColor MutedText,
        MediaColor MeterFill);

    private static readonly Palette DarkPalette = new(
        Bg: ColorFromRgb(0x11, 0x11, 0x1B),
        Surface: ColorFromRgb(0x1E, 0x1E, 0x2E),
        SurfaceRaised: ColorFromRgb(0x31, 0x32, 0x44),
        Border: ColorFromRgb(0x45, 0x47, 0x5A),
        Accent: ColorFromRgb(0x89, 0xB4, 0xFA),
        Text: ColorFromRgb(0xCD, 0xD6, 0xF4),
        MutedText: ColorFromRgb(0x93, 0x99, 0xB2),
        MeterFill: ColorFromRgb(0x94, 0xE2, 0xD5));

    private static readonly Palette LightPalette = new(
        Bg: ColorFromRgb(0xEF, 0xF1, 0xF5),
        Surface: ColorFromRgb(0xFF, 0xFF, 0xFF),
        SurfaceRaised: ColorFromRgb(0xE6, 0xE9, 0xEF),
        Border: ColorFromRgb(0xCC, 0xD0, 0xDA),
        Accent: ColorFromRgb(0x1E, 0x66, 0xF5),
        Text: ColorFromRgb(0x4C, 0x4F, 0x69),
        MutedText: ColorFromRgb(0x6C, 0x6F, 0x85),
        MeterFill: ColorFromRgb(0x17, 0x92, 0x99));

    private static MediaColor ColorFromRgb(byte r, byte g, byte b) => MediaColor.FromRgb(r, g, b);
}
