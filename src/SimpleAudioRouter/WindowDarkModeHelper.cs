using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SimpleAudioRouter;

internal static class WindowDarkModeHelper
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20h1 = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    public static void RegisterForAllWindows()
    {
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded),
            true);
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
            TryEnableDarkTitleBar(window);
    }

    public static void RefreshAllOpenWindows()
    {
        if (System.Windows.Application.Current is null)
            return;

        foreach (Window window in System.Windows.Application.Current.Windows)
            TryEnableDarkTitleBar(window);
    }

    public static void TryEnableDarkTitleBar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var useDark = ThemeManager.IsDarkMode ? 1 : 0;
        if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20h1, ref useDark, sizeof(int));
    }
}
