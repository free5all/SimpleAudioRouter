namespace SimpleAudioRouter;

internal static class StartupShortcutManager
{
    private const string ShortcutName = "SimpleAudioRouter.lnk";

    private static string StartupFolder =>
        Environment.GetFolderPath(Environment.SpecialFolder.Startup);

    private static string ShortcutPath => Path.Combine(StartupFolder, ShortcutName);

    public static bool IsEnabled() => File.Exists(ShortcutPath);

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
            CreateShortcut();
        else
            RemoveShortcut();
    }

    private static void CreateShortcut()
    {
        Directory.CreateDirectory(StartupFolder);
        var exePath = System.Windows.Forms.Application.ExecutablePath;
        var workDir = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory;

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is not available.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(ShortcutPath);
        shortcut.TargetPath = exePath;
        shortcut.Arguments = "";
        shortcut.WorkingDirectory = workDir;
        shortcut.Description = "Simple Audio Router";
        shortcut.IconLocation = $"{exePath},0";
        shortcut.Save();
    }

    private static void RemoveShortcut()
    {
        if (File.Exists(ShortcutPath))
            File.Delete(ShortcutPath);
    }
}
