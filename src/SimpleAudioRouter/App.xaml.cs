using System.Windows;
using Microsoft.Win32;
using SimpleAudioRouter.Core.Settings;

namespace SimpleAudioRouter;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        WindowDarkModeHelper.RegisterForAllWindows();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        var settings = AppSettings.Load();
        var startMinimized = settings.StartMinimized
            || e.Args.Any(a => string.Equals(a, "/tray", StringComparison.OrdinalIgnoreCase));

        _mutex = new Mutex(true, Program.MutexName, out var createdNew);
        if (!createdNew)
        {
            SingleInstancePipe.SignalShowWindow();
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow(startMinimized);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General)
            return;

        WindowDarkModeHelper.RefreshAllOpenWindows();
    }
}
