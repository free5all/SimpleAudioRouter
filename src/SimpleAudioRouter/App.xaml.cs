using System.Windows;
using SimpleAudioRouter.Core.Settings;

namespace SimpleAudioRouter;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        AppUserModelHelper.Register();
        ThemeManager.Initialize();
        WindowDarkModeHelper.RegisterForAllWindows();

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

        if (startMinimized)
            mainWindow.EnsureInitialized();
        else
            mainWindow.Show();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}
