namespace SimpleAudioRouter;

internal static class AppInfo
{
    public static string Version { get; } = typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public static string ProductName { get; } = "Simple Audio Router";
}
